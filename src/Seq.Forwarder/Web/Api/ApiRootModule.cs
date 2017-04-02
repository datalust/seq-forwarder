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

using System.IO;
using Nancy;
using Seq.Forwarder.Diagnostics;
using Serilog.Formatting.Display;

namespace Seq.Forwarder.Web.Api
{
    class ApiRootModule : NancyModule
    {
        public ApiRootModule()
        {
            Get["/"] = _ => ShowIngestionLog();
            Get["/api"] = _ => Response.AsText("{\"Links\":{\"Events\":\"/api/events/describe\"}}", "application/json");
        }

        Response ShowIngestionLog()
        {
            var events = IngestionLog.Read();
            var formatter = new MessageTemplateTextFormatter("{Timestamp:yyyy-MM-dd HH:mm:ss} {Message}{NewLine}", null);
            return new Response
            {
                ContentType = "text/plain",
                Contents = s =>
                {
                    using (var writer = new StreamWriter(s))
                        foreach (var logEvent in events)
                        {
                            formatter.Format(logEvent, writer);
                        }
                }
            };
        }
    }
}
