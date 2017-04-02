// Copyright 2016-2017 Datalust Pty Ltd
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.IO;
using System.Text;
using Nancy;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Seq.Forwarder.Config;
using Seq.Forwarder.Storage;
using Serilog;

namespace Seq.Forwarder.Web.Api
{
    class RawEventsModule : NancyModule
    {
        static ILogger IngestionLog => Diagnostics.IngestionLog.Log;

        readonly Lazy<LogBuffer> _logBuffer;
        readonly Lazy<SeqForwarderOutputConfig> _outputConfig;

        readonly JsonSerializer _rawSerializer = JsonSerializer.Create(
            new JsonSerializerSettings { DateParseHandling = DateParseHandling.None });

        public RawEventsModule(Lazy<LogBuffer> logBuffer, Lazy<SeqForwarderOutputConfig> outputConfig)
        {
            _logBuffer = logBuffer;
            _outputConfig = outputConfig;

            Get["/api/events/describe"] = _ => Response.AsText("{\"Links\":{\"Raw\":\"/api/events/raw\"}}", "application/json");
            Post["/api/events/raw"] = _ => Ingest();
        }

        Response Ingest()
        {
            JObject posted;
            try
            {
                posted = _rawSerializer.Deserialize<JObject>(new JsonTextReader(new StreamReader(Request.Body)));
            }
            catch (Exception ex)
            {
                IngestionLog.Debug(ex, "Rejecting payload from {ClientHostIP} due to invalid JSON, request body could not be parsed", Request.UserHostAddress);
                return Response
                    .AsText("Invalid raw event JSON, body could not be parsed.")
                    .WithStatusCode(HttpStatusCode.BadRequest);
            }

            if (posted == null ||
                !(posted.TryGetValue("events", StringComparison.Ordinal, out JToken eventsToken) ||
                  posted.TryGetValue("Events", StringComparison.Ordinal, out eventsToken)))
            {
                IngestionLog.Debug("Rejecting payload from {ClientHostIP} due to invalid JSON structure", Request.UserHostAddress);
                return Response
                    .AsText("Invalid raw event JSON, body must contain an 'Events' array.")
                    .WithStatusCode(HttpStatusCode.BadRequest);
            }

            var events = eventsToken as JArray;
            if (events == null)
            {
                IngestionLog.Debug("Rejecting payload from {ClientHostIP} due to invalid Events property structure", Request.UserHostAddress);
                return Response
                    .AsText("Invalid raw event JSON, the 'Events' property must be an array.")
                    .WithStatusCode(HttpStatusCode.BadRequest);
            }
            
            var encoded = new byte[events.Count][];
            var i = 0;
            foreach (var e in events)
            {
                var s = e.ToString(Formatting.None);
                var payload = Encoding.UTF8.GetBytes(s);

                if (payload.Length > (int)_outputConfig.Value.EventBodyLimitBytes)
                {
                    var startToLog = (int)Math.Min(_outputConfig.Value.EventBodyLimitBytes / 2, 1024);
                    var prefix = s.Substring(0, startToLog);
                    IngestionLog.Debug("Invalid payload from {ClientHostIP} due to oversized event; first {StartToLog} chars: {DocumentStart:l}", Request.UserHostAddress, startToLog, prefix);

                    var jo = e as JObject;
                    var timestamp = ((string)(dynamic)jo?.GetValue("Timestamp")) ?? DateTime.UtcNow.ToString("o");
                    var level = ((string)(dynamic)jo?.GetValue("Level")) ?? "Warning";

                    if (jo != null)
                    {
                        jo.Remove("Timestamp");
                        jo.Remove("Level");
                    }

                    var compactPrefix = e.ToString(Formatting.None).Substring(0, startToLog);

                    encoded[i] = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
                    {
                        Timestamp = timestamp,
                        MessageTemplate = "Seq Forwarder received and dropped an oversized event",
                        Level = level,
                        Properties = new
                        {
                            Partial = compactPrefix,
                            Environment.MachineName,
                            _outputConfig.Value.EventBodyLimitBytes
                        }
                    }));
                }
                else
                {
                    encoded[i] = payload;
                }
                i++;
            }

            _logBuffer.Value.Enqueue(encoded);
            
            var response = Response.AsText("{}", "application/json");
            response.StatusCode = HttpStatusCode.Created;
            return response;
        }
    }
}
