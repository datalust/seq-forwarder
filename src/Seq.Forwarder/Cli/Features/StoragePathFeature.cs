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

namespace Seq.Forwarder.Cli.Features
{
    class StoragePathFeature : CommandFeature
    {
        string? _storageRoot;

        public string StorageRootPath
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_storageRoot))
                    return _storageRoot;

                return TryQueryInstalledStorageRoot() ?? GetDefaultStorageRoot();
            }
        }
        
        public string ConfigFilePath => Path.Combine(StorageRootPath, "SeqForwarder.json");

        public string BufferPath => Path.Combine(StorageRootPath, "Buffer");

        public override void Enable(OptionSet options)
        {
            options.Add("s=|storage=",
                "Set the folder where data will be stored; " +
                "" + GetDefaultStorageRoot() + " is used by default.",
                v => _storageRoot = Path.GetFullPath(v));
        }

        static string GetDefaultStorageRoot()
        {
            return Path.GetFullPath(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                @"Seq",
                "Forwarder"));
        }

        static string? TryQueryInstalledStorageRoot()
        {
#if WINDOWS
            if (Seq.Forwarder.Util.ServiceConfiguration.GetServiceStoragePath(
                Seq.Forwarder.ServiceProcess.SeqForwarderWindowsService.WindowsServiceName, out var storage))
                return storage;
#endif
            
            return null;
        }
    }
}
