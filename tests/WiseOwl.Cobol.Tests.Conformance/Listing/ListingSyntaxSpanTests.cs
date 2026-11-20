// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol;
using WiseOwl.Cobol.Listing;
using WiseOwl.Cobol.Listing.Html;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance.Listing;

/// <summary>
/// The classes of the source text the listing colours by (DESIGN-listing.md section 7.2; kb/Work PB5949), end to end: each line's
/// spans are what the compiler read the text as, at the edition in force. Every expected column and length is the written text's
/// own (found in the line the test wrote), and every expected class is the standard's category of that text:
/// <list type="bullet">
/// <item>the fixed-form areas (§2023:6.3.1: "The sequence number occupies six character positions"; the indicator area is the 7th
/// position; the text past margin R is ignored, DOC-A.1-158);</item>
/// <item>the character-strings (§2023:8.3.1: "forms a COBOL word, a literal, or a picture character-string"; §1985:IV.4.2.2 adds
/// the comment-entry) and the categories of a COBOL word (§2023:8.3.2.1), reserved words being the EDITION's (§2023:8.3.2.1 SR1:
/// "Reserved words shall not be used as user-defined words"): a word §2002:8.9 reserves and the 1985 list does not (GOBACK) is a
/// user-defined word in a 1985 program and a reserved word at 2023.</item>
/// </list>
/// </summary>
public sealed class ListingSyntaxSpanTests : IDisposable
{
    private readonly string _dir = ScratchDirectory.Create("listingsyntax");

    public void Dispose() => ScratchDirectory.Remove(_dir);

    // Positions 1-6 a sequence number, 7 the indicator, 8-72 the program text, 73-80 past margin R.
    private const string Fixed85 = """
        000100 IDENTIFICATION DIVISION.                                         SYN85
        000200 PROGRAM-ID. SYN85.
        000300 AUTHOR. A WRITER OF ENTRIES.
        000400*A COMMENT LINE
        000500 DATA DIVISION.
        000600 WORKING-STORAGE SECTION.
        000700 01  GOBACK PIC X(5) VALUE "AB".
        000800 01  NUM PIC S9(3)V99 VALUE 12.5.
        000900 01  LONG-LIT PIC X(70) VALUE "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
        001000-    "BBBB".
        001100     COPY SYNREC.
        001200     COPY SYNREC REPLACING ==SYN-FIELD== BY ==SYN-OTHER==.
        001300 PROCEDURE DIVISION.
        001400 P-1.
        001500     MOVE SPACES TO GOBACK.
        001600     MOVE ALL "X" TO GOBACK.
        001700     IF NUM > ZERO DISPLAY GOBACK.
        001800     STOP RUN.
        """;

    private const string Rec = """
               01  SYN-FIELD PIC 9(2).
        """;

    private const string Free2023 = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. SYN23.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01  N   PIC 9(4) VALUE ZERO. *> an inline comment
        PROCEDURE DIVISION.
            COMPUTE N = FUNCTION LENGTH("ABC") + 1
            GOBACK.
        """;

    private ListingDocument Compile(string name, string text, int edition, Frontend.Preprocessor.InitialReferenceFormat format = Frontend.Preprocessor.InitialReferenceFormat.Fixed)
    {
        string source = Path.Combine(_dir, name + ".cob");
        File.WriteAllText(source, text);
        File.WriteAllText(Path.Combine(_dir, "SYNREC.cpy"), Rec);
        var result = CompiledProgramCache.CompileCold(new CompilerDriver.Options(source, Path.Combine(_dir, name + ".dll"),
            DialectLevel: edition, CopyPaths: [_dir], SourceFormat: format,
            Listing: new ListingRequest(Formats: ListingFormats.Html | ListingFormats.Json)));
        Assert.True(result.Success, string.Join("\n", result.Errors));
        ListingJsonContract.AssertHolds(result.Listing!);
        return result.Listing!;
    }

    private static ListingLine Line(ListingDocument listing, int fileLine, int? inclusion = null) =>
        Assert.Single(listing.Lines, l => l.FileLine == fileLine && l.Inclusion == inclusion);

    /// <summary>The span over the <paramref name="occurrence"/>-th written <paramref name="text"/> of <paramref name="line"/>: its
    /// column and length are where the test wrote it, and it has <paramref name="cls"/>.</summary>
    private static void HasSpan(ListingLine line, string text, ListingSpanClass cls, int occurrence = 1)
    {
        int at = -1;
        for (int n = 0; n < occurrence; n++) at = line.Text.IndexOf(text, at + 1, StringComparison.Ordinal);
        Assert.True(at >= 0, $"'{text}' is not written on line {line.FileLine}");
        Assert.Contains(new ListingSpan(at + 1, text.Length, cls), line.Spans);
    }

    [Fact]
    public void TheFixedFormAreas_TheCommentAndTheCommentEntry_AreTheReferenceFormats()
    {
        var listing = Compile("syn85", Fixed85, 85);
        foreach (var line in listing.Lines.Where(l => l.Inclusion is null && l.Text.Length >= 6))
            Assert.Equal(new ListingSpan(1, 6, ListingSpanClass.SequenceArea), line.Spans[0]);
        HasSpan(Line(listing, 1), "SYN85", ListingSpanClass.IdentificationArea);
        Assert.Equal(73, Line(listing, 1).Spans.Single(s => s.Class == ListingSpanClass.IdentificationArea).Column);
        HasSpan(Line(listing, 4), "*", ListingSpanClass.Indicator);
        HasSpan(Line(listing, 4), "A COMMENT LINE", ListingSpanClass.Comment);
        HasSpan(Line(listing, 3), "AUTHOR", ListingSpanClass.ReservedWord);
        HasSpan(Line(listing, 3), "A WRITER OF ENTRIES.", ListingSpanClass.CommentEntry);
        // A continued literal (§1985:IV.7.2.2 continuation) is one literal written on two lines: each line's part is literal text.
        var first = Line(listing, 9);
        HasSpan(first, first.Text[first.Text.IndexOf('"')..], ListingSpanClass.AlphanumericLiteral);
        HasSpan(Line(listing, 10), "-", ListingSpanClass.Indicator);
        HasSpan(Line(listing, 10), "BBBB\"", ListingSpanClass.AlphanumericLiteral);
    }

    [Fact]
    public void AWordThe2002EditionReserves_IsAUserDefinedWordInA1985Program()
    {
        var listing = Compile("syn85", Fixed85, 85);
        HasSpan(Line(listing, 7), "GOBACK", ListingSpanClass.UserWord);
        HasSpan(Line(listing, 15), "GOBACK", ListingSpanClass.UserWord);
        HasSpan(Line(listing, 16), "GOBACK", ListingSpanClass.UserWord);
        HasSpan(Line(listing, 17), "GOBACK", ListingSpanClass.UserWord);
        // The same word read as the statement at 2023 is that edition's reserved word.
        var listing2023 = Compile("syn23", Free2023, 2023, Frontend.Preprocessor.InitialReferenceFormat.Free);
        HasSpan(Line(listing2023, 8), "GOBACK", ListingSpanClass.ReservedWord);
    }

    [Fact]
    public void EachWordLiteralAndPictureString_HasTheCategoryOfWhatItIs()
    {
        var listing = Compile("syn85", Fixed85, 85);
        var line7 = Line(listing, 7);
        HasSpan(line7, "01", ListingSpanClass.LevelNumber);
        HasSpan(line7, "PIC", ListingSpanClass.ReservedWord);
        HasSpan(line7, "X(5)", ListingSpanClass.Picture);
        HasSpan(line7, "VALUE", ListingSpanClass.ReservedWord);
        HasSpan(line7, "\"AB\"", ListingSpanClass.AlphanumericLiteral);
        HasSpan(line7, ".", ListingSpanClass.Separator);
        HasSpan(Line(listing, 8), "12.5", ListingSpanClass.NumericLiteral);
        HasSpan(Line(listing, 8), "S9(3)V99", ListingSpanClass.Picture);
        HasSpan(Line(listing, 14), "P-1", ListingSpanClass.UserWord);
        HasSpan(Line(listing, 15), "SPACES", ListingSpanClass.FigurativeConstant);
        HasSpan(Line(listing, 16), "ALL", ListingSpanClass.FigurativeConstant);
        HasSpan(Line(listing, 17), ">", ListingSpanClass.Operator);
        HasSpan(Line(listing, 17), "ZERO", ListingSpanClass.FigurativeConstant);
        var listing2023 = Compile("syn23", Free2023, 2023, Frontend.Preprocessor.InitialReferenceFormat.Free);
        HasSpan(Line(listing2023, 7), "LENGTH", ListingSpanClass.FunctionName);
        HasSpan(Line(listing2023, 7), "FUNCTION", ListingSpanClass.ReservedWord);
        HasSpan(Line(listing2023, 5), "*> an inline comment", ListingSpanClass.Comment);
    }

    [Fact]
    public void ALibraryTextCopiedTwice_IsClassedInEachBlock_AndAReplacedWordNotAsWritten()
    {
        var listing = Compile("syn85", Fixed85, 85);
        var copies = listing.Lines.Where(l => l.Inclusion is not null).ToList();
        Assert.Equal(2, copies.Count);
        HasSpan(copies[0], "SYN-FIELD", ListingSpanClass.UserWord);
        HasSpan(copies[0], "9(2)", ListingSpanClass.Picture);
        HasSpan(copies[1], "9(2)", ListingSpanClass.Picture);
        // REPLACING compiled SYN-OTHER where SYN-FIELD is written: the written word was not what the lexer read there.
        Assert.DoesNotContain(copies[1].Spans, s => s.Column == copies[1].Text.IndexOf("SYN-FIELD", StringComparison.Ordinal) + 1);
    }

    [Fact]
    public void TheHtmlPage_WrapsEachSpanInTheClassOfItsKind()
    {
        Compile("syn85", Fixed85, 85);
        string html = File.ReadAllText(Path.Combine(_dir, "syn85.lst.html"));
        Assert.Contains($"<span class=\"{HtmlListingRenderer.SpanClass(ListingSpanClass.UserWord)}\">GOBACK</span>", html, StringComparison.Ordinal);
        Assert.Contains($"<span class=\"{HtmlListingRenderer.SpanClass(ListingSpanClass.Comment)}\">A COMMENT LINE</span>", html,
            StringComparison.Ordinal);
    }
}
