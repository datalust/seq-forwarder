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
using System.Collections.Generic;
using System.Configuration.Install;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.ServiceProcess;
using Nancy.Hosting.Self;
using Seq.Forwarder.Cli.Features;
using Seq.Forwarder.Config;
using Seq.Forwarder.ServiceProcess;
using Seq.Forwarder.Util;
using Serilog.Events;

namespace Seq.Forwarder.Cli.Commands
{
    [Command("install", "Install the Seq Forwarder as a Windows service")]
    class InstallCommand : Command
    {
        readonly StoragePathFeature _storagePath;
        readonly ServiceCredentialsFeature _serviceCredentials;

        bool _setup;

        public InstallCommand()
        {
            _storagePath = Enable<StoragePathFeature>();
            _serviceCredentials = Enable<ServiceCredentialsFeature>();

            Options.Add(
                "setup",
                "Install the service or reconfigure the binary location, then start the service",
                v => _setup = true);
        }

        protected override int Run(TextWriter cout)
        {
            try
            {
                if (!_setup)
                {
                    Install(cout);
                    return 0;
                }

                var exit = Setup(cout);
                if (exit == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    cout.WriteLine("Setup completed successfully.");
                    Console.ResetColor();
                }
                return exit;
            }
            catch (DirectoryNotFoundException dex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                cout.WriteLine("Could not install the service, directory not found: " + dex.Message);
                Console.ResetColor();
                return -1;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                cout.WriteLine("Could not install the service: " + ex.Message);
                Console.ResetColor();
                return -1;
            }
        }

        int Setup(TextWriter cout)
        {
            ServiceController controller;
            try
            {
                cout.WriteLine("Checking the status of the Seq Forwarder service...");

                controller = new ServiceController(SeqForwarderWindowsService.WindowsServiceName);
                cout.WriteLine("Status is {0}", controller.Status);
            }
            catch (InvalidOperationException)
            {
                Install(cout);
                var controller2 = new ServiceController(SeqForwarderWindowsService.WindowsServiceName);
                return Start(controller2, cout);
            }

            cout.WriteLine("Service is installed; checking path info...");
            Reconfigure(controller, cout);

            if (controller.Status != ServiceControllerStatus.Running)
                return Start(controller, cout);

            return 0;
        }

        static void Reconfigure(ServiceController controller, TextWriter cout)
        {
            string path;
            if (!ServiceConfiguration.GetServiceBinaryPath(controller, cout, out path))
                return;

            var current = "\"" + typeof(Program).Assembly.Location + "\"";
            if (path.StartsWith(current))
                return;

            var seqRun = path.IndexOf("seq-forwarder.exe\" run", StringComparison.OrdinalIgnoreCase);
            if (seqRun == -1)
            {
                cout.WriteLine("Current binary path is an unrecognized format.");
                return;
            }

            cout.WriteLine("Existing service binary path is: {0}", path);

            var trimmed = path.Substring(seqRun + "seq-forwarder.exe ".Length);
            var newPath = current + trimmed;
            cout.WriteLine("Updating service binary path configuration to: {0}", newPath);

            var escaped = newPath.Replace("\"", "\\\"");
            var sc = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "sc.exe");
            if (0 != CaptiveProcess.Run(sc, "config \"" + controller.ServiceName + "\" binPath= \"" + escaped + "\"", cout.WriteLine, cout.WriteLine))
            {
                cout.WriteLine("Could not reconfigure service path; ignoring.");
                return;
            }

            cout.WriteLine("Service binary path reconfigured successfully.");
        }

        int Start(ServiceController controller, TextWriter cout)
        {
            controller.Start();

            if (controller.Status != ServiceControllerStatus.Running)
            {
                cout.WriteLine("Waiting up to 60 seconds for the service to start (currently: " + controller.Status + ")...");
                controller.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(60));
            }

            if (controller.Status == ServiceControllerStatus.Running)
            {
                cout.WriteLine("Started.");
                return 0;
            }

            cout.WriteLine("The service hasn't started successfully.");
            return -1;
        }

        [DllImport("shlwapi.dll")]
        private static extern bool PathIsNetworkPath(string pszPath);

        void Install(TextWriter cout)
        {
            cout.WriteLine("Installing service...");

            if (PathIsNetworkPath(_storagePath.StorageRootPath))
                throw new ArgumentException("Seq Forwarder requires a local (or SAN) storage location; network shares are not supported.");

            SeqForwarderConfig config;
            if (File.Exists(_storagePath.ConfigFilePath))
            {
                cout.WriteLine($"Using the configuration found in {_storagePath.ConfigFilePath}...");
                config = SeqForwarderConfig.Read(_storagePath.ConfigFilePath);
            }
            else
            {
                cout.WriteLine($"Creating a new configuration file in {_storagePath.ConfigFilePath}...");
                config = CreateDefaultConfig(_storagePath);
            }

            var args = new List<string>
            {
                "/LogFile=\"\"",
                "/ShowCallStack",
                "/storage=\"" + _storagePath.StorageRootPath + "\"",
                GetType().Assembly.Location
            };

            if (_serviceCredentials.IsUsernameSpecified)
            {
                if (!_serviceCredentials.IsPasswordSpecified)
                    throw new ArgumentException("If a service user account is specified, a password for the account must also be specified.");

                // https://technet.microsoft.com/en-us/library/cc794944(v=ws.10).aspx
                cout.WriteLine($"Ensuring {_serviceCredentials.Username} is granted 'Log on as a Service' rights...");
                AccountRightsHelper.EnsureServiceLogOnRights(_serviceCredentials.Username);

                cout.WriteLine($"Granting {_serviceCredentials.Username} rights to {_storagePath.StorageRootPath}...");
                GiveFullControl(_storagePath.StorageRootPath, _serviceCredentials.Username);

                cout.WriteLine($"Granting {_serviceCredentials.Username} rights to {config.Diagnostics.InternalLogPath}...");
                GiveFullControl(config.Diagnostics.InternalLogPath, _serviceCredentials.Username);

                var listenUri = MakeListenUriReservationPattern();
                cout.WriteLine($"Adding URL reservation at {listenUri} for {_serviceCredentials.Username} (may request UAC elevation)...");
                NetSh.AddUrlAcl(listenUri, _serviceCredentials.Username);

                args.Insert(0, "/username=\"" + _serviceCredentials.Username + "\"");
                args.Insert(0, "/password=\"" + _serviceCredentials.Password + "\"");
            }
            else
            {
                cout.WriteLine($"Granting the NT AUTHORITY\\LocalService account rights to {_storagePath.StorageRootPath}...");
                GiveFullControl(_storagePath.StorageRootPath, "NT AUTHORITY\\LocalService");

                cout.WriteLine($"Granting NT AUTHORITY\\LocalService account rights to {config.Diagnostics.InternalLogPath}...");
                GiveFullControl(config.Diagnostics.InternalLogPath, "NT AUTHORITY\\LocalService");

                var listenUri = MakeListenUriReservationPattern();
                cout.WriteLine($"Adding URL reservation at {listenUri} for the Local Service account (may request UAC elevation)...");
                NetSh.AddUrlAcl(listenUri, "NT AUTHORITY\\LocalService");
            }

            ManagedInstallerClass.InstallHelper(args.ToArray());

            Console.ForegroundColor = ConsoleColor.Green;
            cout.WriteLine("Service installed successfully.");
            Console.ResetColor();
        }

        static string MakeListenUriReservationPattern()
        {
            var listenUri = ServerService.ListenUri.Replace("localhost", "+");
            if (!listenUri.EndsWith("/"))
                listenUri += "/";
            return listenUri;
        }

        static void GiveFullControl(string target, string username)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            if (!Directory.Exists(target))
                Directory.CreateDirectory(target);

            var storageInfo = new DirectoryInfo(target);
            var storageAccessControl = storageInfo.GetAccessControl();
            storageAccessControl.AddAccessRule(new FileSystemAccessRule(username,
                FileSystemRights.FullControl, AccessControlType.Allow));
            storageInfo.SetAccessControl(storageAccessControl);
        }

        public static SeqForwarderConfig CreateDefaultConfig(StoragePathFeature storagePath)
        {
            if (!Directory.Exists(storagePath.StorageRootPath))
                Directory.CreateDirectory(storagePath.StorageRootPath);

            var config = new SeqForwarderConfig
            {
                Output =
                {
                    ServerUrl = "http://localhost:5341",
                    ApiKey = null,
                    EventBodyLimitBytes = 256 * 1024,
                    RawPayloadLimitBytes = 10 * 1024 * 1024
                },
                Diagnostics =
                {
                    InternalLoggingLevel = LogEventLevel.Information,
                    InternalLogPath = GetDefaultInternalLogPath()
                },
                Storage =
                {
                    BufferSizeBytes = 64*1024*1024
                }
            };

            SeqForwarderConfig.Write(storagePath.ConfigFilePath, config);

            return config;
        }

        public static string GetDefaultInternalLogPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Seq\Logs\");
        }
    }
}
