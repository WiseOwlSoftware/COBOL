// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CobolNet.CodeGen;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ ONE RULE, ONE PLACE — the §8.3.3.6.2 figurative-operand form (kb/Work PB461). The word <c>ALL</c> is an
/// OPTIONAL word in Formats 1–5 (the printed diagrams underline <c>ZERO</c>/<c>SPACE</c>/… but not <c>ALL</c>)
/// and a REQUIRED one in Format 6 (<c>ALL literal-1</c>), and the parse tree's <c>GetText()</c> glues the two
/// words — so <c>ALL SPACES</c> reaches the emitters spelled <c>ALLSPACES</c> and <c>ALL "*"</c> as
/// <c>ALL"*"</c>. FIVE call sites each wrote their own strip for that, and each accepted a DIFFERENT subset:
/// whitespace-only, whitespace-or-letter, glued-only, no-trim. The consequence was not a formatting nit:
/// §14.9.39.4 GR6 stores a level-88 value "according to the rules for the VALUE clause" and §8.8.4.5.3 GR3 makes
/// the condition true exactly when the stored value is one of those values, so the SET store and the condition
/// TEST reading the SAME text differently made <c>SET c TO TRUE</c> leave <c>c</c> FALSE.
/// <para>These two tests pin both halves of the repair: that the classifier answers identically for every
/// spelling of the same constant (the behaviour), and that no second copy of the strip comes back (the shape).
/// The second is the one that keeps the first true, because the defect was never a wrong answer in a rule — it
/// was a rule written down more than once.</para>
/// </summary>
public sealed class FigurativeOperandClassifierDriftTests
{
    /// <summary>The Formats 1–5 words, each in the three spellings the tree can produce: bare, ALL-glued and
    /// ALL-spaced. §8.3.3.6.2 makes all three the SAME figurative constant, so the classifier owes them one
    /// answer. (NULL/NULLS is the pointer figurative the VALUE/level-88 word maps accept as a LOW-VALUE fill;
    /// it rides <c>includeNull</c> and is covered by the second theory row.)</summary>
    public static TheoryData<string, char> FormatsOneToFive() => new()
    {
        { "ZERO", 'Z' }, { "ZEROS", 'Z' }, { "ZEROES", 'Z' },
        { "SPACE", 'S' }, { "SPACES", 'S' },
        { "QUOTE", 'Q' }, { "QUOTES", 'Q' },
        { "HIGH-VALUE", 'H' }, { "HIGH-VALUES", 'H' },
        { "LOW-VALUE", 'L' }, { "LOW-VALUES", 'L' },
        { "NULL", 'L' }, { "NULLS", 'L' },
    };

    [Theory]
    [MemberData(nameof(FormatsOneToFive))]
    public void EverySpellingOfAFormats1To5Constant_ClassifiesTheSame(string word, char kind)
    {
        foreach (string spelling in new[] { word, "ALL" + word, "ALL " + word, "  ALL   " + word + "  " })
        {
            var op = FigurativeConstants.Classify(spelling);
            Assert.True(op.Kind == kind,
                $"§8.3.3.6.2 makes '{spelling}' the same figurative constant as '{word}' (ALL is an optional "
                + $"word in Formats 1-5); the classifier answered {(op.Kind?.ToString() ?? "<none>")}.");
            Assert.Null(op.AllLiteral);
        }
    }

    /// <summary>Format 6 — <c>ALL literal-1</c>, where <c>ALL</c> IS required — in the glued and spaced
    /// spellings, over the three literal classes §8.3.3.6.3 SR2 admits (alphanumeric, national, boolean) and
    /// both delimiters (§8.3.3.1: the paired quotation symbols may be either apostrophes or quotation marks).
    /// A bare literal with NO <c>ALL</c> is not Format 6 and must classify as neither form.</summary>
    [Theory]
    [InlineData("ALL\"*\"", "\"*\"")]
    [InlineData("ALL \"*\"", "\"*\"")]
    [InlineData("ALL'AB'", "'AB'")]
    [InlineData("ALLN\"AB\"", "N\"AB\"")]
    [InlineData("ALL N\"AB\"", "N\"AB\"")]
    [InlineData("ALLB\"10\"", "B\"10\"")]
    [InlineData("ALLX\"41\"", "X\"41\"")]
    public void Format6_ClassifiesInBothSpellings(string raw, string literal1)
    {
        var op = FigurativeConstants.Classify(raw);
        Assert.Equal(literal1, op.AllLiteral);
        Assert.Null(op.Kind);
    }

    [Theory]
    [InlineData("\"*\"")]
    [InlineData("N\"AB\"")]
    [InlineData("1234")]
    [InlineData("-1.5E+3")]
    [InlineData("ALLOCATED-FLAG")]
    public void AnOrdinaryOperand_IsNeitherForm(string raw)
    {
        var op = FigurativeConstants.Classify(raw);
        Assert.Null(op.Kind);
        Assert.Null(op.AllLiteral);
    }

    /// <summary>The pointer figurative is admitted only where the caller asks for it — the VALUE/level-88 value
    /// producers do, the §13.18.63.3 class validator and the SR9 implied-picture reader do not. One knob, asked
    /// of ONE classifier, rather than two word maps that disagreed about it (the SET store filled LOW-VALUE for
    /// <c>VALUE NULL</c> while the condition test compared the four characters N-U-L-L).</summary>
    [Fact]
    public void Null_RidesIncludeNull_InBothSpellings()
    {
        Assert.Equal('L', FigurativeConstants.Classify("ALLNULLS").Kind);
        Assert.Equal('L', FigurativeConstants.Classify("NULL").Kind);
        Assert.Null(FigurativeConstants.Classify("ALLNULLS", includeNull: false).Kind);
        Assert.Null(FigurativeConstants.Classify("NULL", includeNull: false).Kind);
    }

    /// <summary>Files allowed to spell out an <c>ALL</c> prefix test of their own, each with its reason. Adding a
    /// name here is an adjudication: it says a SIXTH reader of the form is justified, which is the claim that
    /// was wrong five times.</summary>
    private static readonly Dictionary<string, string> Sanctioned = new(StringComparer.OrdinalIgnoreCase)
    {
        ["FigurativeConstants.cs"] = "THE classifier — Formats 1-5, where the ALL word is optional",
        ["CobolLiteral.cs"] = "THE Format-6 codec (AllLiteralRaw), which the classifier delegates to",
        ["InitializeBinder.cs"] = "the INITIALIZE … REPLACING operand reader, a separate mechanism with its own "
            + "note in the register; it is expected to fold into the classifier when that note lands",
    };

    /// <summary>No SIXTH private ALL-strip. The pattern is the literal test every one of the five copies used —
    /// a <c>StartsWith("ALL"…)</c> over an operand text — anywhere under the frontend or the compiler.</summary>
    [Fact]
    public void NoSecondPrivateAllStrip_ExistsInTheTree()
    {
        var offenders = new List<string>();
        foreach (string project in new[] { "Cobol.Net.Compiler", "Cobol.Net.Frontend" })
            foreach (string file in Directory.EnumerateFiles(TestRepo.Src(project), "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                    || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                    || file.Contains($"{Path.DirectorySeparatorChar}Generated{Path.DirectorySeparatorChar}")) continue;
                if (Sanctioned.ContainsKey(Path.GetFileName(file))) continue;
                string text = File.ReadAllText(file);
                // Comments quote the spellings freely; only CODE is the subject, so strip line comments first.
                text = Regex.Replace(text, @"^[ \t]*//[^\r\n]*", "", RegexOptions.Multiline);
                foreach (Match m in Regex.Matches(text, @"StartsWith\(\s*""ALL""", RegexOptions.None))
                    offenders.Add($"{Path.GetFileName(file)} @ {m.Index}");
            }

        Assert.True(offenders.Count == 0,
            "A private ALL-strip is a SIXTH reading of ISO §8.3.3.6.2 and the five that preceded it each "
            + "accepted a different subset of the spellings — ask FigurativeConstants.Classify instead, or add "
            + "the file to Sanctioned with its reason (kb/Work PB461): " + string.Join(", ", offenders));
    }
}
