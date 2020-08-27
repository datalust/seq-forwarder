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
using Seq.Forwarder.Cli.Features;
using Seq.Forwarder.Config;
using Serilog;

namespace Seq.Forwarder.Cli.Commands
{
    [Command("config", "View and set fields in the SeqForwarder.json file; run with no arguments to list all fields")]
    class ConfigCommand : Command
    {
        readonly StoragePathFeature _storagePath;

        string? _key, _value;
        bool _clear;

        public ConfigCommand()
        {
            _storagePath = Enable<StoragePathFeature>();

            Options.Add("k|key=", "The field, for example \"output.serverUrl\"", k => _key = k);
            Options.Add("v|value=", "The field value; if not specified, the command will print the current value", v => _value = v);
            Options.Add("c|clear", "Clear the field", _ => _clear = true);
        }

        protected override int Run(TextWriter cout)
        {
            try
            {
                var config = SeqForwarderConfig.Read(_storagePath.ConfigFilePath);

                if (_key != null)
                {
                    if (_clear)
                    {
                        Clear(config, _key);
                        SeqForwarderConfig.Write(_storagePath.ConfigFilePath, config);
                    }
                    else if (_value != null)
                    {
                        Set(config, _key, _value);
                        SeqForwarderConfig.Write(_storagePath.ConfigFilePath, config);
                    }
                    else
                    {
                        Print(cout, config, _key);
                    }
                }
                else
                {
                    List(cout, config);
                }

                return 0;
            }
            catch (Exception ex)
            {
                var logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

                logger.Fatal(ex, "Could not update config");
                return 1;
            }
        }

        static void Print(TextWriter cout, SeqForwarderConfig config, string key)
        {
            if (cout == null) throw new ArgumentNullException(nameof(cout));
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (key == null) throw new ArgumentNullException(nameof(key));

            var (foundKey, value) = ReadPairs(config).SingleOrDefault(p => p.Key == key);
            if (foundKey == null)
                throw new ArgumentException($"Option {key} not found");
            cout.WriteLine(value);
        }

        static void Set(SeqForwarderConfig config, string key, string? value)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (key == null) throw new ArgumentNullException(nameof(key));

            var steps = key.Split('.');
            if (steps.Length != 2)
                throw new ArgumentException("The format of the key is incorrect; run the command without any arguments to view all keys.");

            var first = config.GetType().GetTypeInfo().DeclaredProperties
                .Where(p => p.GetMethod != null && p.GetMethod.IsPublic && !p.GetMethod.IsStatic)
                .SingleOrDefault(p => Camelize(p.Name) == steps[0]);

            if (first == null)
                throw new ArgumentException("The key could not be found; run the command without any arguments to view all keys.");

            var v = first.GetValue(config);
            if (v == null) throw new ArgumentException("Config is invalid; first property path step is null.");

            var second = v.GetType().GetTypeInfo().DeclaredProperties
                .Where(p => p.GetMethod != null && p.GetMethod.IsPublic && p.SetMethod != null && p.SetMethod.IsPublic && !p.GetMethod.IsStatic)
                .SingleOrDefault(p => Camelize(p.Name) == steps[1]);

            if (second == null)
                throw new ArgumentException("The key could not be found; run the command without any arguments to view all keys.");

            var targetValue = Convert.ChangeType(value, second.PropertyType);
            second.SetValue(v, targetValue);
        }

        static void Clear(SeqForwarderConfig config, string key)
        {
            Set(config, key, null);
        }

        static void List(TextWriter cout, SeqForwarderConfig config)
        {
            foreach (var pr in ReadPairs(config))
            {
                cout.WriteLine($"{pr.Key}:");
                cout.WriteLine($"  {pr.Value}");
            }
        }

        static IEnumerable<KeyValuePair<string, object?>> ReadPairs(object config)
        {
            foreach (var first in config.GetType().GetTypeInfo().DeclaredProperties
                .Where(p => p.GetMethod != null && p.GetMethod.IsPublic && !p.GetMethod.IsStatic)
                .OrderBy(p => p.Name))
            {
                var step1 = Camelize(first.Name) + ".";
                var v = first.GetValue(config);
                if (v == null) throw new ArgumentException("Config is invalid; first property path step is null.");

                foreach (var second in v.GetType().GetTypeInfo().DeclaredProperties
                    .Where(p => p.GetMethod != null && p.GetMethod.IsPublic && p.SetMethod != null &&
                                p.SetMethod.IsPublic && !p.GetMethod.IsStatic && !p.Name.StartsWith("Encoded"))
                    .OrderBy(p => p.Name))
                {
                    var name = step1 + Camelize(second.Name);
                    var v2 = second.GetValue(v);
                    yield return new KeyValuePair<string, object?>(name, v2);
                }
            }
        }

        static string Camelize(string s)
        {
            if (s.Length < 2)
                throw new NotImplementedException("No camel-case support for short names");
            return char.ToLowerInvariant(s[0]) + s.Substring(1);
        }
    }
}
