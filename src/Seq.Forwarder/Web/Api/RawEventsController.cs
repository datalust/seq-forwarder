// Copyright Datalust Pty Ltd
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
using System.Linq;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Seq.Forwarder.Config;
using Seq.Forwarder.Multiplexing;
using Seq.Forwarder.Shipper;
using Serilog;

namespace Seq.Forwarder.Web.Api
{
    public class RawEventsController : Controller
    {
        static readonly Encoding Encoding = new UTF8Encoding(false);
        
        static ILogger IngestionLog => Diagnostics.IngestionLog.Log;

        readonly ActiveLogBufferMap _logBufferMap;
        readonly SeqForwarderOutputConfig _outputConfig;
        readonly ServerResponseProxy _serverResponseProxy;

        readonly JsonSerializer _rawSerializer = JsonSerializer.Create(
            new JsonSerializerSettings { DateParseHandling = DateParseHandling.None });

        public RawEventsController(ActiveLogBufferMap logBufferMap, SeqForwarderOutputConfig outputConfig, ServerResponseProxy serverResponseProxy)
        {
            _logBufferMap = logBufferMap;
            _outputConfig = outputConfig;
            _serverResponseProxy = serverResponseProxy;
        }

        IPAddress ClientHostIP => Request.HttpContext.Connection.RemoteIpAddress;

        [HttpGet, Route("api/events/describe")]
        public IActionResult Resources()
        {
            return Content("{\"Links\":{\"Raw\":\"/api/events/raw\"}}", "application/json", Encoding);
        }   
        
        [HttpPost, Route("api/events/raw")]
        public IActionResult Ingest()
        {
            JObject posted;
            try
            {
                posted = _rawSerializer.Deserialize<JObject>(new JsonTextReader(new StreamReader(Request.Body))) ??
                    throw new BadRequestException("Request body payload is JSON `null`.");
            }
            catch (Exception ex)
            {
                IngestionLog.Debug(ex, "Rejecting payload from {ClientHostIP} due to invalid JSON, request body could not be parsed", ClientHostIP);
                return BadRequest("Invalid raw event JSON, body could not be parsed.");
            }

            if (posted == null ||
                !(posted.TryGetValue("events", StringComparison.Ordinal, out var eventsToken) ||
                  posted.TryGetValue("Events", StringComparison.Ordinal, out eventsToken)))
            {
                IngestionLog.Debug("Rejecting payload from {ClientHostIP} due to invalid JSON structure", ClientHostIP);
                return BadRequest("Invalid raw event JSON, body must contain an 'Events' array.");
            }

            if (!(eventsToken is JArray events))
            {
                IngestionLog.Debug("Rejecting payload from {ClientHostIP} due to invalid Events property structure", ClientHostIP);
                return BadRequest("Invalid raw event JSON, the 'Events' property must be an array.");
            }
            
            var encoded = new byte[events.Count][];
            var i = 0;
            foreach (var e in events)
            {
                var s = e.ToString(Formatting.None);
                var payload = Encoding.UTF8.GetBytes(s);

                if (payload.Length > (int)_outputConfig.EventBodyLimitBytes)
                {
                    var startToLog = (int)Math.Min(_outputConfig.EventBodyLimitBytes / 2, 1024);
                    var prefix = s.Substring(0, startToLog);
                    IngestionLog.Debug("Invalid payload from {ClientHostIP} due to oversized event; first {StartToLog} chars: {DocumentStart:l}", ClientHostIP, startToLog, prefix);

                    var jo = e as JObject;
                    // ReSharper disable SuspiciousTypeConversion.Global
                    var timestamp = (string?)(dynamic?)jo?.GetValue("Timestamp") ?? DateTime.UtcNow.ToString("o");
                    var level = (string?)(dynamic?)jo?.GetValue("Level") ?? "Warning";

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
                            _outputConfig.EventBodyLimitBytes
                        }
                    }));
                }
                else
                {
                    encoded[i] = payload;
                }
                i++;
            }

            var apiKey = GetRequestApiKeyToken();
            _logBufferMap.GetLogBuffer(apiKey).Enqueue(encoded);
            
            var response = Content(_serverResponseProxy.GetResponseText(apiKey), "application/json", Encoding);
            response.StatusCode = (int)HttpStatusCode.Created;
            return response;
        }

        string? GetRequestApiKeyToken()
        {
            var apiKeyToken = Request.Headers[SeqApi.ApiKeyHeaderName].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(apiKeyToken))
                apiKeyToken = Request.Query["apiKey"];

            var normalized = apiKeyToken?.Trim();
            if (string.IsNullOrEmpty(normalized))
                return null;

            return normalized;
        }
    }
}
