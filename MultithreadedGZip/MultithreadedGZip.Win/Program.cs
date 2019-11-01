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

            Console.WriteLine("All done. Starting program...");

            var program = container.Resolve<ProgramStarter>();
            program.Run();

            Console.WriteLine("Program successfully completed");
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();

            Environment.Exit(0);
        }
    }
}
