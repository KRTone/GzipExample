using Unity;

namespace MultithreadedGZip.CompositionRoot
{
    public static partial class UnityConfig
    {
        public static void RegisterTypes(IUnityContainer unityContainer, string[] args)
        {
            RegisterLoggers(unityContainer);
            RegisterConfigurators(unityContainer, args);
            RegisterMultithreadedGZip(unityContainer);
        }
    }
}
