// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// kb/Work PB3294: THE STANDARD DEVICES ON A WINDOWS CONSOLE CARRY THE WHOLE UTF-16 REPERTOIRE AND LEAVE THE CONSOLE'S
/// CODE PAGES AS THEY FOUND THEM. Two defects with one root, <c>StandardStreams.EnsureUtf8</c> reaching the console
/// through the code-page setters:
/// <list type="number">
/// <item><description><b>Interactive ACCEPT.</b> The input arm only decoded a REDIRECTED standard input, so an
/// interactive console kept its input code page and every character outside it was stored as <c>?</c>. ISO §14.9.1.4 GR1
/// leaves "any conversion of data required between the device and the data item" to the implementor and A.1 item 1 makes
/// that conversion a documented one; WiseOwl COBOL's determination (docs/CONFORMANCE.md DOC-A.1-1) is that the device
/// delivers characters of the UTF-16 repertoire. Typed at a console whose input code page is 437, € (U+20AC) and
/// 中 (U+4E2D) must therefore arrive as themselves.</description></item>
/// <item><description><b>Console code-page leak.</b> The output arm called the <see cref="Console.OutputEncoding"/> setter,
/// which is <c>SetConsoleOutputCP(65001)</c> on the console the parent shell shares, and never restored it, so
/// <c>cmd</c> stayed on 65001 after a program that only DISPLAYed. No clause of the standard governs the host shell's
/// console; the bar is DOC-A.1-59 (the standard display device) and the class's own stated rule that
/// <c>StandardStreams</c> must not change a console shared with the parent.</description></item>
/// </list>
/// The witnesses run real programs on a real console with no window, a Windows pseudoconsole
/// (<see cref="WindowsPseudoConsole"/>), because a redirected pipe is exactly the case the defects do NOT reach (the
/// redirected-input arm always worked, <c>AcceptDifferentialTests.Device_RedirectedInput_DecodesAsUtf8</c>). Each puts
/// its console in code page 437 first and asserts it IS in 437 before the program runs, so a green here cannot mean the
/// probe never exercised the defect. Windows only: no other platform has a console code page to leak or lose input to.
/// </summary>
public sealed class WindowsConsoleDeviceTests
{
    private const string AcceptProgram = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. CONACC.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 WS-X PIC {0}.
        PROCEDURE DIVISION.
        P1.
            ACCEPT WS-X.
            DISPLAY FUNCTION ORD(WS-X(1:1)) " " FUNCTION ORD(WS-X(2:1)) " "
                    FUNCTION ORD(WS-X(3:1)) " " FUNCTION ORD(WS-X(4:1)).
            STOP RUN.
        """;

    private const string DisplayProgram = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. CONDISP.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        SPECIAL-NAMES.
            SYSERR IS ERROR-DEVICE.
        PROCEDURE DIVISION.
        P1.
            DISPLAY N"Grüße €".
            DISPLAY N"Fehler ß" UPON ERROR-DEVICE.
            STOP RUN.
        """;

    /// <summary>Prints the console's two code pages as the C# runtime reads them (<c>GetConsoleCP</c> and
    /// <c>GetConsoleOutputCP</c>); it changes neither.</summary>
    private const string CodePageProbe = """
        public static class CpProbe
        {
            public static void Main()
            {
                System.Console.WriteLine("in=" + System.Console.InputEncoding.CodePage + " out=" + System.Console.OutputEncoding.CodePage);
            }
        }
        """;

    /// <summary>A compile error whose message carries the citation's section sign and en dash (the shape
    /// <c>CliDiagnosticEncodingTests</c> uses): COBOL0886.</summary>
    private const string RejectedProgram =
        "       IDENTIFICATION DIVISION.\n"
        + "       PROGRAM-ID. CONBAD IS INITIAL RECURSIVE PROGRAM.\n"
        + "       PROCEDURE DIVISION.\n"
        + "           STOP RUN.\n";

    /// <summary>The last line every script writes, so the run knows the console has rendered everything before it.</summary>
    private const string Sentinel = "RUN-COMPLETE-PB3294";

    private static readonly Regex CodePages = new(@"in=(\d+) out=(\d+)", RegexOptions.NonBacktracking);

    private static string Cmd() => Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe";

    /// <summary>Run <paramref name="body"/> (script lines) after putting the console in code page 437, with the code-page
    /// probe before and after it; return the plain console text and the code pages the two probes read.</summary>
    private static (string Text, List<(int In, int Out)> Probes) RunOnConsole(string dir, string body, string typed)
    {
        string probe = $"dotnet \"{Path.Combine(dir, "CPPROBE.dll")}\"";
        string script = Path.Combine(dir, "run.cmd");
        File.WriteAllText(script, "@echo off\r\nchcp 437 >nul\r\n" + probe + "\r\n" + body + "\r\n" + probe + "\r\necho " + Sentinel + "\r\n");
        var (exit, vt) = WindowsPseudoConsole.Run($"\"{Cmd()}\" /d /c call \"{script}\"", dir, typed, Sentinel);
        string text = WindowsPseudoConsole.Plain(vt);
        Assert.True(exit == 0, $"the script exited {exit}; console text:\n{text}");
        var probes = CodePages.Matches(text).Select(m => (int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value))).ToList();
        Assert.True(probes.Count == 2, $"expected the code-page probe twice, saw {probes.Count}; console text:\n{text}\nraw ({vt.Length} chars): {vt.Replace("\u001b", "<ESC>")}");
        Assert.True(probes[0] == (437, 437),
            $"the probe could not put its console into code page 437, so it was NOT exercising the defect and its green "
            + $"means nothing (feedback_green_gates_arent_evidence); it read {probes[0]}:\n{text}");
        return (text, probes);
    }

    private static string Dll(string dir, string name) => $"dotnet \"{Path.Combine(dir, name + ".dll")}\"";

    // §14.9.1.4 GR1 + CONFORMANCE.md DOC-A.1-1: the device delivers characters of the UTF-16 repertoire, and an
    // alphanumeric or national receiver stores them as-is, one per character position. The ordinal position of a
    // character is its code-unit value plus one (item 188): € U+20AC 8364 -> 8365, 中 U+4E2D 20013 -> 20014,
    // E U+0045 69 -> 70, é U+00E9 233 -> 234. Under input code page 437 the first two were stored as '?' (ORD 64).
    [Theory]
    [InlineData("N(4)")]
    [InlineData("X(4)")]
    public void Accept_from_an_interactive_console_keeps_characters_outside_the_input_code_page(string picture)
    {
        if (!OperatingSystem.IsWindows()) return;   // no console code page elsewhere (see the class remarks)
        string dir = ScratchDirectory.Create("conacc");
        try
        {
            NonCobolElement.CompileCobol(dir, "CONACC", AcceptProgram.Replace("{0}", picture));
            NonCobolElement.CompileCobol(dir, "CONDISP", DisplayProgram);
            NonCobolElement.CompileCSharp(dir, "CPPROBE", CodePageProbe, "CONDISP");

            var (text, probes) = RunOnConsole(dir, Dll(dir, "CONACC"), "€中Eé\r");

            Assert.Contains("8365 20014 70 234", text);
            Assert.Equal((437, 437), probes[1]);   // and the console's code pages are still the shell's
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    // DOC-A.1-59: the standard display device carries UTF-8 on a byte stream and the repertoire through the console's
    // wide-character interface on a console, and no case changes the console's code pages: stdout and stderr on the
    // console with stdin redirected (a), stdout redirected to a file with stderr still on the console (b), stderr
    // redirected with stdout still on the console (c: the other half of the per-handle rule), and all three on the
    // console (d).
    [Theory]
    [InlineData("console")]
    [InlineData("stdout-redirected")]
    [InlineData("stderr-redirected")]
    [InlineData("stdin-console")]
    public void A_program_leaves_the_console_code_pages_as_it_found_them(string arm)
    {
        if (!OperatingSystem.IsWindows()) return;   // no console code page elsewhere (see the class remarks)
        string dir = ScratchDirectory.Create("conpage");
        try
        {
            NonCobolElement.CompileCobol(dir, "CONDISP", DisplayProgram);
            NonCobolElement.CompileCSharp(dir, "CPPROBE", CodePageProbe, "CONDISP");
            string captured = Path.Combine(dir, "captured.txt");
            string run = Dll(dir, "CONDISP");
            string body = arm switch
            {
                "console" => run + " <nul",
                "stdout-redirected" => run + $" <nul >\"{captured}\"",
                "stderr-redirected" => run + $" <nul 2>\"{captured}\"",
                _ => run,
            };

            var (text, probes) = RunOnConsole(dir, body, "");

            Assert.Equal((437, 437), probes[1]);
            // The stream that is redirected carries UTF-8 whatever the console's code page is ("Grüße €" is the bytes
            // 47 72 C3 BC C3 9F 65 20 E2 82 AC; "Fehler ß" is 46 65 68 6C 65 72 20 C3 9F), decoded strictly so an
            // invalid sequence fails here; the stream that is still the console shows its characters as typed, NOT
            // narrowed to code page 437.
            (string onFile, string onFileHex, string onConsole) = arm switch
            {
                "stdout-redirected" => ("Grüße €", "4772C3BCC39F6520E282AC", "Fehler ß"),
                "stderr-redirected" => ("Fehler ß", "4665686C657220C39F", "Grüße €"),
                _ => ("", "", ""),
            };
            if (onFile.Length > 0)
            {
                byte[] bytes = File.ReadAllBytes(captured);
                Assert.Equal(onFile, new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(bytes).TrimEnd('\r', '\n'));
                Assert.Equal(onFileHex, Convert.ToHexString(bytes.Where(b => b is not (0x0D or 0x0A)).ToArray()));
                Assert.Contains(onConsole, text);
            }
            else
            {
                Assert.Contains("Grüße €", text);
                Assert.Contains("Fehler ß", text);
            }
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    // The cobol command is a process entry too (StandardStreams.EnsureUtf8 from its Main): its diagnostics, with the
    // citation's section sign and en dash, reach the console whole, and a compile leaves the console's code pages alone.
    [Fact]
    public void The_cobol_command_leaves_the_console_code_pages_as_it_found_them()
    {
        if (!OperatingSystem.IsWindows()) return;   // no console code page elsewhere (see the class remarks)
        string dir = ScratchDirectory.Create("concli");
        try
        {
            NonCobolElement.CompileCobol(dir, "CONDISP", DisplayProgram);
            NonCobolElement.CompileCSharp(dir, "CPPROBE", CodePageProbe, "CONDISP");
            string source = Path.Combine(dir, "conbad.cob");
            File.WriteAllText(source, RejectedProgram);
            string cli = TestRepo.SrcBin("WiseOwl.Cobol.Cli", "cobol.exe");
            Assert.True(File.Exists(cli), $"the cobol driver is not built at {cli}");

            var (text, probes) = RunOnConsole(dir, $"\"{cli}\" \"{source}\" --std 2023 <nul", "");

            Assert.Equal((437, 437), probes[1]);
            Assert.Contains("COBOL0886", text);
            Assert.Contains("§", text);
            Assert.Contains("–", text);
        }
        finally { ScratchDirectory.Remove(dir); }
    }
}
