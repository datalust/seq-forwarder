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
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Seq.Forwarder.Util;
using Serilog;

namespace Seq.Forwarder.Importer
{
    static class Filters
    {
        // Doing our best here to create a totally "neutral" serializer; may need some more work
        // to avoid special-casing .NET types in any circumstances.
        static readonly JsonSerializer Serializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            DateParseHandling = DateParseHandling.None,
            Binder = new NonBindingSerializationBinder(),
            TypeNameHandling = TypeNameHandling.None
        });

        public static IEnumerable<Line<string>> ReadLines(string file, ILogger log)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            if (log == null) throw new ArgumentNullException(nameof(log));

            var line = 0;
            using (var r = File.OpenText(file))
            {
                for (var l = r.ReadLine(); l != null; l = r.ReadLine())
                {
                    line++;

                    if (l.Length == 0)
                    {
                        log.Information("Line {Line} is blank; skipping", line);
                        continue;
                    }

                    yield return new Line<string>(line, l);
                }
            }
        }

        public static IEnumerable<Line<JObject>> ParseJson(this IEnumerable<Line<string>> documents, ILogger log)
        {
            if (documents == null) throw new ArgumentNullException(nameof(documents));
            if (log == null) throw new ArgumentNullException(nameof(log));

            foreach (var document in documents)
            {
                JObject json;
                try
                {
                    json = Serializer.Deserialize<JObject>(new JsonTextReader(new StringReader(document.Value)));
                }
                catch (Exception ex)
                {
                    log.Error("Line {Line} is not valid JSON, skipping: {ParseError}", document.Number, ex.Message);
                    continue;
                }

                yield return document.MappedTo(json);
            }
        }

        public static IEnumerable<Line<JObject>> AddProperties(this IEnumerable<Line<JObject>> objects,
            Dictionary<string, object> properties)
        {
            if (objects == null) throw new ArgumentNullException(nameof(objects));
            if (properties == null) throw new ArgumentNullException(nameof(properties));

            foreach (var obj in objects)
            {
                var copy = new JObject(obj.Value);

                var json = (dynamic) copy;
                if (json.Properties == null)
                {
                    json.Properties = JObject.FromObject(properties);
                }

                else
                {
                    foreach (var tag in properties)
                    {
                        json.Properties[tag.Key] = JToken.FromObject(tag.Value);
                    }
                }

                yield return obj.MappedTo(copy);
            }
        }

        public static IEnumerable<Line<byte[]>> ToUtf8(this IEnumerable<Line<JObject>> objects)
        {
            var encoding = new UTF8Encoding(false);
            foreach (var obj in objects)
            {
                var bytes = new MemoryStream();

                using (var writer = new JsonTextWriter(new StreamWriter(bytes, encoding, 1024, leaveOpen: true)))
                    Serializer.Serialize(writer, obj.Value);

                yield return obj.MappedTo(bytes.ToArray());
            }
        }

        public static IEnumerable<T> Unwrap<T>(this IEnumerable<Line<T>> lines)
        {
            foreach (var line in lines)
            {
                yield return line.Value;
            }
        }
    }
}