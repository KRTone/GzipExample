using MultithreadedGZip.BLL.GZip;
using MultithreadedGZip.BLL.Interfaces;
using MultithreadedGZip.BLL.MultithreadedExtensions;
using MultithreadedGZip.BLL.Writers;
using Unity;
using Unity.Lifetime;

namespace MultithreadedGZip.CompositionRoot
{
    public static partial class UnityConfig
    {
        static void RegisterMultithreadedGZip(IUnityContainer unityContainer)
        {
            unityContainer.RegisterType<IBlockWriter, BytesToBlockWriter>();
            unityContainer.RegisterType<IBlocksEngine, BlocksEngine>();
            unityContainer.RegisterType<ICustomSemaphore, DynamicSemaphore>(new ContainerControlledLifetimeManager());
            unityContainer.RegisterType<IQueuedThreadPool, QueuedThreadPool>();
            unityContainer.RegisterType<IGZipExecutor, GZipExecutor>();
            unityContainer.RegisterType<ICompressor, GZipCompressor>(new ContainerControlledLifetimeManager());
        }
    }
}
