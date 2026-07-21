// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Editions;
using CobolNet.Frontend.Expressions;
using CobolNet.Frontend.Generated;
using CobolNet.Frontend.Parsing;
using Xunit;

namespace CobolNet.Tests.Unit;

using Core = CobolParserCore;

/// <summary>
/// The shared <see cref="CompileTimeExpressionEvaluator"/> arithmetic core (ISO/IEC 1989:2023 §7.3.6): the
/// §7.3.11.4 GR5 single-literal reclassification (a lone numeric literal keeps its fractional value), the §7.3.6.3
/// GR3 integer truncation of an expression's final result, numeric-name substitution (§7.3.6.2 SR1b), and the
/// SR1a/SR1c rejections — driven through the evaluator with stub name-resolution + a collecting diagnostic sink.
/// </summary>
public sealed class CompileTimeExpressionEvaluatorTests
{
    private sealed class CollectingDiag : ICtDiagnostics
    {
        public readonly List<(CtDiagCode Code, string Message)> Reports = [];
        public void Report(CtDiagCode code, string message) => Reports.Add((code, message));
    }

    private static readonly CtOperandVocabulary Vocab =
        new("previously defined numeric compilation variables", "ISO §7.3.6.2 SR1b");

    /// <summary>Evaluate <paramref name="text"/> as an arithmetic operand; <paramref name="names"/> supplies any
    /// numeric constant-name values.</summary>
    private static (CompileTimeExpressionEvaluator.CtNumber? Result, CollectingDiag Diag) Eval(
        string text, Dictionary<string, decimal>? names = null)
    {
        var flag = new ErrorFlag();
        var lexer = new CobolLexer(new AntlrInputStream(text));
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(flag);
        var tokens = new CommonTokenStream(lexer);
        ZeroTokenRewriter.Rewrite(tokens);
        var parser = new Core(tokens) { Edition = EditionInfo.Of(2023) };
        parser.RemoveErrorListeners();
        parser.AddErrorListener(flag);
        Core.ArithmeticExpressionContext ctx = parser.arithmeticExpression();
        Assert.False(flag.HasError, $"parse error in arithmetic fragment '{text}'");
        Assert.Equal(TokenConstants.EOF, parser.CurrentToken.Type);

        var diag = new CollectingDiag();
        var ev = new CompileTimeExpressionEvaluator(
            resolveNumericName: w => names is not null && names.TryGetValue(w, out var v) ? v : null,
            diag: diag, vocab: Vocab, decimalPointIsComma: false);
        return (ev.EvaluateArithmeticOperand(ctx, "test"), diag);
    }

    [Theory]
    // Expression forms: §8.8.1 precedence, then §7.3.6.3 GR3 integer truncation of the final result.
    [InlineData("2 + 3 * 4", "14", false)]
    [InlineData("(2 + 3) * 4 - 6 / 2", "17", false)]
    [InlineData("7 / 2", "3", false)]              // 3.5 → truncated to 3 (GR3)
    [InlineData("- (3 + 4)", "-7", false)]
    // §7.3.11.4 GR5 / §13.10.3 SR1 — a single numeric literal is a literal, NOT truncated.
    [InlineData("0.25", "0.25", true)]
    [InlineData("-5", "-5", true)]
    [InlineData("42", "42", true)]
    public void Evaluates_ArithmeticAndReclassification(string src, string expectedText, bool wasSingleLiteral)
    {
        var (r, diag) = Eval(src);
        Assert.Empty(diag.Reports);
        Assert.NotNull(r);
        Assert.Equal(expectedText, r!.Value.Text);
        Assert.Equal(wasSingleLiteral, r.Value.WasSingleLiteral);
    }

    [Fact] // §7.3.11.4 GR5 — a SINGLE floating-point (E-form) literal is a valid literal constant (a literal keeps
           // its own class; only an arithmetic-EXPRESSION operand is bound to fixed-point by §7.3.6.2 SR1b).
    public void Accepts_SoleFloatingPointLiteral()
    {
        var (r, diag) = Eval("1.5E3");
        Assert.Empty(diag.Reports);
        Assert.NotNull(r);
        Assert.True(r!.Value.WasSingleLiteral);
        Assert.Equal("1.5E3", r.Value.Text);
        Assert.Equal(1500m, r.Value.Value);
    }

    [Fact] // §7.3.6.2 SR2 — a sole literal beyond the decimal evaluation range is rejected LOUDLY, never a silent null.
    public void Rejects_OverRangeSoleLiteral()
    {
        var (r, diag) = Eval("123456789012345678901234567890123456");   // 36 digits — beyond .NET decimal
        Assert.Null(r);
        Assert.Contains(diag.Reports, x => x.Code == CtDiagCode.ArithmeticRule && x.Message.Contains("SR2"));
    }

    [Fact] // A previously-defined numeric constant-name substitutes its value (§7.3.6.2 SR1b / §13.10.3 SR2).
    public void Substitutes_NumericName()
    {
        var (r, diag) = Eval("K * 2 + 1", new() { ["K"] = 5m });
        Assert.Empty(diag.Reports);
        Assert.Equal("11", r!.Value.Text);
    }

    [Fact] // §7.3.6.2 SR1c — division by zero is rejected.
    public void Rejects_DivisionByZero()
    {
        var (r, diag) = Eval("5 / 0");
        Assert.Null(r);
        Assert.Contains(diag.Reports, x => x.Code == CtDiagCode.ArithmeticRule && x.Message.Contains("SR1c"));
    }

    [Fact] // §7.3.6.2 SR1a — the exponentiation operator is rejected.
    public void Rejects_Exponentiation()
    {
        var (r, diag) = Eval("2 ** 3");
        Assert.Null(r);
        Assert.Contains(diag.Reports, x => x.Code == CtDiagCode.ArithmeticRule && x.Message.Contains("SR1a"));
    }

    [Fact] // §7.3.6.2 SR1b — an undefined / non-numeric name is not a valid operand.
    public void Rejects_UndefinedName()
    {
        var (r, diag) = Eval("UNDEF + 1");
        Assert.Null(r);
        Assert.Contains(diag.Reports, x => x.Code == CtDiagCode.ArithmeticRule && x.Message.Contains("SR1b"));
    }

    private sealed class ErrorFlag : BaseErrorListener, IAntlrErrorListener<int>
    {
        public bool HasError { get; private set; }
        public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol,
            int line, int charPositionInLine, string msg, RecognitionException e) => HasError = true;
        public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol,
            int line, int charPositionInLine, string msg, RecognitionException e) => HasError = true;
    }
}
