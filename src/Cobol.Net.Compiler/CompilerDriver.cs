// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.CodeGen;
using CobolNet.Frontend.Diagnostics;
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
    /// <param name="DialectLevel">ISO dialect year the parser admits (85 / 2002 / 2014 / 2023). DEFAULTS to the LATEST
    /// edition (2023) when unspecified — an unflagged compile targets the newest standard (owner decision, DEVLOG 519;
    /// docs/VERSION_TEST_MATRIX_DESIGN.md §10 #2). Callers that target a specific edition (the NIST harness at 85, the
    /// differential harness, per-edition conformance) pass it explicitly, so the default flip does not affect them.</param>
    /// <param name="CopyPaths">Directories searched for COPY copybooks, in order.</param>
    /// <param name="Permissive">The strict/permissive severity axis (CLI <c>--permissive</c>, orthogonal to
    /// <paramref name="DialectLevel"/>): permissive accepts constructs the targeted edition REMOVED, emitting a
    /// warning and the pre-removal semantics (the documented migration mode, VERSION_TEST_MATRIX_DESIGN §10 #1).
    /// Strict (the default for every named <c>--std</c>) rejects them. Introduction gating is unaffected.</param>
    /// <param name="CheckOnly">Parse + bind + the version-conformance pass ONLY — do NOT run the Roslyn C#→IL
    /// backend and write no <c>.dll</c>/<c>.g.cs</c>. Every edition-gating diagnostic (the "does this compile at
    /// edition X" question the INV-1 continuity sweep asks) is produced in Phase 1/2, BEFORE the backend, so a check-only
    /// compile is verdict-equivalent to a full one for that question while skipping the backend — the dominant
    /// cost. Backend (C#-type) errors are NOT surfaced (they are not an edition-continuity concern).</param>
    public sealed record Options(
        string SourcePath,
        string? OutputPath = null,
        string? NistTestName = null,
        int DialectLevel = 2023,
        IReadOnlyList<string>? CopyPaths = null,
        bool Permissive = false,
        bool CheckOnly = false);

    /// <summary>Which phase a compilation reached (drives the CLI's exit code).</summary>
    public enum Outcome { Success, SourceNotFound, FrontendError, BindError, BackendError }

    /// <summary>The result of a compilation.</summary>
    /// <param name="Status">The phase reached / the failure category.</param>
    /// <param name="OutputDll">The output assembly path (set once an output location is known).</param>
    /// <param name="GeneratedCsPath">The emitted <c>.g.cs</c> path (set once C# is generated).</param>
    /// <param name="Errors">Human-readable diagnostics for a non-success outcome.</param>
    /// <param name="Warnings">Non-failing edition diagnostics (obsolete/archaic 0903 flags; removed constructs
    /// under <see cref="Options.Permissive"/>). Present on EVERY outcome, success included — the CLI prints them
    /// to stderr always (P2.1).</param>
    public sealed record Result(
        Outcome Status,
        string OutputDll,
        string? GeneratedCsPath,
        IReadOnlyList<string> Errors,
        IReadOnlyList<string> Warnings)
    {
        /// <summary>True iff a runnable assembly was produced.</summary>
        public bool Success => Status == Outcome.Success;
    }

    /// <summary>Compile <paramref name="options"/> to a runnable console assembly.</summary>
    public static Result Compile(Options options)
    {
        if (!File.Exists(options.SourcePath))
            return new Result(Outcome.SourceNotFound, "", null, [$"source file not found: {options.SourcePath}"], []);

        // Phase 1 — front-end: preprocess + parse.
        var diagnostics = new DiagnosticBag();
        var frontend = new Frontend.Frontend
        {
            NistTestName = options.NistTestName,
            DialectLevel = options.DialectLevel,
            // The preprocessor-level removal gates (VCR 2/4, W3 — DEVLOG 598) honor the same severity axis.
            Permissive = options.Permissive,
        };
        foreach (string dir in options.CopyPaths ?? [])
            frontend.AddCopySearchPath(dir);
        // The implementor-defined DEFAULT COBOL LIBRARY (ISO §7.2.3.4 GR3 — "the implementor defines the
        // mechanism for identifying the default COBOL library"): in NIST mode it is the `copylib/` directory
        // sibling to the source's directory — the convention the legacy CLI auto-discovers
        // (CobolSharp.CLI/Program.cs::RunCompile), so the SM COPY suite resolves its K*/KP*/ALTLB texts.
        // Appended AFTER caller-supplied paths so an explicit --copy outranks the convention.
        if (options.NistTestName is not null
            && Path.GetDirectoryName(Path.GetFullPath(options.SourcePath)) is { } srcDir
            && Path.GetFullPath(Path.Combine(srcDir, "..", "copylib")) is { } copylib
            && Directory.Exists(copylib))
            frontend.AddCopySearchPath(copylib);

        var tree = frontend.Parse(options.SourcePath, diagnostics);
        // Frontend WARNINGS (the preprocessor 0902-permissive/0903 gates) ride Result.Warnings on every
        // outcome, merged ahead of the edition channel's — same contract as edition warnings.
        var feWarnings = diagnostics.Diagnostics.Where(d => !d.IsError).Select(d => d.ToString()!).ToList();
        if (tree is null || diagnostics.HasErrors)
            return new Result(Outcome.FrontendError, "", null,
                diagnostics.Diagnostics.Where(d => d.IsError).Select(d => d.ToString()!).ToList(), feWarnings);

        // Phase 2 — bind under the targeted EDITION, then run the ONE version-conformance pass, then emit
        // typed-native C#. Edition-gating diagnostics (the four-compilers rule: a construct the targeted edition
        // lacks or forbids REJECTS the program) are semantic errors, not runtime guards; they fail the compile
        // here, BEFORE emit — a removed or not-yet-introduced construct may have no emit path at all.
        var edition = new Binding.EditionContext(options.DialectLevel, options.Permissive);
        // P3 step 14a — the bind/emit SPLIT: bind the whole run unit FIRST (the binder is edition-AGNOSTIC — it
        // raises no edition diagnostic; the render half touches no EditionContext). frontend.TurnEvents are the
        // >>TURN directive events (ISO §7.3.25) that build the group's compile-time TurnState (deep-dive D10).
        var emitter = new CSharpEmitter();
        var bound = emitter.Bind(tree, edition, frontend.TurnEvents);
        // P3 step 14 — the ONE post-bind edition-conformance funnel over the bound run unit (the SOLE gate): its
        // parse-tree arm fires the syntactic introduction/removal/phrase gates + the §8.9 reserved-word funnel on
        // RECOGNITION (it absorbed EditionValidator, step 14h — so a below-edition construct that ALSO fails to
        // bind still names its edition), and its bound-tree arm fires the genuinely-semantic gates. Runs for BOTH
        // a full compile and a CheckOnly verdict — the edition band codes come from here, so a bind-only verdict
        // would silently drop every edition diagnostic. Codegen never runs on an errored tree (exit criterion 9).
        Validation.VersionConformancePass.Run(bound, edition.Edition, edition);
        if (edition.Diagnostics.Count > 0)
            return new Result(Outcome.BindError, "", null, edition.Diagnostics, [.. feWarnings, .. edition.Warnings]);

        // Check-only: every edition-gating diagnostic is now produced (parse + bind + the conformance pass above),
        // so the compile VERDICT is settled. Skip emit AND Phase 3 (the Roslyn C#→IL backend + the dll/g.cs writes) —
        // the dominant cost — since no runnable assembly is wanted (the INV-1 continuity sweep / CLI `check-batch`).
        if (options.CheckOnly)
            return new Result(Outcome.Success, "", null, [], [.. feWarnings, .. edition.Warnings]);

        string csharp = emitter.EmitBound(bound);

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
                    .ToList(), [.. feWarnings, .. edition.Warnings]);

        return new Result(Outcome.Success, outputDll, csPath, [], [.. feWarnings, .. edition.Warnings]);
    }
}
