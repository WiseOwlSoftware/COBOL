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

/// <summary>The ALPHABET clause and the alphabet-name facilities (ISO §12.3.7.2 ALPHABET, §12.3.7.3 SR14-SR16, §12.3.7.4 GR7; the THROUGH range's IN phrase, §14.9.13.3 SR3).
/// A partial of <see cref="SpecialNamesBinder"/>.</summary>
public sealed partial class SpecialNamesBinder
{
    /// <summary>⛔ THE per-TYPE runtime carriers a THROUGH range's <c>IN alphabet-name-1</c> phrase needs (ISO
    /// §14.7.8 rule 2 — "the collating sequence used for range evaluation is the collating sequence defined by that
    /// alphabet"), keyed by alphabet-name as written. One carrier per NAMED alphabet per runtime module, declared
    /// beside <c>__COLLATE</c> by <c>ObjectComputerEmit</c>.
    /// <para>⛔ A FIELD, NOT AN INLINE CARRIER, and that is a correctness-adjacent performance rule rather than a
    /// preference: a range test sits inside a WHEN pair that is re-analysed on every execution of the EVALUATE, and
    /// rendering <c>new AlphanumericCollation(new ushort[256]{…}, …)</c> there would allocate a fresh 256-entry
    /// table per evaluation. SORT / MERGE / the indexed-file key registrations render their alphabet inline because
    /// each of those runs once per statement; a WHEN does not. The map is populated at BIND, so the field exists
    /// before any body that names it is rendered.</para></summary>
    public Dictionary<string, RangeCollationCarrier> RangeCollations { get; } = new(CobolNames.Comparer);

    /// <summary>Record <paramref name="name"/>'s ALPHANUMERIC alphabet as a range collating sequence — with no
    /// carrier when the alphabet is the native order (an identity sequence needs none: §14.7.8 rule 2's "the
    /// collating sequence defined by that alphabet" IS the native one, and the two-argument
    /// <c>CobolString.Compare</c> overload is it).</summary>
    /// <para>⛔ EVERY ACCEPTED ALPHABET GETS AN ENTRY, the two that need no declaration of their own included — an
    /// identity sequence (entry with a NULL field: the native comparison IS "the collating sequence defined by that
    /// alphabet") and one that IS the program collating sequence (whose <c>__COLLATE</c> is already declared). The
    /// renderer looks the NAME up, so an accepted name with no entry falls back to the category-derived sequence
    /// and silently reinstates the very default the phrase overrides — measured, while these two cases returned
    /// early instead of recording themselves.</para>
    private void RegisterRangeCollation(string name, AlphabetDef def)
        => Register(name, ReferenceEquals(def, Cfg.Collating) ? "__COLLATE" : null, def.Table, def.Locale, National: false);

    /// <inheritdoc cref="RegisterRangeCollation(string,AlphabetDef)"/>
    private void RegisterRangeCollation(string name, NationalAlphabetDef def)
        => Register(name, ReferenceEquals(def, Cfg.NationalCollating) ? "__COLLATE_NAT" : null,
            def.Table, def.Locale, National: true);

    /// <summary>⛔ THE ONE resolution of a THROUGH range's <c>IN alphabet-name-1</c> phrase, for EVERY clause that
    /// prints one — §14.9.13.2's range-expression and §13.18.63.2 formats 3 and 5 — because §14.7.8 opens with
    /// "This specification applies to THROUGH phrases specified in the VALUE clause and the EVALUATE statement":
    /// ONE sentence governing both, so one resolver. It enforces both halves of §14.9.13.3 SR3 (§13.18.63.3 SR31 is
    /// the same rule for the VALUE clause) and registers the runtime carrier.
    /// <para>Returns true when the phrase is accepted, in which case <paramref name="alphabetName"/> is what the
    /// bound node carries; false when it was refused, and the range then keeps the implementor default sequence
    /// (§14.7.8 rule 2's no-phrase arm) rather than acquiring a second, invented error mode.</para></summary>
    /// <param name="alphabetName">alphabet-name-1 as written.</param>
    /// <param name="rangeClass">The comparison class of the range's operands, from the ONE comparison-class rule
    /// (<see cref="CollatingSelection.ForComparison"/> over BOTH bounds' categories) — SR3's "of class alphabetic,
    /// alphanumeric, or national" test and its national-vs-alphanumeric split are the same question that rule
    /// answers, so it is asked once and passed in.</param>
    /// <param name="where">The construct, for the diagnostic ("EVALUATE WHEN range", "condition-name 'X'").</param>
    /// <summary>⛔ THE EDITION GATE OF THE <c>[ IN alphabet-name-1 ]</c> RANGE PHRASE — a COBOL-2002 addition
    /// (constructs.json <c>range-in-alphabet-2002</c>; the edge is DERIVED and the derivation is the row's, kb/Work
    /// PB1015). It sits at the one resolver and not in the parse arm because the phrase is decided by SYMBOL: IN is
    /// an optional word (PB983), so `THRU "M" AL` is the phrase only when AL names an alphabet, and an EVALUATE
    /// identifier-4 `IN AL` parses as a qualification until EvaluateBinder peels it (PB843). No recognition-time
    /// arm can see those spellings; every spelling reaches here once it IS the phrase. The VALUE clause's
    /// no-THROUGH refusal (§13.18.63.3 SR31, DiagnosticCatalog.ValueAlphabetWithoutThrough) calls it too, so a phrase the
    /// binder refuses for another reason still names its edition.</summary>
    internal void GateRangeAlphabetPhrase(string where) =>
        ConstructRegistry.Check(Edition.Edition, Edition.Sink, Constructs.RangeInAlphabet2002, where);

    public bool TryResolveRangeAlphabet(Antlr4.Runtime.RuleContext site, string alphabetName, CollatingClass rangeClass,
        string where)
    {
        // The TYPE first: a word that names no alphabet at all is not alphabet-name-1, whatever the range's class —
        // and since IN is an optional word (kb/Work PB983), a stray trailing word after a THROUGH range reaches here
        // too, so it must be told it names nothing rather than that the range's class forbids a phrase it never was.
        if (!IsAlphabetName(alphabetName))
        {
            Edition.Error(DiagnosticCatalog.RangeAlphabetUndeclared, $"{where}: '{alphabetName}' after the THROUGH "
                + "phrase is in the [ IN alphabet-name-1 ] position, but it does not name an alphabet declared in "
                + "SPECIAL-NAMES (ISO §12.3.7; §14.9.13.3 SR3)");
            return false;
        }
        GateRangeAlphabetPhrase(where);
        // SR3 sentence 1 — the phrase is admitted only over a range a COLLATING SEQUENCE can order. §14.7.8 rule 1
        // orders a numeric range algebraically and names no sequence at all, and rule 2 (the sequence rule) is
        // written for "alphanumeric or national literals"; a boolean range cannot carry THROUGH in the first place
        // (§14.9.13.3 SR4 / §13.18.63.3 SR29). ⛔ THE SAME PREDICATE THE EC GATES ASK — rule 2 owns the sequence,
        // the IN phrase and the exception together, so the three sites read one antecedent (kb/Work PB401).
        if (!CollatingSelection.IsCollatedThroughRange(rangeClass))
        {
            Edition.Error(DiagnosticCatalog.RangeAlphabetOperandClass, $"{where}: 'IN {alphabetName}' may be "
                + "specified only when the operands of the THROUGH phrase are of class alphabetic, alphanumeric or "
                + "national (ISO §14.9.13.3 SR3 / §13.18.63.3 SR31)");
            return false;
        }
        // SR3 sentence 2 — the alphabet's class shall be the range's. The two domains are disjoint (§12.3.6
        // SR1/SR2), so the lookup itself IS the class test: a national range resolves in NationalAlphabets, an
        // alphanumeric one in Alphabets, and finding the name in the OTHER map is the violation, not an absence.
        bool national = rangeClass is CollatingClass.National;
        if (national)
        {
            if (Cfg.NationalAlphabets.TryUse(site, alphabetName, out var nd))
            {
                // §12.3.7.4 Table 6 — UTF-8 / UTF-16 name a coded character set only; their collating-sequence
                // column is empty, so they define no sequence this phrase could use.
                if (!nd.HasCollatingSequence)
                {
                    Edition.Error(DiagnosticCatalog.RangeAlphabetClassMismatch, $"{where}: alphabet '{alphabetName}' "
                        + $"({nd.Phrase}) names a national CODED CHARACTER SET only — it defines no national "
                        + "collating sequence (ISO §12.3.7.4 GR7 Table 6; §14.9.13.3 SR3)");
                    return false;
                }
                RegisterRangeCollation(alphabetName, nd);
                return true;
            }
        }
        else if (Cfg.Alphabets.TryUse(site, alphabetName, out var ad))
        {
            RegisterRangeCollation(alphabetName, ad);
            return true;
        }
        // The name IS an alphabet (the type test above) but not in this class's domain, so it is declared in the
        // other one — SR3 sentence 2's violation, told apart from a word that is no alphabet at all so the message
        // names the rule the program actually broke.
        Edition.Error(DiagnosticCatalog.RangeAlphabetClassMismatch, $"{where}: the THROUGH operands are of class "
            + $"{(national ? "national" : "alphabetic / alphanumeric")}, so alphabet-name-1 shall reference an "
            + $"alphabet that defines {(national ? "a national" : "an alphanumeric")} collating sequence — "
            + $"'{alphabetName}' declares the other class (ISO §14.9.13.3 SR3 / §13.18.63.3 SR31)");
        return false;
    }

    /// <param name="known">The field that ALREADY names this sequence (<c>__COLLATE</c> / <c>__COLLATE_NAT</c>), or
    /// null when a fresh carrier is needed. An identity alphabet takes neither, and is recognised here — from both
    /// components being absent — rather than at each caller.</param>
    private void Register(string name, string? known, CollatingTable? table, LocaleCollatingSpec? locale, bool National)
    {
        if (RangeCollations.ContainsKey(name)) return;
        bool identity = table is null && locale is null;
        // An ORDINAL field name, not one derived from the COBOL word: a user-defined word admits both '-' and '_'
        // (§8.3.2.1), so any sanitizing map could collide two distinct alphabet-names onto one field.
        string? field = identity ? null : known ?? $"__COLLATE_R{RangeCollations.Count}";
        RangeCollations[name] = new RangeCollationCarrier(field, name, table, locale, National,
            Declare: field is not null && known is null);
    }

    /// <summary>Is <paramref name="alphabetName"/> an alphabet "associated with a locale" (ISO §8.8.4.4.3 SR2) /
    /// "specified with the LOCALE phrase" (§12.3.7.3 SR16g / SR17d) — of EITHER class (<c>ALPHABET … IS LOCALE</c>
    /// registers in <see cref="ConfigurationSlice.Alphabets"/>, <c>ALPHABET … FOR NATIONAL IS LOCALE</c> in <see cref="ConfigurationSlice.NationalAlphabets"/>)?
    /// Such an alphabet defines a collating sequence only (§12.3.7.4 GR7, Table 6: the LOCALE row names no coded
    /// character set), so the ONE question every site that needs a CODED CHARACTER SET asks is this one (kb/Work PB64
    /// T5 — the class condition's alphabet-name-1 today; the CLASS / SYMBOLIC CHARACTERS <c>IN</c> phrases and CODE-SET
    /// when they bind, kb/Work PB110). An undeclared name is not a locale alphabet — its own diagnostic is elsewhere.
    /// A predicate, recording no §8.3.2.2 use (kb/Work PB3153): a true answer is a refusal at every asker.</summary>
    public bool IsLocaleAlphabet(string alphabetName)
        => (Cfg.Alphabets.TryPeek(alphabetName, out var a) && a.Locale is not null)
        || (Cfg.NationalAlphabets.TryPeek(alphabetName, out var n) && n.Locale is not null);

    /// <summary>⛔ THE ONE resolver of an alphabet-name referenced where a CODED CHARACTER SET is required (ISO
    /// §12.3.7.4 GR7 Table 6; kb/Work PB110 — the class condition's alphabet-name-1, SYMBOLIC CHARACTERS … IN,
    /// CLASS … IN, CODE-SET): the set of the named alphabet of either class, with the two refusals every site
    /// shares — a LOCALE alphabet defines a collating sequence only (Table 6's blank row → COBOL1669, citing
    /// <paramref name="rule"/>), and an undeclared name is COBOL0898 naming the site. Null on either refusal.
    /// <paramref name="at"/> is the reference, whose element's §8.3.2.2 census records the use (kb/Work PB3153).</summary>
    internal CodedCharacterSet? CodedCharacterSetOf(Antlr4.Runtime.RuleContext at, string alphabetName, string site,
        string rule)
    {
        AlphabetDef? a = Cfg.Alphabets.TryUse(at, alphabetName, out var av) ? av : null;
        NationalAlphabetDef? n = a is null && Cfg.NationalAlphabets.TryUse(at, alphabetName, out var nv) ? nv : null;
        if (a is null && n is null)
        {
            Edition.Error(DiagnosticCatalog.AlphabetNameNotDeclared, $"{site}: '{alphabetName}' is not an alphabet-name declared by a "
                + $"SPECIAL-NAMES ALPHABET clause ({rule}; §12.3.7)");
            return null;
        }
        var set = a is not null ? a.CodedSet : n!.CodedSet;   // a LOCALE alphabet has a null CodedSet — never fall to the OTHER class
        if (set is null)
        {
            Edition.Error(DiagnosticCatalog.LocaleAlphabetNotACharacterSet, $"{site}: the alphabet '{alphabetName}' "
                + $"is associated with a locale — an ALPHABET … IS LOCALE defines a collating sequence, not a coded "
                + $"character set (§12.3.7.4 GR7, Table 6), so it cannot be referenced here ({rule})");
            return null;
        }
        return set;
    }

    /// <summary>Build one <c>ALPHABET name IS …</c> clause (ISO §12.3.7 GR7): NATIVE / STANDARD-1 / STANDARD-2 are
    /// the native (ISO/IEC 646) order — no table; a literal phrase assigns successive ascending positions per
    /// k)1–k)6 — a numeric literal is the 1-based NATIVE ordinal (k1a), a multi-character literal positions each
    /// character leftmost-first (k1b), THRU expands the native run in EITHER direction (k5), ALSO members share
    /// ONE position (k6), and every unspecified character takes a DISTINCT ascending position above the highest
    /// specified, in native relative order (k3). HIGH-/LOW-VALUE written INSIDE the clause are the NATIVE extremes
    /// (GR10 — the PCS re-derivation applies only outside SPECIAL-NAMES).</summary>
    private void AlphabetBind(Core.AlphabetClauseContext alpha)
    {
        // ALPHABET … FOR ALPHANUMERIC/NATIONAL — the FOR phrase edition gate is now VersionConformancePass
        // ParseArm.VisitAlphabetClause (14g.4, recognition), as is the UCS-4/UTF-8/UTF-16 phrase gate
        // (alphabet-national-2002). The ISO position for the FOR phrase is between the name and IS
        // (§12.3.7.2); a FOR phrase after the definition is refused by name in ClosedFormatPass (kb/Work PB977)
        // and read here only to recover.
        string name = alpha.cobolWord().GetText();
        _host.DeclareUserWord(name, UserWordKind.AlphabetName);   // §8.3.2.2 — the one declaration funnel (kb/Work PB1083)
        var def = alpha.alphabetDefinition();
        bool national = ForPhraseIsNational(alpha.specialNamesForPhrase(), alpha.misplacedSpecialNamesForPhrase());
        // `IS LOCALE [locale-name-2]` — either branch (§12.3.7.2): Annex A.4.9 item 10 ("LOCALE phrases in the
        // ALPHABET clause"). The grammar recognizes it as its own rule — LOCALE is the phrase's KEYWORD, a formatWord
        // (kb/Work PB764; PB100 fixed the false "reserved word used as a user-defined word" it used to draw) — at every
        // edition, and below 2002 the ONE construct gate (VersionConformancePass, alphabet-locale-2002) refuses it, so
        // there is nothing to bind there.
        if (def.alphabetLocalePhrase() is { } localePhrase)
        {
            if (Edition.DialectLevel < 2002) return;
            // `IS LOCALE` (no locale-name-2) — the LOCALE-based collating sequence of the locale CURRENT at each use
            // (§12.3.7.4 GR7e; §12.3.6.4 GR11/GR12): the runtime LocaleCollation over the derived CLDR/UCA engine (kb/Work
            // PB101 — the T3 arm; determination L5 makes one locale sequence serve both classes). Table 6 row LOCALE: a
            // collating sequence, NOT a coded character set — CODE-SET / SYMBOLIC … IN / CLASS … IN may not name it
            // (§12.3.7.3 SR16g/SR17d); the ONE coded-set resolver (CodedCharacterSetOf) refuses it for all three with
            // COBOL1669, because AlphabetDef.CodedSet is null for this arm.
            // `IS LOCALE locale-name-2` (§12.3.7.3 SR24: "locale-name-2 shall be a locale-name defined by the LOCALE clause")
            // — the NAMED form (T1): the sequence of THAT locale, whose external identification the symbol carries and
            // whose availability is decided at use (EC-LOCALE-MISSING). An undeclared name is COBOL1664 and the
            // alphabet falls back to the current-locale form so the rest of the program still binds.
            var locale = LocaleCollatingSpec.CurrentLocale;
            if (localePhrase.cobolWord() is { } localeName)
            {
                var sym = ResolveLocaleName(localeName, localeName.GetText(), $"ALPHABET {name}{(national ? " FOR NATIONAL" : "")} IS LOCALE {localeName.GetText()}",
                    "ISO §12.3.7.3 SR24 — locale-name-2 shall be a locale-name defined by the LOCALE clause");
                if (sym is not null) locale = new LocaleCollatingSpec(new LocaleRef(sym));
            }
            if (national) Cfg.NationalAlphabets.TryAdd(name, new NationalAlphabetDef(null, locale, HasCollatingSequence: true, "LOCALE"));
            else Cfg.Alphabets.TryAdd(name, new AlphabetDef(null, locale, "LOCALE"));
            return;
        }
        if (national)
        {
            AlphabetBindNational(name, def);
            return;
        }

        // The ALPHANUMERIC branch (explicit FOR ALPHANUMERIC, or implied — §12.3.7.3 SR13). The national
        // coded-set names are NOT in this branch's format (§12.3.7.2 admits them only after FOR NATIONAL) —
        // intercept them rather than mis-binding their letters as literal characters.
        if (CodeSetNameOf(def, _host.CobolWords) is { } wrongBranch)
        {
            Edition.Error(DiagnosticCatalog.AlphabetCodeSetBranch, $"ALPHABET {name} IS {wrongBranch}: the {wrongBranch} coded character "
                + "set may be referenced only in the FOR NATIONAL branch of the ALPHABET clause "
                + "(ISO §12.3.7.2 general format)");
            return;
        }
        if (def.NATIVE() is not null || def.STANDARD_1() is not null || def.STANDARD_2() is not null)
        {
            // One identity COLLATING SEQUENCE, three distinct CODED CHARACTER SETS (Table 6 + GR7 c/d: STANDARD-1/2
            // are the 128 ISO/IEC 646 IRV characters; NATIVE is the whole native set) — the Phrase carries which
            // (kb/Work PB110); IsIdentity stays true, so no runtime carrier is emitted either way.
            Cfg.Alphabets.TryAdd(name, def.NATIVE() is not null ? AlphabetDef.Native
                : new AlphabetDef(null, null, def.STANDARD_1() is not null ? "STANDARD-1" : "STANDARD-2"));
            return;
        }

        // A bare word that is not NATIVE / STANDARD-1 / STANDARD-2 / LOCALE and is not a figurative constant is
        // code-name-1 (§12.3.7.2 general format), resolved against the implementor code-name table (§12.3.7.3
        // SR15) — the ONE lookup, shared with the national arm through AlphabetCodeName. A supported name carries
        // its collating sequence in the row's Table (null = the native order, GR7 i's positions being the native
        // ones) and its coded character set through the row itself (§12.3.7.4 GR7 i, Table 6's two Y columns).
        switch (AlphabetCodeName(name, def, national: false, out var codeName))
        {
            case CodeNameOutcome.Refused: return;
            case CodeNameOutcome.Bound:
                Cfg.Alphabets.TryAdd(name, new AlphabetDef(codeName!.Table, null, codeName.Name, codeName));
                return;
        }

        if (AlphabetLiteralPhrase(name, def, national: false) is { } table)
            Cfg.Alphabets.TryAdd(name, new AlphabetDef(table, null, "literal-phrase"));
    }

    /// <summary>What a bare-word alphabet definition turned out to be once <see cref="AlphabetCodeName"/> asked
    /// the implementor code-name table about it (ISO §12.3.7.3 SR15).</summary>
    private enum CodeNameOutcome
    {
        /// <summary>The definition is not the one-bare-word shape code-name-1/-2 has — the caller continues to
        /// the literal-phrase reading.</summary>
        NotACodeName,

        /// <summary>A supported code-name; <c>row</c> carries its set and sequence.</summary>
        Bound,

        /// <summary>A bare word that is no supported code-name — refused, and the caller binds nothing.</summary>
        Refused,
    }

    /// <summary>⛔ THE ONE §12.3.7.3 SR15 RESOLUTION, for code-name-1 (alphanumeric) and code-name-2 (national)
    /// alike: "<i>The implementor shall specify the names supported for code-name-1 and code-name-2 in the ALPHABET
    /// clause, if any.</i>" The supported set is <see cref="ImplementorCodeNames"/> — a TABLE, so this method is a
    /// LOOKUP with a refusal on miss and the next supported code-name is a row there, never an arm here
    /// (owner decision, kb/Work PB793; CLAUDE.md rule 5). The words that may stand alone in an alphabet definition
    /// besides a code-name are the general format's own keywords (NATIVE, STANDARD-1, STANDARD-2, UCS-4, UTF-8,
    /// UTF-16, LOCALE), which the callers have already consumed, and the figurative constants of a one-operand
    /// literal phrase (§12.3.7.4 GR10).
    /// <para>⛔ An UNSUPPORTED word was silently REINTERPRETED as the characters of its own spelling until
    /// kb/Work PB770: <c>ALPHABET A-ASC IS ASCII</c> built an alphabet whose first four positions were A, S, C, I,
    /// and every downstream reference (PROGRAM COLLATING SEQUENCE, SORT, SYMBOLIC CHARACTERS … IN) read that. A
    /// bare word is not a literal at all (SR14 b2/c2 require an alphanumeric / national literal), so the
    /// literal-phrase reading is not even available to fall back on.</para>
    /// <para>The check is deliberately narrow: it fires only on a definition that is ONE bare word, which is the
    /// only shape code-name-1/-2 has in the general format. A word inside a multi-operand phrase is a figurative
    /// constant or an SR14 b2/c2 violation, and <see cref="AlphabetOperands"/> owns that.</para></summary>
    private CodeNameOutcome AlphabetCodeName(string name, Core.AlphabetDefinitionContext def, bool national,
        out ImplementorCodeName? row)
    {
        row = null;
        // A word the unit declares as a constant-name is literal-1 of a literal phrase (§13.10.3 SR2; kb/Work PB1942) —
        // ⚠ DETERMINATION: the program's own declaration of the word wins over the implementor's code-name of the same
        // spelling, as it does for the LOCALE clause's external-locale-name-1 (LocaleBind).
        if (def.alphabetEntry() is not [{ ChildCount: 1 } only] || only.GetChild(0) is not Core.CobolWordContext cw
            || IsAlphabetFigurativeWord(cw.GetText()) || _host.NamesConstant(cw.GetText()))
            return CodeNameOutcome.NotACodeName;
        if (ImplementorCodeNames.Find(cw.GetText(), national) is { } hit) { row = hit; return CodeNameOutcome.Bound; }
        Edition.Error(DiagnosticCatalog.AlphabetCodeNameUnsupported, $"ALPHABET {name}{(national ? " FOR NATIONAL" : "")} "
            + $"IS {cw.GetText()}: not a supported code-name — the {(national ? "code-name-2" : "code-name-1")} "
            + $"names this implementation supports are: {ImplementorCodeNames.Spellings(national)} "
            + $"(ISO §12.3.7.3 SR15; the {(national ? "national coded character set keywords are NATIVE, UCS-4, UTF-8, and UTF-16" : "alphanumeric coded character set keywords are NATIVE, STANDARD-1, and STANDARD-2")})");
        return CodeNameOutcome.Refused;
    }

    /// <summary>The figurative constants a literal phrase may name (ISO §12.3.7.4 GR10 — "<i>When specified as
    /// literals in the SPECIAL-NAMES paragraph, the figurative constants HIGH-VALUE and LOW-VALUE are associated
    /// with those characters having the highest and lowest positions … in the native national collating sequence,
    /// when the NATIONAL phrase is specified, or in the native alphanumeric collating sequence otherwise</i>"; the
    /// remaining figurative words are §8.3.3.6 constants of the clause's class). ONE list for both arms — the
    /// national arm used to keep its own copy under a different name.</summary>
    private static bool IsAlphabetFigurativeWord(string word) => AlphabetFigurative(word) is not null;

    /// <summary>⛔ THE ONE literal-phrase alphabet builder (ISO §12.3.7.4 GR7 k), for the ALPHANUMERIC arm and the
    /// <c>FOR NATIONAL</c> arm alike. GR7 k states its six sub-rules once, "<i>where the native coded character set
    /// is the type of coded character set or collating sequence being defined, either alphanumeric or national</i>",
    /// and both native sets are the 65,536 UTF-16 code units here (implementor item 188 / D-N1) — so there is ONE
    /// builder and the <paramref name="national"/> flag reaches only the diagnostics and the literal CLASS rule.</summary>
    /// <remarks>k1a — a numeric literal is the 1-based ordinal in the native set; k1b — a (possibly multi-character)
    /// literal takes successive ascending positions leftmost-first; k2 — the operand order IS the ascending position
    /// order; k3 — every UNSPECIFIED character follows the highest specified one, in native relative order (realized
    /// SPARSELY: the runtime computes it arithmetically); k5 — THROUGH expands the native run in EITHER direction;
    /// k6 — ALSO members share ONE position, of which literal-1 is the first character defined.
    /// <para>⛔ §12.3.7.3 SR14 a is enforced HERE, in <c>Assign</c>: "<i>A given character shall not be specified
    /// more than once in that ALPHABET clause.</i>" Both arms used to carry a <c>// diagnostic later</c> comment at
    /// exactly this line and silently keep the first occurrence (kb/Work PB770 leg a). "First wins" survives as the
    /// RECOVERY posture so the rest of the program still binds; it is no longer the answer.</para>
    /// <para>⛔ SR14 b4/c4 — "<i>The number of characters specified shall not exceed the number of characters in the
    /// native … character set</i>" — needs no separate check: SR14 a makes every specified character distinct, and
    /// distinct characters of a set cannot outnumber the set. Enforcing a) enforces it.</para></remarks>
    /// <returns>The table, or null when the phrase specified nothing (every operand failed its syntax rule and the
    /// diagnostics are already reported).</returns>
    private CollatingTable? AlphabetLiteralPhrase(string name, Core.AlphabetDefinitionContext def, bool national)
    {
        var rules = LiteralPhraseRules.Alphabet(name, national);
        string what = rules.What;
        var pos = new Dictionary<char, int>();
        var specOrder = new List<char>();       // every specified character in source order (the GR8/GR9 tie rules)
        var repByPos = new List<char>();        // per position: the FIRST character DEFINED there (§15.15.4 r2 / GR7 k6)
        // ⛔ An int, never a ushort (kb/Work PB1557): the position COUNT reaches 65,536 when the phrase specifies
        // every native character (SR14 b4/c4's bound is "shall not exceed", so equal is legal), one past what a
        // 16-bit counter holds — it wrapped to 0 and the whole table lost its specified block.
        int next = 0;
        void Assign(char c, bool advance)
        {
            if (pos.ContainsKey(c))
            {
                // SR14 a. The recovery posture is the historical one — the FIRST specification wins — so the
                // sequence stays well-formed and the rest of the source still binds.
                Edition.Error(DiagnosticCatalog.AlphabetClauseViolation, $"{what}: the character {DescribeChar(c)} is "
                    + "specified more than once — a given character shall not be specified more than once in that "
                    + "ALPHABET clause (ISO §12.3.7.3 SR14 a)");
                return;
            }
            pos[c] = next;
            specOrder.Add(c);
            if (repByPos.Count == next) repByPos.Add(c);   // the first occupant of the position wins (ALSO literal-1)
            if (advance) next++;
        }

        foreach (var entry in def.alphabetEntry())
        {
            bool thru = entry.THRU() is not null || entry.THROUGH() is not null;
            if (thru && entry.ALSO().Length > 0)
            {
                // ⛔ ONE ENTRY, ONE PHRASE (kb/Work PB790; RENDERED — PDF p321 / folio 291). The literal-phrase
                // figure stacks `{THROUGH|THRU} literal-2` and `{ALSO literal-3}…` inside ONE pair of square brackets
                // with no choice indicators, so §5.2.6.2 admits "one of the alternatives contained within the
                // brackets" — never both. The grammar parses the union (superset-parse / bind-narrow) so the refusal
                // can name its rule; it used to reach the k5 branch below, which `continue`d past the ALSO operands
                // and left them at the GR7 k3 unspecified positions with no diagnostic. There is no merged reading
                // to recover to — k5 and k6 place operands by incompatible rules — so the entry contributes nothing.
                Edition.Error(DiagnosticCatalog.AlphabetThroughWithAlso, $"{what}: the entry for {entry.GetChild(0).GetText()} "
                    + "specifies both a THROUGH phrase and an ALSO phrase — the literal-phrase general format offers "
                    + "them as alternatives within one pair of brackets (ISO §12.3.7.2; §5.2.6.2)");
                continue;
            }
            var operands = AlphabetOperands(entry, rules);
            if (operands.Count == 0) continue;
            // SR14 b3/c3: "Each … literal, when a THROUGH or ALSO phrase is specified, shall be one character in
            // length." A multi-character operand used to be silently DROPPED on the alphanumeric arm — the whole
            // entry vanished from the table with no diagnostic (kb/Work PB770 leg b).
            if ((thru || entry.ALSO().Length > 0) && !OneCharacterOperands(operands, rules, through: thru)) continue;
            if (thru)
            {
                // k5: the native run from operand-1 to operand-2, in EITHER direction, ascending positions.
                if (operands.Count < 2) continue;          // a malformed shape already drew a parse error
                int a = operands[0].Characters[0], b = operands[1].Characters[0], step = a <= b ? 1 : -1;
                for (int c = a; ; c += step) { Assign((char)c, advance: true); if (c == b) break; }
                continue;
            }
            if (entry.ALSO().Length > 0)
            {
                // k6: operand-1 and every ALSO operand share ONE ordinal position; operand-1 is the position's
                // first character (the CHAR() pick and the LOW-VALUE tie winner). ⛔ The advance is GUARDED
                // (PB59): an all-duplicate ALSO group must not advance past an unoccupied position — GR7 k3
                // admits no hole, and RepByPos would acquire one.
                int before = repByPos.Count;
                foreach (var op in operands) Assign(op.Characters[0], advance: false);
                if (repByPos.Count > before) next++;
                continue;
            }
            // k1b: a (possibly multi-character) literal — each character, leftmost first, ascending positions.
            foreach (char c in operands[0].Characters) Assign(c, advance: true);
        }
        if (specOrder.Count == 0) return null;      // nothing was specified — the operand diagnostics stand alone

        // The sparse arrays and the §12.3.7.4 GR8/GR9 extremes — CollatingTable.Build, the ONE place either is
        // computed, shared with the implementor code-name arm (GR7 i/j) which walks a code page instead of a
        // clause. This method used to carry its own copy of the extremes rule (kb/Work PB793).
        return CollatingTable.Build(pos, specOrder, repByPos, next);
    }

    /// <summary>A character named in a diagnostic: its literal form when it is printable, else its U+ code point.</summary>
    private static string DescribeChar(char c) => c > ' ' && c < (char)0x7F ? $"'{c}'" : $"U+{(int)c:X4}";

    /// <summary>The national coded-character-set name a definition consists of ("UCS-4" / "UTF-8" / "UTF-16"),
    /// or null. These are §8.9 CONTEXT-SENSITIVE words scoped to the ALPHABET clause — they arrive as a single
    /// plain <c>cobolWord</c> alphabet entry (never lexer keywords; they stay user-definable elsewhere), so the
    /// shape is: exactly one entry, no THROUGH/ALSO, one cobolWord, whose text is one of the three names.</summary>
    private static string? CodeSetNameOf(Core.AlphabetDefinitionContext def, Editions.CobolWordsMap cobolWords)
    {
        if (def.alphabetEntry() is not [{ } entry]) return null;
        if (entry.THRU() is not null || entry.THROUGH() is not null || entry.ALSO().Length > 0) return null;
        if (entry.ChildCount != 1 || entry.GetChild(0) is not Core.CobolWordContext w) return null;
        // >>COBOL-WORDS (ISO §7.3.10.4 GR2/GR3/GR4; kb/Work PB250): a §8.9/§8.10 word the lexer does not
        // tokenize (UCS-4 / UTF-8 / UTF-16 are §8.10 context-sensitive) is reached ONLY through the map. The
        // CANONICAL name is what is returned, so the downstream Phrase tag never carries a user synonym.
        string? t = cobolWords.Resolve(w.GetText());
        return t is "UCS-4" or "UTF-8" or "UTF-16" ? t : null;
    }

    /// <summary>Bind an <c>ALPHABET … FOR NATIONAL IS …</c> clause (ISO §12.3.7.2 second branch): NATIVE — the
    /// native national coded set and collating sequence (GR7 d2, identity); UCS-4 — the ISO/IEC 10646 coded set
    /// AND its appearance-order collating sequence (GR7 f), which on the D-N1 one-UTF-16-code-unit-per-position
    /// substrate IS the native code-unit order (see <see cref="NationalAlphabetDef"/> — §8.5.1.4 denies surrogate
    /// -pair recognition, so the supplementary-plane codepoint/code-unit divergence is unreachable; the
    /// correspondence is the BMP identity, implementor item 188); UTF-8/UTF-16 — coded character sets ONLY
    /// (GR7 g/h + Table 6: no collating sequence); a literal phrase — the sparse national collating table
    /// (GR7 k over the native national set). STANDARD-1/STANDARD-2 are not in the national branch's format
    /// (alphanumeric-branch-only); the LOCALE phrase is, and <see cref="AlphabetBind"/> binds it for both branches
    /// before reaching here. Unknown words are code-name-2 — the implementor supports none (§12.3.7.3 SR15).</summary>
    private void AlphabetBindNational(string name, Core.AlphabetDefinitionContext def)
    {
        if (def.NATIVE() is not null)
        {
            Cfg.NationalAlphabets.TryAdd(name, new NationalAlphabetDef(null, HasCollatingSequence: true, "NATIVE"));
            return;
        }
        if (def.STANDARD_1() is not null || def.STANDARD_2() is not null)
        {
            Edition.Error(DiagnosticCatalog.AlphabetCodeSetBranch, $"ALPHABET {name} FOR NATIONAL: STANDARD-1/STANDARD-2 name the "
                + "ISO/IEC 646 ALPHANUMERIC coded character set and may be referenced only in the FOR "
                + "ALPHANUMERIC branch (ISO §12.3.7.2 general format)");
            return;
        }
        if (CodeSetNameOf(def, _host.CobolWords) is { } cs)
        {
            // UCS-4 references BOTH a coded set and a collating sequence; UTF-8/UTF-16 a coded set ONLY
            // (§12.3.7 GR7 f/g/h + Table 6). All three collapse to the identity on this substrate — the
            // difference is only WHERE the name may legally be referenced.
            Cfg.NationalAlphabets.TryAdd(name, new NationalAlphabetDef(null, HasCollatingSequence: cs == "UCS-4", cs));
            return;
        }
        // A single non-figurative bare word that is not a coded-set name is code-name-2 (§12.3.7.3 SR15 —
        // implementor-defined). Figurative words are literal-phrase operands (GR10). ONE lookup, shared with the
        // alphanumeric arm since kb/Work PB770 — that arm had no SR15 check at all. ⚠ The table holds no
        // code-name-2 row today (kb/Work PB793 decided the ALPHANUMERIC spellings only), so this arm always
        // refuses; it is a LOOKUP and not a refusal so that a national row would bind with no code change.
        switch (AlphabetCodeName(name, def, national: true, out var codeName))
        {
            case CodeNameOutcome.Refused: return;
            case CodeNameOutcome.Bound:
                Cfg.NationalAlphabets.TryAdd(name, new NationalAlphabetDef(codeName!.Table, null,
                    HasCollatingSequence: true, codeName.Name, codeName));
                return;
        }
        if (AlphabetLiteralPhrase(name, def, national: true) is { } table)
            Cfg.NationalAlphabets.TryAdd(name, new NationalAlphabetDef(table, HasCollatingSequence: true, "literal-phrase"));
    }

    /// <summary>The ALPHABET clause's view of <see cref="LiteralPhraseOperand"/>: the operands of one alphabet
    /// entry (literal-1 and the THROUGH / ALSO operands), each held to §12.3.7.3 SR14 b (ALPHANUMERIC) or SR14 c
    /// (NATIONAL). Both arms had their own copy until kb/Work PB770, and the alphanumeric copy implemented NONE of
    /// the rules (<c>feedback_two_arm_dispatch</c>, fifth instance); the CLASS clause had a third copy until kb/Work
    /// PB976, which is why the decoder is now shared by clause rather than by arm.</summary>
    private List<PhraseOperand> AlphabetOperands(Core.AlphabetEntryContext entry, LiteralPhraseRules rules)
    {
        var result = new List<PhraseOperand>();
        for (int i = 0; i < entry.ChildCount; i++)
            if (entry.GetChild(i) is Core.LiteralContext or Core.CobolWordContext
                && LiteralPhraseOperand(entry.GetChild(i), rules) is { } operand)
                result.Add(operand);
        return result;
    }

    /// <summary>⛔ THE ONE §12.3.7.4 GR10 mapping of a figurative constant written INSIDE the SPECIAL-NAMES
    /// paragraph to the character it names: "<i>When specified as literals in the SPECIAL-NAMES paragraph, the
    /// figurative constants HIGH-VALUE and LOW-VALUE are associated with those characters having the highest and
    /// lowest positions, respectively, in the native national collating sequence, when the NATIONAL phrase is
    /// specified, or in the native alphanumeric collating sequence otherwise</i>" — so they are the NATIVE extremes
    /// here, never the sequence being defined. SPACE, QUOTE and ZERO are their §8.3.3.6 characters.
    /// <para>Null for any other word, which is then not an operand at all. Both native sequences answer HIGH-VALUE
    /// U+FFFF (owner decision kb/Work R52 — <see cref="WiseOwl.Cobol.Runtime.NativeCollatingSequence"/>).</para></summary>
    private static string? AlphabetFigurative(string word) => CobolNames.UpperFold(word) switch
    {
        "HIGH-VALUE" or "HIGH-VALUES" => WiseOwl.Cobol.Runtime.NativeCollatingSequence.HighValue.ToString(),
        "LOW-VALUE" or "LOW-VALUES" => WiseOwl.Cobol.Runtime.NativeCollatingSequence.LowValue.ToString(),
        "SPACE" or "SPACES" => " ",
        "QUOTE" or "QUOTES" => "\"",
        "ZERO" or "ZEROS" or "ZEROES" => "0",
        _ => null,
    };

    /// <summary>Why <paramref name="alphabet"/> does NOT define a collating sequence of the class asked for, as the
    /// clause's diagnostics print it — or null when it does. ⛔ THE ONE CLASS TEST for the three alphabet operands
    /// of §12.4.5.7.3 (SR1 alphabet-name-1 alphanumeric, SR2 alphabet-name-2 national, SR7 alphabet-name-3 of the
    /// key's class). The two alphabet domains are disjoint (§12.3.7: an ALPHABET clause defines one class), so the
    /// lookup IS the class test; a national alphabet naming a coded character set only (UTF-8 / UTF-16 — §12.3.7.4
    /// Table 6's empty collating-sequence column) defines no sequence at all.
    /// <para>⛔ ASKED BY EVERY SITE THAT NAMES AN ALPHABET AS A COLLATING SEQUENCE (kb/Work PB1082, PB1074): the
    /// file-control COLLATING SEQUENCE clause, the OBJECT-COMPUTER PROGRAM COLLATING SEQUENCE clause
    /// (<c>ResolveProgramCollating</c>) and the SORT/MERGE COLLATING SEQUENCE phrase (<c>SortBinder</c>). The program
    /// collating sequence kept its own copy of this test and made alphabet-name-1's undeclared arm inert — its
    /// alphabet-name-2 slot refused the same shapes (the two-arm dispatch).</para>
    /// <para>A word whose ALPHABET clause DID declare it but failed its own screens has no fault of its own: that
    /// clause reported already, and a second "not declared" would be false about the source (one fault, one
    /// verdict) — the caller then finds no definition to apply and continues on the native order, in a compilation
    /// that cannot produce a program.</para></summary>
    internal string? CollatingAlphabetFault(string alphabet, bool national)
    {
        if (!IsAlphabetName(alphabet)
            && _host.OwnWords.IsDeclaredAs(alphabet, UserWordKind.AlphabetName))   // SPECIAL-NAMES: never a method's (§12.3.3 SR2)
            return null;
        if (national)
            return Cfg.NationalAlphabets.TryPeek(alphabet, out var nd)
                ? nd.HasCollatingSequence ? null
                    : $"'{alphabet}' ({nd.Phrase}) names a national coded character set only (ISO §12.3.7.4 Table 6)"
                : Cfg.Alphabets.Declares(alphabet) ? $"'{alphabet}' is an alphanumeric alphabet"
                : $"'{alphabet}' is not an alphabet declared in SPECIAL-NAMES";
        return Cfg.Alphabets.Declares(alphabet) ? null
            : Cfg.NationalAlphabets.Declares(alphabet) ? $"'{alphabet}' is defined FOR NATIONAL"
            : $"'{alphabet}' is not an alphabet declared in SPECIAL-NAMES";
    }

    /// <summary>The message of a COLLATING SEQUENCE alphabet that does not define the class its slot asks for —
    /// one text for the two statement-level sites that share <see cref="CollatingAlphabetFault"/> (PROGRAM COLLATING
    /// SEQUENCE and the SORT/MERGE phrase), alphabet-name-1 being the alphanumeric slot and alphabet-name-2 the
    /// national one. <paramref name="rule"/> names the slot's own syntax rule, never its sibling's.</summary>
    internal static string CollatingAlphabetViolation(string site, string name, bool national, string fault, string rule) =>
        $"{site} '{name}': alphabet-name-{(national ? 2 : 1)} shall reference an alphabet that defines "
        + $"{(national ? "a NATIONAL" : "an ALPHANUMERIC")} collating sequence — {fault} ({rule})";
}
