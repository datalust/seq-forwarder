// Copyright Datalust Pty Ltd
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

#if WINDOWS

using System;
using System.IO;
using System.Linq;
using Seq.Forwarder.Util;

namespace Seq.Forwarder.Cli.Commands
{
    [Command("bind-ssl", "Bind an installed SSL certificate to an HTTPS port served by Seq Forwarder")]
    class BindSslCommand : Command
    {
        int _port = 443;
        string? _thumbprint;
        string? _hostname;

        public BindSslCommand()        
        {
            Options.Add("port=", "The port on which the Seq Forwarder is listening (default is 443)", v => _port = int.Parse(v));
            Options.Add("thumbprint=", "The thumbprint of the SSL certificate to bind; this can be found with the `Manage computer certificates` program", v => _thumbprint = new string(v.Where(char.IsLetterOrDigit).ToArray()));
            Options.Add("hostname=", "If SNI is used, the specific host name to bind to (default is to bind to all hostnames via the IP address)", v => _hostname = v);
        }

        protected override int Run(TextWriter cout)
        {
            try
            {
                if (string.IsNullOrEmpty(_thumbprint)) throw new ArgumentException("A certificate thumbprint is required.");

                var scopeDesc = string.IsNullOrWhiteSpace(_hostname) ? "*" : _hostname.Trim();
                cout.WriteLine("Binding the Seq server on {0}:{1} to SSL certificate {2}", scopeDesc, _port, _thumbprint);

                var scope = string.IsNullOrWhiteSpace(_hostname)
                    ? $"ipport=0.0.0.0:{_port}"
                    : $"hostnameport={_hostname.Trim()}:{_port}";

                CaptiveProcess.Run("netsh", $"http delete sslcert {scope}");

                var addResult = CaptiveProcess.Run("netsh",
                    $"http add sslcert {scope} appid={{25871FA4-D897-49D3-9B35-FFA8E99738F7}} certhash={_thumbprint} certstorename=my",
                    Console.WriteLine,
                    e =>
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine(e);
                        Console.ResetColor();
                    });

                if (addResult == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Certificate bound successfully.");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Failed to bind the certificate: `netsh` returned exit code {0}.", addResult);
                }

                Console.ResetColor();
                return addResult;
            }
            catch (Exception ex)
            {
                cout.WriteLine("Could not bind the certificate: " + ex.Message);
                return -1;
            }
        }
    }
}

#endif
