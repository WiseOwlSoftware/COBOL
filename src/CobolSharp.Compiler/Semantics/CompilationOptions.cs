// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolSharp.Compiler.Semantics;

/// <summary>
/// COBOL dialect mode for feature gating.
/// </summary>
/// <summary>
/// COBOL dialect mode for feature gating.
/// Values are ordered so that numeric comparison works: Default &lt; StrictCobol85 &lt; Cobol2002 &lt; Cobol2014 &lt; Cobol2023.
/// </summary>
public enum DialectMode
{
    /// <summary>Default: permissive mode, accepts vendor extensions via genericClause.</summary>
    Default = 0,
    /// <summary>Strict COBOL-85: rejects non-standard features.</summary>
    StrictCobol85 = 85,
    /// <summary>COBOL-2002: allows BY VALUE, GLOBAL, extended intrinsics; deletes ALTER.</summary>
    Cobol2002 = 2002,
    /// <summary>COBOL-2014: additional features beyond 2002.</summary>
    Cobol2014 = 2014,
    /// <summary>COBOL-2023: current ISO standard.</summary>
    Cobol2023 = 2023,
}

/// <summary>
/// Compilation-level options that affect semantic analysis behavior.
/// </summary>
public sealed class CompilationOptions
{
    /// <summary>COBOL dialect mode for feature gating.</summary>
    public DialectMode Dialect { get; set; } = DialectMode.Default;

    /// <summary>When true, emit warnings for non-standard features even in Default mode.</summary>
    public bool WarnNonStandard { get; set; }

    /// <summary>
    /// Canonical per-version feature model resolved from <see cref="Dialect"/> — the single source of truth
    /// for every version-conditional decision. Ask the config (<c>Config.IsStrict</c>,
    /// <c>Config.IsCobol2002OrLater</c>, <c>Config.DisplayName</c>, <c>Config.SupportsFreeFormSource</c>, …)
    /// rather than re-deriving <c>Dialect &gt;= …</c> at call sites. See docs/MULTIVERSION_ROADMAP.md §3.
    /// </summary>
    public DialectConfig Config => DialectConfig.For(Dialect);

    /// <summary>
    /// Data-model migration kill-switch (<c>docs/RECORD_STRUCT_STORAGE_DESIGN.md</c>): when true, items the
    /// <see cref="RecordClassificationPass"/> marks typed are flipped to native .NET fields (S3: a standalone
    /// elementary alphanumeric item → a <see cref="string"/> field). Default OFF, so the whole existing test
    /// corpus stays byte-identical; a dedicated test sets it ON to exercise the typed path. Each flip-widening
    /// stage keeps the guard green with this OFF and the typed cells reached only via the flag.
    /// </summary>
    public bool EnableTypedFields { get; set; }
}
