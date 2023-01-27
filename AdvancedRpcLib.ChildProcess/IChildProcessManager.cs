using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace AdvancedRpcLib.ChildProcess
{
    /// <summary>
    /// Allows to spawn single child processes with bilateral monitoring. If one of the two terminates, the other one will react appropriately.
    /// </summary>
    public interface IChildProcessManager
    {
        /// <summary>
        /// Start a new singleton child process.
        /// </summary>
        /// <param name="processStartInfo">The <see cref="ProcessStartInfo"/> usually given to <see cref="Process.Start"/></param>
        /// <param name="options">Options that change the behavior of the process management.</param>
        /// <param name="timeout">A timeout to wait for the client to call <see cref="RegisterAsChildProcess(ChildProcessOptions?)"/> before the start fails.</param>
        /// <returns>An object describing the created child process or null if the project was already running</returns>
        /// <exception cref="TimeoutException">Thrown, if the child process didn't call <see cref="RegisterAsChildProcess(ChildProcessOptions?)"/> in the allowed time.</exception>
        IChildProcess? StartSingletonChildProcess(ProcessStartInfo processStartInfo, ChildProcessOptions? options = null, TimeSpan? timeout = null);

        /// <summary>
        /// Registers a process as a child process. This notifies the parent process that the process initialized successfully.
        /// </summary>
        /// <remarks>Call this whenever your RPC mechanism is ready to take requests.</remarks>
        /// <param name="options">The same options the parent process used.</param>
        /// <returns>true, if the handshake with the parent process was successful. False, if the application wasn't started as a child process or the parent did exit.</returns>
        bool RegisterAsChildProcess(ChildProcessOptions? options = null);

        /// <summary>
        /// Called by child processes to wait for an exit notification of the parent process. Whenever this finishes you should shut down the child process.
        /// </summary>
        /// <param name="options">The same options the parent process used.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to force client process shutdown.</param>        
        Task WaitForExitNotificationAsync(ChildProcessOptions? options = null, CancellationToken cancellationToken = default);
    }
}