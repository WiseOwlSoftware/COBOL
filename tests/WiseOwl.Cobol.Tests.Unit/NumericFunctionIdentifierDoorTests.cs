// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Compiler;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ EVERY DOOR THAT BINDS A FUNCTION-IDENTIFIER WHERE AN IDENTIFIER STANDS ASKS COBOL-85's PLACEMENT RULE (kb/Work PB5632,
/// PB5108). "A function-identifier which references an integer or numeric function may be used only in an arithmetic
/// expression" (§1985:IV.4.3.8.3.3 SR4, restated by the 1989 Amendment 1), so at --std 85 a numeric function written as an
/// identifier operand is refused with COBOL0900 (construct <c>numeric-function-identifier-operand-2002</c>), through
/// <c>IntrinsicBinder.ScreenIdentifierPlacement</c>; from 2002 it is a numeric sender (§2002:F.2 item 60). The screen has three
/// doors: <c>IntrinsicBinder.IntrinsicOperand</c> (every sending operand but the arithmetic verbs'),
/// <c>ExpressionBinder.BindArithmeticStatementOperand</c> (ADD, SUBTRACT, MULTIPLY, DIVIDE) and
/// <c>ControlFlowBinder.BindVaryingOperand</c> (PERFORM VARYING). Deleting one door's call left the gate green until each had
/// a test of its own.
/// <para>Each row of <see cref="Doors"/> is one statement, refused at 85 and not at 2002, so the call that stops asking fails
/// ITS row. <see cref="EveryFileThatBindsAFunctionOperand_IsNamedByARowOrAReason"/> reads the compiler's sources for the files
/// that call a door and fails for a file no row (or stated reason) accounts for, so a new door cannot arrive untested.</para>
/// </summary>
public sealed class NumericFunctionIdentifierDoorTests
{
    private static string L(string text) => "       " + text;

    private static string Program(string statement) => string.Join("\n",
        L("IDENTIFICATION DIVISION."),
        L("PROGRAM-ID. DOORS."),
        L("ENVIRONMENT DIVISION."),
        L("INPUT-OUTPUT SECTION."),
        L("FILE-CONTROL."),
        L("    SELECT PRTF ASSIGN TO \"PRTF.DAT\"."),
        L("DATA DIVISION."),
        L("FILE SECTION."),
        L("FD  PRTF."),
        L("01  PREC PIC X(10)."),
        L("WORKING-STORAGE SECTION."),
        L("01  N PIC 9(3) VALUE 7."),
        L("01  Y PIC 9(3) VALUE 10."),
        L("01  K PIC 9(3)."),
        L("01  C PIC 9(3) VALUE 0."),
        L("01  W PIC X(4) VALUE \"ABCD\"."),
        L("01  G."),
        L("    05 G1 PIC 9(3)."),
        L("    05 G2 PIC X(3)."),
        L("COMMUNICATION SECTION."),
        L("CD  CIO FOR I-O"),
        L("    MESSAGE DATE IS IO-DATE"),
        L("    MESSAGE TIME IS IO-TIME"),
        L("    SYMBOLIC TERMINAL IS IO-TERM"),
        L("    TEXT LENGTH IS IO-LEN"),
        L("    END KEY IS IO-END"),
        L("    STATUS KEY IS IO-STAT."),
        L("PROCEDURE DIVISION."),
        L("P1."),
        L("    " + statement.Replace("~", "\n" + L("        "))),   // "~" ends a line: Area B stops at column 72
        L("    STOP RUN."),
        L("P2."),
        L("    EXIT."),
        L("P3."),
        L("    EXIT."),
        "");

    private static CompilerDriver.Result Check(int edition, string source)
    {
        string dir = ScratchDirectory.Create("fn-door");
        try
        {
            string src = Path.Combine(dir, "MAIN.cob");
            File.WriteAllText(src, source);
            return CompilerDriver.Compile(new CompilerDriver.Options(src, EditionInfo.Of(edition), CheckOnly: true,
                SourceFormat: InitialReferenceFormat.Fixed));
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>The refusal's own words (construct <c>numeric-function-identifier-operand-2002</c>).</summary>
    private const string Refusal = "a numeric or integer function-identifier where an identifier is written";

    /// <summary>One statement per door: its name, and the statement written with a numeric function where an identifier stands.</summary>
    public static TheoryData<string, string> Doors => new()
    {
        { "MOVE", "MOVE FUNCTION SQRT (4) TO N." },
        { "ADD", "ADD FUNCTION LENGTH (W) TO Y." },
        { "SUBTRACT", "SUBTRACT FUNCTION SQRT (4) FROM Y." },
        { "MULTIPLY", "MULTIPLY FUNCTION SQRT (4) BY Y." },
        { "DIVIDE", "DIVIDE FUNCTION SQRT (4) INTO Y." },
        { "PERFORM VARYING FROM", "PERFORM VARYING K FROM FUNCTION LENGTH (W) BY 1~UNTIL K > 5 ADD 1 TO C END-PERFORM." },
        { "PERFORM VARYING BY", "PERFORM VARYING K FROM 1 BY FUNCTION SQRT (4)~UNTIL K > 5 ADD 1 TO C END-PERFORM." },
        { "PERFORM TIMES", "PERFORM FUNCTION INTEGER (2.5) TIMES ADD 1 TO C END-PERFORM." },
        { "DISPLAY", "DISPLAY FUNCTION LENGTH (W)." },
        { "GO TO DEPENDING", "GO TO P2 P3 DEPENDING ON FUNCTION INTEGER (1)." },
        { "WRITE ADVANCING", "WRITE PREC AFTER ADVANCING FUNCTION SQRT (4) LINES." },
        { "INITIALIZE REPLACING", "INITIALIZE G REPLACING NUMERIC BY FUNCTION SQRT (4)." },
        { "SEND", "SEND CIO FROM FUNCTION SQRT (4) WITH EMI." },
        { "INSPECT identifier-1", "INSPECT FUNCTION LENGTH (W) TALLYING C FOR ALL \"A\"." },
        { "INSPECT pattern", "INSPECT W TALLYING C FOR ALL FUNCTION LENGTH (W)." },
    };

    [Theory]
    [MemberData(nameof(Doors))]
    public void ANumericFunctionWhereAnIdentifierStands_IsRefusedAt85AndAcceptedFrom2002(string door, string statement)
    {
        string source = Program(statement);
        var at85 = Check(85, source);
        Assert.False(at85.Success);
        Assert.True(at85.Errors.Any(e => e.Contains("error COBOL0900", StringComparison.Ordinal)
                && e.Contains(Refusal, StringComparison.Ordinal)),
            $"{door}: no COBOL0900 for the numeric function-identifier at --std 85:\n{string.Join("\n", at85.Errors)}");
        var at2002 = Check(2002, source);
        Assert.DoesNotContain(at2002.Errors, e => e.Contains(Refusal, StringComparison.Ordinal));
    }

    /// <summary>A statement that gives a numeric function its OWN refusal at every edition draws the placement verdict too, at 85, and
    /// the two arrive in one order: the placement verdict (the cause: the function is written where an identifier stands) first,
    /// the statement's rule after it (kb/Work PB5632). STRING draws only its own (COBOL1626) there.</summary>
    [Theory]
    [InlineData("INSPECT FUNCTION LENGTH (W) TALLYING C FOR ALL \"A\".", "COBOL1626")]
    [InlineData("CANCEL FUNCTION LENGTH (W).", "COBOL1696")]
    public void TheStatementsOwnRefusal_FollowsThePlacementVerdict(string statement, string own)
    {
        var errors = Check(85, Program(statement)).Errors.ToList();
        int placement = errors.FindIndex(e => e.Contains("error COBOL0900", StringComparison.Ordinal)
            && e.Contains(Refusal, StringComparison.Ordinal));
        int statementOwn = errors.FindIndex(e => e.Contains("error " + own, StringComparison.Ordinal));
        Assert.True(placement >= 0 && statementOwn >= 0, $"expected COBOL0900 and {own}:\n{string.Join("\n", errors)}");
        Assert.True(placement < statementOwn, $"the placement verdict must come before {own}:\n{string.Join("\n", errors)}");
    }

    /// <summary>The files of the compiler that call a function-identifier door, and the rows (or the reason) that account for each.</summary>
    private static readonly Dictionary<string, string[]> FileRows = new()
    {
        ["MoveBinder.cs"] = ["MOVE", "INITIALIZE REPLACING"],
        ["ExpressionBinder.cs"] = ["ADD", "SUBTRACT", "MULTIPLY", "DIVIDE"],
        ["ControlFlowBinder.PerformVarying.cs"] = ["PERFORM VARYING FROM", "PERFORM VARYING BY"],
        ["ControlFlowBinder.cs"] = ["PERFORM TIMES", "GO TO DEPENDING"],
        ["AcceptDisplayBinder.cs"] = ["DISPLAY"],
        ["SequentialIoBinder.cs"] = ["WRITE ADVANCING"],
        ["CommunicationBinder.cs"] = ["SEND"],
        ["InspectBinder.cs"] = ["INSPECT identifier-1", "INSPECT pattern"],
    };

    /// <summary>The files whose calls no 85 statement reaches: the text names why, and the test that proves it.</summary>
    private static readonly Dictionary<string, string> FileReasons = new()
    {
        ["CallBinder.cs"] = "a function-identifier as a CALL USING argument is refused at 85 outright (COBOL0900, §1985:X.5.2.3 SR4), whatever its category",
        ["MessagingBinder.cs"] = "the messaging SEND binds only where its construct exists (StatementBinder.BindIntroducedAt: a no-op at 85, the edition pass's COBOL0900 is the answer)",
        ["OoBinder.cs"] = "INVOKE and its method arguments are COBOL-2002 object-orientation constructs, refused at 85 (COBOL0900, ISO §2023:14.9.23)",
    };

    private static string StripComments(string src) => Regex.Replace(src, @"//[^\r\n]*", "");

    [Fact]
    public void EveryFileThatBindsAFunctionOperand_IsNamedByARowOrAReason()
    {
        var callers = Directory.EnumerateFiles(TestRepo.Src("WiseOwl.Cobol.Compiler", "Binding", "Procedure"), "*.cs", SearchOption.AllDirectories)
            .Where(f => Path.GetFileName(f) != "IntrinsicBinder.cs")
            .Where(f => Regex.IsMatch(StripComments(File.ReadAllText(f)), @"\.(IntrinsicOperand|ScreenIdentifierPlacement)\s*\("))
            .Select(Path.GetFileName).OfType<string>().OrderBy(f => f, StringComparer.Ordinal).ToList();
        var unaccounted = callers.Where(f => !FileRows.ContainsKey(f) && !FileReasons.ContainsKey(f)).ToList();
        Assert.True(unaccounted.Count == 0,
            "These files call IntrinsicBinder.IntrinsicOperand or ScreenIdentifierPlacement (a function-identifier where an identifier "
            + "stands, which --std 85 refuses when numeric, kb/Work PB5108) and no row of Doors, nor a reason, accounts for them: "
            + string.Join(", ", unaccounted));
        var stale = FileRows.Keys.Concat(FileReasons.Keys).Where(f => !callers.Contains(f)).ToList();
        Assert.True(stale.Count == 0, "These files no longer call a function-identifier door: " + string.Join(", ", stale));
    }

    [Fact]
    public void EveryRowNamedByAFile_ExistsInDoors()
    {
        var rows = Doors.Select(d => (string)d[0]).ToHashSet();
        foreach (var (file, named) in FileRows)
            foreach (string row in named)
                Assert.True(rows.Contains(row), $"{file} names the row '{row}', which Doors does not hold");
        Assert.Contains("MOVE", rows);
    }
}
