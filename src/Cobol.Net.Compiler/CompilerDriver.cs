// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.CodeGen;
using CobolSharp.Compiler.Diagnostics;
using Microsoft.CodeAnalysis;

namespace CobolNet;

/// <summary>
/// Drives the COBOL.NET compile pipeline end-to-end: COBOL source → typed-native C# → a runnable .NET assembly.
/// This is the library entry point the CLI (<c>Cobol.Net.Cli</c>) and the tests call; it owns no console I/O and
/// no process control, returning a structured <see cref="Result"/> the caller maps to exit codes / assertions.
/// </summary>
public static class CompilerDriver
{
    /// <summary>Inputs to a single compilation.</summary>
    /// <param name="SourcePath">Path to the COBOL source file.</param>
    /// <param name="OutputPath">Output assembly path; defaults to the source path with a <c>.dll</c> extension.</param>
    /// <param name="NistTestName">When set, enables NIST CCVS placeholder preprocessing for that test.</param>
    /// <param name="DialectLevel">ISO dialect year the parser admits (85 / 2002 / 2014 / 2023).</param>
    /// <param name="CopyPaths">Directories searched for COPY copybooks, in order.</param>
    public sealed record Options(
        string SourcePath,
        string? OutputPath = null,
        string? NistTestName = null,
        int DialectLevel = 85,
        IReadOnlyList<string>? CopyPaths = null);

    /// <summary>Which phase a compilation reached (drives the CLI's exit code).</summary>
    public enum Outcome { Success, SourceNotFound, FrontendError, BackendError }

    /// <summary>The result of a compilation.</summary>
    /// <param name="Status">The phase reached / the failure category.</param>
    /// <param name="OutputDll">The output assembly path (set once an output location is known).</param>
    /// <param name="GeneratedCsPath">The emitted <c>.g.cs</c> path (set once C# is generated).</param>
    /// <param name="Errors">Human-readable diagnostics for a non-success outcome.</param>
    public sealed record Result(
        Outcome Status,
        string OutputDll,
        string? GeneratedCsPath,
        IReadOnlyList<string> Errors)
    {
        /// <summary>True iff a runnable assembly was produced.</summary>
        public bool Success => Status == Outcome.Success;
    }

    /// <summary>Compile <paramref name="options"/> to a runnable console assembly.</summary>
    public static Result Compile(Options options)
    {
        if (!File.Exists(options.SourcePath))
            return new Result(Outcome.SourceNotFound, "", null, [$"source file not found: {options.SourcePath}"]);

        // Phase 1 — front-end: preprocess + parse.
        var diagnostics = new DiagnosticBag();
        var frontend = new Frontend.Frontend { NistTestName = options.NistTestName, DialectLevel = options.DialectLevel };
        foreach (string dir in options.CopyPaths ?? [])
            frontend.AddCopySearchPath(dir);

        var tree = frontend.Parse(options.SourcePath, diagnostics);
        if (tree is null || diagnostics.HasErrors)
            return new Result(Outcome.FrontendError, "", null,
                diagnostics.Diagnostics.Select(d => d.ToString()!).ToList());

        // Phase 2 — emit typed-native C#.
        string csharp = new CSharpEmitter().Emit(tree);

        string outputDll = options.OutputPath ?? Path.ChangeExtension(options.SourcePath, ".dll");
        string outDir = Path.GetDirectoryName(Path.GetFullPath(outputDll)) is { Length: > 0 } d ? d : ".";
        Directory.CreateDirectory(outDir);
        string csPath = Path.ChangeExtension(outputDll, ".g.cs");
        File.WriteAllText(csPath, csharp);

        // Phase 3 — compile the generated C# with Roslyn.
        string assemblyName = Path.GetFileNameWithoutExtension(outputDll);
        var backend = RoslynBackend.Compile(csharp, outputDll, assemblyName);
        if (!backend.Success)
            return new Result(Outcome.BackendError, outputDll, csPath,
                backend.Diagnostics
                    .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                    .Select(d => d.ToString())
                    .ToList());

        return new Result(Outcome.Success, outputDll, csPath, []);
    }
}
