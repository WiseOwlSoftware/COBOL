// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol;
using WiseOwl.Cobol.Editions;
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

    private CompilerDriver.Result Compile(ListingSectionSet sections, IEnumerable<string>? source = null, int dialect = 2023)
    {
        string path = Path.Combine(_dir, "dmap.cob");
        File.WriteAllText(path, string.Join("\n", source ?? Source));
        var result = CompiledProgramCache.CompileCold(new CompilerDriver.Options(path, EditionInfo.Of(dialect), Path.Combine(_dir, "dmap.dll"), Listing: new ListingRequest(Formats: ListingFormats.Text | ListingFormats.Json) { Sections = sections }));
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

        // The file map: the SELECT entry's file, its organization, its access mode (the default: §2023:12.4.5.5.3 GR1, "If the
        // ACCESS MODE clause is not specified, sequential access is assumed") and its record.
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
            Assert.Equal(declared is null ? null : new ListingMargin(declared.Id, declared.Offset, declared.Length, declared.Usage, []), line.Margin);
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

    private static readonly ListingSectionSet DataMapSections =
        ListingSectionSet.Of(ListingSection.Source, ListingSection.Programs, ListingSection.DataMap);

    /// <summary>A TYPE clause's effect is "as though the data description identified by type-name-1 had been coded in place"
    /// (§2023:13.18.57.4 GR1), so each entry it copies stands in the program, on the line and at the offset of the entry that writes
    /// the clause; the type declaration's own entries are listed where they are written and, since "A type declaration has no
    /// storage associated with it" (§2023:13.18.58.4 GR2), with no offset, bytes or field, and their lines' margins print no
    /// offset (kb/Work PB5925 review round 1, finding 1).</summary>
    [Fact]
    public void ATypesCopies_StandWhereTheTypeClauseIsWritten_AndTheTypeDeclarationHasNoStorage()
    {
        string[] typed =
        [
            "       IDENTIFICATION DIVISION.",          // 1
            "       PROGRAM-ID. OUTER.",                // 2
            "       DATA DIVISION.",                    // 3
            "       WORKING-STORAGE SECTION.",          // 4
            "       01 T-REC IS TYPEDEF GLOBAL.",       // 5
            "          05 T-A PIC X(3).",               // 6
            "             88 T-A-YES VALUE \"YES\".",   // 7
            "          05 T-B PIC 9(4).",               // 8
            "       PROCEDURE DIVISION.",               // 9
            "           CALL \"INNER\".",               // 10
            "           STOP RUN.",                     // 11
            "       IDENTIFICATION DIVISION.",          // 12
            "       PROGRAM-ID. INNER.",                // 13
            "       DATA DIVISION.",                    // 14
            "       WORKING-STORAGE SECTION.",          // 15
            "       01 G.",                             // 16
            "          05 W-PAD PIC X(5).",             // 17
            "          05 W-IN TYPE T-REC.",            // 18
            "          05 W-IN2 TYPE T-REC.",           // 19
            "       PROCEDURE DIVISION.",               // 20
            "           MOVE \"YES\" TO T-A OF W-IN.",  // 21
            "           GOBACK.",                       // 22
            "       END PROGRAM INNER.",                // 23
            "       END PROGRAM OUTER.",                // 24
        ];
        var listing = Compile(DataMapSections, typed).Listing!;
        var map = Assert.IsType<ListingDataMap>(listing.DataMap);
        var programs = listing.Programs.ToDictionary(p => p.Name, p => p.Id);

        // The type declaration: OUTER's, on the lines written, no storage.
        var declared = map.Items.Where(i => i.Line is >= 5 and <= 8).ToList();
        Assert.Equal([("T-REC", 5), ("T-A", 6), ("T-A-YES", 7), ("T-B", 8)], declared.Select(i => (i.Name, i.Line ?? 0)));
        Assert.All(declared, i => Assert.Equal(programs["OUTER"], i.Program));
        // Every entry of it, its condition-name too, is part of the type declaration (kb/Work PB5925 review round 2, finding 2).
        Assert.All(declared, i => Assert.True(i.TypeDeclaration));
        Assert.All(declared.Where(i => i.Kind == ListingDataItemKind.Data), i =>
            Assert.Equal(((int?)null, (int?)null, (string?)null), (i.Offset, i.Bytes, i.NativeField)));
        // X(3) and 9(4): 3 and 4 positions (§2023:13.18.40.4 GR14), the type 7.
        Assert.Equal([7, 3, 4], declared.Where(i => i.Kind == ListingDataItemKind.Data).Select(i => i.Length ?? 0));
        Assert.All(listing.Lines.Where(l => l.Number is >= 5 and <= 8 && l.Margin is not null), l => Assert.Null(l.Margin!.Offset));

        // Each copy: INNER's, on its TYPE clause's line, owned by the entry that writes the clause, at the offset coded in place:
        // W-IN follows W-PAD's 5 positions and W-IN2 follows W-IN's 7, so W-IN's T-A is at 5 and T-B at 8, W-IN2's at 12 and 15.
        foreach (var (owner, line, start) in new[] { ("W-IN", 18, 5), ("W-IN2", 19, 12) })
        {
            var site = Item(map, owner);
            var copies = map.Items.Where(i => i.Line == line && i.Id != site.Id).ToList();
            Assert.Equal(["T-A", "T-A-YES", "T-B"], copies.Select(i => i.Name));
            Assert.All(copies, i => Assert.Equal(programs["INNER"], i.Program));
            Assert.All(copies, i => Assert.False(i.TypeDeclaration));
            Assert.Equal((site.Id, (int?)start, (int?)3), (copies[0].Owner ?? 0, copies[0].Offset, copies[0].Length));
            Assert.Equal(copies[0].Id, copies[1].Owner);
            Assert.Equal((site.Id, (int?)(start + 3), (int?)4), (copies[2].Owner ?? 0, copies[2].Offset, copies[2].Length));
            Assert.Equal(new ListingMargin(site.Id, site.Offset, site.Length, site.Usage, []), listing.Lines.Single(l => l.Number == line).Margin);
        }
    }

    /// <summary>The data map holds every declared name of every section (section 6.6; kb/Work PB5925 review round 1, findings 2 and
    /// 3): a report description entry and its report group description entries; and the file map holds each file's keys, the
    /// reports its REPORT clause names and its connector.</summary>
    [Fact]
    public void TheReportSectionAndTheFileKeys_AreInTheMaps()
    {
        string[] reported =
        [
            "       IDENTIFICATION DIVISION.",                                       // 1
            "       PROGRAM-ID. P5.",                                                // 2
            "       ENVIRONMENT DIVISION.",                                          // 3
            "       INPUT-OUTPUT SECTION.",                                          // 4
            "       FILE-CONTROL.",                                                  // 5
            "           SELECT IF1 ASSIGN TO \"IF1.DAT\"",                           // 6
            "               ORGANIZATION IS INDEXED ACCESS MODE IS DYNAMIC",         // 7
            "               RECORD KEY IS IR-KEY",                                   // 8
            "               ALTERNATE RECORD KEY IS IR-ALT WITH DUPLICATES.",        // 9
            "           SELECT RF1 ASSIGN TO \"RF1.DAT\"",                           // 10
            "               ORGANIZATION IS RELATIVE ACCESS MODE IS RANDOM",         // 11
            "               RELATIVE KEY IS W-RK.",                                  // 12
            "           SELECT RPTF ASSIGN TO \"RF.TXT\".",                          // 13
            "       DATA DIVISION.",                                                 // 14
            "       FILE SECTION.",                                                  // 15
            "       FD IF1.",                                                        // 16
            "       01 IR.",                                                         // 17
            "          05 IR-KEY PIC X(4).",                                         // 18
            "          05 IR-ALT PIC X(4).",                                         // 19
            "       FD RF1.",                                                        // 20
            "       01 RR PIC X(10).",                                               // 21
            "       FD RPTF REPORT IS RPT.",                                         // 22
            "       WORKING-STORAGE SECTION.",                                       // 23
            "       01 W-RK PIC 9(4).",                                              // 24
            "       01 W-N PIC 9(3) VALUE 5.",                                       // 25
            "       REPORT SECTION.",                                                // 26
            "       RD RPT.",                                                        // 27
            "       01 RL TYPE DETAIL LINE PLUS 1.",                                 // 28
            "          05 COLUMN 1 PIC 9(3) SOURCE W-N.",                            // 29
            "       PROCEDURE DIVISION.",                                            // 30
            "           STOP RUN.",                                                  // 31
        ];
        var listing = Compile(DataMapSections, reported).Listing!;
        var map = Assert.IsType<ListingDataMap>(listing.DataMap);

        // The report-name and its two report group description entries, in the order written, in the REPORT section; the column
        // entry defines a printable item of PICTURE 9(3), 3 positions (§2023:13.18.40.4 GR14); a report group entry is stored
        // nowhere of its own, so it has no offset, bytes or field.
        var rpt = Item(map, "RPT");
        Assert.Equal((ListingDataItemKind.Report, 27, "REPORT"), (rpt.Kind, rpt.Line ?? 0, rpt.Section));
        var group = map.Items.Where(i => i.Line is 28 or 29).ToList();
        Assert.Equal([("RL", 1, rpt.Id), ("FILLER", 5, group[0].Id)], group.Select(i => (i.Name, i.Level ?? 0, i.Owner ?? 0)));
        Assert.All(group, i => Assert.Equal("REPORT", i.Section));
        Assert.Equal(("9(3)", 3), (group[1].Picture, group[1].Length ?? 0));
        Assert.All(group, i => Assert.Equal(((int?)null, (int?)null, (string?)null), (i.Offset, i.Bytes, i.NativeField)));

        // The keys: "The RECORD KEY clause specifies the prime record key access path" (§2023:12.4.5.12.1); the WITH DUPLICATES
        // phrase is the alternate key's (§2023:12.4.5.6.4 GR4); the RELATIVE KEY data item; "The REPORT clause identifies the
        // reports that may be written to a report file" (§2023:13.18.46.1). Each file has a connector of its own.
        var if1 = Assert.Single(map.Files, f => f.Name == "IF1");
        Assert.Equal(("IR-KEY", false), (if1.RecordKey!.Name, if1.RecordKey.Duplicates));
        Assert.Equal([Item(map, "IR-KEY").Id], if1.RecordKey.Items);
        var alternate = Assert.Single(if1.AlternateKeys);
        Assert.Equal(("IR-ALT", true), (alternate.Name, alternate.Duplicates));
        Assert.Equal([Item(map, "IR-ALT").Id], alternate.Items);
        Assert.Null(if1.RelativeKey);
        Assert.Empty(if1.Reports);
        var rf1 = Assert.Single(map.Files, f => f.Name == "RF1");
        Assert.Equal((Item(map, "W-RK").Id, true, 0), (rf1.RelativeKey ?? 0, rf1.RecordKey is null, rf1.AlternateKeys.Count));
        Assert.Equal([rpt.Id], Assert.Single(map.Files, f => f.Name == "RPTF").Reports);
        Assert.All(map.Files, f => Assert.False(string.IsNullOrEmpty(f.Connector)));
        Assert.Equal(map.Files.Count, map.Files.Select(f => f.Connector).Distinct(StringComparer.Ordinal).Count());
        string page = string.Join("\n", ListingPages.Of(File.ReadAllText(Path.Combine(_dir, "dmap.lst")), ListingSection.DataMap));
        Assert.Contains($"RECORD KEY IR-KEY (#{Item(map, "IR-KEY").Id})  ALTERNATE KEY IR-ALT (#{Item(map, "IR-ALT").Id}) DUPLICATES",
            page, StringComparison.Ordinal);
    }

    /// <summary>A method's own data division is in the data map, in the method it is written in (section 6.6; kb/Work PB5925 review
    /// round 1, finding 2).</summary>
    [Fact]
    public void AMethodsOwnData_IsInTheDataMap()
    {
        string[] oo =
        [
            "       IDENTIFICATION DIVISION.",          // 1
            "       CLASS-ID. DMAPM INHERITS FROM BASE.", // 2
            "       ENVIRONMENT DIVISION.",             // 3
            "       CONFIGURATION SECTION.",            // 4
            "       REPOSITORY.",                       // 5
            "           CLASS BASE.",                   // 6
            "       IDENTIFICATION DIVISION.",          // 7
            "       OBJECT.",                           // 8
            "       PROCEDURE DIVISION.",               // 9
            "       IDENTIFICATION DIVISION.",          // 10
            "       METHOD-ID. M1.",                    // 11
            "       DATA DIVISION.",                    // 12
            "       LOCAL-STORAGE SECTION.",            // 13
            "       01 ML PIC X(4).",                   // 14
            "       LINKAGE SECTION.",                  // 15
            "       01 MK PIC 9(2).",                   // 16
            "       PROCEDURE DIVISION USING MK.",      // 17
            "           MOVE \"AB\" TO ML.",            // 18
            "       END METHOD M1.",                    // 19
            "       END OBJECT.",                       // 20
            "       END CLASS DMAPM.",                  // 21
        ];
        var listing = Compile(DataMapSections, oo).Listing!;
        var map = Assert.IsType<ListingDataMap>(listing.DataMap);
        int method = Assert.Single(listing.Programs, p => p.Kind == ListingProgramKind.Method).Id;
        var ml = Item(map, "ML");
        var mk = Item(map, "MK");
        // X(4) is 4 positions, 9(2) 2 (§2023:13.18.40.4 GR14).
        Assert.Equal((14, (int?)method, "LOCAL-STORAGE", 4), (ml.Line ?? 0, ml.Program, ml.Section, ml.Length ?? 0));
        Assert.Equal((16, (int?)method, "LINKAGE", 2), (mk.Line ?? 0, mk.Program, mk.Section, mk.Length ?? 0));
        // "The linkage section describes formal parameters and returning items" (§2023:13.7.1): their storage is the activating
        // element's, so the compiled method holds no field of its own for MK; ML is the method's own (kb/Work PB5925 review
        // round 2, finding 3).
        Assert.Equal(((string?)null, (string?)null), (mk.NativeField, mk.NativeType));
        Assert.NotNull(ml.NativeField);
    }

    /// <summary>A report group description entry is described by the item its data-name names (kb/Work PB5925 review round 2,
    /// finding 1): "If a data-name immediately follows the level number in the entry containing the SUM clause, the data-name is
    /// the name of the sum counter, not the name of the associated printable item" (§2023:13.18.54.4 GR5), and the sum counter
    /// "is a conceptual data item that behaves as a data item of the category numeric" whose digits are those of the entry's
    /// PICTURE (GR1), so it has that many positions and no PICTURE or USAGE of its own, printable or not; an entry that "contains
    /// no COLUMN clause and therefore defines an unprintable item" (§2023:13.18.53.4 GR3) is that elementary item.</summary>
    [Fact]
    public void AReportEntry_IsDescribedByTheItemItsDataNameNames()
    {
        string[] reported =
        [
            "       IDENTIFICATION DIVISION.",                                       // 1
            "       PROGRAM-ID. P11.",                                               // 2
            "       ENVIRONMENT DIVISION.",                                          // 3
            "       INPUT-OUTPUT SECTION.",                                          // 4
            "       FILE-CONTROL.",                                                  // 5
            "           SELECT RPTF ASSIGN TO \"RF.TXT\".",                          // 6
            "       DATA DIVISION.",                                                 // 7
            "       FILE SECTION.",                                                  // 8
            "       FD RPTF REPORT IS RPT.",                                         // 9
            "       WORKING-STORAGE SECTION.",                                       // 10
            "       01 W-N PIC 9(3) VALUE 5.",                                       // 11
            "       REPORT SECTION.",                                                // 12
            "       RD RPT.",                                                        // 13
            "       01 RL TYPE DETAIL LINE PLUS 1.",                                 // 14
            "          05 R-N COLUMN 1 PIC 9(3) SOURCE W-N.",                        // 15
            "          05 R-S COLUMN 10 PIC ZZZZ9 SUM W-N.",                         // 16
            "          05 R-U PIC S9(5) SUM W-N.",                                   // 17
            "          05 R-V PIC 9(4) SOURCE W-N.",                                 // 18
            "       PROCEDURE DIVISION.",                                            // 19
            "           DISPLAY R-U.",                                               // 20
            "           STOP RUN.",                                                  // 21
        ];
        var listing = Compile(DataMapSections, reported).Listing!;
        var map = Assert.IsType<ListingDataMap>(listing.DataMap);
        static (string?, string?, string?, int?) Described(ListingDataItem i) => (i.Category, i.Picture, i.Usage, i.Length);
        // 9(3) and 9(4): 3 and 4 positions (§2023:13.18.40.4 GR14); ZZZZ9 and S9(5): 5 digits each (GR1).
        Assert.Equal(("NUMERIC", "9(3)", "DISPLAY", 3), Described(Item(map, "R-N")));
        Assert.Equal(("NUMERIC", null, null, 5), Described(Item(map, "R-S")));
        Assert.Equal(("NUMERIC", null, null, 5), Described(Item(map, "R-U")));
        Assert.Equal(("NUMERIC", "9(4)", "DISPLAY", 4), Described(Item(map, "R-V")));
        Assert.Equal("GROUP", Item(map, "RL").Category);
        Assert.Equal(5, listing.Lines.Single(l => l.Number == 17).Margin!.Length);
    }

    /// <summary>An item the standard defines implicitly is in the data map, marked implicit: the CAPACITY IN register, which "shall
    /// be treated as though implicitly defined at the same level as the entry containing the OCCURS clause" (§2023:13.18.38.3
    /// SR30); a compiler temporary is no declared name and is not (section 6.6; kb/Work PB5925 review round 1, finding 5).</summary>
    [Fact]
    public void ACapacityRegister_IsInTheDataMap_MarkedImplicit_AtItsTablesLevel()
    {
        string[] dynamic =
        [
            "       IDENTIFICATION DIVISION.",                                  // 1
            "       PROGRAM-ID. CAPP.",                                         // 2
            "       DATA DIVISION.",                                            // 3
            "       WORKING-STORAGE SECTION.",                                  // 4
            "       01 G.",                                                     // 5
            "          05 T OCCURS DYNAMIC CAPACITY IN T-CAP PIC X(2).",        // 6
            "       PROCEDURE DIVISION.",                                       // 7
            "           DISPLAY T-CAP.",                                        // 8
            "           STOP RUN.",                                             // 9
        ];
        var map = Assert.IsType<ListingDataMap>(Compile(DataMapSections, dynamic).Listing!.DataMap);
        var t = Item(map, "T");
        var cap = Item(map, "T-CAP");
        Assert.Equal((6, t.Level, t.Owner, true), (cap.Line ?? 0, cap.Level, cap.Owner, cap.Implicit));
        Assert.Equal(["G", "T", "T-CAP"], map.Items.Select(i => i.Name));
        Assert.All(map.Items.Where(i => i.Name != "T-CAP"), i => Assert.False(i.Implicit));
    }

    /// <summary>A communication description entry declares its cd-name, and its record area is the implicit description the
    /// standard prints: "For each input CD entry, a record area of 87 contiguous character positions is allocated"
    /// (§1985:XIV.2.2.4 GR2), MESSAGE COUNT "an elementary data item whose implicit description is that of an integer of 6 digits"
    /// (§1985:XIV.2.2.4 GR2k), the area's last 6 of its 87 positions; the record written after it is one by which the area "may
    /// be implicitly redefined by user-specified record description entries" (§1985:XIV.2.1.2), so it is the CD's too (kb/Work
    /// PB5925 review round 1, the sibling sweep of findings 2 and 5).</summary>
    [Fact]
    public void ACommunicationDescription_IsInTheDataMap_WithItsImplicitArea()
    {
        string[] cd =
        [
            "       IDENTIFICATION DIVISION.",          // 1
            "       PROGRAM-ID. CDP.",                  // 2
            "       DATA DIVISION.",                    // 3
            "       WORKING-STORAGE SECTION.",          // 4
            "       01 W PIC X.",                       // 5
            "       COMMUNICATION SECTION.",            // 6
            "       CD CDI FOR INITIAL INPUT",          // 7
            "           SYMBOLIC QUEUE IS CQ",          // 8
            "           MESSAGE COUNT IS CMC.",         // 9
            "       01 CREC PIC X(87).",                // 10
            "       PROCEDURE DIVISION.",               // 11
            "       P0.",                               // 12
            "           STOP RUN.",                     // 13
        ];
        var map = Assert.IsType<ListingDataMap>(Compile(DataMapSections, cd, dialect: 85).Listing!.DataMap);
        var cdi = Item(map, "CDI");
        Assert.Equal((ListingDataItemKind.CommunicationDescription, 7, "COMMUNICATION"), (cdi.Kind, cdi.Line ?? 0, cdi.Section));
        Assert.Equal(["INITIAL INPUT"], cdi.Values);
        var area = map.Items.Where(i => i.Line == 7 && i.Kind == ListingDataItemKind.Data).ToList();
        Assert.All(area, i => Assert.True(i.Implicit));
        Assert.Equal((1, cdi.Id, 87), (area[0].Level ?? 0, area[0].Owner ?? 0, area[0].Length ?? 0));
        Assert.Equal((87 - 6, 6), (Item(map, "CMC").Offset ?? 0, Item(map, "CMC").Length ?? 0));
        Assert.Equal((0, 12), (Item(map, "CQ").Offset ?? -1, Item(map, "CQ").Length ?? 0));   // the area's first item, X(12) (GR2)
        var crec = Item(map, "CREC");
        Assert.Equal((cdi.Id, false), (crec.Owner ?? 0, crec.Implicit));
        Assert.False(Item(map, "W").Implicit);
    }
}
