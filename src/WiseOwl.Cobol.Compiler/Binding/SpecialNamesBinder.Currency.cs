// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Common;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend.Generated;

using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding;

using Core = CobolParserCore;
using WiseOwl.Cobol.Runtime;

/// <summary>The CURRENCY SIGN clause and the unit's currency-sign set (ISO §12.3.7.2; §12.3.7.3 SR18-SR28; §12.3.7.4 GR13).
/// A partial of <see cref="SpecialNamesBinder"/>.</summary>
public sealed partial class SpecialNamesBinder
{
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
    /// container's set whole (<see cref="DataBinder.InheritConfiguration"/>, §12.3.4 GR1).
    /// ⛔ THIS REPLACED A SCALAR PAIR (kb/Work PB60 / AR-15.68.3-3): <c>CurrencyPicSymbol</c>/<c>CurrencyString</c>
    /// held ONE symbol and ONE string per unit — a second clause silently overwrote the first, the implied '$'
    /// died the moment any clause bound another symbol (a legal <c>PIC $$,$$9.99</c> under <c>CURRENCY SIGN "#"</c>
    /// was COBOL0808), and a multi-character string was refused outright (COBOL0896, "not yet supported" —
    /// deferral debt). Every PICTURE consumer now asks this set which symbol a mask uses (<c>PictureAnalyzer</c>
    /// records it on <c>PicInfo.CurrencyString</c> after canonicalizing the mask's symbol to '$'), and NUMVAL-C's
    /// §15.68.3 r3 default is <see cref="SoleCurrencyString"/>.
    /// </summary>
    public IReadOnlyDictionary<char, CurrencyDefinition> CurrencySigns => _currencySigns;

    private readonly Dictionary<char, CurrencyDefinition> _currencySigns = new();

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
    /// then violated and the reference is diagnosed at bind (COBOL1644).</summary>
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
        if (!_dollarSpecified) _currencySigns.TryAdd('$', CurrencyDefinition.ImpliedDollar);
    }

    /// <summary>⛔ THE SYNTAX RULES A CURRENCY SIGN LITERAL IS HELD TO, as data — one value per operand, the way
    /// <see cref="LiteralPhraseRules"/> does it for the ALPHABET / CLASS clauses (kb/Work PB791; it used to be a
    /// <c>rule</c> number glued onto one message, which could not say that the two operands carry DIFFERENT
    /// restrictions). §12.3.7.3 SR18 — "<i>Literal-7 shall be an alphanumeric or national literal that is not a
    /// figurative constant</i>" — and SR26 — "<i>Literal-8 shall be an alphanumeric or national literal consisting of
    /// a single character from the computer's compile-time coded character set. It shall be neither a figurative
    /// constant nor a hexadecimal literal</i>" (the one-character half is a LENGTH rule, asked of the decoded
    /// characters by <see cref="SwitchBindCurrency"/>; everything here is about what the operand IS as written).</summary>
    /// <param name="Name">The standard's name for the operand.</param>
    /// <param name="Rule">The syntax rule, "SR18" / "SR26".</param>
    /// <param name="Requirement">The rule's own words, for the diagnostic.</param>
    /// <param name="RefusesHexadecimal">True when a literal in hexadecimal format is refused outright (SR26). A
    /// hexadecimal literal-7 is the opposite: SR19 REQUIRES the PICTURE SYMBOL phrase for it and NOTE 1 exempts its
    /// content from the compile-time currency-string limitations.</param>
    private sealed record CurrencyOperand(string Name, string Rule, string Requirement, bool RefusesHexadecimal)
    {
        public static CurrencyOperand Literal7 { get; } = new("literal-7", "SR18",
            "an alphanumeric or national literal that is not a figurative constant", RefusesHexadecimal: false);

        public static CurrencyOperand Literal8 { get; } = new("literal-8", "SR26",
            "an alphanumeric or national literal that is neither a figurative constant nor a hexadecimal literal",
            RefusesHexadecimal: true);
    }

    /// <summary>One CURRENCY SIGN clause operand as the rules read it: its characters, whether it is WRITTEN in
    /// hexadecimal format (§12.3.7.3 SR19 / SR26 and NOTE 1 ask the form, never the decoded characters) and its class
    /// (SR28's input — alphanumeric or national; every other class was refused).</summary>
    private readonly record struct CurrencyLiteral(string Text, bool Hexadecimal, LiteralClass Class);

    /// <summary>A CURRENCY SIGN clause operand, or null when it is not a literal the operand's rule admits
    /// (<see cref="CurrencyOperand"/>): a NUMERIC literal (⛔ silently ACCEPTED until kb/Work PB770 — the clause called
    /// the CLASS clause's ordinal helper, so <c>CURRENCY SIGN IS 65</c> became the currency symbol 'A'), a BOOLEAN one, a
    /// FIGURATIVE constant (SR18 for literal-7 and SR26 for literal-8 — ⛔ accepted until kb/Work PB791 because the
    /// screen was class-only and <c>LiteralCharsOf</c> decoded a figurative constant as its own SPELLING:
    /// <c>CURRENCY SIGN IS SPACE PICTURE SYMBOL "#"</c> bound the currency string "SPACE"), the predefined NULL (an
    /// identifier, §8.4.3.10.3 SR1 — never a literal), and, for literal-8, a hexadecimal literal (SR26, whatever it
    /// decodes to). The decision is made from the operand as WRITTEN, never from the decoded characters — for a
    /// constant-name (kb/Work PB1942), from literal-1 of its entry, which §13.10.4 GR1 substitutes "as if literal-1 …
    /// were written where constant-name-1 is written" (<see cref="DataBinder.SpecialNamesConstant"/>).</summary>
    private CurrencyLiteral? CurrencyOperandOf(Core.SpecialNamesLiteralContext operand, CurrencyOperand op)
    {
        string? what;
        string spelling;
        CurrencyLiteral? value = null;
        if (operand.literal() is { } lit)
        {
            var nn = lit.nonNumericLiteral();
            if (nn?.predefinedNull() is not null)
            {
                PredefinedNullRule.Report(Edition, $"the {op.Name} of a CURRENCY SIGN clause — {op.Requirement} "
                    + $"(ISO §12.3.7.3 {op.Rule})");
                return null;
            }
            // The literal's CLASS is the grammar's answer (§8.3.3.3.1: "Numeric literals are of the class and category
            // numeric" — the lexer's numeric-literal rule decides it), never a C# parse of its text (kb/Work PB1579):
            // `int.TryParse || decimal.TryParse` answered "not numeric" for a 30- or 31-digit numeric literal (beyond
            // decimal's range), which then fell into the alphanumeric path and was refused under the wrong rule. A
            // §8.8.3 concatenation expression is "equivalent to a literal" (§8.8.3.3 GR3), never one in hexadecimal format.
            bool hex = nn?.concatenationExpression() is null && CobolLiteral.IsHexadecimalFormat(lit.GetText());
            var cls = OperandLiteralClass(lit);
            what = nn?.figurativeConstant() is not null ? "a figurative constant"
                : lit.numericLiteral() is not null ? "a numeric literal"
                : op.RefusesHexadecimal && hex ? "a hexadecimal literal"
                : cls is LiteralClass.Boolean ? "a boolean literal"
                : null;
            spelling = ConcatFolder.Spelling(lit);
            // CURRENCY: no NATIONAL phrase; SR11 names no literal-7/-8.
            if (what is null)
                value = new(LiteralCharsOf(lit, sr11: false), hex,
                    cls is LiteralClass.National ? LiteralClass.National : LiteralClass.Alphanumeric);
        }
        else
        {
            var word = operand.cobolWord();
            if (_host.SpecialNamesConstant(word, Report, $"CURRENCY SIGN {word.GetText()}", op.Name) is not { } k)
                return null;
            // A constant's literal-1 is never a figurative constant (§13.10.3 SR6), so its class and its form remain;
            // the form is literal-1 as written (ConstantDef.WrittenLiteral: through a constant-name written AS another,
            // or a FROM compilation variable, §13.10.4 GR1), never the entry's specification, which for AS KX is
            // the word KX (kb/Work PB780 review R1-1).
            bool hex = k.WrittenLiteral is { } written && CobolLiteral.IsHexadecimalFormat(written);
            what = k.Category switch
            {
                PicCategory.Numeric => "a numeric literal",
                PicCategory.Boolean => "a boolean literal",
                _ when op.RefusesHexadecimal && hex => "a hexadecimal literal",
                _ => null,
            };
            spelling = $"{word.GetText()} (the constant-name for {k.StandsFor})";
            if (what is null)
                value = new(k.Text, hex, k.Category is PicCategory.National ? LiteralClass.National : LiteralClass.Alphanumeric);
        }
        if (value is not null) return value;
        Report($"CURRENCY SIGN {spelling}: {op.Name} shall be {op.Requirement} — this is {what} (ISO §12.3.7.3 {op.Rule})");
        return null;

        // The clause's one report: the pre-catalog COBOL0892 band of CURRENCY SIGN operand rules.
        void Report(string message) => Edition.Error("COBOL0892", message);
    }

    /// <summary>Bind <c>CURRENCY [SIGN] [IS] literal-7 [WITH PICTURE SYMBOL literal-8]</c> (ISO §12.3.7.3
    /// r18–r27) into the currency SET. Bare form: literal-7 is both currency string and symbol — a single
    /// character outside the r22 forbidden set. PICTURE SYMBOL form (2002+ — the 85 standard had only the
    /// single-character form): literal-7 is the currency string ("may have any length", r23 — at least one
    /// non-space character, none of <c>0-9 + - , . *</c>), literal-8 the one-character symbol (r26/r27; a
    /// hexadecimal literal-7 requires this form, r19). r21: two clauses may not give equivalent symbols
    /// different strings. r25's implied '$' is added by <see cref="FinalizeCurrencySigns"/>.
    /// <para>⛔ A CLAUSE REFUSED FOR ITS CURRENCY STRING STILL NAMES ITS PICTURE SYMBOL (kb/Work PB1920). In the PICTURE
    /// SYMBOL form the symbol is literal-8 and the string literal-7, two operands with rules of their own: each is
    /// judged, and a valid literal-8 is registered even when literal-7 broke SR18, SR23 a) or SR23 b)
    /// (<see cref="CurrencyDefinition.OfRefusedClause"/>), so a later <c>PIC #9.99</c> is not a cascade of COBOL0808 /
    /// COBOL0819 behind the clause's one diagnostic. In the bare form literal-7 IS the symbol, so a refused literal-7
    /// names none.</para></summary>
    private void SwitchBindCurrency(Core.CurrencySignClauseContext cur)
    {
        var operands = cur.specialNamesLiteral();
        if (operands.Length == 0) return;
        var operand7 = CurrencyOperandOf(operands[0], CurrencyOperand.Literal7);
        if (cur.PIC() is not null)
        {
            // currency-picture-symbol-2002 (§12.3.7): the pass owns the edition gate (Exec Step E).
            // PICMODE exploit: the word between PICTURE and literal-8 arrives as a PIC_STRING token — it must be
            // the keyword SYMBOL (the grammar cannot distinguish; semantic check per the grammar's own note).
            if (cur.PIC_STRING()?.GetText() is { } sym && !CobolNames.Same(sym, "SYMBOL"))
                Edition.Error("COBOL0892", $"CURRENCY SIGN: expected 'WITH PICTURE SYMBOL', found 'PICTURE {sym}' (ISO §12.3.7)");
            // The currency string and its class (SR28's input), or null when literal-7 was refused.
            CurrencyDefinition? stringDefinition = operand7 is { } literal7 && CurrencyStringAdmitted(literal7)
                ? new CurrencyDefinition(literal7.Text, literal7.Class) : null;
            string? literal8 = operands.Length > 1 ? CurrencyOperandOf(operands[1], CurrencyOperand.Literal8)?.Text : "";
            if (literal8 is null) return;
            if (literal8.Length != 1)
            {
                Edition.Error("COBOL0892", "CURRENCY SIGN: the PICTURE SYMBOL literal shall be a single "
                    + "character (ISO §12.3.7.3 r26)");
                return;
            }
            ValidateCurrencyChar(literal8[0], "PICTURE SYMBOL literal");   // r27 — same forbidden set as r22
            RegisterCurrency(literal8[0], stringDefinition);
            return;
        }
        // Bare form (the COBOL-85 surface): r22 — one character, both string and symbol.
        if (operand7 is not { } bare) return;
        // SR19: a hexadecimal literal-7 (X"…" / NX"…") requires the PICTURE SYMBOL phrase. Detected on the operand's
        // WRITTEN form (the decoded characters cannot tell the forms apart), by CurrencyOperandOf.
        if (bare.Hexadecimal)
            Edition.Error("COBOL0890", "CURRENCY SIGN: a hexadecimal currency literal requires the PICTURE "
                + "SYMBOL phrase (ISO §12.3.7.3 r19)");
        if (bare.Text.Length != 1)
        {
            Edition.Error("COBOL0890", $"CURRENCY SIGN \"{bare.Text}\": without PICTURE SYMBOL the literal "
                + "shall consist of a single character (ISO §12.3.7.3 r22)");
            return;
        }
        ValidateCurrencyChar(bare.Text[0], "CURRENCY SIGN literal");
        RegisterCurrency(bare.Text[0], new CurrencyDefinition(bare.Text, bare.Class));
    }

    /// <summary>§12.3.7.3 SR23 a)/b) — the content of a PICTURE SYMBOL form's currency STRING (its length is free);
    /// reported here, true when it is admitted.</summary>
    private bool CurrencyStringAdmitted(CurrencyLiteral literal7)
    {
        if (literal7.Text.Trim().Length == 0)
        {
            Edition.Error("COBOL0890", "CURRENCY SIGN: the currency string shall contain at least one "
                + "non-space character (ISO §12.3.7.3 r23a)");
            return false;
        }
        // SR23 b is a compile-time limitation on the characters of a currency string, which SR19's NOTE 1 says
        // does not reach "the alphanumeric or national characters represented by hexadecimal literals used as
        // currency strings" — their content is the runtime coded character set's, used exactly as written.
        if (!literal7.Hexadecimal && literal7.Text.Any(c => char.IsAsciiDigit(c) || c is '+' or '-' or ',' or '.' or '*'))
        {
            Edition.Error("COBOL0890", $"CURRENCY SIGN \"{literal7.Text}\": a currency string may not contain the "
                + "digits 0 through 9 or the characters '+' '-' ',' '.' '*' (ISO §12.3.7.3 r23b)");
            return false;
        }
        return true;
    }

    /// <summary>Bind <paramref name="symbol"/> into the currency set: to <paramref name="definition"/>, or — null — as
    /// the symbol of a clause refused for its currency string (<see cref="CurrencyDefinition.OfRefusedClause"/>, kb/Work
    /// PB1920), which neither meets nor makes a §12.3.7.3 SR21 conflict and adds no string to §15.68.3 r3's count.</summary>
    private void RegisterCurrency(char symbol, CurrencyDefinition? definition)
    {
        // THE KEY IS THE IMPLEMENTOR'S DETERMINATION: §12.3.7.3 SR20 leaves "any equivalence between that character and any
        // other character in the computer's compile-time coded character set" of a symbol that is not a COBOL character to
        // the implementor, and Annex A.1 item 43 requires it documented — docs/CONFORMANCE.md DOC-A.1-43 states it: two
        // characters are equivalent exactly when they have the same invariant-culture upper-case mapping. A COBOL character
        // folds by §8.1.3.2 GR3 b), which the same mapping gives (kb/Work PB1092). Change the key and that row together.
        char key = char.ToUpperInvariant(symbol);
        if (key == '$') _dollarSpecified = true;   // r25 — a clause named '$' as literal-8: no implied clause
        bool isBound = _currencySigns.TryGetValue(key, out var bound);
        if (definition is not { } defined)
        {
            if (!isBound) _currencySigns[key] = CurrencyDefinition.OfRefusedClause(symbol);
            return;
        }
        // §12.3.7.3 SR28's input: the CLASS of literal-7 — alphanumeric (a plain or X"…" literal) or national (N"…" /
        // NX"…", a §8.8.3.3 concatenation folding to national, or a constant-name of either). Boolean and every other
        // shape were refused by CurrencyOperandOf.
        // r21: no two clauses may bind equivalent symbols to different strings — and a string of the other CLASS is
        // not the identical string (SR28 would then confine the one symbol to display AND to national items). A refused
        // clause's placeholder states no string, so it neither conflicts nor survives a clause that does.
        if (isBound && !bound.ClauseRefused && bound != defined)
        {
            Edition.Error("COBOL0891", $"CURRENCY SIGN: the symbol '{symbol}' is already bound to "
                + $"{(bound.LiteralClass is LiteralClass.National ? "national " : "")}\"{bound.Text}\" "
                + $"(ISO §12.3.7.3 r21; SR28)");
            return;
        }
        _currencySigns[key] = defined;
        _explicitCurrencyStrings.Add(defined.Text);
        if (defined.Text == "$") _dollarSpecified = true;   // r25 — a clause named '$' as literal-7
    }

    /// <summary>The ISO §12.3.7.3 SR22/SR27 forbidden currency-symbol set: digits; A B C D E N P R S V X Z (either
    /// case) or space; and <c>+ - , . * / ; ( ) " =</c>. The letter test goes through the item-43 equivalence
    /// (<c>char.ToUpperInvariant</c>), so a non-COBOL character whose invariant upper case is one of those letters is
    /// excluded with it — the "additional characters prohibited" of §8.1.2 that Annex A.1 item 44 requires documented
    /// (docs/CONFORMANCE.md DOC-A.1-44; kb/Work PB1397).</summary>
    private void ValidateCurrencyChar(char c, string what)
    {
        bool forbidden = char.IsAsciiDigit(c)
            || char.ToUpperInvariant(c) is 'A' or 'B' or 'C' or 'D' or 'E' or 'N' or 'P' or 'R' or 'S' or 'V' or 'X' or 'Z'
            || c is ' ' or '+' or '-' or ',' or '.' or '*' or '/' or ';' or '(' or ')' or '"' or '=';
        if (forbidden)
            Edition.Error("COBOL0891", $"{what} '{c}': not a valid currency symbol — digits, the picture "
                + "letters A B C D E N P R S V X Z, space, and + - , . * / ; ( ) \" = are excluded (ISO §12.3.7.3 SR22/SR27)");
    }
}
