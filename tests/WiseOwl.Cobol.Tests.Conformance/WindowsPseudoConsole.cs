// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32.SafeHandles;
using WiseOwl.Cobol.Tests.Shared;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// ⛔ A REAL WINDOWS CONSOLE FOR A CHILD PROCESS, WITH NO WINDOW (kb/Work PB3294): the Windows pseudoconsole (ConPTY,
/// <c>CreatePseudoConsole</c>) runs a command line on a console the TEST owns. Its standard handles are real console
/// handles (<c>GetConsoleMode</c> succeeds on them), which no redirected pipe is, so it is the one way to observe what a
/// program does to a console: the console's code pages, and what an interactive <c>ACCEPT</c> reads from it. The child's
/// console is the pseudoconsole's, so the developer's own terminal is never touched (the same isolation
/// <c>CreateNoWindow</c> gave <c>CliDiagnosticEncodingTests</c>) and no window opens (kb/Work PB3138).
/// </summary>
/// <remarks>
/// The child's keyboard is the <paramref name="input"/> text, written to the pseudoconsole's input pipe as UTF-8 (the
/// pseudoconsole's own wire format, whatever the console's code pages are) before the child reads it; what the child
/// writes to its console comes back as the host's VT stream, with the escape sequences removed by <see cref="Plain"/>.
/// The run is bounded by <see cref="ObservationBudget"/> like every observed process (kb/Work PB3108), and a run that
/// does not complete raises: a timeout is a non-observation, never a value to compare.
/// </remarks>
internal static class WindowsPseudoConsole
{
    private const uint ProcThreadAttributePseudoconsole = 0x00020016;
    private const uint ExtendedStartupInfoPresent = 0x00080000;
    private const uint WaitObject0 = 0;
    private const int StartfUseStdHandles = 0x00000100;

    [StructLayout(LayoutKind.Sequential)]
    private struct Coord { public short X; public short Y; }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct StartupInfo
    {
        public int cb;
        public string? lpReserved;
        public string? lpDesktop;
        public string? lpTitle;
        public int dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags;
        public short wShowWindow, cbReserved2;
        public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct StartupInfoEx
    {
        public StartupInfo StartupInfo;
        public IntPtr lpAttributeList;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessInformation
    {
        public IntPtr hProcess, hThread;
        public int dwProcessId, dwThreadId;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CreatePipe(out SafeFileHandle read, out SafeFileHandle write, IntPtr attributes, uint size);

    [DllImport("kernel32.dll")]
    private static extern int CreatePseudoConsole(Coord size, SafeFileHandle input, SafeFileHandle output, uint flags, out IntPtr console);

    [DllImport("kernel32.dll")]
    private static extern void ClosePseudoConsole(IntPtr console);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool InitializeProcThreadAttributeList(IntPtr list, int count, int flags, ref IntPtr size);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool UpdateProcThreadAttribute(IntPtr list, uint flags, IntPtr attribute, IntPtr value, IntPtr size,
        IntPtr previous, IntPtr returned);

    [DllImport("kernel32.dll")]
    private static extern void DeleteProcThreadAttributeList(IntPtr list);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CreateProcessW(string? application, StringBuilder commandLine, IntPtr processAttributes,
        IntPtr threadAttributes, bool inheritHandles, uint flags, IntPtr environment, string? currentDirectory,
        ref StartupInfoEx startup, out ProcessInformation process);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetExitCodeProcess(IntPtr process, out uint exitCode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool TerminateProcess(IntPtr process, uint exitCode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);

    /// <summary>The VT control sequences a pseudoconsole interleaves with the text: CSI sequences and OSC strings.</summary>
    private static readonly Regex VtSequence = new(@"\u001b\[[0-9;?<>=!]*[ -/]*[@-~]|\u001b\][^\u0007\u001b]*(?:\u0007|\u001b\\)|\u001b[()][0-9A-B]",
        RegexOptions.NonBacktracking);

    /// <summary>The text the child wrote to its console, with the host's VT control sequences removed and line endings
    /// normalized to <c>\n</c>.</summary>
    public static string Plain(string vt) => VtSequence.Replace(vt, "").ReplaceLineEndings("\n");

    /// <summary>Run <paramref name="commandLine"/> (a full Windows command line, e.g. <c>cmd.exe /c "…"</c>) on a new
    /// pseudoconsole with <paramref name="input"/> typed at its keyboard, and return its exit code and everything it wrote
    /// to the console (VT stream, UTF-8). ⚠ THE CONSOLE'S LAST OUTPUT IS NOT WHERE THE CHILD'S EXIT PUTS IT: the host
    /// renders the child's writes asynchronously, and closing the pseudoconsole straight after the child exits drops what
    /// it has not rendered yet (measured: a whole run's text was lost). The child therefore ends by writing
    /// <paramref name="sentinel"/>, and the run waits for it to come back before it closes the console.</summary>
    public static (int ExitCode, string Vt) Run(string commandLine, string workDir, string input, string sentinel)
    {
        var budget = ObservationBudget.OneProcess;
        if (!CreatePipe(out var inRead, out var inWrite, IntPtr.Zero, 0)) throw new Win32Exception();
        if (!CreatePipe(out var outRead, out var outWrite, IntPtr.Zero, 0)) throw new Win32Exception();
        int hr = CreatePseudoConsole(new Coord { X = 200, Y = 50 }, inRead, outWrite, 0, out IntPtr pty);
        if (hr != 0)
        {
            inRead.Dispose(); inWrite.Dispose(); outRead.Dispose(); outWrite.Dispose();
            throw new Win32Exception(hr, "CreatePseudoConsole failed (HRESULT 0x" + hr.ToString("X8") + ")");
        }
        // The pseudoconsole holds its own duplicates of its ends of the pipes.
        inRead.Dispose();
        outWrite.Dispose();

        IntPtr attributes = IntPtr.Zero;
        var process = default(ProcessInformation);
        var captured = new MemoryStream();
        using var sentinelSeen = new ManualResetEventSlim(false);
        byte[] sentinelBytes = new UTF8Encoding(false).GetBytes(sentinel);
        Thread? reader = null;
        try
        {
            IntPtr size = IntPtr.Zero;
            InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref size);   // sizing call: fails by design
            attributes = Marshal.AllocHGlobal(size);
            if (!InitializeProcThreadAttributeList(attributes, 1, 0, ref size)) throw new Win32Exception();
            if (!UpdateProcThreadAttribute(attributes, 0, (IntPtr)ProcThreadAttributePseudoconsole, pty, IntPtr.Size,
                    IntPtr.Zero, IntPtr.Zero))
                throw new Win32Exception();

            var startup = new StartupInfoEx { lpAttributeList = attributes };
            startup.StartupInfo.cb = Marshal.SizeOf<StartupInfoEx>();
            // ⚠ STARTF_USESTDHANDLES with all three handles NULL: without it the child takes THIS process's standard handles
            // (a test host's redirected pipes) instead of the pseudoconsole's, and writes there (measured).
            startup.StartupInfo.dwFlags = StartfUseStdHandles;
            if (!CreateProcessW(null, new StringBuilder(commandLine), IntPtr.Zero, IntPtr.Zero, false,
                    ExtendedStartupInfoPresent, IntPtr.Zero, workDir, ref startup, out process))
                throw new Win32Exception();

            // Drain the console's output on its own thread from the first byte: a full pipe would stall the child, and
            // ClosePseudoConsole waits for the pipe to be read to its end.
            reader = new Thread(() =>
            {
                try
                {
                    using var s = new FileStream(outRead, FileAccess.Read, 4096, false);
                    var chunk = new byte[4096];
                    int n;
                    while ((n = s.Read(chunk, 0, chunk.Length)) > 0)
                    {
                        lock (captured)
                        {
                            captured.Write(chunk, 0, n);
                            if (new ReadOnlySpan<byte>(captured.GetBuffer(), 0, (int)captured.Length).IndexOf(sentinelBytes) >= 0)
                                sentinelSeen.Set();
                        }
                    }
                }
                catch (IOException) { /* the pseudoconsole closed its end: end of output */ }
            }) { IsBackground = true, Name = "pseudoconsole-output" };
            reader.Start();

            byte[] typed = new UTF8Encoding(false).GetBytes(input);
            using (var keyboard = new FileStream(inWrite, FileAccess.Write, 4096, false))
            {
                keyboard.Write(typed, 0, typed.Length);
                keyboard.Flush();

                if (!sentinelSeen.Wait(budget.Ms) || WaitForSingleObject(process.hProcess, (uint)budget.Ms) != WaitObject0)
                {
                    TerminateProcess(process.hProcess, 1);
                    throw new TimeoutException($"the pseudoconsole child did not finish within {budget}: {commandLine}");
                }
            }
            GetExitCodeProcess(process.hProcess, out uint exit);
            ClosePseudoConsole(pty);
            pty = IntPtr.Zero;
            if (!reader.Join(budget.Ms)) throw new TimeoutException($"the pseudoconsole output did not end within {budget}");
            return ((int)exit, new UTF8Encoding(false).GetString(captured.ToArray()));
        }
        finally
        {
            if (pty != IntPtr.Zero) ClosePseudoConsole(pty);
            if (process.hProcess != IntPtr.Zero) CloseHandle(process.hProcess);
            if (process.hThread != IntPtr.Zero) CloseHandle(process.hThread);
            if (attributes != IntPtr.Zero) { DeleteProcThreadAttributeList(attributes); Marshal.FreeHGlobal(attributes); }
            inWrite.Dispose();
            outRead.Dispose();
        }
    }
}
