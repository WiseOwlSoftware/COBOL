// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A COBOL TEXT GETS ITS LEXER, ITS NORMALIZED TOKEN STREAM AND ITS PARSER FROM ONE PLACE (kb/Work PB2735):
/// <c>src/WiseOwl.Cobol.Frontend/Parsing/CobolTokenStream.cs</c>. The steps between lexing and parsing (the lexer's priming,
/// the debugging lines first, ZERO beside an operator, the §2023:8.9 retype) are order-dependent, and when the whole group's
/// parse and the fragment parsers each wrote them, the copies drifted: two of the fragment copies omitted the ZERO rewrite,
/// a run-time abort and a silent wrong answer (PB50, PB54), and the keyword probe ran an unprimed lexer with a third error
/// listener. So no other product source constructs a <c>CobolLexer</c> or a <c>CobolParserCore</c>: a new reading of source
/// text calls the one preparation, and the next post-lex step added there reaches every reading.
/// </summary>
[Trait("Sweep", "tree")]
public sealed class TokenStreamPreparationDriftTests
{
    private const string TheOnePlace = "src/WiseOwl.Cobol.Frontend/Parsing/CobolTokenStream.cs";

    private static readonly Regex Construction = new(@"\bnew\s+(CobolLexer|CobolParserCore)\s*\(", RegexOptions.Compiled);

    private static readonly TreeScope ProductSources = new(Under: ["src/"], Except: ["src/WiseOwl.Cobol.Frontend/Generated/"], Kinds: [".cs"]);

    [Fact]
    public void OnlyTheOnePreparation_ConstructsALexerOrAParser()
    {
        var files = TrackedTree.Files(ProductSources);
        Assert.Contains(TheOnePlace, files);   // the scan reads the tree it claims to (a vacuous population fails here)
        string[] elsewhere =
        [
            .. files.Where(rel => rel != TheOnePlace)
                .SelectMany(rel => File.ReadAllLines(TestRepo.Within("src/", rel))
                    .Select((line, i) => (rel, line, i))
                    .Where(x => Construction.IsMatch(x.line))
                    .Select(x => $"{x.rel}:{x.i + 1}: {x.line.Trim()}")),
        ];
        Assert.True(elsewhere.Length == 0,
            "a CobolLexer or CobolParserCore is constructed outside CobolTokenStream (kb/Work PB2735): call "
            + "CobolTokenStream.Prepare / Lexer / Parser instead, so the post-lex steps run in their one order:\n  "
            + string.Join("\n  ", elsewhere));
        Assert.Contains(File.ReadAllLines(TestRepo.Within("src/", TheOnePlace)), line => Construction.IsMatch(line));
    }
}
