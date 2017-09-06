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
using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;
using System.Text;
using Seq.Forwarder.ServiceProcess;

namespace Seq.Forwarder.Cli.Installers
{
    [RunInstaller(true)]
    public class ProjectInstaller : Installer
    {
        readonly ServiceProcessInstaller _serviceProcessInstaller;

        public ProjectInstaller()
        {
            _serviceProcessInstaller = new ServiceProcessInstaller
            {
                Account = ServiceAccount.LocalService
            };

            Installers.Add(_serviceProcessInstaller);

            var serviceInstaller = new ServiceInstaller
            {
                ServiceName = SeqForwarderWindowsService.WindowsServiceName,
                DisplayName = "Seq Forwarder",
                Description = "Stores and forwards application log events to Seq",
                StartType = ServiceStartMode.Automatic
            };

            Installers.Add(serviceInstaller);
        }

        protected override void OnBeforeInstall(IDictionary savedState)
        {
            var path = new StringBuilder(Context.Parameters["assemblypath"]);
            if (path[0] != '"')
            {
                path.Insert(0, '"');
                path.Append('"');
            }

            path.AppendFormat(" run --storage={0}", Context.Parameters["storage"]);

            Context.Parameters["assemblypath"] = path.ToString();

            var username = Context.Parameters["username"];
            if (ParameterHasValue(username))
            {
                var password = Context.Parameters["password"];
                if (!ParameterHasValue(password))
                    throw new InvalidOperationException("A password must be supplied if a service username is specified.");

                _serviceProcessInstaller.Account = ServiceAccount.User;
                Context.Parameters["USERNAME"] = _serviceProcessInstaller.Username = username.Trim('"');
                Context.Parameters["PASSWORD"] = _serviceProcessInstaller.Password = password.Trim('"');
            }

            base.OnBeforeInstall(savedState);
        }

        static bool ParameterHasValue(string provided)
        {
            return !string.IsNullOrEmpty(provided) && provided != "\"\"";
        }
    }
}
