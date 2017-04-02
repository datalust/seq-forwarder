// Copyright 2016-2017 Datalust Pty Ltd and contributors
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
using System.Linq;
using Seq.Forwarder.Storage;

namespace Seq.Forwarder.Importer
{
    class BufferedLogReader : IDisposable
    {
        IEnumerator<byte[]> _enumerator;
         
        readonly SortedDictionary<ulong, byte[]> _entries = new SortedDictionary<ulong, byte[]>();
        ulong _nextId = 1;

        public BufferedLogReader(IEnumerable<byte[]> entries)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            _enumerator = entries.GetEnumerator();
        }

        public LogBufferEntry[] Peek(int maxValueBytesHint)
        {
            var result = new List<LogBufferEntry>();

            var entriesBytes = 0;
            var done = false;

            foreach (var current in _entries)
            {
                var entry = new LogBufferEntry
                {
                    Key = current.Key,
                    Value = current.Value
                };

                entriesBytes += entry.Value.Length;
                if (result.Count != 0 && entriesBytes > maxValueBytesHint)
                {
                    done = true;
                    break;
                }

                result.Add(entry);
            }

            if (!done && _enumerator != null)
            {
                while (_enumerator.MoveNext())
                {
                    var entry = new LogBufferEntry
                    {
                        Key = _nextId++,
                        Value = _enumerator.Current
                    };

                    _entries.Add(entry.Key, entry.Value);

                    entriesBytes += entry.Value.Length;
                    if (result.Count != 0 && entriesBytes > maxValueBytesHint)
                    {
                        done = true;
                        break;
                    }

                    result.Add(entry);
                }

                if (!done)
                {
                    _enumerator.Dispose();
                    _enumerator = null;
                }
            }

            return result.ToArray();
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

        public void Dispose()
        {
            _enumerator?.Dispose();
            _enumerator = null;
        }
    }
}
