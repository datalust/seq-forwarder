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
using Nancy.Hosting.Self;
using Seq.Forwarder.Multiplexing;
using Seq.Forwarder.Web.Host;
using Serilog;

namespace Seq.Forwarder.ServiceProcess
{
    class ServerService
    {
        readonly Lazy<ActiveLogBufferMap> _logBufferMap;
        readonly NancyHost _nancyHost;
        string _listenUri;

        public ServerService(NancyBootstrapper bootstrapper, Lazy<ActiveLogBufferMap> logBufferMap, string listenUri)
        {
            _logBufferMap = logBufferMap;
            _listenUri = listenUri;
            var hc = new HostConfiguration
            {
                UrlReservations = { CreateAutomatically = Environment.UserInteractive }
            };
            _nancyHost = new NancyHost(bootstrapper, hc, new Uri(_listenUri));
        }

        public void Start()
        {
            try
            {
                Log.Debug("Starting Nancy HTTP server...");

                _nancyHost.Start();

                Log.Information("Seq Forwarder listening on {ListenUri}", _listenUri);

                _logBufferMap.Value.Load();
                _logBufferMap.Value.Start();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Error running the server application");
                throw;
            }
        }

        public void Stop()
        {
            Log.Debug("Seq Forwarder stopping");

            _nancyHost.Stop();
            _logBufferMap.Value.Stop();

            Log.Information("Seq Forwarder stopped cleanly");
        }
    }
}
