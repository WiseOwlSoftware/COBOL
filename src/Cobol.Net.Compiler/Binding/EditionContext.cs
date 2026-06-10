// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding;

/// <summary>
/// The per-compilation EDITION context — the bind-side half of the four-compilers-in-one mission: the targeted
/// ISO edition (85 / 2002 / 2014 / 2023, the CLI's <c>--std</c>) and the bind-time diagnostics that REJECT a
/// program whose constructs the targeted edition lacks (introduction gating) or forbids (capacity rules). This is
/// the seam the EditionValidator grows on (VERSION_TEST_MATRIX_DESIGN Phase 2): every edition-varying bind
/// decision consults THIS object, never a global. Diagnostics use the <c>COBOLNET08xx</c> band (edition gating;
/// §14.10 of the SSOT assigns 07xx to exception conditions).
/// </summary>
public sealed class EditionContext(int dialectLevel)
{
    /// <summary>The targeted ISO edition year (85 / 2002 / 2014 / 2023).</summary>
    public int DialectLevel { get; } = dialectLevel;

    /// <summary>Bind-time rejection diagnostics. Any entry fails the compilation (CompilerDriver → BindError).</summary>
    public List<string> Diagnostics { get; } = [];

    /// <summary>The fixed-point digit capacity of the targeted edition: 18 at COBOL-85, 31 at 2002+ (ISO
    /// §8.3.1.2 fixed-point literals 1–31 digits; the §14.7 composite-of-operands rules; PICTURE digit positions).</summary>
    public int MaxDigits => DialectLevel < 2002 ? 18 : 31;

    /// <summary>Record an edition-gating error (fails the compile).</summary>
    public void Error(string code, string message) => Diagnostics.Add($"error {code}: {message}");

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
