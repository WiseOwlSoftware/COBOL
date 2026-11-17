// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ EVERY WALK THE TYPE 6 SCREEN PROVES INTEGER-NESS WITH NAMES EVERY CONCRETE <see cref="BoundExpr"/> KIND (kb/Work PB5886 review
/// R4-1).
/// </summary>
/// <remarks>
/// <para>§2023:15.3 AR6 (cite.py --check): "An arithmetic expression that will always result in an integer value or an integer data item
/// shall be specified". The screen fails closed, and three holes of one kind reached review one after another: a node kind a walk did
/// not name fell to a default that answered as if it were an integer (a quotient by a literal, a nested function, a power over a
/// rounded base). Each walk now names every kind in an explicit arm and refuses in its default; this test enumerates the concrete
/// kinds by reflection and requires each walk's body to name each one, so a kind added later fails here until every walk decides it.</para>
/// </remarks>
public sealed class TypeSixWalkCoverageDriftTests
{
    /// <summary>The proof walks of the type 6 screen, by the text that opens each one's declaration in IntrinsicArgumentRules.cs.</summary>
    private static readonly string[] Walks =
    [
        "private static string? CollectAdditive(",
        "private static (int Lo, int Hi, string? Overflow) DigitBound(",
        "internal static bool ProvablyIntegral(",
        "private static ExactRational? ExactValue(",
    ];

    [Fact]
    public void EveryProofWalk_NamesEveryBoundExpressionKind()
    {
        string source = File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Compiler", "Binding", "IntrinsicArgumentRules.cs"));
        var kinds = typeof(BoundExpr).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsClass: true } && t.IsSubclassOf(typeof(BoundExpr)))
            .Select(t => t.Name).Order(StringComparer.Ordinal).ToList();
        Assert.True(kinds.Count > 0, "no concrete BoundExpr kind found by reflection");
        var missing = new List<string>();
        foreach (string walk in Walks)
        {
            string body = BodyOf(source, walk);
            foreach (string kind in kinds)
                if (!Regex.IsMatch(body, $@"\b{kind}\b"))
                    missing.Add($"{walk.TrimEnd('(')}: {kind}");
        }
        Assert.True(missing.Count == 0,
            "a proof walk of the type 6 screen does not name a BoundExpr kind - decide it in an explicit arm (an unknown kind is not proven "
            + "an integer, kb/Work PB5886 R4-1):\n  " + string.Join("\n  ", missing));
    }

    /// <summary>The text of a member from its declaration to the first line that closes it at the member's indentation.</summary>
    private static string BodyOf(string source, string declaration)
    {
        int start = source.IndexOf(declaration, StringComparison.Ordinal);
        Assert.True(start >= 0, $"IntrinsicArgumentRules.cs declares no '{declaration}' - the walk was renamed; update Walks");
        var close = new Regex(@"^    \};?\s*$", RegexOptions.Multiline);
        var end = close.Match(source, start);
        Assert.True(end.Success, $"no closing brace for '{declaration}'");
        return source[start..end.Index];
    }
}
