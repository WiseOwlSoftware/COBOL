// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;

namespace WiseOwl.Cobol.Runtime.IO;

/// <summary>
/// THE process-wide rule for what WiseOwl COBOL's standard streams carry: the <b>UTF-16 repertoire</b>, never narrowed to
/// a code page. A standard stream that is a byte stream (a pipe or a file, on every platform) carries it as <b>UTF-8</b>;
/// one that is a Windows console carries it through the console's own wide-character interface; one that is a Unix
/// terminal carries it as UTF-8 on output and in the terminal's locale on input. Only an entry that IS the process
/// applies it: the <c>cobol</c> CLI's <c>Main</c> and <see cref="ProgramTable.RunModule"/>, the generated <c>Main</c>'s one
/// statement. A host's process console belongs to the host, and no run unit the host embeds touches it (kb/Work PB3193).
/// </summary>
/// <remarks>
/// <para>
/// The repertoires are UTF-16 (CONFORMANCE.md §7 item 188) and the alphanumeric↔national correspondence is the
/// total identity (item 33), so the DEVICE boundary must carry the repertoire or every wide character silently
/// becomes mojibake at the OS code page — a data loss no rule authorizes. The standard display device therefore
/// writes UTF-8 to a byte stream (CONFORMANCE.md §7 item 59; the PB59 landing), and <c>ProcessObserver</c> in the test
/// harness decodes every capture the same way. The ACCEPT device is the same boundary in the other direction (item 1:
/// the device delivers characters of the UTF-16 repertoire), so a redirected standard input is DECODED as UTF-8, as it
/// already was on a host whose console default is UTF-8. On Windows the default was the console's OEM code page, and
/// a piped <c>é</c> (the bytes C3 A9) reached the program as the two characters <c>├⌐</c> under cp437 (kb/Work
/// PB3193, measured).
/// </para>
/// <para>
/// ⛔ A CONSOLE CODE PAGE IS NEVER CHANGED (kb/Work PB3294). A Windows console is shared with the parent shell, so
/// <c>SetConsoleCP</c> and <c>SetConsoleOutputCP</c> (what <see cref="Console.InputEncoding"/> and
/// <see cref="Console.OutputEncoding"/> call for any code page) outlive the process: <c>cmd</c> stayed on code page
/// 65001 after a program that only DISPLAYed, measured. And an interactive console left on its own input code page loses
/// every character outside it: typing € and 中 at <c>ACCEPT X</c> under code page 437 stored <c>?</c> for both. Both are
/// one mechanism, and .NET offers the way out: a console handle whose encoding is UTF-16 (code page 1200) is read and
/// written with <c>ReadConsoleW</c> / <c>WriteConsoleW</c>, the console's wide-character interface, and the setter skips
/// the code-page call for 1200. So a Windows console handle gets <see cref="Encoding.Unicode"/> and carries the whole
/// repertoire with the console's code pages as the shell left them. That interface does not carry the console's
/// end-of-input signal (Ctrl+Z at the start of a line), so the interactive input is read through
/// <see cref="ConsoleInputReader"/>, which restores it. A Unix terminal has no such shared OS state (the
/// setter records the encoding in the process only), so it keeps the terminal's locale on input and carries UTF-8 on
/// output, as before.
/// </para>
/// <para>
/// ⛔ ONLY A PROCESS ENTRY CALLS IT (kb/Work PB3193; docs/CONFORMANCE.md DOC-A.1-59, DOC-A.1-167). The console
/// (<see cref="Console.Out"/>, <see cref="Console.Error"/>, <see cref="Console.In"/>, <see cref="Console.OutputEncoding"/>)
/// belongs to the PROCESS, and the entries that are the process own it: the CLI, and
/// <see cref="ProgramTable.RunModule"/> ("A standalone <c>cobol</c> executable IS its own host", DOC-A.1-167). A host
/// that embeds a run unit (through a program's generated host entry, <c>Cobol.&lt;assembly&gt;.&lt;NAME&gt;.Call</c>, DESIGN-external-repository §4.4, or <c>RunUnit.Run</c>; a CALL into a module) owns its
/// console, and the run unit writes to it as the host configured it: under a host, the standard display device IS the
/// host's <see cref="Console.Out"/> (DOC-A.1-59). Before PB3193 every <c>RunMain</c> called this, so a host that had
/// given its console another encoding found it switched to UTF-8 by the first run unit it ran, and a host with no
/// console found its own writers replaced by the fallback's. That is the console twin of the exit-code defect PB3163
/// fixed the same way. ISO §14.6.1 makes a run unit "an independent entity that may be executed without communicating
/// with, or being coordinated with, any other run unit" (except through messages, files and switches), and a run unit
/// that rewrote a process-wide cell would coordinate with its host through it. <c>ProcessStateOwnerDriftTests</c>
/// holds the callers to the two process entries.
/// </para>
/// <para>
/// ⛔ WHY IT IS A SHARED HELPER AND NOT AN <c>if</c> (kb/Work PB899). The rule was written down in exactly one of
/// the two places that needed it: the run-unit entry set the encoding for a RUN program, and the
/// <c>cobol</c> CLI's diagnostic path set nothing at all. On a default Windows console that is cp437, so the
/// section sign in <c>(ISO §11.10.3 SR5–6)</c> left the process as the single byte <c>0x15</c> and the en dash
/// as <c>-</c> — measured, byte-for-byte, through a redirected stream. Every ISO citation the compiler prints
/// was corrupted in every CI log and every redirected capture, and no golden could see it because the corpus
/// runner matches <c>.err</c> files on a pure-ASCII diagnostic CODE substring. That is
/// <c>feedback_two_arm_dispatch</c>, the most reproducible defect shape in this repository: a dispatch with two
/// arms, one of them fixed. There is now ONE arm.
/// </para>
/// <para>
/// THE RULE, PER HANDLE. Each of standard input, output and error is decided by what it IS
/// (<see cref="Console.IsInputRedirected"/>, <see cref="Console.IsOutputRedirected"/>,
/// <see cref="Console.IsErrorRedirected"/>; a character device that is not a console, such as <c>NUL</c>, is redirected).
/// A <b>redirected</b> handle gets an explicit BOM-less UTF-8 reader or writer over the standard stream (writers
/// AutoFlush, as Console's own do), on every platform and with no console API involved, so it works in a process with
/// no console at all. A handle that <b>is a console</b> gets the console encoding of its platform: Windows
/// <see cref="Encoding.Unicode"/>, Unix UTF-8 on output. ⚠ The order is load-bearing: the
/// <see cref="Console.OutputEncoding"/> setter re-creates <see cref="Console.Out"/> and <see cref="Console.Error"/> for
/// whichever of them is a console, and a writer created after it for a REDIRECTED handle would carry the console's
/// encoding (UTF-16 bytes into a pipe, measured), so the console encodings are set first and the redirected handles'
/// explicit writers after.
/// </para>
/// </remarks>
public static class StandardStreams
{
    /// <summary>0 = not yet applied, 1 = applied. The encoding is a process-wide property, so the work is
    /// done once however many run units, CALLs or compilations pass through.</summary>
    private static int _applied;

    /// <summary>
    /// Make this process's standard streams carry the UTF-16 repertoire: UTF-8 on every redirected stream, the
    /// console's wide-character interface on a Windows console, and never a change of a console code page. Called by an
    /// entry that IS the process (the CLI's <c>Main</c>, <see cref="ProgramTable.RunModule"/>) and by nothing a host
    /// embeds. Safe to call any number of times, on any platform, including in a process with no console.
    /// </summary>
    public static void EnsureUtf8()
    {
        if (Interlocked.Exchange(ref _applied, 1) != 0) return;

        // BOM-less: a BOM would prefix every capture, and every golden, with three bytes of nothing.
        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        bool inRedirected = Console.IsInputRedirected;
        bool outRedirected = Console.IsOutputRedirected;
        bool errRedirected = Console.IsErrorRedirected;

        // The console handles first (see the class remarks: the setter below re-creates the console's writers, and a
        // redirected handle's explicit writer must be installed after it). Standard input before anything has read from
        // it, so no character is buffered under another decoding. The two directions are tried apart, because a refusal
        // in one leaves the other exactly as it was (a setter that throws has not applied its encoding), and the
        // fallback below must describe the handle's real state.
        bool windows = OperatingSystem.IsWindows();
        if (windows && !inRedirected)
        {
            if (TryApply(() => Console.InputEncoding = Encoding.Unicode))
            {
                // ReadConsoleW hands Ctrl+Z to the program as a character, where the console's own read ends the input.
                Console.SetIn(new ConsoleInputReader(Console.In));
            }
            else
            {
                inRedirected = true;
            }
        }
        if (!outRedirected || !errRedirected)
        {
            if (!TryApply(() => Console.OutputEncoding = windows ? Encoding.Unicode : utf8))
                outRedirected = errRedirected = true;
        }

        // ⚠ The explicit readers and writers are not a second mechanism: they are the SAME rule applied to a byte stream.
        // The reader is shaped like Console's own (no byte-order-mark detection), so a platform whose default is already
        // UTF-8 reads exactly what it read before; AutoFlush matches what Console's own writers do, so nothing a program
        // printed can be lost at an abnormal termination. A process with no standard handle at all (a Windows service)
        // has nothing to encode, and refusing to start over it would be worse than the mojibake this prevents.
        if (inRedirected)
        {
            try { Console.SetIn(new StreamReader(Console.OpenStandardInput(), utf8, detectEncodingFromByteOrderMarks: false)); }
            catch (IOException) { /* no such standard handle: nothing to encode (a Windows service) */ }
        }
        if (outRedirected)
        {
            try { Console.SetOut(new StreamWriter(Console.OpenStandardOutput(), utf8) { AutoFlush = true }); }
            catch (IOException) { /* no such standard handle: nothing to encode (a Windows service) */ }
        }
        if (errRedirected)
        {
            try { Console.SetError(new StreamWriter(Console.OpenStandardError(), utf8) { AutoFlush = true }); }
            catch (IOException) { /* no such standard handle: nothing to encode (a Windows service) */ }
        }
    }

    /// <summary>Apply one console encoding. False when the console refuses it, and then nothing has changed (a setter that
    /// throws has not applied its encoding): the stream is carried as a byte stream by the explicit reader or writer
    /// instead. A Windows wide-character encoding never asks the console (the setter skips the code-page call for
    /// UTF-16), so what this guards is the Unix output setter, which flushes the console writers it replaces, and a
    /// policy that refuses the console to this process.</summary>
    private static bool TryApply(Action apply)
    {
        try { apply(); return true; }
        catch (IOException) { return false; }
        catch (System.Security.SecurityException) { return false; }
    }
}
