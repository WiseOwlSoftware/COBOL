// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// The <c>cobol</c> command run as a user runs it, with its standard output and standard error REDIRECTED to files and,
/// on Windows, under a console whose code page is the OEM one (cp437): the one place a test takes the bytes the command
/// puts on the wire (kb/Work PB899's <see cref="CliDiagnosticEncodingTests"/>, and kb/Work PB5941 review R1-1's
/// <c>--listing-output -</c>).
/// </summary>
/// <remarks>
/// An in-process run (<c>Program.Parse(...).Invoke()</c> under <c>Console.SetOut</c>) cannot see either thing these tests
/// hold: the command's standard output writer is the one <c>StandardStreams.EnsureUtf8</c> sets from <c>Main</c> (BOM-less
/// UTF-8 when redirected), which an in-process run never builds, and a redirected writer never re-encodes through a console
/// code page. A child launched with no console inherits .NET's UTF-8
/// default, so a naive spawn would pass on a build that writes through the code page; on Windows the command is therefore
/// driven from a <c>cmd.exe</c> that first sets its own console to cp437 (<c>CreateNoWindow</c> gives the child its own
/// console, so the developer's terminal is untouched), and <see cref="Run"/> FAILS if that code page was not in force.
/// Elsewhere there is no console code page to force.
/// </remarks>
internal static class OemConsoleCli
{
    /// <summary>What one run put on standard output and on standard error, as bytes.</summary>
    internal sealed record Capture(byte[] Output, byte[] Error);

    /// <summary>Run <see cref="TestRepo.CobolCommand"/> with <paramref name="arguments"/> in <paramref name="work"/>, a scratch
    /// directory the caller owns, and return the two redirected streams.</summary>
    public static Capture Run(string work, params string[] arguments)
    {
        string command = TestRepo.CobolCommand;
        Assert.True(File.Exists(command),
            $"the cobol command is not built at {command}: this assembly references WiseOwl.Cobol.Cli, so building it builds "
            + "the command; a missing one means nothing can be observed, which is not a pass.");

        string output = Path.Combine(work, "stdout.bin");
        string error = Path.Combine(work, "stderr.bin");
        string codePage = Path.Combine(work, "codepage.txt");
        string invocation = string.Join(' ', [Quoted(command), .. arguments.Select(Quoted)]);

        ProcessStartInfo psi;
        if (OperatingSystem.IsWindows())
        {
            string script = Path.Combine(work, "probe.cmd");
            File.WriteAllText(script,
                "@echo off\r\n"
                + $"chcp 437 > \"{codePage}\"\r\n"
                + $"{invocation} > \"{output}\" 2> \"{error}\"\r\n");
            psi = new ProcessStartInfo("cmd.exe") { WorkingDirectory = work };
            psi.ArgumentList.Add("/c");
            psi.ArgumentList.Add(script);
        }
        else
        {
            string script = Path.Combine(work, "probe.sh");
            File.WriteAllText(script, $"#!/bin/sh\n{invocation} > \"{output}\" 2> \"{error}\"\n");
            psi = new ProcessStartInfo("/bin/sh") { WorkingDirectory = work };
            psi.ArgumentList.Add(script);
        }

        var r = ProcessObserver.ObserveOrThrow(psi);
        Assert.True(File.Exists(output) && File.Exists(error),
            $"the command produced no capture at all.\nstdout:\n{r.Stdout}\nstderr:\n{r.Stderr}");

        if (OperatingSystem.IsWindows())
        {
            // The premise, measured: without cp437 in force a probe of the bytes would pass on a build that re-encodes.
            string cp = File.ReadAllText(codePage);
            Assert.True(cp.Contains("437", StringComparison.Ordinal),
                "the probe could not put its own console into cp437, so it was NOT exercising a code page and its green "
                + $"means nothing. chcp said: {cp}");
        }

        return new Capture(File.ReadAllBytes(output), File.ReadAllBytes(error));
    }

    private static string Quoted(string argument) => $"\"{argument}\"";
}
