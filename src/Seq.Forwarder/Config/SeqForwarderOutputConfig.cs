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

using Newtonsoft.Json;
using Seq.Forwarder.Util;

namespace Seq.Forwarder.Config
{
    class SeqForwarderOutputConfig
    {
        const string ProtectedDataPrefix = "pd.";

        public string ServerUrl { get; set; } = "http://localhost:5341";
        public int SocketLifetime { get; set; } = 60000 * 3; //this is what we use for lifetime, 1-5 minutes is pretty much the range so if there is a sweet spot for how the log shipper runs, I can adjust the default
        public ulong EventBodyLimitBytes { get; set; } = 256 * 1024;
        public ulong RawPayloadLimitBytes { get; set; } = 10 * 1024 * 1024;
        
        [JsonProperty("apiKey")]
        public string EncodedApiKey { get; set; }

        [JsonIgnore]
        public string ApiKey
        {
            get
            {
                if (string.IsNullOrWhiteSpace(EncodedApiKey))
                    return null;

                if (!EncodedApiKey.StartsWith(ProtectedDataPrefix))
                    return EncodedApiKey;

                return MachineScopeDataProtection.Unprotect(EncodedApiKey.Substring(ProtectedDataPrefix.Length));
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    EncodedApiKey = null;
                    return;
                }

                EncodedApiKey = $"{ProtectedDataPrefix}{MachineScopeDataProtection.Protect(value)}";
            }
        }
    }
}
