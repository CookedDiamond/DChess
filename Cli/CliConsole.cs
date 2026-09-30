using System;
using System.IO;
using System.Runtime.InteropServices;

namespace DChess.Cli {
    /// <summary>
    /// The project is built as WinExe (GUI subsystem) so MonoGame can own its
    /// window. As a side effect, when the .exe is launched from a terminal
    /// Windows does NOT inherit the parent's console — Console.Out goes to a
    /// dead handle and Console.In returns EOF immediately. Reattaching here
    /// fixes both for the duration of the CLI session.
    /// </summary>
    internal static class CliConsole {

        private const int ATTACH_PARENT_PROCESS = -1;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        public static void AttachToParent() {
            bool inRedirected = Console.IsInputRedirected;
            bool outRedirected = Console.IsOutputRedirected;
            bool errRedirected = Console.IsErrorRedirected;

            // If everything is already redirected (piped), the standard handles
            // are real and writes/reads work — leave them alone. AttachConsole
            // would *replace* the process std handles with console handles and
            // break the pipes.
            if (inRedirected && outRedirected && errRedirected) return;

            if (!AttachConsole(ATTACH_PARENT_PROCESS)) {
                AllocConsole();
            }

            // Rebind only the streams that weren't redirected. Redirected ones
            // still point at the original pipe handles (we never touched them).
            try {
                if (!outRedirected) {
                    var stdout = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
                    Console.SetOut(stdout);
                }
                if (!errRedirected) {
                    var stderr = new StreamWriter(Console.OpenStandardError()) { AutoFlush = true };
                    Console.SetError(stderr);
                }
                if (!inRedirected) {
                    var stdin = new StreamReader(Console.OpenStandardInput());
                    Console.SetIn(stdin);
                }
            } catch {
                // Best-effort — fall through.
            }
        }
    }
}
