// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding;

using Core = CobolParserCore;

/// <summary>The optional phrases of one USAGE clause (ISO §13.18.60.2) as the binder ADMITS them: each field holds
/// what the program wrote only when the phrase is legal on the clause's usage, and its default otherwise, so a
/// refused phrase can never go on to change a representation.</summary>
/// <param name="Unsigned">UNSIGNED on a fixed-width binary usage (SIGNED is the default, §13.18.60.4 GR12).</param>
/// <param name="NoSign">WITH NO SIGN on PACKED-DECIMAL (§13.18.60.4 GR11). Its effect and its §13.18.40.3 SR31
/// screen are adjudicated per elementary item in <c>UsageInheritancePass</c>, because §13.18.60.4 GR1 hands a
/// group-level clause, phrase included, to each elementary item in the group (kb/Work PB570).</param>
/// <param name="Endianness">The item's own endianness-phrase (§13.18.60.4 GR19a/b), which wins over the OPTIONS
/// FLOAT-BINARY clause's implied one by §11.9.8.3 SR2/SR3's own wording.</param>
/// <param name="Encoding">The item's own encoding-phrase (§13.18.60.4 GR20). Applicable only to FLOAT-DECIMAL-16/-34,
/// which are documented non-support (COBOL1564, Annex A.3 item 19), so nothing consumes it yet.</param>
internal readonly record struct UsageClausePhrases(
    bool Unsigned, bool NoSign, FloatEndianness Endianness, FloatEncoding Encoding);

public sealed partial class DataBinder
{
    /// <summary>⛔ THE ONE READER OF A USAGE CLAUSE'S OPTIONAL PHRASES (ISO §13.18.60.2; kb/Work PB174, PB3517).
    /// The grammar parses <c>binarySign? noSignPhrase? floatFormatPhrase*</c> after every usage word, so this
    /// method decides, for each phrase the clause carries, whether the usage's line of the general format prints
    /// it — by asking <see cref="UsagePhraseRules"/>, the one table of that fact — and refuses it by name when it
    /// does not. It runs where the entry's usage is resolved and before anything reads a phrase, and it runs
    /// whether or not the entry turns out to be a group, because which usage a phrase may follow is a fact about
    /// the written clause.
    /// <para>It is asked at every grammar host of <c>usageClause</c> the binder accepts: a data description entry
    /// (<c>BindEntry</c>) and a report group description entry (<c>DescribeReportItem</c>), whose USAGE clause
    /// §13.15.4 GR2 makes "the same clauses as those that are described under the general format for a data
    /// description entry". The third host, a screen description entry, is declined with its whole module
    /// (<c>ScreenFacility</c>, COBOL1560). <c>UsageClausePhraseDriftTests</c> reads every host out of the
    /// grammar and probes each, so a fourth cannot ship unscreened (branch review R1-1 of kb/Work PB3517).</para>
    /// <para>The FLOAT-DECIMAL-16/-34 tail is a bracketed choice-indicator group, and the FLOAT-BINARY-* tail a
    /// single bracket, so the float phrases are also screened for REPEAT: §5.2.6.4, "any single alternative may
    /// be specified only once" (COBOL1718). The same clause makes their order free ("The alternatives may be
    /// specified in any order"), which the grammar's <c>floatFormatPhrase*</c> already is. A phrase on
    /// FLOAT-BINARY-128 / FLOAT-DECIMAL-16 / -34 is applicable even though those usages are documented
    /// non-support (COBOL1564), so the refusal a user sees names the non-support.</para></summary>
    /// <param name="clause">The entry's USAGE clause, or <see langword="null"/> when it wrote none.</param>
    /// <param name="usage">The clause's resolved usage.</param>
    /// <param name="usageText">The usage word as written, for the diagnostic.</param>
    /// <param name="entryWhere">The entry, for the diagnostic.</param>
    private UsageClausePhrases ScreenUsagePhrases(Core.UsageClauseContext? clause, Usage usage, string? usageText,
        string entryWhere)
    {
        if (clause is null) return default;

        bool unsigned = clause.binarySign() is { } sign && Admitted(UsagePhrase.Sign)
            && sign.UNSIGNED() is not null;
        bool noSign = clause.noSignPhrase() is not null && Admitted(UsagePhrase.NoSign);

        FloatEndianness endianness = FloatEndianness.Unspecified;
        FloatEncoding encoding = FloatEncoding.Unspecified;
        foreach (var phrase in clause.floatFormatPhrase())
        {
            if (phrase.endiannessPhrase() is { } ep)
            {
                if (endianness is not FloatEndianness.Unspecified) Repeated("endianness-phrase");
                endianness = FloatFormatPhrase.Endianness(ep);
            }
            else if (phrase.encodingPhrase() is { } enc)
            {
                if (encoding is not FloatEncoding.Unspecified) Repeated("encoding-phrase");
                encoding = FloatFormatPhrase.Encoding(enc);
            }
        }
        if (endianness is not FloatEndianness.Unspecified && !Admitted(UsagePhrase.Endianness))
            endianness = FloatEndianness.Unspecified;
        if (encoding is not FloatEncoding.Unspecified && !Admitted(UsagePhrase.Encoding))
            encoding = FloatEncoding.Unspecified;

        return new UsageClausePhrases(unsigned, noSign, endianness, encoding);

        bool Admitted(UsagePhrase written)
        {
            var rule = UsagePhraseRules.Of(written);
            if (rule.Admits(usage)) return true;
            Edition.Error(rule.Refusal, $"{entryWhere}: the {rule.Words} is written only with {rule.AdmittedOn}, "
                + $"not USAGE {usageText} (ISO §13.18.60.2 general format; {rule.Rule})");
            return false;
        }

        void Repeated(string words) =>
            Edition.Error(DiagnosticCatalog.UsageFloatFormatPhraseRepeated, $"{entryWhere}: the {words} is "
                + "specified more than once on this USAGE clause (ISO §5.2.6.4 — any single alternative may be "
                + "specified only once)");
    }
}
