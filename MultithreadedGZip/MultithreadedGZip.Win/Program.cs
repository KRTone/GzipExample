using MultithreadedGZip.BLL.Interfaces;
using MultithreadedGZip.Win.AppStart;
using System;
using Unity;

namespace MultithreadedGZip.Win
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Registering dependencies ...");

            var container = UnityConfig.GetConfiguredContainer(args);

            Console.WriteLine("Configure logger ...");
            
            var logger = container.Resolve<ILogService>();
            log4net.Config.XmlConfigurator.Configure();

            AppDomain.CurrentDomain.UnhandledException += (s,e) => 
            {
                logger.Exception(e.ExceptionObject as Exception);

                Console.WriteLine("Program failed");
                Console.WriteLine("Press any key to exit");
                Console.ReadKey();

                Environment.Exit(1);
            };

            Console.WriteLine("All done. Starting program...");

            using (var program = container.Resolve<ProgramStarter>())
            {
                program.Run();
            }

            Console.WriteLine("Program successfully completed");
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();

            Environment.Exit(0);
        }
    }


}
