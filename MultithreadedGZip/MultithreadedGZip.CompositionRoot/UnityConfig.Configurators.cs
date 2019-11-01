using MultithreadedGZip.BLL.Configurators;
using MultithreadedGZip.BLL.Interfaces.Configurators;
using MultithreadedGZip.CompositionRoot.ArgsParser;
using System;
using Unity;

namespace MultithreadedGZip.CompositionRoot
{
    public static partial class UnityConfig
    {
        static void RegisterConfigurators(IUnityContainer unityContainer, string[] args)
        {
            PasrseArgs.ThrowIfBadArgs(args);

            var processors = Environment.ProcessorCount;
            var compressionMode = PasrseArgs.GetCompressionMode(args);
            var inFilePath = PasrseArgs.GetInFilePath(args);
            var outFilePath = PasrseArgs.GetOutFilePath(args);
            unityContainer.RegisterInstance(new MultithreadedGZipConfigurator(
                processors, inFilePath, outFilePath, compressionMode, 1024 * 1024));

            unityContainer.RegisterType<IGZipConfigurator, MultithreadedGZipConfigurator>();
            unityContainer.RegisterType<IMultithreadedConfigurator, MultithreadedGZipConfigurator>();
        }
    }
}