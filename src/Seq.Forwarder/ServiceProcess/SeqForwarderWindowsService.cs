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

using System;
using System.ServiceProcess;

namespace Seq.Forwarder.ServiceProcess
{
    class SeqForwarderWindowsService : ServiceBase
    {
        readonly ServerService _serverService;
        readonly IDisposable _disposeOnStop;

        public static string WindowsServiceName { get; } = "Seq Forwarder";

        public SeqForwarderWindowsService(ServerService serverService, IDisposable disposeOnStop)
        {
            _serverService = serverService;
            _disposeOnStop = disposeOnStop;

            ServiceName = WindowsServiceName;
        }

        protected override void OnStart(string[] args)
        {
            _serverService.Start();
        }

        protected override void OnStop()
        {
            _serverService.Stop();
            _disposeOnStop.Dispose();
        }
    }
}