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
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Seq.Forwarder.Administration
{
    class ConfigurationViewModel : INotifyPropertyChanged
    {
        readonly Dispatcher _dispatcher;

        string _page = "Welcome";

        bool _isDone, _hasFailed;
        string _output = "";

        string _storagePath, _serverUrl, _apiKey;

        public ConfigurationViewModel(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;

            if (Environment.CommandLine.Contains("--setup") && SeqForwarderExeUtility.DefaultInstanceIsInstalled())
            {
                Page = "Execute";
                Task.Run(() => CheckInstallation());
            }
            else
            {
                _serverUrl = "";
                _apiKey = "";
                _storagePath = Path.GetFullPath(Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "Seq",
                    "Forwarder"));
            }
        }

        void Install()
        {
            Thread.Sleep(200);

            var exit = 0;

            if (SeqForwarderExeUtility.DefaultInstanceIsInstalled())
            {
                WriteOutput("An existing service instance was detected; removing the service entry...");
                SeqForwarderExeUtility.Run("stop", delegate { }, delegate { });
                exit = SeqForwarderExeUtility.Run("uninstall", WriteOutput, WriteError);
            }

            if (exit == 0)
                exit = SeqForwarderExeUtility.Run($"install --storage=\"{_storagePath}\"", WriteOutput, WriteError);

            if (exit == 0)
                exit = SeqForwarderExeUtility.Run($"config -k output.serverUrl --value=\"{_serverUrl}\"", WriteOutput, WriteError);

            if (exit == 0 && !string.IsNullOrWhiteSpace(_apiKey))
                exit = SeqForwarderExeUtility.Run($"config -k output.apiKey --value=\"{_apiKey}\"", WriteOutput, WriteError);

            if (exit == 0)
                exit = SeqForwarderExeUtility.Run("start", WriteOutput, WriteError);

            _dispatcher.Invoke(() =>
            {
                IsDone = true;
                HasFailed = HasFailed || exit != 0;
            });
        }

        void CheckInstallation()
        {
            Thread.Sleep(200);

            var exit = SeqForwarderExeUtility.Run("install --setup", WriteOutput, WriteError);

            _dispatcher.Invoke(() =>
            {
                IsDone = true;
                HasFailed = HasFailed || exit != 0;
            });
        }

        void WriteOutput(string line)
        {
            var newOutput = _output + line + Environment.NewLine;
            Output = newOutput;
        }

        void WriteError(string line)
        {
            var newOutput = _output + line + Environment.NewLine;
            Output = newOutput;
            _dispatcher.Invoke(() => { HasFailed = true; });
        }

        public bool CanClose(Window window)
        {
            if (_page != "Execute")
                return true;

            if (!_isDone)
                MessageBox.Show(window, "Please wait for configuration to complete before closing the app.");

            return _isDone;
        }

        public void Next(Window window)
        {
            if (Page == "Welcome")
            {
                Page = "Service";
            }
            else
            {
                if (string.IsNullOrWhiteSpace(_serverUrl) ||
                    !_serverUrl.Trim().StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show(window, "Please supply a Seq Server URL to connect to.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(_storagePath) || !Path.IsPathRooted(_storagePath.Trim()))
                {
                    MessageBox.Show(window, "Please supply a fully-qualified storage path.");
                    return;
                }

                Page = "Execute";
                Task.Run(() => Install());
            }
        }

        public bool IsDone
        {
            get { return _isDone; }
            set
            {
                if (value != _isDone)
                {
                    _isDone = value;
                    OnPropertyChanged();
                    OnPropertyChanged("IsRunning");
                }
            }
        }

        public bool HasFailed
        {
            get { return _hasFailed; }
            set
            {
                if (value != _hasFailed)
                {
                    _hasFailed = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsRunning
        {
            get { return !IsDone; }
        }

        public string Output
        {
            get { return _output; }
            set
            {
                if (value != _output)
                {
                    _output = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Page
        {
            get { return _page; }
            set
            {
                if (_page != value)
                {
                    _page = value;
                    OnPropertyChanged();
                }
            }
        }

        public string StoragePath
        {
            get { return _storagePath; }
            set
            {
                if (_storagePath != value)
                {
                    _storagePath = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ServerUrl
        {
            get { return _serverUrl; }
            set
            {
                if (_serverUrl != value)
                {
                    _serverUrl = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ApiKey
        {
            get { return _apiKey; }
            set
            {
                if (_apiKey != value)
                {
                    _apiKey = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
