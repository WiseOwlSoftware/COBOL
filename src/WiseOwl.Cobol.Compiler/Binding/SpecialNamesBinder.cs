// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Common;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Runtime.Collation;

using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding;

using Core = CobolParserCore;
using WiseOwl.Cobol.Runtime;

/// <summary>
/// The binder of one source element's SPECIAL-NAMES paragraph and the OBJECT-COMPUTER clauses that name its
/// alphabets and locales (ISO §2023:12.3.6, §2023:12.3.7; the census's R0-0001 target "SpecialNamesBinder",
/// DESIGN-architecture-review §8.3 (1)). It declares the configuration-derived state of the unit's <see cref="ConfigurationSlice"/>: the external-switch registry (§2023:12.3.7 — a
/// switch-name associates an implementor-defined external switch with an optional mnemonic-name and/or ON/OFF status
/// condition-names; the mnemonic is referenced only in SET [SR5], the condition-names are interrogated as switch-status
/// conditions [GR2 / §2023:8.8.4.6], SET alters the status [GR3 / §2023:14.9.39 F3]), the CLASS, ALPHABET, SYMBOLIC CHARACTERS,
/// LOCALE, ORDER TABLE and DYNAMIC LENGTH STRUCTURE names, the PROGRAM COLLATING SEQUENCE and CHARACTER CLASSIFICATION,
/// DECIMAL-POINT IS COMMA and the CURRENCY SIGN set. The data binder constructs one per unit
/// (<see cref="DataBinder.SpecialNames"/>), walks the configuration section through <see cref="BindSpecialNames"/> at the top of its
/// declaration half, and writes what it declares into the unit's <see cref="ConfigurationSlice"/> (<c>DataBindState.Configuration</c>),
/// which every reader reads; a contained unit's slice is seeded from its container's by <see cref="ConfigurationStep"/> (§2023:12.3.4 GR1).
/// </summary>
public sealed partial class SpecialNamesBinder
{
    private readonly DataBinder _host;

    private EditionContext Edition { get; }

    /// <summary>The unit's configuration slice: the state this binder declares and every reader reads (kb/Work PB2296 M5).</summary>
    private ConfigurationSlice Cfg => _host.State.Configuration;

    internal SpecialNamesBinder(DataBinder host, EditionContext edition)
    {
        _host = host;
        Edition = edition;
    }

    // ⛔ NO SWITCH-MNEMONIC MAP LIVES HERE (kb/Work PB454). A switch's mnemonic-name is read, like every other
    // SPECIAL-NAMES mnemonic, from the ONE per-unit registry `Procedure.MnemonicRegistry` (BinderContext.Mnemonics),
    // whose value is the ImplementorName ROW, so SET Format 3 (§2023:14.9.39.3 SR5) asks the row's KIND. This file used
    // to keep a second, switch-only map built by a second walk of the same entries — the walk in which an Option-2
    // entry (status phrases, no mnemonic) entered the switch-NAME itself as a mnemonic.

    /// <summary>Is <paramref name="name"/> an alphabet-name of EITHER class (ISO §2023:12.3.7 ALPHABET clause)? The
    /// question a trailing <c>IN word</c> after a THROUGH range's identifier asks (kb/Work PB843): §2023:8.3.2.2 makes an
    /// alphabet-name a different type of user-defined word from every name a qualifier can be, so a declared
    /// alphabet-name there is §2023:14.9.13.2's <c>IN alphabet-name-1</c> phrase and never a qualifier. A predicate, so
    /// it records no §2023:8.3.2.2 use: the caller that takes the word as an alphabet-name resolves it through
    /// <see cref="ConfigurationNames{T}.TryUse"/> (kb/Work PB3153).</summary>
    public bool IsAlphabetName(string name) => Cfg.Alphabets.Declares(name) || Cfg.NationalAlphabets.Declares(name);

    /// <summary>Take <paramref name="name"/>, written at <paramref name="site"/>, as a REFERENCE to an alphabet-name of
    /// either class, recording the §2023:8.3.2.2 use (kb/Work PB3153) — the site that names an alphabet whose definition a
    /// later screen or resolver reads (a COLLATING SEQUENCE phrase). False when no alphabet is named so.</summary>
    internal bool UseAlphabetName(Antlr4.Runtime.RuleContext site, string name) =>
        Cfg.Alphabets.TryUse(site, name, out _) || Cfg.NationalAlphabets.TryUse(site, name, out _);

    /// <summary>Is <paramref name="name"/> one of the class condition's user-defined-word alternatives — a
    /// class-name-1 or an alphabet-name-1 (ISO §2023:8.8.4.4.2)? The symbol test that separates a bare EVALUATE object
    /// naming a class from identifier-2 (kb/Work PB843).</summary>
    public bool IsClassConditionWord(string name) => Cfg.UserClasses.Declares(name) || IsAlphabetName(name);

    /// <summary>Normalize a NUMERIC literal's source text to the canonical dot-decimal form the whole emit-side
    /// decode pipeline consumes (ISO §2023:12.3.7.4 GR14 a): under DECIMAL-POINT IS COMMA "the character written in
    /// numeric literals to represent the decimal separator shall be the comma" — and §2023:8.3.3.3.2 admits ONLY the
    /// decimal point in a fixed-point literal, so the OTHER separator is diagnosed in each mode; the legacy's
    /// unconditional acceptance of both is a version-invariant non-conformance, not ported). The ONE literal
    /// chokepoint: expression paths route through <c>StatementBinder.CheckLiteral</c>, VALUE / level-88 capture
    /// call this directly.</summary>
    public string NormalizeNumericLiteral(string text)
    {
        // The normalization ALGORITHM is the ONE shared WiseOwl.Cobol.Common.NumericLiteral.Normalize (Frontend), so the
        // compile-time expression evaluator applies the identical §2023:12.3.7.4 GR14 a) rule; the binder retains ownership
        // of the COBOL0895 diagnostic (its own channel/descriptor), routing the returned issue to it verbatim.
        string norm = Common.NumericLiteral.Normalize(text, Cfg.DecimalPointIsComma, out var issue);
        switch (issue)
        {
            case Common.NumericSeparatorIssue.DecimalPointUnderCommaMode:
                Edition.Error(DiagnosticCatalog.NumericLiteralDecimalSeparator,
                    $"numeric literal '{text}': under DECIMAL-POINT IS COMMA the "
                    + "decimal separator is the comma (ISO §2023:12.3.7.4 GR14a); '.' is not valid in a numeric literal");
                break;
            case Common.NumericSeparatorIssue.CommaWithoutCommaMode:
                Edition.Error(DiagnosticCatalog.NumericLiteralDecimalSeparator,
                    $"numeric literal '{text}': a comma decimal separator requires "
                    + "DECIMAL-POINT IS COMMA (ISO §2023:12.3.7.4 GR14a; §2023:8.3.3.3.2 admits only '.' as the decimal point)");
                break;
        }
        // §2023:8.3.3.3.3 SR2/SR3/SR4 (kb/Work PB99) — the floating-point literal's FORM, checked HERE because both the
        // expression funnel (ExpressionBinder.CheckLiteral) and the VALUE / level-88 funnel normalize through this method.
        if (Common.NumericLiteral.IsFloatingPointForm(norm)
            && Common.NumericLiteral.CheckFloatingPointForm(norm) is var fissue && fissue != Common.NumericLiteral.FloatingLiteralIssue.None)
            Edition.Error(DiagnosticCatalog.FloatingLiteral, $"floating-point numeric literal '{text}': " + fissue switch
            {
                Common.NumericLiteral.FloatingLiteralIssue.SignificandDigits => "the significand shall be from 1 to 36 digits in length (ISO §2023:8.3.3.3.3 SR2)",
                Common.NumericLiteral.FloatingLiteralIssue.ExponentDigits => "the exponent shall have a maximum of four digits (ISO §2023:8.3.3.3.3 SR3)",
                _ => "when all the digits of the significand are zero, all the digits of the exponent shall be zero and neither part shall have a negative sign (ISO §2023:8.3.3.3.3 SR4)",
            });
        return norm;   // canonical dot-decimal; a diagnosed literal is normalized too so downstream decode stays well-formed
    }

    /// <summary>Normalize <paramref name="text"/> when it is a numeric literal (digits with optional sign and
    /// separators); any other operand text (quoted literal, figurative word, data-name) passes through. Used by
    /// the VALUE-clause and level-88 capture paths, whose operand texts are not yet classified.</summary>
    internal string NormalizeIfNumericLiteral(string text)
    {
        bool anyDigit = false, sawE = false;
        foreach (char c in text)
        {
            if (char.IsAsciiDigit(c)) { anyDigit = true; continue; }
            // the floating-point form's ONE exponent marker (§2023:8.3.3.3.3) — its significand normalizes like any literal
            if ((c is 'E' or 'e') && !sawE && anyDigit) { sawE = true; continue; }
            if (c is not ('+' or '-' or '.' or ',')) return text;
        }
        return anyDigit ? NormalizeNumericLiteral(text) : text;
    }

    /// <summary>Bind <c>DECIMAL-POINT IS COMMA</c> (ISO §2023:12.3.7 — the only word the format admits after IS is
    /// COMMA; the grammar carries it as a generic IDENTIFIER).
    /// <para>⛔ COMMA IS NO LEXER TOKEN, so the word is asked through the group's <c>&gt;&gt;COBOL-WORDS</c> map
    /// (<see cref="CobolWordsMap.Is"/>; kb/Work PB4572): after <c>EQUATE "COMMA" WITH "CMA"</c> the synonym "may be used in
    /// any syntax requiring the use of the reserved word" (§2023:7.3.10.4 GR2), so <c>DECIMAL-POINT IS CMA</c> is the clause
    /// and a spelling test of COMMA refused it with COBOL0894. A COMMA the directive de-reserved (GR3, GR4) is no longer
    /// the word this clause requires.</para></summary>
    private void SwitchBindDecimalPoint(Core.DecimalPointClauseContext dp)
    {
        var words = _host.CobolWords;
        string? written = dp.IDENTIFIER()?.GetText();
        if (written is not null && words.Is(written, "COMMA"))
            Cfg.DecimalPointIsComma = true;
        else if (words.RemovedBy("COMMA") is { } withdrawn)
            // A directive of this group withdrew COMMA (UNDEFINE literal-3, SUBSTITUTE literal-4): "the only form is
            // DECIMAL-POINT IS COMMA" would be false about the cause. Under UNDEFINE the clause is not available in the
            // group whatever word follows IS (GR3); under SUBSTITUTE literal-5 "shall be used in any syntax where" COMMA "is
            // documented as required" (GR4), so the clause is available and written with literal-5. The sentence is the ONE
            // CobolWordsOp builds for a withdrawn word (kb/Work PB4573).
            Edition.Error(DiagnosticCatalog.DecimalPointFormInvalid, $"DECIMAL-POINT IS {written}: {withdrawn.Withdrew("COMMA")}; "
                + withdrawn.WithdrawnSyntax("the DECIMAL-POINT IS COMMA clause", "COMMA"));
        else
            Edition.Error(DiagnosticCatalog.DecimalPointFormInvalid, $"DECIMAL-POINT IS {written}: the only form is "
                + "DECIMAL-POINT IS COMMA (ISO §2023:12.3.7)");
    }

    /// <summary>Populate the switch registry from the SPECIAL-NAMES paragraph's switch-name clauses (ISO §2023:12.3.7
    /// general format: <c>switch-name-1 [IS mnemonic-name-1] [ON [STATUS] [IS] condition-name-1]
    /// [OFF [STATUS] [IS] condition-name-2]</c>; the NIST-85 surface also writes <c>ON IS cond</c> with no STATUS —
    /// both shapes are one grammar rule). Only the switch-names <see cref="ImplementorNames"/> makes available are
    /// accepted (§2023:12.3.7.3 SR8, Annex A.1 item 191 — kb/Work PB862).</summary>
    internal void BindSpecialNames(Core.ProgramUnitContext program)
    {
        Core.ProgramCollatingSequenceClauseContext? pcsClause = null;
        Core.CharacterClassificationClauseContext? ccClause = null;
        var classClauses = new List<Core.ClassDefinitionClauseContext>();
        var symbolicClauses = new List<Core.SymbolicCharactersClauseContext>();
        var currencyClauses = new List<Core.CurrencySignClauseContext>();
        foreach (var para in program.environmentDivision()?.configurationSection()?.configurationParagraph() ?? [])
        {
            // OBJECT-COMPUTER … PROGRAM COLLATING SEQUENCE (ISO §2023:12.3.6 — the 85 single-name IS form, the 2002
            // two-name IS form, and the 2002 FOR ALPHANUMERIC/FOR NATIONAL forms; resolved AFTER the walk). The
            // paragraph's clauses are a list since kb/Work PB78 (either clause, either order, with or without a
            // computer-name). ⛔ HOW OFTEN AND IN WHAT ORDER IS NOT ASKED HERE (kb/Work PB3133): the `ClauseCardinalities`
            // OBJECT-COMPUTER row, read by `ClosedFormatPass`, counts every clause. From 2002 the figure's choice
            // indicators (§2023:5.2.6.4, each alternative at most once, any order) report a repeat as COBOL2104 through
            // `ChoiceIndicators.AtMostOnce`; the COBOL-85 figure's three separate brackets report a repeat as
            // COBOL2423 and a misplaced clause as COBOL2987. This loop used to count the two clauses itself
            // (COBOL1652, deleted) beside a table that exempted the list.
            if (para.objectComputerParagraph() is { } ocp)
            {
                foreach (var oc in ocp.objectComputerClause())
                {
                    if (oc.programCollatingSequenceClause() is { } pcs) pcsClause = pcs;
                    // §2023:12.3.6 CHARACTER CLASSIFICATION (Annex A.4.9 item 7) — IMPLEMENTED since kb/Work PB64 T5 (it was
                    // refused by name with COBOL1518 since PB78, and swallowed by the attribute sink before that).
                    // Saved and resolved AFTER the SPECIAL-NAMES paragraph, whose LOCALE clauses declare the names it
                    // references (§2023:12.3.6.3 SR3) — the PCS takes the same path.
                    if (oc.characterClassificationClause() is { } cc) ccClause = cc;
                }
            }
            if (para.specialNamesParagraph() is not { } sn) continue;
            // The paragraph's symbolic-character NAMES, read syntactically before any clause binds: the clauses are
            // order-free, so an ALPHABET clause may precede the SYMBOLIC CHARACTERS clause that declares a name it
            // writes — and §2023:12.3.7.3 SR11 must still recognize it (kb/Work PB226).
            _paragraphSymbolicNames = sn.specialNameEntry()
                .Select(e => e.symbolicCharactersClause()).OfType<Core.SymbolicCharactersClauseContext>()
                .SelectMany(sc => sc.symbolicCharacterEntry()).SelectMany(e => e.cobolWord())
                .Select(w => w.GetText()).ToHashSet(CobolNames.Comparer);
            // ⛔ DEPENDENCY ORDER, NEVER SOURCE ORDER (kb/Work PB1558). A clause that REFERENCES a name another clause
            // of this paragraph DECLARES binds after every declaring clause, whatever the source order:
            //   1. the walk below — the clauses that reference no paragraph name (LOCALE declares locale-names,
            //      ORDER TABLE ordering-names, the dynamic-length-structure clause its names, …);
            //   2. ALPHABET — references locale-names (§2023:12.3.7.3 SR24 "Locale-name-2 shall be a locale-name defined
            //      by the LOCALE clause") and, syntactically, symbolic-character names (SR11 — read above);
            //   3. after the whole configuration section, CLASS / SYMBOLIC CHARACTERS (their IN alphabet-name), then
            //      CURRENCY SIGN (a symbolic-character concatenation operand in literal-7 — kb/Work PB1628), and
            //      the OBJECT-COMPUTER clauses (PROGRAM COLLATING SEQUENCE's alphabets, CHARACTER CLASSIFICATION's
            //      locale-names).
            // The general format itself PRINTS alphabet-name-clause BEFORE the LOCALE clause (§2023:12.3.7.2; §2023:5.2.1
            // "shall be written in the compilation group in the sequence given in the general format"), so an
            // ALPHABET … IS LOCALE locale-name-2 is a FORWARD reference in every conforming program — and binding
            // ALPHABET in source order refused every one of them with COBOL1664 "no LOCALE clause is in scope".
            var alphabetClauses = new List<Core.AlphabetClauseContext>();
            foreach (var entry in sn.specialNameEntry())
            {
                using var _ = Edition.At(entry);
                // §2023:12.3.7 LOCALE clause (Annex A.4.9 item 10, the clause half) — IMPLEMENTED since kb/Work PB64 T1: the
                // locale-name is DECLARED (SpecialNamesBinder.LocaleBind). It used to be refused by name with COBOL1518
                // (PB25 → T0), and before that a raw parse error at the clause's own literal.
                if (entry.localeClause() is { } loc) { LocaleBind(loc); continue; }
                if (entry.orderTableClause() is { } ot) { OrderTableBind(ot); continue; }
                if (entry.dynamicLengthStructureClause() is { } dls) { DynamicLengthStructureBind(dls); continue; }
                if (entry.alphabetClause() is { } alpha) { alphabetClauses.Add(alpha); continue; }
                // CLASS and SYMBOLIC CHARACTERS bind AFTER the walk (kb/Work PB110): their IN alphabet-name may be
                // declared by a LATER ALPHABET clause of the same paragraph (the clauses are order-free — the
                // ResolveProgramCollating shape).
                if (entry.classDefinitionClause() is { } cd) { classClauses.Add(cd); continue; }
                if (entry.symbolicCharactersClause() is { } sc) { symbolicClauses.Add(sc); continue; }
                if (entry.decimalPointClause() is { } dp) { SwitchBindDecimalPoint(dp); continue; }
                // CURRENCY SIGN binds after SYMBOLIC CHARACTERS (kb/Work PB1628): literal-7 may be a concatenation
                // expression (§2023:8.8.3.3 GR3 makes it "equivalent to a literal"; §2023:12.3.7.3 SR18 bars only a figurative
                // constant AS literal-7, and SR11's symbolic-character bar does not name literal-7/8), and a
                // symbolic-character operand's VALUE is declared by that clause, wherever it stands in the source.
                if (entry.currencySignClause() is { } cur) { currencyClauses.Add(cur); continue; }
                // §2023:12.3.7 CURSOR / CRT STATUS (Annex A.4.2 item 25) — the SCREEN module's environment-division
                // surface. ⚠ Both clauses PARSED and were read by no binder at all until kb/Work PB260, so
                // `CRT STATUS IS WS-CRT` compiled clean with ZERO diagnostics: a declined facility that a program
                // could write and get a clean compile out of is not declined, it is undiagnosed. Refused by name
                // (COBOL1560), the same posture as the SCREEN SECTION they belong to.
                if (entry.cursorClause() is { } curs) { ScreenFacility.ReportCursorClause(Edition, curs); continue; }
                if (entry.crtStatusClause() is { } crt) { ScreenFacility.ReportCrtStatusClause(Edition, crt); continue; }
                if (entry.implementorSwitchEntry() is { } sw) BindImplementorNameEntry(sw);
            }
            foreach (var alpha in alphabetClauses)                        // phase 2 — every locale-name is declared
            {
                using var _ = Edition.At(alpha);
                AlphabetBind(alpha);
            }
        }
        // The PCS resolves AFTER the walk (OBJECT-COMPUTER precedes SPECIAL-NAMES in source, §2023:12.3.6 GR9); only
        // the NAMED alphabets become the program sequences — other defined alphabets have no effect (NC219A's
        // unreferenced COLLATING-SEQ-2). A native-order alphabet leaves the sequence null (the fast path).
        if (pcsClause is not null) ResolveProgramCollating(pcsClause);
        if (ccClause is not null) ResolveClassification(ccClause);
        foreach (var cd in classClauses) SwitchBindClass(cd);           // after the alphabets exist (PB110)
        foreach (var sc in symbolicClauses) SwitchBindSymbolic(sc);
        foreach (var cur in currencyClauses)                             // after the symbolic characters (PB1628)
        {
            using var _ = Edition.At(cur);
            SwitchBindCurrency(cur);
        }
        Cfg.FinalizeCurrencySigns();   // §2023:12.3.7.3 r25 — the implied '$' clause, once every explicit clause is in
    }

    /// <summary>One switch-name / feature-name / device-name entry (ISO §2023:12.3.7.2). §2023:12.3.7.3 SR8 — "<i>The implementor
    /// shall specify the names that are available for switch-name-1, feature-name-1, and device-name-1.</i>" — is
    /// decided HERE, once, from the ONE table (<see cref="ImplementorNames"/>): a name that is not a row, or a
    /// non-switch name written with ON/OFF STATUS, is COBOL2241 (kb/Work PB862) and registers nothing. A switch
    /// row registers its condition-names (§2023:12.3.7.4 GR2) here; EVERY row's mnemonic — a switch's for SET
    /// (§2023:12.3.7.3 SR5) as much as a device's or feature's — is read by the procedure binders through
    /// <c>BinderContext.Mnemonics</c>, which classifies it by the same <see cref="ImplementorNameEntry"/>
    /// (kb/Work PB454: one walk of the entries for mnemonics, not two).</summary>
    private void BindImplementorNameEntry(Core.ImplementorSwitchEntryContext sw)
    {
        var e = ImplementorNameEntry.Read(sw);
        using (Edition.At(sw))   // §2023:8.3.2.2 — the entry's user-defined words, through the one funnel (kb/Work PB1083)
        {
            if (e.Mnemonic is { } mnemonic) _host.DeclareUserWord(mnemonic, UserWordKind.MnemonicName);
            // A switch's condition-name is referenced unqualified only (§2023:12.3.7.4 GR2), so a second declaration of one is
            // §2023:8.4.2.1's ambiguity — asked beside the funnel because ConditionName is also the level-88 type, which
            // qualification tells apart (kb/Work PB3216, UserWordKinds.DeclaredUnqualifiable).
            foreach (var condition in new[] { e.OnCondition, e.OffCondition })
                if (condition is { } conditionName)
                {
                    _host.DeclareUserWord(conditionName, UserWordKind.ConditionName);
                    _host.CheckUnqualifiableDeclaration(conditionName, UserWordKind.ConditionName);
                }
        }
        if (e.Unavailable is { } why)
        {
            Edition.Error(DiagnosticCatalog.UnavailableImplementorName, why);
            return;
        }
        // The mnemonic-name's row, for the one procedure-side reader (ConfigurationSlice.TryGetMnemonic): the first declaration
        // of a spelling wins, and a container's entries come after this unit's own (ConfigurationStep).
        if (e.Mnemonic is { } mnemonicName) Cfg.DeclareMnemonic(mnemonicName, e.Row!);
        if (e.Row!.Kind != SystemNameKind.Switch) return;
        string switchName = e.Row.Name;
        if (e.OnCondition is { } on) Cfg.SwitchConditions.TryAdd(on, (switchName, true));
        if (e.OffCondition is { } off) Cfg.SwitchConditions.TryAdd(off, (switchName, false));
    }

    /// <summary>Resolve the PROGRAM COLLATING SEQUENCE clause (ISO §2023:12.3.6): the IS form's alphabet-name-1
    /// [alphabet-name-2], or the FOR ALPHANUMERIC / FOR NATIONAL forms — each class at most once (§2023:12.3.6.2
    /// braces one of each). SR1: alphabet-name-1 shall reference an alphabet defining an ALPHANUMERIC collating
    /// sequence — a NATIONAL alphabet in the slot is the class-mismatch error and so is a word no ALPHABET clause
    /// declares (kb/Work PB1082; it was inert until then). SR2: alphabet-name-2 shall reference an
    /// alphabet defining a NATIONAL collating sequence — a UTF-8/UTF-16 alphabet references a coded character
    /// set but NO collating sequence (§2023:12.3.7 Table 6), and an alphanumeric-class or undeclared name is the
    /// mismatch; the national slot is a new surface, so it is strict.</summary>
    private void ResolveProgramCollating(Core.ProgramCollatingSequenceClauseContext pcs)
    {
        using var _ = Edition.At(pcs);   // every verdict below is about this clause — report it there
        var (alnumName, natName) = ChoiceIndicators.AlphabetPair(Edition, pcs.collatingForPhrase(),
            f => f.NATIONAL() is not null, f => f.cobolWord().GetText(), pcs.cobolWord(),
            "PROGRAM COLLATING SEQUENCE", "12.3.6.2");

        // ⛔ BOTH SLOTS ASK THE ONE CLASS TEST, CollatingAlphabetFault (kb/Work PB1082): name-1 used to answer only a
        // FOR NATIONAL alphabet and leave every other word inert at every edition — an undeclared word, a data-name, a
        // class-name — so the program silently ran on the native order, while name-2's slot refused the same shapes.
        if (alnumName is not null)
        {
            if (CollatingAlphabetFault(alnumName, national: false) is { } fault)
                Edition.Error(DiagnosticCatalog.CollatingSequenceAlphabetKind, CollatingAlphabetViolation("PROGRAM COLLATING SEQUENCE", alnumName,
                    national: false, fault, "ISO §2023:12.3.6.3 SR1"));
            else if (Cfg.Alphabets.TryUse(pcs, alnumName, out var def)) Cfg.Collating = def.IsIdentity ? null : def;
        }
        if (natName is not null)
        {
            if (CollatingAlphabetFault(natName, national: true) is { } fault)
                Edition.Error(DiagnosticCatalog.CollatingSequenceAlphabetKind, CollatingAlphabetViolation("PROGRAM COLLATING SEQUENCE FOR NATIONAL",
                    natName, national: true, fault, "ISO §2023:12.3.6.3 SR2"));
            else if (Cfg.NationalAlphabets.TryUse(pcs, natName, out var def))
                Cfg.NationalCollating = def.IsIdentity ? null : def;   // null for NATIVE/UCS-4 — the identity fast path (D-N3)
        }
    }

    /// <summary>Declare one <c>DYNAMIC LENGTH [STRUCTURE] name [IS] {…}</c> clause (ISO §2023:12.3.7.2; kb/Work PB829):
    /// the layout is read off the choice-indicator group — PREFIXED and DELIMITED, one or more, each at most once
    /// (§2023:5.2.6.4, through the one reader <see cref="ChoiceIndicators.AtMostOnce{T}"/>) — or is the
    /// physical-structure-name-1 alternative, whose allowable names §2023:12.3.7.3 SR32 leaves to the implementor and
    /// this implementor provides NONE of (docs/CONFORMANCE.md §3 D-DL3), so that alternative is refused by name.
    /// A second declaration of one name in the same source element is refused by the declaration funnel
    /// (<see cref="DataBinder.DeclareUserWord"/>, kb/Work PB3216): every reference to it would fail §2023:8.4.2.1's "a reference that
    /// uniquely identifies that resource". A containing element's inherited names may be re-declared (§2023:8.4.6.1).</summary>
    private void DynamicLengthStructureBind(Core.DynamicLengthStructureClauseContext dls)
    {
        if (dls.cobolWord() is not { } nameCtx || dls.dynamicLengthLayout() is not { } layout) return;   // a parse error already
        string name = nameCtx.GetText();
        using (Edition.At(nameCtx)) _host.DeclareUserWord(name, UserWordKind.DynamicLengthStructureName);   // §2023:8.3.2.2 (kb/Work PB1083)
        string where = $"DYNAMIC LENGTH STRUCTURE {name}";
        DynamicLengthStructure declared;
        if (layout.cobolWord() is { } physical)
        {
            Edition.Error(DiagnosticCatalog.DynamicLengthStructureInvalid,
                $"{where} IS {physical.GetText()}: '{physical.GetText()}' is not a physical-structure-name this "
                + "implementation supports — ISO §2023:12.3.7.3 SR32 (\"The implementor shall specify the names supported "
                + "for physical-structure-name-1\") leaves the names to the implementor and WiseOwl COBOL supports none; "
                + "describe the layout with the PREFIXED and/or DELIMITED phrases (docs/CONFORMANCE.md §3 D-DL3)");
            declared = new DynamicLengthStructure(name, DynamicLengthPrefix.None, Delimited: false, physical.GetText());
        }
        else
        {
            var prefixed = ChoiceIndicators.AtMostOnce(Edition, layout.dynamicLengthPrefixedPhrase(), where,
                "the PREFIXED phrase", "12.3.7.2");
            var delimited = ChoiceIndicators.AtMostOnce(Edition, layout.dynamicLengthDelimitedPhrase(), where,
                "the DELIMITED phrase", "12.3.7.2");
            var prefix = prefixed is null ? DynamicLengthPrefix.None
                : (prefixed.SIGNED() is not null, prefixed.dynamicLengthShortWord() is not null) switch
                {
                    (false, false) => DynamicLengthPrefix.Unsigned32,
                    (true, false) => DynamicLengthPrefix.Signed32,
                    (false, true) => DynamicLengthPrefix.Unsigned16,
                    (true, true) => DynamicLengthPrefix.Signed16,
                };
            declared = new DynamicLengthStructure(name, prefix, Delimited: delimited is not null, null);
        }
        // A second declaration of one name was refused by the declaration funnel (DeclareUserWord, ISO §2023:8.4.2.1; kb/Work
        // PB3216): the first stands, so a repeat sets nothing — and an INHERITED same-named structure is still replaced
        // by this element's first (§2023:8.4.6.1), since the census counts this element's own declarations alone.
        if (_host.WordCensus.UnqualifiedDeclarations(name, UserWordKind.DynamicLengthStructureName) > 1) return;
        Cfg.DynamicLengthStructures.Set(name, declared);   // replaces an INHERITED same-named structure (§2023:8.4.6.1)
    }

    private void OrderTableBind(Core.OrderTableClauseContext ot)
    {
        // ONE cobolWord: ordering-name-1. The clause's own keywords ORDER and TABLE are both lexer tokens since
        // kb/Work PB704 — ORDER used to ride cobolWord as slot [0], which is exactly the shape the §2023:8.9 funnel
        // mistook for a user-defined word.
        if (ot.cobolWord() is not { } name1 || ot.specialNamesLiteral() is not { } operand9) return;   // a malformed shape already drew a parse error
        string name = name1.GetText();
        _host.DeclareUserWord(name, UserWordKind.OrderingName);   // §2023:8.3.2.2 — the one declaration funnel (kb/Work PB1083)
        // literal-9, or a constant-name standing for it (§2023:13.10.3 SR2; kb/Work PB1942).
        Antlr4.Runtime.Tree.IParseTree written9 = (Antlr4.Runtime.Tree.IParseTree?)operand9.literal() ?? operand9.cobolWord();
        string raw = operand9.literal() is { } lit ? ConcatFolder.Spelling(lit) : operand9.GetText();
        // SR10 / SR11 for literal-9 — the ONE text-literal rule the LOCALE clause's literal-4 shares.
        if (!TryClauseTextLiteral(written9, $"ORDER TABLE {name} IS {raw}", "literal-9", out string text)) return;
        // ⚠ ONE CLAUSE PER PARAGRAPH, measured off the PRINTED general format rather than inferred from GR17's
        // singular wording: §2023:12.3.7.2 brackets `ORDER TABLE ordering-name-1 IS literal-9` with NO trailing
        // ellipsis, where the repeatable clauses beside it (CLASS, CURRENCY, LOCALE, the switch entry, SYMBOLIC
        // CHARACTERS) each print one. A second clause is therefore a form violation, and so — as a consequence
        // rather than as a second rule — is a second ordering-name.
        if (Cfg.OrderTables.Count > 0)
        {
            Edition.Error(DiagnosticCatalog.OrderTableRepeated, $"ORDER TABLE {name}: the ORDER TABLE clause may be specified only once "
                + "in a SPECIAL-NAMES paragraph — its general format brackets it without an ellipsis, unlike the "
                + $"repeatable clauses (ISO §2023:12.3.7.2); '{Cfg.OrderTables.Names.First()}' is already declared");
            return;
        }
        Cfg.OrderTables.Set(name, text);
        // GR17's implementor half, checked against the ONE resolver the runtime uses (§2023:15.85.4 r2 owns the outcome).
        // A site collation file that cannot be used is that resolver's typed failure, named here with its file, never
        // a crash of the compile (kb/Work PB2773).
        if (CollationEngine.TryGetOrderingTable(text, out _)) return;
        string? failure = CollationEngine.ResolveLocale(text).Failure?.Message;
        Edition.Warning(DiagnosticCatalog.OrderTableUnresolved, $"ORDER TABLE {name} IS {raw}: "
            + (failure is null
                ? $"'{text}' does not name a cultural ordering table this implementation provides (ISO §2023:12.3.7.4 "
                  + "GR17 leaves literal-9's allowable content to the implementor: the default table "
                  + $"'{CollationEngine.DefaultOrderingTableName}' — case-insensitive, space and underscore "
                  + "interchangeable — or a CLDR locale tag naming a tailored collation)"
                : $"the site collation data for '{text}' cannot be used ({failure}), so it names no cultural "
                  + "ordering table this implementation provides (ISO §2023:12.3.7.4 GR17 leaves literal-9's allowable "
                  + "content to the implementor)")
            + $". Every FUNCTION STANDARD-COMPARE reference to {name} will set EC-ORDER-NOT-SUPPORTED at run time "
            + "(§2023:15.85.4 r2)");
    }
}

/// <summary>A SPECIAL-NAMES user-defined class (ISO §2023:12.3.7.2 CLASS clause): its EXPANDED member characters and
/// whether it was declared <c>FOR NATIONAL</c> (§2023:12.3.7.3 SR17 a — ALPHANUMERIC is implied otherwise).</summary>
public sealed record UserClassDef(string Members, bool National);
