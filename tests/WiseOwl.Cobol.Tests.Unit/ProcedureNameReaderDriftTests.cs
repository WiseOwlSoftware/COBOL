// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A <c>procedureName</c> PARSE NODE IS READ BY <c>ProcedureNameShape</c> AND NOWHERE ELSE (kb/Work PB4238, PB4277).
/// The grammar's <c>procedureName</c> is <c>word ((OF | IN) word)?</c> (ISO §8.4.2.2.2 format 4 qualification), so
/// <c>GetText()</c> of the node glues the words — <c>P1 OF S1</c> reads <c>P1OFS1</c>. PB4238 gave the node one reader
/// and said so in its doc; three consumers still called <c>GetText()</c> (the declaratives-boundary message, the
/// RESUME AT message, and the header message), so the claim was false and two diagnostics quoted a name no source
/// element wrote. This test makes the claim true: a syntax scan of every compiler source refuses
/// <c>GetText()</c> on a value that is a <c>ProcedureNameContext</c> — a parameter, local, foreach variable or pattern
/// variable typed so or bound from <c>.procedureName(...)</c>, an element of one, or the accessor call itself —
/// anywhere but <c>ProcedureNameShape.cs</c>. The name in a message is <c>ProcedureNameShape.AsWritten</c>.
/// </summary>
[Trait("Sweep", "tree")]
public sealed class ProcedureNameReaderDriftTests
{
    private const string TheReader = "Binding/Procedure/ProcedureNameShape.cs";
    private const string NodeType = "ProcedureNameContext";
    private const string Accessor = "procedureName";

    /// <summary>The lines of <paramref name="source"/> that call <c>GetText()</c> on a procedure-name node.</summary>
    internal static IReadOnlyList<int> GetTextReads(string source)
    {
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();

        // The names this file binds to a procedure-name node (or an array of them).
        var nodeNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in root.DescendantNodes())
            switch (node)
            {
                case ParameterSyntax { Type: { } t } p when NamesNodeType(t): nodeNames.Add(p.Identifier.ValueText); break;
                case VariableDeclarationSyntax { Type: var t } d when NamesNodeType(t):
                    foreach (var v in d.Variables) nodeNames.Add(v.Identifier.ValueText);
                    break;
                case VariableDeclaratorSyntax { Initializer.Value: { } init } v when ReadsAccessor(init):
                    nodeNames.Add(v.Identifier.ValueText); break;
                case ForEachStatementSyntax f when NamesNodeType(f.Type) || ReadsAccessor(f.Expression):
                    nodeNames.Add(f.Identifier.ValueText); break;
                case SingleVariableDesignationSyntax s when s.Parent is DeclarationPatternSyntax dp && NamesNodeType(dp.Type):
                    nodeNames.Add(s.Identifier.ValueText); break;
                case IsPatternExpressionSyntax ip when ReadsAccessor(ip.Expression):
                    foreach (var d in ip.Pattern.DescendantNodesAndSelf().OfType<SingleVariableDesignationSyntax>())
                        nodeNames.Add(d.Identifier.ValueText);
                    break;
            }

        bool IsNode(ExpressionSyntax e) => e switch
        {
            ParenthesizedExpressionSyntax p => IsNode(p.Expression),
            PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.SuppressNullableWarningExpression } b => IsNode(b.Operand),
            ElementAccessExpressionSyntax el => IsNode(el.Expression),
            ConditionalAccessExpressionSyntax ca => IsNode(ca.Expression),
            IdentifierNameSyntax id => nodeNames.Contains(id.Identifier.ValueText),
            _ => ReadsAccessor(e),
        };

        var lines = new List<int>();
        foreach (var call in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            ExpressionSyntax? receiver = call.Expression switch
            {
                MemberAccessExpressionSyntax { Name.Identifier.ValueText: "GetText" } m => m.Expression,
                MemberBindingExpressionSyntax { Name.Identifier.ValueText: "GetText" } when call.Parent is ConditionalAccessExpressionSyntax c => c.Expression,
                _ => null,
            };
            if (receiver is not null && IsNode(receiver))
                lines.Add(call.GetLocation().GetLineSpan().StartLinePosition.Line + 1);
        }
        return lines;
    }

    private static bool NamesNodeType(TypeSyntax t) => t switch
    {
        ArrayTypeSyntax a => NamesNodeType(a.ElementType),
        NullableTypeSyntax n => NamesNodeType(n.ElementType),
        QualifiedNameSyntax q => NamesNodeType(q.Right),
        IdentifierNameSyntax id => id.Identifier.ValueText == NodeType,
        _ => false,
    };

    /// <summary>Whether <paramref name="e"/> is a call of the generated <c>procedureName(...)</c> accessor, possibly
    /// indexed, null-forgiven or parenthesised.</summary>
    private static bool ReadsAccessor(ExpressionSyntax e) => e switch
    {
        ParenthesizedExpressionSyntax p => ReadsAccessor(p.Expression),
        PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.SuppressNullableWarningExpression } b => ReadsAccessor(b.Operand),
        ElementAccessExpressionSyntax el => ReadsAccessor(el.Expression),
        InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax m } => m.Name.Identifier.ValueText == Accessor,
        InvocationExpressionSyntax { Expression: MemberBindingExpressionSyntax b } => b.Name.Identifier.ValueText == Accessor,
        ConditionalAccessExpressionSyntax ca => ReadsAccessor(ca.WhenNotNull),
        _ => false,
    };

    [Fact]
    public void NoCompilerSourceReadsAProcedureNameNodeByGetText()
    {
        string root = TestRepo.Src("WiseOwl.Cobol.Compiler");
        var found = new List<string>();
        int scanned = 0;
        foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(root, file).Replace('\\', '/');
            if (rel.StartsWith("obj/", StringComparison.Ordinal) || rel.StartsWith("bin/", StringComparison.Ordinal)
                || rel == TheReader) continue;
            scanned++;
            foreach (int line in GetTextReads(File.ReadAllText(file))) found.Add($"{rel}:{line}");
        }
        Assert.True(scanned > 100, "the scan must see the compiler's sources");
        Assert.True(found.Count == 0,
            "GetText() on a procedureName node glues a qualified name into one word (P1 OF S1 -> P1OFS1): "
            + string.Join(", ", found) + ". Read the node through ProcedureNameShape (Head, Qualifier, AsWritten) — "
            + "the one reader (kb/Work PB4238, PB4277).");
    }

    /// <summary>A planted source shows the scan sees each way a procedure-name node is held and ignores the rest.</summary>
    [Fact]
    public void TheScanSeesEveryFormOfRead()
    {
        const string planted = """
            class C
            {
                void A(Core.ProcedureNameContext pn) { var s = pn.GetText(); }
                void B(Core.ProcedureNameContext[] names) { var s = names[0].GetText(); }
                void C2(X x) { var s = x.procedureName(0).GetText(); }
                void D(X x) { var s = x.paragraphName().procedureName().GetText(); }
                void E(X x) { var pn = x.procedureName()!; var s = pn.GetText(); }
                void F(X x) { if (x.procedureName() is not { Length: >= 2 } names) return; var s = names[1].GetText(); }
                void G(X x) { foreach (var n in x.procedureName()) { var s = n.GetText(); } }
                void H(object o) { if (o is Core.ProcedureNameContext p) { var s = p.GetText(); } }
                void I(Core.ProcedureNameContext? pn) { var s = pn?.GetText(); }
                void J(X x) { var s = x.procedureName()?.GetText(); }
            }
            """;
        Assert.Equal(Enumerable.Range(3, 10).ToArray(), GetTextReads(planted).ToArray());

        const string innocent = """
            class C
            {
                // pn.GetText() in a comment, and "pn.GetText()" in a string, are not reads.
                void A(Core.CobolWordContext w, X x) { var s = w.GetText(); var t = x.dataReference().GetText(); }
                void B(Core.ProcedureNameContext pn) { var s = ProcedureNameShape.AsWritten(pn); var h = ProcedureNameShape.Head(pn); }
            }
            """;
        Assert.Empty(GetTextReads(innocent));
    }
}
