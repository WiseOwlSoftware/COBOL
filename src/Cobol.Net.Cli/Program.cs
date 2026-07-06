// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.CommandLine;
using System.Diagnostics;
using CobolNet;

namespace CobolNet.Cli;

/// <summary>
/// The <c>cobol</c> command-line driver: a thin shell over <see cref="CompilerDriver"/> — it parses arguments
/// (via <c>System.CommandLine</c>, Microsoft's first-party parser), prints diagnostics, optionally runs the
/// compiled program, and maps the compile outcome to a process exit code. All compilation logic lives in the
/// Cobol.Net.Compiler library (so it is testable without an exe).
/// </summary>
/// <remarks>
/// Usage: <c>cobol &lt;source.cob&gt; [-o out.dll] [--nist [NAME]] [--std 85|2002|2014|2023] [--permissive]
/// [--copy DIR] [--run]</c>. Every value option accepts BOTH <c>--opt value</c> and <c>--opt=value</c>; no
/// option can swallow a following flag. The generated C# is written next to the output assembly
/// (<c>&lt;name&gt;.g.cs</c>) so the translation is directly inspectable.
/// </remarks>
internal static class Program
{
    private static int Main(string[] args)
    {
        var (root, resolve) = BuildParser();
        root.SetAction(parse => RunCompile(resolve(parse)));
        return root.Parse(args).Invoke();
    }

    /// <summary>Build the argument parser (exposed for unit testing the grammar without spawning a process):
    /// the configured <see cref="RootCommand"/> plus a pure resolver that maps a <see cref="ParseResult"/> to the
    /// <see cref="CliOptions"/> DTO (the <c>--nist</c>/<c>--std</c> defaulting is here, testable in isolation).</summary>
    internal static (RootCommand Command, Func<ParseResult, CliOptions> Resolve) BuildParser()
    {
        var sourceArgument = new Argument<string>("source")
        {
            Description = "The COBOL source file to compile.",
        };
        var outputOption = new Option<string?>("-o", "--output")
        {
            Description = "Output assembly path (default: the source path with a .dll extension).",
            HelpName = "out.dll",
        };
        var nistOption = new Option<string?>("--nist")
        {
            Description = "Enable NIST CCVS placeholder preprocessing; the optional test name defaults to the "
                + "source file's base name.",
            Arity = ArgumentArity.ZeroOrOne,   // the name is optional — `--nist` alone is legal
            HelpName = "NAME",
        };
        var stdOption = new Option<int?>("--std")
        {
            Description = "Target ISO edition: 85, 2002, 2014, or 2023 (default: 2023, or 85 with --nist).",
            HelpName = "YEAR",
        };
        stdOption.Validators.Add(result =>
        {
            if (result.GetValueOrDefault<int?>() is { } y and not (85 or 2002 or 2014 or 2023))
                result.AddError($"--std must be one of 85, 2002, 2014, 2023 (got {y}).");
        });
        var permissiveOption = new Option<bool>("--permissive")
        {
            Description = "Accept constructs the targeted edition removed, warning instead of rejecting (the "
                + "migration mode; orthogonal to --std/--nist).",
        };
        var copyOption = new Option<string[]>("--copy")
        {
            Description = "A COPY copybook search directory (repeatable).",
            Arity = ArgumentArity.OneOrMore,
            AllowMultipleArgumentsPerToken = false,   // `--copy A --copy B`, never grabbing the source positional
            HelpName = "DIR",
        };
        var runOption = new Option<bool>("--run")
        {
            Description = "Run the compiled assembly after a successful compile.",
        };

        var root = new RootCommand("cobol — translate a COBOL source unit to typed-native .NET (the Roslyn backend).")
        {
            sourceArgument, outputOption, nistOption, stdOption, permissiveOption, copyOption, runOption,
        };

        CliOptions Resolve(ParseResult parse)
        {
            string source = parse.GetValue(sourceArgument)!;
            // The NIST test name is OPTIONAL: `--nist` present means NIST mode (GetResult is non-null even with
            // no value); an absent name is derived from the source file's base name (the CCVS convention).
            bool nistEnabled = parse.GetResult(nistOption) is not null;
            string? nistName = nistEnabled
                ? parse.GetValue(nistOption) ?? Path.GetFileNameWithoutExtension(source)
                : null;
            // --std wins when given; else the NIST CCVS corpus is COBOL-85, otherwise the 2023 default (DEVLOG 519).
            int std = parse.GetValue(stdOption) ?? (nistEnabled ? 85 : 2023);

            return new CliOptions(
                source, parse.GetValue(outputOption), nistName, std,
                parse.GetValue(copyOption) ?? [], parse.GetValue(runOption), parse.GetValue(permissiveOption));
        }
        return (root, Resolve);
    }

    /// <summary>Compile per the resolved options, print diagnostics, and map the outcome to a POSIX exit code.</summary>
    private static int RunCompile(CliOptions options)
    {
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
