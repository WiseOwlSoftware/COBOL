// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Xml.Linq;
using WiseOwl.Cobol.Compiler;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Listing;
using WiseOwl.Cobol.Listing.Html;
using WiseOwl.Cobol.Listing.Text;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance.Listing;

/// <summary>
/// The cross-reference (docs/rearchitecture/DESIGN-listing.md sections 6.7 and 6.7.1, section 10 item 4), held on a planted program whose
/// every expected value is written from its source text: each reference's line and column is where the test's own line writes the name,
/// and each use is how the statement's general rules treat the position: an ADD's identifier-2 is an addend whose sum is stored back in
/// it (§2023:14.9.2.4 GR1: the sum of the initial evaluation and identifier-2), so it is MODIFIED; a MOVE's receiving operand is WRITTEN and its
/// sending operand and every subscript READ; SET condition-name TO TRUE stores into the conditional variable, so the condition-name is
/// WRITTEN; SET index-name UP BY and STRING's POINTER operand read the item and store into it (MODIFIED), and STRING's INTO operand is
/// WRITTEN. A name the program declares and never references is flagged; a PERFORM ... THRU is two
/// edges of the PERFORM graph. The source lines carry each reference (<see cref="ListingLine.Names"/>) and a statement line's margin the
/// lines that define the names it uses; the text and the page render exactly them.
/// </summary>
public sealed class ListingCrossReferenceTests : IDisposable
{
    private readonly string _dir = ScratchDirectory.Create("listingxref");

    public void Dispose() => ScratchDirectory.Remove(_dir);

    private static readonly string[] Source =
    [
        "       IDENTIFICATION DIVISION.",                                             // 1
        "       PROGRAM-ID. XREF.",                                                    // 2
        "       ENVIRONMENT DIVISION.",                                                // 3
        "       INPUT-OUTPUT SECTION.",                                                // 4
        "       FILE-CONTROL.",                                                        // 5
        "           SELECT F ASSIGN TO \"xref.dat\" ORGANIZATION SEQUENTIAL.",         // 6
        "       DATA DIVISION.",                                                       // 7
        "       FILE SECTION.",                                                        // 8
        "       FD F.",                                                                // 9
        "       01 F-REC PIC X(4).",                                                   // 10
        "       WORKING-STORAGE SECTION.",                                             // 11
        "       01 WS-A PIC 9(3) VALUE 1.",                                            // 12
        "       01 WS-B PIC 9(3) VALUE 2.",                                            // 13
        "       01 FLAG PIC X VALUE \"N\".",                                           // 14
        "          88 FLAG-ON VALUE \"Y\".",                                           // 15
        "       01 TBL.",                                                              // 16
        "          05 T-N PIC 9 OCCURS 9 INDEXED BY IX.",                              // 17
        "       01 S PIC X(10).",                                                      // 18
        "       01 P PIC 99 VALUE 1.",                                                 // 19
        "       01 NEVER PIC X.",                                                      // 20
        "       PROCEDURE DIVISION.",                                                  // 21
        "       MAIN-P.",                                                              // 22
        "           ADD WS-A",                                                         // 23
        "               TO WS-B.",                                                     // 24
        "           MOVE T-N (T-N (1)) TO T-N (2).",                                   // 25
        "           SET FLAG-ON TO TRUE.",                                             // 26
        "           IF FLAG-ON PERFORM P1 THRU P2 END-IF.",                            // 27
        "           SET IX UP BY 1.",                                                  // 28
        "           STRING \"AB\" DELIMITED BY SIZE INTO S WITH POINTER P.",           // 29
        "           OPEN INPUT F.",                                                    // 30
        "           READ F AT END CONTINUE END-READ.",                                  // 31
        "           CLOSE F.",                                                         // 32
        "           STOP RUN.",                                                        // 33
        "       P1.",                                                                  // 34
        "           DISPLAY WS-B.",                                                    // 35
        "       P2.",                                                                  // 36
        "           EXIT.",                                                            // 37
    ];

    private ListingDocument Compile()
    {
        string path = Path.Combine(_dir, "xref.cob");
        File.WriteAllText(path, string.Join("\n", Source));
        var result = CompiledProgramCache.CompileCold(new CompilerDriver.Options(path, EditionInfo.Of(2023), Path.Combine(_dir, "xref.dll"),
            Listing: new ListingRequest(Formats: ListingFormats.Text | ListingFormats.Html | ListingFormats.Json) { Sections = ListingSectionSet.All }));
        Assert.True(result.Success, string.Join("\n", result.Errors));
        var listing = Assert.IsType<ListingDocument>(result.Listing);
        ListingJsonContract.AssertHolds(listing);
        return listing;
    }

    /// <summary>The <paramref name="nth"/> (1-based) place <paramref name="name"/> is written on source line <paramref name="line"/>: its
    /// line and 1-based character position, read off the test's own text.</summary>
    private static (int? Line, int? Column) At(int line, string name, int nth = 1)
    {
        string text = Source[line - 1];
        int at = -1;
        for (int i = 0; i < nth; i++) at = text.IndexOf(name, at + 1, StringComparison.Ordinal);
        Assert.True(at >= 0, $"'{name}' is not written {nth} time(s) on line {line}");
        return (line, at + 1);
    }

    private static ListingSymbol Symbol(ListingCrossReference xref, string name) => Assert.Single(xref.Symbols, s => s.Name == name);

    private static List<(int? Line, int? Column, ListingUse Use)> Uses(ListingSymbol symbol) =>
        [.. symbol.References.Select(r => (r.Line, r.Column, r.Use))];

    [Fact]
    public void EachReference_StandsWhereItIsWritten_WithTheUseItsPositionGives()
    {
        var xref = Assert.IsType<ListingCrossReference>(Compile().CrossReference);
        // The definition stands where its entry does: a data description entry at its level-number (section 6.6).
        Assert.Equal([(At(12, "01").Line, At(12, "01").Column, ListingUse.Defined), (At(23, "WS-A").Line, At(23, "WS-A").Column, ListingUse.Read)],
            Uses(Symbol(xref, "WS-A")));
        // ADD WS-A / TO WS-B on two lines: each use on its own line; identifier-2 is read and stored.
        Assert.Equal([(13, At(13, "01").Column, ListingUse.Defined), (24, At(24, "WS-B").Column, ListingUse.Modified),
            (35, At(35, "WS-B").Column, ListingUse.Read)], Uses(Symbol(xref, "WS-B")));
        // MOVE T-N (T-N (1)) TO T-N (2): the sending operand and its subscript are read, the receiving operand written.
        Assert.Equal([(17, At(17, "05").Column, ListingUse.Defined), (25, At(25, "T-N", 1).Column, ListingUse.Read),
            (25, At(25, "T-N", 2).Column, ListingUse.Read), (25, At(25, "T-N", 3).Column, ListingUse.Written)], Uses(Symbol(xref, "T-N")));
        Assert.Equal([(15, At(15, "88").Column, ListingUse.Defined), (26, At(26, "FLAG-ON").Column, ListingUse.Written),
            (27, At(27, "FLAG-ON").Column, ListingUse.Read)], Uses(Symbol(xref, "FLAG-ON")));
        // SET index-name UP BY changes the index-name by the increment (§2023:14.9.39.4 GR3): the reference stands where it is written and
        // is MODIFIED, whatever holds the store (the index-name's cell is a field name, no place; section 6.7.1, "Store kind").
        Assert.Equal((28, At(28, "IX").Column, ListingUse.Modified), Uses(Symbol(xref, "IX"))[^1]);
        Assert.Equal((29, At(29, "INTO S").Column + 5, ListingUse.Written), Uses(Symbol(xref, "S"))[^1]);
        Assert.Equal((29, At(29, "POINTER P").Column + 8, ListingUse.Modified), Uses(Symbol(xref, "P"))[^1]);
        // A file's references carry their verb; a procedure's its transfer.
        // The FD's file-name is a reference to the file control entry's declaration (§2023:13.4.5.3 SR1), outside every statement.
        Assert.Equal([(9, (string?)null), (30, "OPEN"), (31, "READ"), (32, "CLOSE")],
            Symbol(xref, "F").References.Where(r => r.Use != ListingUse.Defined).Select(r => (r.Line ?? 0, r.Verb)));
        Assert.Equal([(27, (int?)At(27, "P1").Column, "PERFORM")],
            Symbol(xref, "P1").References.Where(r => r.Use != ListingUse.Defined).Select(r => (r.Line, r.Column, r.Verb)));
        Assert.True(Symbol(xref, "NEVER").Unreferenced);
        Assert.False(Symbol(xref, "WS-A").Unreferenced);
    }

    [Fact]
    public void ThePerformGraph_HasAnEdgePerProcedureAPerformNames_FromTheProcedureItIsWrittenIn()
    {
        var xref = Assert.IsType<ListingCrossReference>(Compile().CrossReference);
        int main = Symbol(xref, "MAIN-P").Id;
        Assert.Equal([(main, Symbol(xref, "P1").Id, 27), (main, Symbol(xref, "P2").Id, 27)],
            xref.Performs.Select(e => (e.From ?? 0, e.To, e.Line ?? 0)));
        Assert.Empty(xref.Calls);   // this program writes no CALL or CANCEL (ListingCallGraphTests holds the graph of one that does)
    }

    [Fact]
    public void ALinesNamesAndMargin_AreWhatTheCrossReferenceSaysOfIt_AndTheTextAndPageRenderThem()
    {
        var listing = Compile();
        var xref = listing.CrossReference!;
        foreach (var line in listing.Lines)
        {
            var expected = xref.Symbols.SelectMany(s => s.References.Where(r => r.Line == line.Number && r.Column is not null && r.Use != ListingUse.Defined)
                .Select(r => (r.Column!.Value, s.Id, r.Use))).OrderBy(n => n.Value).ToList();
            Assert.Equal(expected, line.Names.Select(n => (n.Column, n.Symbol, n.Use)).ToList());
        }
        // A statement line's margin: the lines that define the names it uses.
        Assert.Equal([12], listing.Lines[22].Margin!.Definitions);
        Assert.Equal([17], listing.Lines[24].Margin!.Definitions);
        Assert.Equal([15, 34, 36], listing.Lines[26].Margin!.Definitions.Order());
        string text = File.ReadAllText(Path.Combine(_dir, "xref.lst"));
        string row24 = Assert.Single(text.Split('\n'), r => r.StartsWith("    24 ", StringComparison.Ordinal));
        Assert.Equal(TextListingRenderer.DefinitionsMark + "13", row24[(TextListingRenderer.MarginColumn - 1)..]);
        // The page links each name on a source line to its definition's line.
        var page = XDocument.Parse(File.ReadAllText(Path.Combine(_dir, "xref.lst.html")));
        var row = page.Descendants().Single(e => (string?)e.Attribute("id") == HtmlListingRenderer.LineId(24));
        var link = Assert.Single(row.Descendants(), e => e.Name.LocalName == "a" && (string?)e.Attribute("class") == "nm");
        Assert.Equal("#" + HtmlListingRenderer.LineId(13), (string?)link.Attribute("href"));
        Assert.Equal("WS-B", link.Value);
        // The cross-reference section lists the symbol's references; its rows hold every symbol.
        Assert.Contains(page.Descendants(), e => (string?)e.Attribute("id") == HtmlListingRenderer.SymbolId(Symbol(xref, "WS-B").Id));
    }
}
