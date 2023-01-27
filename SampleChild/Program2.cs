using AdvancedRpcLib.ChildProcess;
using System.Diagnostics;

namespace SampleChild
{
    internal class Program2
    {
        static async Task Main(string[] args)
        {
         //   while(!Debugger.IsAttached) { Thread.Sleep(5); }

            if (!ChildProcessManager.Instance.RegisterAsChildProcess())
            {
                // another instance has been started during our startup sequence -> quit immediately
                return;
            }

            Console.WriteLine("Sample Child");

            var cts = new CancellationTokenSource();


            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true; // we handle process termination ourselves
                Console.WriteLine("CTRL-C pressed. Process will exit and should be restarted.");
                cts.Cancel();
            };

            Console.WriteLine("Press CTRL-C to terminate child process. It should be restarted by parent process.");

            await ChildProcessManager.Instance.WaitForExitNotificationAsync(cancellationToken: cts.Token);
            if (!cts.IsCancellationRequested)
            {
                Console.WriteLine("Parent process requested termination. Terminating.");
            }
        }
    }
}