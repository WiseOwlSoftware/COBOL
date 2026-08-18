// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using Antlr4.Runtime.Tree;
using CobolNet.Common;
using CobolNet.Frontend.Generated;

namespace CobolNet.Frontend.Expressions;

using Core = CobolParserCore;

/// <summary>
/// The ONE compile-time expression evaluator (ISO/IEC 1989:2023 §7.3.6 arithmetic; §7.3.7/§8.8.2 boolean is added
/// with the frontend directive rewire). It walks a parsed <c>arithmeticExpression</c> and returns its value,
/// reachable by BOTH consumers of a compile-time expression (the singular-pattern requirement, ledger C2): the
/// frontend conditional-compilation stage (which fragment-parses a directive operand) and the CONSTANT-entry
/// binder (§13.10.4 GR4, which already has the parse tree). It reads NO consumer state — everything specific is
/// injected: how a name resolves to a numeric value, where diagnostics go (code-preserving), the per-consumer
/// operand wording/citation, and the DECIMAL-POINT IS COMMA mode.
///
/// The §7.3.11.4 GR5 reclassification (a single numeric literal stays a literal, keeping its fractional value) and
/// the §7.3.6.3 GR3 truncation (an arithmetic EXPRESSION's final result is truncated to its integer part) are
/// applied HERE, at the public <see cref="EvaluateArithmeticOperand"/> boundary — the raw-decimal recursion stays
/// private so intermediate results are correctly un-truncated (§7.3.6.3 GR1), and no consumer re-implements the
/// probe/truncate rule.
/// </summary>
public sealed class CompileTimeExpressionEvaluator
{
    private readonly Func<string, CtValue?> _resolveName;
    private readonly ICtDiagnostics _diag;
    private readonly CtOperandVocabulary _vocab;
    private readonly bool _decimalPointIsComma;

    /// <param name="resolveName">A bare (unqualified, unsubscripted) name → its bound <see cref="CtValue"/> if it is
    /// a currently-defined constant/compilation-variable, else <see langword="null"/>. An arithmetic operand uses
    /// only the NUMERIC case (§7.3.6.2 SR1b — a non-numeric or undefined name is rejected); a boolean operand uses
    /// the BOOLEAN case (§7.3.7 substitution); a defined-condition uses mere presence (§7.3.8.4.4).</param>
    /// <param name="diag">The code-preserving diagnostic sink (§5.2 of the design).</param>
    /// <param name="vocab">Per-consumer operand wording + citation.</param>
    /// <param name="decimalPointIsComma">The active §12.3.7 GR14a mode (binder: the real SPECIAL-NAMES setting;
    /// frontend: false — a directive operand is processed before SPECIAL-NAMES is bound, so it is dot-decimal).</param>
    public CompileTimeExpressionEvaluator(
        Func<string, CtValue?> resolveName, ICtDiagnostics diag, CtOperandVocabulary vocab,
        bool decimalPointIsComma)
    {
        _resolveName = resolveName;
        _diag = diag;
        _vocab = vocab;
        _decimalPointIsComma = decimalPointIsComma;
    }

    /// <summary>The final value of one compile-time arithmetic operand (§7.3.6). <paramref name="WasSingleLiteral"/>
    /// is true when the operand was a single numeric literal (§7.3.11.4 GR5 / §13.10.3 SR1 — treated as a literal,
    /// NOT truncated). <paramref name="Text"/> is the canonical value text (a single literal's normalized text,
    /// sign included; an expression's GR3-truncated integer) — the substitution form consumers store.</summary>
    public readonly record struct CtNumber(bool WasSingleLiteral, decimal Value, string Text);

    /// <summary>Evaluate one compile-time arithmetic operand (ISO §7.3.6), applying §7.3.11.4 GR5 (single-literal
    /// reclassification) and §7.3.6.3 GR3 (integer truncation of an expression's final result) at this boundary.
    /// <see langword="null"/> (already reported) on any §7.3.6.2 violation.</summary>
    public CtNumber? EvaluateArithmeticOperand(Core.ArithmeticExpressionContext expr, string where)
    {
        // §7.3.11.4 GR5 / §13.10.3 SR1 — a single (possibly signed) numeric literal is a LITERAL, not an
        // expression, so it keeps its value (AS 0.25 stays 0.25) and is NOT truncated. As a literal it may be of
        // ANY numeric class, so a floating-point (E-form) literal is valid here — unlike a §7.3.6.2 SR1b
        // arithmetic-EXPRESSION operand, which must be fixed-point (see ParseLiteral). Parse permissively
        // (AllowExponent); a literal whose magnitude exceeds the decimal evaluation range is rejected LOUDLY
        // (never a silent null — the boundary's "null means already reported" contract).
        if (SoleNumericLiteral(expr) is { } lit)
        {
            string text = CobolNet.Common.NumericLiteral.Normalize(lit.Text, _decimalPointIsComma, out var issue);
            ReportSeparator(issue, lit.Text);
            if (lit.Negative) text = "-" + text.TrimStart('+');
            if (decimal.TryParse(text,
                    NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent,
                    CultureInfo.InvariantCulture, out decimal v))
                return new CtNumber(true, v, text);
            _diag.Report(CtDiagCode.ArithmeticRule, $"{where}: the numeric literal '{lit.Text}' exceeds the .NET "
                + "decimal evaluation range (96-bit, 28–29 significant digits — the documented §7.3.6.2 SR2 "
                + "implementor limit)");
            return null;
        }
        // An arithmetic EXPRESSION: evaluate the raw value (intermediates un-truncated, §7.3.6.3 GR1), then
        // truncate the FINAL result to its integer part (§7.3.6.3 GR3 / INTEGER-PART §15.49).
        if (EvalArith(expr, where) is not { } result) return null;
        decimal truncated = decimal.Truncate(result);
        return new CtNumber(false, truncated, truncated.ToString(CultureInfo.InvariantCulture));
    }

    // ── The §7.3.6 raw-decimal recursion (lifted from the CONSTANT binder's battery-tested EvalConstExpr) ────────

    /// <summary>Evaluate a compile-time arithmetic expression to its raw (un-truncated) value (ISO §7.3.6):
    /// operands are fixed-point numeric literals (§7.3.6.2 SR1b) or previously-defined numeric names substituting
    /// them; exponentiation is rejected (SR1a); division by zero is rejected (SR1c); intermediates ride .NET
    /// <see cref="decimal"/> (96-bit, 28–29 significant digits — the documented §7.3.6.2 SR2 implementor choice).
    /// <see langword="null"/> (already reported) on any violation.</summary>
    private decimal? EvalArith(IParseTree node, string where)
    {
        switch (node)
        {
            case Core.ArithmeticExpressionContext a:
                return EvalArith(a.GetChild(0), where);
            case Core.AdditiveExpressionContext or Core.MultiplicativeExpressionContext:
            {
                decimal? acc = null;
                char op = '+';
                for (int i = 0; i < node.ChildCount; i++)
                {
                    var c = node.GetChild(i);
                    if (c is Core.AddOpContext or Core.MulOpContext) { op = c.GetText()[0]; continue; }
                    if (EvalArith(c, where) is not { } v) return null;
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
                                    _diag.Report(CtDiagCode.ArithmeticRule, $"{where}: the compile-time arithmetic "
                                        + "expression divides by zero — the expression shall be specified in such a "
                                        + "way that a division by zero cannot occur (ISO §7.3.6.2 SR1c)");
                                    return null;
                                }
                                acc /= v;
                                break;
                        }
                    }
                    catch (OverflowException)
                    {
                        _diag.Report(CtDiagCode.ArithmeticRule, $"{where}: an intermediate result of the compile-time "
                            + "arithmetic expression exceeds the .NET decimal evaluation range (96-bit, 28–29 "
                            + "significant digits — the documented §7.3.6.2 SR2 implementor limit)");
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
                    _diag.Report(CtDiagCode.ArithmeticRule, $"{where}: the exponentiation operator shall not be "
                        + "specified in a compile-time arithmetic expression (ISO §7.3.6.2 SR1a)");
                    return null;
                }
                return EvalArith(bases[0], where);
            }
            case Core.UnaryExpressionContext u:
            {
                if (u.primaryExpression() is { } pr) return EvalArith(pr, where);
                var inner = EvalArith(u.unaryExpression(), where);
                return inner is null ? null : u.addOp().GetText() == "-" ? -inner : inner;
            }
            case Core.PrimaryExpressionContext pe:
            {
                if (pe.numericLiteral() is { } num) return ParseLiteral(num.GetText(), where);
                if (pe.ZERO_ARITH() is not null) return 0m;
                if (pe.arithmeticExpression() is { } paren) return EvalArith(paren, where);
                if (pe.dataReference() is { } dref)
                {
                    // A name operand substitutes its literal (§7.3.6.2 SR1b) — only a BARE (unqualified,
                    // unsubscripted) NUMERIC constant/compilation-variable is a valid operand.
                    if (dref.dataReferenceSuffix().Length == 0 && dref.cobolWord() is { } w
                        && _resolveName(w.GetText()) is { Category: CtCategory.Numeric } cv)
                        return cv.Number;
                    _diag.Report(CtDiagCode.ArithmeticRule, $"{where}: '{dref.GetText()}' — all operands of the "
                        + $"compile-time arithmetic expression shall be fixed-point numeric literals or {_vocab.OperandSource} "
                        + $"({_vocab.GoverningCitation})");
                    return null;
                }
                _diag.Report(CtDiagCode.ArithmeticRule, $"{where}: '{pe.GetText()}' is not a valid compile-time "
                    + $"arithmetic operand — operands shall be fixed-point numeric literals ({_vocab.GoverningCitation})");
                return null;
            }
            default:
                _diag.Report(CtDiagCode.ArithmeticRule,
                    $"{where}: unsupported compile-time arithmetic expression shape (ISO §7.3.6)");
                return null;
        }
    }

    /// <summary>Parse one fixed-point numeric literal operand (§7.3.6.2 SR1b): dot-decimal after the §12.3.7 GR14a
    /// normalization; a floating-point (E-form) literal is NOT fixed-point and rejects.</summary>
    private decimal? ParseLiteral(string text, string where)
    {
        string norm = CobolNet.Common.NumericLiteral.Normalize(text, _decimalPointIsComma, out var issue);
        ReportSeparator(issue, text);
        if (decimal.TryParse(norm, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture, out decimal v))
            return v;
        _diag.Report(CtDiagCode.ArithmeticRule, $"{where}: '{text}' — all operands of a compile-time arithmetic "
            + "expression shall be fixed-point numeric literals (ISO §7.3.6.2 SR1b)");
        return null;
    }

    /// <summary>Report a §12.3.7 GR14a decimal-separator violation through the code-preserving sink (the consumer
    /// routes <see cref="CtDiagCode.NumericSeparator"/> to COBOLNET0895 / its own code). The message matches the
    /// binder's historical wording so its diagnostic is unchanged.</summary>
    private void ReportSeparator(NumericSeparatorIssue issue, string text)
    {
        switch (issue)
        {
            case NumericSeparatorIssue.DecimalPointUnderCommaMode:
                _diag.Report(CtDiagCode.NumericSeparator, $"numeric literal '{text}': under DECIMAL-POINT IS COMMA "
                    + "the decimal separator is the comma (ISO §12.3.7 GR14a); '.' is not valid in a numeric literal");
                break;
            case NumericSeparatorIssue.CommaWithoutCommaMode:
                _diag.Report(CtDiagCode.NumericSeparator, $"numeric literal '{text}': a comma decimal separator "
                    + "requires DECIMAL-POINT IS COMMA (ISO §12.3.7 GR14a; §8.3.3.3.2 admits only '.' as the decimal point)");
                break;
        }
    }

    /// <summary>The single (possibly signed) numeric literal an arithmetic expression consists of, or null — the
    /// §13.10.3 SR1 / §7.3.11.4 GR5 re-classification probe ("if the operand consists of a single numeric literal,
    /// that operand is treated as a literal, not as an arithmetic-expression"). Walks the sole-child expression
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

    // ══ Directive-context operand dispatch (§7.3.3 SR10 master constraint) ═══════════════════════════════════════

    /// <summary>Evaluate one compile-time operand (a <c>&gt;&gt;DEFINE</c> value / <c>&gt;&gt;EVALUATE</c> subject /
    /// <c>&gt;&gt;WHEN</c> object) to a category-tagged <see cref="CtValue"/> (ISO §7.3). Enforces the §7.3.3 SR10
    /// master constraint — no floating-point literal, no figurative constant, no concatenation expression in a
    /// compiler directive — which is why a directive numeric operand is NOT the same as a CONSTANT data-entry
    /// operand (§13.10.3 admits a floating-point literal; a directive does not). <see langword="null"/> (already
    /// reported) on any violation.</summary>
    public CtValue? EvaluateOperand(Core.CompileTimeOperandContext op, string where)
    {
        if (op.booleanExpression() is { } be)
            return EvaluateBoolean(be, where) is { } b ? CtValue.Boolean(b) : null;
        if (op.arithmeticExpression() is { } ae)
        {
            // A bare (unqualified, unsubscripted) NAME substitutes its stored value — of ANY category (numeric /
            // alphanumeric / national / boolean; §7.3.11.4 GR6-GR8). Only a genuine arithmetic EXPRESSION (or a
            // numeric literal) runs through the §7.3.6 numeric core.
            if (SoleDataRef(ae) is { } dref && dref.dataReferenceSuffix().Length == 0 && dref.cobolWord() is { } w)
            {
                if (_resolveName(w.GetText()) is { } cv) return cv;
                ReportDirective(where, $"'{w.GetText()}' is not a previously-defined compilation variable (ISO §7.3.11 / §13.10.3)");
                return null;
            }
            return EvaluateDirectiveArithmetic(ae, where) is { } n ? CtValue.Numeric(n.Value, n.Text) : null;
        }
        if (op.nonNumericLiteral() is { } nn)
            return NonNumericOperand(nn, where);
        ReportDirective(where, "an empty compile-time operand");
        return null;
    }

    /// <summary>A directive arithmetic operand: the §7.3.3 SR10 float/figurative bar applied BEFORE the shared
    /// §7.3.6 arithmetic core (which stays consumer-agnostic — the CONSTANT binder keeps float acceptance).</summary>
    private CtNumber? EvaluateDirectiveArithmetic(Core.ArithmeticExpressionContext expr, string where)
    {
        if (ContainsToken(expr, Core.FLOATLIT) || ContainsToken(expr, Core.COMMA_FLOATLIT))
        { ReportDirective(where, "a floating-point numeric literal shall not appear in a compiler directive (ISO §7.3.3 SR10)"); return null; }
        if (ContainsToken(expr, Core.ZERO_ARITH))
        { ReportDirective(where, "a figurative constant shall not appear in a compiler directive (ISO §7.3.3 SR10)"); return null; }
        return EvaluateArithmeticOperand(expr, where);
    }

    private CtValue? NonNumericOperand(Core.NonNumericLiteralContext nn, string where)
    {
        if (nn.concatenationExpression() is not null)
        { ReportDirective(where, "a concatenation expression shall not appear in a compiler directive (ISO §7.3.3 SR10)"); return null; }
        if (nn.figurativeConstant() is not null)
        { ReportDirective(where, "a figurative constant shall not appear in a compiler directive (ISO §7.3.3 SR10)"); return null; }
        if (nn.STRINGLIT() is { } s) return CtValue.Alphanumeric(CobolLiteral.Decode(s.GetText()));
        if (nn.HEXLIT() is { } h) return CtValue.Alphanumeric(CobolLiteral.Decode(h.GetText()));   // X"…" — category alphanumeric
        if (nn.NATLIT() is { } nat) return CtValue.National(CobolLiteral.Decode(nat.GetText()));
        if (nn.BOOLLIT() is { } bl) return CtValue.Boolean(BitString.Of(CobolLiteral.Decode(bl.GetText())));
        ReportDirective(where, $"'{nn.GetText()}' is not a supported compile-time literal operand");
        return null;
    }

    // ══ Boolean fold (§7.3.7 → §8.8.2) via the ONE shared BooleanExpressionResolver ═════════════════════════════

    /// <summary>Evaluate one compile-time boolean operand (ISO §7.3.7 → §8.8.2) to its bit string via the ONE
    /// shared <see cref="BooleanExpressionResolver"/> — the same precedence/grouping the runtime binder uses,
    /// including the context-inherited rule-7b shift precedence a context-free grammar cannot express. Compile-time
    /// operands are boolean LITERALS (§7.3.7.2 SR1) and previously-defined boolean compilation-variable
    /// substitutions; §7.3.3 SR10 bars the figurative constants (<c>ZERO</c> / <c>ALL "literal"</c>) that the
    /// runtime §8.8.2 admits. <see langword="null"/> (already reported) on any violation, propagated through the
    /// fold so an errored sub-expression never yields a value.</summary>
    public BitString? EvaluateBoolean(Core.BooleanExpressionContext expr, string where) =>
        BooleanExpressionResolver.Resolve<BitString?>(
            expr,
            leaf: vo => BooleanLeaf(vo, where),
            not: b => b?.Not(),
            binary: (l, op, r) => l is null || r is null ? null : BitString.Combine(l, op, r),
            shift: (b, suf) => BooleanShift(b, suf, where));

    /// <summary>Resolve a boolean-expression leaf operand (§8.8.2 operand list) to its bit string. §7.3.7.2 SR1
    /// admits ONLY a boolean literal (or a substituted previously-defined boolean compilation variable); §7.3.3
    /// SR10 bars a figurative constant / concatenation; a non-boolean literal is not a boolean operand.</summary>
    private BitString? BooleanLeaf(Core.ValueOperandContext vo, string where)
    {
        if (vo.nonNumericLiteral() is { } nn)
        {
            if (nn.BOOLLIT() is { } bl) return BitString.Of(CobolLiteral.Decode(bl.GetText()));
            if (nn.concatenationExpression() is not null)
            { ReportDirective(where, "a concatenation expression shall not appear in a compiler directive (ISO §7.3.3 SR10)"); return null; }
            if (nn.figurativeConstant() is not null)
            { ReportDirective(where, "a figurative constant shall not appear in a compile-time boolean expression (ISO §7.3.3 SR10 / §7.3.7.2 SR1 — boolean literals only)"); return null; }
            ReportDirective(where, $"'{nn.GetText()}' is not a boolean operand — a compile-time boolean expression admits boolean literals only (ISO §7.3.7.2 SR1 / §8.8.2)");
            return null;
        }
        // A bare data-name substitutes a previously-defined BOOLEAN compilation variable (§7.3.7 — its value is a
        // boolean literal). Any other arithmetic operand is not a boolean operand.
        if (vo.arithmeticExpression() is { } expr && SoleDataRef(expr) is { } dref
            && dref.dataReferenceSuffix().Length == 0 && dref.cobolWord() is { } w)
        {
            if (_resolveName(w.GetText()) is { Category: CtCategory.Boolean, Bits: { } b }) return b;
            ReportDirective(where, $"'{w.GetText()}' is not a previously-defined boolean compilation variable (ISO §7.3.7.2 SR1)");
            return null;
        }
        ReportDirective(where, $"'{vo.GetText()}' is not a valid compile-time boolean operand (ISO §7.3.7.2 SR1 / §8.8.2)");
        return null;
    }

    /// <summary>Apply one boolean shift suffix (<c>(B-SHIFT-L|R|LC|RC) integer</c>, §8.8.2 rule 8). The second
    /// operand is an INTEGER operand (rule 5) — evaluated through the arithmetic boundary (GR3-truncated) then
    /// required to be integral; a negative count is rejected (the spec defines only counts ≥ 1). Result length =
    /// the first operand's length.</summary>
    private BitString? BooleanShift(BitString? operand, Core.BooleanShiftSuffixContext suf, string where)
    {
        if (operand is null) return null;   // already reported
        bool circular = suf.B_SHIFT_LC() is not null || suf.B_SHIFT_RC() is not null;
        bool left = suf.B_SHIFT_L() is not null || suf.B_SHIFT_LC() is not null;
        if (EvaluateDirectiveArithmetic(suf.arithmeticExpression(), where) is not { } count) return null;
        if (count.Value != decimal.Truncate(count.Value))
        { ReportDirective(where, "the second operand of a boolean shift shall be an integer operand (ISO §8.8.2 rule 5)"); return null; }
        if (count.Value < 0)
        { ReportDirective(where, "a boolean shift count shall not be negative (ISO §8.8.2 rule 8)"); return null; }
        // Reduce the count to a small equivalent BEFORE the (long) cast so an astronomically large literal count
        // cannot overflow the cast: a LOGICAL shift by ≥ the length is all boolean zeros (cap at the length), and a
        // CIRCULAR shift is periodic in the length (mod). A zero-length operand shifts to itself.
        int n = operand.Length;
        long k = n == 0 ? 0
               : circular ? (long)(count.Value % n)
               : count.Value > n ? n : (long)count.Value;
        return operand.Shift(k, circular, left);
    }

    // ══ Constant-conditional-expression (§7.3.8) over the ANTLR tree ═════════════════════════════════════════════

    /// <summary>Evaluate a constant-conditional-expression (ISO §7.3.8) — true/false, or <see langword="null"/>
    /// when a formation rule is violated (already reported). Per §8.8.4.13 the VALUE may short-circuit, but a
    /// FORMATION error is reportable regardless of branch, so every AND/OR operand is evaluated; the frontend
    /// treats a null result as false for line selection.</summary>
    public bool? EvaluateCce(Core.ConstantConditionalExpressionContext cce, string where) => EvalCceOr(cce.cceOr(), where);

    private bool? EvalCceOr(Core.CceOrContext o, string where)
    {
        bool result = false, ok = true;
        foreach (var a in o.cceAnd())
        {
            var v = EvalCceAnd(a, where);
            if (v is null) ok = false; else result |= v.Value;
        }
        return ok ? result : (bool?)null;
    }

    private bool? EvalCceAnd(Core.CceAndContext a, string where)
    {
        bool result = true, ok = true;
        foreach (var n in a.cceNot())
        {
            var v = EvalCceNot(n, where);
            if (v is null) ok = false; else result &= v.Value;
        }
        return ok ? result : (bool?)null;
    }

    private bool? EvalCceNot(Core.CceNotContext n, string where) =>
        n.NOT() is not null
            ? EvalCceNot(n.cceNot(), where) is { } inner ? !inner : (bool?)null
            : EvalCcePrimary(n.ccePrimary(), where);

    private bool? EvalCcePrimary(Core.CcePrimaryContext p, string where)
    {
        if (p.constantConditionalExpression() is { } inner) return EvaluateCce(inner, where);   // ( … )
        if (p.definedCondition() is { } d) return EvalDefined(d);
        return EvalRelationOrBoolean(p.cceRelationOrBoolean(), where);
    }

    /// <summary>A defined-condition (§7.3.8.4.4): <c>name IS [NOT] DEFINED</c> — true iff the compilation variable
    /// is currently defined (a name in scope resolves to a non-null value), negated by NOT.</summary>
    private bool EvalDefined(Core.DefinedConditionContext d)
    {
        bool defined = _resolveName(d.cobolWord().GetText()) is not null;
        return d.NOT() is not null ? !defined : defined;
    }

    private bool? EvalRelationOrBoolean(Core.CceRelationOrBooleanContext r, string where)
    {
        // The bare simple-boolean-condition alt (§8.8.4.3) — a genuine boolean expression used as a condition.
        if (r.booleanExpression() is { } be) return SimpleBoolean(EvaluateBoolean(be, where), where);
        var operands = r.compileTimeOperand();
        var left = EvaluateOperand(operands[0], where);
        if (r.comparisonOperator() is not { } opCtx)
        {
            // A bare operand as a cce primary — only a BOOLEAN operand (a length-1 boolean literal) is a valid
            // simple boolean condition (§8.8.4.3); any other bare operand is not a condition.
            if (left is null) return null;
            if (left.Category == CtCategory.Boolean) return SimpleBoolean(left.Bits, where);
            ReportDirective(where, $"'{operands[0].GetText()}' is not a valid constant-conditional-expression (ISO §7.3.8)");
            return null;
        }
        var right = operands.Length > 1 ? EvaluateOperand(operands[1], where) : null;
        if (left is null || right is null) return null;
        return CceRelation(left, right, opCtx, r.NOT() is not null, where);
    }

    /// <summary>A simple boolean condition (§8.8.4.3): SR1 — the value shall be of length 1; GR1 — true iff the
    /// bit is 1.</summary>
    private bool? SimpleBoolean(BitString? bits, string where)
    {
        if (bits is null) return null;
        if (bits.Length != 1)
        { ReportDirective(where, "a simple boolean condition shall reference a boolean value of length 1 (ISO §8.8.4.3 SR1)"); return null; }
        return bits.IsTrue;
    }

    /// <summary>A constant-conditional relation (§7.3.8.2 SR1a / §7.3.8.3 GR2): SR1a.1 both operands same category;
    /// SR1a.2 non-numeric operands admit only equal/unequal; numeric compared by value; non-numeric compared by
    /// binary character/bit value, LENGTH-SENSITIVE (unequal length ⇒ unequal, no collating). The relation-level
    /// <c>NOT</c> negates the result.</summary>
    private bool? CceRelation(CtValue left, CtValue right, Core.ComparisonOperatorContext opCtx, bool negate, string where)
    {
        if (left.Category != right.Category)
        { ReportDirective(where, $"the operands of a constant-conditional relation shall be of the same category ('{left.Category}' vs '{right.Category}', ISO §7.3.8.2 SR1a.1)"); return null; }
        string op = MapOperator(opCtx.GetText());
        if (negate) op = NegateOp(op);
        if (left.Category == CtCategory.Numeric)
        {
            int cmp = decimal.Compare(left.Number, right.Number);
            return op switch { "==" => cmp == 0, "!=" => cmp != 0, "<" => cmp < 0, ">" => cmp > 0, "<=" => cmp <= 0, ">=" => cmp >= 0, _ => false };
        }
        if (op is not ("==" or "!="))
        { ReportDirective(where, "a non-numeric constant-conditional relation admits only IS EQUAL / IS NOT EQUAL (ISO §7.3.8.2 SR1a.2)"); return null; }
        // §7.3.8.3 GR2 — Alphanumeric/National binary + LENGTH-sensitive; §8.8.4.2.8 — Boolean right-zero-extends
        // the shorter operand (GR2's length-sensitivity is for operands "not numeric or boolean").
        bool eq = left.RelationalEquals(right);
        return op == "==" ? eq : !eq;
    }

    // ── shared small helpers ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Report a compiler-directive expression formation violation through the code-preserving sink (the
    /// frontend routes <see cref="CtDiagCode.DirectiveRule"/> to COBOLNET1619).</summary>
    private void ReportDirective(string where, string message) => _diag.Report(CtDiagCode.DirectiveRule, $"{where}: {message}");

    /// <summary>The sole (unqualified, unsubscripted) data reference an arithmetic expression consists of, or null
    /// — walking the single-child expression spine to the primary.</summary>
    private static Core.DataReferenceContext? SoleDataRef(Core.ArithmeticExpressionContext expr)
    {
        IParseTree n = expr;
        while (n is not Core.PrimaryExpressionContext)
        {
            if (n.ChildCount != 1) return null;
            n = n.GetChild(0);
        }
        return ((Core.PrimaryExpressionContext)n).dataReference();
    }

    /// <summary>True when the subtree contains a terminal of the given token <paramref name="type"/> — the §7.3.3
    /// SR10 float/figurative scan over a directive arithmetic operand.</summary>
    private static bool ContainsToken(IParseTree t, int type)
    {
        if (t is ITerminalNode term) return term.Symbol.Type == type;
        for (int i = 0; i < t.ChildCount; i++) if (ContainsToken(t.GetChild(i), type)) return true;
        return false;
    }

    /// <summary>Normalize a <c>comparisonOperator</c>'s concatenated text to <c>==</c>/<c>!=</c>/<c>&lt;</c>/<c>&gt;</c>/
    /// <c>&lt;=</c>/<c>&gt;=</c> (the §8.8.4.2 relational-operator set). Mirrors the runtime binder's mapping — a small
    /// CFG-independent pure function duplicated across the Frontend↔Compiler layer boundary.</summary>
    private static string MapOperator(string raw)
    {
        string t = raw.ToUpperInvariant().Replace("IS", "").Replace("THAN", "").Replace("TO", "");
        if (t.Contains("<>")) return "!=";
        bool not = t.Contains("NOT");
        bool orEqual = t.Contains(">=") || t.Contains("<=") || t.Contains("OREQUAL");
        string baseOp =
            t.Contains('>') || t.Contains("GREATER") ? (orEqual ? ">=" : ">")
            : t.Contains('<') || t.Contains("LESS") ? (orEqual ? "<=" : "<")
            : "==";
        return not ? NegateOp(baseOp) : baseOp;
    }

    private static string NegateOp(string op) => op switch
    { ">" => "<=", ">=" => "<", "<" => ">=", "<=" => ">", "==" => "!=", _ => "==" };
}
