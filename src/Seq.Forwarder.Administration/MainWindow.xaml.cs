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

using System.Windows;

namespace Seq.Forwarder.Administration
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        readonly ConfigurationViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = _viewModel = new ConfigurationViewModel(Dispatcher);
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_viewModel.CanClose(this))
                e.Cancel = true;
        }

        private void OnClosing(object sender, RoutedEventArgs e)
        {
            if (_viewModel.CanClose(this))
                Close();
        }

        private void OnNext(object sender, RoutedEventArgs e)
        {
            _viewModel.Next(this);
        }
    }
}
