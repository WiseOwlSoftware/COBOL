// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Editions;

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
    /// §2023:8.3.3.3.2 fixed-point literals 1–31 digits; the §2023:14.7 composite-of-operands rules; PICTURE digit
    /// positions).</summary>
    public int MaxDigits => Year < 2002 ? 18 : 31;

    /// <summary>⛔ WHETHER A data-name-n CLAUSE OPERAND MAY BE WRITTEN SUBSCRIPTED (kb/Work PB5602): COBOL-2002 reads data-name-n as "a
    /// user-defined word, including qualification and subscription if needed" (§2002:5.1.3), and its FILE STATUS, RECORD KEY,
    /// ALTERNATE RECORD KEY, RELATIVE KEY, COLLATING SEQUENCE and CONTROL clauses say only that the operand "may be qualified". The
    /// COBOL-2014 text prints the restriction (§2014:13.18.16.2 SR3 "Data-name-1 shall not be subscripted"), COBOL-2023's is "shall not
    /// be subject to any OCCURS clauses" (§2023:13.18.16.3 SR3), and COBOL-85 keeps its own reading, which the register leaves open.
    /// ONE predicate, asked by every clause that admits the subscript (docs/conformance/3/D-CLAUSE-SUBSCRIPT.md).</summary>
    public bool SubscriptedClauseOperands => Year == 2002;

    /// <summary>⛔ WHETHER AN ALPHABETIC ITEM STANDS WHEREVER AN ALPHANUMERIC OPERAND DOES (kb/Work PB5615): COBOL-2002 says "An alphabetic
    /// data item may be specified wherever an elementary alphanumeric data item is permitted as an operand. Where an alphabetic data
    /// item is not expressly permitted, the alphabetic data item is treated as though it were an alphanumeric data item" (§2002:8.5.2.1),
    /// so a rule worded "category alphanumeric" admits a PIC A item at that edition. COBOL-2014 withdrew it ("Data items of class and
    /// category alphabetic may be used only where rules explicitly allow them", §2014:E.2) and COBOL-2023 has no such sentence; COBOL-85
    /// has no general rule either. ONE predicate, asked through <c>ItemCategory.IsAlphanumericOrNational(Place, EditionInfo)</c> and
    /// the intrinsic argument class screen, never a per-site <c>Year == 2002</c>.</summary>
    public bool AlphabeticIsAlphanumeric => Year == 2002;

    /// <summary>⛔ WHETHER A CONTROL OPERAND MAY BE AN INDEX DATA ITEM (kb/Work PB5774): COBOL-85 provides for that control
    /// ("If the control data item is an index data item, the relation test is for the comparison of two index data items",
    /// §1985:XIII.3.7.4 GR3b) and limits explicit references to an index data item to Procedure Division statements
    /// (§1985:VI.5.14.3 SR5 read with §1985:IV.4.4.1), which a Report Section clause is not. COBOL-2023 makes a data-division
    /// clause naming a data item an explicit reference (§2023:8.4.5) and its SR10 list of the contexts of one
    /// (§2023:13.18.60.3) does not contain the CONTROL clause.</summary>
    public bool ControlOperandMayBeIndexDataItem => Year < 2002;

    /// <summary>⛔ WHETHER THE ROUNDED PHRASE OF A SUM CLAUSE NEEDS THE ENTRY'S COLUMN CLAUSE (kb/Work PB5644): COBOL-2014 states it
    /// ("The ROUNDED phrase may be specified in the SUM clause only if the COLUMN clause is specified for the subject of the entry",
    /// §2014:13.18.54.2 SR3) and COBOL-2023 keeps it (§2023:13.18.54.3 SR3). COBOL-2002's SUM clause prints the phrase in its general
    /// format (§2002:13.16.52.1) and its syntax rules (§2002:13.16.52.2) hold no such limit, so an entry with no COLUMN clause may
    /// write it. COBOL-85's SUM clause has no ROUNDED phrase.</summary>
    public bool SumRoundedRequiresColumn => Year >= 2014;

    /// <summary>⛔ THE ONE HOME OF THE EXCESS ARM OF THE DIGIT CAPACITY (kb/Work PB5529): the descriptor a count of
    /// <paramref name="digits"/> digit positions earns when it passes <see cref="MaxDigits"/> — more than 31 is
    /// <see cref="Diagnostics.DiagnosticCatalog.DigitCapacityOver31"/> at every edition, 19–31 at COBOL-85 is
    /// <see cref="Diagnostics.DiagnosticCatalog.DigitCapacityOver18Pre2002"/> — and null when the count fits. A PICTURE's digit
    /// positions (<c>EditionContext.CheckDigitCapacity</c>) and a numeric literal's digits (<c>LiteralScreen</c>) are two
    /// subjects of the one capacity, each wording its own message under the descriptor this returns.</summary>
    public Diagnostics.DiagnosticDescriptor? DigitCapacityExcess(int digits) =>
        digits <= MaxDigits ? null
        : digits > 31 ? Diagnostics.DiagnosticCatalog.DigitCapacityOver31
        : Diagnostics.DiagnosticCatalog.DigitCapacityOver18Pre2002;

    /// <summary>Where a REPLACE statement's effect ends (kb/Work PB5277): at the end of the separately compiled program at
    /// COBOL-85 (§1985:XII.3.4 GR3, "until the next occurrence of the statement or the end of the separately compiled
    /// program"), at the end of the compilation group from 2002 (§2002:7.1.3.3 GR5; §2023:7.2.4.4 GR5), a group of one or more
    /// programs.</summary>
    public bool ReplaceScopeEndsWithSeparatelyCompiledProgram => Year < 2002;

    /// <summary>Whether text a replacement introduces is laid out in the lines of the fixed reference format (kb/Work
    /// PB5382): at COBOL-85 every introduced line takes "the same character as the line on which the text being replaced
    /// begins" in its indicator area (§1985:XII.3.4 GR11), and a literal too long for one line is continued onto a
    /// continuation line, which a debugging line cannot be ("If replacement requires the continued literal to be continued
    /// on a debugging line, the program is in error", the same rule; §1985:XII.2.4 GR9 for COPY). From 2002 the rule is the
    /// debugging line alone — "If the text being replaced begins on a debugging line, the resultant text is on a debugging
    /// line" (§2002:7.1.3.3 GR11) — and no continued-literal error remains.</summary>
    public bool ReplacedTextIsLaidOutInFixedForm => Year < 2002;

    /// <summary>Whether a COPY statement's text-name and library-name are USER-DEFINED WORDS (kb/Work PB5482): at COBOL-85 they
    /// are, each in a disjoint set of its own, and "All user-defined words, except segment-numbers and level-numbers, can belong
    /// to one and only one of these disjoint sets" (§1985:IV.4.2.2.1.1), so a data-name, an index-name or any other
    /// user-defined word of the same source program shall not be spelled like one. From 2002 they are system-names, "instead of
    /// user-defined words, and do not have to be different from user-defined words" (§2002:F.2, item 135).</summary>
    public bool CopyNamesAreUserDefinedWords => Year < 2002;

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
