// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics;
using CobolNet;

namespace CobolNet.Cli;

/// <summary>
/// The <c>cobol</c> command-line driver: a thin shell over <see cref="CompilerDriver"/> — it parses arguments,
/// prints diagnostics, optionally runs the compiled program, and maps the compile outcome to a process exit code.
/// All compilation logic lives in the Cobol.Net.Compiler library (so it is testable without an exe).
/// </summary>
/// <remarks>
/// Usage: <c>cobol &lt;source.cob&gt; [-o out.dll] [--nist NAME] [--std 85|2002|2014|2023] [--permissive] [--copy DIR] [--run]</c>.
/// The generated C# is always written next to the output assembly (<c>&lt;name&gt;.g.cs</c>) so the translation is
/// directly inspectable.
/// </remarks>
internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help")
        {
            Console.Error.WriteLine(
                "cobol <source.cob> [-o out.dll] [--nist NAME] [--std 85|2002|2014|2023] [--permissive] [--copy DIR] [--run]");
            return args.Length == 0 ? 64 : 0;
        }

        var options = CliOptions.Parse(args);
        if (options is null) return 64;

        var result = CompilerDriver.Compile(new CompilerDriver.Options(
            options.SourcePath, options.OutputPath, options.NistTestName, options.DialectLevel, options.CopyPaths,
            options.Permissive));

        // Edition warnings (obsolete/archaic 0903 flags; removed constructs under --permissive) print to stderr
        // ALWAYS — success or failure — so migration users see them without a failing build (P2.1).
        foreach (string w in result.Warnings)
            Console.Error.WriteLine($"  {w}");

        if (!result.Success)
        {
            if (result.Status == CompilerDriver.Outcome.BackendError)
                Console.Error.WriteLine($"error: backend compilation failed (generated C# at {result.GeneratedCsPath}):");
            foreach (string e in result.Errors)
                Console.Error.WriteLine($"  {e}");

            // POSIX sysexits: 66 = no input, 65 = data/format error (source), 70 = internal software error (backend).
            return result.Status switch
            {
                CompilerDriver.Outcome.SourceNotFound => 66,
                CompilerDriver.Outcome.FrontendError => 65,
                CompilerDriver.Outcome.BackendError => 70,
                _ => 70,
            };
        }

        return options.Run ? Run(result.OutputDll) : 0;
    }

    /// <summary>Launch the compiled assembly via the shared <c>dotnet</c> host and forward its exit code.</summary>
    private static int Run(string dllPath)
    {
        var psi = new ProcessStartInfo("dotnet", $"\"{dllPath}\"") { UseShellExecute = false };
        using var proc = Process.Start(psi);
        proc!.WaitForExit();
        return proc.ExitCode;
    }
}
