// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics;
using System.Text;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ WHAT THE <c>cobol</c> CLI PUTS ON THE WIRE, IN BYTES — kb/Work PB899.
/// </summary>
/// <remarks>
/// <para>
/// Diagnostics carry ISO citations, so they carry non-ASCII: <c>(ISO §11.10.3 SR5–6)</c> holds U+00A7 and
/// U+2013. The CLI never set <see cref="Console.OutputEncoding"/>, while <c>ProgramTable.RunMain</c> did — one
/// rule, two places it could be written, one of them written (<c>feedback_two_arm_dispatch</c>). On a default
/// Windows console (cp437) the section sign therefore left the process as the single byte <c>0x15</c> — a
/// control character in UTF-8 and in every Windows ANSI code page — and the en dash was best-fitted to
/// <c>-</c>. Every citation in every CI log and every redirected capture was corrupted.
/// </para>
/// <para>
/// ⛔ NO GOLDEN COULD SEE IT, which is why this fact asserts BYTES and not text. The corpus runner matches
/// <c>.err</c> files on a diagnostic CODE substring (<c>COBOLNET0901</c>), which is pure ASCII; and the test
/// harness decodes every child capture as UTF-8 (<c>ProcessObserver</c>), so a mangled byte would come back as
/// a control character nobody asserts on. The observation has to be taken at the byte level, through a
/// REDIRECTED stream, which is what a user and a CI log actually read.
/// </para>
/// <para>
/// ⚠ AND THE PROBE ASSERTS ITS OWN PREMISE. A child launched with no console inherits .NET's UTF-8 default, so
/// a naive spawn would pass on the BROKEN build too — the shape the subject hides
/// (<c>feedback_probe_the_shape_the_subject_hides</c>). On Windows the CLI is therefore driven from a
/// <c>cmd.exe</c> that first sets its own console to cp437 (<c>CreateNoWindow</c> gives the child its own
/// console, so the developer's terminal is untouched — measured), and the test FAILS if that code page was not
/// actually in force. Elsewhere there is no console code page to force and the byte assertion stands alone.
/// </para>
/// </remarks>
public sealed class CliDiagnosticEncodingTests
{
    /// <summary>U+00A7 SECTION SIGN as UTF-8. In cp437 it is the single byte 0x15.</summary>
    private static readonly byte[] SectionSignUtf8 = [0xC2, 0xA7];

    /// <summary>U+2013 EN DASH as UTF-8. Best-fitted to '-' by every single-byte code page.</summary>
    private static readonly byte[] EnDashUtf8 = [0xE2, 0x80, 0x93];

    /// <summary>The built <c>cobol</c> driver, in the SAME configuration and framework this test assembly was
    /// built for — never a guess at "Debug", which would test a stale binary on a Release CI leg.</summary>
    private static string CliCommand()
    {
        // TestRepo.SrcBin resolves the SAME configuration this assembly was built in — a hard-coded "Debug"
        // would make a Release CI leg measure a stale binary.
        string host = TestRepo.SrcBin("Cobol.Net.Cli", OperatingSystem.IsWindows() ? "cobol.exe" : "cobol");
        if (File.Exists(host)) return host;

        string dll = TestRepo.SrcBin("Cobol.Net.Cli", "cobol.dll");
        Assert.True(File.Exists(dll),
            $"the cobol driver is not built at {Path.GetDirectoryName(dll)} — this assembly references "
            + "Cobol.Net.Cli, so building it builds the driver; a missing one means the observation cannot be "
            + "taken at all, which is not a pass.");
        return dll;
    }

    /// <summary>⛔ THE INVARIANT: a diagnostic's non-ASCII characters reach a redirected stream as UTF-8,
    /// whatever the console code page is.</summary>
    [Fact]
    public void ADiagnosticsCitation_ReachesRedirectedOutput_AsUtf8_UnderAnOemConsoleCodePage()
    {
        string work = Path.Combine(Path.GetTempPath(), "cobolnet-pb899-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        try
        {
            // INITIAL + RECURSIVE on one PROGRAM-ID is rejected by COBOLNET0886, whose message carries the
            // §-bearing citation. A unique PROGRAM-ID keeps .NET from serving a stale same-named assembly.
            string source = Path.Combine(work, "pb899enc.cob");
            File.WriteAllText(source,
                "       IDENTIFICATION DIVISION.\n"
                + "       PROGRAM-ID. PB899ENC IS INITIAL RECURSIVE PROGRAM.\n"
                + "       PROCEDURE DIVISION.\n"
                + "           STOP RUN.\n");

            string captured = Path.Combine(work, "diag.txt");
            string codePage = Path.Combine(work, "codepage.txt");
            string command = CliCommand();
            string invocation = command.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                ? $"dotnet \"{command}\""
                : $"\"{command}\"";

            ProcessStartInfo psi;
            if (OperatingSystem.IsWindows())
            {
                string script = Path.Combine(work, "probe.cmd");
                File.WriteAllText(script,
                    "@echo off\r\n"
                    + $"chcp 437 > \"{codePage}\"\r\n"
                    + $"{invocation} \"{source}\" --std 2023 > \"{captured}\" 2>&1\r\n");
                psi = new ProcessStartInfo("cmd.exe") { WorkingDirectory = work };
                psi.ArgumentList.Add("/c");
                psi.ArgumentList.Add(script);
            }
            else
            {
                string script = Path.Combine(work, "probe.sh");
                File.WriteAllText(script,
                    "#!/bin/sh\n"
                    + $"echo 'no console code page on this platform' > \"{codePage}\"\n"
                    + $"{invocation} \"{source}\" --std 2023 > \"{captured}\" 2>&1\n");
                psi = new ProcessStartInfo("/bin/sh") { WorkingDirectory = work };
                psi.ArgumentList.Add(script);
            }

            var r = ProcessObserver.ObserveOrThrow(psi);
            Assert.True(File.Exists(captured),
                $"the driver produced no capture at all.\nstdout:\n{r.Stdout}\nstderr:\n{r.Stderr}");

            if (OperatingSystem.IsWindows())
            {
                // The premise, measured: without cp437 in force this probe would pass on the broken build too.
                string cp = File.ReadAllText(codePage);
                Assert.True(cp.Contains("437", StringComparison.Ordinal),
                    "the probe could not put its own console into cp437, so it was NOT exercising the defect "
                    + $"and its green means nothing (feedback_green_gates_arent_evidence). chcp said: {cp}");
            }

            byte[] bytes = File.ReadAllBytes(captured);
            string decoded = Encoding.UTF8.GetString(bytes);
            Assert.True(decoded.Contains("COBOLNET0886", StringComparison.Ordinal),
                $"the probe did not produce the expected diagnostic, so nothing was measured: {decoded}");

            Assert.True(IndexOf(bytes, SectionSignUtf8) >= 0,
                "the diagnostic's section sign did not reach redirected output as UTF-8 (C2 A7). The CLI is "
                + "writing at the OS code page again — in cp437 U+00A7 is the single byte 0x15 (PB899).\n"
                + $"bytes: {Hex(bytes)}");
            Assert.True(IndexOf(bytes, EnDashUtf8) >= 0,
                "the diagnostic's en dash did not reach redirected output as UTF-8 (E2 80 93) — a single-byte "
                + $"code page best-fits it to '-' without error, which is the silent half of PB899.\nbytes: {Hex(bytes)}");
            Assert.DoesNotContain((byte)0x15, bytes);
        }
        finally
        {
            try { Directory.Delete(work, recursive: true); } catch (IOException) { /* best effort */ }
        }
    }

    private static int IndexOf(byte[] haystack, byte[] needle)
    {
        for (int i = 0; i + needle.Length <= haystack.Length; i++)
        {
            int j = 0;
            while (j < needle.Length && haystack[i + j] == needle[j]) j++;
            if (j == needle.Length) return i;
        }
        return -1;
    }

    private static string Hex(byte[] bytes) => Convert.ToHexString(bytes);
}
