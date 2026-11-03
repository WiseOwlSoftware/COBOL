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
/// M5): everything ISO §12.3 ("The configuration section specifies aspects of the data processing system that are
/// dependent on the specific system", §12.3.1) and the OPTIONS paragraph (§11.9) establish for the unit — SPECIAL-NAMES
/// (decimal point, currency signs, the configuration-section names, mnemonic-names), the OBJECT-COMPUTER collating
/// sequences and classification, DEBUGGING MODE, the REPOSITORY specifiers, and the OPTIONS model.
/// <para>ONE PRODUCER PER UNIT: <see cref="ConfigurationStep"/> seeds it from the arm the unit's kind names — an outermost
/// program, function, class definition or interface binds its own section (<c>Own</c>), a contained program takes its
/// container's slice (<c>Contained</c>; §12.3.3 SR1, §12.3.4 GR1), an OBJECT or FACTORY half takes its class definition's
/// (<c>ClassHalf</c>; §12.3.3 SR3) — and the unit's own CONFIGURATION SECTION and OPTIONS paragraph then overlay it in
/// <c>DataBinder.BindDeclarations</c> (the section's state is bound by <c>SpecialNamesBinder</c>, kb/Work PB2355). The slice is read by the data phases, the procedure binders and code generation through
/// <see cref="DataBindState.Configuration"/>; a method reads its half's (§12.3.3 SR2).</para>
/// </summary>
public sealed class ConfigurationSlice(Action<Antlr4.Runtime.RuleContext?, string, UserWordKind> recordUse)
{
    /// <summary>The unit's OPTIONS model (ISO §11.9): its own OPTIONS paragraph over <see cref="InheritedOptions"/> (§11.9.4
    /// GR1), bound once in <c>DataBinder.BindDeclarations</c>; the all-defaults model until then and when there is none. A
    /// method's own OPTIONS model governs that method only, through <c>OoDataBinder.EntryOptions</c> while its data entries
    /// are described (kb/Work PB4464), <see cref="ProcedureBindState.Options"/> while its statements bind and
    /// <c>EmitContext.Options</c> while its LOCAL-STORAGE initializers are emitted.</summary>
    public OptionsModel Options { get; internal set; } = OptionsModel.Default;

    /// <summary>The OPTIONS baseline the unit starts from — its container's model (ISO §11.9.4 GR1: "The clauses in the
    /// OPTIONS paragraph apply to the source element in which they are specified and to all source elements contained in
    /// that source element unless overridden by a clause in an OPTIONS paragraph in a contained source element"), or the
    /// class definition's for an OBJECT or FACTORY half. Null for an outermost unit (the all-defaults model). Written by
    /// <see cref="ConfigurationStep"/> only.</summary>
    internal OptionsModel? InheritedOptions { get; set; }

    /// <summary>The OPTIONS paragraph of an OO source element, which sits on the SKELETON (§10.6.1: the class, OBJECT,
    /// FACTORY and INTERFACE definitions) outside the synthetic program unit <c>DataBinder.BindDeclarations</c> walks;
    /// null for a program unit, whose paragraph is in its identification division. Written by <see cref="ConfigurationStep"/>
    /// only; bound at the ONE point every source element's OPTIONS paragraph binds, after the element's constant entries are
    /// declared (kb/Work PB780: the INITIALIZE clause's literal-1 may be a constant-name, §13.10.3 SR2).</summary>
    internal Core.OptionsParagraphContext? OptionsParagraph { get; set; }

    /// <summary>The SPECIAL-NAMES {mnemonic-name → implementor system-name} map (ISO §12.3.7.2: <c>switch-name-1 IS
    /// mnemonic-name-1</c> | <c>feature-name-1 IS mnemonic-name-2</c> | <c>device-name-1 IS mnemonic-name-3</c>), the unit's
    /// own entries first and its container's after (§12.3.7.4 GR1; the nearest declaration wins). The value is the
    /// <see cref="ImplementorName"/> ROW, so every consumer asks the row's KIND, never the spelling of the entry (kb/Work
    /// PB862). An entry naming an unavailable name contributes nothing (the SPECIAL-NAMES binder reported it, COBOL2241).
    /// Read by <c>Procedure.MnemonicRegistry</c> alone, through this method.</summary>
    internal bool TryGetMnemonic(string name, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out ImplementorName row) =>
        _mnemonics.TryGetValue(name, out row) || (_mnemonicContainer?.TryGetMnemonic(name, out row) ?? false);

    /// <summary>Declare the unit's own mnemonic-name <paramref name="name"/>; the first declaration of a spelling is kept.</summary>
    internal void DeclareMnemonic(string name, ImplementorName row) => _mnemonics.TryAdd(name, row);

    /// <summary>The unit's own mnemonic-name entries.</summary>
    private readonly Dictionary<string, ImplementorName> _mnemonics = new(CobolNames.Comparer);

    /// <summary>The containing slice whose mnemonic-names this unit inherits (§12.3.7.4 GR1), read after its own.</summary>
    private ConfigurationSlice? _mnemonicContainer;

    /// <summary>Inherit <paramref name="container"/>'s mnemonic-names. <see cref="ConfigurationStep"/> only.</summary>
    internal void InheritMnemonicsFrom(ConfigurationSlice container) => _mnemonicContainer = container;

    /// <summary>Switch-status condition-names (case-insensitive) → (implementor switch-name, posited-ON) (ISO
    /// §12.3.7 GR2; §8.4.4.2 Format 1 SR1 — the condition-name shall be associated with a switch-name in
    /// SPECIAL-NAMES). Consulted by the condition binder AFTER level-88 resolution: a switch name declared in the
    /// same source element as a level-88 of the spelling counts as one more candidate of an unqualified reference, and a
    /// nearer 88 hides it (<c>DataBinder.SwitchConditionCompetes</c>, §8.4.2.2.1, §8.4.6.1; kb/Work PB3347).</summary>
    internal ConfigurationNames<(string ImplementorName, bool IsOn)> SwitchConditions =>
        field ??= new(UserWordKind.ConditionName, recordUse);

    /// <summary>User-defined CLASS names (case-insensitive) → the EXPANDED member-character set (ISO §12.3.7
    /// class-name clause: each literal lists its characters; a THRU pair contributes every character between the
    /// two ordinals in the NATIVE collating sequence, in either order). Consulted by the class-condition binder
    /// (§8.8.4.4 — true when the operand consists entirely of members).
    /// <para>Each entry carries the CLASS of its characters (kb/Work PB976): a <c>FOR NATIONAL</c> class is a set of
    /// national characters, and "the relevant native character set" of §12.3.7.4 GR12 is the national one. Both
    /// native sets are the same UTF-16 repertoire today (implementor item 188), so the members coincide — the flag
    /// is recorded so that a native alphanumeric set that is ever NOT that repertoire cannot silently resolve a
    /// national class's ordinals in the wrong set.</para></summary>
    internal ConfigurationNames<UserClassDef> UserClasses =>
        field ??= new(UserWordKind.ClassName, recordUse);

    /// <summary>ALPHANUMERIC alphabet names (case-insensitive) → what the name references (ISO §12.3.7 GR7): the
    /// built collating table of a literal phrase, the LOCALE arm of an <c>IS LOCALE</c> phrase, the identity
    /// (<see cref="AlphabetDef.Native"/>) for NATIVE/STANDARD-1/STANDARD-2 (ISO/IEC 646 order IS the Latin-1 native
    /// order — no table), or an implementor code-name's row (§12.3.7.3 SR15 / §12.3.7.4 GR7 i —
    /// <c>DataBinder.ImplementorCodeNames</c>: ASCII, identity like STANDARD-1; EBCDIC, whose 256-entry table the row
    /// carries). An <c>ALPHABET … FOR NATIONAL</c> clause registers in <see cref="NationalAlphabets"/>
    /// instead — the two classes are disjoint reference domains (§12.3.6 SR1/SR2, §14.9.40 GR5).</summary>
    internal ConfigurationNames<AlphabetDef> Alphabets =>
        field ??= new(UserWordKind.AlphabetName, recordUse);

    /// <summary>NATIONAL alphabet names (case-insensitive) → what the name references (ISO §12.3.7 GR7 b/d2/f/g/h/k
    /// + Table 6): a null-table identity for NATIVE and the coded-set names (UCS-4 collates in ISO 10646 order,
    /// which on the D-N1 one-code-unit-per-position substrate IS the native code-unit order — see
    /// <see cref="NationalAlphabetDef"/> for the §8.5.1.4 derivation; UTF-8/UTF-16 name coded character sets ONLY),
    /// or the sparse <see cref="CollatingTable"/> of a literal phrase (the SAME record the alphanumeric arm
    /// builds - one §12.3.7.4 GR7 k model for both classes).</summary>
    internal ConfigurationNames<NationalAlphabetDef> NationalAlphabets =>
        field ??= new(UserWordKind.AlphabetName, recordUse);

    /// <summary>SYMBOLIC CHARACTERS figurative constants (ISO §12.3.7.4 GR11 a — "Symbolic-character-1 defines a
    /// figurative constant"; kb/Work PB110): name → the ONE-character value (GR11 b/c — the character at ordinal
    /// integer-1 of the native or IN-alphabet coded character set) and its class. Substituted wherever a figurative
    /// constant may stand — the reference seams consult <c>SpecialNamesBinder.SymbolicOf</c> exactly as they consult the
    /// §13.10 constant table, and the value fills like every figurative (§8.3.3.6.4 GR2 / GR10).</summary>
    internal ConfigurationNames<(string Value, bool National)> SymbolicCharacters =>
        field ??= new(UserWordKind.SymbolicCharacter, recordUse);

    /// <summary>SPECIAL-NAMES <c>ORDER TABLE</c> ordering-names (case-insensitive) → the DECODED literal-9 that
    /// identifies the cultural ordering table (ISO §12.3.7.2's last clause; §12.3.7.4 GR17 — "When ORDER TABLE is
    /// specified, ordering-name-1 shall reference a cultural ordering table that is identified by literal-9 and
    /// constructed in accordance with ISO/IEC 14651:2020, Annex A. The implementor specifies the allowable content
    /// of literal-9"). Read by exactly one consumer, §12.3.7.3 SR9's only legal reference site —
    /// <c>IntrinsicBinder.BindStandardCompare</c> (§15.85.3 r5 / §15.3 argument type 12), which copies the literal
    /// onto the bound call so the backend never has to ask the SPECIAL-NAMES model anything.
    /// <para>⚠ The literal is stored DECODED, not as source text: §12.3.7.3 SR10 admits a national literal
    /// (<c>N"…"</c>) as well as an alphanumeric one, and the runtime resolver matches ordering-table NAMES, not
    /// COBOL literal spellings.</para></summary>
    internal ConfigurationNames<string> OrderTables =>
        field ??= new(UserWordKind.OrderingName, recordUse);

    /// <summary>SPECIAL-NAMES <c>DYNAMIC LENGTH STRUCTURE</c> declarations (case-insensitive
    /// dynamic-length-structure-name → its layout; ISO §12.3.7.2; kb/Work PB829), read by the DYNAMIC LENGTH clause's
    /// dynamic-length-structure-name-1 (§13.18.19.3 SR2 / SR4). Configuration-section names reach contained source
    /// elements (§8.4.6.1), so the dictionary inherits with its siblings.</summary>
    internal ConfigurationNames<DynamicLengthStructure> DynamicLengthStructures =>
        field ??= new(UserWordKind.DynamicLengthStructureName, recordUse);

    /// <summary>SPECIAL-NAMES <c>LOCALE locale-name-1 IS {external-locale-name-1 | literal-4}</c> declarations
    /// (case-insensitive locale-name → the symbol; ISO §12.3.7.2; DESIGN-locale-facility seam S1, T1): the locale-name
    /// is a user-defined word (§8.3.2.2) referenced by <c>ALPHABET … IS LOCALE locale-name-2</c> (§12.3.7.3 SR24), SET
    /// LOCALE (§14.9.39.3 SR26), and — in later increments — the LOCALE phrases of PICTURE format 2, LOCALE-COMPARE /
    /// -DATE / -TIME, UPPER-CASE / LOWER-CASE and CHARACTER CLASSIFICATION. The symbol holds the EXTERNAL IDENTIFICATION
    /// only (§8.1.5 — the locale is "determined at runtime"); it inherits into contained units (§12.3.7.4 GR1).</summary>
    internal ConfigurationNames<LocaleSymbol> Locales =>
        field ??= new(UserWordKind.LocaleName, recordUse);

    /// <summary>The OBJECT-COMPUTER <c>CHARACTER CLASSIFICATION</c> clause (ISO §12.3.6.2; §12.3.6.4 GR5–GR8; kb/Work PB64
    /// T5), or null when none is specified and no containing unit specifies one (GR6 — the coded character set's
    /// classification). Inherited per §12.3.6.4 GR1; consumed by the emitter (resolved at each activation —
    /// <c>__CLASSIFY</c>), by UPPER-CASE / LOWER-CASE without a LOCALE phrase (§15.97.4 r3 / §15.57.4 r3) and by the
    /// ALPHABETIC class tests (§8.8.4.4.4 GR3).</summary>
    public ClassificationSpec? Classification { get; internal set; }

    /// <summary>The resolved ALPHANUMERIC PROGRAM COLLATING SEQUENCE (ISO §12.3.6 GR9–GR11) — a literal-phrase table
    /// or a LOCALE sequence — or null when none is specified / the named alphabet is the native order. Drives
    /// relation, condition-name and SORT/MERGE-key comparisons (GR11/GR13), MAX/MIN, ORD/CHAR, and the HIGH-/LOW-VALUE
    /// characters (§8.3.3.6 GR6/GR7). The emitter renders it ONCE as the program's <c>__COLLATE</c> carrier.</summary>
    public AlphabetDef? Collating { get; internal set; }

    /// <summary>The resolved NATIONAL PROGRAM COLLATING SEQUENCE (ISO §12.3.6 GR9 — alphabet-name-2 / the FOR
    /// NATIONAL form), or null when none is specified / the named alphabet is an identity sequence (NATIVE, UCS-4 —
    /// the native UTF-16 code-unit order, D-N3). Drives NATIONAL relation and condition-name comparisons (GR11 /
    /// §8.8.4.2.9), CHAR-NATIONAL/ORD-over-national (§15.16.4 / §15.70.4 r2), and the national HIGH-/LOW-VALUE
    /// characters (§12.3.7 GR8/GR9). National SORT/MERGE keys (GR13 / §14.9.40 GR5b) cannot yet EXIST — D-N2
    /// refuses national leaves in FD/SD records and the table-sort national key is staged loud — so no key
    /// consumer reads this yet.</summary>
    public NationalAlphabetDef? NationalCollating { get; internal set; }

    /// <summary>DECIMAL-POINT IS COMMA (ISO §12.3.7 GR14 — the decimal and grouping separator characters EXCHANGE
    /// functionality in numeric literals [GR14a] and PICTURE character-strings / edited insertion [GR14b]).
    /// Version-invariant 85→2023 (no VERSION_CHANGE_REFERENCE row). Set BEFORE any literal or PICTURE binds —
    /// SPECIAL-NAMES is walked at the top of <c>DataBinder.Bind</c>.</summary>
    public bool DecimalPointIsComma { get; internal set; }

    /// <summary>The character a numeric literal of this program writes its decimal separator with (ISO §12.3.7.4 GR14 a):
    /// ',' under DECIMAL-POINT IS COMMA, else '.'. A contained program inherits the container's
    /// (<c>ConfigurationStep.Produce</c>, §12.3.4 GR1).</summary>
    public char DecimalSeparator => DecimalPointIsComma ? ',' : '.';

    /// <summary>
    /// The unit's CURRENCY SIGN SET (ISO §12.3.7): every currency PICTURE SYMBOL (uppercase-keyed — §12.3.7.3
    /// r20 / §8.1.3 GR3 make the letter cases equivalent) → the currency STRING it stands for (GR13) AND the CLASS
    /// of the literal-7 that gave it (<see cref="CurrencyDefinition"/>; §12.3.7.3 SR28 confines an alphanumeric
    /// literal-7's symbol to usage-display numeric-edited items and a national one's to usage-national ones, and a set
    /// of symbol → string alone cannot ask it — kb/Work PB1089). A bare
    /// <c>CURRENCY SIGN IS literal-7</c> makes literal-7 both string and symbol (r22, one character); the 2002+
    /// <c>WITH PICTURE SYMBOL literal-8</c> form names the one-character symbol (r26/r27) for a string "of any
    /// length" (r23). r25 IMPLIES <c>CURRENCY SIGN '$' PICTURE SYMBOL '$'</c> unless a clause specifies '$' as
    /// literal-7 or literal-8 — so <c>PIC $$9</c> stays legal beside a declared '#', and the set is never empty.
    /// r21: two clauses may not bind equivalent symbols to different strings. Completed by
    /// <see cref="FinalizeCurrencySigns"/> at the end of the SPECIAL-NAMES walk; a contained program inherits the
    /// container's set whole (<c>ConfigurationStep.Produce</c>, §12.3.4 GR1).
    /// ⛔ THIS REPLACED A SCALAR PAIR (kb/Work PB60 / AR-15.68.3-3): <c>CurrencyPicSymbol</c>/<c>CurrencyString</c>
    /// held ONE symbol and ONE string per unit — a second clause silently overwrote the first, the implied '$'
    /// died the moment any clause bound another symbol (a legal <c>PIC $$,$$9.99</c> under <c>CURRENCY SIGN "#"</c>
    /// was COBOL0808), and a multi-character string was refused outright (COBOL0896, "not yet supported" —
    /// deferral debt). Every PICTURE consumer now asks this set which symbol a mask uses (<c>PictureAnalyzer</c>
    /// records it on <c>PicInfo.CurrencyString</c> after canonicalizing the mask's symbol to '$'), and NUMVAL-C's
    /// §15.68.3 r3 default is <see cref="SoleCurrencyString"/>.
    /// </summary>
    public IReadOnlyDictionary<char, CurrencyDefinition> CurrencySigns => DeclaredCurrencySigns;

    internal Dictionary<char, CurrencyDefinition> DeclaredCurrencySigns { get; } = new();

    /// <summary>The DISTINCT currency strings the unit's EXPLICIT CURRENCY SIGN clauses specify (r3 of §15.68.3
    /// counts these: "there shall be only one currency string for the compilation unit, either the default
    /// currency sign or a currency string specified in the SPECIAL-NAMES paragraph" — the implied '$' clause of
    /// r25 is the picture-symbol default, not a competing string).</summary>
    internal HashSet<string> ExplicitCurrencyStrings { get; } = new(StringComparer.Ordinal);

    /// <summary>True once a clause named '$' as literal-7 or literal-8 (r25 — the implied clause is then NOT added).</summary>
    internal bool DollarSpecified { get; set; }

    /// <summary>The ONE currency string NUMVAL-C / TEST-NUMVAL-C use when argument-2 (and LOCALE) is absent
    /// (§15.68.3 r3): the single explicitly specified string, or the default "$" when the unit has no CURRENCY
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

    /// <summary>r25's implied clause: after the SPECIAL-NAMES walk, '$' → "$" joins the set unless a clause
    /// specified '$' as literal-7 or literal-8. Idempotent (a re-walk cannot double it).</summary>
    internal void FinalizeCurrencySigns()
    {
        if (!DollarSpecified) DeclaredCurrencySigns.TryAdd('$', CurrencyDefinition.ImpliedDollar);
    }

    /// <summary>True when SOURCE-COMPUTER declares WITH DEBUGGING MODE (the X3.23-1985 compile-time debug
    /// switch) — consumed by the declaratives binder to decide the USE FOR DEBUGGING posture: with the switch the
    /// debugging section is compiled AND its procedure-trigger leg is modeled (fires; the DEBUG-ITEM register
    /// resolves), without it the section is comment-treated (VCR Table 7 rows 7.9/7.17).</summary>
    public bool DebuggingModeDeclared { get; internal set; }

    /// <summary>The unit's REPOSITORY PROPERTY specifier names (§12.3.8) — the §8.4.3.9.3 SR1 gate for
    /// object-property references (case-insensitive per §8.3.2).</summary>
    /// <para>Keyed by property-name-1 → the name the property is known by in the declared classes: literal-4 when
    /// the specifier writes <c>AS literal-4</c> (§12.3.8.2, kb/Work PB974), otherwise property-name-1 itself.</para>
    internal Dictionary<string, string> OoRepositoryProperties { get; } = new(CobolNames.Comparer);

    /// <summary>The unit's REPOSITORY PROGRAM specifiers (ISO §12.3.8.2's program-specifier, <c>PROGRAM
    /// program-prototype-name-1 [AS literal-3]</c>) keyed by program-prototype-name — the ONE declaration surface
    /// for a program-prototype-name, and therefore the §14.9.4.3 SR16 / §14.9.5.3 SR3 precondition for every
    /// Format-2 CALL and prototype CANCEL (kb/Work PB237). §8.3.2 case-insensitive. The SPECIFIER is collected
    /// here (syntax); §12.3.8.3 SR15's self/containing-program ignore and §12.3.8.4 GR10's resolution against the
    /// compilation group need the unit's identity and its siblings, so they run in
    /// <c>BinderDriver.ProgramPrototypesOf</c>, exactly as the function twin resolves in <c>BinderDriver.UserFunctionsOf</c>.</summary>
    internal Dictionary<string, ProgramSpecifier> ProgramSpecifiers { get; } = new(CobolNames.Comparer);

    /// <summary>The unit's REPOSITORY user-function specifiers (§12.3.8 — <c>FUNCTION function-prototype-name</c>
    /// WITHOUT the INTRINSIC phrase): the precondition for a user-function reference, and per §12.3.8.4 GR12
    /// (:14885) the declaration that makes the name refer to the USER-DEFINED function "and not to an intrinsic
    /// function of the same name" — so the binder's user-function dispatch precedes the intrinsic catalog.</summary>
    internal HashSet<string> UserFunctionNames { get; } = new(CobolNames.Comparer);

    /// <summary>The unit's user-defined-function specifiers by function-prototype-name-1 (<see cref="FunctionSpecifier"/>):
    /// its EXTERNALIZED name is literal-5 when <c>AS literal-5</c> is written (§12.3.8.4 GR11 NOTE 2 — "Literal-5, if
    /// specified, is the externalized name of the function prototype; otherwise, the externalized name is
    /// function-prototype-name-1"), otherwise the name itself. GR11's search of the compilation group runs in
    /// <c>BinderDriver.UserFunctionsOf</c>, the twin of <c>ProgramPrototypesOf</c> (kb/Work PB974).</summary>
    internal Dictionary<string, FunctionSpecifier> FunctionSpecifiers { get; } = new(CobolNames.Comparer);

    /// <summary>The unit's REPOSITORY intrinsic-function specifiers by name (§12.3.8 —
    /// <c>FUNCTION intrinsic-function-name INTRINSIC</c>): the §8.4.3.2 SR2 precondition that lets the word
    /// FUNCTION be OMITTED when referencing that intrinsic (GR13). §8.3.2 case-insensitive.</summary>
    internal HashSet<string> RepositoryIntrinsics { get; } = new(CobolNames.Comparer);

    /// <summary>True when the unit's REPOSITORY specifies <c>FUNCTION ALL INTRINSIC</c> (§12.3.8 GR14): the word
    /// FUNCTION may be omitted for EVERY §8.11 intrinsic-function-name in this scope (SR2/GR13). Read only through
    /// <c>DataBinder.IsRepositoryIntrinsic</c> — the membership that applies GR14's >>COBOL-WORDS clauses and the edition
    /// — which the SR13 user-word prohibition (<c>DataBinder.DeclareUserWord</c>) asks too.</summary>
    internal bool RepositoryAllIntrinsic { get; set; }
}
