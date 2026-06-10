// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Cli;

/// <summary>Parsed <c>cobol</c> command-line options.</summary>
/// <param name="SourcePath">The COBOL source file to compile.</param>
/// <param name="OutputPath">Output assembly path (<c>-o</c>); null = the source path with a <c>.dll</c> extension.</param>
/// <param name="NistTestName">NIST CCVS test name (<c>--nist</c>) enabling placeholder preprocessing.</param>
/// <param name="DialectLevel">ISO dialect year (<c>--std</c>): 85 / 2002 / 2014 / 2023.</param>
/// <param name="CopyPaths">COPY copybook search directories (<c>--copy</c>), in order.</param>
/// <param name="Run">Run the compiled assembly after a successful compile (<c>--run</c>).</param>
internal sealed record CliOptions(
    string SourcePath,
    string? OutputPath,
    string? NistTestName,
    int DialectLevel,
    IReadOnlyList<string> CopyPaths,
    bool Run)
{
    /// <summary>Parse <paramref name="args"/>, or return <see langword="null"/> on a usage error.</summary>
    public static CliOptions? Parse(string[] args)
    {
        string? source = null, output = null, nist = null;
        int std = 2023;        // no --std ⇒ the LATEST edition (owner decision, DEVLOG 519)
        bool stdGiven = false;
        var copy = new List<string>();
        bool run = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-o": output = Next(args, ref i); break;
                case "--nist": nist = Next(args, ref i); break;
                case "--copy": if (Next(args, ref i) is { } c) copy.Add(c); break;
                case "--run": run = true; break;
                case "--std":
                    stdGiven = true;
                    if (!int.TryParse(Next(args, ref i), out std)) { Console.Error.WriteLine("error: --std expects a year"); return null; }
                    break;
                default:
                    if (args[i].StartsWith('-')) { Console.Error.WriteLine($"error: unknown option {args[i]}"); return null; }
                    source = args[i];
                    break;
            }
        }

        if (source is null) { Console.Error.WriteLine("error: no source file"); return null; }
        // The NIST CCVS corpus is COBOL-85: --nist without an explicit --std targets 85, not the 2023 default
        // (else a CCVS program hits new-2023 reserved words / removed constructs). DEVLOG 519.
        if (nist is not null && !stdGiven) std = 85;
        return new CliOptions(source, output, nist, std, copy, run);

        static string? Next(string[] a, ref int i) => ++i < a.Length ? a[i] : null;
    }
}
