using Unity;

namespace MultithreadedGZip.Win.AppStart
{
    public class UnityConfig
    {
        public static IUnityContainer GetConfiguredContainer(string[] args)
        {
            var container = new UnityContainer();
            RegisterTypes(container, args);
            return container;
        }

        static void RegisterTypes(IUnityContainer container, string[] args)
        {
            CompositionRoot.UnityConfig.RegisterTypes(container, args);
        }
    }
}
