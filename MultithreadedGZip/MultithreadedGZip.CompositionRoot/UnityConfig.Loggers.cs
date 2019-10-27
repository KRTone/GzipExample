using MultithreadedGZip.BLL.Interfaces;
using MultithreadedGZip.Loggers;
using Unity;

namespace MultithreadedGZip.CompositionRoot
{
    public static partial class UnityConfig
    {
		static void RegisterLoggers(IUnityContainer unityContainer)
        {
            unityContainer.RegisterSingleton<ILogger, ConsoleLogger>("consoleLogger");
            unityContainer.RegisterSingleton<ILogger, FileLogger>("fileLogger");
            unityContainer.RegisterSingleton<ILogService, LogService>();
        }
    }
}
