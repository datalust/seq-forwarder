// Copyright 2016 Datalust Pty Ltd and contributors
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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Seq.Forwarder.Cli;
using Seq.Forwarder.Importer;
using Serilog;
using System.Linq;
using Seq.Forwarder.Cli.Features;
using Seq.Forwarder.Config;

namespace seq_import
{
    [Command("import", "Import JSON log files directly into Seq")]
    class ImportCommand : Command
    {
        readonly StoragePathFeature _storagePath;
        readonly ServerInformationFeature _serverInformation;
        readonly KeyValuePropertiesFeature _keyValueProperties;

        string _file;

        public ImportCommand()
        {
            _storagePath = Enable<StoragePathFeature>();
            _serverInformation = Enable<ServerInformationFeature>();
            _keyValueProperties = Enable<KeyValuePropertiesFeature>();

            Options.Add(
                "f=|file=",
                "The file to import",
                v => _file = v.Trim());
        }

        protected override int Run(TextWriter cout)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.LiterateConsole()
                .CreateLogger();

            try
            {
                if (string.IsNullOrWhiteSpace(_file))
                {
                    Log.Fatal("A log file to import must be specified");
                    return 1;
                }

                if (!File.Exists(_file))
                {
                    Log.Fatal("The specified import log file does not exist");
                    return 1;
                }

                string serverUrl = null, apiKey = null;
                if (_serverInformation.IsUrlSpecified)
                {
                    serverUrl = _serverInformation.Url;
                    apiKey = _serverInformation.ApiKey;
                }
                else if (File.Exists(_storagePath.ConfigFilePath))
                {
                    var config = SeqForwarderConfig.Read(_storagePath.ConfigFilePath);
                    if (string.IsNullOrEmpty(config.Output.ServerUrl))
                    {
                        serverUrl = config.Output.ServerUrl;
                        apiKey = _serverInformation.IsApiKeySpecified ? _serverInformation.ApiKey : config.Output.ApiKey;
                    }
                }

                if (serverUrl == null)
                {
                    Log.Fatal("A Seq server URL must be specified or set in the SeqForwarder.json config file");
                    return 1;
                }

                Task.Run(async () => await Run(serverUrl, apiKey, _file, _keyValueProperties.Properties, 256*1024, 1024*1024)).Wait();
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Could not complete import");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        static async Task Run(string server, string apiKey, string file, IDictionary<string, string> additionalTags, ulong bodyLimitBytes, ulong payloadLimitBytes)
        {
            var originalFilename = Path.GetFileName(file);
            Log.Information("Opening JSON log file {OriginalFilename}", originalFilename);

            var importId = Guid.NewGuid();
            var tags = new Dictionary<string, object>
            {
                ["ImportId"] = importId
            };

            if (additionalTags?.Any() ?? false)
            {
                Log.Information("Adding tags {@Tags} to import", additionalTags);

                foreach (var p in additionalTags)
                    tags.Add(p.Key, p.Value);
            }

            var logBuffer = new InMemoryLogBuffer(file, tags);

            var shipper = new HttpImporter(logBuffer, new SeqImportConfig
            {
                ServerUrl = server,
                ApiKey = apiKey,
                EventBodyLimitBytes = bodyLimitBytes,
                RawPayloadLimitBytes = payloadLimitBytes
            });

            var sw = Stopwatch.StartNew();
            Log.Information("Starting import {ImportId}", importId);
            await shipper.Import();
            sw.Stop();
            Log.Information("Import {ImportId} completes in {Elapsed:0.0} ms", importId, sw.Elapsed.TotalMilliseconds);
        }
    }
}
