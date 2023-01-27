using AdvancedRpcLib.ChildProcess;
using System.Diagnostics;

namespace SampleParent
{
    internal class Program
    {
        private static IChildProcess ChildProcess;

        static void Main(string[] args)
        {
            Console.WriteLine("Sample Parent");
            while (true)
            {
                Console.WriteLine("1) Try to start new child");
                Console.WriteLine("2) Exit");

                string input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        StartChild();
                        break;
                    case "2":
                        Exit();
                        return;
                }
            }
        }

        static void StartChild()
        {           
            var psi = new ProcessStartInfo(@"..\..\..\..\SampleChild\bin\Debug\net6.0\SampleChild.exe")
            {
                CreateNoWindow = false,
                UseShellExecute = true
            };

            var newProcess = ChildProcessManager.Instance.StartSingletonChildProcess(psi, new ChildProcessOptions { RestartDelay = TimeSpan.FromSeconds(1) }); ;
            if (newProcess != null)
            {
                ChildProcess = newProcess;
            }
        }

        static void Exit()
        {
            ChildProcess?.Terminate(TimeSpan.FromSeconds(60));
        }
    }
}