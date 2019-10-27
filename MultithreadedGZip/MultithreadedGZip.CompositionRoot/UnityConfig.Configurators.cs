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
            var processors = Environment.ProcessorCount;
            var compressionMode = args.GetCompressionMode();
            var inFilePath = args.GetInFilePath();
            var outFilePath = args.GetOutFilePath();
            unityContainer.RegisterInstance(new MultithreadedGZipConfigurator(
                processors, inFilePath, outFilePath, compressionMode, 1024 * 1024));

            unityContainer.RegisterType<IGZipConfigurator, MultithreadedGZipConfigurator>();
            unityContainer.RegisterType<IMultithreadedConfigurator, MultithreadedGZipConfigurator>();
        }
    }
}