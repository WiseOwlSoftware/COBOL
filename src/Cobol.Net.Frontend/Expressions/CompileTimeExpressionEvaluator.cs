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
    private readonly Func<string, decimal?> _resolveNumericName;
    private readonly ICtDiagnostics _diag;
    private readonly CtOperandVocabulary _vocab;
    private readonly bool _decimalPointIsComma;

    /// <param name="resolveNumericName">A bare (unqualified, unsubscripted) name → its numeric value if it is a
    /// currently-defined NUMERIC constant/compilation-variable, else <see langword="null"/> (§7.3.6.2 SR1b — only
    /// a numeric name is a valid operand; a non-numeric or undefined name resolves to null and is rejected).</param>
    /// <param name="diag">The code-preserving diagnostic sink (§5.2 of the design).</param>
    /// <param name="vocab">Per-consumer operand wording + citation.</param>
    /// <param name="decimalPointIsComma">The active §12.3.7 GR14a mode (binder: the real SPECIAL-NAMES setting;
    /// frontend: false — a directive operand is processed before SPECIAL-NAMES is bound, so it is dot-decimal).</param>
    public CompileTimeExpressionEvaluator(
        Func<string, decimal?> resolveNumericName, ICtDiagnostics diag, CtOperandVocabulary vocab,
        bool decimalPointIsComma)
    {
        _resolveNumericName = resolveNumericName;
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
                        && _resolveNumericName(w.GetText()) is { } value)
                        return value;
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
}
