// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE <c>&lt;&gt;</c> INTRODUCTION GATE IS ONE BODY, ASKED BY TWO LANES (kb/Work PB3604). ISO §8.7.5.2 SR11 makes
/// the symbol "an abbreviation for NOT EQUAL" and Annex D.3 item 16 of the 2009 committee draft introduces it at 2014;
/// the construct row is <c>not-equal-operator-2014</c>. A relation is written in two places the compilation-unit walk
/// does not both reach — a statement's condition (the walk) and a <c>&gt;&gt;IF</c> / <c>&gt;&gt;EVALUATE</c> constant-
/// conditional-expression (a fragment the conditional-compilation stage parses apart) — and until PB3604 the row sat at
/// 2002 with the directive lane unasked, so <c>&gt;&gt;IF 5 &lt;&gt; 3</c> compiled at 2002. Three facts keep that shut:
/// the row's id is read by the one gate body, both lanes call that body, and the token is spelled by one grammar rule
/// (so the node the gate recognizes is the only way to write the operator).
/// </summary>
[Trait("Sweep", "tree")]
public sealed class RelationalOperatorGateDriftTests
{
    private const string GateFile = "src/WiseOwl.Cobol.Frontend/Expressions/RelationalOperatorGate.cs";

    /// <summary>The generated registry files DEFINE the id; they are not readers of it. The gate body is the rule's owner
    /// and is set aside by name.</summary>
    private static readonly TreeScope Sources = new(Under: ["src/"],
        Except: ["src/WiseOwl.Cobol.Editions/Constructs.g.cs", "src/WiseOwl.Cobol.Editions/ConstructRegistry.g.cs",
                 "src/WiseOwl.Cobol.Frontend/Expressions/RelationalOperatorGate.cs"],
        Kinds: [".cs"]);

    private static readonly TreeScope Grammars = new(Under: ["src/WiseOwl.Cobol.Frontend/Grammar/"], Kinds: [".g4"]);

    private static string Read(string repoRelative) => File.ReadAllText(TestRepo.At(repoRelative.Split('/')));

    /// <summary>No file but the gate body names the row: a second reader of <c>NotEqualOperator2014</c> would be a lane
    /// with its own copy of the rule, which is how the directive lane came to have none.</summary>
    [Fact]
    public void TheRowIsReadOnlyByTheGateBody()
    {
        var readers = TrackedTree.Files(Sources)
            .Where(f => Regex.IsMatch(Read(f), @"\bConstructs\s*\.\s*NotEqualOperator2014\b"))
            .ToList();
        Assert.True(readers.Count == 0,
            "the not-equal-operator-2014 row is gated through RelationalOperatorGate (kb/Work PB3604), the one body both the "
            + "compilation-unit walk and the conditional-compilation stage ask; these files read the row themselves: "
            + string.Join(", ", readers));
        Assert.Matches(@"\bConstructs\s*\.\s*NotEqualOperator2014\b", Read(GateFile));
    }

    /// <summary>Both lanes ask the body: the parse arm per <c>comparisonOperator</c> node and the directive stage per
    /// evaluated fragment.</summary>
    [Theory]
    [InlineData("src/WiseOwl.Cobol.Compiler/Validation/VersionConformancePass.cs")]
    [InlineData("src/WiseOwl.Cobol.Frontend/Preprocessor/ConditionalCompilationProcessor.cs")]
    public void EachLane_AsksTheGateBody(string lane)
        => Assert.Matches(@"\bRelationalOperatorGate\s*\.\s*Check\s*\(", Read(lane));

    /// <summary>The NOTEQUAL token is named by exactly one parser rule, <c>comparisonOperator</c> — the node the gate
    /// recognizes. A rule that wrote the token itself would be an operator the gate never sees.</summary>
    [Fact]
    public void TheNotEqualToken_IsSpelledOnlyByComparisonOperator()
    {
        var namers = new SortedSet<string>(StringComparer.Ordinal);
        foreach (string file in TrackedTree.Files(Grammars))
        {
            string text = Regex.Replace(Read(file), @"//[^\r\n]*", "");
            if (Regex.IsMatch(text, @"^\s*lexer\s+grammar\b", RegexOptions.Multiline)) continue;
            foreach (string chunk in text.Split(';'))
            {
                var m = Regex.Match(chunk, @"\A\s*(?<name>[a-z]\w*)\s*:(?<body>.*)\z", RegexOptions.Singleline);
                if (m.Success && Regex.IsMatch(m.Groups["body"].Value, @"\bNOTEQUAL\b"))
                    namers.Add(m.Groups["name"].Value);
            }
        }
        Assert.Equal(["comparisonOperator"], namers);
    }
}
