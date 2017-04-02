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
using System.Collections.Generic;
using Autofac;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Bootstrappers.Autofac;
using Nancy.Responses;
using Seq.Forwarder.Diagnostics;
using Seq.Forwarder.Web.Formats;
using Serilog;

namespace Seq.Forwarder.Web.Host
{
    public class NancyBootstrapper : AutofacNancyBootstrapper
    {
        readonly ILifetimeScope _container;

        public NancyBootstrapper(ILifetimeScope container)
        {
            _container = container;
        }

        protected override ILifetimeScope GetApplicationContainer()
        {
            return _container;
        }

        protected override void ApplicationStartup(ILifetimeScope container, IPipelines pipelines)
        {
            Log.Debug("Bootstrapping Nancy...");

            base.ApplicationStartup(container, pipelines);

            pipelines.OnError.AddItemToEndOfPipeline((nancyContext, exception) =>
            {
                var bre = exception as BadRequestException;

                if (bre != null)
                {
                    Log.Debug("Bad request for {RequestUrl}: {Message}", nancyContext.Request.Url, exception.Message);
                    PopLogContext(nancyContext);

                    return new JsonResponse(
                        new { Error = exception.Message },
                        new JsonNetSerializer())
                    {
                        StatusCode = bre.StatusCode
                    };
                }

                var token = Guid.NewGuid().ToString("n");
                Log.Error(exception, "Error serving {RequestUrl} (token: {ErrorToken})", nancyContext.Request.Url, token);
                PopLogContext(nancyContext);

                return new JsonResponse(
                    new
                    {
                        Error = "An unhandled error occurred while serving the request (token: " + token + ").",
                    },
                    new JsonNetSerializer())
                    {
                        StatusCode = HttpStatusCode.InternalServerError
                    };
            });

            pipelines.AfterRequest.AddItemToEndOfPipeline(nancyContext =>
            {
                Log.Debug("Responding with {StatusCode}", nancyContext.Response.StatusCode);
                PopLogContext(nancyContext);
            });

            Log.Debug("Nancy bootstrapped.");
            IngestionLog.Log.Debug("The Seq Forwarder is configured and accepting requests");
        }

        protected override void RegisterRequestContainerModules(ILifetimeScope container, IEnumerable<ModuleRegistration> moduleRegistrationTypes)
        {
        }

        protected override INancyModule GetModule(ILifetimeScope container, Type moduleType)
        {
            return (INancyModule)container.Resolve(moduleType);
        }

        static void PopLogContext(NancyContext nancyContext)
        {
            object logContext;
            if (nancyContext.Items.TryGetValue("RequestId", out logContext))
            {
                var pop = logContext as IDisposable;
                pop?.Dispose();
            }
        }
    }
}
