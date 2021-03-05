﻿// Copyright 2016-2017 Datalust Pty Ltd
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
using Seq.Forwarder.Cli.Features;
using Seq.Forwarder.Config;
using Seq.Forwarder.Cryptography;
using Seq.Forwarder.Multiplexing;
using Serilog;

namespace Seq.Forwarder.Cli.Commands
{
    [Command("dump", "Print the complete log buffer contents as JSON")]
    class DumpCommand : Command
    {
        readonly StoragePathFeature _storagePath;

        public DumpCommand()
        {
            _storagePath = Enable<StoragePathFeature>();
        }

        protected override int Run(TextWriter cout)
        {
            try
            {
                var config = SeqForwarderConfig.ReadOrInit(_storagePath.ConfigFilePath);
                using var buffer = new ActiveLogBufferMap(_storagePath.BufferPath, config.Storage, config.Output, new InertLogShipperFactory(), StringDataProtector.CreatePlatformDefault());
                buffer.Load();
                buffer.Enumerate((k, v) =>
                {
                    var s = Encoding.UTF8.GetString(v);
                    Console.WriteLine(s);
                });
                return 0;
            }
            catch (Exception ex)
            {
                var logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

                logger.Fatal(ex, "Could not dump events");
                return 1;
            }
        }
    }
}
