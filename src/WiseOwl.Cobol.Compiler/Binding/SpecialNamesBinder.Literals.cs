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

/// <summary>The literal-operand screens every SPECIAL-NAMES clause shares (ISO §12.3.7.3 SR11, SR14; §12.3.7.4 GR10): the one decoder of an alphabet or class operand, the zero-length test and the figurative-constant mapping.
/// A partial of <see cref="SpecialNamesBinder"/>.</summary>
public sealed partial class SpecialNamesBinder
{
    /// <summary>The ONE rule for a SPECIAL-NAMES clause's TEXT literal (ISO §12.3.7.3 SR10 — "shall be alphanumeric or
    /// national"; SR11 — "shall specify neither a symbolic-character figurative constant nor a zero-length literal"),
    /// shared by the ORDER TABLE clause's literal-9 and the LOCALE clause's literal-4: decodes the literal (a
    /// concatenation expression folds first — §8.8.3.3 GR3 makes it usable anywhere a literal of its class is),
    /// reporting COBOL0898 and returning false on a violation. <paramref name="what"/> names the clause for the
    /// message; <paramref name="operand"/> the literal's role ("literal-9").
    /// <para>⛔ SR11 bars ONE kind of figurative constant — the symbolic-character one — and §8.3.3.6.3 SR1 admits every
    /// other as a literal of its class, so <c>ORDER TABLE T1 IS SPACE</c> and <c>LOCALE LA IS SPACE</c> are legal:
    /// SPACE then names an ordering table / a locale this implementation does not provide, which is the RUN-TIME
    /// answer (the COBOL1662 advice and EC-ORDER-NOT-SUPPORTED / EC-LOCALE-MISSING), not a syntax error. This screen
    /// used to refuse every figurative constant under a rule that quotes only the symbolic-character one (kb/Work
    /// PB1537), and the NULL it also refused was not a figurative constant at all (§8.4.3.10.3 SR1).</para>
    /// <para>A WORD at the position is a constant-name (§13.10.3 SR2, kb/Work PB1942 — <see cref="DataBinder.SpecialNamesConstant"/>):
    /// its literal-1 meets the same two rules.</para></summary>
    private bool TryClauseTextLiteral(Antlr4.Runtime.Tree.IParseTree written, string what, string operand, out string text)
    {
        text = "";
        if (written is Core.CobolWordContext word)
        {
            if (_host.SpecialNamesConstant(word, Report, what, operand) is not { } constant) return false;
            if (constant.Category is not (PicCategory.Alphanumeric or PicCategory.National))
            {
                Report($"{what}: {operand} shall be an alphanumeric or national literal (ISO "
                    + $"§12.3.7.3 SR10); the constant-name '{word.GetText()}' stands for {constant.StandsFor}, of "
                    + $"class {constant.Category} (§13.10.4 GR1/GR2)");
                return false;
            }
            text = constant.Text;
            return Sr11ZeroLength(IsZeroLengthConstant(constant));
        }
        var lit = (Core.LiteralContext)written;
        var nn = lit.nonNumericLiteral();
        if (nn?.predefinedNull() is not null)
        {
            PredefinedNullRule.Report(Edition, $"{what}: {operand} — an alphanumeric or national literal "
                + "(ISO §12.3.7.3 SR10)");
            return false;
        }
        if (nn?.figurativeConstant() is { } fig)
        {
            var slot = new FigurativeSlot(what, Report,
                $"{operand} shall be an alphanumeric or national literal", "SR10",
                category => category is PicCategory.Alphanumeric or PicCategory.National,
                $"{operand} shall specify neither a symbolic-character figurative constant nor a zero-length literal");
            if (SpecialNamesFigurative(fig, slot) is not { } figurative) return false;
            text = figurative;
            return Sr11ZeroLength(text.Length == 0);
        }
        // §8.8.3.3 GR3 makes a concatenation expression "equivalent to a literal of the same class and value,
        // [usable] anywhere a literal of that class may be used", so `IS "ISO " & "14651_2020_TABLE1"` is legal
        // here and folds to the one literal SR10 then classes. Refusing it would reject conforming source.
        // ⛔ THE ONE DECODE (kb/Work PB3218): the literal's class is OperandLiteralClass's (a concatenation's by the
        // §8.8.3.3 GR1 fold, a plain literal's by its prefix) and its characters LiteralCharsOf's — the pair the
        // ALPHABET / CLASS operands decode through — where this screen kept an inline copy of both. A figurative
        // HIGH-/LOW-VALUE written INSIDE SPECIAL-NAMES takes the NATIVE extremes (§12.3.7.4 GR10 — one pair for both
        // classes, owner R52) and SR11 forbids a symbolic-character operand in literal-9 (the posture this screen
        // gives literal-4 as well). SR10: CobolLiteral.ClassOf answers from the PREFIX, so a plain literal admits both
        // quoting forms and both hexadecimal spellings (X"…" is one FORM of an alphanumeric literal, §8.3.3.2) and
        // refuses a boolean one; a NUMERIC literal is not a quoted literal and lands here too.
        var literalClass = OperandLiteralClass(lit);
        if (literalClass is not (LiteralClass.Alphanumeric or LiteralClass.National))
        {
            Report($"{what}: {operand} shall be an alphanumeric or national literal (ISO §12.3.7.3 SR10)"
                + (nn?.concatenationExpression() is not null
                    ? $"; this concatenation expression is class {literalClass} (§8.8.3.3 GR1/GR3)" : ""));
            return false;
        }
        text = LiteralCharsOf(lit, sr11: true);
        return Sr11ZeroLength(IsZeroLengthOperand(lit));

        // SR11's SECOND half, asked of the answer the ONE predicate gives (IsZeroLengthOperand / IsZeroLengthConstant).
        bool Sr11ZeroLength(bool zeroLength)
        {
            if (!zeroLength) return true;
            Report($"{what}: {operand} shall specify neither a "
                + "symbolic-character figurative constant nor a zero-length literal (ISO §12.3.7.3 SR11)");
            return false;
        }

        // The clause's one report: the pre-catalog COBOL0898 band of SPECIAL-NAMES literal rules.
        void Report(string message) => Edition.Error("COBOL0898", message);
    }

    /// <summary>One decoded operand of a literal phrase: the characters it stands for AND the parse node it was
    /// WRITTEN as. The SYNTAX RULES that speak of "each alphanumeric / national LITERAL" (§12.3.7.3 SR14 b3/c3,
    /// SR17 b4/c4 — one character under THROUGH) are asked of the operand as written, never of the decoded characters:
    /// a NUMERIC literal is an ordinal (SR14 b1/c1, SR17 b2/c2), names one character of its set by construction, and
    /// is no national literal (kb/Work PB1091's L7.4 measurement — a numeric ordinal naming a supplementary
    /// character decodes to a two-code-unit surrogate pair and was refused under c4).</summary>
    /// <param name="Members">How many CHARACTERS of a coded set the operand names — more than one only for a numeric
    /// ordinal under a CLASS clause's IN phrase that lands on an ALSO group (§12.3.7.4 GR7 k6; kb/Work PB1095). It is a
    /// count of characters, never of code units, so a supplementary character (two code units) is still one member.</param>
    /// <param name="IsOrdinal">True when the operand is an ORDINAL: written as a numeric literal that is an integer, or a
    /// constant-name standing for an integer (§13.10.3 SR2; kb/Work PB1942) — decided where the operand is decoded.</param>
    private readonly record struct PhraseOperand(
        Antlr4.Runtime.Tree.IParseTree Node, string Characters, int Members = 1, bool IsOrdinal = false)
    {
        /// <summary>The operand as the source spelled it, for a diagnostic.</summary>
        public string Spelling => Node.GetText();
    }

    /// <summary>⛔ THE SYNTAX RULES A SPECIAL-NAMES CLAUSE HOLDS ITS LITERAL OPERANDS TO — one value per clause, so
    /// <see cref="LiteralPhraseOperand"/> can never report one clause's rule under another's name (the PB770 leg d
    /// misfiling: a general helper with OPTIONAL arguments defaulted every caller to the CLASS clause's identity).
    /// §12.3.7.3 states the same four operand rules twice, once per clause, under different item numbers:
    /// <list type="bullet">
    /// <item>SR14 (ALPHABET literal-phrase) — b1/c1 the numeric-literal ordinal, b2/c2 the noninteger literal's
    /// class, b3/c3 one character "when a THROUGH or ALSO phrase is specified", b4/c4 the count;</item>
    /// <item>SR17 (CLASS) — b2/c2 the ordinal (in the native set "or, when the IN phrase is specified, … the
    /// character set referenced by alphabet-name-4"), b3/c3 the class, b4/c4 one character "when a THROUGH phrase
    /// is specified", b5/c5 the count.</item>
    /// </list>
    /// The b-arm applies when the ALPHANUMERIC phrase is specified or implied, the c-arm under NATIONAL.</summary>
    private sealed record LiteralPhraseRules(
        string What, bool National, DiagnosticDescriptor Code, string RuleNumber,
        char OrdinalItem, char ClassItem, char LengthItem, string LengthPhrases,
        CodedCharacterSet? InSet)
    {
        /// <summary>"SR14 b1" / "SR17 c3" — the sub-rule of THIS clause, in THIS class.</summary>
        public string Rule(char item) => $"{RuleNumber} {(National ? 'c' : 'b')}{item}";

        /// <summary>The class every noninteger operand shall be (SR14 b2/c2, SR17 b3/c3).</summary>
        public LiteralClass LiteralClass => National ? LiteralClass.National : LiteralClass.Alphanumeric;

        public static LiteralPhraseRules Alphabet(string name, bool national) => new(
            $"ALPHABET {name}{(national ? " FOR NATIONAL" : "")}", national, DiagnosticCatalog.AlphabetClauseViolation,
            "SR14", '1', '2', '3', "a THROUGH or ALSO phrase", InSet: null);

        public static LiteralPhraseRules Class(string name, bool national, CodedCharacterSet? inSet) => new(
            $"CLASS {name}{(national ? " FOR NATIONAL" : "")}", national, DiagnosticCatalog.ClassClauseViolation,
            "SR17", '2', '3', '4', "a THROUGH phrase", inSet);
    }

    /// <summary>The symbolic-character names the SPECIAL-NAMES paragraph being bound declares (see
    /// <c>SwitchBindSpecialNames</c>) — read by the §12.3.7.3 SR11 arm of <see cref="LiteralPhraseOperand"/>.</summary>
    private HashSet<string> _paragraphSymbolicNames = new(CobolNames.Comparer);

    /// <summary>⛔ THE ONE SPECIAL-NAMES literal-operand decoder (ISO §12.3.7.3 SR14 b/c for the ALPHABET clause,
    /// SR17 b/c for the CLASS clause — see <see cref="LiteralPhraseRules"/>): the characters one operand stands
    /// for, or null when it violates its clause's rule (the diagnostic is already reported).</summary>
    /// <remarks>The ordinal rule — "<i>Each numeric literal shall be an unsigned integer and shall have a value
    /// within the range of one through the maximum number of characters in the native … character set</i>" — is
    /// 1-based, so it names code unit <c>ordinal − 1</c> of the 65,536-character repertoire (§12.3.7.4 GR7 k1a),
    /// or, for a CLASS clause with an IN phrase, the character at that ordinal of alphabet-name-4's set (GR12 a).
    /// The class rule — "<i>Each noninteger literal shall be an alphanumeric / a national literal</i>" — is decided
    /// by the literal's own prefix. GR10 — the figurative words written inside SPECIAL-NAMES are the NATIVE
    /// extremes/values of the clause's class.
    /// <para>⛔ A numeric ordinal under <c>IN</c> names EVERY character at that ordinal of alphabet-name-4's set —
    /// an ALSO group has several (GR7 k6; <see cref="CodedCharacterSet.CharactersAt"/>, kb/Work PB1095) — and the
    /// operand carries how many, because a THROUGH phrase needs exactly one (<see cref="OneCharacterOperands"/>).</para></remarks>
    private PhraseOperand? LiteralPhraseOperand(Antlr4.Runtime.Tree.IParseTree operand, LiteralPhraseRules r)
    {
        PhraseOperand? Of(string? characters, int members = 1, bool ordinal = false) =>
            characters is null ? null : new PhraseOperand(operand, characters, members, ordinal);
        switch (operand)
        {
            // ⛔ A FIGURATIVE CONSTANT IS A LITERAL, not a word: `nonNumericLiteral : figurativeConstant | …`, so
            // SPACE / LOW-VALUE / QUOTE reach here through the literal arm whenever the spelling is a lexer token —
            // which is every ordinary spelling. The cobolWord arm below is the non-tokenized route (a >>COBOL-WORDS
            // synonym). Both call the ONE GR10 mapping. (The CLASS clause used to decode a figurative constant as
            // its own SPELLING — `CLASS C IS SPACE` was the class {S, P, A, C, E} — kb/Work PB976's sweep.)
            case Core.LiteralContext figLit when figLit.nonNumericLiteral()?.figurativeConstant() is { } fig:
                return Of(SpecialNamesFigurative(fig, FigurativeSlotOf(r)));
            case Core.LiteralContext lit:
                string text = lit.GetText();
                // The predefined NULL is no literal and no figurative constant (§8.4.3.10.3 SR1): the one refusal.
                if (lit.nonNumericLiteral()?.predefinedNull() is not null)
                {
                    PredefinedNullRule.Report(Edition, $"{r.What}: an operand of the literal phrase — each operand is a "
                        + $"numeric literal, {(r.National ? "a NATIONAL" : "an alphanumeric")} literal or a figurative "
                        + $"constant (ISO §12.3.7.3 {r.Rule(r.ClassItem)})");
                    return null;
                }
                // §12.3.7.3 SR11 — literal-1 … literal-6 "shall specify neither a symbolic-character figurative
                // constant nor a zero-length literal" (a symbolic character is no literal token here, so only the
                // second half can be written). Asked of the literal AS DECODED (IsZeroLengthOperand, kb/Work PB3218): a
                // concatenation of zero-length literals IS one (§8.8.3.3 GR2) though its tokens are not.
                if (IsZeroLengthOperand(lit))
                {
                    Edition.Error(r.Code, $"{r.What}: {ConcatFolder.Spelling(lit)} — an operand shall not be a zero-length "
                        + "literal (ISO §12.3.7.3 SR11)");
                    return null;
                }
                // ⛔ WHICH RULE is decided by the literal's FORM — an integer literal is held to the ordinal rule
                // whatever its value — never by whether its text fits an `int` (kb/Work PB1557's sibling):
                // `int.TryParse` failed on an 11-digit ordinal and sent it to the CLASS rule below, which reported
                // "each noninteger literal shall be an alphanumeric literal" for a literal that is an integer. A
                // NONINTEGER numeric literal (1.5) is not an integer, so it is the class rule's noninteger literal and
                // falls through to it (kb/Work PB770).
                if (lit.numericLiteral() is { } numeric && IntegerLiteralDigits(numeric) is ({ } digits, var signed))
                    return Ordinal(digits, signed, text);
                if (OperandLiteralClass(lit) == r.LiteralClass)
                    return Of(LiteralCharsOf(lit, sr11: true));
                // The class rule. A noninteger literal of the wrong class: name the rule, then RECOVER with the
                // literal's characters when it is a string at all, so one bad operand does not cascade.
                ClassRule(text);
                return Of(CobolLiteral.IsStringLiteral(text) ? CobolLiteral.Decode(text) : null);
            case Core.CobolWordContext w when _host.NamesConstant(w.GetText()):
                // A CONSTANT-NAME (§13.10.3 SR2, kb/Work PB1942): "as if literal-1 … were written where constant-name-1
                // is written" (§13.10.4 GR1), so its literal-1 meets every rule above — the ordinal rule for an integer
                // (GR3's integer forms included), the class rule otherwise, SR11's zero-length half. Its literal-1 is
                // never a figurative constant (§13.10.3 SR6).
                if (_host.SpecialNamesConstant(w, m => Edition.Error(r.Code, m), r.What, "a literal-phrase operand") is not { } k) return null;
                string spec = $"{w.GetText()} (the constant-name for {k.StandsFor})";
                if (k.Category is PicCategory.Numeric && k.IntegerText is { } integer)
                    return Ordinal(integer.TrimStart('-'), k.Text.StartsWith('+') || k.Text.StartsWith('-'), spec);
                if (IsZeroLengthConstant(k))
                {
                    Edition.Error(r.Code, $"{r.What}: {spec} — an operand shall not be a zero-length literal "
                        + "(ISO §12.3.7.3 SR11)");
                    return null;
                }
                LiteralClass? constantClass = k.Category switch
                {
                    PicCategory.Alphanumeric => LiteralClass.Alphanumeric,
                    PicCategory.National => LiteralClass.National,
                    PicCategory.Boolean => LiteralClass.Boolean,
                    _ => null,   // a noninteger numeric literal
                };
                if (constantClass == r.LiteralClass) return Of(k.Text);
                ClassRule(spec);
                return Of(constantClass is null ? null : k.Text);
            case Core.CobolWordContext w:
                // GR10: the NATIVE extremes/values of the clause's class. ⛔ There is NO `_ => t` fallback: an
                // unrecognized word is code-name-1/-2, which AlphabetCodeName has already resolved or refused for
                // the only shape it can legally take, and inside a multi-operand phrase it is a class-rule
                // violation — never the characters of its own spelling (kb/Work PB770 leg e).
                if (AlphabetFigurative(w.GetText()) is { } wordValue) return Of(wordValue);
                // §12.3.7.3 SR11 — literal-1 … literal-6 "shall specify neither a symbolic-character figurative
                // constant nor a zero-length literal": a symbolic-character name reaches here as a word, and it is
                // THAT rule it breaks, not the class rule below (kb/Work PB226).
                if (IsSymbolicCharacterName(w.GetText()))
                {
                    Edition.Error(r.Code, $"{r.What}: {w.GetText()} — an operand shall not be a symbolic-character "
                        + "figurative constant (ISO §12.3.7.3 SR11)");
                    return null;
                }
                Edition.Error(r.Code, $"{r.What}: {w.GetText()} is not a literal — each operand shall be a numeric "
                    + $"literal, {(r.National ? "a NATIONAL" : "an alphanumeric")} literal, a figurative constant or a "
                    + $"constant-name standing for a literal (ISO §12.3.7.3 {r.Rule(r.ClassItem)}; §12.3.7.4 GR10; "
                    + "§13.10.3 SR2)");
                return null;
        }
        return null;

        // The ordinal rule, BOTH halves of one sentence: "shall be an UNSIGNED INTEGER **and** shall have a value
        // within the range …" — of an integer literal as written, or of the integer a constant-name stands for.
        PhraseOperand? Ordinal(string digits, bool signed, string spelling)
        {
            if (signed)
            {
                Edition.Error(r.Code, $"{r.What}: {spelling} — each numeric literal shall be an UNSIGNED integer "
                    + $"(ISO §12.3.7.3 {r.Rule(r.OrdinalItem)})");
                return null;
            }
            int ordinal = WiseOwl.Cobol.Validation.IntegerOperandRules.HostValue(digits);
            if (r.InSet is { } inSet)
            {
                // GR12 a — the ordinal names every character at that position of the IN set (an ALSO group's
                // members, GR7 k6); the SYMBOLIC CHARACTERS clause alone takes literal-1 (SwitchBindSymbolic).
                if (inSet.CharactersAt(ordinal) is { } named) return Of(string.Concat(named), named.Count, ordinal: true);
                Edition.Error(r.Code, $"{r.What}: the ordinal {spelling} does not exist in the character set "
                    + $"referenced by the IN alphabet ({inSet.Phrase}, {inSet.OrdinalCount} characters) — ISO "
                    + $"§12.3.7.3 {r.Rule(r.OrdinalItem)}");
                return null;
            }
            if (ordinal is >= 1 and <= CollatingTable.Repertoire) return Of(((char)(ordinal - 1)).ToString(), ordinal: true);
            Edition.Error(r.Code, $"{r.What}: the ordinal {spelling} does not exist in the native "
                + $"{(r.National ? "national" : "alphanumeric")} character set ({CollatingTable.Repertoire} "
                + "characters) — each numeric literal shall be an unsigned integer with a value from one "
                + $"through the maximum number of characters in that set (ISO §12.3.7.3 {r.Rule(r.OrdinalItem)})");
            return null;
        }

        // The class rule, for a noninteger literal of the wrong class.
        void ClassRule(string spelling) =>
            Edition.Error(r.Code, $"{r.What}: {spelling} — each noninteger literal shall be "
                + $"{(r.National ? "a NATIONAL literal (N\"…\")" : "an alphanumeric literal")} "
                + $"(ISO §12.3.7.3 {r.Rule(r.ClassItem)})");
    }

    /// <summary>The digits and sign of a numeric literal that is an INTEGER — a fixed-point integer core with an
    /// optional sign (§8.3.3.3.2) — or null for a noninteger one (decimal, floating-point). The SPECIAL-NAMES
    /// ordinal rule (ISO §12.3.7.3 SR14 b1/c1, SR17 b2/c2) is asked of the PARSE, not of the text:
    /// <c>int.TryParse</c> accepts a sign and FAILS on an integer too long for an <c>int</c>, so it answered
    /// "is this an integer?" and "what is its value?" as one question and got the first wrong whenever the second
    /// overflowed. The VALUE is read by THE one ordinal reader, <c>IntegerOperandRules.HostValue</c> — the same one
    /// SYMBOLIC CHARACTERS integer-1 (SR16 e/f) is read by, so the three ordinal sites of this paragraph (ALPHABET,
    /// CLASS, SYMBOLIC CHARACTERS) cannot disagree about an integer too long for an <c>int</c> (kb/Work PB1091,
    /// PB1557's sibling): it saturates, and a saturated ordinal is out of range of every character set exactly when
    /// the literal is.</summary>
    private static (string Digits, bool Signed)? IntegerLiteralDigits(Core.NumericLiteralContext numeric)
    {
        if (numeric.signedNumericLiteral() is not { } s || s.numericLiteralCore() is not { ChildCount: 1 } core)
            return null;
        bool separateSign = s.PLUS() is not null || s.MINUS() is not null;
        if (core.INTEGERLIT() is [{ } unsigned]) return (unsigned.GetText(), separateSign);
        // A sign-adjacent literal (a FUNCTION-argument lexer region) is one token whose first character is the sign.
        if (core.SIGNED_INTEGERLIT() is { } adjacent) return (adjacent.GetText()[1..], true);
        return null;
    }

    /// <summary>⛔ WHERE A FIGURATIVE CONSTANT STANDS IN A SPECIAL-NAMES LITERAL SLOT, as data — what the slot is
    /// called, how it reports a refusal, what class it demands of an <c>ALL literal-1</c> operand and how it phrases
    /// the §12.3.7.3 SR11 symbolic-character bar. One value per CLAUSE KIND (kb/Work PB1537): the ALPHABET / CLASS
    /// operands (<see cref="FigurativeSlotOf"/>) and the ORDER TABLE / LOCALE text literals
    /// (<see cref="TryClauseTextLiteral"/>) read their figurative constants through the ONE
    /// <see cref="SpecialNamesFigurative"/>, which had been written only for the first two — the third screen refused
    /// EVERY figurative constant under a rule that bars only one kind of them.</summary>
    /// <param name="What">The clause, for the message ("ALPHABET AL", "ORDER TABLE T1 IS SPACE").</param>
    /// <param name="Refuse">Reports one refusal under the slot's own diagnostic.</param>
    /// <param name="ClassClause">The slot's class rule in its own words (appended to the operand).</param>
    /// <param name="ClassRule">The syntax rule it is, e.g. "SR14 b2".</param>
    /// <param name="Admits">Whether an <c>ALL literal-1</c> of that category is admitted.</param>
    /// <param name="SymbolicClause">The SR11 sentence for a symbolic-character operand.</param>
    private readonly record struct FigurativeSlot(string What, Action<string> Refuse, string ClassClause,
        string ClassRule, Func<PicCategory, bool> Admits, string SymbolicClause);

    /// <summary>The ALPHABET / CLASS clause's figurative slot (§12.3.7.3 SR14 b2/c2, SR17 b3/c3, SR11).</summary>
    private FigurativeSlot FigurativeSlotOf(LiteralPhraseRules r) => new(r.What,
        message => Edition.Error(r.Code, message),
        $"each noninteger literal shall be {(r.National ? "a NATIONAL literal (N\"…\")" : "an alphanumeric literal")}",
        r.Rule(r.ClassItem),
        category => category == (r.National ? PicCategory.National : PicCategory.Alphanumeric),
        "an operand shall not be a symbolic-character figurative constant");

    /// <summary>⛔ THE ONE DECODING of a figurative constant written in a SPECIAL-NAMES literal slot (ISO §12.3.7.4
    /// GR10; §8.3.3.6.3 SR1 — "<i>A figurative constant may be used whenever 'literal' appears in a format</i>"): the
    /// characters it stands for, or null after a refusal. SPACE, QUOTE and ZERO are their §8.3.3.6.4 GR1 characters;
    /// HIGH-VALUE and LOW-VALUE are the NATIVE extremes (GR10); <c>ALL literal-1</c> is literal-1 itself (§8.3.3.6.4
    /// GR3 c, "<i>the length of the string is the length of literal-1</i>"), <c>ALL SPACE</c> and its kin one
    /// character (GR3 b). The ONE refusal the slots share is SR11's "<i>neither a symbolic-character figurative
    /// constant nor a zero-length literal</i>": <c>ALL symbolic-character-1</c> is the symbolic-character figurative
    /// constant a literal-slot can still spell (Format 7), and was reported as "not a character" before — a real
    /// rule answering a different question. ⛔ It used to be REFUSED outright in an ALPHABET literal phrase under that
    /// same SR11 (kb/Work PB976's sweep) and, in the ORDER TABLE / LOCALE screen, STILL was (kb/Work PB1537).</summary>
    private string? SpecialNamesFigurative(Core.FigurativeConstantContext fig, FigurativeSlot slot)
    {
        if (fig.ALL() is null) return AlphabetFigurative(fig.GetText()) ?? FigurativeNotACharacter(fig, slot);
        if (fig.cobolWord() is { } word && IsSymbolicCharacterName(word.GetText()))
        {
            slot.Refuse($"{slot.What}: {ConcatFolder.Spelling(fig)} — {slot.SymbolicClause} (ISO §12.3.7.3 SR11)");
            return null;
        }
        return ConcatFolder.FoldAllLiteral(fig, LiteralEnvironment.SpecialNames(_host, refusesSymbolicCharacters: true),
                Edition) is { } literal1 ? AllLiteralCharacters(fig, literal1, slot)
            : AlphabetFigurative(fig.GetChild(1).GetText()) is { } oneChar ? oneChar
            : FigurativeNotACharacter(fig, slot);
    }

    /// <summary>A figurative constant that names no character (<c>ALL</c> over a word that is neither a literal nor
    /// a constant) — never an operand of these clauses: §12.3.7.4 GR10 maps HIGH-VALUE and LOW-VALUE, §8.3.3.6.4 GR1
    /// SPACE, QUOTE and ZERO, and nothing maps the rest. (The predefined NULL is no figurative constant at all —
    /// §8.4.3.10.3 SR1, <see cref="PredefinedNullRule"/>.)</summary>
    private static string? FigurativeNotACharacter(Core.FigurativeConstantContext fig, FigurativeSlot slot)
    {
        slot.Refuse($"{slot.What}: {ConcatFolder.Spelling(fig)} — the figurative constant is not a character of the native "
            + "character set, so it cannot be an operand of this clause (ISO §12.3.7.4 GR10 names HIGH-VALUE and "
            + "LOW-VALUE; §8.3.3.6.4 GR1 SPACE, QUOTE and ZERO)");
        return null;
    }

    /// <summary>The characters of an <c>ALL literal-1</c> operand: literal-1 itself (§8.3.3.6.4 GR3 c, "the length
    /// of the string is the length of literal-1"), folded by the ONE Format 6 reader
    /// (<see cref="ConcatFolder.FoldAllLiteral"/> — a quoted literal, a concatenation expression classed by the
    /// pairwise §8.8.3.3 GR1 fold, or a constant-name) and held to the slot's class rule as ONE literal: a
    /// concatenation expression is "equivalent to a literal of the same class and value" (§8.8.3.3 GR3).</summary>
    private static string? AllLiteralCharacters(Core.FigurativeConstantContext fig, ConcatFolder.Folded literal1,
        FigurativeSlot slot)
    {
        if (slot.Admits(literal1.Category)) return literal1.Value;
        slot.Refuse($"{slot.What}: {ConcatFolder.Spelling(fig)} — {slot.ClassClause} (ISO §12.3.7.3 {slot.ClassRule})");
        return null;
    }

    /// <summary>The CLASS of a noninteger literal operand — alphanumeric (§8.3.3.2, incl. the hexadecimal format
    /// X"hh…"), national (§8.3.3.5 — N"…" / NX"…") or boolean — as §12.3.7.3 SR14 b2/c2 and SR17 b3/c3 ask it; null for a
    /// word that is no quoted literal. ⛔ ONE classifier, <see cref="CobolLiteral.ClassOf"/>, keyed on the parsed
    /// PREFIX: the former alphanumeric test asked <c>CobolLiteral.IsStringLiteral</c>, which answers true for EVERY
    /// prefixed quoted literal, so N"A" and B"1" passed as alphanumeric and <c>ALPHABET A IS N"A"</c> /
    /// <c>CLASS C IS N"0" THRU N"9"</c> compiled clean (kb/Work PB976). A §8.8.3.3 concatenation expression is of
    /// the class §8.8.3.3 GR1 folds pairwise — THE ConcatFolder answer, never the leading operand's prefix, which
    /// called <c>SPACE &amp; N"A"</c> alphanumeric (kb/Work PB1406).</summary>
    internal LiteralClass? OperandLiteralClass(Core.LiteralContext lit)
    {
        if (lit.nonNumericLiteral()?.concatenationExpression() is { } ce)
            return ConcatFolder.ClassOf(ce, LiteralEnvironment.SpecialNames(_host, refusesSymbolicCharacters: true)) switch
            {
                PicCategory.National => LiteralClass.National,
                PicCategory.Boolean => LiteralClass.Boolean,
                _ => LiteralClass.Alphanumeric,
            };
        return CobolLiteral.ClassOf(lit.GetText());
    }

    /// <summary>⛔ THE ONE QUESTION "IS THIS SPECIAL-NAMES LITERAL OPERAND ZERO-LENGTH?" (ISO §12.3.7.3 SR11 — literal-1
    /// … literal-6 and literal-9 "shall specify neither a symbolic-character figurative constant nor a zero-length
    /// literal"; kb/Work PB3218). It is asked of the literal AS DECODED, never of its tokens: §8.8.3.3 GR2 makes a
    /// concatenation of zero-length literals a zero-length literal, and GR3 makes any concatenation expression usable
    /// "anywhere a literal of that class may be used", so <c>"" &amp; ""</c> is the zero-length literal SR11 bars. The
    /// ALPHABET / CLASS operands (<see cref="LiteralPhraseOperand"/>) tested the raw token (not zero-length as written)
    /// while the LOCALE / ORDER TABLE literals (<see cref="TryClauseTextLiteral"/>) tested the folded value, so
    /// <c>ALPHABET AZ IS "" &amp; ""</c> compiled clean beside the LOCALE clause's refusal of the same operand. A plain
    /// token is asked structurally (<see cref="CobolLiteral.IsZeroLength"/> — a malformed hexadecimal literal decodes to
    /// nothing too and is LiteralScreen's COBOL1635, not SR11), a concatenation through the fold's diagnostic-free
    /// walk (the reporting fold is <see cref="LiteralCharsOf"/>'s).</summary>
    private bool IsZeroLengthOperand(Core.LiteralContext lit) =>
        lit.nonNumericLiteral()?.concatenationExpression() is { } ce
            ? ConcatFolder.Peek(ce, LiteralEnvironment.SpecialNames(_host, refusesSymbolicCharacters: true)).Value.Length == 0
            : CobolLiteral.IsZeroLength(lit.GetText());

    /// <summary>The constant-name arm of <see cref="IsZeroLengthOperand"/>: a constant-name is "as if literal-1 … were
    /// written" (§13.10.4 GR1), so a nonnumeric constant whose literal-1 is zero-length is the zero-length literal SR11
    /// bars. The same structural test <c>ConcatFolder</c> takes of a constant operand (its re-quoted
    /// <see cref="ConstantDef.RawText"/>).</summary>
    private static bool IsZeroLengthConstant(DataBinder.ConstantDef constant) =>
        constant.Category is not PicCategory.Numeric && CobolLiteral.IsZeroLength(constant.RawText);

    /// <summary>Whether <paramref name="word"/> is a symbolic-character name of this paragraph — declared by any
    /// SYMBOLIC CHARACTERS clause, read syntactically because the clauses are order-free (kb/Work PB226), or already
    /// bound. The ONE recognition §12.3.7.3 SR11 asks, of a bare operand and of a concatenation operand alike.</summary>
    internal bool IsSymbolicCharacterName(string word) =>
        _paragraphSymbolicNames.Contains(word) || SymbolicCharacters.Declares(word);

    /// <summary>The characters of an alphabet-entry string literal: a §8.8.3.3 GR3 concatenation folded first — in
    /// the SPECIAL-NAMES environment (§12.3.7.4 GR10's native extremes), with §12.3.7.3 SR11's
    /// symbolic-character prohibition when <paramref name="sr11"/> —, the §8.3.3.2 hexadecimal format decoded
    /// pairwise, otherwise the literal's own characters. ⛔ It never resolves an ORDINAL — that is SR14 b1/c1's job
    /// and it lives in <see cref="AlphabetOperands"/>, so the ALPHABET path can no longer inherit the CLASS clause's
    /// descriptor, message and rule number (kb/Work PB770 leg d).
    /// <para>⛔ A word that is not a literal is not a literal (kb/Work PB791): this used to END with <c>return text</c>,
    /// the SPELLING of whatever it was handed, which is how <c>ALPHABET X IS SPACE</c> positioned S, P, A, C, E
    /// (PB770 fixed that arm) and <c>CURRENCY SIGN IS SPACE PICTURE SYMBOL "#"</c> bound the currency string "SPACE"
    /// (the other caller). Both callers screen the operand's class first (<see cref="LiteralPhraseOperand"/>,
    /// <see cref="CurrencyOperandOf"/>), so what reaches here is a quoted literal — hexadecimal included, which
    /// <see cref="CobolLiteral.Decode"/> decodes itself — or a concatenation expression, and anything else is a caller
    /// that skipped its screen: a defect to find at once, never a value to invent.</para></summary>
    internal string LiteralCharsOf(Core.LiteralContext lit, bool sr11)
    {
        if (lit.nonNumericLiteral()?.concatenationExpression() is { } ce)
            return ConcatFolder.Fold(ce, Edition, LiteralEnvironment.SpecialNames(_host, sr11)).Value;
        string text = lit.GetText();
        if (CobolLiteral.IsStringLiteral(text)) return CobolLiteral.Decode(text);
        throw new InvalidOperationException($"'{text}' is neither a quoted literal nor a concatenation expression — "
            + "the caller shall refuse an operand of any other form before decoding it (kb/Work PB791)");
    }

    /// <summary>The one-character rule a THROUGH (or, in an alphabet, ALSO) phrase imposes on its operands —
    /// §12.3.7.3 SR14 b3/c3 for the ALPHABET clause, SR17 b4/c4 for the CLASS clause, one check for both. False,
    /// with each offending operand reported, when any is not exactly one character.
    /// <para>⛔ The syntax rule governs "each alphanumeric / national LITERAL", so it is asked of the operand AS WRITTEN
    /// (<see cref="PhraseOperand.IsOrdinal"/>): a NUMERIC literal is an ordinal, names exactly one character of its set,
    /// and is held to the ordinal rule instead (SR14 b1/c1, SR17 b2/c2). What a THROUGH phrase then needs of that
    /// character is a POSITION in the native set: §12.3.7.4 GR12 (CLASS) expands "the contiguous characters in the native
    /// character set" between the bounds and GR7 k5 (ALPHABET) "the set of consecutive characters in the native character
    /// set", and the native national set is the 65,536 UTF-16 code units (DOC-A.1-188; docs/CONFORMANCE.md) — so a
    /// numeric ordinal under <c>IN</c> UCS-4 / UTF-8 that names a SUPPLEMENTARY character (two code units) is a character
    /// with no position in that run, and is refused under the rule that says so, not under c4 (kb/Work PB1091). An ALSO
    /// phrase (ALPHABET only, GR7 k6: the operands share ONE ordinal position) asks the same of each operand.</para></summary>
    /// <param name="through">True for a THROUGH phrase, false for an ALSO phrase — the rule the supplementary-character
    /// refusal names.</param>
    private bool OneCharacterOperands(IReadOnlyList<PhraseOperand> operands, LiteralPhraseRules r, bool through)
    {
        bool ok = true;
        foreach (var op in operands)
        {
            if (op.IsOrdinal)
            {
                if (op.Characters.Length == 1) continue;
                if (op.Members > 1)
                {
                    // An ordinal that lands on an ALSO group (GR7 k6) names SEVERAL characters, and a THROUGH bound
                    // is ONE character: the specification defines no run that begins at, or ends at, a group.
                    Edition.Error(r.Code, $"{r.What}: the ordinal {op.Spelling} names {op.Members} characters of the "
                        + "character set referenced by the IN alphabet (an ALSO group shares one ordinal position, "
                        + "ISO §12.3.7.4 GR7 k6), so it is no single character to begin or end the run of native "
                        + "characters a THROUGH phrase expands (ISO §12.3.7.4 GR12)");
                    ok = false;
                    continue;
                }
                Edition.Error(r.Code, $"{r.What}: the ordinal {op.Spelling} names a character outside the native "
                    + $"{(r.National ? "national" : "alphanumeric")} character set (it needs {op.Characters.Length} of its "
                    + "16-bit code units), so it has no position in "
                    + (through
                        ? "the run of native characters a THROUGH phrase expands "
                          + $"(ISO §12.3.7.4 {(r.RuleNumber == "SR17" ? "GR12" : "GR7 k5")}"
                        : "the native character set whose characters an ALSO phrase gives one shared ordinal position "
                          + "(ISO §12.3.7.4 GR7 k6")
                    + "; the native set is the 65,536 UTF-16 code units — docs/CONFORMANCE.md DOC-A.1-188)");
                ok = false;
                continue;
            }
            string chars = op.Characters;
            if (chars.Length != 1)
            {
                Edition.Error(r.Code, $"{r.What}: the operand '{chars}' is {chars.Length} characters — each "
                    + $"{(r.National ? "national" : "alphanumeric")} literal, when {r.LengthPhrases} is specified, shall "
                    + $"be one character in length (ISO §12.3.7.3 {r.Rule(r.LengthItem)})");
                ok = false;
            }
        }
        return ok;
    }
}
