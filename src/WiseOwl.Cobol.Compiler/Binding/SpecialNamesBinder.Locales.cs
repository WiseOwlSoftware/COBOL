// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Common;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Runtime.Collation;

using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding;

using Core = CobolParserCore;
using WiseOwl.Cobol.Runtime;

/// <summary>The LOCALE clause and the OBJECT-COMPUTER CHARACTER CLASSIFICATION clause (ISO §12.3.7.2; §12.3.6.2; DESIGN-locale-facility).
/// A partial of <see cref="SpecialNamesBinder"/>.</summary>
public sealed partial class SpecialNamesBinder
{
    /// <summary>SPECIAL-NAMES <c>LOCALE locale-name-1 IS {external-locale-name-1 | literal-4}</c> declarations
    /// (case-insensitive locale-name → the symbol; ISO §12.3.7.2; DESIGN-locale-facility seam S1, T1): the locale-name
    /// is a user-defined word (§8.3.2.2) referenced by <c>ALPHABET … IS LOCALE locale-name-2</c> (§12.3.7.3 SR24), SET
    /// LOCALE (§14.9.39.3 SR26), and — in later increments — the LOCALE phrases of PICTURE format 2, LOCALE-COMPARE /
    /// -DATE / -TIME, UPPER-CASE / LOWER-CASE and CHARACTER CLASSIFICATION. The symbol holds the EXTERNAL IDENTIFICATION
    /// only (§8.1.5 — the locale is "determined at runtime"); it inherits into contained units (§12.3.7.4 GR1).</summary>
    internal ConfigurationNames<LocaleSymbol> Locales =>
        field ??= new(UserWordKind.LocaleName, _host.RecordConfigurationNameUse);

    /// <summary>The OBJECT-COMPUTER <c>CHARACTER CLASSIFICATION</c> clause (ISO §12.3.6.2; §12.3.6.4 GR5–GR8; kb/Work PB64
    /// T5), or null when none is specified and no containing unit specifies one (GR6 — the coded character set's
    /// classification). Inherited per §12.3.6.4 GR1; consumed by the emitter (resolved at each activation —
    /// <c>__CLASSIFY</c>), by UPPER-CASE / LOWER-CASE without a LOCALE phrase (§15.97.4 r3 / §15.57.4 r3) and by the
    /// ALPHABETIC class tests (§8.8.4.4.4 GR3).</summary>
    public ClassificationSpec? Classification { get; private set; }

    /// <summary>Bind the OBJECT-COMPUTER <c>CHARACTER CLASSIFICATION {IS locale-phrase-1 [locale-phrase-2] | {FOR ALPHANUMERIC
    /// IS locale-phrase-1 | FOR NATIONAL IS locale-phrase-2}…}</c> clause (ISO §12.3.6.2; kb/Work PB64 T5): each phrase is
    /// LOCALE (the current locale at activation), SYSTEM-DEFAULT, USER-DEFAULT, or a locale-name of the SPECIAL-NAMES
    /// paragraph (§12.3.6.3 SR3 — undeclared → COBOL1664, the ONE undeclared-locale-name diagnostic); the two-phrase IS
    /// form gives the alphanumeric then the national classification (GR5 a–j); a class named twice in the FOR form is
    /// a form violation (§5.2.6.4 — each at most once).</summary>
    private void ResolveClassification(Core.CharacterClassificationClauseContext cc)
    {
        using var _ = Edition.At(cc);
        LocalePhrase? alphanumeric = null, national = null;
        // The FOR pair — each class at most once (§5.2.6.4) — through the one reader (kb/Work PB1075).
        var (alnumFor, natFor) = ChoiceIndicators.ForPhrasePair(Edition, cc.classificationForPhrase(),
            f => f.NATIONAL() is not null, "CHARACTER CLASSIFICATION", "12.3.6.2");
        if (alnumFor is not null || natFor is not null)
        {
            if (alnumFor is not null
                && (alphanumeric = ClassificationPhrase(alnumFor.localePhrase(), alnumFor.localePhrase().GetText(), national: false)) is null) return;
            if (natFor is not null
                && (national = ClassificationPhrase(natFor.localePhrase(), natFor.localePhrase().GetText(), national: true)) is null) return;
        }
        else
        {
            var phrases = cc.localePhrase();   // [0] = locale-phrase-1, [1] = locale-phrase-2
            if (phrases.Length < 1) return;    // a malformed shape already drew a parse error
            alphanumeric = ClassificationPhrase(phrases[0], phrases[0].GetText(), national: false);
            if (alphanumeric is null) return;
            if (phrases.Length > 1)
            {
                national = ClassificationPhrase(phrases[1], phrases[1].GetText(), national: true);
                if (national is null) return;
            }
        }
        Classification = new ClassificationSpec(alphanumeric, national);
    }

    private LocalePhrase? ClassificationPhrase(Antlr4.Runtime.RuleContext at, string word, bool national)
    {
        // >>COBOL-WORDS (ISO §7.3.10.4 GR2/GR3/GR4; kb/Work PB250): a §8.9/§8.10 word the lexer does not
        // tokenize is reached ONLY through the map; a raw text test here is inert to the directive both ways.
        if (_host.CobolWords.Is(word, "LOCALE")) return new LocalePhrase(Runtime.Globalization.LocalePhraseKind.Current, null);
        if (_host.CobolWords.Is(word, "SYSTEM-DEFAULT")) return new LocalePhrase(Runtime.Globalization.LocalePhraseKind.SystemDefault, null);
        if (_host.CobolWords.Is(word, "USER-DEFAULT")) return new LocalePhrase(Runtime.Globalization.LocalePhraseKind.UserDefault, null);
        var sym = ResolveLocaleName(at, word, $"CHARACTER CLASSIFICATION{(national ? " FOR NATIONAL" : "")} {word}",
            $"ISO §12.3.6.3 SR3 — locale-name-{(national ? 2 : 1)} shall be a locale name defined in the SPECIAL-NAMES paragraph");
        return sym is null ? null : new LocalePhrase(Runtime.Globalization.LocalePhraseKind.Named, sym);
    }

    /// <summary>
    /// Bind the SPECIAL-NAMES <c>ORDER TABLE ordering-name-1 IS literal-9</c> clause (ISO §12.3.7.2 — the last item
    /// of the paragraph's general format; kb/Work PB101 / DESIGN-locale-facility §4.9). §12.3.7.4 GR17: "When ORDER
    /// TABLE is specified, ordering-name-1 shall reference a cultural ordering table that is identified by
    /// literal-9 and constructed in accordance with ISO/IEC 14651:2020, Annex A. The implementor specifies the
    /// allowable content of literal-9."
    /// <list type="bullet">
    ///   <item>SR10 — "Literal-4 and literal-9 shall be alphanumeric or national literals": a numeric, boolean or
    ///   figurative operand is COBOL0898.</item>
    ///   <item>SR11 — literal-9 "shall specify neither a symbolic-character figurative constant nor a zero-length
    ///   literal".</item>
    ///   <item>The clause is BRACKETED and unrepeated in the general format (unlike LOCALE / CURRENCY / CLASS,
    ///   which carry a printed ellipsis), so a second ORDER TABLE clause — or a repeated ordering-name — is a form
    ///   violation.</item>
    /// </list>
    /// <para>⚖ The ADVISORY (COBOL1662, a warning): GR17 leaves literal-9's allowable content to the
    /// implementor, so a spelling this implementation cannot resolve is legal source with a defined RUNTIME
    /// outcome — §15.85.4 r2 sets EC-ORDER-NOT-SUPPORTED at every reference. Rejecting it would refuse a
    /// conforming program; saying nothing would let a program whose every STANDARD-COMPARE is inoperative compile
    /// clean. The resolver consulted here is the SAME one the runtime uses
    /// (<c>CollationEngine.TryGetOrderingTable</c>), so bind-time advice and run-time behaviour cannot drift.</para>
    /// </summary>
    /// <summary>The SPECIAL-NAMES <c>LOCALE locale-name-1 IS {external-locale-name-1 | literal-4}</c> clause (ISO
    /// §12.3.7.2; DESIGN-locale-facility §4.1, T1): declare the locale-name with its external identification. §12.3.7.3
    /// SR10 — literal-4 "shall be alphanumeric or national"; SR11 — "shall not be a symbolic-character figurative
    /// constant" nor a zero-length literal (the grammar admits any literal, so both are checked here); a locale-name
    /// declared twice is COBOL4250, asked by the declaration funnel (§8.4.2.1; kb/Work PB3216). §12.3.7.4
    /// GR5 — the implementor specifies the allowable content: DETERMINATION L1, a locale tag, POSIX spellings normalized
    /// (<see cref="LocaleSymbol.Tag"/>); availability is NOT checked here (§8.1.5 — run time; L1 item 4).</summary>
    private void LocaleBind(Core.LocaleClauseContext loc)
    {
        var words = loc.cobolWord();                  // [0] = locale-name-1, [1] = external-locale-name-1 (word branch); LOCALE is the formatWord
        string name = words[0].GetText();
        _host.DeclareUserWord(name, UserWordKind.LocaleName);   // §8.3.2.2 — the one declaration funnel (kb/Work PB1083)
        string external;
        // literal-4 is written as a literal, or as a constant-name standing for one (§13.10.3 SR2; kb/Work PB1942). The
        // word branch is ALSO external-locale-name-1, a system-name: ⚠ DETERMINATION — a word this unit declares as a
        // constant-name takes the constant reading (the program's own declaration of the word), any other word is
        // external-locale-name-1.
        Antlr4.Runtime.Tree.IParseTree? written4 = loc.literal() is { } lit4 ? lit4
            : words.Length > 1 && _host.NamesConstant(words[1].GetText()) ? words[1]
            : null;
        bool fromLiteral = written4 is not null;
        if (written4 is not null)
        {
            string spelling = loc.literal() is { } l ? ConcatFolder.Spelling(l) : written4.GetText();
            // SR10 / SR11 for literal-4 — the ONE text-literal rule the ORDER TABLE clause's literal-9 shares.
            if (!TryClauseTextLiteral(written4, $"LOCALE {name} IS {spelling}", "literal-4", out external)) return;
        }
        else external = words[1].GetText();
        // A second LOCALE of one name was refused by the declaration funnel (DeclareUserWord, ISO §8.4.2.1; kb/Work
        // PB3216); the first stands, as every registry keeps it.
        Locales.TryAdd(name, new LocaleSymbol(name, external, fromLiteral));
    }

    /// <summary>Resolve a locale-name reference (ISO §12.3.7.3 SR24 for the ALPHABET clause; §14.9.39.3 SR26 for SET
    /// LOCALE; the later increments' sites) to its symbol, or null after COBOL1664 — the ONE undeclared-locale-name
    /// diagnostic, with the citing site named (DESIGN-locale-facility §7 rule a).</summary>
    /// <param name="at">The reference, whose source element's §8.3.2.2 census records the use (kb/Work PB3153).</param>
    internal LocaleSymbol? ResolveLocaleName(Antlr4.Runtime.RuleContext at, string name, string site, string rule)
    {
        if (Locales.TryUse(at, name, out var sym)) return sym;
        Edition.Error("COBOL1664", $"{site}: '{name}' is not a locale-name declared by a SPECIAL-NAMES LOCALE clause ({rule})"
            + (Locales.Count == 0 ? "; no LOCALE clause is in scope" : $"; declared: {string.Join(", ", Locales.Names)}"));
        return null;
    }
}
