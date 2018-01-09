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

using Autofac;
using Seq.Forwarder.Cli.Features;
using Seq.Forwarder.Config;
using Seq.Forwarder.ServiceProcess;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using System;
using System.IO;
using System.Net;
using System.ServiceProcess;

namespace Seq.Forwarder.Cli.Commands
{
    [Command("run", "Run the server interactively")]
    class RunCommand : Command
    {
        readonly StoragePathFeature _storagePath;
        readonly ListenUriFeature _listenUri;

        bool _nologo;

        public RunCommand()
        {
            Options.Add("nologo", v => _nologo = true);
            _storagePath = Enable<StoragePathFeature>();
            _listenUri = Enable<ListenUriFeature>();
        }

        protected override int Run(TextWriter cout)
        {
            if (Environment.UserInteractive)
            {
                if (!_nologo)
                {
                    WriteBanner();
                    cout.WriteLine();
                }

                cout.WriteLine("Running as server; press Ctrl+C to exit.");
                cout.WriteLine();
            }

            SeqForwarderConfig config;

            try
            {
                config = LoadOrCreateConfig();
            }
            catch (Exception ex)
            {
                var logger = CreateLogger(
                    LogEventLevel.Information,
                    SeqForwarderDiagnosticConfig.GetDefaultInternalLogPath());

                logger.Fatal(ex, "Failed to load configuration from {ConfigFilePath}", _storagePath.ConfigFilePath);
                (logger as IDisposable)?.Dispose();
                return 1;
            }

            Log.Logger = CreateLogger(config.Diagnostics.InternalLoggingLevel, config.Diagnostics.InternalLogPath);

            Uri listenUri = new Uri(_listenUri.ListenUri ?? config.Api.ListenUri);
            ServicePoint servicePoint = ServicePointManager.FindServicePoint(listenUri);
            servicePoint.ConnectionLeaseTimeout = config.Output.SocketLifetime;

            var builder = new ContainerBuilder();
            builder.RegisterModule(new SeqForwarderModule(_storagePath.BufferPath, listenUri.AbsoluteUri, config));

            var container = builder.Build();
            var exit = Environment.UserInteractive
                ? RunInteractive(container, cout)
                : RunService(container);

            Log.CloseAndFlush();
            return exit;
        }

        ILogger CreateLogger(LogEventLevel internalLoggingLevel, string internalLogPath)
        {
            var loggerConfiguration = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .MinimumLevel.Is(internalLoggingLevel)
                .WriteTo.RollingFile(
                    new RenderedCompactJsonFormatter(),
                    GetRollingLogFilePathFormat(internalLogPath),
                    fileSizeLimitBytes: 1024 * 1024);

            if (Environment.UserInteractive)
                loggerConfiguration.WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information);

            return loggerConfiguration.CreateLogger();
        }

        // Re-create just in case it was inadvertently deleted.
        SeqForwarderConfig LoadOrCreateConfig()
        {
            if (File.Exists(_storagePath.ConfigFilePath))
            {
                return SeqForwarderConfig.Read(_storagePath.ConfigFilePath);
            }

            return InstallCommand.CreateDefaultConfig(_storagePath);
        }

        string GetRollingLogFilePathFormat(string internalLogPath)
        {
            if (internalLogPath == null) throw new ArgumentNullException(nameof(internalLogPath));

            return Path.Combine(internalLogPath, "seq-forwarder-{Date}.log");
        }

        int RunService(IContainer container)
        {
            try
            {
                ServiceBase.Run(new ServiceBase[] {
                    new SeqForwarderWindowsService(container.Resolve<ServerService>(),
                        container)
                });
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Failed to construct service");
                throw;
            }
        }

        int RunInteractive(IContainer container, TextWriter cout)
        {
            var service = container.Resolve<ServerService>();

            try
            {
                service.Start();

                Console.TreatControlCAsInput = true;
                var k = Console.ReadKey(true);
                while (k.Key != ConsoleKey.C || !k.Modifiers.HasFlag(ConsoleModifiers.Control))
                    k = Console.ReadKey(true);

                cout.WriteLine("Ctrl+C pressed; stopping...");
                Console.TreatControlCAsInput = false;

                service.Stop();

                return 0;
            }
            catch
            {
                return -1;
            }
            finally
            {
                container.Dispose();
            }
        }

        static void WriteBanner()
        {
            Write("─", ConsoleColor.DarkGray, 47);
            Console.WriteLine();
            Write(" Seq Forwarder", ConsoleColor.White);
            Write(" ──", ConsoleColor.DarkGray);
            Write(" © 2017 Datalust Pty Ltd", ConsoleColor.Gray);
            Console.WriteLine();
            Write("─", ConsoleColor.DarkGray, 47);
            Console.WriteLine();
        }

        static void Write(string s, ConsoleColor color, int repeats = 1)
        {
            Console.ForegroundColor = color;
            for (var i = 0; i < repeats; ++i)
                Console.Write(s);
            Console.ResetColor();
        }
    }
}
