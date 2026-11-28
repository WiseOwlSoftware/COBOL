// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Compiler;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Compiler.Symbols;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Listing;

/// <summary>
/// ⛔ EVERY STORE OCCURRENCE CARRIES ITS TRUE STORE KIND, WHATEVER HOLDS THE STORE (docs/rearchitecture/DESIGN-listing.md sections 6.7
/// and 6.7.1, "Store kind", kb/Work PB5925 step 7b review round 1, R1-1). A store position stands at a SITE the bound tree holds: a
/// <c>Place</c>, or, for an index-name (whose cell is a field name, no place), the receiver node itself; and a whole-table store (a table
/// SORT, INITIALIZE of a whole dynamic table) at the whole-table place its resolver built. <c>BoundStores.StorePositionsOf</c> asks every
/// site the tree holds and every site the table linked, so a store position with NO linked occurrence is still seen, and the pairing
/// (<c>EntryStore.UnpairedStorePositions</c>) is red for it. The NIST corpus's 1985 verbs cannot show the 2002-and-later stores, so this
/// planted program does: each statement's expected use is written from the general rules of its verb.
/// <list type="bullet">
/// <item>SET index-name UP BY sets the index-name to the value its occurrence number incremented by the increment gives (§2023:14.9.39.4
/// GR4b): MODIFIED; SET index-name TO sets it to the value the sending operand gives (GR2): WRITTEN. A PERFORM's VARYING phrase augments the
/// indexes it names (§2023:14.9.28.4 GR12): MODIFIED; its UNTIL condition reads it. SEARCH's VARYING index-name is the index the search
/// varies (§2023:14.9.37.4 GR3): MODIFIED, whether it is an index of the searched table or another table's. A table SORT places the sorted
/// elements in the table (§2023:14.9.40.4 GR24), so the table and each key data-name it writes are MODIFIED; INITIALIZE's identifier-1 is its
/// receiving operand (§2023:14.9.20.3 SR7), and a whole OCCURS DYNAMIC table is stored into whole: WRITTEN.</item>
/// </list>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class ReferenceStoreKindDriftTests : WiseOwlCobolTestBase
{
    private static readonly string[] Source =
    [
        "       IDENTIFICATION DIVISION.",                                          // 1
        "       PROGRAM-ID. STORES.",                                               // 2
        "       DATA DIVISION.",                                                    // 3
        "       WORKING-STORAGE SECTION.",                                          // 4
        "       01 TBL.",                                                           // 5
        "          05 TE OCCURS 9 INDEXED BY IX IY.",                              // 6
        "             10 K PIC 9.",                                                 // 7
        "             10 V PIC 9.",                                                 // 8
        "       01 OTH.",                                                         // 9
        "          05 OE PIC 9 OCCURS 9 INDEXED BY JX.",                            // 10
        "       01 DYN.",                                                           // 11
        "          05 DT PIC 9 OCCURS DYNAMIC CAPACITY IN DC.",                     // 12
        "       PROCEDURE DIVISION.",                                               // 13
        "       MAIN-P.",                                                           // 14
        "           SET IX UP BY 1.",                                               // 15
        "           SET IY TO 3.",                                                  // 16
        "           PERFORM P1 VARYING IX FROM 1 BY 1 UNTIL IX > 3.",               // 17
        "           SEARCH TE VARYING IX WHEN V (IX) = 1 CONTINUE END-SEARCH.",     // 18
        "           SEARCH TE VARYING JX WHEN V (IX) = 2 CONTINUE END-SEARCH.",     // 19
        "           SORT TE ON ASCENDING KEY K.",                                   // 20
        "           INITIALIZE DT.",                                                // 21
        "           STOP RUN.",                                                     // 22
        "       P1.",                                                               // 23
        "           EXIT.",                                                         // 24
    ];

    private (List<(SymbolOccurrence Occurrence, NameClass Class, string Name)> Occurrences, List<string> Findings) Bind()
    {
        string path = Path.Combine(TempDir, "stores.cob");
        File.WriteAllText(path, string.Join("\n", Source));
        var options = new CompilerDriver.Options(path, EditionInfo.Of(2023), Path.Combine(TempDir, "stores.dll"));
        var (_, occurrences, _, _, errors) = ReferenceOccurrenceDriftTests.Bind(options);
        Assert.True(errors.Count == 0, string.Join("\n", errors));
        return (occurrences, ReferenceOccurrenceDriftTests.Findings(options));
    }

    /// <summary>The uses of <paramref name="name"/>'s occurrences, each with its line, in the order written.</summary>
    private static List<(int Line, SymbolUse Use)> Uses(List<(SymbolOccurrence Occurrence, NameClass Class, string Name)> all, string name) =>
        [.. all.Where(o => o.Name == name).OrderBy(o => (o.Occurrence.At.Line, o.Occurrence.At.Column)).Select(o => (o.Occurrence.At.Line, o.Occurrence.Use))];

    [Fact]
    public void EveryIndexNameAndWholeTableStore_IsClassifiedByItsPosition_AndPairsWithItsOccurrence()
    {
        var (all, findings) = Bind();
        Assert.Empty(findings);   // no store position stands at a site linked to no occurrence
        // IX: SET UP BY (15), PERFORM VARYING and its UNTIL (17), SEARCH VARYING and the subscript in its WHEN (18), the subscript in 19's WHEN.
        Assert.Equal([(15, SymbolUse.Modified), (17, SymbolUse.Modified), (17, SymbolUse.Read), (18, SymbolUse.Modified),
            (18, SymbolUse.Read), (19, SymbolUse.Read)], Uses(all, "IX"));
        Assert.Equal([(16, SymbolUse.Written)], Uses(all, "IY"));
        Assert.Equal([(19, SymbolUse.Modified)], Uses(all, "JX"));
        // SORT TE ON ASCENDING KEY K: the table and its key are put back in order. SEARCH reads TE, and V in its WHEN.
        Assert.Equal([(18, SymbolUse.Read), (19, SymbolUse.Read), (20, SymbolUse.Modified)], Uses(all, "TE"));
        Assert.Equal([(20, SymbolUse.Modified)], Uses(all, "K"));
        Assert.Equal([(21, SymbolUse.Written)], Uses(all, "DT"));
    }

    /// <summary>The next whole-table store is linked the day it is written: a statement that names a whole table (SEARCH, a table SORT and
    /// its keys, INITIALIZE of a dynamic table) asks <c>ReferenceResolver.WholeTable</c>, which links the place to the occurrence at the
    /// reference's head, and never <c>PlaceBuilder.WholeTable</c>, which builds an unlinked one. Red on any other caller.</summary>
    [Fact]
    public void NoWholeTableIsBuiltOutsideTheResolver()
    {
        var offenders = new List<string>();
        foreach (string file in Directory.EnumerateFiles(TestRepo.Src("WiseOwl.Cobol.Compiler"), "*.cs", SearchOption.AllDirectories))
        {
            string name = Path.GetFileName(file);
            if (name is "PlaceBuilder.cs" or "ReferenceResolver.cs") continue;
            foreach (var (line, number) in File.ReadLines(file).Select((l, i) => (l, i + 1)))
                if (!line.TrimStart().StartsWith("//", StringComparison.Ordinal) && line.Contains(".Places.WholeTable(", StringComparison.Ordinal))
                    offenders.Add($"{name}:{number}");
        }
        Assert.True(offenders.Count == 0, "PlaceBuilder.WholeTable called outside the resolver (use ReferenceResolver.WholeTable): " + string.Join(", ", offenders));
    }

    /// <summary>No listing asked, no listing work (section 9): an identification funnel records through <c>SymbolTable.RecordData</c>, which
    /// asks the entry of a data-name or condition-name candidate (<c>DataEntryOf</c>, which fails loudly for a candidate with none) only
    /// when the group records references. Red on a call of <c>DataEntryOf</c> in the table's files outside the guard and the entry's own
    /// declaration (R1-3).</summary>
    [Fact]
    public void NoFunnelAsksADataEntry_ExceptThroughTheGuardedRecord()
    {
        var offenders = new List<string>();
        foreach (string file in Directory.EnumerateFiles(TestRepo.Src("WiseOwl.Cobol.Compiler", "Binding", "SymbolTable"), "SymbolTable.*.cs"))
        {
            string name = Path.GetFileName(file);
            foreach (var (line, number) in File.ReadLines(file).Select((l, i) => (l, i + 1)))
                if (!line.TrimStart().StartsWith("//", StringComparison.Ordinal) && line.Contains("DataEntryOf(", StringComparison.Ordinal)
                    && !line.Contains("internal SymbolId DataEntryOf(", StringComparison.Ordinal)
                    && !(name == "SymbolTable.Occurrences.cs" && line.Contains("Record(DataEntryOf(candidate)", StringComparison.Ordinal))
                    && !(name == "SymbolTable.DataNameCandidates.cs" && line.Contains("_entries.Record(DataEntryOf(one)", StringComparison.Ordinal)))
                    offenders.Add($"{name}:{number}");
        }
        Assert.True(offenders.Count == 0, "DataEntryOf asked outside SymbolTable.RecordData: " + string.Join(", ", offenders));
    }
}
