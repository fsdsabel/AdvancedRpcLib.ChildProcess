using System;
using System.Diagnostics;
namespace AdvancedRpcLib.ChildProcess
{
    public class ChildProcessOptions
    {
        /// <summary>
        /// Default options
        /// </summary>
        public static ChildProcessOptions Default { get; } = new ChildProcessOptions();

        /// <summary>
        /// Restart the child process with the same <see cref="ProcessStartInfo"/>
        /// </summary>
        public bool RestartOnExit { get; set; } = true;

        /// <summary>
        /// Delay between restart tries.
        /// </summary>
        public TimeSpan RestartDelay { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// An identifier that is used in addition to user name and client process executable path
        /// to distinguish client instances. Changing this you can run multiple client processes
        /// with the same executable and location.
        /// </summary>
        public string SingletonIdentifier { get; set; } = "6BCA8284CC0E4B70B8836DC3B72FD8B7";

        /// <summary>
        /// True to allow starting multiple child processes from different paths. Otherwise
        /// all processes with the same <see cref="SingletonIdentifier"/> are treated equally.
        /// </summary>
        public bool AllowMultipleChildProcessesFromDifferentPaths { get; set; } = true;

        /// <summary>
        /// Allows the same child process for different users on the same machine. This
        /// is for fast user switching and Terminal Servers.
        /// </summary>
        public bool AllowMultipleChildProcessesForDifferentUsers { get; set; } = true;
    }
}