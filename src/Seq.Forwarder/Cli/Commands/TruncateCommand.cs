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
using Seq.Forwarder.Cli.Features;
using Seq.Forwarder.Config;
using Seq.Forwarder.Storage;
using Serilog;

namespace Seq.Forwarder.Cli.Commands
{
    [Command("truncate", "Clear the log buffer contents")]
    class TruncateCommand : Command
    {
        readonly StoragePathFeature _storagePath;

        public TruncateCommand()
        {
            _storagePath = Enable<StoragePathFeature>();
        }

        protected override int Run(TextWriter cout)
        {
            try
            {
                var config = SeqForwarderConfig.Read(_storagePath.ConfigFilePath);
                using (var buffer = new LogBuffer(_storagePath.BufferPath, config.Storage.BufferSizeBytes))
                {
                    buffer.Truncate();
                }
                return 0;
            }
            catch (Exception ex)
            {
                var logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

                logger.Fatal(ex, "Could not truncate log buffer");
                return 1;
            }
        }
    }
}
