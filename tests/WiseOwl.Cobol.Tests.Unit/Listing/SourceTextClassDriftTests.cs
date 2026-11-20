// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Frontend.Common;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Frontend.Parsing;
using WiseOwl.Cobol.Listing;
using WiseOwl.Cobol.Listing.Building;
using WiseOwl.Cobol.Listing.Html;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Listing;

/// <summary>
/// The source listing's classes of the text cannot drift from the compiler that reads it (DESIGN-listing.md section 7.2; kb/Work
/// PB5949):
/// <list type="bullet">
/// <item>EVERY TOKEN KIND HAS A CLASS. Every token type of the parser's vocabulary (the lexer's, and the types a rewriter mints:
/// <c>ZERO_ARITH</c>, <c>EMBEDDED_BLOCK</c>) is either a type <see cref="SourceTextClassifier.FixedClassOf"/> classes, or a WORD
/// the parse and the edition class: <c>IDENTIFIER</c>, or a keyword token, proved one from the vocabulary itself (its spelling is a
/// COBOL word the lexer makes that type of, <see cref="CobolKeywordTokens.TryTokenType"/>) or declared one
/// (<see cref="SourceTextClassifier.UnspelledKeywords"/>). A token kind added to the lexer without a class is red here.</item>
/// <item>THE FRONT END'S CLASSES AND THE MODEL'S ARE ONE LIST: <see cref="ListingSpanClasses.Of"/> is total both ways, member by
/// member of the same name.</item>
/// <item>THE PAGE COLOURS EVERY CLASS, LIGHT AND DARK: the style holds a rule for each class's markup
/// (<see cref="HtmlListingRenderer.SpanClass"/>), and each colour variable a rule reads is defined for both schemes.</item>
/// </list>
/// Each check is a function of its inputs, so <see cref="TheTokenCheck_CanFail"/> shows it red on a classifier that forgets a kind.
/// </summary>
public sealed class SourceTextClassDriftTests
{
    /// <summary>The token types <paramref name="fixedClassOf"/> and <paramref name="unspelled"/> leave with no class: neither a
    /// fixed class nor a word.</summary>
    private static List<string> Unclassified(Func<int, SourceTextClass?> fixedClassOf, IReadOnlySet<int> unspelled)
    {
        var vocabulary = CobolParserCore.DefaultVocabulary;
        int maxType = ((Antlr4.Runtime.Vocabulary)vocabulary).getMaxTokenType();
        var missing = new List<string>();
        for (int type = 1; type <= maxType; type++)
        {
            if (fixedClassOf(type) is not null || type == CobolLexer.IDENTIFIER || unspelled.Contains(type)) continue;
            if (IsSpelledKeyword(vocabulary, type)) continue;
            missing.Add(vocabulary.GetSymbolicName(type) ?? type.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        return missing;
    }

    /// <summary>Whether the lexer makes <paramref name="type"/> of a COBOL word: the word its literal name spells, or the word its
    /// symbolic name spells (<c>HIGH_VALUE</c> is HIGH-VALUE, <c>NULL_</c> NULL), as the lexer itself reads it.</summary>
    private static bool IsSpelledKeyword(Antlr4.Runtime.IVocabulary vocabulary, int type)
    {
        foreach (string? spelling in new[] { vocabulary.GetLiteralName(type)?.Trim('\''), vocabulary.GetSymbolicName(type)?.TrimEnd('_').Replace('_', '-') })
            if (spelling is not null && Regex.IsMatch(spelling, "^[A-Za-z][A-Za-z0-9-]*$")
                && CobolKeywordTokens.TryTokenType(spelling, out int lexed) && lexed == type)
                return true;
        return false;
    }

    [Fact]
    public void EveryTokenKind_TheLexerOrARewriterMakes_HasAClass() =>
        Assert.Empty(Unclassified(SourceTextClassifier.FixedClassOf, SourceTextClassifier.UnspelledKeywords));

    [Fact]
    public void TheTokenCheck_CanFail()
    {
        // A classifier that forgot the alphanumeric literal, and a word list that forgot DEFINED, each leave their kind unclassed.
        Assert.Equal(["STRINGLIT"], Unclassified(t => t == CobolLexer.STRINGLIT ? null : SourceTextClassifier.FixedClassOf(t),
            SourceTextClassifier.UnspelledKeywords));
        Assert.Equal(["DEFINED"], Unclassified(SourceTextClassifier.FixedClassOf, new HashSet<int> { CobolLexer.PIC_IS }));
    }

    [Fact]
    public void TheSpanClassMap_IsTotal_BothWays()
    {
        var images = new HashSet<ListingSpanClass>();
        foreach (var cls in Enum.GetValues<SourceTextClass>())
        {
            var image = ListingSpanClasses.Of(cls);
            Assert.Equal(cls.ToString(), image.ToString());
            images.Add(image);
        }
        Assert.Equal(Enum.GetValues<ListingSpanClass>().ToHashSet(), images);
    }

    [Fact]
    public void ThePage_ColoursEveryClass_ForALightAndADarkPage()
    {
        string page = ListingRendering.Html(JsonListingWriterTests.EveryMember(), new ListingRequest(Formats: ListingFormats.Html));
        string style = page[(page.IndexOf("<style>", StringComparison.Ordinal) + 7)..page.IndexOf("</style>", StringComparison.Ordinal)];
        foreach (var cls in Enum.GetValues<ListingSpanClass>())
            Assert.Matches($@"\.{Regex.Escape(HtmlListingRenderer.SpanClass(cls))}\b[^{{]*\{{[^}}]*color:", style);
        int dark = style.IndexOf("prefers-color-scheme: dark", StringComparison.Ordinal);
        Assert.True(dark > 0, "the style has no dark scheme");
        var read = Regex.Matches(style, @"var\((--syn-[a-z]+)\)").Select(m => m.Groups[1].Value).ToHashSet();
        Assert.NotEmpty(read);
        foreach (string variable in read)
        {
            Assert.Matches($@"{Regex.Escape(variable)}:", style[..dark]);
            Assert.Matches($@"{Regex.Escape(variable)}:", style[dark..]);
        }
    }
}
