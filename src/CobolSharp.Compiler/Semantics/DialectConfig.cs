// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolSharp.Compiler.Semantics;

/// <summary>
/// Canonical, per-version feature model resolved from <see cref="DialectMode"/>. This is the single source of
/// truth for every version-conditional decision in the compiler: callers ask the config
/// (<c>Config.IsStrict</c>, <c>Config.IsCobol2002OrLater</c>, <c>Config.SupportsFreeFormSource</c>, …) instead
/// of re-deriving <c>Dialect &gt;= …</c> at each site. One canonical dispatch — see
/// docs/MULTIVERSION_ROADMAP.md §3 (M0 — version engine).
///
/// Instances are immutable and cached per <see cref="DialectMode"/> (see <see cref="For"/>). The forward
/// feature flags (COBOL-2002+/2014+) are the foundation the M2–M4 workstreams build on; the removed-after-'85
/// policy is what M1's WS-DIALECT extends into per-construct flagging.
/// </summary>
public sealed class DialectConfig
{
    private DialectConfig(DialectMode version) => Version = version;

    /// <summary>The selected standard.</summary>
    public DialectMode Version { get; }

    // ── Strictness axis ──────────────────────────────────────────────────────────────────────
    /// <summary>
    /// True for every named standard (COBOL-85 and later); false only for the permissive <see
    /// cref="DialectMode.Default"/>. Strict modes reject the CCVS grammar leniencies (see
    /// docs/dialect-strictness.md).
    /// </summary>
    public bool IsStrict => Version != DialectMode.Default;

    // ── Version thresholds ───────────────────────────────────────────────────────────────────
    /// <summary>True when targeting COBOL-2002 or later (where '85-obsolete features are deleted).</summary>
    public bool IsCobol2002OrLater => Version >= DialectMode.Cobol2002;
    /// <summary>True when targeting COBOL-2014 or later.</summary>
    public bool IsCobol2014OrLater => Version >= DialectMode.Cobol2014;
    /// <summary>True when targeting COBOL-2023 or later.</summary>
    public bool IsCobol2023OrLater => Version >= DialectMode.Cobol2023;

    /// <summary>
    /// Numeric standard level driving the ANTLR parser dialect predicates (<c>is85()/is2002()/…</c>). The
    /// permissive <see cref="DialectMode.Default"/> parses as the '85 level (it accepts the superset and the
    /// semantic phase applies leniencies).
    /// </summary>
    public int ParserLevel => Version == DialectMode.Default ? 85 : (int)Version;

    /// <summary>Display name for diagnostic messages.</summary>
    public string DisplayName => Version switch
    {
        DialectMode.StrictCobol85 => "COBOL-85",
        DialectMode.Cobol2002 => "COBOL-2002",
        DialectMode.Cobol2014 => "COBOL-2014",
        DialectMode.Cobol2023 => "COBOL-2023",
        _ => "default",
    };

    // ── Removed-after-'85 policy ─────────────────────────────────────────────────────────────
    /// <summary>
    /// True when features REMOVED after COBOL-85 (ALTER, bare GO TO, USE FOR DEBUGGING, segmentation,
    /// communication, …) must be flagged as removed. They are accepted under <c>cobol85</c>/Default and
    /// flagged from COBOL-2002 onward. WS-DIALECT extends this into a per-construct policy.
    /// </summary>
    public bool FlagsFeaturesRemovedAfter85 => IsCobol2002OrLater;

    // ── Forward feature availability (M2–M4 foundation) ──────────────────────────────────────
    /// <summary>Free-form reference format is available from COBOL-2002.</summary>
    public bool SupportsFreeFormSource => IsCobol2002OrLater;
    /// <summary>Compiler directives (<c>&gt;&gt;</c>) + conditional compilation, from COBOL-2002.</summary>
    public bool SupportsCompilerDirectives => IsCobol2002OrLater;
    /// <summary>User-defined functions (<c>FUNCTION-ID</c>), from COBOL-2002.</summary>
    public bool SupportsUserDefinedFunctions => IsCobol2002OrLater;
    /// <summary>Object orientation (<c>CLASS-ID/METHOD-ID/INVOKE</c>), from COBOL-2002.</summary>
    public bool SupportsObjectOrientation => IsCobol2002OrLater;
    /// <summary>National character data (<c>USAGE NATIONAL</c>), from COBOL-2002.</summary>
    public bool SupportsNationalData => IsCobol2002OrLater;
    /// <summary>Bit and boolean data, from COBOL-2002.</summary>
    public bool SupportsBitAndBooleanData => IsCobol2002OrLater;
    /// <summary>Pointer data + based addressing, from COBOL-2002.</summary>
    public bool SupportsPointers => IsCobol2002OrLater;
    /// <summary>The <c>VALIDATE</c> data-validation facility, from COBOL-2002.</summary>
    public bool SupportsValidate => IsCobol2002OrLater;
    /// <summary>Dynamic-capacity tables (<c>OCCURS DYNAMIC CAPACITY</c>), from COBOL-2014.</summary>
    public bool SupportsDynamicTables => IsCobol2014OrLater;
    /// <summary>Type declarations (<c>TYPEDEF</c> / <c>SAME AS</c>), from COBOL-2014.</summary>
    public bool SupportsTypedef => IsCobol2014OrLater;

    // ── Resolution / caching ─────────────────────────────────────────────────────────────────
    private static readonly DialectConfig s_default = new(DialectMode.Default);
    private static readonly DialectConfig s_cobol85 = new(DialectMode.StrictCobol85);
    private static readonly DialectConfig s_cobol2002 = new(DialectMode.Cobol2002);
    private static readonly DialectConfig s_cobol2014 = new(DialectMode.Cobol2014);
    private static readonly DialectConfig s_cobol2023 = new(DialectMode.Cobol2023);

    /// <summary>Resolve the canonical config for a dialect mode (cached, immutable singletons).</summary>
    public static DialectConfig For(DialectMode version) => version switch
    {
        DialectMode.StrictCobol85 => s_cobol85,
        DialectMode.Cobol2002 => s_cobol2002,
        DialectMode.Cobol2014 => s_cobol2014,
        DialectMode.Cobol2023 => s_cobol2023,
        _ => s_default,
    };
}
