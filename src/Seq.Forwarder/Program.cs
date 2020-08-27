using System;
using Autofac;
using Seq.Forwarder.Cli;

namespace Seq.Forwarder
{
    class Program
    {
#if WINDOWS
        public const string BinaryName = "seqfwd.exe";
#endif   
        
        static int Main(string[] args)
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<CommandLineHost>();
            builder.RegisterAssemblyTypes(typeof(Program).Assembly)
                .As<Command>()
                .WithMetadataFrom<CommandAttribute>();

            using var container = builder.Build();
            var clh = container.Resolve<CommandLineHost>();
            return clh.Run(args, Console.Out, Console.Error);
        }
    }
}
