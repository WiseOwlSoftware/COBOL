// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Common;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Runtime.Collation;

namespace WiseOwl.Cobol.Binding;

using Core = WiseOwl.Cobol.Frontend.Generated.CobolParserCore;

/// <summary>
/// ⛔ THE CONFIGURATION SLICE of ONE bound unit (docs/rearchitecture/DESIGN-architecture-review.md §8.3 (1); kb/Work PB2296
/// M5): everything ISO §2023:12.3 ("The configuration section specifies aspects of the data processing system that are
/// dependent on the specific system", §2023:12.3.1) and the OPTIONS paragraph (§2023:11.9) establish for the unit — SPECIAL-NAMES
/// (decimal point, currency signs, the configuration-section names, mnemonic-names), the OBJECT-COMPUTER collating
/// sequences and classification, DEBUGGING MODE, the REPOSITORY specifiers, and the OPTIONS model.
/// <para>ONE PRODUCER PER UNIT: <see cref="ConfigurationStep"/> seeds it from the arm the unit's kind names — an outermost
/// program, function, class definition or interface binds its own section (<c>Own</c>), a contained program takes its
/// container's slice (<c>Contained</c>; §2023:12.3.3 SR1, §2023:12.3.4 GR1), an OBJECT or FACTORY half takes its class definition's
/// (<c>ClassHalf</c>; §2023:12.3.3 SR3) — and the unit's own CONFIGURATION SECTION and OPTIONS paragraph then overlay it in
/// <c>DataBinder.BindDeclarations</c> (the section's state is bound by <c>SpecialNamesBinder</c>, kb/Work PB2355). The slice is read by the data phases, the procedure binders and code generation through
/// <see cref="DataBindState.Configuration"/>; a method reads its half's (§2023:12.3.3 SR2).</para>
/// </summary>
public sealed class ConfigurationSlice
{
    /// <summary>The unit's OPTIONS model (ISO §2023:11.9): its own OPTIONS paragraph over <see cref="InheritedOptions"/> (§2023:11.9.4
    /// GR1), bound once in <c>DataBinder.BindDeclarations</c>; the all-defaults model until then and when there is none. A
    /// method's own OPTIONS model governs that method only, through <c>OoDataBinder.EntryOptions</c> while its data entries
    /// are described (kb/Work PB4464), <see cref="ProcedureBindState.Options"/> while its statements bind and
    /// <c>EmitContext.Options</c> while its LOCAL-STORAGE initializers are emitted.</summary>
    public OptionsModel Options { get; internal set; } = OptionsModel.Default;

    /// <summary>The OPTIONS baseline the unit starts from — its container's model (ISO §2023:11.9.4 GR1: "The clauses in the
    /// OPTIONS paragraph apply to the source element in which they are specified and to all source elements contained in
    /// that source element unless overridden by a clause in an OPTIONS paragraph in a contained source element"), or the
    /// class definition's for an OBJECT or FACTORY half. Null for an outermost unit (the all-defaults model). Written by
    /// <see cref="ConfigurationStep"/> only.</summary>
    internal OptionsModel? InheritedOptions { get; set; }

    /// <summary>The OPTIONS paragraph of an OO source element, which sits on the SKELETON (§2023:10.6.1: the class, OBJECT,
    /// FACTORY and INTERFACE definitions) outside the synthetic program unit <c>DataBinder.BindDeclarations</c> walks;
    /// null for a program unit, whose paragraph is in its identification division. Written by <see cref="ConfigurationStep"/>
    /// only; bound at the ONE point every source element's OPTIONS paragraph binds, after the element's constant entries are
    /// declared (kb/Work PB780: the INITIALIZE clause's literal-1 may be a constant-name, §2023:13.10.3 SR2).</summary>
    internal Core.OptionsParagraphContext? OptionsParagraph { get; set; }

    /// <summary>The OBJECT-COMPUTER <c>CHARACTER CLASSIFICATION</c> clause (ISO §2023:12.3.6.2; §2023:12.3.6.4 GR5–GR8; kb/Work PB64
    /// T5), or null when none is specified and no containing unit specifies one (GR6 — the coded character set's
    /// classification). Inherited per §2023:12.3.6.4 GR1; consumed by the emitter (resolved at each activation —
    /// <c>__CLASSIFY</c>), by UPPER-CASE / LOWER-CASE without a LOCALE phrase (§2023:15.97.4 RV3 / §2023:15.57.4 RV3) and by the
    /// ALPHABETIC class tests (§2023:8.8.4.4.4 GR3).</summary>
    public ClassificationSpec? Classification { get; internal set; }

    /// <summary>The resolved ALPHANUMERIC PROGRAM COLLATING SEQUENCE (ISO §2023:12.3.6.4 GR9–GR11) — a literal-phrase table
    /// or a LOCALE sequence — or null when none is specified / the named alphabet is the native order. Drives
    /// relation, condition-name and SORT/MERGE-key comparisons (GR11/GR13), MAX/MIN, ORD/CHAR, and the HIGH-/LOW-VALUE
    /// characters (§2023:8.3.3.6.4 GR6/GR7). The emitter renders it ONCE as the program's <c>__COLLATE</c> carrier.</summary>
    public AlphabetDef? Collating { get; internal set; }

    /// <summary>The resolved NATIONAL PROGRAM COLLATING SEQUENCE (ISO §2023:12.3.6.4 GR9 — alphabet-name-2 / the FOR
    /// NATIONAL form), or null when none is specified / the named alphabet is an identity sequence (NATIVE, UCS-4 —
    /// the native UTF-16 code-unit order, D-N3). Drives NATIONAL relation and condition-name comparisons (GR11 /
    /// §2023:8.8.4.2.9), CHAR-NATIONAL/ORD-over-national (§2023:15.16.4 / §2023:15.70.4 RV2), and the national HIGH-/LOW-VALUE
    /// characters (§2023:12.3.7.4 GR8/GR9). National SORT/MERGE keys (GR13 / §2023:14.9.40.4 GR5b) cannot yet EXIST — D-N2
    /// refuses national leaves in FD/SD records and the table-sort national key is staged loud — so no key
    /// consumer reads this yet.</summary>
    public NationalAlphabetDef? NationalCollating { get; internal set; }

    /// <summary>DECIMAL-POINT IS COMMA (ISO §2023:12.3.7.4 GR14 — the decimal and grouping separator characters EXCHANGE
    /// functionality in numeric literals [GR14a] and PICTURE character-strings / edited insertion [GR14b]).
    /// Version-invariant 85→2023 (no VERSION_CHANGE_REFERENCE row). Set BEFORE any literal or PICTURE binds —
    /// SPECIAL-NAMES is walked at the top of <c>DataBinder.Bind</c>.</summary>
    public bool DecimalPointIsComma { get; internal set; }

    /// <summary>The character a numeric literal of this program writes its decimal separator with (ISO §2023:12.3.7.4 GR14a):
    /// ',' under DECIMAL-POINT IS COMMA, else '.'. A contained program inherits the container's
    /// (<c>ConfigurationStep.Produce</c>, §2023:12.3.4 GR1).</summary>
    public char DecimalSeparator => DecimalPointIsComma ? ',' : '.';

    /// <summary>
    /// The unit's CURRENCY SIGN SET (ISO §2023:12.3.7): every currency PICTURE SYMBOL (uppercase-keyed — §2023:12.3.7.3
    /// r20 / §2023:8.1.3.2 GR3 make the letter cases equivalent) → the currency STRING it stands for (GR13) AND the CLASS
    /// of the literal-7 that gave it (<see cref="CurrencyDefinition"/>; §2023:12.3.7.3 SR28 confines an alphanumeric
    /// literal-7's symbol to usage-display numeric-edited items and a national one's to usage-national ones, and a set
    /// of symbol → string alone cannot ask it — kb/Work PB1089). A bare
    /// <c>CURRENCY SIGN IS literal-7</c> makes literal-7 both string and symbol (r22, one character); the 2002+
    /// <c>WITH PICTURE SYMBOL literal-8</c> form names the one-character symbol (r26/r27) for a string "of any
    /// length" (r23). r25 IMPLIES <c>CURRENCY SIGN '$' PICTURE SYMBOL '$'</c> unless a clause specifies '$' as
    /// literal-7 or literal-8 — so <c>PIC $$9</c> stays legal beside a declared '#', and the set is never empty.
    /// r21: two clauses may not bind equivalent symbols to different strings. Completed by
    /// <see cref="FinalizeCurrencySigns"/> at the end of the SPECIAL-NAMES walk; a contained program inherits the
    /// container's set whole (<c>ConfigurationStep.Produce</c>, §2023:12.3.4 GR1).
    /// ⛔ THIS REPLACED A SCALAR PAIR (kb/Work PB60 / AR-15.68.3-3): <c>CurrencyPicSymbol</c>/<c>CurrencyString</c>
    /// held ONE symbol and ONE string per unit — a second clause silently overwrote the first, the implied '$'
    /// died the moment any clause bound another symbol (a legal <c>PIC $$,$$9.99</c> under <c>CURRENCY SIGN "#"</c>
    /// was COBOL0808), and a multi-character string was refused outright (COBOL0896, "not yet supported" —
    /// deferral debt). Every PICTURE consumer now asks this set which symbol a mask uses (<c>PictureAnalyzer</c>
    /// records it on <c>PicInfo.CurrencyString</c> after canonicalizing the mask's symbol to '$'), and NUMVAL-C's
    /// §2023:15.68.3 AR3 default is <see cref="SoleCurrencyString"/>.
    /// </summary>
    public IReadOnlyDictionary<char, CurrencyDefinition> CurrencySigns => DeclaredCurrencySigns;

    internal Dictionary<char, CurrencyDefinition> DeclaredCurrencySigns { get; } = new();

    /// <summary>The DISTINCT currency strings the unit's EXPLICIT CURRENCY SIGN clauses specify (r3 of §2023:15.68.3
    /// counts these: "there shall be only one currency string for the compilation unit, either the default
    /// currency sign or a currency string specified in the SPECIAL-NAMES paragraph" — the implied '$' clause of
    /// r25 is the picture-symbol default, not a competing string).</summary>
    internal HashSet<string> ExplicitCurrencyStrings { get; } = new(StringComparer.Ordinal);

    /// <summary>True once a clause named '$' as literal-7 or literal-8 (r25 — the implied clause is then NOT added).</summary>
    internal bool DollarSpecified { get; set; }

    /// <summary>The ONE currency string NUMVAL-C / TEST-NUMVAL-C use when argument-2 (and LOCALE) is absent
    /// (§2023:15.68.3 AR3): the single explicitly specified string, or the default "$" when the unit has no CURRENCY
    /// SIGN clause; NULL when the unit specifies two or more distinct strings — r3's "there shall be only one" is
    /// then violated and the reference is diagnosed at bind (COBOL1644).</summary>
    public string? SoleCurrencyString => ExplicitCurrencyStrings.Count switch
    {
        0 => "$",
        1 => ExplicitCurrencyStrings.First(),
        _ => null,
    };

    /// <summary>The number of distinct explicitly specified currency strings (for the r3 diagnostic's text).</summary>
    public int ExplicitCurrencyStringCount => ExplicitCurrencyStrings.Count;

    /// <summary>After the SPECIAL-NAMES walk, '$' → "$" joins the set unless a clause specified '$' as literal-7 or literal-8.
    /// From COBOL-2014 it joins as r25's implied clause, whose alphanumeric literal-7 SR28 asks of every use
    /// (<see cref="CurrencyDefinition.ImpliedDollar"/>); at COBOL-2002 no such clause exists and it joins as the default currency
    /// sign, with no literal-7 for SR24 to ask (<see cref="CurrencyDefinition.DefaultDollar"/>; kb/Work PB5614). Idempotent (a
    /// re-walk cannot double it).</summary>
    internal void FinalizeCurrencySigns(EditionInfo edition)
    {
        if (!DollarSpecified)
            DeclaredCurrencySigns.TryAdd('$', edition.ImpliesDollarCurrencyClause ? CurrencyDefinition.ImpliedDollar : CurrencyDefinition.DefaultDollar);
    }

    /// <summary>True when SOURCE-COMPUTER declares WITH DEBUGGING MODE (the X3.23-1985 compile-time debug
    /// switch) — consumed by the declaratives binder to decide the USE FOR DEBUGGING posture: with the switch the
    /// debugging section is compiled AND its procedure-trigger leg is modeled (fires; the DEBUG-ITEM register
    /// resolves), without it the section is comment-treated (VCR Table 7 rows 7.9/7.17).</summary>
    public bool DebuggingModeDeclared { get; internal set; }


    /// <summary>The unit's REPOSITORY intrinsic-function specifiers by name (§2023:12.3.8 —
    /// <c>FUNCTION intrinsic-function-name INTRINSIC</c>): the §2023:8.4.3.2.3 SR2 precondition that lets the word
    /// FUNCTION be OMITTED when referencing that intrinsic (GR13). Keyed by <see cref="StandardWord"/>: an
    /// intrinsic-function-name is a word of the standard (§2023:8.11), never a user-defined word, so it is no name of the
    /// symbol table's (docs/rearchitecture/DESIGN-listing.md section 6.7.1, "Step 6b, slice 7"). The user-defined-function
    /// specifiers beside it are the symbol table's (<see cref="SymbolTable.DeclareFunctionSpecifier"/>).</summary>
    internal HashSet<StandardWord> RepositoryIntrinsics { get; } = [];

    /// <summary>True when the unit's REPOSITORY specifies <c>FUNCTION ALL INTRINSIC</c> (§2023:12.3.8.4 GR14): the word
    /// FUNCTION may be omitted for EVERY §2023:8.11 intrinsic-function-name in this scope (SR2/GR13). Read only through
    /// <c>DataBinder.IsRepositoryIntrinsic</c> — the membership that applies GR14's >>COBOL-WORDS clauses and the edition
    /// — which the SR13 user-word prohibition (<c>DataBinder.DeclareUserWord</c>) asks too.</summary>
    internal bool RepositoryAllIntrinsic { get; set; }

    /// <summary>The configuration facts a backend reads (<see cref="ConfigurationFacts"/>), copied from this slice when the unit's
    /// <see cref="DataBindResult"/> is made (<c>DataBinder.Result</c>; kb/Work PB2361): the OPTIONS model, the CHARACTER
    /// CLASSIFICATION, the two PROGRAM COLLATING SEQUENCEs, DECIMAL-POINT IS COMMA and the program-specifiers' externalized
    /// names, in the order they were written (<paramref name="programSpecifierNames"/>, the symbol table's,
    /// <see cref="SymbolTable.ProgramSpecifierExternalizedNames"/>). The declaration tables and the name-use recorder stay with the binder.</summary>
    internal ConfigurationFacts Facts(IReadOnlyList<string> programSpecifierNames) =>
        new(Options, Classification, Collating, NationalCollating, DecimalPointIsComma, programSpecifierNames);
}
