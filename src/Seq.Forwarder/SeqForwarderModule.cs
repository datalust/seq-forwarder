// Copyright Datalust Pty Ltd
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
using System.Net.Http;
using Autofac;
using Seq.Forwarder.Config;
using Seq.Forwarder.Cryptography;
using Seq.Forwarder.Multiplexing;
using Seq.Forwarder.Web.Host;

namespace Seq.Forwarder
{
    class SeqForwarderModule : Module
    {
        readonly string _bufferPath;
        readonly SeqForwarderConfig _config;

        public SeqForwarderModule(string bufferPath, SeqForwarderConfig config)
        {
            _bufferPath = bufferPath ?? throw new ArgumentNullException(nameof(bufferPath));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ServerService>().SingleInstance();
            builder.RegisterType<ActiveLogBufferMap>()
                .WithParameter("bufferPath", _bufferPath)
                .SingleInstance();

            builder.RegisterType<HttpLogShipperFactory>().As<ILogShipperFactory>();
            builder.RegisterInstance(_config.Storage);
            builder.RegisterInstance(_config.Output);
            builder.RegisterType<ServerResponseProxy>().SingleInstance();

            builder.Register(c =>
            {
                var baseUri = c.Resolve<SeqForwarderOutputConfig>().ServerUrl;
                if (string.IsNullOrWhiteSpace(baseUri))
                    throw new ArgumentException("The destination Seq server URL must be configured in SeqForwarder.json.");

                if (!baseUri.EndsWith("/"))
                    baseUri += "/";

                return new HttpClient { BaseAddress = new Uri(baseUri) };
            }).SingleInstance();

            builder.RegisterInstance(StringDataProtector.CreatePlatformDefault());
        }
    }
}
