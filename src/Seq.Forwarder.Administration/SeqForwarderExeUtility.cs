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
using System.IO;
using System.Linq;
using Seq.Forwarder.Util;

namespace Seq.Forwarder.Administration
{
    static class SeqForwarderExeUtility
    {
        public static int Run(string args, Action<string> writeOutput, Action<string> writeError)
        {
            var thisDir = Path.GetDirectoryName(Path.GetFullPath(typeof(SeqForwarderExeUtility).Assembly.Location)) ?? ".";

            var seqExe = Path.Combine(thisDir, "seq-forwarder.exe");
            if (!File.Exists(seqExe))
                seqExe = Path.Combine(thisDir, @"..\..\..\..\Seq.Forwarder\bin\x64\Debug\net4.5.2\seq-forwarder.exe");

            return CaptiveProcess.Run(seqExe, args, writeOutput, writeError);
        }

        public static bool DefaultInstanceIsInstalled()
        {
            var lines = new StringWriter();
            if (Run("status", lines.WriteLine, s => { }) != 0)
                return false;

            return lines.ToString().Split(new [] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .Any(l => l.Contains("is installed"));
        }
    }
}
