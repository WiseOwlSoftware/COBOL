// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding;

/// <summary>
/// The per-compilation EDITION context — the bind-side half of the four-compilers-in-one mission: the targeted
/// ISO edition (85 / 2002 / 2014 / 2023, the CLI's <c>--std</c>), the strict/permissive severity axis (the CLI's
/// <c>--permissive</c> — VERSION_TEST_MATRIX_DESIGN §10 #1: strict is the DEFAULT for every named edition;
/// permissive is the documented migration mode), and the diagnostics that REJECT or FLAG a program whose
/// constructs the targeted edition lacks (introduction gating, COBOLNET0900), forbids (capacity rules, 08xx),
/// reserves (§8.9 word gating, 0901), removed (0902), or obsoleted (0903). This is the seam the EditionValidator
/// and every edition-varying bind decision consult — never a global.
/// </summary>
/// <remarks>
/// Two channels, one policy seam (VERSION_TEST_MATRIX_DESIGN "Phase-2 implementation plan" P2.1):
/// <list type="bullet">
/// <item><see cref="Diagnostics"/> is ERRORS-ONLY — <see cref="CompilerDriver"/> fails the compile on ANY entry,
/// so nothing below error severity may ever be appended there.</item>
/// <item><see cref="Warnings"/> is the non-failing channel (obsolete/archaic 0903 flags; removed constructs under
/// permissive); <see cref="Warning"/> is its only writer. The driver surfaces it on every
/// <see cref="CompilerDriver.Result"/>, success or not.</item>
/// <item><see cref="Removed"/> is the ONE severity decision for removed-construct gating: error when strict,
/// warning when permissive — one policy, several emit sites (feedback_singular_pattern).</item>
/// </list>
/// </remarks>
public sealed class EditionContext(int dialectLevel, bool permissive = false)
{
    /// <summary>The targeted ISO edition year (85 / 2002 / 2014 / 2023).</summary>
    public int DialectLevel { get; } = dialectLevel;

    /// <summary>The severity axis (VERSION_TEST_MATRIX_DESIGN §10 #1): strict (default) rejects removed
    /// constructs; permissive accepts them with a warning and the pre-removal semantics (the migration mode).
    /// Introduction gating (a construct NEWER than the edition, 0900) is an error on BOTH axes.</summary>
    public bool Permissive { get; } = permissive;

    /// <summary>Bind-time rejection diagnostics. Any entry fails the compilation (CompilerDriver → BindError).</summary>
    public List<string> Diagnostics { get; } = [];

    /// <summary>Non-failing edition diagnostics (obsolete/archaic flags; removed constructs under
    /// <see cref="Permissive"/>). Printed to stderr by the CLI, carried on <see cref="CompilerDriver.Result"/>;
    /// never fails a compile. <see cref="Warning"/> is the only writer.</summary>
    public List<string> Warnings { get; } = [];

    /// <summary>True when any failing diagnostic has been recorded (the CompilerDriver fail-fast gate).</summary>
    public bool HasErrors => Diagnostics.Count > 0;

    /// <summary>The fixed-point digit capacity of the targeted edition: 18 at COBOL-85, 31 at 2002+ (ISO
    /// §8.3.1.2 fixed-point literals 1–31 digits; the §14.7 composite-of-operands rules; PICTURE digit positions).</summary>
    public int MaxDigits => DialectLevel < 2002 ? 18 : 31;

    /// <summary>Record an edition-gating error (fails the compile).</summary>
    public void Error(string code, string message) => Diagnostics.Add($"error {code}: {message}");

    /// <summary>Record a non-failing edition diagnostic (the 0903 obsolete/archaic flags; removed constructs
    /// under <see cref="Permissive"/> via <see cref="Removed"/>).</summary>
    public void Warning(string code, string message) => Warnings.Add($"warning {code}: {message}");

    /// <summary>THE severity seam for removed-construct gating (P2.1): a construct the targeted edition REMOVED
    /// is an error under strict, a warning (with the pre-removal semantics preserved) under
    /// <see cref="Permissive"/>. Every removed/reserved-word emit site routes its severity decision through
    /// here — never a local strictness test.</summary>
    public void Removed(string code, string message)
    {
        if (Permissive) Warning(code, message);
        else Error(code, message);
    }

    /// <summary>Check a fixed-point digit-position count against the edition cap (ISO §8.3.1.2 / §13.18.40):
    /// 19–31 digits require COBOL-2002+; more than 31 is invalid at every edition.</summary>
    public void CheckDigitCapacity(int digits, string what)
    {
        if (digits <= MaxDigits) return;
        if (digits > 31)
            Error("COBOLNET0801", $"{what} has {digits} digit positions; ISO/IEC 1989 limits fixed-point items "
                + "and literals to 31 digits (ISO §8.3.1.2)");
        else
            Error("COBOLNET0802", $"{what} has {digits} digit positions; COBOL-85 limits fixed-point items and "
                + $"literals to 18 digits — 19–31 digits require --std 2002 or later (targeting COBOL-{DialectLevel})");
    }
}
