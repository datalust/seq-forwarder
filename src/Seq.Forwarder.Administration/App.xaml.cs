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
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;

namespace Seq.Forwarder.Administration
{
    public partial class App
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (!IsUserAdministrator() &&
                !Environment.CommandLine.Contains("--elevated"))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = typeof(App).Assembly.Location,
                    Arguments = "--elevated" + (Environment.CommandLine.Contains("--setup") ? " --setup" : ""),
                    Verb = "runas"
                });

                Environment.Exit(0);
            }
        }

        public bool IsUserAdministrator()
        {
            try
            {
                var user = WindowsIdentity.GetCurrent();
                if (user == null)
                    return false;

                var principal = new WindowsPrincipal(user);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }
    }
}
