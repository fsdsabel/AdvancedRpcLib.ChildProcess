using AdvancedRpcLib.ChildProcess.Internal;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AdvancedRpcLib.ChildProcess
{

    /// <summary>
    /// The default implementation of <see cref="IChildProcessManager"/>.
    /// </summary>
    public class ChildProcessManager : IChildProcessManager
    {
        private const string SingletonHandleName = "Singleton";
        private const string ProcessStartupHandleName = "ProcessStartup";
        private const string ProcessTerminateHandleName = "ProcessTerminate";

        /// <summary>
        /// A default instance of <see cref="IChildProcessManager"/>.
        /// </summary>
        public static IChildProcessManager Instance { get; } = new ChildProcessManager();

        class ChildProcess : IChildProcess
        {
            private readonly EventWaitHandle _terminateWaitHandle;

            public ChildProcess(Process process, EventWaitHandle terminateWaitHandle)
            {
                Process = process;
                _terminateWaitHandle = terminateWaitHandle;
            }

            public bool IsDisposed { get; private set; }
            public Process Process { get; private set; }

            internal void Update(ChildProcess? newChildProcess)
            {
                if (newChildProcess is null)
                {
                    // process exited so nothing to do anymore
                    IsDisposed = true;
                }
                else
                {
                    Process = newChildProcess.Process;             
                }
            }

            public void Terminate(TimeSpan timeout)
            {
                if (!IsDisposed)
                {
                    IsDisposed = true;
                    _terminateWaitHandle.Set();
                    _terminateWaitHandle.Dispose();
                    if (!Process.WaitForExit((int)timeout.TotalMilliseconds))
                    {
#if NET6_0_OR_GREATER
                        Process.Kill(true);
#else
                    Process.Kill();
#endif
                    }
                }
            }

#if NET6_0_OR_GREATER
            public async Task TerminateAsync(CancellationToken cancellationToken = default)
            {
                if (!IsDisposed)
                {
                    IsDisposed = true;
                    _terminateWaitHandle.Set();
                    _terminateWaitHandle.Dispose();
                    await Process.WaitForExitAsync(cancellationToken);
                    if (cancellationToken.IsCancellationRequested)
                    {
                        Process.Kill(true);
                    }
                }
            }
#endif

            public void Dispose()
            {
                if (!IsDisposed)
                {
                    IsDisposed = true;
                    _terminateWaitHandle.Set();
                    _terminateWaitHandle.Dispose();
                    Process.WaitForExit();
                }
            }

#if NET6_0_OR_GREATER
            public async ValueTask DisposeAsync()
            {
                if (!IsDisposed)
                {
                    IsDisposed = true;
                    _terminateWaitHandle.Set();
                    _terminateWaitHandle.Dispose();
                    await Process.WaitForExitAsync();
                }
            }

           
#endif
        }

       

        private static string CreateHandleName(string baseName, string executableName, ChildProcessOptions options)
        {
            var singletonIdentifier = baseName + options.SingletonIdentifier;

            if (options.AllowMultipleChildProcessesFromDifferentPaths)
            {
                using var md5 = MD5.Create();
                singletonIdentifier += Convert.ToBase64String(md5.ComputeHash(Encoding.UTF8.GetBytes(Path.GetFullPath(executableName).ToLowerInvariant())));
            }
            if (options.AllowMultipleChildProcessesForDifferentUsers)
            {
                singletonIdentifier += Environment.UserName + "@" + Environment.UserDomainName;
            }

            singletonIdentifier = (options.AllowMultipleChildProcessesForDifferentUsers ? @"Local\" : @"Global\") + singletonIdentifier;

            return singletonIdentifier;
        }

        /// <inheritdoc />
        public IChildProcess? StartSingletonChildProcess(ProcessStartInfo processStartInfo, ChildProcessOptions? options = null, TimeSpan? timeout = null)
        {            
            options ??= ChildProcessOptions.Default;

            var childProcess = new Process
            {
                StartInfo = processStartInfo
            };

            var singletonIdentifier = CreateHandleName(SingletonHandleName, processStartInfo.FileName, options);

            if (Mutex.TryOpenExisting(singletonIdentifier, out var mutex))
            {
                mutex.Dispose();
                return null;
            }

            var terminateWaitHandle = new EventWaitHandle(false,
                EventResetMode.AutoReset, CreateHandleName(ProcessTerminateHandleName, processStartInfo.FileName, options));

            var childProcessHandle = new ChildProcess(childProcess, terminateWaitHandle);
            
            if (options.RestartOnExit)
            {
                childProcess.EnableRaisingEvents = true;
                childProcess.Exited += (s, e) =>
                {
                    if(!childProcessHandle.IsDisposed)
                    {
                        while (childProcessHandle.Process.ExitTime + options.RestartDelay > DateTime.Now)
                        {
                            Thread.Sleep(100);
                        }
                        var newChildProcess = (ChildProcess) StartSingletonChildProcess(processStartInfo, options)!;
                        childProcessHandle.Update(newChildProcess);
                    }
                };
            }

            using var startupWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset, CreateHandleName(ProcessStartupHandleName, processStartInfo.FileName, options));

            childProcess.Start();

            timeout ??= Timeout.InfiniteTimeSpan;
            if (!startupWaitHandle.WaitOne(timeout.Value))
            {
                childProcessHandle.Terminate(TimeSpan.Zero);
                throw new TimeoutException("The child process failed to call RegisterAsChildProcess in time.");
            }

            return childProcessHandle;
        }

        /// <inheritdoc />
        public bool RegisterAsChildProcess(ChildProcessOptions? options = null)
        {
            options ??= ChildProcessOptions.Default;
            var exeName = Process.GetCurrentProcess().MainModule?.FileName ?? "";
            // create a mutex to tell the parent process we are here
            var singletonIdentifier = CreateHandleName(SingletonHandleName, exeName, options);
            _ = new Mutex(true, singletonIdentifier, out var createdNew); // the mutex is closed by the system when the process terminates

            // signal the parent that we are initialized
            if (!EventWaitHandle.TryOpenExisting(CreateHandleName(ProcessStartupHandleName, exeName, options), out var startupHandle))
            {
                return false;
            }
            startupHandle.Set();

            return createdNew;
        }

        /// <inheritdoc />
        public async Task WaitForExitNotificationAsync(ChildProcessOptions? options = null, CancellationToken cancellationToken = default)
        {
            try
            {
                // set up watchdog for parent process .. if it is killed, we terminate too
                options ??= ChildProcessOptions.Default;
                var parent = ParentProcessUtilities.GetParentProcess();
                var exeName = Process.GetCurrentProcess().MainModule?.FileName ?? "";

                EventWaitHandle.TryOpenExisting(CreateHandleName(ProcessTerminateHandleName, exeName, options), out var parentRequestedExitHandle);

                await Task.Run(async () =>
                {
                    try
                    {
                        while (!cancellationToken.IsCancellationRequested && !(parentRequestedExitHandle?.WaitOne(0) ?? false))
                        {
                            if (parent.HasExited)
                            {                                
                                return;
                            }
                            await Task.Delay(50, cancellationToken);
                        }
                    }
                    catch (Exception)
                    {                     
                    }
                });
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}