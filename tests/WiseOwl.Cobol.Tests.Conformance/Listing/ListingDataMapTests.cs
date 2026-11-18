// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol;
using WiseOwl.Cobol.Listing;
using WiseOwl.Cobol.Listing.Text;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance.Listing;

/// <summary>
/// The data map, the file map, the program attributes and the margin (DESIGN-listing.md sections 6.2, 6.5 and 6.6; step 6 of
/// section 11), views of the finished symbol table, on a real compilation. Every expected value is derived from the source the
/// test writes and the standard's rules, never read off a listing: each size from the PICTURE character-string
/// (§2023:13.18.40.4 GR14: "Each symbol 'X' is counted in the size of the item", and "For usages display and national, each '9'
/// represents a character position"; §2023:13.18.40.3 SR6: "An unsigned nonzero integer that is enclosed in parentheses
/// indicates the number of consecutive occurrences of the symbol"), each offset as the sum of the sizes before it in its record,
/// a table's bytes as its occurrences times one occurrence's (§2023:13.18.38.4 GR4: "The value of integer-2 represents the fixed
/// number of occurrences of the subject of the entry"), a REDEFINES item's offset as its object's (§2023:13.18.44.4 GR1: "Storage
/// association for the subject of the entry starts at the first bit of the data item referenced by data-name-2") and a RENAMES
/// THRU item's extent from data-name-2's first elementary item to data-name-3's last (§2023:13.18.45.4 GR2: "data-name-1 defines
/// an alphanumeric group item that includes all elementary items starting with data-name-2").
/// </summary>
public sealed class ListingDataMapTests : IDisposable
{
    private readonly string _dir = ScratchDirectory.Create("listingdatamap");

    public void Dispose() => ScratchDirectory.Remove(_dir);

    /// <summary>One program with every kind of name the data map holds: a file and its record, a constant, a record of a group, a
    /// table with an index-name and a condition-name, a REDEFINES and a RENAMES THRU entry, and a nested program.</summary>
    private static readonly string[] Source =
    [
        "       IDENTIFICATION DIVISION.",                                     // 1
        "       PROGRAM-ID. DMAP IS INITIAL.",                                 // 2
        "       ENVIRONMENT DIVISION.",                                        // 3
        "       INPUT-OUTPUT SECTION.",                                        // 4
        "       FILE-CONTROL.",                                                // 5
        "           SELECT F ASSIGN TO \"dmap.dat\" ORGANIZATION SEQUENTIAL.", // 6
        "       DATA DIVISION.",                                               // 7
        "       FILE SECTION.",                                                // 8
        "       FD F.",                                                        // 9
        "       01 F-REC PIC X(20).",                                          // 10
        "       WORKING-STORAGE SECTION.",                                     // 11
        "       01 K CONSTANT AS 5.",                                          // 12
        "       01 REC.",                                                      // 13
        "          05 A PIC X(3).",                                            // 14
        "          05 T OCCURS 3 INDEXED BY IX.",                              // 15
        "             10 T-X PIC X(2).",                                       // 16
        "             10 T-N PIC 9(2).",                                       // 17
        "                88 T-OK VALUE 1 THRU 9.",                             // 18
        "          05 C PIC 9(4).",                                            // 19
        "          05 C-R REDEFINES C PIC X(4).",                              // 20
        "       66 R-ALIAS RENAMES A THRU C.",                                 // 21
        "       PROCEDURE DIVISION.",                                          // 22
        "           MOVE K TO C.",                                             // 23
        "           GOBACK.",                                                  // 24
        "       IDENTIFICATION DIVISION.",                                     // 25
        "       PROGRAM-ID. INNER.",                                           // 26
        "       DATA DIVISION.",                                               // 27
        "       WORKING-STORAGE SECTION.",                                     // 28
        "       01 W PIC X(7).",                                               // 29
        "       PROCEDURE DIVISION.",                                          // 30
        "           GOBACK.",                                                  // 31
        "       END PROGRAM INNER.",                                           // 32
        "       END PROGRAM DMAP.",                                            // 33
    ];

    private CompilerDriver.Result Compile(ListingSectionSet sections, IEnumerable<string>? source = null)
    {
        string path = Path.Combine(_dir, "dmap.cob");
        File.WriteAllText(path, string.Join("\n", source ?? Source));
        var result = CompiledProgramCache.CompileCold(new CompilerDriver.Options(path, Path.Combine(_dir, "dmap.dll"),
            DialectLevel: 2023, Listing: new ListingRequest(Formats: ListingFormats.Text | ListingFormats.Json) { Sections = sections }));
        if (result.Listing is { } listing) ListingJsonContract.AssertHolds(listing);
        return result;
    }

    private static ListingDataItem Item(ListingDataMap map, string name) => Assert.Single(map.Items, i => i.Name == name);

    /// <summary>Every name, in the order written, each on its own line with its level and kind; each size and offset as the
    /// PICTURE character-strings and the clauses give them (the class summary's rules).</summary>
    [Fact]
    public void TheDataMap_HoldsEveryName_InTheOrderWritten_WithTheSizesThePicturesGive()
    {
        var result = Compile(ListingSectionSet.Of(ListingSection.Source, ListingSection.Programs, ListingSection.DataMap));
        Assert.Equal(CompilerDriver.Outcome.Success, result.Status);
        var map = Assert.IsType<ListingDataMap>(result.Listing!.DataMap);
        Assert.Contains(result.Listing.Sections, s => s is { Section: ListingSection.DataMap, Produced: true });

        // The order written (the data division, then the nested program's), with each declaration's line.
        Assert.Equal(
            [("F-REC", 10), ("K", 12), ("REC", 13), ("A", 14), ("T", 15), ("IX", 15), ("T-X", 16), ("T-N", 17), ("T-OK", 18),
             ("C", 19), ("C-R", 20), ("R-ALIAS", 21), ("W", 29)],
            map.Items.Select(i => (i.Name, i.Line ?? 0)));
        Assert.Equal([ListingDataItemKind.Constant], map.Items.Where(i => i.Name == "K").Select(i => i.Kind));
        Assert.Equal(ListingDataItemKind.Index, Item(map, "IX").Kind);
        Assert.Equal(ListingDataItemKind.Condition, Item(map, "T-OK").Kind);

        // Sizes: X(3) is 3, X(2) 2, 9(2) 2, 9(4) 4 (GR14, SR6); one occurrence of T is 2 + 2 = 4 and its three occurrences 12
        // (GR4); REC is 3 + 12 + 4 = 19, C-R adding nothing (it shares C's storage, GR1).
        Assert.Equal((0, 3, 3), Sizes(Item(map, "A")));
        Assert.Equal((3, 4, 12), Sizes(Item(map, "T")));
        Assert.Equal((3, 2, 2), Sizes(Item(map, "T-X")));   // the first occurrence's
        Assert.Equal((5, 2, 2), Sizes(Item(map, "T-N")));
        Assert.Equal((15, 4, 4), Sizes(Item(map, "C")));
        Assert.Equal((15, 4, 4), Sizes(Item(map, "C-R")));
        Assert.Equal((0, 19, 19), Sizes(Item(map, "REC")));
        Assert.Equal((0, 20, 20), Sizes(Item(map, "F-REC")));
        // R-ALIAS spans A's first position through C's last: 0 to 19 (§2023:13.18.45.4 GR2).
        Assert.Equal((0, 19), (Item(map, "R-ALIAS").Offset, Item(map, "R-ALIAS").Length));
        Assert.Equal((7, 7), (Item(map, "W").Length, Item(map, "W").Bytes));

        // The levels and the links between the entries.
        Assert.Equal([1, 1, 5, 5, 10, 10, 88, 5, 5, 66], new[] { "F-REC", "REC", "A", "T", "T-X", "T-N", "T-OK", "C", "C-R", "R-ALIAS" }
            .Select(n => Item(map, n).Level ?? 0));
        Assert.Equal(Item(map, "REC").Id, Item(map, "A").Owner);
        Assert.Equal(Item(map, "T").Id, Item(map, "T-X").Owner);
        Assert.Equal(Item(map, "T-N").Id, Item(map, "T-OK").Owner);
        Assert.Equal(Item(map, "T").Id, Item(map, "IX").Owner);
        Assert.Equal([Item(map, "IX").Id], Item(map, "T").Indexes);
        Assert.Equal((3, 3), (Item(map, "T").OccursMin, Item(map, "T").OccursMax));
        Assert.Equal(Item(map, "C").Id, Item(map, "C-R").Redefines);
        Assert.Equal((Item(map, "A").Id, Item(map, "C").Id), (Item(map, "R-ALIAS").RenamesFrom, Item(map, "R-ALIAS").RenamesThru));
        Assert.Equal(["1 THRU 9"], Item(map, "T-OK").Values);
        Assert.Equal(["5"], Item(map, "K").Values);

        // Usages, pictures, categories and sections as written.
        Assert.Equal(("DISPLAY", "X(3)", "ALPHANUMERIC", "WORKING-STORAGE"),
            (Item(map, "A").Usage, Item(map, "A").Picture, Item(map, "A").Category, Item(map, "A").Section));
        Assert.Equal(("9(4)", "NUMERIC"), (Item(map, "C").Picture, Item(map, "C").Category));
        Assert.Equal(("GROUP", "FILE"), (Item(map, "REC").Category, Item(map, "F-REC").Section));

        // Each data item and index-name names the field and the .NET type it became; a RENAMES alias owns no field.
        Assert.All(new[] { "A", "T", "T-X", "C", "REC", "IX", "W" }, n =>
        {
            Assert.False(string.IsNullOrEmpty(Item(map, n).NativeField));
            Assert.False(string.IsNullOrEmpty(Item(map, n).NativeType));
        });
        Assert.Null(Item(map, "R-ALIAS").NativeField);

        // The program of each declaration is the innermost program its line is in (section 6.5).
        var programs = result.Listing.Programs.ToDictionary(p => p.Name, p => p.Id);
        Assert.Equal(programs["INNER"], Item(map, "W").Program);
        Assert.All(map.Items.Where(i => i.Name != "W"), i => Assert.Equal(programs["DMAP"], i.Program));

        // The file map: the SELECT entry's file, its organization, its access mode (the default, §2023:12.4.5.3) and its record.
        var file = Assert.Single(map.Files);
        Assert.Equal(("F", 6, "SEQUENTIAL", "SEQUENTIAL", false), (file.Name, file.Line ?? 0, file.Organization, file.Access, file.SortMerge));
        Assert.Equal([Item(map, "F-REC").Id], file.Records);
    }

    private static (int?, int?, int?) Sizes(ListingDataItem item) => (item.Offset, item.Length, item.Bytes);

    /// <summary>Section 6.5: a bound program's attributes join the program map: DMAP is INITIAL (its PROGRAM-ID says so), INNER
    /// is not; each is externalized under its own name (no AS phrase) and names the .NET type it became.</summary>
    [Fact]
    public void TheProgramMap_CarriesEachBoundProgramsAttributes()
    {
        var listing = Compile(ListingSectionSet.Of(ListingSection.Programs)).Listing!;
        var dmap = Assert.Single(listing.Programs, p => p.Name == "DMAP").Attributes!;
        var inner = Assert.Single(listing.Programs, p => p.Name == "INNER").Attributes!;
        Assert.Equal((true, false, false, false, "DMAP"), (dmap.Initial, dmap.Common, dmap.Recursive, dmap.Prototype, dmap.ExternalizedName));
        Assert.Equal((false, "INNER"), (inner.Initial, inner.ExternalizedName));
        Assert.NotEqual(dmap.NativeType, inner.NativeType);
        string page = Assert.Single(ListingPages.Of(File.ReadAllText(Path.Combine(_dir, "dmap.lst")), ListingSection.Programs));
        Assert.Contains("INITIAL  AS 'DMAP'  .NET " + dmap.NativeType, page, StringComparison.Ordinal);
    }

    /// <summary>Section 6.2: each line that declares a data item carries its offset, length and usage in the margin, as the data map
    /// holds them, and the text listing prints exactly them from column 100; a line that declares none has no margin.</summary>
    [Fact]
    public void EachDefinitionLine_CarriesTheDataMapsOffsetLengthAndUsage_InTheMargin()
    {
        var listing = Compile(ListingSectionSet.Of(ListingSection.Source, ListingSection.DataMap)).Listing!;
        var map = listing.DataMap!;
        foreach (var line in listing.Lines)
        {
            var declared = map.Items.FirstOrDefault(i => i.Kind == ListingDataItemKind.Data && i.Line == line.Number);
            Assert.Equal(declared is null ? null : new ListingMargin(declared.Id, declared.Offset, declared.Length, declared.Usage), line.Margin);
        }
        string text = File.ReadAllText(Path.Combine(_dir, "dmap.lst"));
        string row = Assert.Single(ListingPages.Of(text, ListingSection.Source).SelectMany(p => p.Split('\n')),
            r => r.Contains("05 A PIC X(3).", StringComparison.Ordinal));
        Assert.Equal("OFF=0 LEN=3 DISPLAY", row[(TextListingRenderer.MarginColumn - 1)..]);
        Assert.Contains("DATA MAP", text, StringComparison.Ordinal);
    }

    /// <summary>Section 4.3: a bind that refuses the program makes no symbol table, so the data map is not produced and says why;
    /// the source is still listed.</summary>
    [Fact]
    public void ABindError_LeavesTheDataMapNotProduced_SayingWhy()
    {
        string[] refused = [.. Source.Take(22), "           MOVE NOWHERE TO C.", .. Source.Skip(23)];
        var result = Compile(ListingSectionSet.Of(ListingSection.Source, ListingSection.DataMap), refused);
        Assert.Equal(CompilerDriver.Outcome.BindError, result.Status);
        Assert.Null(result.Listing!.DataMap);
        Assert.Contains(result.Listing.Sections, s => s is { Section: ListingSection.DataMap, Produced: false, Reason: "the bind refused the program" });
        Assert.Contains("Not produced: the bind refused the program",
            Assert.Single(ListingPages.Of(File.ReadAllText(Path.Combine(_dir, "dmap.lst")), ListingSection.DataMap)), StringComparison.Ordinal);
    }

    /// <summary>Section 6.6: a class's OBJECT data is in the data map too, placed in the object definition it is written in.</summary>
    [Fact]
    public void AClassesObjectData_IsInTheDataMap_InItsObjectDefinition()
    {
        string[] oo =
        [
            "       IDENTIFICATION DIVISION.",          // 1
            "       CLASS-ID. DMAPC INHERITS FROM BASE.", // 2
            "       ENVIRONMENT DIVISION.",             // 3
            "       CONFIGURATION SECTION.",            // 4
            "       REPOSITORY.",                       // 5
            "           CLASS BASE.",                   // 6
            "       IDENTIFICATION DIVISION.",          // 7
            "       OBJECT.",                           // 8
            "       DATA DIVISION.",                    // 9
            "       WORKING-STORAGE SECTION.",          // 10
            "       01 OW PIC X(6).",                   // 11
            "       PROCEDURE DIVISION.",               // 12
            "       END OBJECT.",                       // 13
            "       END CLASS DMAPC.",                  // 14
        ];
        var listing = Compile(ListingSectionSet.Of(ListingSection.Source, ListingSection.Programs, ListingSection.DataMap), oo).Listing!;
        var ow = Item(Assert.IsType<ListingDataMap>(listing.DataMap), "OW");
        Assert.Equal((11, 0, 6, 6), (ow.Line ?? 0, ow.Offset ?? -1, ow.Length ?? 0, ow.Bytes ?? 0));   // X(6): GR14, SR6
        Assert.Equal(Assert.Single(listing.Programs, p => p.Kind == ListingProgramKind.Object).Id, ow.Program);
    }

    /// <summary>Section 9: a listing that asks for no section of the symbol table makes no symbol table.</summary>
    [Fact]
    public void AListingOfNoSymbolTableSection_HoldsNoDataMap()
    {
        var listing = Compile(ListingSectionSet.Of(ListingSection.Source)).Listing!;
        Assert.Null(listing.DataMap);
        Assert.All(listing.Lines, l => Assert.Null(l.Margin));
    }
}
