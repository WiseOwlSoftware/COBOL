// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolSharp.Compiler.Semantics;

/// <summary>
/// COBOL dialect mode for feature gating.
/// </summary>
public enum DialectMode
{
    /// <summary>Default: permissive mode, accepts vendor extensions via genericClause.</summary>
    Default,
    /// <summary>Strict COBOL-85: rejects non-standard features.</summary>
    StrictCobol85,
    /// <summary>COBOL-2002+: allows BY VALUE, GLOBAL, extended intrinsics.</summary>
    Cobol2002,
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
}
