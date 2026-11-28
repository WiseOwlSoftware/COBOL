// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using WiseOwl.Cobol.Compiler;
using WiseOwl.Cobol.Compiler.Binding;
using WiseOwl.Cobol.Compiler.Binding.Model;
using WiseOwl.Cobol.Compiler.Oo;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend;
using WiseOwl.Cobol.Frontend.Diagnostics;
using WiseOwl.Cobol.Compiler.Symbols;
using Xunit;
using Core = WiseOwl.Cobol.Frontend.Parsing.CobolParserCore;

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
/// <item>Every identification answers the RIGHT entry: each planted reference, identified through the table's own funnels after the
/// bind, carries the id of the entry of the declaration the rules resolve it to (class, name and position), so a funnel that answers
/// some other entry is red even though every id stays set (review round 1, R1-2).</item>
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
    /// names, a file, a constant, a type declaration, an index-name, a CAPACITY register, procedure-names, and a reference to each;
    /// two paragraphs of one spelling in two sections, never referenced (a candidate set of two); a function whose REPOSITORY names the
    /// function itself (§2023:12.3.8.3 SR11); and a program whose TYPE clause copies a table with an index-name.</summary>
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
        "       DUP-A SECTION.",
        "       DUP.",
        "           EXIT.",
        "       S-2 SECTION.",
        "       DUP.",
        "           EXIT.",
        "       END PROGRAM ENTRIES.",
        "       IDENTIFICATION DIVISION.",
        "       FUNCTION-ID. RECUR.",
        "       ENVIRONMENT DIVISION.",
        "       CONFIGURATION SECTION.",
        "       REPOSITORY.",
        "           FUNCTION RECUR.",
        "       DATA DIVISION.",
        "       LINKAGE SECTION.",
        "       01 ARG PIC 9(4).",
        "       01 RES PIC 9(4).",
        "       PROCEDURE DIVISION USING ARG RETURNING RES.",
        "           IF ARG > 0 COMPUTE RES = FUNCTION RECUR(ARG) END-IF",
        "           GOBACK.",
        "       END FUNCTION RECUR.",
        "       IDENTIFICATION DIVISION.",
        "       PROGRAM-ID. TYPEIX.",
        "       DATA DIVISION.",
        "       WORKING-STORAGE SECTION.",
        "       01 T-REC TYPEDEF.",
        "          05 T-ELT PIC X OCCURS 3 INDEXED BY T-IX.",
        "       01 A TYPE T-REC.",
        "       PROCEDURE DIVISION.",
        "           GOBACK.",
        "       END PROGRAM TYPEIX.",
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
        "           CALL \"EXTSUB\"",
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
        // A program no source element of the group defines, which CALL's literal names (§1985:X.5.2.3 SR1): entered where its literal is
        // bound, declared nowhere.
        Assert.False(Assert.Single(symbols.Entries, e => e.Class == NameClass.ExternalProgram && e.Name == "EXTSUB").DeclaredAt.IsSet);
    }

    /// <summary>The 2023 report program: two VARYING counters of one clause, each declared by its data-name-1 (§2023:13.18.64).</summary>
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
        "             03 COLUMNS 1 3 5 PIC 9 SOURCE K",
        "                VARYING J FROM 1 BY 1 K FROM 1 BY 1.",
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
        AssertEntry(symbols, ProgramVarying, NameClass.ReportVarying, "J", line: 15, word: "J FROM");
        AssertEntry(symbols, ProgramVarying, NameClass.ReportVarying, "K", line: 15, word: "K FROM");
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

    // ---- every identification answers the RIGHT entry ------------------------------------------------------------------------

    /// <summary>Each reference of the 2023 program, identified by the table as the binder identifies it, answers the entry of the
    /// declaration the rules resolve it to: the class, the name and the position of that declaration's word, never only "an id".
    /// Each class's own way of choosing the entry is read here: the configuration-section answer, a procedure-name's, the containing
    /// function definition a function-prototype-name goes to (§2023:12.3.8.3 SR11), a candidate set's entries in its order, the
    /// CAPACITY register's and the index-name's (<c>IndexEntryOf</c>).</summary>
    [Fact]
    public void Program2023_EveryReference_AnswersItsDeclarationsEntry()
    {
        var bind = BindLive(Program2023, 2023);
        var entries = bind.Table("ENTRIES");
        var main = new ProcedureSite(null, null);
        AssertAnswers(bind, Answer(entries, Word(bind, 20, "ALPHA-A"), NameClass.Alphabet), NameClass.Alphabet, "ALPHA-A", 25);
        AssertAnswers(bind, Answer(entries, Word(bind, 55, "SW-ON"), NameClass.SwitchCondition), NameClass.SwitchCondition, "SW-ON", 23);
        AssertAnswers(bind, Answer(entries, Word(bind, 56, "HEXDIG"), NameClass.ClassName), NameClass.ClassName, "HEXDIG", 24);
        AssertAnswers(bind, Answer(entries, Word(bind, 57, "BELL"), NameClass.SymbolicCharacter), NameClass.SymbolicCharacter, "BELL", 26);
        AssertAnswers(bind, Answer(entries, Word(bind, 62, "CONS-OUT"), NameClass.Mnemonic), NameClass.Mnemonic, "CONS-OUT", 22);
        foreach (var (line, name, @class) in new[] { (27, "NAT-A", NameClass.NationalAlphabet), (28, "LOC-A", NameClass.Locale),
                     (29, "ORD-T", NameClass.OrderingName), (30, "DLS", NameClass.DynamicLengthStructure) })
            AssertAnswers(bind, Answer(entries, Word(bind, line, name), @class), @class, name, line);   // its declaring word
        AssertAnswers(bind, Answer(entries, Word(bind, 39, "INFILE"), NameClass.File), NameClass.File, "INFILE", 36, "SELECT");
        AssertAnswers(bind, Answer(entries, Word(bind, 58, "K"), NameClass.Constant), NameClass.Constant, "K", 42, "01");
        AssertAnswers(bind, Answer(entries, Word(bind, 45, "T-TYPE"), NameClass.TypeName), NameClass.TypeName, "T-TYPE", 43, "01");
        AssertAnswers(bind, Answer(entries, Word(bind, 61, "SUBP"), NameClass.ProgramPrototype), NameClass.ProgramPrototype, "SUBP", 33);
        AssertAnswers(bind, Answer(entries, Word(bind, 60, "TWICE"), NameClass.FunctionPrototype), NameClass.FunctionPrototype, "TWICE", 32);
        AssertAnswers(bind, Answer(entries, At<Core.ProcedureNameContext>(bind, 63, "P-2"), NameClass.Procedure, main),
            NameClass.Procedure, "P-2", 65);

        // A candidate set's entries stand in its order (two paragraphs of one spelling in two sections, in declaration order).
        var dup = Assert.IsType<ProcedureCandidates>(
            entries.Identify(At<Core.ProcedureNameContext>(bind, 68, "DUP"), NameClass.Procedure, report: false, site: main)!.Symbol);
        Assert.Equal(2, dup.Ids.Count);
        AssertEntryIs(bind, dup.Ids[0], NameClass.Procedure, "DUP", 68);
        AssertEntryIs(bind, dup.Ids[1], NameClass.Procedure, "DUP", 71);

        var capacity = Assert.IsType<CapacityRegisterCandidates>(
            entries.Identify(Word(bind, 59, "CAP"), NameClass.CapacityRegister, report: false)!.Symbol);
        AssertEntryIs(bind, Assert.Single(capacity.Ids), NameClass.CapacityRegister, "CAP", 49, "05");

        var ix = entries.IdentifyIndexName(WrittenOf(bind, 58, "IX"), report: false)?.Single;
        Assert.NotNull(ix);
        AssertEntryIs(bind, entries.IndexEntryOf(ix), NameClass.Index, "IX", 47, "05");

        // §2023:12.3.8.3 SR11: the function's own name, though its REPOSITORY specifies it, goes to the function definition.
        AssertAnswers(bind, Answer(bind.Table("RECUR"), Word(bind, 85, "RECUR"), NameClass.FunctionPrototype),
            NameClass.Program, "RECUR", 74, "IDENTIFICATION");
    }

    /// <summary>The data-names' and condition-names' entries are the bind's, made where each is declared (DESIGN-listing.md section
    /// 6.7.1, "Step 6b, remainder slice 5"): a data-name or condition-name reference identified as the binder identifies it answers,
    /// through <c>DataEntryOf</c>, the entry of the declaration the rules resolve it to, at that entry's level-number; a member a TYPE
    /// clause copied in answers its own entry on the line of the entry that wrote the clause (§2023:13.18.57.4 GR1: as though coded in
    /// place), never the type declaration's member's; and an object no declaration entered fails loudly.</summary>
    [Fact]
    public void Program2023_DataAndConditionReferences_AnswerTheirDeclarationsEntry()
    {
        var bind = BindLive(Program2023, 2023);
        var entries = bind.Table("ENTRIES");
        var n = entries.IdentifyDataName(WrittenOf(bind, 59, "N"), report: false).Single;
        Assert.NotNull(n);
        AssertEntryIs(bind, entries.DataEntryOf(n), NameClass.Data, "N", 50, "01");
        var record = entries.IdentifyDataName(WrittenOf(bind, 57, "IN-REC"), report: false).Single;
        Assert.NotNull(record);
        AssertEntryIs(bind, entries.DataEntryOf(record), NameClass.Data, "IN-REC", 40, "01");
        var condition = Assert.Single(entries.IdentifyConditionName(WrittenOf(bind, 55, "N-ZERO"), report: false));
        AssertEntryIs(bind, entries.DataEntryOf(condition), NameClass.Condition, "N-ZERO", 51, "88");

        var copy = entries.IdentifyDataName(new WrittenQualifiedName(new CommonToken(1, "T-A"), []), report: false).Single;
        Assert.NotNull(copy);
        Assert.NotNull(copy.ClonedFrom);   // the copy under T-USE, not the type declaration's member
        AssertEntryIs(bind, entries.DataEntryOf(copy), NameClass.Data, "T-A", 45, "01");
        AssertEntryIs(bind, entries.DataEntryOf(copy.ClonedFrom!), NameClass.Data, "T-A", 44, "05");

        var refused = Assert.Throws<InvalidOperationException>(() => entries.DataEntryOf(new DataItem { Level = 1, CobolName = "X", ClrName = "X" }));
        Assert.Contains("entered no entry", refused.Message, StringComparison.Ordinal);
    }

    /// <summary>An index-name a TYPE clause's copy declares answers the copy's own entry; the type declaration's member, which a
    /// lapsed candidate stands for (§2023:8.4.2.2.1 SR4), declared no index cell and entered nothing, so asking its entry fails
    /// loudly, never a silent no-entry (it only joins a set beside a declared survivor, never a set of one).</summary>
    [Fact]
    public void ATypeClauseCopysIndexName_AnswersItsEntry_AndALapsedMember_HasNone()
    {
        var bind = BindLive(Program2023, 2023);
        var table = bind.Table("TYPEIX");
        var copy = table.IdentifyIndexName(new WrittenQualifiedName(new CommonToken(1, "T-IX"), []), report: false)?.Single;
        Assert.NotNull(copy);
        Assert.NotNull(copy.Table.ClonedFrom);   // the copy's table, under A
        var entry = bind.Entries[table.IndexEntryOf(copy)];
        Assert.Equal((NameClass.Index, "T-IX"), (entry.Class, entry.Name));
        Assert.Same(copy, entry.Symbol);
        var lapsed = new IndexDeclaration("T-IX", copy.Table.ClonedFrom!, "");
        var refused = Assert.Throws<InvalidOperationException>(() => table.IndexEntryOf(lapsed));
        Assert.Contains("entered no entry", refused.Message, StringComparison.Ordinal);
    }

    /// <summary>The 1985 program's references: a file, a report, a CD, a report group (a candidate set of the report group description
    /// entry's entry), a USE FOR DEBUGGING operand's procedure-name and a DEBUG-ITEM register.</summary>
    [Fact]
    public void Program1985_EveryReference_AnswersItsDeclarationsEntry()
    {
        var bind = BindLive(Program1985, 85);
        var table = bind.Table("ENTRIES85");
        AssertAnswers(bind, Answer(table, Word(bind, 32, "RPT-FILE"), NameClass.File), NameClass.File, "RPT-FILE", 8, "SELECT");
        AssertAnswers(bind, Answer(table, Word(bind, 33, "RPT"), NameClass.Report), NameClass.Report, "RPT", 18, "RD");
        AssertAnswers(bind, Answer(table, Word(bind, 15, "CD-IN"), NameClass.CommunicationDescription),
            NameClass.CommunicationDescription, "CD-IN", 15, "CD");
        var dtl = Assert.IsAssignableFrom<ICandidateEntries>(table.Identify(At<Core.ReportGroupReferenceContext>(bind, 34, "DTL"),
            NameClass.ReportGroup, report: false)!.Symbol);
        AssertEntryIs(bind, Assert.Single(dtl.Ids), NameClass.Data, "DTL", 19, "01");
        AssertAnswers(bind, Answer(table, At<Core.DataReferenceContext>(bind, 26, "P-1"), NameClass.Procedure, new ProcedureSite(null, null)),
            NameClass.Procedure, "P-1", 31);
        var debugName = bind.Entries[Answer(table, Word(bind, 28, "DEBUG-NAME"), NameClass.DebugRegister).Id];
        Assert.Equal((NameClass.DebugRegister, "DEBUG-NAME"), (debugName.Class, debugName.Name));
    }

    /// <summary>A VARYING counter's reference answers ITS word's entry, not another counter's of the same clause.</summary>
    [Fact]
    public void ProgramVarying_ACounterReference_AnswersItsOwnWordsEntry()
    {
        var bind = BindLive(ProgramVarying, 2023);
        var declaring = At<Core.ReportGroupEntryContext>(bind, 14, "03");
        List<ReportVaryingModel> counters =
            [new() { Name = "J", Uid = 1, Entry = declaring }, new() { Name = "K", Uid = 2, Entry = declaring }];
        var answer = bind.Table("RPTV").Identify(TokenWord(bind, 14, "K", after: "SOURCE "), NameClass.ReportVarying, report: false,
            reportSite: new ReportSite(null, counters));
        Assert.NotNull(answer);
        AssertAnswers(bind, answer, NameClass.ReportVarying, "K", 15, "K FROM");
    }

    /// <summary>The group tier's lookup (<c>SymbolTable.ClassNames.Lookup</c>) answers the interface's, the standard class's and the
    /// parameter-name's entries.</summary>
    [Fact]
    public void ProgramOo_EveryClassNameReference_AnswersItsDeclarationsEntry()
    {
        var bind = BindLive(ProgramOo, 2023);
        var names = bind.Binder.Session.OoClasses.Names;
        var iface = Word(bind, 13, "IFACE");
        AssertEntryIs(bind, names.Lookup(iface, iface, OoNameWant.Interface).Entry, NameClass.Interface, "IFACE", 2);
        var standardBase = Word(bind, 5, "BASE");
        var baseEntry = bind.Entries[names.Lookup(standardBase, standardBase, OoNameWant.Class).Entry];
        Assert.Equal((NameClass.ObjectClass, "BASE", false), (baseEntry.Class, baseEntry.Name, baseEntry.At.IsSet));
        var formal = Word(bind, 22, "ELEM");
        AssertEntryIs(bind, names.Lookup(formal, formal, OoNameWant.Class).Entry, NameClass.ParameterName, "ELEM", 17);
    }

    private static IdentifiedName Answer(SymbolTable table, ParserRuleContext written, NameClass @class, ProcedureSite? site = null)
    {
        var answer = table.Identify(written, @class, report: false, site: site);
        Assert.True(answer is not null, $"'{written.GetText()}' identified no {@class}");
        return answer;
    }

    /// <summary>Assert <paramref name="answer"/>'s entry is the declaration of class <paramref name="class"/> named
    /// <paramref name="name"/> at <paramref name="word"/> (default: the name) on 1-based source line <paramref name="line"/>.</summary>
    private static void AssertAnswers(LiveBind bind, IdentifiedName answer, NameClass @class, string name, int line, string? word = null) =>
        AssertEntryIs(bind, answer.Id, @class, name, line, word);

    private static void AssertEntryIs(LiveBind bind, SymbolId id, NameClass @class, string name, int line, string? word = null)
    {
        Assert.True(id.IsSet, $"the {@class} '{name}' answered no entry");
        var entry = bind.Entries[id];
        Assert.Equal((@class, name, new SymbolPosition(line, ColumnOf(bind.Source, line, word ?? name))), (entry.Class, entry.Name, entry.At));
    }

    /// <summary>The resultant column of <paramref name="word"/> on 1-based source <paramref name="line"/> (its first occurrence after
    /// <paramref name="after"/>, when given).</summary>
    private static int ColumnOf(string[] source, int line, string word, string? after = null)
    {
        string text = source[line - 1];
        int from = after is null ? 0 : text.IndexOf(after, StringComparison.Ordinal) + after.Length;
        int written = text.IndexOf(word, from, StringComparison.Ordinal);
        Assert.True(written >= FixedFormPrefix, $"line {line} does not write '{word}'");
        return written - FixedFormPrefix;
    }

    /// <summary>The innermost context that holds exactly the token <paramref name="word"/> written on 1-based <paramref name="line"/>.</summary>
    private static ParserRuleContext Word(LiveBind bind, int line, string word) => TokenWord(bind, line, word);

    private static ParserRuleContext TokenWord(LiveBind bind, int line, string word, string? after = null) =>
        (ParserRuleContext)TokenAt(bind, line, word, after).Parent;

    /// <summary>The innermost context of type <typeparamref name="T"/> that holds <paramref name="word"/> on <paramref name="line"/>.</summary>
    private static T At<T>(LiveBind bind, int line, string word) where T : ParserRuleContext
    {
        for (IParseTree? node = TokenAt(bind, line, word); node is not null; node = node.Parent)
            if (node is T found) return found;
        throw new Xunit.Sdk.XunitException($"no {typeof(T).Name} holds '{word}' on line {line}");
    }

    /// <summary>A written qualified name of one word, its token the one written at <paramref name="word"/> on <paramref name="line"/>.</summary>
    private static WrittenQualifiedName WrittenOf(LiveBind bind, int line, string word) => new(TokenAt(bind, line, word).Symbol, []);

    private static ITerminalNode TokenAt(LiveBind bind, int line, string word, string? after = null)
    {
        int column = ColumnOf(bind.Source, line, word, after);
        var stack = new Stack<IParseTree>([bind.Tree]);
        while (stack.TryPop(out var node))
        {
            if (node is ITerminalNode t && t.Symbol.Line == line && t.Symbol.Column == column && t.Symbol.Text == word) return t;
            for (int i = 0; i < node.ChildCount; i++) stack.Push(node.GetChild(i));
        }
        throw new Xunit.Sdk.XunitException($"no token '{word}' at {line}:{column}");
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
    private SymbolTableSnapshot Bind(string[] source, int edition) => BindLive(source, edition).Snapshot;

    /// <summary>A clean bind of a planted program, kept live: the finished snapshot, the parse tree, the bound units and the binder
    /// whose session holds each element's <see cref="SymbolTable"/> and the group's entries.</summary>
    private sealed record LiveBind(string[] Source, SymbolTableSnapshot Snapshot, Core.CompilationUnitContext Tree,
        BoundCompilation Compilation, BinderDriver Binder)
    {
        /// <summary>The compilation group's entries (one store, <c>OoClassTable.Entries</c>).</summary>
        public SymbolTable.EntryStore Entries => Binder.Session.OoClasses.Entries;

        /// <summary>The symbol table of the program or function named <paramref name="unit"/>.</summary>
        public SymbolTable Table(string unit) =>
            Binder.Session.Binders.Of(Compilation.Units.Single(u => u.Name == unit)).Data.Symbols;
    }

    private LiveBind BindLive(string[] source, int edition)
    {
        string path = Path.Combine(TempDir, $"entries{edition}-{Guid.NewGuid():N}.cob");
        File.WriteAllText(path, string.Join("\n", source));
        var options = new CompilerDriver.Options(path, EditionInfo.Of(edition), CheckOnly: true);
        var frontend = CompilerDriver.NewFrontend(options, new CompilationInputs(), recordsListing: false);
        var bag = new DiagnosticBag();
        var tree = frontend.Parse(path, bag);
        Assert.True(tree is not null, string.Join("\n", bag.Diagnostics));
        var binder = new BinderDriver();
        var context = new EditionContext(edition, false) { LineMap = frontend.LineMap, RecordsSymbols = true, RecordsNames = true, RecordsReferences = true };
        var compilation = binder.Bind(tree, context, frontend.Directives);
        Assert.False(context.HasErrors, string.Join("\n", context.Diagnostics));
        return new(source, Assert.IsType<SymbolTableSnapshot>(binder.Symbols), tree, compilation, binder);
    }
}
