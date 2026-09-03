// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Editions;

/// <summary>
/// The targeted ISO/IEC 1989 COBOL edition (85 / 2002 / 2014 / 2023, the CLI's <c>--std</c>) plus the
/// strict/permissive severity axis (the CLI's <c>--permissive</c>). This is the immutable-value half of the
/// old <c>EditionContext</c> — the SINGLE source of the dialect year (rearchitecture PHASE 02; it ends the
/// triple-sourcing of <c>DialectLevel</c> across the parser base, the frontend, and the compiler). It carries
/// no diagnostics: reporting is the separate <see cref="IDiagnosticSink"/> channel.
/// </summary>
/// <remarks>
/// Threaded by value (never a global): constructed once from the compiler options, read by the parser's base
/// class (the <c>{isXXXX()}?</c> gates ask <see cref="Has(int)"/>), by the frontend preprocessor gates, and by
/// the binder/validator. A <c>default(EditionInfo)</c> is <c>Year == 0</c>, which is invalid — always default
/// fields to <see cref="Latest"/> explicitly, and <see cref="Of(int, bool)"/> rejects any year outside
/// {85, 2002, 2014, 2023} (risk R5).
/// </remarks>
public readonly record struct EditionInfo(int Year, bool Permissive = false)
{
    /// <summary>The newest supported edition (the default target — VERSION_TEST_MATRIX_DESIGN: <c>--std</c>
    /// defaults to COBOL-2023).</summary>
    public static readonly EditionInfo Latest = new(2023);

    /// <summary>Construct a validated edition value. Throws when <paramref name="year"/> is not a known ISO
    /// edition — guards against a silently-defaulted <c>Year == 0</c>.</summary>
    public static EditionInfo Of(int year, bool permissive = false) => new(Validate(year), permissive);

    /// <summary>The fixed-point digit capacity of the targeted edition: 18 at COBOL-85, 31 at 2002+ (ISO
    /// §8.3.3.3.2 fixed-point literals 1–31 digits; the §14.7 composite-of-operands rules; PICTURE digit
    /// positions).</summary>
    public int MaxDigits => Year < 2002 ? 18 : 31;

    /// <summary>True when the targeted edition is at or after <paramref name="introducedIn"/> — the one
    /// predicate behind the grammar's <c>{isXXXX()}?</c> introduction gates and every version-varying bind
    /// decision.</summary>
    public bool Has(int introducedIn) => Year >= introducedIn;

    /// <summary>Every ISO edition this compiler targets, OLDEST first — the one list. <see cref="Validate"/>
    /// and every "was it so at an earlier edition?" walk read it, so a new edition is added in one place.</summary>
    public static ReadOnlySpan<int> All => [85, 2002, 2014, 2023];

    /// <summary>The editions STRICTLY OLDER than <paramref name="year"/>, oldest first.</summary>
    public static ReadOnlySpan<int> Before(int year)
    {
        var all = All;
        int n = 0;
        while (n < all.Length && all[n] < year) n++;
        return all[..n];
    }

    private static int Validate(int y)
    {
        foreach (int v in All) if (v == y) return y;
        throw new ArgumentOutOfRangeException(nameof(y), y, "edition must be 85/2002/2014/2023");
    }
}
