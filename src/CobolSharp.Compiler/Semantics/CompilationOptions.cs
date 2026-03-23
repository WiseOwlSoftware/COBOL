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

    /// <summary>True when targeting COBOL-2002 or later, where obsolete features are deleted.</summary>
    public bool IsCobol2002OrLater => Dialect >= DialectMode.Cobol2002;

    /// <summary>Display name for the current dialect (used in diagnostic messages).</summary>
    public string DialectName => Dialect switch
    {
        DialectMode.StrictCobol85 => "COBOL-85",
        DialectMode.Cobol2002 => "COBOL-2002",
        DialectMode.Cobol2014 => "COBOL-2014",
        DialectMode.Cobol2023 => "COBOL-2023",
        _ => "default"
    };
}
