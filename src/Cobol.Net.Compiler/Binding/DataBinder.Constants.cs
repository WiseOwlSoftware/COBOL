// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using Antlr4.Runtime.Tree;
using CobolNet.Common;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

using CobolNet.Binding.Model;

namespace CobolNet.Binding;

using Core = CobolParserCore;

/// <summary>
/// The CONSTANT-entry half of the data binder (ISO/IEC 1989:2023 §13.10 constant entry + §13.18.15 CONSTANT
/// RECORD; P10 Step 15). A constant entry defines a COMPILE-TIME substitution: §13.10.4 GR1/GR3 make every
/// reference to constant-name-1 "as if [the] literal were written where constant-name-1 is written" — so a
/// constant occupies NO storage and produces NO <see cref="DataItem"/>; <see cref="BindConstantEntry"/> folds
/// the entry into the per-unit compile-time constant table (<see cref="_constants"/>), and the reference
/// chokepoints substitute the literal:
/// <list type="bullet">
/// <item><c>ExpressionBinder.FieldOperand</c> / <c>RefExpr</c> — the ONE dataReference→operand/expression
/// mappings every verb consumes (MOVE/DISPLAY sources, relations, PERFORM TIMES, arithmetic);</item>
/// <item><c>ReferenceResolver.ResolveSubscriptName</c> — subscript positions (§13.10.3 SR2: an integer
/// constant-name stands where the format specifies an integer);</item>
/// <item><see cref="OccursBoundValue"/> — the OCCURS integer-1/integer-2 bounds (SR2, via the
/// <c>occursBound</c> grammar alternative);</item>
/// <item><see cref="ExpandPicConstants"/> — PICTURE repetition <c>X(K)</c> (SR2 second sentence);</item>
/// <item><c>ExtractValue</c> — a VALUE-clause constant-name operand substitutes its raw literal text (the
/// text-plumbed data path, the ConcatFolder RawText precedent);</item>
/// <item><c>ExpressionBinder.ResolveReceiving</c> — a constant-name as a receiving operand rejects
/// (<c>COBOLNET1548</c> — a literal cannot be a receiver; SR2 permits a constant only where a LITERAL is
/// permitted).</item>
/// </list>
/// The four AS forms (§13.10.2): <b>AS literal-1</b> (GR1/GR2 — the constant IS the literal, class and
/// category preserved; a single numeric literal in the arithmetic-expression position re-classifies as a
/// literal per SR1, so <c>AS 0.25</c> keeps its non-integer value); <b>AS arithmetic-expression-1</b>
/// (GR4 — evaluated per §7.3.6 compile-time arithmetic: no exponentiation SR1a, fixed-point numeric literal
/// operands SR1b — a constant-name operand substitutes its literal per §13.10.3 SR2/GR1, with SR4/SR5 ruling
/// out circularity — no division by zero SR1c, the final result truncated to its integer part §7.3.6.3 GR3;
/// intermediate results ride .NET <see cref="decimal"/>, the documented §7.3.6.2 SR2 implementor choice);
/// <b>AS LENGTH OF data-name-2</b> (GR6 — the value of the §15.50 LENGTH function: <c>DataItem.ImageWidth</c>,
/// THE character-position width authority the FUNCTION LENGTH fold reads, which is maximum-allocation based
/// so the GR6 occurs-depending exception holds); <b>AS BYTE-LENGTH OF</b> (GR5) is STAGED LOUD — the §15.14
/// BYTE-LENGTH intrinsic is itself a Deferred catalog row and the byte-width authority lands once, with it.
/// The <b>FROM compilation-variable-name-1</b> leg (GR1 — the &gt;&gt;DEFINE tie-in) is STAGED LOUD: the
/// preprocessor's compilation-variable store is local to the text stage (<c>ConditionalCompilationProcessor.
/// Process</c>) and not reachable at bind time; the SR8 "currently true" position-correct capture across COPY
/// expansion is the recorded residue.
/// Definition-before-reference is required (the GR1 textual-substitution model — substitution happens as the
/// source is bound, in order); a forward reference fails as an unknown constant-name.
/// </summary>
public sealed partial class DataBinder
{
    /// <summary>One folded compile-time constant (ISO §13.10). <paramref name="Text"/> is the substitution
    /// value: for class numeric the normalized literal text (dot-decimal, sign included); for the string
    /// classes the DECODED character value. <paramref name="RawText"/> is the equivalent literal as RAW source
    /// text (re-quoted per class) — the currency of the text-plumbed paths (the DATA-division VALUE capture),
    /// mirroring <c>ConcatFolder.Folded.RawText</c>.</summary>
    public sealed record ConstantDef(
        string Name, PicCategory Category, string Text, bool IsInteger, bool IsGlobal, string RawText);

    /// <summary>The per-unit compile-time constant table (§13.10.4 GR1 substitution source). GLOBAL visibility
    /// into contained programs (the [IS GLOBAL] phrase) is parsed and recorded but not yet propagated — a
    /// nested-program reference fails loud as an unresolved name (recorded P10 residue).</summary>
    private readonly Dictionary<string, ConstantDef> _constants = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The defined constant named <paramref name="name"/>, or null (§13.10.4 GR1 lookup).</summary>
    internal ConstantDef? FindConstant(string name) => _constants.TryGetValue(name, out var d) ? d : null;

    /// <summary>The constant a BARE (unqualified, unsubscripted) data reference names, or null. A constant-name
    /// takes no qualifiers, subscripts, or reference-modification — it substitutes a literal (§13.10.4 GR1) —
    /// so any suffixed reference falls through to ordinary resolution.</summary>
    internal ConstantDef? ConstantOf(Core.DataReferenceContext dref) =>
        dref.dataReferenceSuffix().Length == 0 && dref.cobolWord() is { } w
        && _constants.TryGetValue(w.GetText(), out var def) ? def : null;

    /// <summary>Whether <paramref name="item"/> is, or is subordinate to, a CONSTANT RECORD (ISO §13.18.15.3
    /// SR2 — "neither the data item described by the subject of the entry nor any data item subordinate to
    /// [it] shall be specified as a receiving data item"). The flag lives on the level-01 root; this walk
    /// covers the subtree.</summary>
    internal bool IsConstantRecordItem(DataItem item)
    {
        for (DataItem? n = item; n is not null; n = n.Parent)
            if (n.IsConstantRecord) return true;
        return false;
    }

    /// <summary>The §13.18.15.3 SR2 receiving-operand guard for CONSTANT RECORD content, shared by every
    /// receiving chokepoint that resolves its own <see cref="Place"/> (ACCEPT / INITIALIZE / INSPECT targets /
    /// MOVE CORRESPONDING receivers) in addition to the ONE <c>ExpressionBinder.ResolveReceiving</c> spine.
    /// True (and the <c>COBOLNET1548</c> diagnostic reported) when the place stores into a structured
    /// constant.</summary>
    internal bool RejectConstantStore(Place? place, string what)
    {
        if (place is null || !IsConstantRecordItem(place.Item)) return false;
        DataItem root = place.Item;
        while (root.Parent is { } p) root = p;
        Edition.Error(DiagnosticCatalog.ConstantAsReceiver, $"{what} names a data item of the CONSTANT RECORD "
            + $"'{root.CobolName ?? root.CsName}' — the content of a structured constant cannot be modified "
            + "(ISO §13.18.15.3 SR2)");
        return true;
    }

    // ── The constant-entry bind (§13.10) ─────────────────────────────────────────────────────────────────────

    /// <summary>Bind one constant entry (§13.10.2 general format — the <c>constantEntryBody</c> alternative of
    /// <c>dataDescriptionBody</c>) into the constant table. Produces NO <see cref="DataItem"/> (§13.10.4
    /// GR1/GR3 — a constant is a substitution, not storage). The COBOL-2002 introduction gate is the
    /// VersionConformancePass parse arm (<c>VisitConstantEntryBody</c> → constant-entry-2002 → COBOLNET0900
    /// below 2002), NOT here (the binder stays edition-agnostic — Step E).</summary>
    private void BindConstantEntry(Core.DataDescriptionEntryContext entry, Core.ConstantEntryBodyContext body)
    {
        string? name = entry.dataName()?.GetText();
        string where = $"constant entry '{name ?? "?"}'";
        // §13.10.2: the general format admits level {1 | 01} only, and constant-name-1 is mandatory.
        if (entry.levelNumber().GetText() is not ("1" or "01"))
            Edition.Error(DiagnosticCatalog.ConstantEntryRule,
                $"{where}: a constant entry shall have level-number 1 or 01 (ISO §13.10.2)");
        if (name is null || name.Equals("FILLER", StringComparison.OrdinalIgnoreCase))
        {
            Edition.Error(DiagnosticCatalog.ConstantEntryRule,
                "a constant entry shall be named — constant-name-1 is required (ISO §13.10.2)");
            return;
        }
        bool isGlobal = body.GLOBAL() is not null;

        // FROM compilation-variable-name-1 (§13.10.4 GR1's >>DEFINE leg) — STAGED LOUD: the compilation-
        // variable store is local to the preprocessor text stage; see the class doc + the catalog descriptor.
        if (body.FROM() is not null)
        {
            Edition.Error(DiagnosticCatalog.ConstantFromCompilationVariable,
                $"{where}: CONSTANT … FROM compilation-variable-name (ISO §13.10, the >>DEFINE tie-in)");
            return;
        }

        var cv = body.constantValue();
        ConstantDef? def =
            cv.LENGTH() is not null ? BindConstantLength(name, isGlobal, cv.dataReference(), where)
            : cv.nonNumericLiteral() is { } nn ? BindConstantStringLiteral(name, isGlobal, nn, where)
            : BindConstantArithmetic(name, isGlobal, cv.arithmeticExpression(), where);
        if (def is null) return;

        // §13.10.3 SR9: a duplicate constant-name shall carry the SAME specification as the prior entry.
        if (_constants.TryGetValue(name, out var prior))
        {
            if (prior.Category != def.Category || prior.Text != def.Text)
                Edition.Error(DiagnosticCatalog.ConstantEntryRule, $"{where}: duplicates constant-name "
                    + $"'{prior.Name}' with a different specification — a duplicated constant-name shall carry "
                    + "the same specification (ISO §13.10.3 SR9)");
            return;
        }
        _constants[name] = def;
    }

    /// <summary>AS literal-1 for the STRING classes (§13.10.4 GR1/GR2 — the constant is the literal; class and
    /// category preserved). A §8.8.3 concatenation expression folds to its equivalent single literal FIRST
    /// (GR3 of §8.8.3.3 — the ConcatFolder chokepoint), so <c>AS "A" &amp; "B"</c> is the literal "AB". A
    /// figurative constant is rejected (§13.10.3 SR6).</summary>
    private ConstantDef? BindConstantStringLiteral(
        string name, bool isGlobal, Core.NonNumericLiteralContext nn, string where)
    {
        if (nn.concatenationExpression() is { } ce)
        {
            var folded = ConcatFolder.Fold(ce, Edition, Collating, NationalCollating);
            return new ConstantDef(name, folded.Category, folded.Value, false, isGlobal, folded.RawText);
        }
        if (nn.figurativeConstant() is not null)
        {
            Edition.Error(DiagnosticCatalog.ConstantEntryRule, $"{where}: literal-1 shall not be a figurative "
                + $"constant (ISO §13.10.3 SR6; '{nn.GetText()}')");
            return null;
        }
        var (cat, value) =
            nn.STRINGLIT() is { } s ? (PicCategory.Alphanumeric, CobolLiteral.Decode(s.GetText()))
            : nn.HEXLIT() is { } x ? (PicCategory.Alphanumeric, CobolLiteral.DecodeHex(x.GetText()))
            : nn.NATLIT() is { } nat ? (PicCategory.National, CobolLiteral.Decode(nat.GetText()))
            : (PicCategory.Boolean, CobolLiteral.Decode(nn.BOOLLIT()!.GetText()));
        return new ConstantDef(name, cat, value, false, isGlobal,
            new ConcatFolder.Folded(cat, value).RawText);
    }

    /// <summary>The arithmetic-expression AS form. §13.10.3 SR1 first: an operand that is a SINGLE numeric
    /// literal is a LITERAL, not an arithmetic expression — <c>AS 0.25</c> keeps class/category numeric with
    /// its non-integer value (GR1/GR2), where the expression form would have truncated to an integer (GR4).
    /// Otherwise the expression evaluates per §7.3.6 and the result is an integer (GR4 + §7.3.6.3 GR3). The
    /// <c>BYTE-LENGTH OF x</c> form parses through this leg as a qualified dataReference (no dedicated token)
    /// and is recognized and STAGED LOUD here (GR5; the §15.14 intrinsic is itself Deferred).</summary>
    private ConstantDef? BindConstantArithmetic(
        string name, bool isGlobal, Core.ArithmeticExpressionContext expr, string where)
    {
        // §13.10.3 SR1 — a single (possibly signed) numeric literal is treated as a literal.
        if (SoleNumericLiteral(expr) is { } lit)
        {
            string text = NormalizeNumericLiteral(lit.Text);
            if (lit.Negative) text = "-" + text.TrimStart('+');
            bool isInt = !text.Contains('.') && !text.Contains('E') && !text.Contains('e');
            return new ConstantDef(name, PicCategory.Numeric, text, isInt, isGlobal, text);
        }
        // The BYTE-LENGTH OF form (§13.10.4 GR5) — recognized as the sole qualified dataReference shape.
        if (Procedure.ConditionBinder.SoleDataRef(expr) is { } sole
            && sole.cobolWord()?.GetText().Equals("BYTE-LENGTH", StringComparison.OrdinalIgnoreCase) is true
            && sole.dataReferenceSuffix().Any(sfx => sfx.qualification() is not null))
        {
            Edition.Error(DiagnosticCatalog.ConstantByteLength,
                $"{where}: CONSTANT … AS BYTE-LENGTH OF (ISO §13.10.4 GR5 / §15.14)");
            return null;
        }
        if (EvalConstExpr(expr, where) is not { } v) return null;
        // §7.3.6.3 GR3: the final result is truncated to its integer part and is an integer numeric literal.
        string result = decimal.Truncate(v).ToString(CultureInfo.InvariantCulture);
        return new ConstantDef(name, PicCategory.Numeric, result, true, isGlobal, result);
    }

    /// <summary>AS LENGTH OF data-name-2 (§13.10.4 GR6): the value of the §15.50 LENGTH function — the item's
    /// length in character positions, read from <c>DataItem.ImageWidth</c> (THE width authority the FUNCTION
    /// LENGTH fold reads; maximum-allocation based, so the GR6 occurs-depending-group exception — "the maximum
    /// size of the data item is used" — holds by construction). SR checks: SR3 all subscripts shall be
    /// literals; SR10 no ANY LENGTH operand; SR12 no dynamic-length operand. data-name-2 must already be bound
    /// (definition-before-reference — SR4 rules out the reverse dependence).</summary>
    private ConstantDef? BindConstantLength(
        string name, bool isGlobal, Core.DataReferenceContext dref, string where)
    {
        string? baseName = dref.cobolWord()?.GetText();
        if (baseName is null) return null;
        var qualifiers = new List<string>();
        foreach (var suffix in dref.dataReferenceSuffix())
        {
            if (suffix.qualification() is { } q) qualifiers.Add(q.cobolWord().GetText());
            else if (suffix.subscriptPart()?.subscriptOrRefMod() is { } sub)
            {
                // §13.10.3 SR3: all subscripts of data-name-2 shall be literals. (A subscript never changes
                // the LENGTH — every occurrence has the same description — so the tokens are only validated.)
                var toks = new List<Antlr4.Runtime.IToken>();
                ReferenceResolver.CollectLeafTokens(sub, toks);
                if (toks.Any(t => t.Type is not (Core.SUB_INTEGERLIT or Core.INTEGERLIT or Core.SUB_WS
                        or Core.SUB_LPAREN or Core.SUB_RPAREN or Core.SUB_COMMA)))
                {
                    Edition.Error(DiagnosticCatalog.ConstantEntryRule, $"{where}: all subscripts of the LENGTH "
                        + "OF operand shall be literals (ISO §13.10.3 SR3)");
                    return null;
                }
            }
        }
        DataItem? item = new ReferenceResolver(this).FindItem(baseName, qualifiers);
        if (item is null)
        {
            Edition.Error(DiagnosticCatalog.ConstantEntryRule, $"{where}: LENGTH OF '{dref.GetText()}' — the "
                + "data-name is not defined at this point (a constant entry reads only PRECEDING declarations; "
                + "ISO §13.10.3 SR4 rules out the reverse dependence)");
            return null;
        }
        if (item.IsAnyLength)
        {
            Edition.Error(DiagnosticCatalog.ConstantEntryRule, $"{where}: the LENGTH OF operand shall not be "
                + "described with the ANY LENGTH clause (ISO §13.10.3 SR10)");
            return null;
        }
        if (HasDynamicTable(item))
        {
            Edition.Error(DiagnosticCatalog.ConstantEntryRule, $"{where}: the LENGTH OF operand shall not be a "
                + "dynamic-length item or a group containing a dynamic-capacity table — its length has no "
                + "compile-time maximum (ISO §13.10.3 SR12)");
            return null;
        }
        int width = item.ImageWidth;
        if (width <= 0)
        {
            // A TYPE-clause reference not yet expanded / a pending PICTURE-less usage — loud, never a wrong 0.
            Edition.Error(DiagnosticCatalog.ConstantEntryRule, $"{where}: the length of '{dref.GetText()}' is "
                + "not computable at this point in the data division (ISO §13.10.4 GR6 — the §15.50 LENGTH "
                + "value; a TYPE-expanded or usage-pending operand is a recorded residue)");
            return null;
        }
        string text = width.ToString(CultureInfo.InvariantCulture);
        return new ConstantDef(name, PicCategory.Numeric, text, true, isGlobal, text);

        static bool HasDynamicTable(DataItem item) =>
            item.IsDynamicTable || item.Children.Any(HasDynamicTable);
    }

    // ── The §7.3.6 compile-time arithmetic evaluator ─────────────────────────────────────────────────────────

    /// <summary>Evaluate a compile-time arithmetic expression (ISO §7.3.6, invoked from §13.10.4 GR4):
    /// operands are fixed-point numeric literals (§7.3.6.2 SR1b) or already-defined numeric constant-names
    /// (which substitute their literals, §13.10.3 SR2/GR1 — SR4/SR5 rule out circularity); exponentiation is
    /// rejected (SR1a); division by zero is rejected (SR1c). Intermediate results ride .NET
    /// <see cref="decimal"/> (28–29 significant digits, truncation on division) — the documented §7.3.6.2 SR2
    /// implementor definition. Null (already reported) on any violation.</summary>
    private decimal? EvalConstExpr(IParseTree node, string where)
    {
        switch (node)
        {
            case Core.ArithmeticExpressionContext a:
                return EvalConstExpr(a.GetChild(0), where);
            case Core.AdditiveExpressionContext or Core.MultiplicativeExpressionContext:
            {
                decimal? acc = null;
                char op = '+';
                for (int i = 0; i < node.ChildCount; i++)
                {
                    var c = node.GetChild(i);
                    if (c is Core.AddOpContext or Core.MulOpContext) { op = c.GetText()[0]; continue; }
                    if (EvalConstExpr(c, where) is not { } v) return null;
                    if (acc is null) { acc = v; continue; }
                    try
                    {
                        switch (op)
                        {
                            case '+': acc += v; break;
                            case '-': acc -= v; break;
                            case '*': acc *= v; break;
                            case '/':
                                if (v == 0m)
                                {
                                    Edition.Error(DiagnosticCatalog.ConstantEntryRule, $"{where}: the "
                                        + "compile-time arithmetic expression divides by zero — the expression "
                                        + "shall be specified in such a way that a division by zero cannot "
                                        + "occur (ISO §7.3.6.2 SR1c)");
                                    return null;
                                }
                                acc /= v;
                                break;
                        }
                    }
                    catch (OverflowException)
                    {
                        Edition.Error(DiagnosticCatalog.ConstantEntryRule, $"{where}: an intermediate result "
                            + "of the compile-time arithmetic expression exceeds the decimal128 evaluation "
                            + "range (the documented §7.3.6.2 SR2 implementor limit)");
                        return null;
                    }
                }
                return acc;
            }
            case Core.PowerExpressionContext p:
            {
                var bases = p.unaryExpression();
                if (bases.Length > 1)
                {
                    Edition.Error(DiagnosticCatalog.ConstantEntryRule, $"{where}: the exponentiation operator "
                        + "shall not be specified in a compile-time arithmetic expression (ISO §7.3.6.2 SR1a)");
                    return null;
                }
                return EvalConstExpr(bases[0], where);
            }
            case Core.UnaryExpressionContext u:
            {
                if (u.primaryExpression() is { } pr) return EvalConstExpr(pr, where);
                var inner = EvalConstExpr(u.unaryExpression(), where);
                return inner is null ? null : u.addOp().GetText() == "-" ? -inner : inner;
            }
            case Core.PrimaryExpressionContext pe:
            {
                if (pe.numericLiteral() is { } num) return ParseConstLiteral(num.GetText(), where);
                if (pe.ZERO_ARITH() is not null) return 0m;
                if (pe.arithmeticExpression() is { } paren) return EvalConstExpr(paren, where);
                if (pe.dataReference() is { } dref)
                {
                    // A constant-name operand substitutes its literal (§13.10.3 SR2 / §13.10.4 GR1) — only a
                    // NUMERIC one is a valid §7.3.6 operand.
                    if (ConstantOf(dref) is { Category: PicCategory.Numeric } k)
                        return ParseConstLiteral(k.Text, where);
                    Edition.Error(DiagnosticCatalog.ConstantEntryRule, $"{where}: '{dref.GetText()}' — all "
                        + "operands of the compile-time arithmetic expression shall be fixed-point numeric "
                        + "literals or previously defined numeric constant-names substituting them "
                        + "(ISO §13.10.3 SR7 / §7.3.6.2 SR1b)");
                    return null;
                }
                Edition.Error(DiagnosticCatalog.ConstantEntryRule, $"{where}: '{pe.GetText()}' is not a valid "
                    + "compile-time arithmetic operand — operands shall be fixed-point numeric literals "
                    + "(ISO §13.10.3 SR7 / §7.3.6.2 SR1b)");
                return null;
            }
            default:
                Edition.Error(DiagnosticCatalog.ConstantEntryRule, $"{where}: unsupported compile-time "
                    + "arithmetic expression shape (ISO §7.3.6)");
                return null;
        }
    }

    /// <summary>Parse one fixed-point numeric literal operand (§7.3.6.2 SR1b): dot-decimal after the
    /// DECIMAL-POINT IS COMMA normalization; a floating-point (E-form) literal is NOT fixed-point and
    /// rejects.</summary>
    private decimal? ParseConstLiteral(string text, string where)
    {
        string norm = NormalizeNumericLiteral(text);
        if (decimal.TryParse(norm, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture, out decimal v))
            return v;
        Edition.Error(DiagnosticCatalog.ConstantEntryRule, $"{where}: '{text}' — all operands of a "
            + "compile-time arithmetic expression shall be fixed-point numeric literals (ISO §7.3.6.2 SR1b)");
        return null;
    }

    /// <summary>The single (possibly signed) numeric literal an arithmetic expression consists of, or null —
    /// the §13.10.3 SR1 re-classification probe ("if the operand … consists of a single numeric literal, that
    /// operand is treated as a literal, not as an arithmetic-expression"). Walks the sole-child expression
    /// spine; unary minus toggles the sign.</summary>
    private static (string Text, bool Negative)? SoleNumericLiteral(Core.ArithmeticExpressionContext expr)
    {
        IParseTree n = expr;
        bool neg = false;
        while (true)
        {
            switch (n)
            {
                case Core.ArithmeticExpressionContext or Core.AdditiveExpressionContext
                    or Core.MultiplicativeExpressionContext or Core.PowerExpressionContext:
                    if (n.ChildCount != 1) return null;
                    n = n.GetChild(0);
                    continue;
                case Core.UnaryExpressionContext u:
                    if (u.primaryExpression() is { } pr) { n = pr; continue; }
                    if (u.addOp().GetText() == "-") neg = !neg;
                    n = u.unaryExpression();
                    continue;
                case Core.PrimaryExpressionContext pe:
                    return pe.numericLiteral() is { } num ? (num.GetText(), neg) : null;
                default:
                    return null;
            }
        }
    }

    // ── The data-division substitution chokepoints (§13.10.3 SR2) ────────────────────────────────────────────

    /// <summary>The value of one OCCURS fixed bound (the <c>occursBound</c> grammar alternative): integer-1/
    /// integer-2 as written, or an INTEGER constant-name substituting one (ISO §13.10.3 SR2 — "if
    /// constant-name-1 is an integer, it may also be used to specify … repetition"; §13.10.4 GR1/GR3). Null
    /// (reported) for a non-integer or unknown constant-name.</summary>
    private int? OccursBoundValue(Core.OccursBoundContext bound, string where)
    {
        if (bound.integerLiteral() is { } il)
            return int.TryParse(il.GetText(), out int n) ? n : null;
        string word = bound.cobolWord().GetText();
        if (_constants.TryGetValue(word, out var k))
        {
            if (k is { Category: PicCategory.Numeric, IsInteger: true } && int.TryParse(k.Text, out int kv))
                return kv;
            Edition.Error(DiagnosticCatalog.ConstantEntryRule, $"{where}: the OCCURS bound '{word}' shall be "
                + "an INTEGER constant-name (ISO §13.10.3 SR2 — only an integer constant may specify an OCCURS "
                + "integer position)");
            return null;
        }
        Edition.Error(DiagnosticCatalog.ConstantEntryRule, $"{where}: the OCCURS bound '{word}' is not a "
            + "defined constant-name — the OCCURS integer positions admit an integer literal or an integer "
            + "constant-name (ISO §13.18.38 / §13.10.3 SR2)");
        return null;
    }

    /// <summary>Expand integer constant-name repetition counts inside a PICTURE character-string (ISO
    /// §13.10.3 SR2 second sentence — "if constant-name-1 is an integer, it may also be used to specify
    /// repetition in a picture character-string, as specified in 13.18.40"): each parenthesized WORD (a
    /// repetition position is otherwise an unsigned integer, §13.18.40.2) is replaced by its constant's
    /// value. Called only when the unit defines constants, so a constant-free program's PICTURE pipeline is
    /// byte-identical to before.</summary>
    private string ExpandPicConstants(string pictureText, string where)
    {
        return System.Text.RegularExpressions.Regex.Replace(pictureText,
            @"\(\s*([A-Za-z][A-Za-z0-9-]*)\s*\)",
            m =>
            {
                string word = m.Groups[1].Value;
                if (_constants.TryGetValue(word, out var k))
                {
                    if (k is { Category: PicCategory.Numeric, IsInteger: true }) return "(" + k.Text + ")";
                    Edition.Error(DiagnosticCatalog.ConstantEntryRule, $"{where}: '{word}' in the PICTURE "
                        + "repetition position shall be an INTEGER constant-name (ISO §13.10.3 SR2)");
                    return "(1)";   // recovery shape — the compile has already failed
                }
                Edition.Error(DiagnosticCatalog.ConstantEntryRule, $"{where}: '{word}' in the PICTURE "
                    + "repetition position is not a defined constant-name (ISO §13.18.40.2 — a repetition "
                    + "count is an unsigned integer or an integer constant-name, §13.10.3 SR2)");
                return "(1)";
            });
    }

    /// <summary>The RAW literal text a VALUE-clause constant-name operand substitutes (§13.10.3 SR2 — a VALUE
    /// operand is a literal position; §13.10.4 GR1 — "as if [the] literal were written"), or null when the
    /// operand is not a bare constant-name reference. The raw-text form feeds the existing VALUE data path
    /// (ValidateValueCategory → FieldEmitter → ValueInitializer), which stores raw literal text and decodes at
    /// emit time — the ConcatFolder RawText precedent.</summary>
    private string? ConstantValueRawText(Core.ValueClauseOperandContext op)
    {
        // A word operand parses through the unaryExpression spine to a bare dataReference; walk the
        // sole-child chain (any operator or suffix makes the operand a non-constant shape).
        IParseTree? n = op.unaryExpression();
        while (n is not null and not Core.DataReferenceContext)
            n = n.ChildCount == 1 ? n.GetChild(0) : null;
        if (n is not Core.DataReferenceContext dref) return null;
        return ConstantOf(dref)?.RawText;
    }
}
