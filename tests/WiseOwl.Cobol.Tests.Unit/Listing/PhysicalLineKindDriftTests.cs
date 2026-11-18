// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Diagnostics;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Listing;
using WiseOwl.Cobol.Listing.Building;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Listing;

/// <summary>
/// The recorded kind of each physical line (<see cref="ReferenceFormatMap.KindAt"/>, DESIGN-listing.md section 4.4.1) and the
/// ONE reader of a fixed-form line's columns (<see cref="FixedFormLine"/>, kb/Work PB1966) cannot drift: for a corpus of
/// fixed-form lines, the kind the conversion recorded equals the classification <see cref="FixedFormLine.Indicator"/> gives
/// (§2023:6.3.3: "The indicator area identifies the type of a source line"; §1985:IV.7.2.4: the slant and the asterisk are
/// both comment lines, the slant ejecting a page in the listing). And the map from the front end's kind to the model's is
/// total in both directions.
/// </summary>
public sealed class PhysicalLineKindDriftTests
{
    /// <summary>A fixed-form corpus: one line per indicator, plus a blank line and a comment-entry body (the converter
    /// decides those). Sequence areas are numeric so the corpus also reads as fixed form under detection.</summary>
    private static readonly string[] FixedCorpus =
    [
        "000100 IDENTIFICATION DIVISION.",
        "000200 PROGRAM-ID. KINDS.",
        "000300 AUTHOR. A WRITER.",
        "000400     WHOSE ENTRY GOES ON.",
        "000500 ENVIRONMENT DIVISION.",
        "000600* AN ASTERISK COMMENT",
        "000700/ A SLANT COMMENT",
        "000800D    DISPLAY 'DEBUG'.",
        "000900 DATA DIVISION.",
        "001000 01 X PIC X(10) VALUE 'ABCDEFGHIJ",
        "001100-    'KL'.",
        "001200",
        "001300 PROCEDURE DIVISION.",
    ];

    /// <summary>What the indicator area says, read by the one reader of its columns, refined where the corpus states the line's
    /// program text is blank or a comment-entry body (the converter's decisions, not the indicator's).</summary>
    private static PhysicalLineKind Expected(string line, int number) => new FixedFormLine(line).Indicator.Value switch
    {
        _ when number == 4 => PhysicalLineKind.CommentEntry,      // the Area-B body of the AUTHOR paragraph
        ' ' when string.IsNullOrWhiteSpace(new FixedFormLine(line).ProgramText) => PhysicalLineKind.Blank,
        ' ' => PhysicalLineKind.Source,
        '*' => PhysicalLineKind.Comment,
        '/' => PhysicalLineKind.PageEjectComment,
        'D' => PhysicalLineKind.DebuggingLine,
        '-' => PhysicalLineKind.Continuation,
        var other => throw new InvalidOperationException($"the corpus holds no indicator '{(char)other}'"),
    };

    [Fact]
    public void EveryFixedFormLine_RecordsTheKindItsIndicatorGives()
    {
        var gates = new ReferenceFormatDiagnostics(85, false, new DiagnosticBag());
        ReferenceFormatProcessor.NormalizeToFreeFormMapped(string.Join('\n', FixedCorpus), gates, "kinds.cob", initialFixed: true,
            out var map);
        for (int n = 1; n <= FixedCorpus.Length; n++)
            Assert.True(Expected(FixedCorpus[n - 1], n) == map.KindAt(n),
                $"line {n} '{FixedCorpus[n - 1]}': expected {Expected(FixedCorpus[n - 1], n)}, recorded {map.KindAt(n)}");
    }

    /// <summary>Free form, where the edition reads the floating comment indicator and has compiler directives (2023): a
    /// <c>*&gt;</c> line is a comment line (§2023:6.2.3.1), a <c>&gt;&gt;</c> line a directive (§2023:7.3.3), an empty line
    /// blank, and a discarded <c>&gt;&gt;SOURCE FORMAT</c> line a directive too (§2023:6.5 1)).</summary>
    [Fact]
    public void FreeFormLines_RecordCommentDirectiveBlankAndSource()
    {
        string[] lines =
        [
            ">>SOURCE FORMAT FREE",
            "IDENTIFICATION DIVISION.",
            "*> a floating comment line",
            "",
            ">>DEFINE X AS 1",
            "PROGRAM-ID. FREE1.",
        ];
        var gates = new ReferenceFormatDiagnostics(2023, false, new DiagnosticBag());
        ReferenceFormatProcessor.NormalizeToFreeFormMapped(string.Join('\n', lines), gates, "free.cob", initialFixed: false, out var map);
        PhysicalLineKind[] expected =
        [
            PhysicalLineKind.Directive, PhysicalLineKind.Source, PhysicalLineKind.Comment, PhysicalLineKind.Blank,
            PhysicalLineKind.Directive, PhysicalLineKind.Source,
        ];
        Assert.Equal(expected, Enumerable.Range(1, lines.Length).Select(map.KindAt));
    }

    /// <summary>A line that is not one of the text's is the caller's fault and throws (an input the map cannot resolve fails
    /// loudly).</summary>
    [Fact]
    public void KindAt_OutsideTheText_Throws()
    {
        ReferenceFormatProcessor.NormalizeToFreeFormMapped("000100 X.", null, "one.cob", initialFixed: true, out var map);
        Assert.Throws<ArgumentOutOfRangeException>(() => map.KindAt(2));
        Assert.Throws<ArgumentOutOfRangeException>(() => map.KindAt(0));
    }

    /// <summary>⛔ The one switch from the front end's kind to the model's (DESIGN-listing.md section 4.2) is total both ways: every
    /// <see cref="PhysicalLineKind"/> but the candidate (which the builder settles first) maps, the candidate throws, and every
    /// <see cref="ListingLineKind"/> is the image of some physical kind. A member added to either enum without the other is red.</summary>
    [Fact]
    public void TheKindMap_IsTotal_BothWays()
    {
        var images = new HashSet<ListingLineKind>();
        foreach (var kind in Enum.GetValues<PhysicalLineKind>())
        {
            if (kind == PhysicalLineKind.CommentEntryCandidate)
            {
                Assert.Throws<InvalidOperationException>(() => ListingLineKinds.Of(kind));
                continue;
            }
            images.Add(ListingLineKinds.Of(kind));
        }
        Assert.Equal(Enum.GetValues<ListingLineKind>().ToHashSet(), images);
    }
}
