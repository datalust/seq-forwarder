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

using Seq.Forwarder.ServiceProcess;
using Seq.Forwarder.Util;
using System;
using System.IO;

namespace Seq.Forwarder.Cli.Features
{
    class ListenUriFeature : CommandFeature
    {
        string _listenUri;

        public string ListenUri
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_listenUri))
                    return _listenUri;

                return TryQueryInstalledListenUri() ?? GetDefaultListenUri();
            }
        }

        public override void Enable(OptionSet options)
        {
            options.Add("l=|listen=",
                "Set the listen Uri; " + GetDefaultListenUri() + " is used by default.",
                v => _listenUri = v);
        }

        string GetDefaultListenUri()
        {
            return "http://localhost:15341";
        }

        string TryQueryInstalledListenUri()
        {
            if (ServiceConfiguration.GetServiceListenUri(SeqForwarderWindowsService.WindowsServiceName, new StringWriter(), out var listenUri))
                return listenUri;

            return null;
        }
    }
}
