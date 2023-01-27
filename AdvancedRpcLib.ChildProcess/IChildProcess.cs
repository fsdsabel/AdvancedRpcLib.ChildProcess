using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace AdvancedRpcLib.ChildProcess
{
    public interface IChildProcess : IDisposable
#if NET6_0_OR_GREATER
            , IAsyncDisposable
#endif
    {
        /// <summary>
        /// The child process object.
        /// </summary>
        Process Process { get; }

        /// <summary>
        /// True, if the process has been terminated.
        /// </summary>
        bool IsDisposed { get; }

        /// <summary>
        /// Forces termination of the child process.
        /// </summary>
        /// <param name="timeout">A timeout to wait for process termination.</param>
        void Terminate(TimeSpan timeout);

#if NET6_0_OR_GREATER
        /// <summary>
        /// Forces termination of the child process.
        /// </summary>
        /// <param name="cancellationToken">If the cancellationToken is canceled, waiting for process termination will be aborted.</param>        
        Task TerminateAsync(CancellationToken cancellationToken = default);
#endif
    }
}