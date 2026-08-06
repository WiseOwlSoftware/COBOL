// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antlr4.Runtime.Tree;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Generated;
using Xunit;
using CnFrontend = CobolNet.Frontend.Frontend;

namespace CobolNet.Tests.Unit;

/// <summary>
/// PARSE-level pins for the INVOKE <c>BY CONTENT</c> operand group (ISO §14.9.23.2 — <c>arithmetic-expression-1 |
/// boolean-expression-1 | identifier-5 | literal-2</c>), fix-queue PB46.
/// <para>⛔ WHY THESE EXIST AS PARSE TESTS RATHER THAN OUTPUT ASSERTIONS. The boolean alternative is gated by
/// <c>{boolExprAhead()}?</c>, the SHARED condition predicate, whose scan runs to the statement's period — so in
/// <c>USING BY CONTENT N + 1 BY CONTENT B1 B-AND B2</c> the FIRST argument's decision already sees the SECOND
/// argument's <c>B-AND</c> and takes the boolean alternative. The compiler is correct anyway because the BINDER
/// reduces a B-operator-free <c>booleanExpression</c> back to its bare <c>valueOperand</c>. That makes the
/// output-level regression controls in <c>pb46_invoke_by_content_boolean</c> pass — and pass EQUALLY WELL if the
/// over-reach never happened at all, in which case they would be testing nothing
/// (feedback_green_gates_arent_evidence). These tests assert the over-reach IS real, which is what makes those
/// controls evidence.</para>
/// </summary>
public sealed class InvokeContentOperandChannelTests
{
    private static (CobolParserCore.CompilationUnitContext? Tree, DiagnosticBag Diags) Parse(string src)
    {
        string path = Path.Combine(Path.GetTempPath(), "cn_ivc_" + Guid.NewGuid().ToString("N")[..8] + ".cob");
        File.WriteAllText(path, src);
        try
        {
            var diags = new DiagnosticBag();
            var tree = new CnFrontend { DialectLevel = 2023 }.Parse(path, diags);
            return (tree, diags);
        }
        finally { try { File.Delete(path); } catch { /* best-effort */ } }
    }

    /// <summary>A minimal program whose PROCEDURE DIVISION is <paramref name="invoke"/>. The INVOKE need not
    /// BIND (no class is declared) — these assertions read the parse tree only.</summary>
    private static string Prog(string invoke) =>
        "IDENTIFICATION DIVISION.\n" +
        "PROGRAM-ID. IVCHAN.\n" +
        "DATA DIVISION.\n" +
        "WORKING-STORAGE SECTION.\n" +
        "01 O USAGE OBJECT REFERENCE.\n" +
        "01 N PIC S9(4) VALUE 5.\n" +
        "01 B1 PIC 1(4) USAGE BIT VALUE B\"1100\".\n" +
        "01 B2 PIC 1(4) USAGE BIT VALUE B\"1010\".\n" +
        "PROCEDURE DIVISION.\n" +
        "MAIN.\n    " + invoke + "\n    STOP RUN.\n";

    private static IEnumerable<T> Descendants<T>(IParseTree node) where T : class
    {
        for (int i = 0; i < node.ChildCount; i++)
        {
            var child = node.GetChild(i);
            if (child is T t) yield return t;
            foreach (var d in Descendants<T>(child)) yield return d;
        }
    }

    private static List<CobolParserCore.InvokeArgumentContext> ArgsOf(string invoke)
    {
        var r = Parse(Prog(invoke));
        Assert.NotNull(r.Tree);
        Assert.False(r.Diags.HasErrors, string.Join("\n", r.Diags.Diagnostics.Select(d => d.ToString())));
        return Descendants<CobolParserCore.InvokeArgumentContext>(r.Tree!).ToList();
    }

    /// <summary>The plain case: a genuine boolean expression takes the boolean alternative.</summary>
    [Fact]
    public void BooleanExpressionOperand_TakesTheBooleanAlternative()
    {
        var args = ArgsOf("INVOKE O \"M\" USING BY CONTENT B1 B-AND B2.");
        Assert.Single(args);
        Assert.NotNull(args[0].booleanExpression());
    }

    /// <summary>The predicate does NOT fire without a boolean operator: an arithmetic operand keeps its own
    /// node, so the ordinary path is untouched in the ordinary program.</summary>
    [Fact]
    public void ArithmeticOperand_Alone_DoesNotEnterTheBooleanNode()
    {
        var args = ArgsOf("INVOKE O \"M\" USING BY CONTENT N + 1.");
        Assert.Single(args);
        Assert.Null(args[0].booleanExpression());
        Assert.NotNull(args[0].arithmeticExpression());
    }

    /// <summary>⛔ THE OVER-REACH ITSELF, ASSERTED. A boolean argument LATER in the same statement pulls the
    /// FIRST argument into the boolean node, because <c>boolExprAhead()</c> scans to the period. This is the
    /// fact that makes the binder's <c>UnwrapBareBool</c> normalization load-bearing rather than defensive —
    /// and the fact that makes the golden's regression controls mean something.</summary>
    [Theory]
    [InlineData("BY CONTENT N + 1")]   // arithmetic-expression-1
    [InlineData("BY CONTENT N")]       // identifier-5
    [InlineData("BY CONTENT 42")]      // numeric literal-2
    [InlineData("BY CONTENT \"XY\"")]  // alphanumeric literal-2
    public void ALaterBooleanArgument_PullsTheEarlierOperandIntoTheBooleanNode(string firstArg)
    {
        var args = ArgsOf($"INVOKE O \"M\" USING {firstArg} BY CONTENT B1 B-AND B2.");
        Assert.Equal(2, args.Count);
        Assert.NotNull(args[0].booleanExpression());   // the over-reach — real, and harmless by construction
        Assert.NotNull(args[1].booleanExpression());
    }

    /// <summary>A boolean LITERAL is literal-2, not boolean-expression-1: with no boolean OPERATOR present the
    /// predicate is false and it parses through the literal alternative — which is why the binder routes
    /// <c>BOOLLIT</c> onto the boolean value channel from the LITERAL arm, not the expression one.</summary>
    [Fact]
    public void BooleanLiteralOperand_ParsesAsALiteral()
    {
        var args = ArgsOf("INVOKE O \"M\" USING BY CONTENT B\"1010\".");
        Assert.Single(args);
        Assert.Null(args[0].booleanExpression());
        Assert.NotNull(args[0].literal()?.nonNumericLiteral()?.BOOLLIT());
    }

    /// <summary>BY VALUE does NOT gain the boolean arm — the §14.9.23.2 format's BY VALUE branch is
    /// <c>arithmetic-expression-1 | identifier-5 | literal-2</c>, and the two phrases genuinely differ. The
    /// SAME operand text is asserted against BOTH phrases so the contrast is the assertion: under BY CONTENT it
    /// is one boolean-expression argument, under BY VALUE the boolean operator is not consumed at all and
    /// spills into further argument positions (which is what later surfaces as COBOLNET0901, <c>B-AND</c> in a
    /// name slot — the pre-existing shape, deliberately unchanged).</summary>
    [Fact]
    public void ByValue_HasNoBooleanAlternative_UnlikeByContent()
    {
        var byContent = ArgsOf("INVOKE O \"M\" USING BY CONTENT B1 B-AND B2.");
        Assert.Single(byContent);
        Assert.NotNull(byContent[0].booleanExpression());

        var byValue = ArgsOf("INVOKE O \"M\" USING BY VALUE B1 B-AND B2.");
        Assert.Null(byValue[0].booleanExpression());
        Assert.True(byValue.Count > 1,
            "BY VALUE must not consume the boolean operator — the operand group has no boolean alternative");
    }
}
