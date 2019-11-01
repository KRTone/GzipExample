using MultithreadedGZip.BLL.GZip;
using MultithreadedGZip.BLL.Interfaces;
using MultithreadedGZip.BLL.Interfaces.Configurators;
using Unity;

namespace MultithreadedGZip.CompositionRoot
{
    public static partial class UnityConfig
    {
        static void RegisterMultithreadedGZip(IUnityContainer unityContainer)
        {
            if (unityContainer.Resolve<IGZipConfigurator>().CompressionMode == System.IO.Compression.CompressionMode.Compress)
                unityContainer.RegisterType<IMultithreadedGZipExecutor, MultithreadedCompressor>();
            else
                unityContainer.RegisterType<IMultithreadedGZipExecutor, MultithreadedDecompressor>();
        }
    }
}
