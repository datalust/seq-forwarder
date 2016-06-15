using System;
using System.Linq;
using Nancy;
using Seq.Forwarder.Web.Api;
using Xunit;
using Xunit.Sdk;

namespace Seq.Forwarder.Tests.Web
{
    public class ApiTests
    {
        [Fact]
        public void ApiModulesHaveOnlyLazyDependencies()
        {
            var modules = typeof(ApiRootModule).Assembly.GetTypes()
                        .Where(t => typeof(NancyModule).IsAssignableFrom(t))
                        .Where(t => !t.IsAbstract && t.Namespace != null && t.Namespace.Contains(".Api."));

            foreach (var module in modules)
            {
                var ctor = module.GetConstructors().SingleOrDefault();
                Assert.True(ctor != null, $"Module {module.Name} has no constructor?");

                foreach (var parameter in ctor.GetParameters())
                {
                    if (!parameter.ParameterType.IsGenericType ||
                        parameter.ParameterType.GetGenericTypeDefinition() != typeof(Lazy<>))
                    {
                        throw new XunitException($"Constructor parameter {parameter.Name} of module {module.Name} is not lazy; this will cause Nancy to resolve-the-world at startup.");
                    }
                }
            }
        }
    }
}
