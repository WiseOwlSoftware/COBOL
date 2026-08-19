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
    /// (§8.8.4.1.4 — true when the operand consists entirely of members).</summary>
    public Dictionary<string, string> UserClasses { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>ALPHANUMERIC alphabet names (case-insensitive) → what the name references (ISO §12.3.7 GR7): the
    /// built collating table of a literal phrase, the LOCALE arm of an <c>IS LOCALE</c> phrase, or the identity
    /// (<see cref="AlphabetDef.Native"/>) for NATIVE/STANDARD-1/STANDARD-2 (ISO/IEC 646 order IS the Latin-1 native
    /// order — no table). An <c>ALPHABET … FOR NATIONAL</c> clause registers in <see cref="NationalAlphabets"/>
    /// instead — the two classes are disjoint reference domains (§12.3.6 SR1/SR2, §14.9.40 GR5).</summary>
    public Dictionary<string, AlphabetDef> Alphabets { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>NATIONAL alphabet names (case-insensitive) → what the name references (ISO §12.3.7 GR7 b/d2/f/g/h/k
    /// + Table 6): a null-table identity for NATIVE and the coded-set names (UCS-4 collates in ISO 10646 order,
    /// which on the D-N1 one-code-unit-per-position substrate IS the native code-unit order — see
    /// <see cref="NationalAlphabetDef"/> for the §8.5.1.4 derivation; UTF-8/UTF-16 name coded character sets ONLY),
    /// or the sparse <see cref="NationalCollatingTable"/> of a literal phrase.</summary>
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
        string literal7 = LiteralChars(lits[0]);
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
            string literal8 = lits.Length > 1 ? LiteralChars(lits[1]) : "";
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

    /// <summary>Populate the switch registry from the SPECIAL-NAMES paragraph's switch-name clauses (ISO §12.3.7
    /// general format: <c>switch-name-1 [IS mnemonic-name-1] [ON [STATUS] [IS] condition-name-1]
    /// [OFF [STATUS] [IS] condition-name-2]</c>; the NIST-85 surface also writes <c>ON IS cond</c> with no STATUS —
    /// both shapes are one grammar rule). Any switch-name is accepted (SR8 — the available names are
    /// implementor-specified; see <c>ExternalSwitches</c> for the documented item-191 contract).</summary>
    private void SwitchBindSpecialNames(Core.ProgramUnitContext program)
    {
        Core.ProgramCollatingSequenceClauseContext? pcsClause = null;
        Core.CharacterClassificationClauseContext? ccClause = null;
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
                if (entry.classDefinitionClause() is { } cd) { SwitchBindClass(cd); continue; }
                if (entry.symbolicCharactersClause() is { } sc)
                {
                    // SYMBOLIC CHARACTERS … FOR ALPHANUMERIC/NATIONAL — the FOR phrase edition gate is now
                    // VersionConformancePass ParseArm.VisitSymbolicCharactersClause (14g.4). The base SYMBOLIC
                    // CHARACTERS clause stays accepted-inert (unbound).
                    continue;
                }
                if (entry.decimalPointClause() is { } dp) { SwitchBindDecimalPoint(dp); continue; }
                if (entry.currencySignClause() is { } cur) { SwitchBindCurrency(cur); continue; }
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
        if (word.Equals("LOCALE", StringComparison.OrdinalIgnoreCase)) return new LocalePhrase(Runtime.Globalization.LocalePhraseKind.Current, null);
        if (word.Equals("SYSTEM-DEFAULT", StringComparison.OrdinalIgnoreCase)) return new LocalePhrase(Runtime.Globalization.LocalePhraseKind.SystemDefault, null);
        if (word.Equals("USER-DEFAULT", StringComparison.OrdinalIgnoreCase)) return new LocalePhrase(Runtime.Globalization.LocalePhraseKind.UserDefault, null);
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
    /// declared twice is COBOLNET1665 (§8.3.1.1.1 — a user-defined word of one type is unique within its scope). §12.3.7.4
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
                + $"(as {Locales[name]}); a user-defined word is unique within its scope (ISO §8.3.1.1.1 / §12.3.7.2)");
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
        var words = ot.cobolWord();                       // [0] = the keyword ORDER, [1] = ordering-name-1
        if (words.Length < 2 || ot.literal() is not { } lit) return;   // a malformed shape already drew a parse error
        string name = words[1].GetText();
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
        var fors = alpha.alphabetForPhrase();
        if (fors.Length > 1)
            Edition.Error("COBOLNET0898", $"ALPHABET {name}: the FOR phrase may be written once — between "
                + "alphabet-name and IS (ISO §12.3.7.2 general format)");
        bool national = fors.Any(f => f.NATIONAL() is not null);
        // `IS LOCALE [locale-name-2]` — either branch (§12.3.7.2): Annex A.4.9 item 10 ("LOCALE phrases in the
        // ALPHABET clause"). LOCALE is not a lexer token, so the phrase arrives as one or two code-name-shaped entries
        // (kb/Work PB100 fixed the false "reserved word used as a user-defined word" it used to draw); it is a plain
        // word below 2002 (a code-name-1 there), so the phrase is 2002+ only. Since kb/Work PB101 the bare form is
        // IMPLEMENTED and the named form is refused by name until the LOCALE clause lands (design §12 T1).
        if (Edition.DialectLevel >= 2002 && IsAlphabetLocalePhrase(def))
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
        if (CodeSetNameOf(def) is { } wrongBranch)
        {
            Edition.Error("COBOLNET0898", $"ALPHABET {name} IS {wrongBranch}: the {wrongBranch} coded character "
                + "set may be referenced only in the FOR NATIONAL branch of the ALPHABET clause "
                + "(ISO §12.3.7.2 general format)");
            return;
        }
        if (def.NATIVE() is not null || def.STANDARD_1() is not null || def.STANDARD_2() is not null)
        {
            Alphabets.TryAdd(name, AlphabetDef.Native);
            return;
        }

        var pos = new ushort[256];
        Array.Fill(pos, ushort.MaxValue);                  // sentinel: not yet specified
        var specOrder = new List<char>();                  // every specified char in source order (tie rules)
        var repByPos = new List<ushort>();                 // per position: the FIRST char DEFINED there (§15.15.4 r2 / GR7 1.6 — PB59; the national builder's twin)
        ushort next = 0;
        void Assign(char c, bool advance)
        {
            int code = c & 0xFF;
            if (pos[code] != ushort.MaxValue) return;      // SR14a duplicate — first wins (diagnostic later)
            pos[code] = next;
            specOrder.Add((char)code);
            if (repByPos.Count == next) repByPos.Add((ushort)code);   // first occupant of the position wins (ALSO literal-1)
            if (advance) next++;
        }

        foreach (var entry in def.alphabetEntry())
        {
            var operands = AlphabetOperands(entry);
            if (operands.Count == 0) continue;
            if (entry.THRU() is not null || entry.THROUGH() is not null)
            {
                // k)5: the native run from operand-1 to operand-2, either direction, ascending positions.
                if (operands.Count >= 2 && operands[0].Length == 1 && operands[1].Length == 1)
                {
                    int a = operands[0][0] & 0xFF, b = operands[1][0] & 0xFF, step = a <= b ? 1 : -1;
                    for (int c = a; ; c += step) { Assign((char)c, advance: true); if (c == b) break; }
                }
                continue;
            }
            if (entry.ALSO().Length > 0)
            {
                // k)6: operand-1 and every ALSO operand share ONE ordinal position; operand-1 is the position's
                // first character (the CHAR() pick and the LOW-VALUE tie winner). ⛔ The advance is GUARDED the
                // way the national arm's is (PB59): an all-duplicate ALSO group must not advance past an
                // unoccupied position — GR7 1.3 admits no hole, and RepByPos would acquire one.
                int before = repByPos.Count;
                foreach (var op in operands)
                    if (op.Length == 1) Assign(op[0], advance: false);
                if (repByPos.Count > before) next++;
                continue;
            }
            // k)1.b: a (possibly multi-character) literal — each character, leftmost first, ascending positions.
            foreach (char c in operands[0]) Assign(c, advance: true);
        }

        // §12.3.7.4 GR7 1.3: unspecified characters follow, DISTINCT ascending positions in native relative order.
        for (int code = 0; code < 256; code++)
            if (pos[code] == ushort.MaxValue) { pos[code] = next; repByPos.Add((ushort)code); next++; }

        // GR8/GR9 extremes: highest/lowest POSITION; ties (an ALSO group) take the last/first char SPECIFIED.
        ushort maxPos = 0, minPos = ushort.MaxValue;
        for (int code = 0; code < 256; code++) { if (pos[code] > maxPos) maxPos = pos[code]; if (pos[code] < minPos) minPos = pos[code]; }
        char high = '\u00ff', low = '\u0000';
        for (int code = 255; code >= 0; code--) if (pos[code] == maxPos) { high = (char)code; break; }
        foreach (char c in specOrder) if (pos[c & 0xFF] == maxPos) high = c;                  // tie → LAST specified
        for (int code = 0; code < 256; code++) if (pos[code] == minPos) { low = (char)code; break; }
        foreach (char c in specOrder) if (pos[c & 0xFF] == minPos) { low = c; break; }        // tie → FIRST specified

        Alphabets.TryAdd(name, new AlphabetDef(new CollatingTable(pos, repByPos.ToArray(), next, high, low), null, "literal-phrase"));
    }

    /// <summary>The national coded-character-set name a definition consists of ("UCS-4" / "UTF-8" / "UTF-16"),
    /// or null. These are §8.9 CONTEXT-SENSITIVE words scoped to the ALPHABET clause — they arrive as a single
    /// plain <c>cobolWord</c> alphabet entry (never lexer keywords; they stay user-definable elsewhere), so the
    /// shape is: exactly one entry, no THROUGH/ALSO, one cobolWord, whose text is one of the three names.</summary>
    /// <summary>The ALPHABET clause's `IS LOCALE [locale-name-2]` phrase (ISO §12.3.7.2; kb/Work PB100): the first
    /// definition entry is the bare word LOCALE, optionally followed by one bare-word entry (locale-name-2) — LOCALE is
    /// not a lexer token, so the phrase arrives as one or two code-name-shaped entries.</summary>
    internal static bool IsAlphabetLocalePhrase(Core.AlphabetDefinitionContext def)
    {
        var entries = def.alphabetEntry();
        if (entries.Length is 0 or > 2) return false;
        foreach (var e in entries)
            if (e.THRU() is not null || e.THROUGH() is not null || e.ALSO().Length > 0 || e.ChildCount != 1 || e.GetChild(0) is not Core.CobolWordContext)
                return false;
        return string.Equals(entries[0].GetText(), "LOCALE", StringComparison.OrdinalIgnoreCase);
    }

    private static string? CodeSetNameOf(Core.AlphabetDefinitionContext def)
    {
        if (def.alphabetEntry() is not [{ } entry]) return null;
        if (entry.THRU() is not null || entry.THROUGH() is not null || entry.ALSO().Length > 0) return null;
        if (entry.ChildCount != 1 || entry.GetChild(0) is not Core.CobolWordContext w) return null;
        string t = w.GetText().ToUpperInvariant();
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
        if (CodeSetNameOf(def) is { } cs)
        {
            // UCS-4 references BOTH a coded set and a collating sequence; UTF-8/UTF-16 a coded set ONLY
            // (§12.3.7 GR7 f/g/h + Table 6). All three collapse to the identity on this substrate — the
            // difference is only WHERE the name may legally be referenced.
            NationalAlphabets.TryAdd(name, new NationalAlphabetDef(null, HasCollatingSequence: cs == "UCS-4", cs));
            return;
        }
        // A single non-figurative bare word that is not a coded-set name would be code-name-2 (§12.3.7.3 SR15
        // — implementor-defined; none are supported). Figurative words are literal-phrase operands (GR10).
        if (def.alphabetEntry() is [{ ChildCount: 1 } only] && only.GetChild(0) is Core.CobolWordContext cw
            && !IsNationalFigurativeWord(cw.GetText()))
        {
            Edition.Error("COBOLNET0898", $"ALPHABET {name} FOR NATIONAL IS {cw.GetText()}: not a supported "
                + "code-name — this implementation defines no code-name-2 names (ISO §12.3.7.3 SR15; the "
                + "national coded character sets are NATIVE, UCS-4, UTF-8, and UTF-16)");
            return;
        }
        AlphabetBindNationalLiteralPhrase(name, def);
    }

    private static bool IsNationalFigurativeWord(string word) => word.ToUpperInvariant()
        is "HIGH-VALUE" or "HIGH-VALUES" or "LOW-VALUE" or "LOW-VALUES"
        or "SPACE" or "SPACES" or "QUOTE" or "QUOTES" or "ZERO" or "ZEROS" or "ZEROES";

    /// <summary>Build a NATIONAL literal-phrase alphabet (ISO §12.3.7 GR7 k over the NATIVE NATIONAL character
    /// set — the 65,536 UTF-16 code units, D-N1): a numeric literal is the 1-based NATIONAL ordinal (k1a, SR14c1
    /// — 1..65536 ⇒ code unit ordinal−1); a noninteger literal shall be a NATIONAL literal (SR14c2), each
    /// character taking successive ascending positions leftmost-first (k1b); THROUGH expands the native run in
    /// EITHER direction (k5); ALSO members share ONE position (k6); every unspecified code unit takes a DISTINCT
    /// ascending position above the highest specified, in native relative order (k3 — realized SPARSELY, the
    /// runtime computes it arithmetically). Figurative words inside SPECIAL-NAMES are the NATIVE NATIONAL
    /// extremes/values (GR10 — HIGH-VALUE = U+FFFF, LOW-VALUE = U+0000 in the native national sequence).</summary>
    private void AlphabetBindNationalLiteralPhrase(string name, Core.AlphabetDefinitionContext def)
    {
        var pos = new Dictionary<char, ushort>();
        var specOrder = new List<char>();          // every specified char in source order (GR8/GR9 tie rules)
        var repByPos = new List<char>();           // the FIRST char defined per position (§15.16.4 r2)
        ushort next = 0;
        void Assign(char c, bool advance)
        {
            if (pos.ContainsKey(c)) return;        // SR14a duplicate — first wins (the alphanumeric builder's posture)
            pos[c] = next;
            specOrder.Add(c);
            if (repByPos.Count == next) repByPos.Add(c);
            if (advance) next++;
        }

        foreach (var entry in def.alphabetEntry())
        {
            var operands = AlphabetOperandsNational(name, entry);
            if (operands.Count == 0) continue;
            if (entry.THRU() is not null || entry.THROUGH() is not null)
            {
                // k)5: the native national run from operand-1 to operand-2, either direction (SR14c3 — one char each).
                if (operands.Count >= 2 && operands[0].Length == 1 && operands[1].Length == 1)
                {
                    int a = operands[0][0], b = operands[1][0], step = a <= b ? 1 : -1;
                    for (int c = a; ; c += step) { Assign((char)c, advance: true); if (c == b) break; }
                }
                else
                    Edition.Error("COBOLNET0898", $"ALPHABET {name} FOR NATIONAL: each THROUGH operand shall "
                        + "be one character in length (ISO §12.3.7.3 SR14c3)");
                continue;
            }
            if (entry.ALSO().Length > 0)
            {
                // k)6: operand-1 and every ALSO operand share ONE position; operand-1 is that position's first
                // character. Advance only when the group assigned something (a duplicate-only group is inert).
                int before = repByPos.Count;
                foreach (var op in operands)
                {
                    if (op.Length == 1) Assign(op[0], advance: false);
                    else Edition.Error("COBOLNET0898", $"ALPHABET {name} FOR NATIONAL: each ALSO operand shall "
                        + "be one character in length (ISO §12.3.7.3 SR14c3)");
                }
                if (repByPos.Count > before) next++;
                continue;
            }
            // k)1.b: a (possibly multi-character) national literal — each character, leftmost first.
            foreach (char c in operands[0]) Assign(c, advance: true);
        }
        if (specOrder.Count == 0) return;           // every operand failed its SR — errors already reported

        // Sparse arrays sorted by code (the runtime's binary-search key).
        var codes = pos.Keys.Order().ToArray();
        var positions = new ushort[codes.Length];
        for (int i = 0; i < codes.Length; i++) positions[i] = pos[codes[i]];

        // GR8/GR9 extremes over the FULL national sequence: position 0 belongs to the first character specified
        // (a position-0 ALSO tie also resolves to it — GR9 takes the FIRST specified); the HIGHEST position
        // belongs to the largest UNSPECIFIED code unit (unspecified characters sit above all specified ones,
        // §12.3.7.4 GR7 1.3) — U+FFFF unless specified, else the next free code downward; if every code unit is specified
        // (unreachable from real source), GR8's tie rule over the top specified position applies.
        char low = specOrder[0];
        char high = '\uffff';
        while (pos.ContainsKey(high) && high > '\u0000') high--;
        if (pos.ContainsKey(high))                   // all 65,536 specified — GR8 over the specified block
        {
            ushort maxPos = positions.Max();
            foreach (char c in specOrder) if (pos[c] == maxPos) high = c;   // tie → LAST specified (GR8)
        }

        NationalAlphabets.TryAdd(name, new NationalAlphabetDef(
            new NationalCollatingTable(codes.Select(c => (ushort)c).ToArray(), positions,
                repByPos.Select(c => (ushort)c).ToArray(), next, high, low),
            HasCollatingSequence: true, "literal-phrase"));
    }

    /// <summary>A NATIONAL alphabet entry's operand texts in source order (ISO §12.3.7.3 SR14c): a quoted
    /// literal shall be a NATIONAL literal (SR14c2 — decoded to its characters); an unsigned integer literal is
    /// the character at that 1-based ordinal of the NATIVE NATIONAL character set (SR14c1 — 1..65536); the
    /// figurative words written inside SPECIAL-NAMES are the NATIVE NATIONAL extremes/values (GR10 —
    /// HIGH-VALUE = U+FFFF, LOW-VALUE = U+0000, SPACE, QUOTE, ZERO).</summary>
    private List<string> AlphabetOperandsNational(string name, Core.AlphabetEntryContext entry)
    {
        var result = new List<string>();
        for (int i = 0; i < entry.ChildCount; i++)
        {
            switch (entry.GetChild(i))
            {
                case Core.LiteralContext lit:
                    string text = lit.GetText();
                    if (int.TryParse(text, out int ordinal))
                    {
                        if (ordinal is >= 1 and <= 0x10000) result.Add(((char)(ordinal - 1)).ToString());
                        else Edition.Error("COBOLNET0898", $"ALPHABET {name} FOR NATIONAL: ordinal {ordinal} is "
                            + "outside the native national character set (1..65536, ISO §12.3.7.3 SR14c1)");
                    }
                    else if (text.Length >= 1 && text[0] is 'N' or 'n')
                        result.Add(CobolLiteral.Decode(text));
                    else
                    {
                        Edition.Error("COBOLNET0898", $"ALPHABET {name} FOR NATIONAL: {text} — each noninteger "
                            + "literal shall be a NATIONAL literal (N\"…\"; ISO §12.3.7.3 SR14c2)");
                        if (CobolLiteral.IsStringLiteral(text)) result.Add(CobolLiteral.Decode(text));
                    }
                    break;
                case Core.CobolWordContext w:
                    string t = w.GetText().ToUpperInvariant();
                    result.Add(t switch
                    {
                        // GR10: the native NATIONAL extremes — the highest/lowest of the 65,536-code-unit set.
                        "HIGH-VALUE" or "HIGH-VALUES" => "\uffff",
                        "LOW-VALUE" or "LOW-VALUES" => "\u0000",
                        "SPACE" or "SPACES" => " ",
                        "QUOTE" or "QUOTES" => "\"",
                        "ZERO" or "ZEROS" or "ZEROES" => "0",
                        _ => t,
                    });
                    break;
            }
        }
        return result;
    }

    /// <summary>An alphabet entry's operand texts in source order: quoted literals decoded, an unsigned integer
    /// literal as the character at that 1-based NATIVE ordinal (GR7 k1a), and the figurative words written inside
    /// SPECIAL-NAMES as the NATIVE extremes/values (GR10 — HIGH-VALUE=U+00FF, LOW-VALUE=U+0000, SPACE, QUOTE,
    /// ZERO).</summary>
    private List<string> AlphabetOperands(Core.AlphabetEntryContext entry)
    {
        var result = new List<string>();
        for (int i = 0; i < entry.ChildCount; i++)
        {
            switch (entry.GetChild(i))
            {
                case Core.LiteralContext lit:
                    result.Add(LiteralChars(lit));
                    break;
                case Core.CobolWordContext w:
                    string t = w.GetText().ToUpperInvariant();
                    result.Add(t switch
                    {
                        "HIGH-VALUE" or "HIGH-VALUES" => "\u00ff",
                        "LOW-VALUE" or "LOW-VALUES" => "\u0000",
                        "SPACE" or "SPACES" => " ",
                        "QUOTE" or "QUOTES" => "\"",
                        "ZERO" or "ZEROS" or "ZEROES" => "0",
                        _ => t,
                    });
                    break;
            }
        }
        return result;
    }

    /// <summary>One <c>CLASS class-name IS {literal [THRU literal]}…</c> clause (ISO §12.3.7): expand each value
    /// item to its member characters — a multi-character literal lists each character; a THRU pair contributes the
    /// contiguous native-collating range between the two single-character ordinals, ASCENDING OR DESCENDING (the
    /// clause's GR allows either order — NC174A's <c>"D" THROUGH "A"</c> equals <c>"A" THRU "D"</c>).</summary>
    private void SwitchBindClass(Core.ClassDefinitionClauseContext cd)
    {
        // CLASS … FOR ALPHANUMERIC/NATIONAL — the FOR phrase edition gate is now VersionConformancePass
        // ParseArm.VisitClassDefinitionClause (14g.4, recognition).
        string name = cd.cobolWord(0).GetText();
        var members = new System.Text.StringBuilder();
        foreach (var item in cd.classValueSet().classValueItem())
        {
            var lits = item.literal();
            string lo = LiteralChars(lits[0]);
            if (lits.Length >= 2)
            {
                string hi = LiteralChars(lits[1]);
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

    /// <summary>The character content of a class-definition literal: a quoted literal's characters, or — for an
    /// unsigned integer literal — the character at that ORDINAL position of the native collating sequence
    /// (1-based, ISO §12.3.7; ordinal n ⇒ char code n−1 over the 8-bit native sequence).</summary>
    private string LiteralChars(Core.LiteralContext lit)
    {
        // §8.8.3.3 GR3: a concatenation expression stands anywhere a literal of its class may — fold an
        // ALPHABET/CLASS operand concat to its character value before decoding (GetText would glue the
        // operand tokens and mis-decode). No PCS applies here — these clauses are DEFINING the sequences.
        if (lit.nonNumericLiteral()?.concatenationExpression() is { } ce)
            return ConcatFolder.Fold(ce, Edition, collate: null).Value;
        string text = lit.GetText();
        if (CobolLiteral.IsStringLiteral(text))   // both ISO §8.3.1.2 delimiters (an apostrophe CLASS literal was miscompiled)
            return CobolLiteral.Decode(text);
        // §8.3.3.2 hexadecimal-format alphanumeric literal (X"hh…"): each hex-digit pair is one character. Without
        // this, X"FF" fell through to raw text, so its length != 1 skipped the THRU/ALSO range and the alphabet was
        // silently left native (e.g. ALPHABET … X"FF" THRU X"00" never reversed — §12.3.7.4 GR5).
        if (text.Length >= 3 && text[0] is 'X' or 'x' && text[1] is '"' or '\'')
            return CobolLiteral.DecodeHex(text);
        return int.TryParse(text, out int ordinal) && ordinal >= 1 && ordinal <= 256
            ? ((char)(ordinal - 1)).ToString()
            : text;
    }
}
