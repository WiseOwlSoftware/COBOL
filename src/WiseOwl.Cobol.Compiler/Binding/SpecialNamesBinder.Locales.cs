// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Symbols;
using WiseOwl.Cobol.Common;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Runtime.Collation;

using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Editions.Diagnostics;

namespace WiseOwl.Cobol.Binding;

using Core = CobolParserCore;
using WiseOwl.Cobol.Runtime;

/// <summary>The LOCALE clause and the OBJECT-COMPUTER CHARACTER CLASSIFICATION clause (ISO §2023:12.3.7.2; §2023:12.3.6.2; DESIGN-locale-facility).
/// A partial of <see cref="SpecialNamesBinder"/>.</summary>
public sealed partial class SpecialNamesBinder
{
    /// <summary>Bind the OBJECT-COMPUTER <c>CHARACTER CLASSIFICATION {IS locale-phrase-1 [locale-phrase-2] | {FOR ALPHANUMERIC
    /// IS locale-phrase-1 | FOR NATIONAL IS locale-phrase-2}…}</c> clause (ISO §2023:12.3.6.2; kb/Work PB64 T5): each phrase is
    /// LOCALE (the current locale at activation), SYSTEM-DEFAULT, USER-DEFAULT, or a locale-name of the SPECIAL-NAMES
    /// paragraph (§2023:12.3.6.3 SR3 — undeclared → COBOL1664, the ONE undeclared-locale-name diagnostic); the two-phrase IS
    /// form gives the alphanumeric then the national classification (GR5 a–j); a class named twice in the FOR form is
    /// a form violation (§2023:5.2.6.4 — each at most once).</summary>
    private void ResolveClassification(Core.CharacterClassificationClauseContext cc)
    {
        using var _ = Edition.At(cc);
        LocalePhrase? alphanumeric = null, national = null;
        // The FOR pair — each class at most once (§2023:5.2.6.4) — through the one reader (kb/Work PB1075).
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
        Cfg.Classification = new ClassificationSpec(alphanumeric, national);
    }

    private LocalePhrase? ClassificationPhrase(Antlr4.Runtime.ParserRuleContext at, string word, bool national)
    {
        // >>COBOL-WORDS (ISO §2023:7.3.10.4 GR2/GR3/GR4; kb/Work PB250): a §2023:8.9/§2023:8.10 word the lexer does not
        // tokenize is reached ONLY through the map; a raw text test here is inert to the directive both ways.
        if (_host.CobolWords.Is(word, "LOCALE")) return new LocalePhrase(Runtime.Globalization.LocalePhraseKind.Current, null);
        if (_host.CobolWords.Is(word, "SYSTEM-DEFAULT")) return new LocalePhrase(Runtime.Globalization.LocalePhraseKind.SystemDefault, null);
        if (_host.CobolWords.Is(word, "USER-DEFAULT")) return new LocalePhrase(Runtime.Globalization.LocalePhraseKind.UserDefault, null);
        var sym = ResolveLocaleName(at, word, $"CHARACTER CLASSIFICATION{(national ? " FOR NATIONAL" : "")} {word}",
            $"ISO §2023:12.3.6.3 SR3 — locale-name-{(national ? 2 : 1)} shall be a locale name defined in the SPECIAL-NAMES paragraph");
        return sym is null ? null : new LocalePhrase(Runtime.Globalization.LocalePhraseKind.Named, sym);
    }

    /// <summary>The SPECIAL-NAMES <c>LOCALE locale-name-1 IS {external-locale-name-1 | literal-4}</c> clause (ISO
    /// §2023:12.3.7.2; DESIGN-locale-facility §4.1, T1): declare the locale-name with its external identification. §2023:12.3.7.3
    /// SR10 — literal-4 "shall be alphanumeric or national"; SR11 — "shall not be a symbolic-character figurative
    /// constant" nor a zero-length literal (the grammar admits any literal, so both are checked here); a locale-name
    /// declared twice is COBOL4250, asked by the declaration funnel (§2023:8.4.2.1; kb/Work PB3216). §2023:12.3.7.4
    /// GR5 — the implementor specifies the allowable content: DETERMINATION L1, a locale tag, POSIX spellings normalized
    /// (<see cref="LocaleSymbol.Tag"/>); availability is NOT checked here (§2023:8.1.5 — run time; L1 item 4).</summary>
    private void LocaleBind(Core.LocaleClauseContext loc)
    {
        var words = loc.cobolWord();                  // [0] = locale-name-1, [1] = external-locale-name-1 (word branch); LOCALE is the formatWord
        string name = words[0].GetText();
        _host.DeclareUserWord(name, UserWordKind.LocaleName);   // §2023:8.3.2.2 — the one declaration funnel (kb/Work PB1083)
        string external;
        // literal-4 is written as a literal, or as a constant-name standing for one (§2023:13.10.3 SR2; kb/Work PB1942). The
        // word branch is ALSO external-locale-name-1, a system-name: ⚠ DETERMINATION — a word this unit declares as a
        // constant-name takes the constant reading (the program's own declaration of the word), any other word is
        // external-locale-name-1.
        Antlr4.Runtime.Tree.IParseTree? written4 = loc.literal() is { } lit4 ? lit4
            : words.Length > 1 && _host.Symbols.NamesConstant(words[1].GetText()) ? words[1]
            : null;
        bool fromLiteral = written4 is not null;
        if (written4 is not null)
        {
            string spelling = loc.literal() is { } l ? ConcatFolder.Spelling(l) : written4.GetText();
            // SR10 / SR11 for literal-4 — the ONE text-literal rule the ORDER TABLE clause's literal-9 shares.
            if (!TryClauseTextLiteral(written4, $"LOCALE {name} IS {spelling}", "literal-4", out external)) return;
        }
        else external = words[1].GetText();
        // A second LOCALE of one name was refused by the declaration funnel (DeclareUserWord, ISO §2023:8.4.2.1; kb/Work
        // PB3216); the first stands, as every registry keeps it.
        _host.Symbols.Declare(new LocaleSymbol(name, external, fromLiteral));
    }

    /// <summary>Resolve a locale-name reference (ISO §2023:12.3.7.3 SR24 for the ALPHABET clause; §2023:14.9.39.3 SR26 for SET
    /// LOCALE; the later increments' sites) to its symbol, or null after COBOL1664 — the ONE undeclared-locale-name
    /// diagnostic, with the citing site named (DESIGN-locale-facility §7 rule a).</summary>
    /// <param name="written">The reference as written, identified through <see cref="SymbolTable.Identify(Antlr4.Runtime.ParserRuleContext,
    /// NameClass, bool, NameReach, ProcedureSite?)"/>, whose source element's §2023:8.3.2.2 census records the use (kb/Work PB3153).</param>
    /// <param name="name">The name as the diagnostic prints it.</param>
    internal LocaleSymbol? ResolveLocaleName(Antlr4.Runtime.ParserRuleContext written, string name, string site, string rule)
    {
        if (_host.Symbols.Identify(written, NameClass.Locale, report: true)?.Symbol is LocaleSymbol sym) return sym;
        var (count, names) = _host.Symbols.LocaleNames;
        Edition.Error(DiagnosticCatalog.LocaleNameUndeclared, $"{site}: '{name}' is not a locale-name declared by a SPECIAL-NAMES LOCALE clause ({rule})"
            + (count == 0 ? "; no LOCALE clause is in scope" : $"; declared: {string.Join(", ", names)}"));
        return null;
    }
}
