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
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace Seq.Forwarder.Config
{
    class SeqForwarderOutputConfig
    {
        const string ProtectedDataPrefix = "pd.";

        public string ServerUrl { get; set; }
        public ulong EventBodyLimitBytes { get; set; }
        public ulong RawPayloadLimitBytes { get; set; }

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

                var pmk = EncodedApiKey.Substring(ProtectedDataPrefix.Length);
                var parts = pmk.Split(new[] { '$' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                    throw new InvalidOperationException("API key format is invalid.");

                var bytes = Convert.FromBase64String(parts[0]);
                var salt = Convert.FromBase64String(parts[1]);
                var decoded = ProtectedData.Unprotect(bytes, salt, DataProtectionScope.LocalMachine);
                return Encoding.UTF8.GetString(decoded);
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    EncodedApiKey = null;
                    return;
                }

                var salt = new byte[16];
                using (var cp = new RNGCryptoServiceProvider())
                    cp.GetBytes(salt);

                var bytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(value), salt, DataProtectionScope.LocalMachine);
                EncodedApiKey = $"{ProtectedDataPrefix}{Convert.ToBase64String(bytes)}${Convert.ToBase64String(salt)}";
            }
        }
    }
}
