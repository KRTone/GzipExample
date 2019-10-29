using System;
using Unity;

namespace MultithreadedGZip.Win.AppStart
{
    public class UnityConfig
    {
        public static IUnityContainer GetConfiguredContainer(string[] args)
        {
            var container = new UnityContainer();

            //надо так же перехватывать ошибки при сборке/разрешении зависимостей
            //поэтому подписываемя перед регистрацией типов
            Console.WriteLine("Configure logger ...");
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                Exception exToLog = ex;
                while (exToLog.InnerException != null)
                {
                    exToLog = ex.InnerException;
                }
                
                container.Resolve<BLL.Interfaces.ILogService>().Exception(exToLog);

                Console.WriteLine("Program failed");
                Console.WriteLine("Press any key to exit");
                Console.ReadKey();

                Environment.Exit(1);
            };

            Console.WriteLine("Register types ...");
            CompositionRoot.UnityConfig.RegisterTypes(container, args);
            return container;
        }
    }
}
