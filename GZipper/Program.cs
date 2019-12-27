using AM.GZipperLib;
using System;
using System.Diagnostics;

namespace GZipper
{
    public class Program
    {
        static void Main(string[] args)
        {
            var argProcessor = new ArgumentProcessor(args);
            if (!argProcessor.ParseArguments())
                return;

            CompressionConfig config = argProcessor.GetConfig();
            var taskDispatcher = new TaskDispatcherFabric().CreateTaskDispatcher(config, new ConsoleInformer());
            Console.CancelKeyPress += (sender, e) => 
            {
                e.Cancel = true;
                taskDispatcher.Cancel();
            };

            try
            {
                taskDispatcher.RunAndWait();
            }
            catch (Exception ex)
            {
                Console.WriteLine(Environment.NewLine + $"Error during process: {ex.Message}");
            }
            Console.WriteLine(Environment.NewLine + "Finished");
        }
    }
}
