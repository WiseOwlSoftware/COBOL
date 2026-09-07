// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;
using CobolNet.Runtime.Collation;

using CobolNet.Binding.Model;

namespace CobolNet.Binding;

using Core = CobolParserCore;

/// <summary>
/// The SPECIAL-NAMES external-switch registry (ISO §12.3.7, version-invariant 85→2023): the switch-name clause
/// associates an implementor-defined external switch with an optional mnemonic-name (Option 1) and/or ON/OFF
/// status condition-names (either Option). The mnemonic is referenced only in SET (SR5); the condition-names are
/// interrogated as switch-status conditions (GR2 / §8.8.4.6); SET alters the status (GR3 / §14.9.39 F3).
/// </summary>
public sealed partial class DataBinder
{
    /// <summary>SPECIAL-NAMES switch mnemonic-names (case-insensitive) → the implementor switch-name they set
    /// (ISO §12.3.7 Option 1; SR5 — a mnemonic-name may be specified only in a SET statement). An Option 2 entry
    /// (no mnemonic) registers the switch-name itself, accepting <c>SET switch-name</c> — legacy-parity leniency
    /// (the conforming program cannot SET an Option 2 switch at all, so no conforming program changes meaning).</summary>
    public Dictionary<string, string> SwitchMnemonics { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Switch-status condition-names (case-insensitive) → (implementor switch-name, posited-ON) (ISO
    /// §12.3.7 GR2; §8.4.4.2 Format 1 SR1 — the condition-name shall be associated with a switch-name in
    /// SPECIAL-NAMES). Consulted by the condition binder AFTER level-88 resolution (NC211A defines a name as BOTH;
    /// the level-88 wins).</summary>
    public Dictionary<string, (string ImplementorName, bool IsOn)> SwitchConditions { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>User-defined CLASS names (case-insensitive) → the EXPANDED member-character set (ISO §12.3.7
    /// class-name clause: each literal lists its characters; a THRU pair contributes every character between the
    /// two ordinals in the NATIVE collating sequence, in either order). Consulted by the class-condition binder
    /// (§8.8.4.4 — true when the operand consists entirely of members).</summary>
    public Dictionary<string, string> UserClasses { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>ALPHANUMERIC alphabet names (case-insensitive) → what the name references (ISO §12.3.7 GR7): the
    /// built collating table of a literal phrase, the LOCALE arm of an <c>IS LOCALE</c> phrase, the identity
    /// (<see cref="AlphabetDef.Native"/>) for NATIVE/STANDARD-1/STANDARD-2 (ISO/IEC 646 order IS the Latin-1 native
    /// order — no table), or an implementor code-name's row (§12.3.7.3 SR15 / §12.3.7.4 GR7 i —
    /// <see cref="ImplementorCodeNames"/>: ASCII, identity like STANDARD-1; EBCDIC, whose 256-entry table the row
    /// carries). An <c>ALPHABET … FOR NATIONAL</c> clause registers in <see cref="NationalAlphabets"/>
    /// instead — the two classes are disjoint reference domains (§12.3.6 SR1/SR2, §14.9.40 GR5).</summary>
    public Dictionary<string, AlphabetDef> Alphabets { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>NATIONAL alphabet names (case-insensitive) → what the name references (ISO §12.3.7 GR7 b/d2/f/g/h/k
    /// + Table 6): a null-table identity for NATIVE and the coded-set names (UCS-4 collates in ISO 10646 order,
    /// which on the D-N1 one-code-unit-per-position substrate IS the native code-unit order — see
    /// <see cref="NationalAlphabetDef"/> for the §8.5.1.4 derivation; UTF-8/UTF-16 name coded character sets ONLY),
    /// or the sparse <see cref="CollatingTable"/> of a literal phrase (the SAME record the alphanumeric arm
    /// builds - one §12.3.7.4 GR7 k model for both classes).</summary>
    public Dictionary<string, NationalAlphabetDef> NationalAlphabets { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Is <paramref name="alphabetName"/> an alphabet "associated with a locale" (ISO §8.8.4.4.3 SR2) /
    /// "specified with the LOCALE phrase" (§12.3.7.3 SR16g / SR17d) — of EITHER class (<c>ALPHABET … IS LOCALE</c>
    /// registers in <see cref="Alphabets"/>, <c>ALPHABET … FOR NATIONAL IS LOCALE</c> in <see cref="NationalAlphabets"/>)?
    /// Such an alphabet defines a collating sequence only (§12.3.7.4 GR7, Table 6: the LOCALE row names no coded
    /// character set), so the ONE question every site that needs a CODED CHARACTER SET asks is this one (kb/Work PB64
    /// T5 — the class condition's alphabet-name-1 today; the CLASS / SYMBOLIC CHARACTERS <c>IN</c> phrases and CODE-SET
    /// when they bind, kb/Work PB110). An undeclared name is not a locale alphabet — its own diagnostic is elsewhere.</summary>
    public bool IsLocaleAlphabet(string alphabetName)
        => (Alphabets.TryGetValue(alphabetName, out var a) && a.Locale is not null)
        || (NationalAlphabets.TryGetValue(alphabetName, out var n) && n.Locale is not null);

    /// <summary>⛔ THE ONE resolver of an alphabet-name referenced where a CODED CHARACTER SET is required (ISO
    /// §12.3.7.4 GR7 Table 6; kb/Work PB110 — the class condition's alphabet-name-1, SYMBOLIC CHARACTERS … IN,
    /// CLASS … IN, CODE-SET): the set of the named alphabet of either class, with the two refusals every site
    /// shares — a LOCALE alphabet defines a collating sequence only (Table 6's blank row → COBOLNET1669, citing
    /// <paramref name="rule"/>), and an undeclared name is COBOLNET0898 naming the site. Null on either refusal.</summary>
    internal CodedCharacterSet? CodedCharacterSetOf(string alphabetName, string site, string rule)
    {
        AlphabetDef? a = Alphabets.TryGetValue(alphabetName, out var av) ? av : null;
        NationalAlphabetDef? n = NationalAlphabets.TryGetValue(alphabetName, out var nv) ? nv : null;
        if (a is null && n is null)
        {
            Edition.Error("COBOLNET0898", $"{site}: '{alphabetName}' is not an alphabet-name declared by a "
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

    /// <summary>SYMBOLIC CHARACTERS figurative constants (ISO §12.3.7.4 GR11 a — "Symbolic-character-1 defines a
    /// figurative constant"; kb/Work PB110): name → the ONE-character value (GR11 b/c — the character at ordinal
    /// integer-1 of the native or IN-alphabet coded character set) and its class. Substituted wherever a figurative
    /// constant may stand — the reference seams consult <see cref="SymbolicOf"/> exactly as they consult the
    /// §13.10 constant table, and the value fills like every figurative (§8.3.3.6.4 GR2 / GR10).</summary>
    public Dictionary<string, (string Value, bool National)> SymbolicCharacters { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The symbolic character a BARE (unqualified, unsubscripted) data reference names, or null — the
    /// twin of <c>ConstantOf</c> (one shape for "a word that stands for a literal").</summary>
    internal (string Value, bool National)? SymbolicOf(Core.DataReferenceContext dref) =>
        dref.dataReferenceSuffix().Length == 0 && dref.cobolWord() is { } w
        && SymbolicCharacters.TryGetValue(w.GetText(), out var def) ? def : null;

    /// <summary>The symbolic character named by a bare word, or null (the FigurativeOperand ALL-form lookup).</summary>
    internal (string Value, bool National)? SymbolicOf(string word) =>
        SymbolicCharacters.TryGetValue(word, out var def) ? def : null;

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
    public Dictionary<string, string> OrderTables { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>SPECIAL-NAMES <c>LOCALE locale-name-1 IS {external-locale-name-1 | literal-4}</c> declarations
    /// (case-insensitive locale-name → the symbol; ISO §12.3.7.2; DESIGN-locale-facility seam S1, T1): the locale-name
    /// is a user-defined word (§8.3.2.2) referenced by <c>ALPHABET … IS LOCALE locale-name-2</c> (§12.3.7.3 SR24), SET
    /// LOCALE (§14.9.39.3 SR26), and — in later increments — the LOCALE phrases of PICTURE format 2, LOCALE-COMPARE /
    /// -DATE / -TIME, UPPER-CASE / LOWER-CASE and CHARACTER CLASSIFICATION. The symbol holds the EXTERNAL IDENTIFICATION
    /// only (§8.1.5 — the locale is "determined at runtime"); it inherits into contained units (§12.3.7.4 GR1).</summary>
    public Dictionary<string, LocaleSymbol> Locales { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The OBJECT-COMPUTER <c>CHARACTER CLASSIFICATION</c> clause (ISO §12.3.6.2; §12.3.6.4 GR5–GR8; kb/Work PB64
    /// T5), or null when none is specified and no containing unit specifies one (GR6 — the coded character set's
    /// classification). Inherited per §12.3.6.4 GR1; consumed by the emitter (resolved at each activation —
    /// <c>__CLASSIFY</c>), by UPPER-CASE / LOWER-CASE without a LOCALE phrase (§15.97.4 r3 / §15.57.4 r3) and by the
    /// ALPHABETIC class tests (§8.8.4.4.4 GR3).</summary>
    public ClassificationSpec? Classification { get; private set; }

    /// <summary>The resolved ALPHANUMERIC PROGRAM COLLATING SEQUENCE (ISO §12.3.6 GR9–GR11) — a literal-phrase table
    /// or a LOCALE sequence — or null when none is specified / the named alphabet is the native order. Drives
    /// relation, condition-name and SORT/MERGE-key comparisons (GR11/GR13), MAX/MIN, ORD/CHAR, and the HIGH-/LOW-VALUE
    /// characters (§8.3.3.6 GR6/GR7). The emitter renders it ONCE as the program's <c>__COLLATE</c> carrier.</summary>
    public AlphabetDef? Collating { get; private set; }

    /// <summary>The resolved NATIONAL PROGRAM COLLATING SEQUENCE (ISO §12.3.6 GR9 — alphabet-name-2 / the FOR
    /// NATIONAL form), or null when none is specified / the named alphabet is an identity sequence (NATIVE, UCS-4 —
    /// the native UTF-16 code-unit order, D-N3). Drives NATIONAL relation and condition-name comparisons (GR11 /
    /// §8.8.4.2.9), CHAR-NATIONAL/ORD-over-national (§15.16.4 / §15.70.4 r2), and the national HIGH-/LOW-VALUE
    /// characters (§12.3.7 GR8/GR9). National SORT/MERGE keys (GR13 / §14.9.40 GR5b) cannot yet EXIST — D-N2
    /// refuses national leaves in FD/SD records and the table-sort national key is staged loud — so no key
    /// consumer reads this yet.</summary>
    public NationalAlphabetDef? NationalCollating { get; private set; }

    /// <summary>DECIMAL-POINT IS COMMA (ISO §12.3.7 GR14 — the decimal and grouping separator characters EXCHANGE
    /// functionality in numeric literals [GR14a] and PICTURE character-strings / edited insertion [GR14b]).
    /// Version-invariant 85→2023 (no VERSION_CHANGE_REFERENCE row). Set BEFORE any literal or PICTURE binds —
    /// SPECIAL-NAMES is walked at the top of <see cref="Bind"/>.</summary>
    public bool DecimalPointIsComma { get; private set; }

    /// <summary>
    /// The unit's CURRENCY SIGN SET (ISO §12.3.7): every currency PICTURE SYMBOL (uppercase-keyed — §12.3.7.3
    /// r20 / §8.1.3 GR3 make the letter cases equivalent) → the currency STRING it stands for (GR13). A bare
    /// <c>CURRENCY SIGN IS literal-7</c> makes literal-7 both string and symbol (r22, one character); the 2002+
    /// <c>WITH PICTURE SYMBOL literal-8</c> form names the one-character symbol (r26/r27) for a string "of any
    /// length" (r23). r25 IMPLIES <c>CURRENCY SIGN '$' PICTURE SYMBOL '$'</c> unless a clause specifies '$' as
    /// literal-7 or literal-8 — so <c>PIC $$9</c> stays legal beside a declared '#', and the set is never empty.
    /// r21: two clauses may not bind equivalent symbols to different strings. Completed by
    /// <see cref="FinalizeCurrencySigns"/> at the end of the SPECIAL-NAMES walk; a contained program inherits the
    /// container's set whole (<see cref="InheritConfiguration"/>, §12.3.4 GR1).
    /// ⛔ THIS REPLACED A SCALAR PAIR (kb/Work PB60 / AR-15.68.3-3): <c>CurrencyPicSymbol</c>/<c>CurrencyString</c>
    /// held ONE symbol and ONE string per unit — a second clause silently overwrote the first, the implied '$'
    /// died the moment any clause bound another symbol (a legal <c>PIC $$,$$9.99</c> under <c>CURRENCY SIGN "#"</c>
    /// was COBOLNET0808), and a multi-character string was refused outright (COBOLNET0896, "not yet supported" —
    /// deferral debt). Every PICTURE consumer now asks this set which symbol a mask uses (<c>PictureAnalyzer</c>
    /// records it on <c>PicInfo.CurrencyString</c> after canonicalizing the mask's symbol to '$'), and NUMVAL-C's
    /// §15.68.3 r3 default is <see cref="SoleCurrencyString"/>.
    /// </summary>
    public IReadOnlyDictionary<char, string> CurrencySigns => _currencySigns;

    private readonly Dictionary<char, string> _currencySigns = new();

    /// <summary>The DISTINCT currency strings the unit's EXPLICIT CURRENCY SIGN clauses specify (r3 of §15.68.3
    /// counts these: "there shall be only one currency string for the compilation unit, either the default
    /// currency sign or a currency string specified in the SPECIAL-NAMES paragraph" — the implied '$' clause of
    /// r25 is the picture-symbol default, not a competing string).</summary>
    private readonly HashSet<string> _explicitCurrencyStrings = new(StringComparer.Ordinal);

    /// <summary>True once a clause named '$' as literal-7 or literal-8 (r25 — the implied clause is then NOT added).</summary>
    private bool _dollarSpecified;

    /// <summary>The ONE currency string NUMVAL-C / TEST-NUMVAL-C use when argument-2 (and LOCALE) is absent
    /// (§15.68.3 r3): the single explicitly specified string, or the default "$" when the unit has no CURRENCY
    /// SIGN clause; NULL when the unit specifies two or more distinct strings — r3's "there shall be only one" is
    /// then violated and the reference is diagnosed at bind (COBOLNET1644).</summary>
    public string? SoleCurrencyString => _explicitCurrencyStrings.Count switch
    {
        0 => "$",
        1 => _explicitCurrencyStrings.First(),
        _ => null,
    };

    /// <summary>The number of distinct explicitly specified currency strings (for the r3 diagnostic's text).</summary>
    public int ExplicitCurrencyStringCount => _explicitCurrencyStrings.Count;

    /// <summary>r25's implied clause: after the SPECIAL-NAMES walk, '$' → "$" joins the set unless a clause
    /// specified '$' as literal-7 or literal-8. Idempotent (a re-walk cannot double it).</summary>
    private void FinalizeCurrencySigns()
    {
        if (!_dollarSpecified) _currencySigns.TryAdd('$', "$");
    }

    /// <summary>Normalize a NUMERIC literal's source text to the canonical dot-decimal form the whole emit-side
    /// decode pipeline consumes (ISO §12.3.7 GR14a: under DECIMAL-POINT IS COMMA "the character written in
    /// numeric literals to represent the decimal separator shall be the comma" — and §8.3.3.3.2 admits ONLY the
    /// decimal point in a fixed-point literal, so the OTHER separator is diagnosed in each mode; the legacy's
    /// unconditional acceptance of both is a version-invariant non-conformance, not ported). The ONE literal
    /// chokepoint: expression paths route through <c>StatementBinder.CheckLiteral</c>, VALUE / level-88 capture
    /// call this directly.</summary>
    public string NormalizeNumericLiteral(string text)
    {
        // The normalization ALGORITHM is the ONE shared CobolNet.Common.NumericLiteral.Normalize (Frontend), so the
        // compile-time expression evaluator applies the identical §12.3.7 GR14a rule; the binder retains ownership
        // of the COBOLNET0895 diagnostic (its own channel/descriptor), routing the returned issue to it verbatim.
        string norm = Common.NumericLiteral.Normalize(text, DecimalPointIsComma, out var issue);
        switch (issue)
        {
            case Common.NumericSeparatorIssue.DecimalPointUnderCommaMode:
                Edition.Error("COBOLNET0895", $"numeric literal '{text}': under DECIMAL-POINT IS COMMA the "
                    + "decimal separator is the comma (ISO §12.3.7 GR14a); '.' is not valid in a numeric literal");
                break;
            case Common.NumericSeparatorIssue.CommaWithoutCommaMode:
                Edition.Error("COBOLNET0895", $"numeric literal '{text}': a comma decimal separator requires "
                    + "DECIMAL-POINT IS COMMA (ISO §12.3.7 GR14a; §8.3.3.3.2 admits only '.' as the decimal point)");
                break;
        }
        // §8.3.3.3.3 SR2/SR3/SR4 (kb/Work PB99) — the floating-point literal's FORM, checked HERE because both the
        // expression funnel (ExpressionBinder.CheckLiteral) and the VALUE / level-88 funnel normalize through this method.
        if (Common.NumericLiteral.IsFloatingPointForm(norm)
            && Common.NumericLiteral.CheckFloatingPointForm(norm) is var fissue && fissue != Common.NumericLiteral.FloatingLiteralIssue.None)
            Edition.Error(DiagnosticCatalog.FloatingLiteral, $"floating-point numeric literal '{text}': " + fissue switch
            {
                Common.NumericLiteral.FloatingLiteralIssue.SignificandDigits => "the significand shall be from 1 to 36 digits in length (ISO §8.3.3.3.3 SR2)",
                Common.NumericLiteral.FloatingLiteralIssue.ExponentDigits => "the exponent shall have a maximum of four digits (ISO §8.3.3.3.3 SR3)",
                _ => "when all the digits of the significand are zero, all the digits of the exponent shall be zero and neither part shall have a negative sign (ISO §8.3.3.3.3 SR4)",
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
            // the floating-point form's ONE exponent marker (§8.3.3.3.3) — its significand normalizes like any literal
            if ((c is 'E' or 'e') && !sawE && anyDigit) { sawE = true; continue; }
            if (c is not ('+' or '-' or '.' or ',')) return text;
        }
        return anyDigit ? NormalizeNumericLiteral(text) : text;
    }

    /// <summary>The OPTIONS baseline a contained program starts from — its container's model (ISO §11.9.4 GR1:
    /// "The clauses in the OPTIONS paragraph apply to the source element in which they are specified and to all
    /// source elements contained in that source element unless overridden by a clause in an OPTIONS paragraph in
    /// a contained source element"). Null for an outermost unit (the all-defaults model).</summary>
    private OptionsModel? _inheritedOptions;

    /// <summary>
    /// ⛔ ISO §12.3.4 GR1 — "The entries explicitly or implicitly specified in the configuration section of a
    /// source unit that contains other source units apply to each directly or indirectly contained source unit"
    /// — and §12.3.3 SR1 forbids the containee a configuration section of its own, so a contained program CANNOT
    /// restate DECIMAL-POINT IS COMMA, CURRENCY SIGN, a CLASS, an ALPHABET, the PROGRAM COLLATING SEQUENCE or a
    /// switch and is entitled to inherit every one of them. Called by <c>BinderDriver.BindUnitData</c> for every
    /// contained unit BEFORE <see cref="Bind"/> (the SPECIAL-NAMES state must be in place before the containee's
    /// first literal or PICTURE binds); the container itself inherited from ITS container first (units bind
    /// container-first), so one level carries the whole ancestry. ONE method, the WHOLE configuration-derived
    /// state (feedback_model_the_rule_shape_not_one_case): before this landed only the REPOSITORY sets were
    /// inherited, and inside a contained program of a DECIMAL-POINT IS COMMA unit NUMVAL("123,45") valued 0
    /// while NUMVAL("123.45") valued 123.45 — the exact inversion of §15.67.3 r5, with no diagnostic
    /// (kb/Work PB60 / AR-15.67.3-5). §11.9.4 GR1's OPTIONS inheritance rides the same call.
    /// <para>⚠ One sibling still lives elsewhere: the Format-4 device-name mnemonics (DISPLAY UPON / ACCEPT FROM)
    /// inherit through <c>MnemonicRegistry.Of</c>'s enclosing-unit PARSE-TREE walk — correct, but a second
    /// mechanism for the same rule; fold it in here when the environment division gets a bound model.</para>
    /// </summary>
    internal void InheritConfiguration(DataBinder container)
    {
        // SPECIAL-NAMES (§12.3.7)
        DecimalPointIsComma = container.DecimalPointIsComma;
        foreach (var (k, v) in container._currencySigns) _currencySigns.TryAdd(k, v);
        _explicitCurrencyStrings.UnionWith(container._explicitCurrencyStrings);
        _dollarSpecified = container._dollarSpecified;
        foreach (var (k, v) in container.SwitchMnemonics) SwitchMnemonics.TryAdd(k, v);
        foreach (var (k, v) in container.SwitchConditions) SwitchConditions.TryAdd(k, v);
        foreach (var (k, v) in container.UserClasses) UserClasses.TryAdd(k, v);
        foreach (var (k, v) in container.Alphabets) Alphabets.TryAdd(k, v);
        foreach (var (k, v) in container.NationalAlphabets) NationalAlphabets.TryAdd(k, v);
        foreach (var (k, v) in container.OrderTables) OrderTables.TryAdd(k, v);
        foreach (var (k, v) in container.Locales) Locales.TryAdd(k, v);   // §12.3.7.4 GR1 — locale-names reach contained units
        // OBJECT-COMPUTER … PROGRAM COLLATING SEQUENCE (§12.3.6 GR9–GR11) and CHARACTER CLASSIFICATION (GR1 — every
        // OBJECT-COMPUTER clause applies to the contained units)
        Collating = container.Collating;
        Classification = container.Classification;
        NationalCollating = container.NationalCollating;
        // SOURCE-COMPUTER … WITH DEBUGGING MODE (the '85 compile-time switch)
        DebuggingModeDeclared = container.DebuggingModeDeclared;
        // REPOSITORY (§12.3.8) — the M2-UDF-1/-4 inheritance, now here with its siblings
        UserFunctionNames.UnionWith(container.UserFunctionNames);
        // §12.3.8.2's program-specifiers reach contained units on the same rule as their siblings (kb/Work PB237):
        // §12.3.8.4 GR10 scopes program-prototype-name-1 "throughout the scope of the containing environment
        // division", and a contained program with no CONFIGURATION SECTION of its own is inside that scope. The
        // containee's OWN specifier wins on a name clash — TryAdd, the sibling posture, and the one §12.3.8.3 SR15
        // shape that matters here (the containee may re-specify a name the container also declares).
        foreach (var (k, v) in container.ProgramSpecifiers) ProgramSpecifiers.TryAdd(k, v);
        RepositoryIntrinsics.UnionWith(container.RepositoryIntrinsics);
        if (container.RepositoryAllIntrinsic) RepositoryAllIntrinsic = true;
        OoRepositoryProperties.UnionWith(container.OoRepositoryProperties);
        // OPTIONS (§11.9.4 GR1) — the containee's own OPTIONS paragraph overrides clause by clause at Bind.
        _inheritedOptions = container.Options;
    }

    /// <summary>Bind <c>DECIMAL-POINT IS COMMA</c> (ISO §12.3.7 — the only word the format admits after IS is
    /// COMMA; the grammar carries it as a generic IDENTIFIER).</summary>
    private void SwitchBindDecimalPoint(Core.DecimalPointClauseContext dp)
    {
        if (dp.IDENTIFIER()?.GetText() is { } word && word.Equals("COMMA", StringComparison.OrdinalIgnoreCase))
            DecimalPointIsComma = true;
        else
            Edition.Error("COBOLNET0894", $"DECIMAL-POINT IS {dp.IDENTIFIER()?.GetText()}: the only form is "
                + "DECIMAL-POINT IS COMMA (ISO §12.3.7)");
    }

    /// <summary>The characters of a CURRENCY SIGN clause literal, or null when it is not a literal of the class the
    /// clause requires: §12.3.7.3 SR18 — "<i>Literal-7 shall be an alphanumeric or national literal that is not a
    /// figurative constant</i>" — and SR26 — "<i>Literal-8 shall be an alphanumeric or national literal consisting of
    /// a single character from the computer's compile-time coded character set</i>". ⛔ A NUMERIC literal was
    /// silently ACCEPTED here until kb/Work PB770's sweep: the clause called the CLASS clause's ordinal helper, so
    /// <c>CURRENCY SIGN IS 65</c> became the currency symbol 'A' with no diagnostic (and an out-of-range ordinal
    /// would have reported a CLASS-clause violation on a program with no CLASS clause).</summary>
    private string? CurrencyTextLiteral(Core.LiteralContext lit, string operand, string rule)
    {
        if (!int.TryParse(lit.GetText(), out _) && !decimal.TryParse(lit.GetText(), System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out _))
            return LiteralCharsOf(lit);
        Edition.Error("COBOLNET0892", $"CURRENCY SIGN {lit.GetText()}: {operand} shall be an alphanumeric or "
            + $"national literal, not a numeric one (ISO §12.3.7.3 SR{rule})");
        return null;
    }

    /// <summary>Bind <c>CURRENCY [SIGN] [IS] literal-7 [WITH PICTURE SYMBOL literal-8]</c> (ISO §12.3.7.3
    /// r18–r27) into the currency SET. Bare form: literal-7 is both currency string and symbol — a single
    /// character outside the r22 forbidden set. PICTURE SYMBOL form (2002+ — the 85 standard had only the
    /// single-character form): literal-7 is the currency string ("may have any length", r23 — at least one
    /// non-space character, none of <c>0-9 + - , . *</c>), literal-8 the one-character symbol (r26/r27; a
    /// hexadecimal literal-7 requires this form, r19). r21: two clauses may not give equivalent symbols
    /// different strings. r25's implied '$' is added by <see cref="FinalizeCurrencySigns"/>.</summary>
    private void SwitchBindCurrency(Core.CurrencySignClauseContext cur)
    {
        var lits = cur.literal();
        if (lits.Length == 0) return;
        if (CurrencyTextLiteral(lits[0], "literal-7", "18") is not { } literal7) return;
        // r19: a hexadecimal literal-7 (X"…" / NX"…") requires the PICTURE SYMBOL phrase — detected on the token
        // text (the literal's decoded characters cannot tell the forms apart).
        string raw7 = lits[0].GetText();
        bool literal7Hex = raw7.Length > 2 && (raw7.Contains('"') || raw7.Contains('\''))
            && (raw7.StartsWith("X", StringComparison.OrdinalIgnoreCase) || raw7.StartsWith("NX", StringComparison.OrdinalIgnoreCase));

        char symbol;
        string currencyString;
        if (cur.PIC() is not null)
        {
            // currency-picture-symbol-2002 (§12.3.7): the pass owns the edition gate (Exec Step E).
            // PICMODE exploit: the word between PICTURE and literal-8 arrives as a PIC_STRING token — it must be
            // the keyword SYMBOL (the grammar cannot distinguish; semantic check per the grammar's own note).
            if (cur.PIC_STRING()?.GetText() is { } sym && !sym.Equals("SYMBOL", StringComparison.OrdinalIgnoreCase))
                Edition.Error("COBOLNET0892", $"CURRENCY SIGN: expected 'WITH PICTURE SYMBOL', found 'PICTURE {sym}' (ISO §12.3.7)");
            if (lits.Length > 1 && CurrencyTextLiteral(lits[1], "literal-8", "26") is null) return;
            string literal8 = lits.Length > 1 ? LiteralCharsOf(lits[1]) : "";
            if (literal8.Length != 1)
            {
                Edition.Error("COBOLNET0892", "CURRENCY SIGN: the PICTURE SYMBOL literal shall be a single "
                    + "character (ISO §12.3.7.3 r26)");
                return;
            }
            // r23a/b — the currency STRING's content (its length is free).
            if (literal7.Trim().Length == 0)
            {
                Edition.Error("COBOLNET0890", "CURRENCY SIGN: the currency string shall contain at least one "
                    + "non-space character (ISO §12.3.7.3 r23a)");
                return;
            }
            if (literal7.Any(c => char.IsAsciiDigit(c) || c is '+' or '-' or ',' or '.' or '*'))
            {
                Edition.Error("COBOLNET0890", $"CURRENCY SIGN \"{literal7}\": a currency string may not contain the "
                    + "digits 0 through 9 or the characters '+' '-' ',' '.' '*' (ISO §12.3.7.3 r23b)");
                return;
            }
            symbol = literal8[0];
            currencyString = literal7;
            ValidateCurrencyChar(symbol, "PICTURE SYMBOL literal");   // r27 — same forbidden set as r22
        }
        else
        {
            // Bare form (the COBOL-85 surface): r22 — one character, both string and symbol.
            if (literal7Hex)
                Edition.Error("COBOLNET0890", "CURRENCY SIGN: a hexadecimal currency literal requires the PICTURE "
                    + "SYMBOL phrase (ISO §12.3.7.3 r19)");
            if (literal7.Length != 1)
            {
                Edition.Error("COBOLNET0890", $"CURRENCY SIGN \"{literal7}\": without PICTURE SYMBOL the literal "
                    + "shall consist of a single character (ISO §12.3.7.3 r22)");
                return;
            }
            symbol = literal7[0];
            currencyString = literal7;
            ValidateCurrencyChar(symbol, "CURRENCY SIGN literal");
        }

        char key = char.ToUpperInvariant(symbol);
        // r21: no two clauses may bind equivalent symbols to different strings.
        if (_currencySigns.TryGetValue(key, out string? bound) && bound != currencyString)
        {
            Edition.Error("COBOLNET0891", $"CURRENCY SIGN: the symbol '{symbol}' is already bound to "
                + $"\"{bound}\" (ISO §12.3.7.3 r21)");
            return;
        }
        _currencySigns[key] = currencyString;
        _explicitCurrencyStrings.Add(currencyString);
        if (currencyString == "$" || key == '$') _dollarSpecified = true;   // r25 — no implied clause
    }

    /// <summary>The ISO §12.3.7 SR22/SR27 forbidden currency-symbol set: digits; A B C D E N P R S V X Z (either
    /// case) or space; and <c>+ - , . * / ; ( ) " =</c>.</summary>
    private void ValidateCurrencyChar(char c, string what)
    {
        bool forbidden = char.IsAsciiDigit(c)
            || char.ToUpperInvariant(c) is 'A' or 'B' or 'C' or 'D' or 'E' or 'N' or 'P' or 'R' or 'S' or 'V' or 'X' or 'Z'
            || c is ' ' or '+' or '-' or ',' or '.' or '*' or '/' or ';' or '(' or ')' or '"' or '=';
        if (forbidden)
            Edition.Error("COBOLNET0891", $"{what} '{c}': not a valid currency symbol — digits, the picture "
                + "letters A B C D E N P R S V X Z, space, and + - , . * / ; ( ) \" = are excluded (ISO §12.3.7 SR22/SR27)");
    }

    // ── The §12.3.8.2 program-specifier (kb/Work PB237) ──────────────────────────────────────────────────────
    /// <summary>Bind ONE <c>PROGRAM program-prototype-name-1 [AS literal-3]</c> entry of the REPOSITORY paragraph
    /// (ISO §12.3.8.2's program-specifier, PDF page 334). Two declaration rules apply and both are checked here:
    /// §12.3.8.3 syntax rule 2 (literal-3's class), and syntax rule 1 — "If any … program-prototype-name-1 … is
    /// specified more than once in the REPOSITORY paragraph, all the specifications for that name shall be
    /// identical", which for a program-specifier means the same externalized name.
    /// <para>What the specifier RESOLVES to — §12.3.8.4 GR10's a)/b)/c) search of the compilation group, and
    /// §12.3.8.3 SR15's "this specifier is ignored" for the unit's own or a containing program's name — needs the
    /// unit's identity and its siblings, which this binder does not have. Those run in
    /// <c>BinderDriver.ProgramPrototypesOf</c>, exactly where the function twin's GR11 search runs
    /// (<c>BuildUserFunctionTable</c>). Collecting the SYNTAX here and resolving it there is the same split.</para></summary>
    private void BindProgramSpecifier(Core.RepositoryEntryContext re, string name)
    {
        using var _ = Edition.At(re);   // the sink stamps every report below with THIS entry's position (PB82)
        string externalized = name;   // §12.3.8.4 GR10 NOTE 1: without AS, the externalized name IS the prototype name
        if (re.externalizedNamePhrase() is { } asPhrase)
        {
            // §12.3.8.3 SR2 is the SAME sentence the five id paragraphs restate — one screen, kb/Work PB303.
            string where = $"REPOSITORY PROGRAM '{name}' AS {asPhrase.literal().GetText()}";
            if (ExternalizedName.Screen(asPhrase.literal(), Edition, DiagnosticCatalog.RepositoryProgramSpecifier,
                    where, "literal-3", "ISO §12.3.8.3 SR2",
                    collate: Collating, natCollate: NationalCollating) is not { } lit3) return;
            externalized = lit3;
        }
        // §12.3.8.3 SR1 — a repeated name is legal, an INCONSISTENT repeat is not. Ordinal on the externalized
        // name: it is an operating-environment name, not a COBOL word, so §8.3.2's case-insensitivity does not
        // reach it (the KEY is case-insensitive because program-prototype-name-1 IS a COBOL word).
        if (ProgramSpecifiers.TryGetValue(name, out var prior))
        {
            if (!string.Equals(prior.ExternalizedName, externalized, StringComparison.Ordinal))
                Edition.Error(DiagnosticCatalog.RepositoryProgramSpecifier,
                    $"REPOSITORY PROGRAM '{name}' is specified more than once with different externalized names "
                    + $"('{prior.ExternalizedName}' then '{externalized}'); ISO §12.3.8.3 syntax rule 1 requires "
                    + "all the specifications for one name to be identical");
            return;
        }
        ProgramSpecifiers[name] = new ProgramSpecifier(name, externalized);
    }


    /// <summary>Populate the switch registry from the SPECIAL-NAMES paragraph's switch-name clauses (ISO §12.3.7
    /// general format: <c>switch-name-1 [IS mnemonic-name-1] [ON [STATUS] [IS] condition-name-1]
    /// [OFF [STATUS] [IS] condition-name-2]</c>; the NIST-85 surface also writes <c>ON IS cond</c> with no STATUS —
    /// both shapes are one grammar rule). Any switch-name is accepted (SR8 — the available names are
    /// implementor-specified; see <c>ExternalSwitches</c> for the documented item-191 contract).</summary>
    private void SwitchBindSpecialNames(Core.ProgramUnitContext program)
    {
        Core.ProgramCollatingSequenceClauseContext? pcsClause = null;
        Core.CharacterClassificationClauseContext? ccClause = null;
        var classClauses = new List<Core.ClassDefinitionClauseContext>();
        var symbolicClauses = new List<Core.SymbolicCharactersClauseContext>();
        foreach (var para in EnvDivisions(program)
                     .SelectMany(env => env.configurationSection()?.configurationParagraph() ?? []))
        {
            // OBJECT-COMPUTER … PROGRAM COLLATING SEQUENCE (ISO §12.3.6 — the 85 single-name IS form, the 2002
            // two-name IS form, and the 2002 FOR ALPHANUMERIC/FOR NATIONAL forms; resolved AFTER the walk). The
            // paragraph's clauses are a list since kb/Work PB78 (either clause, either order, with or without a
            // computer-name); §5.2.6.4 admits each at most once — a second is COBOLNET1652.
            if (para.objectComputerParagraph() is { } ocp)
            {
                foreach (var oc in ocp.objectComputerClause())
                {
                    using var _ = Edition.At(oc);
                    if (oc.programCollatingSequenceClause() is { } pcs)
                    {
                        if (pcsClause is not null)
                            Edition.Error(DiagnosticCatalog.ObjectComputerDuplicateClause,
                                "the OBJECT-COMPUTER PROGRAM COLLATING SEQUENCE clause is specified more than once "
                                + "(ISO §12.3.6.2 — the paragraph's clauses may each appear at most once, §5.2.6.4)");
                        pcsClause = pcs;
                    }
                    // §12.3.6 CHARACTER CLASSIFICATION (Annex A.4.9 item 7) — IMPLEMENTED since kb/Work PB64 T5 (it was
                    // refused by name with COBOLNET1518 since PB78, and swallowed by the attribute sink before that).
                    // Saved and resolved AFTER the SPECIAL-NAMES paragraph, whose LOCALE clauses declare the names it
                    // references (§12.3.6.3 SR3) — the PCS takes the same path. A second clause is COBOLNET1652.
                    if (oc.characterClassificationClause() is { } cc)
                    {
                        if (ccClause is not null)
                            Edition.Error(DiagnosticCatalog.ObjectComputerDuplicateClause,
                                "the OBJECT-COMPUTER CHARACTER CLASSIFICATION clause is specified more than once "
                                + "(ISO §12.3.6.2 — the paragraph's clauses may each appear at most once, §5.2.6.4)");
                        ccClause = cc;
                    }
                }
            }
            if (para.specialNamesParagraph() is not { } sn) continue;
            foreach (var entry in sn.specialNameEntry())
            {
                using var _ = Edition.At(entry);
                // §12.3.7 LOCALE clause (Annex A.4.9 item 10, the clause half) — IMPLEMENTED since kb/Work PB64 T1: the
                // locale-name is DECLARED (DataBinder.LocaleBind). It used to be refused by name with COBOLNET1518
                // (PB25 → T0), and before that a raw parse error at the clause's own literal.
                if (entry.localeClause() is { } loc) { LocaleBind(loc); continue; }
                if (entry.orderTableClause() is { } ot) { OrderTableBind(ot); continue; }
                if (entry.alphabetClause() is { } alpha) { AlphabetBind(alpha); continue; }
                // CLASS and SYMBOLIC CHARACTERS bind AFTER the walk (kb/Work PB110): their IN alphabet-name may be
                // declared by a LATER ALPHABET clause of the same paragraph (the clauses are order-free — the
                // ResolveProgramCollating shape).
                if (entry.classDefinitionClause() is { } cd) { classClauses.Add(cd); continue; }
                if (entry.symbolicCharactersClause() is { } sc) { symbolicClauses.Add(sc); continue; }
                if (entry.decimalPointClause() is { } dp) { SwitchBindDecimalPoint(dp); continue; }
                if (entry.currencySignClause() is { } cur) { SwitchBindCurrency(cur); continue; }
                // §12.3.7 CURSOR / CRT STATUS (Annex A.4.2 item 25) — the SCREEN module's environment-division
                // surface. ⚠ Both clauses PARSED and were read by no binder at all until kb/Work PB260, so
                // `CRT STATUS IS WS-CRT` compiled clean with ZERO diagnostics: a declined facility that a program
                // could write and get a clean compile out of is not declined, it is undiagnosed. Refused by name
                // (COBOLNET1560), the same posture as the SCREEN SECTION they belong to.
                if (entry.cursorClause() is { } curs) { ScreenFacility.ReportCursorClause(Edition, curs); continue; }
                if (entry.crtStatusClause() is { } crt) { ScreenFacility.ReportCrtStatusClause(Edition, crt); continue; }
                if (entry.implementorSwitchEntry() is not { } sw) continue;
                var ids = sw.cobolWord();   // [0] = switch-name; [1] = mnemonic-name when Option 1
                if (ids.Length == 0) continue;
                string? onName = sw.switchOnClause()?.cobolWord()?.GetText();
                string? offName = sw.switchOffClause()?.cobolWord()?.GetText();
                // Only a genuine switch clause registers: a mnemonic (Option 1) or ≥1 status condition (Option 2)
                // — the §12.3.7 format requires at least one of the three phrases.
                if (ids.Length < 2 && onName is null && offName is null) continue;

                string implName = ids[0].GetText();
                SwitchMnemonics.TryAdd(ids.Length >= 2 ? ids[1].GetText() : implName, implName);
                if (onName is not null) SwitchConditions.TryAdd(onName, (implName, true));
                if (offName is not null) SwitchConditions.TryAdd(offName, (implName, false));
            }
        }
        // The PCS resolves AFTER the walk (OBJECT-COMPUTER precedes SPECIAL-NAMES in source, §12.3.6 GR9); only
        // the NAMED alphabets become the program sequences — other defined alphabets have no effect (NC219A's
        // unreferenced COLLATING-SEQ-2). A native-order alphabet leaves the sequence null (the fast path).
        if (pcsClause is not null) ResolveProgramCollating(pcsClause);
        if (ccClause is not null) ResolveClassification(ccClause);
        foreach (var cd in classClauses) SwitchBindClass(cd);           // after the alphabets exist (PB110)
        foreach (var sc in symbolicClauses) SwitchBindSymbolic(sc);
        FinalizeCurrencySigns();   // §12.3.7.3 r25 — the implied '$' clause, once every explicit clause is in
    }

    /// <summary>Resolve the PROGRAM COLLATING SEQUENCE clause (ISO §12.3.6): the IS form's alphabet-name-1
    /// [alphabet-name-2], or the FOR ALPHANUMERIC / FOR NATIONAL forms — each class at most once (§12.3.6.2
    /// braces one of each). SR1: alphabet-name-1 shall reference an alphabet defining an ALPHANUMERIC collating
    /// sequence — an unknown name-1 stays inert (the historical single-name leniency; NC219A posture), but a
    /// NATIONAL alphabet in the slot is the class-mismatch error. SR2: alphabet-name-2 shall reference an
    /// alphabet defining a NATIONAL collating sequence — a UTF-8/UTF-16 alphabet references a coded character
    /// set but NO collating sequence (§12.3.7 Table 6), and an alphanumeric-class or undeclared name is the
    /// mismatch; the national slot is a new surface, so it is strict.</summary>
    private void ResolveProgramCollating(Core.ProgramCollatingSequenceClauseContext pcs)
    {
        string? alnumName = null, natName = null;
        var fors = pcs.collatingForPhrase();
        if (fors.Length > 0)
        {
            foreach (var f in fors)
            {
                bool isNat = f.NATIONAL() is not null;
                ref string? slot = ref isNat ? ref natName : ref alnumName;
                if (slot is not null)
                    Edition.Error("COBOLNET0898", "PROGRAM COLLATING SEQUENCE: the FOR "
                        + $"{(isNat ? "NATIONAL" : "ALPHANUMERIC")} phrase may be specified only once "
                        + "(ISO §12.3.6.2 general format)");
                slot = f.cobolWord().GetText();
            }
        }
        else
        {
            var words = pcs.cobolWord();
            alnumName = words.Length > 0 ? words[0].GetText() : null;
            natName = words.Length > 1 ? words[1].GetText() : null;
        }

        if (alnumName is not null)
        {
            if (Alphabets.TryGetValue(alnumName, out var def)) Collating = def.IsIdentity ? null : def;
            else if (NationalAlphabets.ContainsKey(alnumName))
                Edition.Error("COBOLNET0898", $"PROGRAM COLLATING SEQUENCE '{alnumName}': alphabet-name-1 "
                    + "shall reference an alphabet that defines an ALPHANUMERIC collating sequence — this "
                    + "alphabet is defined FOR NATIONAL (ISO §12.3.6 SR1)");
            // else: an undeclared name-1 stays inert (the historical 85-surface leniency).
        }
        if (natName is not null)
        {
            if (!NationalAlphabets.TryGetValue(natName, out var def))
                Edition.Error("COBOLNET0898", $"PROGRAM COLLATING SEQUENCE FOR NATIONAL '{natName}': "
                    + "alphabet-name-2 shall reference an alphabet that defines a NATIONAL collating sequence "
                    + $"({(Alphabets.ContainsKey(natName) ? "this alphabet is alphanumeric — write ALPHABET … FOR NATIONAL" : "no such national alphabet is declared in SPECIAL-NAMES")}; "
                    + "ISO §12.3.6 SR2)");
            else if (!def.HasCollatingSequence)
                Edition.Error("COBOLNET0898", $"PROGRAM COLLATING SEQUENCE FOR NATIONAL '{natName}': a "
                    + $"{def.Phrase} alphabet references a coded character set but NOT a collating sequence "
                    + "(ISO §12.3.7 GR7 Table 6) — only NATIVE, UCS-4, and literal-phrase national alphabets "
                    + "may collate (ISO §12.3.6 SR2)");
            else
                NationalCollating = def.IsIdentity ? null : def;   // null for NATIVE/UCS-4 — the identity fast path (D-N3)
        }
    }

    /// <summary>Bind the OBJECT-COMPUTER <c>CHARACTER CLASSIFICATION {IS locale-phrase-1 [locale-phrase-2] | {FOR ALPHANUMERIC
    /// IS locale-phrase-1 | FOR NATIONAL IS locale-phrase-2}…}</c> clause (ISO §12.3.6.2; kb/Work PB64 T5): each phrase is
    /// LOCALE (the current locale at activation), SYSTEM-DEFAULT, USER-DEFAULT, or a locale-name of the SPECIAL-NAMES
    /// paragraph (§12.3.6.3 SR3 — undeclared → COBOLNET1664, the ONE undeclared-locale-name diagnostic); the two-phrase IS
    /// form gives the alphanumeric then the national classification (GR5 a–j); a class named twice in the FOR form is
    /// a form violation (§5.2.6.4 — each at most once).</summary>
    private void ResolveClassification(Core.CharacterClassificationClauseContext cc)
    {
        using var _ = Edition.At(cc);
        LocalePhrase? alphanumeric = null, national = null;
        var fors = cc.classificationForPhrase();
        if (fors.Length > 0)
        {
            foreach (var f in fors)
            {
                bool nat = f.NATIONAL() is not null;
                var phrase = ClassificationPhrase(f.cobolWord().GetText(), nat);
                if (phrase is null) return;
                if ((nat ? national : alphanumeric) is not null)
                {
                    Edition.Error("COBOLNET0898", $"CHARACTER CLASSIFICATION FOR {(nat ? "NATIONAL" : "ALPHANUMERIC")} is specified more than once "
                        + "— each alternative of the clause's brace shall be specified at most once (ISO §12.3.6.2 / §5.2.6.4)");
                    return;
                }
                if (nat) national = phrase; else alphanumeric = phrase;
            }
        }
        else
        {
            var words = cc.cobolWord();   // [0] = CLASSIFICATION, [1] = locale-phrase-1, [2] = locale-phrase-2
            if (words.Length < 2) return;  // a malformed shape already drew a parse error
            alphanumeric = ClassificationPhrase(words[1].GetText(), national: false);
            if (alphanumeric is null) return;
            if (words.Length > 2)
            {
                national = ClassificationPhrase(words[2].GetText(), national: true);
                if (national is null) return;
            }
        }
        Classification = new ClassificationSpec(alphanumeric, national);
    }

    private LocalePhrase? ClassificationPhrase(string word, bool national)
    {
        // >>COBOL-WORDS (ISO §7.3.10.4 GR2/GR3/GR4; kb/Work PB250): a §8.9/§8.10 word the lexer does not
        // tokenize is reached ONLY through the map; a raw text test here is inert to the directive both ways.
        if (CobolWords.Is(word, "LOCALE")) return new LocalePhrase(Runtime.Globalization.LocalePhraseKind.Current, null);
        if (CobolWords.Is(word, "SYSTEM-DEFAULT")) return new LocalePhrase(Runtime.Globalization.LocalePhraseKind.SystemDefault, null);
        if (CobolWords.Is(word, "USER-DEFAULT")) return new LocalePhrase(Runtime.Globalization.LocalePhraseKind.UserDefault, null);
        var sym = ResolveLocaleName(word, $"CHARACTER CLASSIFICATION{(national ? " FOR NATIONAL" : "")} {word}",
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
    ///   figurative operand is COBOLNET0898.</item>
    ///   <item>SR11 — literal-9 "shall specify neither a symbolic-character figurative constant nor a zero-length
    ///   literal".</item>
    ///   <item>The clause is BRACKETED and unrepeated in the general format (unlike LOCALE / CURRENCY / CLASS,
    ///   which carry a printed ellipsis), so a second ORDER TABLE clause — or a repeated ordering-name — is a form
    ///   violation.</item>
    /// </list>
    /// <para>⚖ The ADVISORY (COBOLNET1662, a warning): GR17 leaves literal-9's allowable content to the
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
    /// declared twice is COBOLNET1665 (§8.3.2.2 — a user-defined word of one type is unique within its scope). §12.3.7.4
    /// GR5 — the implementor specifies the allowable content: DETERMINATION L1, a locale tag, POSIX spellings normalized
    /// (<see cref="LocaleSymbol.Tag"/>); availability is NOT checked here (§8.1.5 — run time; L1 item 4).</summary>
    private void LocaleBind(Core.LocaleClauseContext loc)
    {
        var words = loc.cobolWord();                  // [0] = the keyword LOCALE, [1] = locale-name-1, [2] = external-locale-name-1 (word branch)
        string name = words[1].GetText();
        string external;
        bool fromLiteral = loc.literal() is not null;
        if (fromLiteral)
        {
            // SR10 / SR11 for literal-4 — the ONE text-literal rule the ORDER TABLE clause's literal-9 shares.
            if (!TryClauseTextLiteral(loc.literal()!, $"LOCALE {name} IS {loc.literal()!.GetText()}", "literal-4", out external)) return;
        }
        else external = words[2].GetText();
        var symbol = new LocaleSymbol(name, external, fromLiteral);
        if (!Locales.TryAdd(name, symbol))
        {
            Edition.Error("COBOLNET1665", $"LOCALE {name}: the locale-name is already declared in this SPECIAL-NAMES paragraph "
                + $"(as {Locales[name]}); a user-defined word is unique within its scope (ISO §8.3.2.2 / §12.3.7.2)");
        }
    }

    /// <summary>Resolve a locale-name reference (ISO §12.3.7.3 SR24 for the ALPHABET clause; §14.9.39.3 SR26 for SET
    /// LOCALE; the later increments' sites) to its symbol, or null after COBOLNET1664 — the ONE undeclared-locale-name
    /// diagnostic, with the citing site named (DESIGN-locale-facility §7 rule a).</summary>
    internal LocaleSymbol? ResolveLocaleName(string name, string site, string rule)
    {
        if (Locales.TryGetValue(name, out var sym)) return sym;
        Edition.Error("COBOLNET1664", $"{site}: '{name}' is not a locale-name declared by a SPECIAL-NAMES LOCALE clause ({rule})"
            + (Locales.Count == 0 ? "; no LOCALE clause is in scope" : $"; declared: {string.Join(", ", Locales.Keys)}"));
        return null;
    }

    /// <summary>The ONE rule for a SPECIAL-NAMES clause's TEXT literal (ISO §12.3.7.3 SR10 — "shall be alphanumeric or
    /// national"; SR11 — "shall specify neither a symbolic-character figurative constant nor a zero-length literal"),
    /// shared by the ORDER TABLE clause's literal-9 and the LOCALE clause's literal-4: decodes the literal (a
    /// concatenation expression folds first — §8.8.3.3 GR3 makes it usable anywhere a literal of its class is),
    /// reporting COBOLNET0898 and returning false on a violation. <paramref name="what"/> names the clause for the
    /// message; <paramref name="operand"/> the literal's role ("literal-9").</summary>
    private bool TryClauseTextLiteral(Core.LiteralContext lit, string what, string operand, out string text)
    {
        text = "";
        string raw = lit.GetText();
        var nn = lit.nonNumericLiteral();
        // SR11's FIRST half, reported before SR10 so a figurative constant draws the rule that names it.
        if (nn?.figurativeConstant() is not null)
        {
            Edition.Error("COBOLNET0898", $"{what}: {operand} shall specify neither a "
                + "symbolic-character figurative constant nor a zero-length literal (ISO §12.3.7.3 SR11)");
            return false;
        }
        // §8.8.3.3 GR3 makes a concatenation expression "equivalent to a literal of the same class and value,
        // [usable] anywhere a literal of that class may be used", so `IS "ISO " & "14651_2020_TABLE1"` is legal
        // here and folds to the one literal SR10 then classes. Refusing it would reject conforming source.
        if (nn?.concatenationExpression() is { } cat)
        {
            // The collating arguments are NULL deliberately: a figurative HIGH-/LOW-VALUE written INSIDE
            // SPECIAL-NAMES takes the NATIVE extremes (the ALPHABET binder's GR10 note), and the PCS is not
            // resolved until after this walk anyway.
            var folded = ConcatFolder.Fold(cat, Edition, collate: null);
            if (folded.Category is not (PicCategory.Alphanumeric or PicCategory.National))
            {
                Edition.Error("COBOLNET0898", $"{what}: {operand} shall be an alphanumeric "
                    + "or national literal (ISO §12.3.7.3 SR10); this concatenation expression is class "
                    + $"{folded.Category} (§8.8.3.3 GR1/GR3)");
                return false;
            }
            text = folded.Value;
        }
        else
        {
            // SR10 — alphanumeric or national. CobolLiteral.ClassOf answers from the PREFIX, so it admits both
            // quoting forms and both hexadecimal spellings (X"…" is one FORM of an alphanumeric literal,
            // §8.3.3.2) and refuses a boolean one; a NUMERIC literal is not a quoted literal and lands here too.
            if (CobolLiteral.ClassOf(raw) is not (LiteralClass.Alphanumeric or LiteralClass.National))
            {
                Edition.Error("COBOLNET0898", $"{what}: {operand} shall be an alphanumeric or "
                    + "national literal (ISO §12.3.7.3 SR10)");
                return false;
            }
            text = CobolLiteral.Decode(raw);
        }
        // SR11's SECOND half.
        if (text.Length == 0)
        {
            Edition.Error("COBOLNET0898", $"{what}: {operand} shall specify neither a "
                + "symbolic-character figurative constant nor a zero-length literal (ISO §12.3.7.3 SR11)");
            return false;
        }
        return true;
    }

    private void OrderTableBind(Core.OrderTableClauseContext ot)
    {
        // ONE cobolWord: ordering-name-1. The clause's own keywords ORDER and TABLE are both lexer tokens since
        // kb/Work PB704 — ORDER used to ride cobolWord as slot [0], which is exactly the shape the §8.9 funnel
        // mistook for a user-defined word.
        if (ot.cobolWord() is not { } name1 || ot.literal() is not { } lit) return;   // a malformed shape already drew a parse error
        string name = name1.GetText();
        string raw = lit.GetText();
        // SR10 / SR11 for literal-9 — the ONE text-literal rule the LOCALE clause's literal-4 shares.
        if (!TryClauseTextLiteral(lit, $"ORDER TABLE {name} IS {raw}", "literal-9", out string text)) return;
        // ⚠ ONE CLAUSE PER PARAGRAPH, measured off the PRINTED general format rather than inferred from GR17's
        // singular wording: §12.3.7.2 brackets `ORDER TABLE ordering-name-1 IS literal-9` with NO trailing
        // ellipsis, where the repeatable clauses beside it (CLASS, CURRENCY, LOCALE, the switch entry, SYMBOLIC
        // CHARACTERS) each print one. A second clause is therefore a form violation, and so — as a consequence
        // rather than as a second rule — is a second ordering-name.
        if (OrderTables.Count > 0)
        {
            Edition.Error("COBOLNET0898", $"ORDER TABLE {name}: the ORDER TABLE clause may be specified only once "
                + "in a SPECIAL-NAMES paragraph — its general format brackets it without an ellipsis, unlike the "
                + $"repeatable clauses (ISO §12.3.7.2); '{OrderTables.Keys.First()}' is already declared");
            return;
        }
        OrderTables[name] = text;
        // GR17's implementor half, checked against the ONE resolver the runtime uses (§15.85.4 r2 owns the outcome).
        if (!CollationEngine.TryGetOrderingTable(text, out _))
            Edition.Warning(DiagnosticCatalog.OrderTableUnresolved, $"ORDER TABLE {name} IS {raw}: "
                + $"'{text}' does not name a cultural ordering table this implementation provides (ISO §12.3.7.4 "
                + "GR17 leaves literal-9's allowable content to the implementor: the default table "
                + $"'{CollationEngine.DefaultOrderingTableName}' — case-insensitive, space and underscore "
                + "interchangeable — or a CLDR locale tag naming a tailored collation). Every FUNCTION "
                + $"STANDARD-COMPARE reference to {name} will set EC-ORDER-NOT-SUPPORTED at run time (§15.85.4 r2)");
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
        // (§12.3.7.2); the historical postfix position is an accepted superset — either site names the
        // class, both at once is malformed.
        string name = alpha.cobolWord().GetText();
        var def = alpha.alphabetDefinition();
        var fors = alpha.specialNamesForPhrase();
        if (fors.Length > 1)
            Edition.Error("COBOLNET0898", $"ALPHABET {name}: the FOR phrase may be written once — between "
                + "alphabet-name and IS (ISO §12.3.7.2 general format)");
        bool national = fors.Any(f => f.NATIONAL() is not null);
        // `IS LOCALE [locale-name-2]` — either branch (§12.3.7.2): Annex A.4.9 item 10 ("LOCALE phrases in the
        // ALPHABET clause"). LOCALE is not a lexer token, so the phrase arrives as one or two code-name-shaped entries
        // (kb/Work PB100 fixed the false "reserved word used as a user-defined word" it used to draw); it is a plain
        // word below 2002 (a code-name-1 there), so the phrase is 2002+ only. Since kb/Work PB101 the bare form is
        // IMPLEMENTED and the named form is refused by name until the LOCALE clause lands (design §12 T1).
        if (Edition.DialectLevel >= 2002 && IsAlphabetLocalePhrase(def, CobolWords))
        {
            // `IS LOCALE` (no locale-name-2) — the LOCALE-based collating sequence of the locale CURRENT at each use
            // (§12.3.7.4 GR7e; §12.3.6.4 GR11/GR12): the runtime LocaleCollation over the derived CLDR/UCA engine (kb/Work
            // PB101 — the T3 arm; determination L5 makes one locale sequence serve both classes). Table 6 row LOCALE: a
            // collating sequence, NOT a coded character set — CODE-SET / SYMBOLIC … IN / CLASS … IN may not name it
            // (§12.3.7.3 SR16g/SR17d); those three alphabet references are inert in this compiler today (no binder
            // resolves them), so there is no name check to extend yet — recorded here so the check lands with them.
            // `IS LOCALE locale-name-2` (§12.3.7.3 SR24: "locale-name-2 shall be a locale-name defined by the LOCALE clause")
            // — the NAMED form (T1): the sequence of THAT locale, whose external identification the symbol carries and
            // whose availability is decided at use (EC-LOCALE-MISSING). An undeclared name is COBOLNET1664 and the
            // alphabet falls back to the current-locale form so the rest of the program still binds.
            var locale = LocaleCollatingSpec.CurrentLocale;
            if (def.alphabetEntry().Length == 2)
            {
                var sym = ResolveLocaleName(def.alphabetEntry()[1].GetText(), $"ALPHABET {name}{(national ? " FOR NATIONAL" : "")} IS LOCALE {def.alphabetEntry()[1].GetText()}",
                    "ISO §12.3.7.3 SR24 — locale-name-2 shall be a locale-name defined by the LOCALE clause");
                if (sym is not null) locale = new LocaleCollatingSpec(new LocaleRef(sym));
            }
            if (national) NationalAlphabets.TryAdd(name, new NationalAlphabetDef(null, locale, HasCollatingSequence: true, "LOCALE"));
            else Alphabets.TryAdd(name, new AlphabetDef(null, locale, "LOCALE"));
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
        if (CodeSetNameOf(def, CobolWords) is { } wrongBranch)
        {
            Edition.Error("COBOLNET0898", $"ALPHABET {name} IS {wrongBranch}: the {wrongBranch} coded character "
                + "set may be referenced only in the FOR NATIONAL branch of the ALPHABET clause "
                + "(ISO §12.3.7.2 general format)");
            return;
        }
        if (def.NATIVE() is not null || def.STANDARD_1() is not null || def.STANDARD_2() is not null)
        {
            // One identity COLLATING SEQUENCE, three distinct CODED CHARACTER SETS (Table 6 + GR7 c/d: STANDARD-1/2
            // are the 128 ISO/IEC 646 IRV characters; NATIVE is the whole native set) — the Phrase carries which
            // (kb/Work PB110); IsIdentity stays true, so no runtime carrier is emitted either way.
            Alphabets.TryAdd(name, def.NATIVE() is not null ? AlphabetDef.Native
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
                Alphabets.TryAdd(name, new AlphabetDef(codeName!.Table, null, codeName.Name, codeName));
                return;
        }

        if (AlphabetLiteralPhrase(name, def, national: false) is { } table)
            Alphabets.TryAdd(name, new AlphabetDef(table, null, "literal-phrase"));
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
        if (def.alphabetEntry() is not [{ ChildCount: 1 } only] || only.GetChild(0) is not Core.CobolWordContext cw
            || IsAlphabetFigurativeWord(cw.GetText()))
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
    private static bool IsAlphabetFigurativeWord(string word) => AlphabetFigurative(word, national: false) is not null;

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
        string what = $"ALPHABET {name}{(national ? " FOR NATIONAL" : "")}";
        var pos = new Dictionary<char, ushort>();
        var specOrder = new List<char>();       // every specified character in source order (the GR8/GR9 tie rules)
        var repByPos = new List<char>();        // per position: the FIRST character DEFINED there (§15.15.4 r2 / GR7 k6)
        ushort next = 0;
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
            var operands = AlphabetOperands(name, entry, national);
            if (operands.Count == 0) continue;
            bool thru = entry.THRU() is not null || entry.THROUGH() is not null;
            if (thru || entry.ALSO().Length > 0)
            {
                // SR14 b3/c3: "Each … literal, when a THROUGH or ALSO phrase is specified, shall be one character
                // in length." A multi-character operand used to be silently DROPPED on the alphanumeric arm — the
                // whole entry vanished from the table with no diagnostic (kb/Work PB770 leg b).
                bool ok = true;
                foreach (var op in operands)
                    if (op.Length != 1)
                    {
                        Edition.Error(DiagnosticCatalog.AlphabetClauseViolation, $"{what}: the operand '{op}' is "
                            + $"{op.Length} characters — each {(national ? "national" : "alphanumeric")} literal, when a "
                            + $"THROUGH or ALSO phrase is specified, shall be one character in length (ISO §12.3.7.3 "
                            + $"SR14 {(national ? "c3" : "b3")})");
                        ok = false;
                    }
                if (!ok) continue;
            }
            if (thru)
            {
                // k5: the native run from operand-1 to operand-2, in EITHER direction, ascending positions.
                if (operands.Count < 2) continue;          // a malformed shape already drew a parse error
                int a = operands[0][0], b = operands[1][0], step = a <= b ? 1 : -1;
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
                foreach (var op in operands) Assign(op[0], advance: false);
                if (repByPos.Count > before) next++;
                continue;
            }
            // k1b: a (possibly multi-character) literal — each character, leftmost first, ascending positions.
            foreach (char c in operands[0]) Assign(c, advance: true);
        }
        if (specOrder.Count == 0) return null;      // nothing was specified — the operand diagnostics stand alone

        // The sparse arrays and the §12.3.7.4 GR8/GR9 extremes — CollatingTable.Build, the ONE place either is
        // computed, shared with the implementor code-name arm (GR7 i/j) which walks a code page instead of a
        // clause. This method used to carry its own copy of the extremes rule (kb/Work PB793).
        return CollatingTable.Build(pos, specOrder, repByPos, next, national);
    }

    /// <summary>A character named in a diagnostic: its literal form when it is printable, else its U+ code point.</summary>
    private static string DescribeChar(char c) => c > ' ' && c < (char)0x7F ? $"'{c}'" : $"U+{(int)c:X4}";

    /// <summary>The national coded-character-set name a definition consists of ("UCS-4" / "UTF-8" / "UTF-16"),
    /// or null. These are §8.9 CONTEXT-SENSITIVE words scoped to the ALPHABET clause — they arrive as a single
    /// plain <c>cobolWord</c> alphabet entry (never lexer keywords; they stay user-definable elsewhere), so the
    /// shape is: exactly one entry, no THROUGH/ALSO, one cobolWord, whose text is one of the three names.</summary>
    /// <summary>The ALPHABET clause's `IS LOCALE [locale-name-2]` phrase (ISO §12.3.7.2; kb/Work PB100): the first
    /// definition entry is the bare word LOCALE, optionally followed by one bare-word entry (locale-name-2) — LOCALE is
    /// not a lexer token, so the phrase arrives as one or two code-name-shaped entries.</summary>
    internal static bool IsAlphabetLocalePhrase(Core.AlphabetDefinitionContext def, Editions.CobolWordsMap cobolWords)
    {
        var entries = def.alphabetEntry();
        if (entries.Length is 0 or > 2) return false;
        foreach (var e in entries)
            if (e.THRU() is not null || e.THROUGH() is not null || e.ALSO().Length > 0 || e.ChildCount != 1 || e.GetChild(0) is not Core.CobolWordContext)
                return false;
        return cobolWords.Is(entries[0].GetText(), "LOCALE");
    }

    private static string? CodeSetNameOf(Core.AlphabetDefinitionContext def, Editions.CobolWordsMap cobolWords)
    {
        if (def.alphabetEntry() is not [{ } entry]) return null;
        if (entry.THRU() is not null || entry.THROUGH() is not null || entry.ALSO().Length > 0) return null;
        if (entry.ChildCount != 1 || entry.GetChild(0) is not Core.CobolWordContext w) return null;
        // >>COBOL-WORDS (ISO §7.3.10.4 GR2/GR3/GR4; kb/Work PB250): a §8.9/§8.10 word the lexer does not
        // tokenize (UCS-4 / UTF-8 / UTF-16 are §8.10 context-sensitive) is reached ONLY through the map. The
        // CANONICAL name is what is returned, so the downstream Phrase tag never carries a user synonym.
        string? t = cobolWords.Resolve(w.GetText().ToUpperInvariant());
        return t is "UCS-4" or "UTF-8" or "UTF-16" ? t : null;
    }

    /// <summary>Bind an <c>ALPHABET … FOR NATIONAL IS …</c> clause (ISO §12.3.7.2 second branch): NATIVE — the
    /// native national coded set and collating sequence (GR7 d2, identity); UCS-4 — the ISO/IEC 10646 coded set
    /// AND its appearance-order collating sequence (GR7 f), which on the D-N1 one-UTF-16-code-unit-per-position
    /// substrate IS the native code-unit order (see <see cref="NationalAlphabetDef"/> — §8.5.1.4 denies surrogate
    /// -pair recognition, so the supplementary-plane codepoint/code-unit divergence is unreachable; the
    /// correspondence is the BMP identity, implementor item 188); UTF-8/UTF-16 — coded character sets ONLY
    /// (GR7 g/h + Table 6: no collating sequence); a literal phrase — the sparse national collating table
    /// (GR7 k over the native national set). STANDARD-1/STANDARD-2 and LOCALE are not in the national branch's
    /// format (STANDARD-1/2 are alphanumeric-branch-only; the LOCALE phrase has no compiler surface — the locale
    /// subsystem is unimplemented and the word fails loud at parse). Unknown words are code-name-2 — the
    /// implementor supports none (§12.3.7.3 SR15).</summary>
    private void AlphabetBindNational(string name, Core.AlphabetDefinitionContext def)
    {
        if (def.NATIVE() is not null)
        {
            NationalAlphabets.TryAdd(name, new NationalAlphabetDef(null, HasCollatingSequence: true, "NATIVE"));
            return;
        }
        if (def.STANDARD_1() is not null || def.STANDARD_2() is not null)
        {
            Edition.Error("COBOLNET0898", $"ALPHABET {name} FOR NATIONAL: STANDARD-1/STANDARD-2 name the "
                + "ISO/IEC 646 ALPHANUMERIC coded character set and may be referenced only in the FOR "
                + "ALPHANUMERIC branch (ISO §12.3.7.2 general format)");
            return;
        }
        if (CodeSetNameOf(def, CobolWords) is { } cs)
        {
            // UCS-4 references BOTH a coded set and a collating sequence; UTF-8/UTF-16 a coded set ONLY
            // (§12.3.7 GR7 f/g/h + Table 6). All three collapse to the identity on this substrate — the
            // difference is only WHERE the name may legally be referenced.
            NationalAlphabets.TryAdd(name, new NationalAlphabetDef(null, HasCollatingSequence: cs == "UCS-4", cs));
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
                NationalAlphabets.TryAdd(name, new NationalAlphabetDef(codeName!.Table, null,
                    HasCollatingSequence: true, codeName.Name, codeName));
                return;
        }
        if (AlphabetLiteralPhrase(name, def, national: true) is { } table)
            NationalAlphabets.TryAdd(name, new NationalAlphabetDef(table, HasCollatingSequence: true, "literal-phrase"));
    }

    /// <summary>⛔ THE ONE alphabet-entry operand decoder (ISO §12.3.7.3 SR14 b for the ALPHANUMERIC arm, SR14 c
    /// for the NATIONAL one — the SAME four sub-rules, differing only in which literal class each operand shall be
    /// and which native set an ordinal indexes). Both arms had their own copy until kb/Work PB770, and the
    /// alphanumeric copy implemented NONE of the rules: it borrowed the CLASS clause's <c>LiteralChars</c> for the
    /// ordinal range (so an out-of-range ALPHABET ordinal reported <c>CLASS : … §12.3.7.3 SR17 b2</c> on a program
    /// with no CLASS clause), accepted a noninteger literal, and turned an unrecognized word into the characters of
    /// its own spelling. <c>feedback_two_arm_dispatch</c>, fifth instance.</summary>
    /// <remarks>b1/c1 — "<i>Each numeric literal shall be an unsigned integer and shall have a value within the
    /// range of one through the maximum number of characters in the native … character set</i>": the ordinal is
    /// 1-based, so it names code unit <c>ordinal − 1</c> of the 65,536-character repertoire (§12.3.7.4 GR7 k1a).
    /// b2/c2 — "<i>Each noninteger literal shall be an alphanumeric / a national literal</i>". GR10 — the figurative
    /// words written inside SPECIAL-NAMES are the NATIVE extremes/values of the clause's class.</remarks>
    private List<string> AlphabetOperands(string name, Core.AlphabetEntryContext entry, bool national)
    {
        string what = $"ALPHABET {name}{(national ? " FOR NATIONAL" : "")}";
        var result = new List<string>();
        for (int i = 0; i < entry.ChildCount; i++)
        {
            switch (entry.GetChild(i))
            {
                // ⛔ A FIGURATIVE CONSTANT IS A LITERAL, not a word: `nonNumericLiteral : figurativeConstant | …`,
                // so SPACE / LOW-VALUE / QUOTE reach here through the literal arm whenever the spelling is a lexer
                // token — which is every ordinary spelling. The cobolWord arm below is the non-tokenized route
                // (a >>COBOL-WORDS synonym). Both call the ONE GR10 mapping.
                case Core.LiteralContext { } figLit when figLit.nonNumericLiteral()?.figurativeConstant() is { } fig:
                    if (fig.ALL() is not null)
                    {
                        // §12.3.7.3 SR11 — literal-1/-2/-3 "shall specify neither a symbolic-character figurative
                        // constant nor a zero-length literal"; and an ALL figurative has no length of its own,
                        // which GR7 k1b's per-character positioning would need.
                        Edition.Error(DiagnosticCatalog.AlphabetClauseViolation, $"{what}: {fig.GetText()} — an ALL "
                            + "figurative constant is not an operand of a literal phrase; a symbolic-character "
                            + "figurative constant is forbidden outright (ISO §12.3.7.3 SR11)");
                        break;
                    }
                    if (AlphabetFigurative(fig.GetText(), national) is { } figValue) result.Add(figValue);
                    else Edition.Error(DiagnosticCatalog.AlphabetClauseViolation, $"{what}: {fig.GetText()} — the "
                        + "figurative constant is not a character of the native character set, so it cannot take a "
                        + "position in a collating sequence (ISO §12.3.7.4 GR10 names HIGH-VALUE, LOW-VALUE, SPACE, "
                        + "QUOTE and ZERO)");
                    break;
                case Core.LiteralContext lit:
                    string text = lit.GetText();
                    if (int.TryParse(text, out int ordinal))
                    {
                        // b1/c1, BOTH halves of one sentence: "shall be an UNSIGNED INTEGER **and** shall have a
                        // value within the range of one through the maximum number of characters in the native …
                        // character set". ⛔ int.TryParse accepts a leading sign, so the unsigned half has to be
                        // asked separately — otherwise `+5` reads as ordinal 5 and only a NEGATIVE value is caught,
                        // by the range half, which is a different rule answering for this one.
                        if (text[0] is '+' or '-')
                            Edition.Error(DiagnosticCatalog.AlphabetClauseViolation, $"{what}: {text} — each numeric "
                                + "literal shall be an UNSIGNED integer (ISO §12.3.7.3 SR14 "
                                + $"{(national ? "c1" : "b1")})");
                        else if (ordinal is >= 1 and <= CollatingTable.Repertoire) result.Add(((char)(ordinal - 1)).ToString());
                        else Edition.Error(DiagnosticCatalog.AlphabetClauseViolation, $"{what}: the ordinal {ordinal} "
                            + $"does not exist in the native {(national ? "national" : "alphanumeric")} character set "
                            + $"({CollatingTable.Repertoire} characters) — each numeric literal shall be an unsigned "
                            + $"integer with a value from one through the maximum number of characters in that set "
                            + $"(ISO §12.3.7.3 SR14 {(national ? "c1" : "b1")})");
                    }
                    else if (national ? text.Length >= 1 && text[0] is 'N' or 'n' : IsAlphanumericLiteral(lit))
                        result.Add(LiteralCharsOf(lit));
                    else
                    {
                        // b2/c2. A noninteger literal of the wrong class: name the rule, then RECOVER with the
                        // literal's characters when it is a string at all, so one bad operand does not cascade.
                        Edition.Error(DiagnosticCatalog.AlphabetClauseViolation, $"{what}: {text} — each noninteger "
                            + $"literal shall be {(national ? "a NATIONAL literal (N\"…\")" : "an alphanumeric literal")} "
                            + $"(ISO §12.3.7.3 SR14 {(national ? "c2" : "b2")})");
                        if (CobolLiteral.IsStringLiteral(text)) result.Add(CobolLiteral.Decode(text));
                    }
                    break;
                case Core.CobolWordContext w:
                    // GR10: the NATIVE extremes/values of the clause's class. ⛔ There is NO `_ => t` fallback any
                    // more: an unrecognized word is code-name-1/-2, which AlphabetCodeName has already resolved
                    // or refused for the only shape it can legally take, and inside a multi-operand phrase it is an
                    // SR14 b2/c2 violation — never the characters of its own spelling (kb/Work PB770 leg e).
                    if (AlphabetFigurative(w.GetText(), national) is { } wordValue) result.Add(wordValue);
                    else Edition.Error(DiagnosticCatalog.AlphabetClauseViolation, $"{what}: {w.GetText()} is not a "
                        + $"literal — each operand of a literal phrase shall be a numeric literal, {(national ? "a NATIONAL" : "an alphanumeric")} "
                        + $"literal or a figurative constant (ISO §12.3.7.3 SR14 {(national ? "c2" : "b2")}; §12.3.7.4 GR10)");
                    break;
            }
        }
        return result;
    }

    /// <summary>⛔ THE ONE §12.3.7.4 GR10 mapping of a figurative constant written INSIDE the SPECIAL-NAMES
    /// paragraph to the character it names: "<i>When specified as literals in the SPECIAL-NAMES paragraph, the
    /// figurative constants HIGH-VALUE and LOW-VALUE are associated with those characters having the highest and
    /// lowest positions, respectively, in the native national collating sequence, when the NATIONAL phrase is
    /// specified, or in the native alphanumeric collating sequence otherwise</i>" — so they are the NATIVE extremes
    /// here, never the sequence being defined. SPACE, QUOTE and ZERO are their §8.3.3.6 characters.
    /// <para>Null for any other word, which is then not an operand at all. The alphanumeric HIGH-VALUE stays at
    /// U+00FF: the documented §8.3.3.6 byte-stability pin recorded in PHASE4_RECONCILIATION, the same one
    /// <see cref="AlphabetExtremes"/> keeps.</para></summary>
    private static string? AlphabetFigurative(string word, bool national) => word.ToUpperInvariant() switch
    {
        "HIGH-VALUE" or "HIGH-VALUES" => national ? "\uFFFF" : "\u00FF",
        "LOW-VALUE" or "LOW-VALUES" => "\u0000",
        "SPACE" or "SPACES" => " ",
        "QUOTE" or "QUOTES" => "\"",
        "ZERO" or "ZEROS" or "ZEROES" => "0",
        _ => null,
    };

    /// <summary>Is <paramref name="lit"/> an ALPHANUMERIC literal (ISO §8.3.3.1 — both quotation forms — or the
    /// §8.3.3.2 hexadecimal format X"hh…"), as SR14 b2 requires? A national literal (N"…" / NX"…") is not.</summary>
    private static bool IsAlphanumericLiteral(Core.LiteralContext lit)
    {
        string text = lit.GetText();
        if (lit.nonNumericLiteral()?.concatenationExpression() is not null) return text.Length > 0 && text[0] is not ('N' or 'n');
        if (CobolLiteral.IsStringLiteral(text)) return true;
        return text.Length >= 3 && text[0] is 'X' or 'x' && text[1] is '"' or '\'';
    }

    /// <summary>The characters of an alphabet-entry string literal: a §8.8.3.3 GR3 concatenation folded first, the
    /// §8.3.3.2 hexadecimal format decoded pairwise, otherwise the literal's own characters. ⛔ It never resolves an
    /// ORDINAL — that is SR14 b1/c1's job and it lives in <see cref="AlphabetOperands"/>, so the ALPHABET path can
    /// no longer inherit the CLASS clause's descriptor, message and rule number (kb/Work PB770 leg d).</summary>
    private string LiteralCharsOf(Core.LiteralContext lit)
    {
        if (lit.nonNumericLiteral()?.concatenationExpression() is { } ce)
            return ConcatFolder.Fold(ce, Edition, collate: null).Value;
        string text = lit.GetText();
        if (CobolLiteral.IsStringLiteral(text)) return CobolLiteral.Decode(text);
        if (text.Length >= 3 && text[0] is 'X' or 'x' && text[1] is '"' or '\'') return CobolLiteral.DecodeHex(text);
        return text;
    }
    /// <summary>One <c>CLASS class-name IS {literal [THRU literal]}…</c> clause (ISO §12.3.7): expand each value
    /// item to its member characters — a multi-character literal lists each character; a THRU pair contributes the
    /// contiguous native-collating range between the two single-character ordinals, ASCENDING OR DESCENDING (the
    /// clause's GR allows either order — NC174A's <c>"D" THROUGH "A"</c> equals <c>"A" THRU "D"</c>).</summary>
    private void SwitchBindClass(Core.ClassDefinitionClauseContext cd)
    {
        // CLASS … FOR ALPHANUMERIC/NATIONAL — the FOR phrase edition gate is now VersionConformancePass
        // ParseArm.VisitClassDefinitionClause (14g.4, recognition).
        using var _ = Edition.At(cd);
        string name = cd.cobolWord(0).GetText();
        // The IN phrase (ISO §12.3.7.4 GR12 a; kb/Work PB110): a NUMERIC literal is the ordinal of a character
        // within the character set referenced by alphabet-name-4 — not the native set. SR17 d (a LOCALE alphabet)
        // and an undeclared name refuse through the ONE resolver; the clause then binds no class (its references
        // stay loud) rather than silently reverting to native ordinals.
        CodedCharacterSet? inSet = null;
        if (cd.cobolWord().Length > 1)
        {
            inSet = CodedCharacterSetOf(cd.cobolWord(1).GetText(), $"CLASS {name} … IN {cd.cobolWord(1).GetText()}",
                "ISO §12.3.7.3 SR17 d — alphabet-name-4 shall not reference an alphabet specified with the LOCALE phrase");
            if (inSet is null) return;
        }
        var members = new System.Text.StringBuilder();
        foreach (var item in cd.classValueSet().classValueItem())
        {
            var lits = item.literal();
            string lo = ClassLiteralChars(lits[0], inSet, name);
            if (lits.Length >= 2)
            {
                string hi = ClassLiteralChars(lits[1], inSet, name);
                if (lo.Length == 1 && hi.Length == 1)
                {
                    char a = lo[0], b = hi[0];
                    if (a > b) (a, b) = (b, a);
                    for (char c = a; c <= b; c++) members.Append(c);
                    continue;
                }
            }
            members.Append(lo);
        }
        UserClasses.TryAdd(name, members.ToString());
    }

    /// <summary>Bind one SYMBOLIC CHARACTERS clause (ISO §12.3.7.2; §12.3.7.3 SR16; §12.3.7.4 GR11 — kb/Work
    /// PB110: the clause was accepted-inert): each symbolic-character-1 defines a FIGURATIVE CONSTANT whose value
    /// is the character at ordinal integer-1 in the native character set of the clause's class (ALPHANUMERIC
    /// implied — SR16 d) or, under IN, in the coded character set of alphabet-name-3 (GR11 b/c; SR16 e1/f1 the
    /// class of that alphabet; SR16 g / the ONE resolver refuse a LOCALE alphabet). The pairing is positional
    /// per ENTRY (SR16 b), names then integers, with a one-to-one correspondence (SR16 c); a name may be defined
    /// once across ALL the paragraph's SYMBOLIC CHARACTERS clauses (SR16 a).</summary>
    private void SwitchBindSymbolic(Core.SymbolicCharactersClauseContext sc)
    {
        using var _ = Edition.At(sc);
        bool national = sc.specialNamesForPhrase()?.NATIONAL() is not null;
        CodedCharacterSet? inSet = null;
        if (sc.cobolWord() is { } inWord && inWord is not null)
        {
            inSet = CodedCharacterSetOf(inWord.GetText(), $"SYMBOLIC CHARACTERS … IN {inWord.GetText()}",
                "ISO §12.3.7.3 SR16 g — alphabet-name-3 shall not reference an alphabet specified with the LOCALE phrase");
            if (inSet is null) return;
            if (inSet.National != national)
            {
                Edition.Error(DiagnosticCatalog.SymbolicCharactersViolation, $"SYMBOLIC CHARACTERS{(national ? " FOR NATIONAL" : "")} "
                    + $"IN {inWord.GetText()}: alphabet-name-3 shall reference an alphabet that defines "
                    + $"{(national ? "a NATIONAL" : "an ALPHANUMERIC")} character set — this alphabet is "
                    + $"{(inSet.National ? "FOR NATIONAL" : "alphanumeric")} (ISO §12.3.7.3 SR16 {(national ? "f1" : "e1")})");
                return;
            }
        }
        foreach (var entry in sc.symbolicCharacterEntry())
        {
            var names = entry.cobolWord();
            var ords = entry.integerLiteral();
            if (names.Length != ords.Length)
            {
                Edition.Error(DiagnosticCatalog.SymbolicCharactersViolation, $"SYMBOLIC CHARACTERS: {names.Length} "
                    + $"symbolic-character name(s) against {ords.Length} integer(s) — there shall be a one-to-one "
                    + "correspondence, paired by position (ISO §12.3.7.3 SR16 b/c)");
                continue;
            }
            for (int i = 0; i < names.Length; i++)
            {
                string symName = names[i].GetText();
                if (!int.TryParse(ords[i].GetText(), out int ordinal)) continue;
                string? value = inSet is not null ? inSet.CharAt(ordinal)
                    : ordinal >= 1 && ordinal <= 65536 ? ((char)(ordinal - 1)).ToString() : null;
                if (value is null)
                {
                    Edition.Error(DiagnosticCatalog.SymbolicCharactersViolation, $"SYMBOLIC CHARACTERS {symName} IS "
                        + $"{ordinal}: the ordinal position does not exist in the "
                        + $"{(inSet is not null ? $"character set referenced by the IN alphabet ({inSet.Phrase}, {inSet.OrdinalCount} characters)" : "native character set (65 536 characters)")}"
                        + $" — ISO §12.3.7.3 SR16 {(national ? "f" : "e")}{(inSet is not null ? "1" : "2")}");
                    continue;
                }
                if (!SymbolicCharacters.TryAdd(symName, (value, national)))
                    Edition.Error(DiagnosticCatalog.SymbolicCharactersViolation, $"SYMBOLIC CHARACTERS: '{symName}' is "
                        + "already defined — a given symbolic-character-1 may be specified only once within the SYMBOLIC "
                        + "CHARACTER clauses of this SPECIAL-NAMES paragraph (ISO §12.3.7.3 SR16 a)");
            }
        }
    }

    /// <summary>The character content of a CLASS-clause literal-5/-6: a quoted literal's characters, or — for an
    /// unsigned integer literal — the character at that ORDINAL position of the native character set, or of the
    /// IN alphabet's coded character set when given (1-based; ISO §12.3.7.4 GR12 a — kb/Work PB110: the IN phrase
    /// used to be silently ignored, building the class from NATIVE ordinals). SR17 b2's range is the set's.
    /// <para>⛔ IT BELONGS TO THE CLASS CLAUSE AND TO NOTHING ELSE, and the now-REQUIRED <paramref name="className"/>
    /// says so at every call site. It used to be a general "literal characters" helper with optional arguments, and
    /// its two other callers inherited the CLASS descriptor, the CLASS message text with an empty name and the CLASS
    /// RULE NUMBER: the ALPHABET clause reported <c>COBOLNET1671: CLASS : … §12.3.7.3 SR17 b2</c> for an ordinal
    /// governed by SR14 b1, on a program with no CLASS clause at all, and the CURRENCY SIGN clause silently turned a
    /// numeric literal-7 that §12.3.7.3 SR18 forbids into the character at that native ordinal (kb/Work PB770 leg d
    /// and its sweep). The general form is now <see cref="LiteralCharsOf"/>, which resolves NO ordinals, so no
    /// construct can inherit another's rule number through it again.</para></summary>
    private string ClassLiteralChars(Core.LiteralContext lit, CodedCharacterSet? inSet, string className)
    {
        // §8.8.3.3 GR3: a concatenation expression stands anywhere a literal of its class may — fold an
        // ALPHABET/CLASS operand concat to its character value before decoding (GetText would glue the
        // operand tokens and mis-decode). No PCS applies here — these clauses are DEFINING the sequences.
        if (lit.nonNumericLiteral()?.concatenationExpression() is { } ce)
            return ConcatFolder.Fold(ce, Edition, collate: null).Value;
        string text = lit.GetText();
        if (CobolLiteral.IsStringLiteral(text))   // both ISO §8.3.3.1 delimiters (an apostrophe CLASS literal was miscompiled)
            return CobolLiteral.Decode(text);
        // §8.3.3.2 hexadecimal-format alphanumeric literal (X"hh…"): each hex-digit pair is one character. Without
        // this, X"FF" fell through to raw text, so its length != 1 skipped the THRU/ALSO range and the alphabet was
        // silently left native (e.g. ALPHABET … X"FF" THRU X"00" never reversed — §12.3.7.4 GR5).
        if (text.Length >= 3 && text[0] is 'X' or 'x' && text[1] is '"' or '\'')
            return CobolLiteral.DecodeHex(text);
        if (int.TryParse(text, out int ordinal))
        {
            if (inSet is not null)
            {
                // GR12 a — "the ordinal number of a character … when the IN phrase is specified, within the character
                // set referenced by alphabet-name-4"; SR17 b2 bounds it by that set's character count.
                if (inSet.CharAt(ordinal) is { } ch) return ch;
                Edition.Error(DiagnosticCatalog.ClassClauseViolation, $"CLASS {className}: the ordinal {ordinal} does not "
                    + $"exist in the character set referenced by the IN alphabet ({inSet.Phrase}, {inSet.OrdinalCount} "
                    + "characters) — ISO §12.3.7.3 SR17 b2");
                return "";
            }
            if (ordinal >= 1 && ordinal <= 65536) return ((char)(ordinal - 1)).ToString();
            Edition.Error(DiagnosticCatalog.ClassClauseViolation, $"CLASS {className}: the ordinal {ordinal} does not "
                + "exist in the native character set (65 536 characters) — ISO §12.3.7.3 SR17 b2");
            return "";
        }
        return text;
    }
}
