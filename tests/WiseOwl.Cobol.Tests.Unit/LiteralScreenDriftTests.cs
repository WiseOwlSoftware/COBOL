// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Frontend.Parsing;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A LITERAL'S OWN SYNTAX RULES ARE ASKED ONCE, OF EVERY LITERAL TOKEN, AT ONE SITE (kb/Work PB1393): the §8.3.3
/// length rule (COBOL0814), hexadecimal grouping rule (COBOL1635) and content repertoire rule (COBOL2630,
/// kb/Work PB1441) live in <c>LiteralScreen</c>, which walks every token of the unit's tree and of each compiler-directive
/// fragment (kb/Work PB1616), and nowhere else; and
/// the literal token set (<c>LiteralTokens.Types</c>, shared with the §8.3.5 <c>SeparatorRule</c>) is every token the
/// lexer defines over a literal body fragment, so a new literal token joins both rules or fails here.
/// </summary>
/// <remarks>
/// Each rule used to be written in the one funnel its author was fixing (the length cap in two procedure-operand
/// arms; the grouping rule in two version-pass visitors), and every other position a literal can be written in —
/// VALUE, level-88, CONSTANT, ALL literal-1, a concatenation operand, a keyword-omitted intrinsic argument —
/// compiled the violation in silence. The keyword-omitted argument was the reason for the token-set half while it was
/// a SUBSCRIPT-mode capture (a hole exactly as long as <c>X"…"</c> had no twin there); since kb/Work PB2113 it is
/// parsed in place, so its literals are the ordinary tokens of the walked tree.
/// </remarks>
[Trait("Sweep", "tree")]
public sealed class LiteralScreenDriftTests
{
    /// <summary>The lexer fragments whose match IS a literal of class alphanumeric, boolean or national.</summary>
    private static readonly string[] LiteralBodies = ["STR_BODY", "NAT_BODY", "BOOL_BODY", "HEX_BODY"];

    private static readonly string LexerText =
        File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Frontend", "Grammar", "Core", "CobolLexer.g4"));

    [Fact]
    public void ScreenTokenSet_IsEveryTokenDefinedOverALiteralBody()
    {
        foreach (string body in LiteralBodies)
            Assert.Matches(new Regex($@"(?m)^fragment\s+{body}\s*:"), LexerText);   // a rename must fail here, not empty the set
        // A literal token may carry the one delimited-literal action (OnDelimitedLiteral, kb/Work PB3245) after its body.
        var defined = Regex.Matches(LexerText,
                $@"(?m)^([A-Z_]+)\s*:\s*({string.Join("|", LiteralBodies)})\s*(?:\{{\s*OnDelimitedLiteral\(\);\s*\}}\s*)?;")
            .Select(m => m.Groups[1].Value).ToHashSet();
        Assert.True(defined.Count >= 4,
            $"expected the four literal tokens (STRINGLIT, HEXLIT, NATLIT, BOOLLIT), found {defined.Count}");
        var screened = LiteralTokens.Types
            .Select(t => CobolLexer.DefaultVocabulary.GetSymbolicName(t)).ToHashSet();
        Assert.Equal(defined.Order(), screened.Order());
    }

    /// <summary>The floating-point numeric literal's edition gate (kb/Work PB674) reads
    /// <c>LiteralTokens.FloatingPointTypes</c>, which must be every token the lexer defines over a <c>FLOAT*_BODY</c>
    /// fragment (the sign-adjacent <c>FN_SIGNED_*</c> twins are re-typed to these by <c>OnSignedLiteral</c>, so they
    /// are not in the set and never reach the parser). A new float token spelling joins the gate or fails here, and the
    /// gate has the one construct id and the one reporting site.</summary>
    [Fact]
    public void FloatingPointTokenSet_IsEveryTokenDefinedOverAFloatBody()
    {
        var bodies = Regex.Matches(LexerText, @"(?m)^fragment\s+(FLOAT\w*_BODY)\s*:").Select(m => m.Groups[1].Value).ToList();
        Assert.True(bodies.Count >= 2, $"expected FLOAT_BODY and FLOAT_COMMA_BODY, found {bodies.Count}");   // a rename must fail here
        var defined = Regex.Matches(LexerText, $@"(?m)^([A-Z_]+)\s*:\s*({string.Join("|", bodies)})\s*;")
            .Select(m => m.Groups[1].Value).ToHashSet();
        var screened = LiteralTokens.FloatingPointTypes
            .Select(t => CobolLexer.DefaultVocabulary.GetSymbolicName(t)).ToHashSet();
        Assert.Equal(defined.Order(), screened.Order());
        Assert.Equal(1, Directory.EnumerateFiles(TestRepo.Src(), "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                        && Path.GetFileName(p) is not ("Constructs.g.cs" or "ConstructRegistry.g.cs"))
            .Count(p => File.ReadAllText(p).Contains("Constructs.FloatingPointLiteral2002", StringComparison.Ordinal)));
    }

    /// <summary>The three rule predicates are asked only through <c>CobolLiteral.SyntaxViolation</c>, their one order;
    /// that is asked by <c>LiteralScreen</c> alone, over the unit's tree and over each compiler-directive fragment (lexed
    /// apart from the unit); a directive fragment is parsed only in the conditional-compilation stage's three funnels
    /// (kb/Work PB1616), each of which screens it; the descriptors have ONE emit site, and no emit site spells a code as
    /// a bare string. The closing-delimiter rule (COBOL5563, kb/Work PB3245) is asked of the tokens before the parse: its
    /// one question is asked by LiteralScreen alone, and LiteralScreen's one report is called by the two lexings — the
    /// unit's (Frontend) and each directive fragment's.</summary>
    [Theory]
    [InlineData("CobolLiteral.SyntaxViolation(", "LiteralScreen.cs")]
    [InlineData("DirectiveExpressionFragment.Parse", "ConditionalCompilationProcessor.cs")]
    [InlineData("CobolLiteral.HexGroupViolation(", "")]
    [InlineData("CobolLiteral.RepertoireViolation(", "")]
    [InlineData("CobolLiteral.LengthViolation(", "")]
    [InlineData("DiagnosticCatalog.LiteralContentRepertoire", "LiteralScreen.cs")]
    [InlineData("DiagnosticCatalog.HexLiteralDigitGrouping", "LiteralScreen.cs")]
    [InlineData("DiagnosticCatalog.LiteralTooLong", "LiteralScreen.cs")]
    [InlineData("\"COBOL2630\"", "")]
    [InlineData("\"COBOL0814\"", "")]
    [InlineData("\"COBOL1635\"", "")]
    [InlineData("CobolLiteral.IsUnterminated(", "LiteralScreen.cs")]
    [InlineData("DiagnosticCatalog.LiteralUnterminated", "LiteralScreen.cs")]
    [InlineData("LiteralScreen.ReportUnterminated(", "ConditionalCompilationProcessor.cs,Frontend.cs")]
    [InlineData("\"COBOL5563\"", "")]
    public void LiteralRule_HasOneReportingSite(string needle, string expectedSites)
    {
        char sep = Path.DirectorySeparatorChar;
        var sites = Directory.EnumerateFiles(TestRepo.Src(), "*.cs", SearchOption.AllDirectories)
            .Where(p => p.Contains($"{sep}WiseOwl.Cobol.") && !p.Contains($"{sep}obj{sep}") && !p.Contains($"{sep}bin{sep}")
                        // ConstructRegistry.g.cs is DATA, the rendering of constructs.json: the row literal-length-8191-2014
                        // names COBOL0814 as its diagnostic, the way every row names its own (kb/Work PB1615) — it reports nothing.
                        && Path.GetFileName(p) is not ("DiagnosticCatalog.cs" or "CobolLiteral.cs" or "ConstructRegistry.g.cs"))
            .Where(p => File.ReadAllText(p).Contains(needle, StringComparison.Ordinal))
            .Select(p => Path.GetFileName(p))
            .Order(StringComparer.Ordinal)
            .ToList();
        string[] expected = expectedSites.Length == 0 ? [] : expectedSites.Split(',');
        Assert.True(sites.SequenceEqual(expected),
            $"'{needle}' is used at [{string.Join(", ", sites)}], expected [{string.Join(", ", expected)}] — a literal's "
            + "own §8.3.3 rules are a property of the TOKEN; a second site is a second funnel the next literal position "
            + "will miss");
    }

    /// <summary>The file-level pin above says WHICH file parses a directive fragment; this says WHERE in it: every
    /// <c>DirectiveExpressionFragment.Parse…</c> call sits inside one of the three funnels, and each funnel screens what
    /// it parsed. A fourth parse arm written beside an evaluator would otherwise skip the screen (kb/Work PB1616).</summary>
    [Fact]
    public void DirectiveFragmentParse_IsCalledOnlyInsideTheThreeScreeningFunnels()
    {
        string text = File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Frontend", "Preprocessor", "ConditionalCompilationProcessor.cs"));
        var decls = Regex.Matches(text, @"(?m)^[ \t]*(?:private|internal|public)[ \t]+(?:static[ \t]+)?[^=;(\r\n]*?\b(\w+)\(")
            .Select(m => (m.Index, Name: m.Groups[1].Value)).ToList();
        string[] funnels = ["ParseOperandFragment", "ParseCceFragment", "ParseDisplayFragment"];
        var calls = Regex.Matches(text, @"DirectiveExpressionFragment\.Parse\w*\(").ToList();
        Assert.Equal(funnels.Length, calls.Count);   // a fourth call fails here, a vanished funnel too
        foreach (Match call in calls)
        {
            var enclosing = decls.Last(d => d.Index < call.Index);
            Assert.True(funnels.Contains(enclosing.Name),
                $"'{call.Value}' is called inside {enclosing.Name}, not one of the screening funnels [{string.Join(", ", funnels)}]");
            int end = decls.Where(d => d.Index > call.Index).Select(d => d.Index).DefaultIfEmpty(text.Length).First();
            Assert.Contains("ScreenFragment(", text.Substring(call.Index, end - call.Index), StringComparison.Ordinal);
        }
    }
}
