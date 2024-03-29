﻿// Copyright 2017 Datalust Pty Ltd and Contributors
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
using System.Security.Cryptography;
using System.Text;

namespace Seq.Forwarder.Cryptography
{
    class DpapiMachineScopeDataProtect : IStringDataProtector
    {
        public string Unprotect(string @protected)
        {
            var parts = @protected.Split(new[] { '$' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
                throw new InvalidOperationException("Encoded data format is invalid.");

            var bytes = Convert.FromBase64String(parts[0]);
            var salt = Convert.FromBase64String(parts[1]);
            var decoded = ProtectedData.Unprotect(bytes, salt, DataProtectionScope.LocalMachine);
            return Encoding.UTF8.GetString(decoded);
        }

        public string Protect(string value)
        {
            var salt = RandomNumberGenerator.GetBytes(16);
            var bytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(value), salt, DataProtectionScope.LocalMachine);
            return $"{Convert.ToBase64String(bytes)}${Convert.ToBase64String(salt)}";
        }
    }
}

#endif
