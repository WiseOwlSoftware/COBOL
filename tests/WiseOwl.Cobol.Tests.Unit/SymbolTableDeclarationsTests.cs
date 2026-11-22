// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Frontend.Diagnostics;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;
using CnFrontend = WiseOwl.Cobol.Frontend.Frontend;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// The declaration questions listing step 6b's remainder slice 4 moved into <see cref="SymbolTable"/>
/// (docs/rearchitecture/DESIGN-listing.md section 6.7.1, "Step 6b, remainder slice 4"; kb/Work PB5932). Each compares
/// names by the table's one equality: two spellings are one word when they differ only in the case of a basic letter,
/// "Equivalence of uppercase and lowercase basic letters is achieved by folding from uppercase to lowercase in accordance
/// with the case mapping described in Annex C" (§2023:8.1.3.2 GR3). Every expected value below is that rule applied to the
/// inputs written here, never a captured answer.
/// </summary>
public sealed class SymbolTableDeclarationsTests
{
    /// <summary>§2023:8.3.2.2: "Within a source element, a given user-defined word may be used as only one type of user-defined
    /// word". The census answers the first type when a second, disjoint type is recorded for the same word in another case, and
    /// answers nothing for a third record, so one conflict draws one refusal.</summary>
    [Fact]
    public void Census_FoldsCase_AndReportsAConflictOnce()
    {
        var census = new SymbolTable.UserWordCensus();
        Assert.Null(census.Record("Total-A", UserWordKind.ParagraphName, Editions.EditionInfo.Latest));
        Assert.Equal(UserWordKind.ParagraphName, census.Record("TOTAL-A", UserWordKind.DataName, Editions.EditionInfo.Latest));
        Assert.Null(census.Record("total-a", UserWordKind.DataName, Editions.EditionInfo.Latest));
        Assert.True(census.IsDeclaredAs("TOTAL-a", UserWordKind.ParagraphName));
        Assert.False(census.IsDeclaredAs("OTHER", UserWordKind.ParagraphName));
    }

    /// <summary>An unqualifiable declaration counts each declaration of one word as one count, whatever its case.</summary>
    [Fact]
    public void Census_CountsUnqualifiedDeclarationsOfOneWord()
    {
        var census = new SymbolTable.UserWordCensus();
        Assert.Equal(1, census.DeclareUnqualified("Sw-On", UserWordKind.ConditionName));
        Assert.Equal(2, census.DeclareUnqualified("SW-ON", UserWordKind.ConditionName));
        Assert.Equal(2, census.UnqualifiedDeclarations("sw-on", UserWordKind.ConditionName));
        Assert.Equal(0, census.UnqualifiedDeclarations("SW-ON", UserWordKind.DataName));
    }

    /// <summary>The declaration funnel reports each refused word once per binder and refusal: the same word in another case is
    /// a repeat, and a refusal of another kind is reported on its own.</summary>
    [Fact]
    public void FirstRefusal_IsOncePerWordAndRefusal()
    {
        var symbols = Bind(Program("SYMDECL1", "       01 W PIC X.")).Symbols;
        Assert.True(symbols.FirstRefusal(SymbolTable.DeclarationRefusal.Letterless, "1-2"));
        Assert.False(symbols.FirstRefusal(SymbolTable.DeclarationRefusal.Letterless, "1-2"));
        Assert.True(symbols.FirstRefusal(SymbolTable.DeclarationRefusal.RepositoryIntrinsic, "Random"));
        Assert.False(symbols.FirstRefusal(SymbolTable.DeclarationRefusal.RepositoryIntrinsic, "RANDOM"));
        Assert.True(symbols.FirstRefusal(SymbolTable.DeclarationRefusal.Letterless, "RANDOM"));
    }

    /// <summary>The entry order's names: every data description entry that declares a data-name, in declaration order, found
    /// by any case of the word; a constant entry, a condition-name and FILLER declare none.</summary>
    [Fact]
    public void DataEntriesDeclaring_ListsEachDeclaringEntry_InOrder()
    {
        var data = Bind(Program("SYMDECL2", """
                   01 GRP-1.
                      05 Fld PIC X.
                      05 FILLER PIC X.
                   01 GRP-2.
                      05 FLD PIC 9.
                         88 FLD-ON VALUE 1.
                   01 K CONSTANT AS 3.
            """));
        var entries = data.Symbols.DataEntriesDeclaring("fld");
        Assert.Equal(2, entries.Count);
        Assert.True(entries[0].Entry.Start.Line < entries[1].Entry.Start.Line);
        Assert.All(entries, p => Assert.Same(p.Entry, p.Run[p.Index]));
        Assert.Empty(data.Symbols.DataEntriesDeclaring("FLD-ON"));
        Assert.Empty(data.Symbols.DataEntriesDeclaring("K"));
        Assert.Empty(data.Symbols.DataEntriesDeclaring("FILLER"));
    }

    /// <summary>§1985:VI.5.8.3 SR14, "Index-name-1 must be a unique word within the program": the second and later
    /// declarations of one index-name, in any case, are the ones refused.</summary>
    [Fact]
    public void RepeatedIndexNames_AreTheSecondAndLaterOfOneWord()
    {
        var data = Bind(Program("SYMDECL3", """
                   01 T-1.
                      05 E-1 PIC X OCCURS 2 INDEXED BY IX JX.
                   01 T-2.
                      05 E-2 PIC X OCCURS 2 INDEXED BY ix.
            """));
        var repeated = SymbolTable.RepeatedIndexNames(data.State.Forest.OwnIndexes).ToList();
        Assert.Single(repeated);
        Assert.Equal("ix", repeated[0].Name);
        Assert.Equal("E-2", repeated[0].Table.CobolName);
    }

    /// <summary>The CD entry's data-names are unique within it (§1985:XIV.2.2.3 SR6, "must be unique within the CD entry"): a
    /// name another field carries, in any case, is a repeat; the field's own earlier name is not.</summary>
    [Fact]
    public void CdEntryNamesElsewhere_SeesAnotherFieldOnly()
    {
        var names = new Dictionary<CommunicationField, string> { [CommunicationField.Queue] = "Q-NAME" };
        Assert.True(SymbolTable.CdEntryNamesElsewhere(names, CommunicationField.SubQueue1, "q-name"));
        Assert.False(SymbolTable.CdEntryNamesElsewhere(names, CommunicationField.Queue, "Q-NAME"));
        Assert.False(SymbolTable.CdEntryNamesElsewhere(names, CommunicationField.SubQueue1, "Q-OTHER"));
    }

    private static string Program(string id, string workingStorage) => $"""
               IDENTIFICATION DIVISION.
               PROGRAM-ID. {id}.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
        {workingStorage}
               PROCEDURE DIVISION.
                   STOP RUN.
        """;

    private static DataBinder Bind(string source)
    {
        string path = Path.Combine(Path.GetTempPath(), "cn_symdecl_" + Guid.NewGuid().ToString("N")[..8] + ".cob");
        File.WriteAllText(path, source);
        try
        {
            var diags = new DiagnosticBag();
            var tree = new CnFrontend { InitialFormat = TestSourceFormat.Inline, DialectLevel = 2002 }.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            var program = tree!.compilationGroup().SelectMany(g => g.programUnit()).First();
            var data = new DataBinder(new EditionContext(2002));
            data.Bind(program);
            return data;
        }
        finally { FileHarness.TryDelete(path); }
    }
}
