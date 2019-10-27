using Unity;

namespace MultithreadedGZip.CompositionRoot
{
    public static partial class UnityConfig
    {
        public static void RegisterTypes(IUnityContainer unityContainer, string[] args)
        {
            RegisterConfigurators(unityContainer, args);
            RegisterLoggers(unityContainer);
            RegisterMultithreadedGZip(unityContainer);
        }
    }
}
