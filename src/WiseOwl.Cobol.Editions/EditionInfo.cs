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

    /// <summary>⛔ THE LONGEST TEXT-WORD A COPY LIBRARY TEXT, A COPY REPLACING PSEUDO-TEXT OR A REPLACE PSEUDO-TEXT MAY HOLD
    /// (kb/Work PB5700): 322 character positions at COBOL-2002 (§2002:7.1.2.2 SR11 "shall be from 1 through 322 character
    /// positions"; §2002:7.1.3.2 SR9), 65,535 from 2014 (§2014:7.2.2.2 SR11; §2023:7.2.3.3 SR9). COBOL-85 only says "The implementor
    /// must allow a length from 1 through 322 characters" (§1985:XII.2.3 SR8), a floor that leaves the ceiling to the implementor,
    /// and WiseOwl COBOL allows the later editions' 65,535 there (inventory row 1985-SR-XII.2.3-8).</summary>
    public int MaxTextWordLength => Year == 2002 ? 322 : 65_535;

    /// <summary>Whether a text-word is a sequence of contiguous COBOL CHARACTERS (§1985:III.2 item 3 "Any other sequence of contiguous COBOL
    /// characters"; §2002:7.1.1.4 item 3 "any other sequence of contiguous COBOL characters bounded by separators") rather than, from 2014,
    /// "any other character or sequence of contiguous characters from the compile-time coded character set" (§2014:7.2.1.4 item 3): below
    /// 2014 a character outside the edition's COBOL character repertoire is in no text-word (kb/Work PB5608).</summary>
    public bool TextWordsAreCobolCharacters => Year < 2014;

    /// <summary>Whether the identifier operands of a COPY REPLACING phrase (text-1 and text-2) keep the subscript restriction of
    /// §2002:7.1.2.2 SR9 / §2014:7.2.2.2 SR9: "If subscripting is specified, it shall not include any arithmetic expressions as
    /// subscripts with the exception of a single literal or identifier, or an identifier plus or minus an integer". COBOL-2023 removed the
    /// operand form (Annex E.2), so its text holds no such rule; <c>--permissive</c> keeps accepting the removed form under the
    /// 2014 rule, as the word operand's SR10 does (kb/Work PB5085, PB5700).</summary>
    public bool ReplacingIdentifierSubscriptsRestricted => Year is 2002 or 2014 || (Year == 2023 && Permissive);

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

    /// <summary>Whether the edition HAS compiler directive lines (kb/Work PB5803): COBOL-2002 introduced them ("A compiler directive
    /// shall be preceded only by zero, one, or more space characters", §2002:7.2.2 SR2; §2023:7.3.3 SR2), and COBOL-85 has none, so
    /// there a line that begins <c>&gt;&gt;</c> is ordinary text: a sequence of contiguous COBOL characters bounded by separators, a
    /// text-word (§1985:III.2 item 3), which pseudo-text may hold (§1985:XII.3.3 SR3 / SR4).</summary>
    public bool HasCompilerDirectives => Year >= 2002;

    /// <summary>Whether a COPY statement's text-name and library-name are USER-DEFINED WORDS (kb/Work PB5482): at COBOL-85 they
    /// are, each in a disjoint set of its own, and "All user-defined words, except segment-numbers and level-numbers, can belong
    /// to one and only one of these disjoint sets" (§1985:IV.4.2.2.1.1), so a data-name, an index-name or any other
    /// user-defined word of the same source program shall not be spelled like one. From 2002 they are system-names, "instead of
    /// user-defined words, and do not have to be different from user-defined words" (§2002:F.2, item 135).</summary>
    public bool CopyNamesAreUserDefinedWords => Year < 2002;

    /// <summary>Whether condition-names, data-names and record-names are ONE disjoint set of user-defined words (kb/Work PB5972): at
    /// COBOL-85 the fourth of §1985:IV.4.2.2.1.1's disjoint sets is "condition-names, data-names, and record-names", whose duplicates
    /// qualification makes unique (§1985:IV.4.3.8.1 SRL2.1). From 2002 a condition-name is a type of user-defined word of its own,
    /// outside the exception that lets a data-name, a record-name and three other types share one word (§2002:8.3.1.1.1;
    /// §2023:8.3.2.2).</summary>
    public bool ConditionNamesShareTheDataNameSet => Year < 2002;

    /// <summary>Whether a UNARY arithmetic operator may be FOLLOWED by a left parenthesis with no separator space (kb/Work PB5989): from 2002
    /// "no space is required between a left parenthesis and a unary operator or between a unary operator and a left parenthesis"
    /// (§2002:8.7.1; §2023:8.7.1). COBOL-85 has no exception: its arithmetic operators, unary ones included, "must be preceded by a
    /// space and followed by a space" (§1985:VI.6.2.2). A left parenthesis directly BEFORE a unary operator (or a logical NOT) is accepted at
    /// every edition: the determination D-85-OPERATOR-SPACING (<c>docs/conformance/3/</c>) rules it from the 1985 text, which itself
    /// writes <c>(NOT (a = b))</c> (§1985:VI.6.3.3) and <c>(- argument-2)</c> (§1989:p.A-34).</summary>
    public bool UnaryOperatorMayTouchParenthesis => Year >= 2002;

    /// <summary>Whether the relational operators and the logical connectives of a condition each need a separator space on both sides
    /// (kb/Work PB5989): at COBOL-85 "A space must precede and follow each reserved word comprising the relational operator"
    /// (§1985:VI.6.3.1.1) and "The logical operators must be preceded by a space and followed by a space" (§1985:VI.6.3.2); no later
    /// edition states either sentence, so there a parenthesis or an operand's edge may touch them.</summary>
    public bool ConditionOperatorsNeedSeparatorSpaces => Year < 2002;

    /// <summary>The name of the standard this edition is, as its document is titled: what the source listing's page heading
    /// prints beside <c>--std</c> (DESIGN-listing.md section 6.1).</summary>
    public string StandardName => Year switch
    {
        85 => "ANSI X3.23-1985",
        2002 => "ISO/IEC 1989:2002",
        2014 => "ISO/IEC 1989:2014",
        2023 => "ISO/IEC 1989:2023",
        _ => throw new InvalidOperationException($"edition {Year} has no standard name"),
    };

    /// <summary>⛔ THE ONE CHOICE OF A RULE'S CITATION BY EDITION (kb/Work PB4950, the owner's decision of 2026-11-07: "a
    /// diagnostic cite the rule of the edition the program was compiled under"): <paramref name="y1985"/> at COBOL-85 and
    /// <paramref name="later"/> from COBOL-2002 on, each text written once. Where an edition's own text differs it overrides:
    /// <paramref name="y2002"/> at COBOL-2002 (and at COBOL-2014 unless <paramref name="y2014"/> is given), <paramref name="y2014"/>
    /// at COBOL-2014; <paramref name="later"/> is always COBOL-2023's. So a message sends the reader to the text of the edition they
    /// compile at. Every per-edition message asks it; no site spells its own <c>Year &lt; 2002 ? ... : ...</c> between texts
    /// (the build refuses one: EditionCiteAnalyzer in <c>src/WiseOwl.Cobol.Analyzers</c>, WOCA0001). Each citation is in the
    /// edition form of kb/Work PB4887 (<c>§1985:VI.6.22.3 SR1</c>), checked by <c>scripts/spec/audit_code_citations.py</c>.</summary>
    public string Cite(string y1985, string later, string? y2002 = null, string? y2014 = null) =>
        Year < 2002 ? y1985 : Year < 2014 ? y2002 ?? later : Year < 2023 ? y2014 ?? y2002 ?? later : later;

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
