// Copyright 2017 Datalust Pty Ltd and Contributors
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
using System.IO;
using Seq.Forwarder.Config;
using Seq.Forwarder.Shipper;
using Seq.Forwarder.Storage;
using Seq.Forwarder.Util;
using Serilog;

namespace Seq.Forwarder.ApiKeys
{
    class ActiveLogBufferMap : IDisposable
    {
        readonly ulong _bufferSizeBytes;
        readonly ServerResponseProxy _serverResponseProxy;
        readonly SeqForwarderOutputConfig _outputConfig;
        readonly string _bufferPath;
        readonly ILogger _log = Log.ForContext<ActiveLogBufferMap>();

        readonly object _sync = new object();
        ActiveLogBuffer _noApiKeyLogBuffer;
        readonly Dictionary<string, ActiveLogBuffer> _logBuffersByApiKey = new Dictionary<string, ActiveLogBuffer>();

        public ActiveLogBufferMap(string bufferPath, ulong bufferSizeBytes, SeqForwarderOutputConfig outputConfig, ServerResponseProxy serverResponseProxy)
        {
            _bufferSizeBytes = bufferSizeBytes;
            _serverResponseProxy = serverResponseProxy ?? throw new ArgumentNullException(nameof(serverResponseProxy));
            _outputConfig = outputConfig ?? throw new ArgumentNullException(nameof(outputConfig));
            _bufferPath = bufferPath ?? throw new ArgumentNullException(nameof(bufferPath));
        }

        public void Load()
        {
            // At startup, we look for buffers and either delete them if they're empty, or load them
            // up if they're not. This garbage collection at start-up is a simplification,
            // we might try cleaning up in the background if the gains are worthwhile, although more synchronization
            // would be required.

            lock (_sync)
            {
                var defaultDataFilePath = Path.Combine(_bufferPath, "data.mdb");
                if (File.Exists(defaultDataFilePath))
                {
                    _log.Information("Loading the default log buffer in {Path}", defaultDataFilePath);
                    var buffer = new LogBuffer(_bufferPath, _bufferSizeBytes);
                    if (buffer.Peek(0).Length == 0)
                    {
                        _log.Information("The default buffer is empty and will be removed until more data is received");
                        buffer.Dispose();
                        File.Delete(defaultDataFilePath);
                        var lockFilePath = Path.Combine(_bufferPath, "lock.mdb");
                        if (File.Exists(lockFilePath))
                            File.Delete(lockFilePath);
                    }
                    else
                    {
                        _noApiKeyLogBuffer = new ActiveLogBuffer(buffer, new HttpLogShipper(buffer, _outputConfig.DefaultApiKey, _outputConfig, _serverResponseProxy));
                    }
                }

                foreach (var subfolder in Directory.GetDirectories(_bufferPath))
                {
                    var encodedApiKeyFilePath = Path.Combine(subfolder, ".apikey");
                    if (!File.Exists(encodedApiKeyFilePath))
                    {
                        _log.Information("Folder {Path} does not appear to be a log buffer; skipping", subfolder);
                        continue;
                    }

                    var buffer = new LogBuffer(subfolder, _bufferSizeBytes);
                    if (buffer.Peek(0).Length == 0)
                    {
                        _log.Information("API key-specific buffer in {Path} is empty and will be removed until more data is received", subfolder);
                        buffer.Dispose();
                        Directory.Delete(subfolder, true);
                    }
                    else
                    {
                        var apiKey = MachineScopeDataProtection.Unprotect(File.ReadAllText(encodedApiKeyFilePath));
                        var activeBuffer = new ActiveLogBuffer(buffer, new HttpLogShipper(buffer, apiKey, _outputConfig, _serverResponseProxy));
                        _logBuffersByApiKey.Add(apiKey, activeBuffer);
                    }
                }
            }
        }

        public void Start()
        {
            lock (_sync)
            {
                _noApiKeyLogBuffer?.Shipper.Start();
                foreach (var buffer in _logBuffersByApiKey.Values)
                {
                    buffer.Shipper.Start();
                }
            }
        }

        public void Dispose()
        {
            lock (_sync)
            {
                _noApiKeyLogBuffer?.Dispose();
                foreach (var buffer in _logBuffersByApiKey.Values)
                {
                    buffer.Dispose();
                }
            }
        }

        public void Enumerate(Action<ulong, byte[]> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            lock (_sync)
            {
                _noApiKeyLogBuffer?.LogBuffer.Enumerate(action);
                foreach (var buffer in _logBuffersByApiKey.Values)
                {
                    buffer.LogBuffer.Enumerate(action);
                }
            }
        }

        public static void Truncate(string bufferPath)
        {
            throw new NotImplementedException();
        }
    }
}
