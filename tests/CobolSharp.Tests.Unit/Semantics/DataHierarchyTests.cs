using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Lexing;
using CobolSharp.Compiler.Parsing;
using CobolSharp.Compiler.Semantics;
using Xunit;

namespace CobolSharp.Tests.Unit.Semantics;

public class DataHierarchyTests
{
    private SemanticModel Analyze(string cobolSource)
    {
        var source = SourceText.From(cobolSource);
        var diagnostics = new DiagnosticBag();
        var lexer = new Lexer(source, diagnostics);
        var tokens = lexer.Tokenize();
        var parser = new Parser(tokens, diagnostics);
        var ast = parser.ParseCompilationUnit();
        var analyzer = new SemanticAnalyzer(diagnostics);
        return analyzer.Analyze(ast);
    }

    [Fact]
    public void GroupItem_HasChildren()
    {
        var model = Analyze("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TEST1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-RECORD.
               05 WS-NAME PIC X(20).
               05 WS-AGE PIC 99.
            PROCEDURE DIVISION.
                STOP RUN.
            """);

        var record = model.SymbolTable.Resolve("WS-RECORD");
        Assert.NotNull(record);
        Assert.True(record!.IsGroup);
        Assert.Equal(2, record.Children.Count);
        Assert.Equal("WS-NAME", record.Children[0].Name);
        Assert.Equal("WS-AGE", record.Children[1].Name);
    }

    [Fact]
    public void GroupItem_ByteSize_SumOfChildren()
    {
        var model = Analyze("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TEST2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-RECORD.
               05 WS-NAME PIC X(20).
               05 WS-AGE PIC 99.
            PROCEDURE DIVISION.
                STOP RUN.
            """);

        var record = model.SymbolTable.Resolve("WS-RECORD");
        Assert.NotNull(record);
        Assert.Equal(22, record!.ByteSize); // 20 + 2
    }

    [Fact]
    public void NestedGroups()
    {
        var model = Analyze("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TEST3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-PERSON.
               05 WS-NAME.
                  10 WS-FIRST PIC X(15).
                  10 WS-LAST PIC X(20).
               05 WS-AGE PIC 99.
            PROCEDURE DIVISION.
                STOP RUN.
            """);

        var person = model.SymbolTable.Resolve("WS-PERSON");
        Assert.NotNull(person);
        Assert.True(person!.IsGroup);
        Assert.Equal(2, person.Children.Count); // WS-NAME, WS-AGE

        var nameGroup = model.SymbolTable.Resolve("WS-NAME");
        Assert.NotNull(nameGroup);
        Assert.True(nameGroup!.IsGroup);
        Assert.Equal(2, nameGroup.Children.Count); // WS-FIRST, WS-LAST
        Assert.Equal(35, nameGroup.ByteSize); // 15 + 20

        Assert.Equal(37, person.ByteSize); // 35 + 2
    }

    [Fact]
    public void Level77_Standalone()
    {
        var model = Analyze("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TEST4.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            77 WS-COUNTER PIC 9(5).
            77 WS-FLAG PIC X.
            PROCEDURE DIVISION.
                STOP RUN.
            """);

        var counter = model.SymbolTable.Resolve("WS-COUNTER");
        Assert.NotNull(counter);
        Assert.Equal(77, counter!.LevelNumber);
        Assert.False(counter.IsGroup);
        Assert.Equal(5, counter.ByteSize);
    }

    [Fact]
    public void Level88_ConditionName()
    {
        var model = Analyze("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TEST5.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-STATUS PIC 9.
               88 STATUS-OK VALUE 0.
               88 STATUS-ERR VALUE 1 THRU 9.
            PROCEDURE DIVISION.
                STOP RUN.
            """);

        var status = model.SymbolTable.Resolve("WS-STATUS");
        Assert.NotNull(status);
        // Level 88s are children of their parent
        Assert.Equal(2, status!.Children.Count);
        Assert.Equal(88, status.Children[0].LevelNumber);
    }

    [Fact]
    public void Occurs_SetsCount()
    {
        var model = Analyze("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TEST6.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-TABLE.
               05 WS-ITEM PIC X(10) OCCURS 5 TIMES.
            PROCEDURE DIVISION.
                STOP RUN.
            """);

        var item = model.SymbolTable.Resolve("WS-ITEM");
        Assert.NotNull(item);
        Assert.Equal(5, item!.OccursCount);
        Assert.Equal(10, item.ByteSize);
        Assert.Equal(50, item.TotalByteSize); // 10 * 5

        var table = model.SymbolTable.Resolve("WS-TABLE");
        Assert.Equal(50, table!.ByteSize); // group size = sum of children total
    }

    [Fact]
    public void Redefines_SharesOffset()
    {
        var model = Analyze("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TEST7.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-DATE PIC 9(8).
            01 WS-DATE-PARTS REDEFINES WS-DATE.
               05 WS-YEAR PIC 9(4).
               05 WS-MONTH PIC 99.
               05 WS-DAY PIC 99.
            PROCEDURE DIVISION.
                STOP RUN.
            """);

        var date = model.SymbolTable.Resolve("WS-DATE");
        var parts = model.SymbolTable.Resolve("WS-DATE-PARTS");
        Assert.NotNull(date);
        Assert.NotNull(parts);
        Assert.Equal(date!.Offset, parts!.Offset);
        Assert.NotNull(parts.RedefinesTarget);
    }

    [Fact]
    public void ChildParent_References()
    {
        var model = Analyze("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TEST8.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-REC.
               05 WS-FLD1 PIC X(5).
               05 WS-FLD2 PIC 99.
            PROCEDURE DIVISION.
                STOP RUN.
            """);

        var fld1 = model.SymbolTable.Resolve("WS-FLD1");
        Assert.NotNull(fld1);
        Assert.NotNull(fld1!.Parent);
        Assert.Equal("WS-REC", fld1.Parent!.Name);
    }

    [Fact]
    public void Offsets_Computed()
    {
        var model = Analyze("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TEST9.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-REC.
               05 WS-A PIC X(10).
               05 WS-B PIC X(5).
               05 WS-C PIC 999.
            PROCEDURE DIVISION.
                STOP RUN.
            """);

        var a = model.SymbolTable.Resolve("WS-A");
        var b = model.SymbolTable.Resolve("WS-B");
        var c = model.SymbolTable.Resolve("WS-C");
        Assert.Equal(0, a!.Offset);
        Assert.Equal(10, b!.Offset);
        Assert.Equal(15, c!.Offset);
    }
}
