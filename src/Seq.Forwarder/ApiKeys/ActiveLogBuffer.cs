// Copyright 2017 Datalust Pty Ltd and Contributors
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
using Seq.Forwarder.Shipper;
using Seq.Forwarder.Storage;

namespace Seq.Forwarder.ApiKeys
{
    sealed class ActiveLogBuffer : IDisposable
    {
        public HttpLogShipper Shipper { get; }
        public LogBuffer LogBuffer { get; }

        public ActiveLogBuffer(LogBuffer logBuffer, HttpLogShipper logShipper)
        {
            LogBuffer = logBuffer ?? throw new ArgumentNullException(nameof(logBuffer));
            Shipper = logShipper ?? throw new ArgumentNullException(nameof(logShipper));
        }

        public void Dispose()
        {
            Shipper.Dispose();
            LogBuffer.Dispose();
        }
    }
}