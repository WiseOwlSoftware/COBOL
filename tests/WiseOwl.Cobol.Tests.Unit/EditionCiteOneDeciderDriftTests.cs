// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>⛔ ONE DECIDER OF A MESSAGE'S WORDS BY EDITION (kb/Work PB4950 review rounds 1 and 2; docs/VERSION_TEST_MATRIX_DESIGN.md
/// P2.3a). <c>EditionInfo.Cite(y1985, y2002, y2023, y2014)</c> is the one choice of a citation, or of the words a message quotes, by
/// edition. A site that spells its own choice is a second decider the next edition's text has to be threaded through by hand.
/// This test reads the SYNTAX TREE of every <c>src/**/*.cs</c> (the decider <c>EditionInfo.cs</c> and the generated parser
/// excepted) and refuses each of the three shapes a choice takes, wherever its condition reads the edition and it chooses between
/// strings (a literal, an interpolated string, or a concatenation holding one) — two string arms at least; a rule gated by edition
/// that answers a message or null is a verdict, not a choice of words:
/// <list type="bullet">
/// <item>a conditional expression (<c>c ? a : b</c>, on one line or many);</item>
/// <item>a switch expression whose governing expression or an arm's <c>when</c> clause reads the edition;</item>
/// <item>an <c>if</c> with an <c>else</c> whose two branches each return or assign a string.</item>
/// </list>
/// "Reads the edition" is derived, never hand-listed: a member of <see cref="EditionInfo"/> that answers WHICH edition or a number
/// of it (each public instance <c>int</c> property: <c>Year</c>, <c>MaxDigits</c>, ...; each <c>bool</c> method of an <c>int</c>:
/// <c>Has</c>), the compiler's mirror <c>DialectLevel</c>, the CLI's <c>Std</c>, and a comparison or pattern of a name that says
/// edition, level or year against an edition of <see cref="EditionInfo.All"/>, wherever it stands in the condition (beside a
/// question about the program too: <c>x &amp;&amp; Year &lt; 2002 ? a : b</c> is written <c>x ? Cite(a, b, b) : b</c>).
/// <see cref="EditionInfo"/>'s <c>bool</c> properties are not among them: each is a named rule fact (<c>SubscriptedClauseOperands</c>,
/// <c>CopyNamesAreUserDefinedWords</c>) whose answer is a verdict about the program, not a choice of words.</summary>
public sealed class EditionCiteOneDeciderDriftTests
{
    /// <summary>The member names whose read is an edition question.</summary>
    private static readonly HashSet<string> EditionMembers = BuildEditionMembers();

    /// <summary>The editions as the source compares them.</summary>
    private static readonly HashSet<int> EditionValues = [.. EditionInfo.All.ToArray()];

    private static HashSet<string> BuildEditionMembers()
    {
        var t = typeof(EditionInfo);
        var names = t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(int))
            .Select(p => p.Name)
            .Concat(t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => m.ReturnType == typeof(bool) && m.GetParameters() is [{ ParameterType: var pt }] && pt == typeof(int))
                .Select(m => m.Name))
            .Where(n => n != nameof(EditionInfo.Permissive))
            .ToHashSet(StringComparer.Ordinal);
        names.Add("DialectLevel");   // EditionContext's mirror of EditionInfo.Year
        names.Add("Std");            // the CLI's edition option
        return names;
    }

    /// <summary>Every ad-hoc edition choice of a string in <paramref name="source"/>, as "line: kind".</summary>
    public static IReadOnlyList<string> Find(string source)
    {
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();
        var found = new List<string>();
        void Report(SyntaxNode node, string kind) =>
            found.Add($"{node.GetLocation().GetLineSpan().StartLinePosition.Line + 1}: {kind}");
        foreach (var node in root.DescendantNodes())
        {
            switch (node)
            {
                case ConditionalExpressionSyntax c when ReadsEdition(c.Condition) && IsString(c.WhenTrue) && IsString(c.WhenFalse):
                    Report(c, "conditional expression");
                    break;
                case SwitchExpressionSyntax s when (ReadsEdition(s.GoverningExpression)
                                                    || (NamesEdition(s.GoverningExpression)
                                                        && s.Arms.Any(a => a.Pattern.DescendantNodesAndSelf()
                                                            .OfType<LiteralExpressionSyntax>().Any(IsEditionValue)))
                                                    || s.Arms.Any(a => a.WhenClause is { } w && ReadsEdition(w.Condition)))
                                                   && s.Arms.Count(a => IsString(a.Expression)) >= 2:
                    Report(s, "switch expression");
                    break;
                case IfStatementSyntax i when i.Else is { } e && ReadsEdition(i.Condition)
                                              && YieldsString(i.Statement) && YieldsString(e.Statement):
                    Report(i, "if/else");
                    break;
            }
        }
        return found;
    }

    private static bool ReadsEdition(SyntaxNode condition) => condition.DescendantNodesAndSelf().Any(n => n switch
    {
        IdentifierNameSyntax id => EditionMembers.Contains(id.Identifier.ValueText),
        BinaryExpressionSyntax b => (IsEditionValue(b.Right) && NamesEdition(b.Left)) || (IsEditionValue(b.Left) && NamesEdition(b.Right)),
        IsPatternExpressionSyntax p => NamesEdition(p.Expression)
            && p.Pattern.DescendantNodesAndSelf().OfType<LiteralExpressionSyntax>().Any(IsEditionValue),
        _ => false,
    });

    private static bool IsEditionValue(ExpressionSyntax e) =>
        e is LiteralExpressionSyntax { Token.Value: int v } && EditionValues.Contains(v);

    /// <summary>A name that says it holds an edition: <c>edition</c>, <c>dialectLevel</c>, <c>year</c> and the like.</summary>
    private static bool NamesEdition(ExpressionSyntax e)
    {
        string name = e switch
        {
            IdentifierNameSyntax id => id.Identifier.ValueText,
            MemberAccessExpressionSyntax m => m.Name.Identifier.ValueText,
            _ => "",
        };
        return name.Contains("edition", StringComparison.OrdinalIgnoreCase) || name.Contains("level", StringComparison.OrdinalIgnoreCase)
            || name.Contains("year", StringComparison.OrdinalIgnoreCase) || EditionMembers.Contains(name);
    }

    private static bool IsString(ExpressionSyntax e) => e switch
    {
        LiteralExpressionSyntax l => l.IsKind(SyntaxKind.StringLiteralExpression),
        InterpolatedStringExpressionSyntax => true,
        ParenthesizedExpressionSyntax p => IsString(p.Expression),
        BinaryExpressionSyntax b when b.IsKind(SyntaxKind.AddExpression) => IsString(b.Left) || IsString(b.Right),
        ConditionalExpressionSyntax c => IsString(c.WhenTrue) || IsString(c.WhenFalse),
        _ => false,
    };

    private static bool YieldsString(StatementSyntax s) => s switch
    {
        BlockSyntax { Statements: [var only] } => YieldsString(only),
        ReturnStatementSyntax { Expression: { } r } => IsString(r),
        ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax a } => IsString(a.Right),
        _ => false,
    };

    [Fact]
    public void NoSourceChoosesAStringByEditionOutsideCite()
    {
        string src = TestRepo.Src();
        string decider = Path.Combine("WiseOwl.Cobol.Editions", "EditionInfo.cs");
        var offences = Directory.EnumerateFiles(src, "*.cs", SearchOption.AllDirectories)
            .Select(f => (Full: f, Rel: Path.GetRelativePath(src, f)))
            .Where(f => !f.Rel.Split(Path.DirectorySeparatorChar).Any(seg => seg is "bin" or "obj" or "Generated"))
            .Where(f => f.Rel != decider && !f.Rel.EndsWith(".g.cs", StringComparison.Ordinal))
            .SelectMany(f => Find(File.ReadAllText(f.Full)).Select(hit => $"{f.Rel.Replace('\\', '/')}:{hit}"))
            .Order(StringComparer.Ordinal)
            .ToList();
        Assert.True(offences.Count == 0,
            "a site chooses a string by edition with its own conditional, switch or if/else; write it as "
            + "EditionInfo.Cite(y1985, y2002, y2023[, y2014]) (kb/Work PB4950, P2.3a):\n" + string.Join("\n", offences));
    }

    /// <summary>The detector reports what it is for, across lines and in each shape, so an empty answer above is evidence.</summary>
    [Fact]
    public void TheDetector_ReportsEachShape_AcrossLines()
    {
        const string planted = """
            class C
            {
                string A(EditionContext Edition) => Edition.DialectLevel < 2002
                    ? "ISO §1985:VI.1"
                    : "ISO §2023:8.1";
                string B(EditionInfo edition) => edition.Year switch
                {
                    85 => $"85 {1}",
                    _ => "later",
                };
                string D(int dialectLevel)
                {
                    if (dialectLevel is 2002 or 2014)
                        return "a";
                    else
                        return "b" + "c";
                }
                string E(EditionInfo e) => e.Has(2002) ? "x" : "y";
                string Fine(EditionInfo edition) => edition.Cite("a", "b", "b");
                int Count(int dialectLevel) => dialectLevel >= 2023 ? 63 : 31;
                string Mode(EditionContext Edition) => Edition.Permissive ? "warning" : "error";
                string Mixed(EditionContext Edition, bool index) => index && Edition.DialectLevel < 2002 ? "refused" : "admitted";
                string Fact(EditionInfo edition) => edition.SubscriptedClauseOperands ? "subscripted" : "plain";
                string Level(int dialectLevel) => dialectLevel switch { >= 2023 => "a", _ => "b" };
            }
            """;
        Assert.Equal(["3: conditional expression", "6: switch expression", "13: if/else", "18: conditional expression",
            "22: conditional expression", "24: switch expression"], Find(planted));
    }
}
