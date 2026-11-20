// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol;
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Frontend;
using WiseOwl.Cobol.Frontend.Diagnostics;
using WiseOwl.Cobol.Symbols;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Listing;

/// <summary>
/// ⛔ EVERY DECLARED NAME IS AN ENTRY, AND EVERY IDENTIFICATION CARRIES ITS ENTRY (docs/rearchitecture/DESIGN-listing.md section 6.7.1,
/// "Step 6b, the entry ids"; kb/Work PB5932's dated file <c>2026-11-19-step-6b-entry-ids</c>). Each class whose declaration is the
/// symbol table's makes its entry (a <see cref="SymbolId"/>) where the binder declares the name, and <c>SymbolTable.Identify</c>,
/// <c>IdentifyIndexName</c> and <c>SymbolTable.ClassNames.Lookup</c> hand the id back with the symbol, so listing step 7b joins an
/// occurrence to its entry by id.
/// <list type="bullet">
/// <item>An identification without an entry cannot be made: <see cref="IdentifiedName"/>'s constructor refuses one (the planted
/// answers below are red), so a declaring path that entered nothing fails at its first reference in every compilation.</item>
/// <item>Planted programs declare a name of every class the table declares, at a position written from the source text, and
/// reference each: the bind must succeed (each reference is identified, so each declaring path entered its name) and the finished
/// table must hold each entry with the class, the name as written and the line and column of its declaring word (the data map's
/// placement for a data description entry's names: an index-name and a CAPACITY register on their table's line, a constant at its
/// level-number). A class with no witness here is red (<see cref="EveryClassOfTheTable_HasAWitness"/>).</item>
/// </list>
/// </summary>
public sealed class SymbolEntryDriftTests : WiseOwlCobolTestBase
{
    // ---- the guard: an answer without an entry is refused --------------------------------------------------------------------

    [Fact]
    public void AnIdentification_WithoutAnEntry_IsRefused()
    {
        var refused = Assert.Throws<InvalidOperationException>(() => new IdentifiedName(NameClass.File, new object(), default));
        Assert.Contains("entered no entry", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ACandidateSet_WithACandidateWithoutAnEntry_IsRefused_AndOneWithEveryEntry_IsAnAnswer()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new IdentifiedName(NameClass.ReportGroup, new ReportCandidates<string>(["A", "B"], [new SymbolId(1), default]), default));
        Assert.Throws<InvalidOperationException>(() =>   // a set names no one entry of its own
            new IdentifiedName(NameClass.ReportGroup, new ReportCandidates<string>(["A"], [new SymbolId(1)]), new SymbolId(1)));
        var answer = new IdentifiedName(NameClass.ReportGroup, new ReportCandidates<string>(["A", "B"], [new SymbolId(1), new SymbolId(2)]), default);
        Assert.False(answer.Id.IsSet);
        // An empty set (a sum counter whose qualifiers reach none, step 6b slice 5) is an answer with no entry to name.
        Assert.False(new IdentifiedName(NameClass.SumCounter, new ReportCandidates<string>([], []), default).Id.IsSet);
    }

    [Fact]
    public void AnAnswerWithItsEntry_IsMade()
    {
        var answer = new IdentifiedName(NameClass.File, new object(), new SymbolId(7));
        Assert.Equal(new SymbolId(7), answer.Id);
    }

    // ---- the planted programs ------------------------------------------------------------------------------------------------

    /// <summary>The 2023 program: a user function and a program the main program's REPOSITORY specifies, the configuration-section
    /// names, a file, a constant, a type declaration, an index-name, a CAPACITY register, procedure-names, and a reference to each.</summary>
    private static readonly string[] Program2023 =
    [
        "       IDENTIFICATION DIVISION.",
        "       FUNCTION-ID. TWICE.",
        "       DATA DIVISION.",
        "       LINKAGE SECTION.",
        "       01 ARG PIC 9(4).",
        "       01 RES PIC 9(4).",
        "       PROCEDURE DIVISION USING ARG RETURNING RES.",
        "           COMPUTE RES = ARG * 2",
        "           GOBACK.",
        "       END FUNCTION TWICE.",
        "       IDENTIFICATION DIVISION.",
        "       PROGRAM-ID. SUBP.",
        "       PROCEDURE DIVISION.",
        "           GOBACK.",
        "       END PROGRAM SUBP.",
        "       IDENTIFICATION DIVISION.",
        "       PROGRAM-ID. ENTRIES.",
        "       ENVIRONMENT DIVISION.",
        "       CONFIGURATION SECTION.",
        "       OBJECT-COMPUTER. X PROGRAM COLLATING SEQUENCE IS ALPHA-A.",
        "       SPECIAL-NAMES.",
        "           CONSOLE IS CONS-OUT",
        "           SWITCH-1 IS SW1 ON STATUS IS SW-ON",
        "           CLASS HEXDIG IS \"0\" THRU \"9\" \"A\" THRU \"F\"",
        "           ALPHABET ALPHA-A IS NATIVE",
        "           SYMBOLIC CHARACTERS BELL IS 8",
        "           ALPHABET NAT-A FOR NATIONAL IS UTF-16",
        "           LOCALE LOC-A IS \"en_US\"",
        "           ORDER TABLE ORD-T IS \"ISO14651_2001_TABLE1\"",
        "           DYNAMIC LENGTH STRUCTURE DLS IS DELIMITED.",
        "       REPOSITORY.",
        "           FUNCTION TWICE",
        "           PROGRAM SUBP.",
        "       INPUT-OUTPUT SECTION.",
        "       FILE-CONTROL.",
        "           SELECT INFILE ASSIGN TO \"IN.DAT\".",
        "       DATA DIVISION.",
        "       FILE SECTION.",
        "       FD INFILE.",
        "       01 IN-REC PIC X(4).",
        "       WORKING-STORAGE SECTION.",
        "       01 K CONSTANT AS 3.",
        "       01 T-TYPE TYPEDEF.",
        "          05 T-A PIC X.",
        "       01 T-USE TYPE T-TYPE.",
        "       01 TBL.",
        "          05 TBL-E PIC X OCCURS 3 INDEXED BY IX.",
        "       01 DYN.",
        "          05 DYN-E PIC X OCCURS DYNAMIC CAPACITY IN CAP.",
        "       01 N PIC 9(4).",
        "          88 N-ZERO VALUE 0.",
        "       PROCEDURE DIVISION.",
        "       MAIN-SEC SECTION.",
        "       P-1.",
        "           IF SW-ON OR N-ZERO CONTINUE END-IF",
        "           IF IN-REC IS HEXDIG CONTINUE END-IF",
        "           MOVE BELL TO IN-REC",
        "           SET IX TO K",
        "           MOVE CAP TO N",
        "           COMPUTE N = FUNCTION TWICE(N)",
        "           CALL SUBP",
        "           DISPLAY IN-REC UPON CONS-OUT",
        "           PERFORM P-2",
        "           STOP RUN.",
        "       P-2.",
        "           EXIT.",
        "       END PROGRAM ENTRIES.",
    ];

    [Fact]
    public void Program2023_EnteredEveryName_WhereItIsDeclared()
    {
        var symbols = Bind(Program2023, 2023);
        AssertEntry(symbols, Program2023, NameClass.Program, "TWICE", line: 1, word: "IDENTIFICATION");   // a unit's entry: its first token
        AssertEntry(symbols, Program2023, NameClass.Program, "SUBP", line: 11, word: "IDENTIFICATION");
        AssertEntry(symbols, Program2023, NameClass.Program, "ENTRIES", line: 16, word: "IDENTIFICATION");
        AssertEntry(symbols, Program2023, NameClass.Mnemonic, "CONS-OUT", line: 22);
        AssertEntry(symbols, Program2023, NameClass.SwitchCondition, "SW-ON", line: 23);
        AssertEntry(symbols, Program2023, NameClass.ClassName, "HEXDIG", line: 24);
        AssertEntry(symbols, Program2023, NameClass.Alphabet, "ALPHA-A", line: 25);
        AssertEntry(symbols, Program2023, NameClass.SymbolicCharacter, "BELL", line: 26);
        AssertEntry(symbols, Program2023, NameClass.NationalAlphabet, "NAT-A", line: 27);
        AssertEntry(symbols, Program2023, NameClass.Locale, "LOC-A", line: 28);
        AssertEntry(symbols, Program2023, NameClass.OrderingName, "ORD-T", line: 29);
        AssertEntry(symbols, Program2023, NameClass.DynamicLengthStructure, "DLS", line: 30);
        AssertEntry(symbols, Program2023, NameClass.FunctionPrototype, "TWICE", line: 32);
        AssertEntry(symbols, Program2023, NameClass.ProgramPrototype, "SUBP", line: 33);
        AssertEntry(symbols, Program2023, NameClass.File, "INFILE", line: 36, word: "SELECT");   // the file control entry (section 6.6)
        AssertEntry(symbols, Program2023, NameClass.Constant, "K", line: 42, word: "01");     // at its level-number (section 6.6)
        AssertEntry(symbols, Program2023, NameClass.TypeName, "T-TYPE", line: 43, word: "01");
        AssertEntry(symbols, Program2023, NameClass.Index, "IX", line: 47, word: "05");       // on its table's line (section 6.6)
        AssertEntry(symbols, Program2023, NameClass.CapacityRegister, "CAP", line: 49, word: "05");
        AssertEntry(symbols, Program2023, NameClass.Procedure, "MAIN-SEC", line: 53);
        AssertEntry(symbols, Program2023, NameClass.Procedure, "P-1", line: 54);
        AssertEntry(symbols, Program2023, NameClass.Procedure, "P-2", line: 65);
    }

    /// <summary>The 1985 program: a report with a detail group and a SUM counter, a CD, and the DEBUG-ITEM registers a debugging
    /// section activates, which the standard declares implicitly (an entry at no position).</summary>
    private static readonly string[] Program1985 =
    [
        "       IDENTIFICATION DIVISION.",
        "       PROGRAM-ID. ENTRIES85.",
        "       ENVIRONMENT DIVISION.",
        "       CONFIGURATION SECTION.",
        "       SOURCE-COMPUTER. X WITH DEBUGGING MODE.",
        "       INPUT-OUTPUT SECTION.",
        "       FILE-CONTROL.",
        "           SELECT RPT-FILE ASSIGN TO \"R.TXT\".",
        "       DATA DIVISION.",
        "       FILE SECTION.",
        "       FD RPT-FILE REPORT IS RPT.",
        "       WORKING-STORAGE SECTION.",
        "       01 AMT PIC 9(4) VALUE 1.",
        "       COMMUNICATION SECTION.",
        "       CD CD-IN FOR INPUT.",
        "       01 CD-REC PIC X(87).",
        "       REPORT SECTION.",
        "       RD RPT CONTROL IS FINAL PAGE LIMIT IS 20 LINES.",
        "       01 DTL TYPE DETAIL LINE 1.",
        "          05 COLUMN 1 PIC 9(4) SOURCE AMT.",
        "       01 TYPE CONTROL FOOTING FINAL LINE 2.",
        "          05 TOT COLUMN 1 PIC 9(6) SUM AMT.",
        "       PROCEDURE DIVISION.",
        "       DECLARATIVES.",
        "       DBG SECTION.",
        "           USE FOR DEBUGGING ON P-1.",
        "       D-1.",
        "           DISPLAY DEBUG-NAME.",
        "       END DECLARATIVES.",
        "       MAIN SECTION.",
        "       P-1.",
        "           OPEN OUTPUT RPT-FILE",
        "           INITIATE RPT",
        "           GENERATE DTL",
        "           TERMINATE RPT",
        "           CLOSE RPT-FILE",
        "           STOP RUN.",
    ];

    [Fact]
    public void Program1985_EnteredEveryName_WhereItIsDeclared()
    {
        var symbols = Bind(Program1985, 85);
        AssertEntry(symbols, Program1985, NameClass.CommunicationDescription, "CD-IN", line: 15, word: "CD");
        AssertEntry(symbols, Program1985, NameClass.Report, "RPT", line: 18, word: "RD");
        AssertEntry(symbols, Program1985, NameClass.Data, "DTL", line: 19, word: "01");     // a report group description entry
        AssertEntry(symbols, Program1985, NameClass.Data, "TOT", line: 22, word: "05");     // a SUM entry: its sum counter's name
        AssertEntry(symbols, Program1985, NameClass.Procedure, "DBG", line: 25);
        AssertEntry(symbols, Program1985, NameClass.Procedure, "P-1", line: 31);
        var debugName = Assert.Single(symbols.Entries, e => e.Class == NameClass.DebugRegister && e.Name == "DEBUG-NAME");
        Assert.False(debugName.DeclaredAt.IsSet);   // §1985:XV.3.2.4 GR10: "DEBUG-ITEM has the following implicit description"
    }

    /// <summary>The 2023 report program: a VARYING counter, declared by the clause's data-name-1 (§2023:13.18.64).</summary>
    private static readonly string[] ProgramVarying =
    [
        "       IDENTIFICATION DIVISION.",
        "       PROGRAM-ID. RPTV.",
        "       ENVIRONMENT DIVISION.",
        "       INPUT-OUTPUT SECTION.",
        "       FILE-CONTROL.",
        "           SELECT RFILE ASSIGN TO \"R.TXT\".",
        "       DATA DIVISION.",
        "       FILE SECTION.",
        "       FD RFILE REPORT IS R-V.",
        "       REPORT SECTION.",
        "       RD R-V.",
        "       01 DET-A TYPE DE.",
        "          02 LINE PLUS 1.",
        "             03 COLUMNS 1 3 5 PIC 9 VARYING K FROM 1 BY 1 SOURCE K.",
        "       PROCEDURE DIVISION.",
        "           OPEN OUTPUT RFILE",
        "           INITIATE R-V",
        "           GENERATE DET-A",
        "           TERMINATE R-V",
        "           CLOSE RFILE",
        "           STOP RUN.",
    ];

    [Fact]
    public void ProgramVarying_EnteredTheCounter_AtItsWord()
    {
        var symbols = Bind(ProgramVarying, 2023);
        AssertEntry(symbols, ProgramVarying, NameClass.ReportVarying, "K", line: 14, word: "K FROM");
        AssertEntry(symbols, ProgramVarying, NameClass.Data, "DET-A", line: 12, word: "01");
    }

    /// <summary>The OO program: an interface, a class that inherits the standard class BASE and implements it, each named by the
    /// class's REPOSITORY paragraph; the group tier's entries.</summary>
    private static readonly string[] ProgramOo =
    [
        "       IDENTIFICATION DIVISION.",
        "       INTERFACE-ID. IFACE.",
        "       END INTERFACE IFACE.",
        "       IDENTIFICATION DIVISION.",
        "       CLASS-ID. ACLASS INHERITS FROM BASE.",
        "       ENVIRONMENT DIVISION.",
        "       CONFIGURATION SECTION.",
        "       REPOSITORY.",
        "           CLASS BASE",
        "           INTERFACE IFACE.",
        "       IDENTIFICATION DIVISION.",
        "       OBJECT.",
        "       IMPLEMENTS IFACE.",
        "       END OBJECT.",
        "       END CLASS ACLASS.",
        "       IDENTIFICATION DIVISION.",
        "       CLASS-ID. PHOLD INHERITS FROM BASE USING ELEM.",
        "       ENVIRONMENT DIVISION.",
        "       CONFIGURATION SECTION.",
        "       REPOSITORY.",
        "           CLASS BASE",
        "           CLASS ELEM.",
        "       END CLASS PHOLD.",
    ];

    [Fact]
    public void ProgramOo_EnteredTheGroupTiersNames()
    {
        var symbols = Bind(ProgramOo, 2023);
        AssertEntry(symbols, ProgramOo, NameClass.Interface, "IFACE", line: 2);
        AssertEntry(symbols, ProgramOo, NameClass.ObjectClass, "ACLASS", line: 5);
        AssertEntry(symbols, ProgramOo, NameClass.ParameterName, "ELEM", line: 17);   // the definition is bound as itself
        var standardBase = Assert.Single(symbols.Entries, e => e.Class == NameClass.ObjectClass && e.Name == "BASE");
        Assert.False(standardBase.DeclaredAt.IsSet);   // declared by the standard, entered when the group first names it
    }

    /// <summary>Every class of <see cref="NameClass"/> that is an entry's class has a witness among the planted programs; the classes
    /// a reference's POSITION admits whose entry is another class's (a report group, a sum counter, a report entry and a SUM operand
    /// identify a report group description entry, a <see cref="NameClass.Data"/> entry) are named with that reason. A new class is red
    /// until a planted program declares one.</summary>
    [Fact]
    public void EveryClassOfTheTable_HasAWitness()
    {
        var witnessed = new[] { (Program2023, 2023), (Program1985, 85), (ProgramOo, 2023), (ProgramVarying, 2023) }
            .SelectMany(p => Bind(p.Item1, p.Item2).Entries).Select(static e => e.Class).ToHashSet();
        NameClass[] positionOnly = [NameClass.ReportGroup, NameClass.SumCounter, NameClass.ReportEntry, NameClass.ReportSumOperand];
        var unwitnessed = Enum.GetValues<NameClass>().Where(c => !witnessed.Contains(c) && !positionOnly.Contains(c)).ToList();
        Assert.True(unwitnessed.Count == 0, "no planted program declares a name of class " + string.Join(", ", unwitnessed));
        Assert.DoesNotContain(witnessed, positionOnly.Contains);
    }

    /// <summary>A position is RESULTANT (section 6.6): the line as the reference-format conversion leaves it, which drops a fixed-form
    /// line's sequence number area and indicator area, its first seven characters, so a column is counted from the area that follows.</summary>
    private const int FixedFormPrefix = 7;

    /// <summary>Assert the finished table holds an entry of <paramref name="class"/> named <paramref name="name"/> at the position of
    /// <paramref name="word"/> (default: the name itself) on 1-based source <paramref name="line"/>: the column is where the source
    /// text writes it, counted in the resultant line (<see cref="FixedFormPrefix"/>).</summary>
    private static void AssertEntry(SymbolTableSnapshot symbols, string[] source, NameClass @class, string name, int line, string? word = null)
    {
        int written = source[line - 1].IndexOf(word ?? name, StringComparison.Ordinal);
        Assert.True(written >= FixedFormPrefix, $"line {line} does not write '{word ?? name}'");
        int column = written - FixedFormPrefix;
        var entries = symbols.Entries.Where(e => e.Class == @class && e.Name == name).ToList();
        Assert.True(entries.Any(e => e.DeclaredAt == new SymbolPosition(line, column)),
            $"no {@class} entry '{name}' at {line}:{column}; the table holds "
            + string.Join(", ", symbols.Entries.Where(e => e.Name == name).Select(e => $"{e.Class} {e.Name} {e.DeclaredAt}")));
    }

    /// <summary>Bind <paramref name="source"/> at <paramref name="edition"/> with every name finished (the data map's request), through
    /// the front end and the binder the driver uses; the bind must be clean, so every reference was identified.</summary>
    private SymbolTableSnapshot Bind(string[] source, int edition)
    {
        string path = Path.Combine(TempDir, $"entries{edition}-{Guid.NewGuid():N}.cob");
        File.WriteAllText(path, string.Join("\n", source));
        var options = new CompilerDriver.Options(path, DialectLevel: edition, CheckOnly: true);
        var frontend = CompilerDriver.NewFrontend(options, new CompilationInputs(), recordsListing: false);
        var bag = new DiagnosticBag();
        var tree = frontend.Parse(path, bag);
        Assert.True(tree is not null, string.Join("\n", bag.Diagnostics));
        var binder = new BinderDriver();
        var context = new EditionContext(edition, false) { LineMap = frontend.LineMap, RecordsSymbols = true, RecordsNames = true };
        binder.Bind(tree, context, frontend.Directives);
        Assert.False(context.HasErrors, string.Join("\n", context.Diagnostics));
        return Assert.IsType<SymbolTableSnapshot>(binder.Symbols);
    }
}
