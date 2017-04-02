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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Autofac.Features.Metadata;

namespace Seq.Forwarder.Cli.Commands
{
    [Command("help", "Show information about available commands")]
    public class HelpCommand : Command
    {
        readonly List<Meta<Lazy<Command>, CommandMetadata>> _availableCommands;

        public HelpCommand(IEnumerable<Meta<Lazy<Command>, CommandMetadata>> availableCommands)
        {
            _availableCommands = availableCommands.OrderBy(c => c.Metadata.Name).ToList();
        }

        protected override int Run(string[] unrecognised, TextWriter cout, TextWriter cerr)
        {
            var ea = Assembly.GetEntryAssembly();
            var name = ea.GetName().Name;

            if (unrecognised.Length > 0)
            {
                var target = unrecognised[0].ToLowerInvariant();
                var cmd = _availableCommands.SingleOrDefault(c => c.Metadata.Name == target);
                if (cmd != null)
                {
                    var argHelp = cmd.Value.Value.HasArgs ? " [<args>]" : "";
                    cout.WriteLine(name + " " + cmd.Metadata.Name + argHelp);
                    cout.WriteLine();
                    cout.WriteLine(cmd.Metadata.HelpText);
                    cout.WriteLine();

                    cmd.Value.Value.PrintUsage(cout);
                    return 0;
                }

                base.Run(unrecognised, cout, cerr);
            }

            cout.WriteLine($"Usage: {name} <command> [<args>]");
            cout.WriteLine();
            cout.WriteLine("Available commands are:");
            
            foreach (var availableCommand in _availableCommands)
            {
                Printing.Define(
                    "  " + availableCommand.Metadata.Name,
                    availableCommand.Metadata.HelpText,
                    13,
                    cout);
            }

            cout.WriteLine();
            cout.WriteLine($"Type '{name} help <command>' for detailed help.");

            return 0;
        }
    }
}