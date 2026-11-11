// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol;
using WiseOwl.Cobol.Frontend.Preprocessor;
using Xunit;
using WiseOwl.Cobol.Tests.Shared;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// The printed order of the paired conditional phrases at COBOL-85 (kb/Work PB5158; construct
/// <c>conditional-phrases-any-order-2002</c>). "Clauses must be written in the sequence given in the general formats"
/// (§1985:IV.2.1), and every 1985 format prints the positive phrase before its NOT twin; ISO/IEC 1989:2002 Annex F.2
/// item 33 lets them "be specified in any order" (§2002:F.2), and RETURN's twin is §2023:14.9.34.3 SR4. Every statement
/// that writes the one <c>conditionalPhrases</c> rule and exists at 85 is compiled with its NOT phrase first, refused at
/// --std 85 at the late phrase and accepted at --std 2002, and the printed order is accepted at 85.
/// </summary>
public sealed class ConditionalPhraseOrderTests
{
    private static string L(string programText) => "       " + programText;

    /// <summary>A program whose statement under test is lines 34-35, inside a sort output procedure (RETURN's home).</summary>
    private static string Program(string head, string first, string second, string terminator) => string.Join("\n",
        L("IDENTIFICATION DIVISION."),
        L("PROGRAM-ID. CPORDER."),
        L("ENVIRONMENT DIVISION."),
        L("INPUT-OUTPUT SECTION."),
        L("FILE-CONTROL."),
        L("    SELECT SF ASSIGN TO \"SF.DAT\"."),
        L("    SELECT IX ASSIGN TO \"IX.DAT\" ORGANIZATION INDEXED"),
        L("        ACCESS MODE DYNAMIC RECORD KEY IK."),
        L("    SELECT SR ASSIGN TO \"SR.TMP\"."),
        L("    SELECT PRTF ASSIGN TO \"PRTF.DAT\"."),
        L("DATA DIVISION."),
        L("FILE SECTION."),
        L("FD  SF."),
        L("01  SREC PIC X(10)."),
        L("FD  IX."),
        L("01  IREC."),
        L("    05 IK PIC X(4)."),
        L("    05 ID1 PIC X(6)."),
        L("SD  SR."),
        L("01  RREC PIC X(10)."),
        L("FD  PRTF LINAGE 10 FOOTING 8."),
        L("01  PREC PIC X(10)."),
        L("WORKING-STORAGE SECTION."),
        L("01  A PIC 9 VALUE 0."),
        L("01  S PIC X(10)."),
        L("01  T PIC X(3) VALUE \"ABC\"."),
        L("PROCEDURE DIVISION."),
        L("P0 SECTION."),
        L("P1."),
        L("    SORT SR ON ASCENDING KEY RREC USING SF OUTPUT PROCEDURE P2."),
        L("    STOP RUN."),
        L("P2 SECTION."),
        L("P3."),
        L($"    {head} {first} DISPLAY \"1\""),
        L($"        {second} DISPLAY \"2\" {terminator}."),
        "");

    private static CompilerDriver.Result Check(int edition, string source)
    {
        string dir = ScratchDirectory.Create("phrase-order");
        try
        {
            string src = Path.Combine(dir, "MAIN.cob");
            File.WriteAllText(src, source);
            return CompilerDriver.Compile(new CompilerDriver.Options(src, DialectLevel: edition, CheckOnly: true,
                SourceFormat: InitialReferenceFormat.Fixed));
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>Every owner of <c>conditionalPhrases</c> that COBOL-85 prints with a phrase pair (§1985 VI.6, VII.4,
    /// VIII.4, IX.4, XII.4 and X CALL), with the pair's positive phrase as written.</summary>
    public static TheoryData<string, string, string> Statements => new()
    {
        { "ADD 1 TO A", "ON SIZE ERROR", "END-ADD" },
        { "SUBTRACT 1 FROM A", "ON SIZE ERROR", "END-SUBTRACT" },
        { "MULTIPLY 2 BY A", "ON SIZE ERROR", "END-MULTIPLY" },
        { "DIVIDE 2 INTO A", "ON SIZE ERROR", "END-DIVIDE" },
        { "COMPUTE A = 100", "ON SIZE ERROR", "END-COMPUTE" },
        { "STRING T DELIMITED SIZE INTO S", "ON OVERFLOW", "END-STRING" },
        { "UNSTRING T INTO S", "ON OVERFLOW", "END-UNSTRING" },
        { "CALL \"SUBP\"", "ON EXCEPTION", "END-CALL" },
        { "READ SF", "AT END", "END-READ" },
        { "RETURN SR", "AT END", "END-RETURN" },
        { "READ IX KEY IK", "INVALID KEY", "END-READ" },
        { "WRITE IREC", "INVALID KEY", "END-WRITE" },
        { "REWRITE IREC", "INVALID KEY", "END-REWRITE" },
        { "DELETE IX RECORD", "INVALID KEY", "END-DELETE" },
        { "START IX KEY = IK", "INVALID KEY", "END-START" },
        { "WRITE PREC", "AT END-OF-PAGE", "END-WRITE" },
    };

    [Theory]
    [MemberData(nameof(Statements))]
    public void NotPhraseFirst_IsRefusedAt85AtTheLatePhrase_AndAcceptedFrom2002(string head, string positive, string terminator)
    {
        string source = Program(head, "NOT " + positive, positive, terminator);
        var at85 = Check(85, source);
        Assert.False(at85.Success);
        string verb = head.Split(' ')[0];
        Assert.Contains(at85.Errors, e => e.Contains("(35,16): error COBOL0900", StringComparison.Ordinal)
            && e.Contains($"the {verb} statement writes {positive} after NOT {positive}", StringComparison.Ordinal));
        Assert.Single(at85.Errors);
        var at2002 = Check(2002, source);
        Assert.True(at2002.Success, string.Join("\n", at2002.Errors));
    }

    [Theory]
    [MemberData(nameof(Statements))]
    public void PrintedOrder_IsAcceptedAt85(string head, string positive, string terminator)
    {
        var at85 = Check(85, Program(head, positive, "NOT " + positive, terminator));
        Assert.True(at85.Success, string.Join("\n", at85.Errors));
    }
}
