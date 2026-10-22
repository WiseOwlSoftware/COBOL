// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions.Diagnostics;

namespace CobolNet.Binding.Model;

/// <summary>The optional phrases the USAGE clause's general format (ISO §13.18.60.2) prints AFTER a usage word, on
/// some of its lines and not others. The clause's other operands — <c>POINTER [ TO type-name-1 ]</c>, the two
/// prototype pointers' <c>TO</c> phrases and OBJECT REFERENCE's class phrase — belong to one usage word each and
/// are written inside that word's own grammar rule, so they cannot appear on the wrong usage and are not here.
/// <para>The grammar parses these four after EVERY usage word (<c>usageClause : (USAGE IS?)? usageKeyword
/// binarySign? noSignPhrase? floatFormatPhrase*</c>), so which usage a phrase may follow is decided at bind time,
/// by <see cref="UsagePhraseRules"/>, and nowhere else.</para></summary>
public enum UsagePhrase
{
    /// <summary><c>SIGNED</c> / <c>UNSIGNED</c> (grammar rule <c>binarySign</c>).</summary>
    Sign,
    /// <summary><c>WITH NO SIGN</c> (grammar rule <c>noSignPhrase</c>; COBOL-2023).</summary>
    NoSign,
    /// <summary>The endianness-phrase, <c>HIGH-ORDER-LEFT</c> / <c>HIGH-ORDER-RIGHT</c> (grammar rule
    /// <c>endiannessPhrase</c>, reached through <c>floatFormatPhrase</c>; COBOL-2014).</summary>
    Endianness,
    /// <summary>The encoding-phrase, <c>BINARY-ENCODING</c> / <c>DECIMAL-ENCODING</c> (grammar rule
    /// <c>encodingPhrase</c>, reached through <c>floatFormatPhrase</c>; COBOL-2014).</summary>
    Encoding,
}

/// <summary>One row of <see cref="UsagePhraseRules"/>: a phrase, the usages its general-format line admits it on,
/// and how a phrase written anywhere else is refused.</summary>
/// <param name="Phrase">The phrase.</param>
/// <param name="Admits">True for exactly the usages whose §13.18.60.2 line prints the phrase. Keyed on the
/// RESOLVED <see cref="Usage"/>, so a vendor spelling of a standard usage (COMP-3 for PACKED-DECIMAL, register row
/// <c>usage-comp-3</c>) takes that usage's phrases, and a vendor usage with no standard twin (COMP-5) takes none.</param>
/// <param name="Words">The phrase as a programmer writes it, for the diagnostic.</param>
/// <param name="AdmittedOn">The usages the phrase is written with, for the diagnostic.</param>
/// <param name="Refusal">The diagnostic for the phrase written on any other usage.</param>
/// <param name="Rule">The general rule that gives the phrase its meaning on the usages that admit it, cited
/// after §13.18.60.2 in the diagnostic.</param>
public sealed record UsagePhraseRule(
    UsagePhrase Phrase, Func<Usage, bool> Admits, string Words, string AdmittedOn, DiagnosticDescriptor Refusal,
    string Rule);

/// <summary>⛔ THE ONE TABLE OF WHICH USAGE EACH OPTIONAL USAGE-CLAUSE PHRASE MAY FOLLOW — ISO §13.18.60.2's general
/// format, read off the PRINTED page (PDF p.533 = printed folio 503): <c>BINARY-CHAR { SIGNED | UNSIGNED }</c> and
/// its three siblings, <c>PACKED-DECIMAL [ WITH NO SIGN ]</c>, <c>FLOAT-BINARY-32/-64/-128 [ endianness-phrase
/// ]</c> and <c>FLOAT-DECIMAL-16/-34</c> followed by a bracketed choice-indicator group over { encoding-phrase,
/// endianness-phrase }. Every other line prints none of the four.
/// <para>Before kb/Work PB3517 the rule was written three times in <c>DataBinder.BindEntry</c>, one arm per phrase,
/// and the SIGNED / UNSIGNED arm was never written: <c>PIC 9(2) USAGE COMP UNSIGNED</c>, <c>USAGE DISPLAY
/// SIGNED</c> and <c>PIC S9(3) PACKED-DECIMAL UNSIGNED</c> compiled clean and the word was ignored. Now
/// <c>DataBinder.ScreenUsagePhrases</c> asks this table for every phrase the clause carries, and
/// <c>UsageClausePhraseDriftTests</c> holds the table total over <see cref="UsagePhrase"/>, holds the grammar's
/// phrase rules to its rows, checks every grammar usage word against the printed figure, and pins this file as
/// the only reader of the four refusal descriptors.</para></summary>
public static class UsagePhraseRules
{
    /// <summary>The row for <paramref name="phrase"/>.</summary>
    public static UsagePhraseRule Of(UsagePhrase phrase) => phrase switch
    {
        UsagePhrase.Sign => Sign,
        UsagePhrase.NoSign => NoSign,
        UsagePhrase.Endianness => Endianness,
        UsagePhrase.Encoding => Encoding,
        _ => throw new ArgumentOutOfRangeException(nameof(phrase), phrase, "no §13.18.60.2 row for this phrase"),
    };

    /// <summary><c>BINARY-CHAR { SIGNED | UNSIGNED }</c> and its three siblings; §13.18.60.4 GR12 tabulates the
    /// signed and unsigned ranges of those four usages and of no other.</summary>
    private static readonly UsagePhraseRule Sign = new(UsagePhrase.Sign, UsageFamilies.IsFixedWidthBinary,
        "SIGNED / UNSIGNED phrase", "USAGE BINARY-CHAR, BINARY-SHORT, BINARY-LONG or BINARY-DOUBLE",
        DiagnosticCatalog.UsageSignPhraseScope, "§13.18.60.4 GR12");

    /// <summary><c>PACKED-DECIMAL [ WITH NO SIGN ]</c>; §13.18.60.4 GR11 gives the phrase its meaning.</summary>
    private static readonly UsagePhraseRule NoSign = new(UsagePhrase.NoSign, static u => u is Usage.Packed,
        "WITH NO SIGN phrase", "USAGE PACKED-DECIMAL",
        DiagnosticCatalog.UsageNoSignPhraseScope, "§13.18.60.4 GR11");

    /// <summary>The five standard floating-point usages; §13.18.60.4 GR19c/d scope the IMPLIED phrase the same
    /// way. The implementor-defined float usages (COMP-1/COMP-2/FLOAT-SHORT/-LONG/-EXTENDED) are outside it.</summary>
    private static readonly UsagePhraseRule Endianness = new(UsagePhrase.Endianness, UsageFamilies.IsStandardFloat,
        "endianness-phrase (HIGH-ORDER-LEFT / HIGH-ORDER-RIGHT)",
        "a standard floating-point usage — FLOAT-BINARY-32/-64/-128 or FLOAT-DECIMAL-16/-34",
        DiagnosticCatalog.UsageEndiannessPhraseScope, "§13.18.60.4 GR19c/d");

    /// <summary>Only FLOAT-DECIMAL-16/-34; §13.18.60.4 GR20a says the same in prose ("any standard decimal
    /// floating-point usage").</summary>
    private static readonly UsagePhraseRule Encoding = new(UsagePhrase.Encoding, UsageFamilies.IsStandardDecimalFloat,
        "encoding-phrase (BINARY-ENCODING / DECIMAL-ENCODING)",
        "a standard decimal floating-point usage — FLOAT-DECIMAL-16 or FLOAT-DECIMAL-34",
        DiagnosticCatalog.UsageEncodingPhraseScope, "§13.18.60.4 GR20a");
}
