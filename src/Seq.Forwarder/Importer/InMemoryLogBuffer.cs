// Copyright 2016 Datalust Pty Ltd and contributors
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
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Seq.Forwarder.Storage;
using Seq.Forwarder.Util;
using Serilog;

namespace Seq.Forwarder.Importer
{
    // A rewrite that's both lazy and more efficient is needed here.
    class InMemoryLogBuffer
    {
        // Doing our best here to create a totally "neutral" serializer; may need some more work
        // to avoid special-casing .NET types in any circumstances.
        readonly JsonSerializer _serializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            DateParseHandling = DateParseHandling.None,
            Binder = new NonBindingSerializationBinder(),
            TypeNameHandling = TypeNameHandling.None
        });

        readonly SortedDictionary<ulong, byte[]> _entries = new SortedDictionary<ulong, byte[]>();
        ulong _nextId = 1;

        public InMemoryLogBuffer(string file, Dictionary<string, object> tags)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            if (tags == null) throw new ArgumentNullException(nameof(tags));

            var line = 0;
            using (var r = File.OpenText(file))
            {
                var encoding = new UTF8Encoding(false);

                for (var l = r.ReadLine(); l != null; l = r.ReadLine())
                {
                    line++;

                    if (l.Length == 0) continue;

                    try
                    {
                        dynamic json = _serializer.Deserialize<dynamic>(new JsonTextReader(new StringReader(l)));
                        if (json.Properties == null)
                        {
                            json.Properties = JObject.FromObject(tags);
                        }
                        else
                        {
                            foreach (var tag in tags)
                            {
                                json.Properties[tag.Key] = JToken.FromObject(tag.Value);
                            }
                        }

                        var bytes = new MemoryStream();
                        
                        using (var writer = new JsonTextWriter(new StreamWriter(bytes, encoding, 1024, leaveOpen: true)))
                            _serializer.Serialize(writer, json);

                        Enqueue(bytes.ToArray());
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Line {Line} is not valid JSON; skipping", line);
                    }
                }
            }
        }

        public void Enqueue(byte[] value)
        {
            _entries.Add(_nextId++, value);
        }

        public LogBufferEntry[] Peek(int maxValueBytesHint)
        {
            var entries = new List<LogBufferEntry>();

            var entriesBytes = 0;

            foreach (var current in _entries)
            {
                var entry = new LogBufferEntry
                {
                    Key = current.Key,
                    Value = current.Value
                };

                entriesBytes += entry.Value.Length;
                if (entries.Count != 0 && entriesBytes > maxValueBytesHint)
                    break;

                entries.Add(entry);
            }

            return entries.ToArray();
        }

        public void Dequeue(ulong toKey)
        {
            while (_entries.Count > 0)
            {
                var current = _entries.First();
                if (current.Key > toKey)
                    break;

                _entries.Remove(current.Key);
            }
        }
    }
}
