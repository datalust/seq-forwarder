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
using Autofac;
using Nancy;
using Seq.Forwarder.Config;
using Seq.Forwarder.Multiplexing;
using Seq.Forwarder.ServiceProcess;
using Seq.Forwarder.Web.Formats;
using Seq.Forwarder.Web.Host;

namespace Seq.Forwarder
{
    class SeqForwarderModule : Module
    {
        readonly string _bufferPath;
        readonly string _listenUri;
        readonly SeqForwarderConfig _config;

        public SeqForwarderModule(string bufferPath, string listenUri, SeqForwarderConfig config)
        {
            _bufferPath = bufferPath ?? throw new ArgumentNullException(nameof(bufferPath));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _listenUri = listenUri ?? throw new ArgumentNullException(nameof(listenUri));
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<JsonNetSerializer>().As<ISerializer>();

            builder.RegisterAssemblyTypes(ThisAssembly)
                .AssignableTo<INancyModule>()
                .As<INancyModule>()
                .AsSelf()
                .PropertiesAutowired();

            builder.Register(c => new ServerService(c.Resolve<NancyBootstrapper>(), c.Resolve<Lazy<ActiveLogBufferMap>>(), _listenUri)).SingleInstance();
            builder.RegisterType<NancyBootstrapper>();

            builder.RegisterType<ActiveLogBufferMap>()
                .WithParameter("bufferPath", _bufferPath)
                .SingleInstance();

            builder.RegisterType<HttpLogShipperFactory>().As<ILogShipperFactory>();
            builder.RegisterInstance(_config.Storage);
            builder.RegisterInstance(_config.Output);
            builder.RegisterType<ServerResponseProxy>().SingleInstance();
        }
    }
}
