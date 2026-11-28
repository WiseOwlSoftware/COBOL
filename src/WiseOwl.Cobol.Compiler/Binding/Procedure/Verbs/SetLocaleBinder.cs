// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Compiler.Symbols;
using WiseOwl.Cobol.Compiler.Binding.Bound;
using WiseOwl.Cobol.Frontend.Cst;
using WiseOwl.Cobol.Frontend.Parsing;
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Compiler.Binding.Model;

namespace WiseOwl.Cobol.Compiler.Binding.Procedure.Verbs;

using Core = CobolParserCore;

/// <summary>The SET Formats 11 and 12, the locale (ISO §2023:14.9.39.2; Annex A.4.9 item 9; kb/Work PB64 T1): one binder of the SET format families <see cref="SetBinder"/> dispatches to (kb/Work
/// PB2314; DESIGN-architecture-review §8.3, R0-0019). <see cref="SetFormatSelection"/> chose the format before this binder runs.</summary>
internal sealed class SetLocaleBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary><c>SET LOCALE {category… | USER-DEFAULT} TO {identifier-10 | locale-name-1 | USER-DEFAULT | SYSTEM-DEFAULT}</c>
    /// (ISO §2023:14.9.39 Format 11; DESIGN-locale-facility §4.3; kb/Work PB64 T1). The first operand is USER-DEFAULT (GR22 —
    /// the user default is set) or a SET of categories (the printed brace carries choice indicators — §2023:5.2.6.4: one or
    /// more, each at most once, any order; a duplicate is COBOL1666). The TO operand is one dataReference split here:
    /// USER-DEFAULT / SYSTEM-DEFAULT (GR23b/c; SR25 — not with a USER-DEFAULT first operand → COBOL1667), a
    /// locale-name of the SPECIAL-NAMES LOCALE clause (SR26 — undeclared → COBOL1664), else identifier-10, which
    /// shall be an elementary data item of category data-pointer (SR27 → COBOL1668).</summary>
    internal BoundStatement BindSetLocale(Core.SetLocaleStatementContext sl)
    {
        var words = sl.formatWord();                // [0] = LOCALE, [1..] = categories | USER-DEFAULT (formatWords, kb/Work PB764)
        bool setsUserDefault = false;
        var categories = LocaleCategorySet.None;
        // >>COBOL-WORDS (ISO §2023:7.3.10.4 GR2/GR3/GR4; kb/Work PB250): USER-DEFAULT and the LC_ categories are
        // §2023:8.9/§2023:8.10 words the lexer does not tokenize, so the map reaches them only here.
        if (ctx.CobolWords.Is(words[1].GetText(), "USER-DEFAULT"))
        {
            setsUserDefault = true;
            if (words.Length > 2)
            {
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.SetLocaleCategories, $"SET LOCALE USER-DEFAULT {WrittenSource.Of(words[2])}: USER-DEFAULT as the first operand stands alone — "
                    + "the general format's outer brace is a plain alternation of the category list OR USER-DEFAULT (ISO §2023:14.9.39.2 format 11)");
            }
        }
        else
        {
            for (int i = 1; i < words.Length; i++)
            {
                // The CANONICAL word decides the category (GR2/GR4); a de-reserved word resolves to null and
                // is not a category at all (GR3) - the diagnostic below then names it, as it should. A category
                // is a keyword: a word written with an extended letter is none (kb/Work PB4240).
                string w = ctx.CobolWords.Resolve(words[i].GetText()) ?? "";
                LocaleCategorySet cat = w switch
                {
                    "LC_ALL" => LocaleCategorySet.All,
                    "LC_COLLATE" => LocaleCategorySet.Collate,
                    "LC_CTYPE" => LocaleCategorySet.Ctype,
                    "LC_MESSAGES" => LocaleCategorySet.Messages,
                    "LC_MONETARY" => LocaleCategorySet.Monetary,
                    "LC_NUMERIC" => LocaleCategorySet.Numeric,
                    "LC_TIME" => LocaleCategorySet.Time,
                    _ => LocaleCategorySet.None,
                };
                if (cat == LocaleCategorySet.None)
                {
                    return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.SetLocaleCategories, $"SET LOCALE {WrittenSource.Of(words[i])}: '{WrittenSource.Of(words[i])}' is not a locale category — "
                        + "the first operand is one or more of LC_ALL, LC_COLLATE, LC_CTYPE, LC_MESSAGES, LC_MONETARY, LC_NUMERIC, LC_TIME, "
                        + "or USER-DEFAULT (ISO §2023:14.9.39.2 format 11)");
                }
                // §2023:5.2.6.4 — "any single alternative shall be specified only once": the SAME word twice is the violation
                // (LC_ALL beside LC_TIME is two different alternatives — redundant, legal).
                bool duplicate = false;
                for (int j = 1; j < i; j++) if (ctx.CobolWords.Is(words[j].GetText(), w)) duplicate = true;
                if (duplicate)
                {
                    return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.SetLocaleCategories, $"SET LOCALE {string.Join(' ', words.Skip(1).Take(i).Select(x => x.GetText()))}: the category {w} is specified more than once — each alternative "
                        + "of the category brace shall be specified at most once (ISO §2023:14.9.39.2 format 11 / §2023:5.2.6.4)");
                }
                categories |= cat;
            }
        }
        // The TO operand: USER-DEFAULT / SYSTEM-DEFAULT / locale-name-1 / identifier-10.
        // The grammar split the keywords from the names (setLocaleSource, kb/Work PB764): USER-DEFAULT / SYSTEM-DEFAULT
        // arrive as a formatWord, identifier-10 / locale-name-1 as the dataReference.
        var source = sl.setLocaleSource();
        string first = setsUserDefault ? "SET LOCALE USER-DEFAULT" : $"SET LOCALE {string.Join(' ', words.Skip(1).Select(x => x.GetText()))}";
        if (source.formatWord() is { } keyword)
        {
            string kw = keyword.GetText();
            bool user = ctx.CobolWords.Is(kw, "USER-DEFAULT");
            if (setsUserDefault)
            {
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.SetLocaleUserDefaultSource, $"{first} TO {kw.ToUpperInvariant()}: if USER-DEFAULT is specified as the first operand, "
                    + "identifier-10 or locale-name-1 shall be specified in the TO phrase (ISO §2023:14.9.39.3 SR25)");
            }
            return new BoundSetLocale(categories, false, user ? LocaleSetSource.UserDefault : LocaleSetSource.SystemDefault, null, null);
        }
        var to = source.dataReference();
        string toText = to.GetText();
        string toTextWritten = WrittenSource.Of(to);
        // A bare word that is a declared locale-name (SR26): §2023:8.3.2.2 keeps the two name types apart within one
        // source element. A locale-name a CONTAINING element declares yields to the referring element's own item of
        // the same spelling — "a source element's reference to such a name, even when it is a different type of
        // user-defined word, is to the item which that source element describes" (§2023:8.4.6.1; kb/Work PB3153) — so
        // the own-item test is asked first, and only the locale-name reading records the §2023:8.3.2.2 use.
        if (to.ChildCount == 1 && ctx.Symbols.DeclaresConfigurationName(NameClass.Locale, toText) && !ctx.Refs.DescribesOwnItem(to)
            && ctx.Symbols.Identify(to, NameClass.Locale, report: true)?.Symbol is LocaleSymbol symbol)
            return new BoundSetLocale(categories, setsUserDefault, LocaleSetSource.LocaleName, symbol, null);
        // identifier-10: an elementary data item of category data-pointer (SR27).
        if (ctx.Refs.Probe(to) is { } probe)
        {
            if (probe.Item.Pic?.Category is not PicCategory.Pointer)
            {
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.SetLocalePointerCategory, $"{first} TO {toTextWritten}: identifier-10 shall reference an elementary data item of category "
                    + $"data-pointer (ISO §2023:14.9.39.3 SR27) — '{toTextWritten}' is {(probe.Item.Pic is { } pic ? $"of category {ItemCategory.CategoryName(pic)}" : "a group")}");
            }
            if (host.Expr.ResolveSending(to) is var toAnswer && toAnswer.Place is not { } place)
                return toAnswer.Refusal(ctx.Edition);   // the resolver's answer (kb/Work PB1030)
            return new BoundSetLocale(categories, setsUserDefault, LocaleSetSource.SavedPointer, null, place);
        }
        // Neither: the ONE undeclared-locale-name diagnostic, with this site named (SR26).
        ctx.Data.SpecialNames.ResolveLocaleName(to, toText, $"{first} TO {toTextWritten}",
            "ISO §2023:14.9.39.3 SR26 — locale-name-1 shall be specified in the LOCALE clause of the SPECIAL-NAMES paragraph; "
            + "or SR27 — identifier-10 shall reference an elementary data item of category data-pointer, and no such item is declared");
        return BoundRejected.Reported(ctx.Edition);
    }

    /// <summary><c>SET identifier-11 TO LOCALE {LC_ALL | USER-DEFAULT}</c> (ISO §2023:14.9.39 Format 12; kb/Work PB64 T1):
    /// identifier-11 shall be an elementary data item of category data-pointer (SR28 → COBOL1668); the words after TO
    /// are LOCALE and LC_ALL (GR26) or USER-DEFAULT (GR27) — the predicate admitted exactly those.</summary>
    internal BoundStatement BindSaveLocale(Core.SetLocaleStatementContext sl)
    {
        bool userDefault = ctx.CobolWords.Is(sl.formatWord(1).GetText(), "USER-DEFAULT");
        var target = sl.setReceivingIdentifier().dataReference();   // a data reference: BindSet refused any other form (kb/Work PB1551)
        if (host.Expr.ResolveReceiving(target) is not { } place || place.Item.Pic?.Category is not PicCategory.Pointer)
        {
            return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.SetLocalePointerCategory, $"SET {WrittenSource.Of(target)} TO LOCALE {(userDefault ? "USER-DEFAULT" : "LC_ALL")}: identifier-11 shall "
                + "reference an elementary data item of category data-pointer (ISO §2023:14.9.39.3 SR28)");
        }
        return new BoundSaveLocale(place, userDefault);
    }
}
