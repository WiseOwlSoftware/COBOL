// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol;
using WiseOwl.Cobol.Listing;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance.Listing;

/// <summary>
/// ⛔ THE CALL GRAPH OF A PROGRAM THAT CALLS BY LITERAL (docs/rearchitecture/DESIGN-listing.md sections 6.7 and 6.7.1, "The program-name
/// literal"; kb/Work PB5925 step 7b review round 1, R1-2). At COBOL-85 every CALL and CANCEL names its program by literal-1 or by an
/// identifier (§1985:X.5.2.3 SR1, §1985:X.5.3.3 SR1: "Literal-1 must be a nonnumeric literal"), so a
/// literal target is the reference the graph is made of: the occurrence stands at the literal, of the entry of the program it names, the
/// contained SUB of this compilation here, and, where the group defines no such program, of the external program of that name. The
/// expected edges and positions are written from the program's own source lines below, at <c>--std 85</c>.
/// </summary>
public sealed class ListingCallGraphTests : IDisposable
{
    private readonly string _dir = ScratchDirectory.Create("listingcalls");

    public void Dispose() => ScratchDirectory.Remove(_dir);

    private static readonly string[] Source =
    [
        "       IDENTIFICATION DIVISION.",                      // 1
        "       PROGRAM-ID. MAINP.",                            // 2
        "       DATA DIVISION.",                                // 3
        "       WORKING-STORAGE SECTION.",                      // 4
        "       01 X PIC 9 VALUE 1.",                           // 5
        "       PROCEDURE DIVISION.",                           // 6
        "       MAIN-P.",                                       // 7
        "           CALL \"SUB\" USING X.",                     // 8
        "           CALL \"EXTP\".",                            // 9
        "           CANCEL \"SUB\".",                           // 10
        "           STOP RUN.",                                 // 11
        "       IDENTIFICATION DIVISION.",                      // 12
        "       PROGRAM-ID. SUB.",                              // 13
        "       DATA DIVISION.",                                // 14
        "       LINKAGE SECTION.",                              // 15
        "       01 Y PIC 9.",                                   // 16
        "       PROCEDURE DIVISION USING Y.",                   // 17
        "       SUB-P.",                                        // 18
        "           EXIT PROGRAM.",                             // 19
        "       END PROGRAM SUB.",                              // 20
        "       END PROGRAM MAINP.",                            // 21
    ];

    private ListingCrossReference Compile()
    {
        string path = Path.Combine(_dir, "calls.cob");
        File.WriteAllText(path, string.Join("\n", Source));
        var result = CompiledProgramCache.CompileCold(new CompilerDriver.Options(path, Path.Combine(_dir, "calls.dll"), DialectLevel: 85,
            Listing: new ListingRequest(Formats: ListingFormats.Text | ListingFormats.Json) { Sections = ListingSectionSet.All }));
        Assert.True(result.Success, string.Join("\n", result.Errors));
        var listing = Assert.IsType<ListingDocument>(result.Listing);
        ListingJsonContract.AssertHolds(listing);
        return Assert.IsType<ListingCrossReference>(listing.CrossReference);
    }

    private static ListingSymbol Symbol(ListingCrossReference xref, string name) => Assert.Single(xref.Symbols, s => s.Name == name);

    /// <summary>The 1-based column the <paramref name="text"/> is written at on <paramref name="line"/>.</summary>
    private static int Column(int line, string text) => Source[line - 1].IndexOf(text, StringComparison.Ordinal) + 1;

    [Fact]
    public void ALiteralTarget_IsAnEdgeToTheProgramItNames_ContainedOrExternal()
    {
        var xref = Compile();
        int main = Symbol(xref, "MAINP").Id, sub = Symbol(xref, "SUB").Id, external = Symbol(xref, "EXTP").Id;
        Assert.Equal([(main, sub, 8, "CALL"), (main, external, 9, "CALL"), (main, sub, 10, "CANCEL")],
            xref.Calls.Select(e => (e.From ?? 0, e.To, e.Line ?? 0, e.Verb)));
        // The two literals, and the END PROGRAM header's name, which references the program it ends (§2023:8.4.6.3: a program-name may be referenced by the end program marker): no verb, no edge.
        Assert.Equal([(8, Column(8, "\"SUB\""), 5, "CALL"), (10, Column(10, "\"SUB\""), 5, "CANCEL"), (20, Column(20, "SUB"), 3, "")],
            Symbol(xref, "SUB").References.Where(r => r.Use != ListingUse.Defined).Select(r => (r.Line ?? 0, r.Column ?? 0, r.Length, r.Verb ?? "")));
        Assert.False(Symbol(xref, "SUB").Unreferenced);   // the contained program is called, so it is no unreferenced definition
        // A program the group defines nowhere is the external program of that name: its kind says so, it has no definition, and it is no
        // "defined and never referenced" symbol.
        var ext = Symbol(xref, "EXTP");
        Assert.Equal(ListingSymbolKind.ExternalProgram, ext.Kind);
        Assert.DoesNotContain(ext.References, r => r.Use == ListingUse.Defined);
        Assert.Equal([(9, Column(9, "\"EXTP\""), 6, "CALL")], ext.References.Select(r => (r.Line ?? 0, r.Column ?? 0, r.Length, r.Verb ?? "")));
        Assert.False(ext.Unreferenced);
    }

    [Fact]
    public void TheTextCrossReference_PrintsTheCallGraphRows()
    {
        Compile();
        string[] lines = File.ReadAllLines(Path.Combine(_dir, "calls.lst"));
        int start = Array.FindIndex(lines, l => l.TrimEnd() == "CALL GRAPH");
        Assert.True(start >= 0, "the CALL GRAPH heading is printed");
        var rows = lines[(start + 2)..].Select(l => l.Split(' ', StringSplitOptions.RemoveEmptyEntries)).Where(p => p.Length >= 4 && p[2] is "CALL" or "CANCEL").ToList();
        Assert.Equal(["MAINP 8 CALL SUB", "MAINP 9 CALL EXTP", "MAINP 10 CANCEL SUB"], rows.Select(p => string.Join(' ', p[0], p[1], p[2], p[3])));
    }
}
