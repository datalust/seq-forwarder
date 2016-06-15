// Copyright 2016 Datalust Pty Ltd
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

using System.Collections.Generic;
using Serilog;
using Serilog.Events;

namespace Seq.Forwarder.Diagnostics
{
    static class IngestionLog
    {
        const int Capacity = 100;

        static readonly InMemorySink _sink = new InMemorySink(Capacity);

        static IngestionLog()
        {
            Log = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Sink(_sink)
                .WriteTo.Logger(Serilog.Log.Logger)
                .CreateLogger();
        }

        public static ILogger Log { get; }

        public static IEnumerable<LogEvent> Read()
        {
            return _sink.Read();
        } 
    }
}
