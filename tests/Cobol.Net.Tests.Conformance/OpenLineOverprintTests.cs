// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// A line presented while an earlier write's line is still OPEN is presented at the SAME vertical position as that
/// line - an overprint from column 1, never a continuation of its text (kb/Work PB1840; ISO/IEC 1989:2023
/// §14.9.51.4 GR25 c), e), f), g), h)). An AFTER write presents after its advance (GR25 f)), so its line is open
/// when the next statement runs; a BEFORE write presents before its advance (GR25 e)), so no advance lies between
/// the two lines and the second lands on the first's vertical position, exactly as GR25 c)'s zero advance puts
/// it. The device representation of "the same vertical position" is the bare carriage return of ADVANCING 0
/// (docs/CONFORMANCE.md "ADVANCING 0", owner decision kb/Work R54).
/// <para>The tests assert the BYTES of the print file and the I-O status of every WRITE. The refused-WRITE tests
/// (kb/Work PB3353, the owner's exception-path rule) pin that a statement which presents nothing - '44' outside the
/// RECORD IS VARYING bounds (GR14, GR15), '71' for a character outside the line sequential character set (GR23),
/// and a LINAGE violation at the page transition (§13.18.34.4 GR6 b) 2) - neither writes the overprint return nor
/// closes the open line, and that the BEFORE write that follows overprints exactly once.</para>
/// </summary>
public sealed class OpenLineOverprintTests
{
    private static readonly CobolNetCompiler Cn85 = new();
    private static readonly CobolNetCompiler Cn2023 = new(2023);
    private const string LineSequentialOrg = "\n        ORGANIZATION IS LINE SEQUENTIAL";

    /// <summary>One WRITE of <paramref name="rec"/> under <paramref name="clause"/>, then its I-O status;
    /// <paramref name="pre"/> is COBOL run after the record is moved and before the WRITE.</summary>
    private static string W(string rec, string clause, string pre = "") =>
        $"""
            MOVE "{rec}" TO P-REC.
            {pre}
            WRITE P-REC {clause}.
            DISPLAY "FS=" FS.
        """;

    private static string Source(string id, string org, string fd, string ws, string declarative, string proc) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {id}.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT LPF ASSIGN TO "{id}.prt"{org}
                FILE STATUS IS FS.
        DATA DIVISION.
        FILE SECTION.
        FD LPF
            {fd}.
        01 P-REC PIC X(4).
        WORKING-STORAGE SECTION.
        01 FS PIC XX VALUE "??".
        {ws}
        PROCEDURE DIVISION.
        {declarative}
        MAIN-PARA.
            OPEN OUTPUT LPF.
        {proc}
            CLOSE LPF.
            DISPLAY "CLOSE=" FS.
            STOP RUN.
        """;

    private const string Declarative = """
        DECLARATIVES.
        ERR-SECT SECTION.
            USE AFTER STANDARD ERROR PROCEDURE ON LPF.
        ERR-PARA.
            DISPLAY "USE=" FS.
        END DECLARATIVES.
        MAIN-SECT SECTION.
        """;

    /// <summary>Compile and run, then assert the exact bytes of the print file and the stdout (the statuses).
    /// A <c>\r\n</c> in <paramref name="bytes"/> is a LINE END and is mapped to the host newline for a LINE
    /// SEQUENTIAL file or a file with a LINAGE clause, and to a line feed for any other record sequential print
    /// stream (docs/CONFORMANCE.md DOC-A.1-146 (c'), kb/Work PB1664); a lone <c>\r</c> and <c>\f</c> are controls.</summary>
    private static void Assert2(CobolNetCompiler compiler, string id, string source, string bytes, string stdout)
    {
        var (ok, output, detail, produced) = compiler.CompileRunAndReadFile(source, id + ".prt");
        Assert.True(ok, $"WiseOwl COBOL failed: {detail}");
        Assert.NotNull(produced);
        bool hostNewline = source.Contains("LINE SEQUENTIAL", StringComparison.Ordinal)
            || source.Contains("LINAGE IS", StringComparison.Ordinal);
        string expected = bytes.Replace("\r\n", hostNewline ? Environment.NewLine : "\n", StringComparison.Ordinal);
        Assert.Equal(expected, System.Text.Encoding.Latin1.GetString(produced!));
        Assert.Equal(stdout, output.Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd('\n'));
    }

    // ── The success paths: one rule, every shape of print file ──────────────────────────────────────────────

    [Theory]
    // A record sequential print stream (line end LF). GR25 f) then e): AAAA is presented after the advance, so its
    // line is open; BBBB is presented before ITS advance, onto that same line. The plain WRITE is an AFTER
    // ADVANCING 1 LINE (GR25, kb/Work PB1027), so it advances once more: a blank line follows a BEFORE write.
    [InlineData("OLRS1", "AFTER ADVANCING 1 LINE", "BEFORE ADVANCING 1 LINE", "\r\nAAAA\rBBBB\r\n\r\nCCCC\r\n")]
    // GR25 g)/h) change only the advance, not the presentation's place: BBBB overprints AAAA, and the form feed
    // follows it. The plain write after the page advance is one more AFTER 1.
    [InlineData("OLRS2", "AFTER ADVANCING 1 LINE", "BEFORE ADVANCING PAGE", "\r\nAAAA\rBBBB\f\r\nCCCC\r\n")]
    // GR25 c): the zero advance returns the carriage AFTER BBBB, so the device already stands at column 1 of the
    // open line and the plain write's own travel ends it: one return before BBBB, one after, never two in a row.
    [InlineData("OLRS3", "AFTER ADVANCING 1 LINE", "BEFORE ADVANCING 0 LINES", "\r\nAAAA\rBBBB\r\r\nCCCC\r\n")]
    // AFTER ADVANCING 0 returns first, then presents (GR25 c), f)): BBBB overprints AAAA, and its line is the
    // open one the plain write's travel ends.
    [InlineData("OLRS4", "AFTER ADVANCING 1 LINE", "AFTER ADVANCING 0 LINES", "\r\nAAAA\rBBBB\r\nCCCC\r\n")]
    // The control: nothing is open when BBBB is presented (AAAA's BEFORE advance ended its line), so there is
    // nothing to overprint and no return is written.
    [InlineData("OLRS5", "BEFORE ADVANCING 1 LINE", "BEFORE ADVANCING 1 LINE", "AAAA\r\nBBBB\r\n\r\nCCCC\r\n")]
    public void RecordSequential_BeforeOntoAnOpenLine_IsAnOverprint(string id, string first, string second, string bytes)
        => Assert2(Cn85, id, Source(id, "", "", "", "", $"""
                {W("AAAA", first)}
                {W("BBBB", second)}
                {W("CCCC", "")}
            """), bytes, "FS=00\nFS=00\nFS=00\nCLOSE=00");

    [Fact]
    // Zero-advance chains, record sequential (LF). AFTER 1 AAAA; BEFORE 0 BBBB (CR, BBBB, CR); BEFORE 1 CCCC finds
    // the device returned already: AAAA CR BBBB CR CCCC LF - two returns, not three.
    public void RecordSequential_ZeroAdvanceThenBefore_WritesNoSecondReturn()
        => Assert2(Cn85, "OLRS6", Source("OLRS6", "", "", "", "", $"""
                {W("AAAA", "AFTER ADVANCING 1 LINE")}
                {W("BBBB", "BEFORE ADVANCING 0 LINES")}
                {W("CCCC", "BEFORE ADVANCING 1 LINE")}
            """), "\r\nAAAA\rBBBB\rCCCC\r\n", "FS=00\nFS=00\nFS=00\nCLOSE=00");

    [Fact]
    // A plain WRITE is an explicit AFTER ADVANCING 1 LINE (GR25), so it leaves its line open exactly as an AFTER
    // write does and the BEFORE write that follows overprints it (the shape kb/Work PB1027 created).
    public void RecordSequential_PlainWriteThenBefore_IsAnOverprint()
        => Assert2(Cn85, "OLRS7", Source("OLRS7", "", "", "", "", $"""
                {W("AAAA", "AFTER ADVANCING 1 LINE")}
                {W("BBBB", "")}
                {W("CCCC", "BEFORE ADVANCING 1 LINE")}
            """), "\r\nAAAA\r\nBBBB\rCCCC\r\n", "FS=00\nFS=00\nFS=00\nCLOSE=00");

    [Fact]
    // The CLOSE of a stream whose last line a BEFORE write overprinted and ended writes no further line end (the
    // BEFORE advance ended the line), and one whose last BEFORE write is a zero advance ends it once.
    public void RecordSequential_CloseAfterAnOverprintedLine_EndsItOnce()
        => Assert2(Cn85, "OLRS8", Source("OLRS8", "", "", "", "", $"""
                {W("AAAA", "AFTER ADVANCING 1 LINE")}
                {W("BBBB", "BEFORE ADVANCING 0 LINES")}
            """), "\r\nAAAA\rBBBB\r\r\n", "FS=00\nFS=00\nCLOSE=00");

    [Theory]
    [InlineData("OLLS1", "AFTER ADVANCING 1 LINE", "BEFORE ADVANCING 1 LINE", "\r\nAAAA\rBBBB\r\n\r\nCCCC\r\n")]
    [InlineData("OLLS2", "AFTER ADVANCING 1 LINE", "BEFORE ADVANCING 0 LINES", "\r\nAAAA\rBBBB\r\r\nCCCC\r\n")]
    [InlineData("OLLS3", "AFTER ADVANCING 1 LINE", "BEFORE ADVANCING PAGE", "\r\nAAAA\rBBBB\f\r\nCCCC\r\n")]
    public void LineSequential_BeforeOntoAnOpenLine_IsAnOverprint(string id, string first, string second, string bytes)
        => Assert2(Cn2023, id, Source(id, LineSequentialOrg, "", "", "", $"""
                {W("AAAA", first)}
                {W("BBBB", second)}
                {W("CCCC", "")}
            """), bytes, "FS=00\nFS=00\nFS=00\nCLOSE=00");

    [Theory]
    // A LINAGE file travels the LOGICAL page (GR25 g), GR26 a)) but presents through the same Present, so the
    // overprint is identical. LINAGE IS 5: BBBB is on page line 2 over AAAA. After BEFORE ADVANCING PAGE the
    // device is on page line 1 of the next page: AAAA's line, three more body lines (counter 2 -> past line 5)
    // and the first line of the next page are four line ends, and the plain write's AFTER 1 adds a fifth.
    [InlineData("OLLG1", "BEFORE ADVANCING 1 LINE", "\r\nAAAA\rBBBB\r\n\r\nCCCC\r\n")]
    [InlineData("OLLG2", "BEFORE ADVANCING PAGE", "\r\nAAAA\rBBBB\r\n\r\n\r\n\r\n\r\nCCCC\r\n")]
    public void Linage_BeforeOntoAnOpenLine_IsAnOverprint(string id, string second, string bytes)
        => Assert2(Cn85, id, Source(id, "", "LINAGE IS 5 LINES", "", "", $"""
                {W("AAAA", "AFTER ADVANCING 1 LINE")}
                {W("BBBB", second)}
                {W("CCCC", "")}
            """), bytes, "FS=00\nFS=00\nFS=00\nCLOSE=00");

    [Fact]
    // The top margin is behind the device before the overprint return is decided: LINAGE IS 2 LINES AT TOP 1. The
    // margin line, AAAA's advance, then BBBB over AAAA and the BEFORE advance past the page body (counter 2 + 1
    // is past 2, the page-overflow arm of GR26 a): one line end onto the next page).
    public void Linage_WithATopMargin_OverprintFollowsTheMargin()
        => Assert2(Cn85, "OLLG3", Source("OLLG3", "", "LINAGE IS 2 LINES LINES AT TOP 1", "", "", $"""
                {W("AAAA", "AFTER ADVANCING 1 LINE")}
                {W("BBBB", "BEFORE ADVANCING 1 LINE")}
            """), "\r\n\r\nAAAA\rBBBB\r\n", "FS=00\nFS=00\nCLOSE=00");

    // ── The exception paths: a write that presents nothing leaves the open line exactly as it was ───────────

    [Fact]
    // GR14/GR15: a WRITE whose length is outside the RECORD IS VARYING bounds is unsuccessful ('44') and "the write
    // operation does not take place". AAAA's line stays open; the refused BEFORE write wrote no return; the next
    // BEFORE write (VL = 3, the record BBB) overprints once.
    public void RecordSequential_RefusedBeforeWrite_LeavesTheOpenLineUntouched()
        => Assert2(Cn85, "OLEX1", Source("OLEX1", "", "RECORD IS VARYING IN SIZE FROM 2 TO 4 CHARACTERS DEPENDING ON VL",
                "01 VL PIC 9 VALUE 4.", "", $"""
                {W("AAAA", "AFTER ADVANCING 1 LINE")}
                {W("XXXX", "BEFORE ADVANCING 1 LINE", "MOVE 9 TO VL. ")}
                {W("BBBB", "BEFORE ADVANCING 1 LINE", "MOVE 3 TO VL. ")}
            """), "\r\nAAAA\rBBB\r\n", "FS=00\nFS=44\nFS=00\nCLOSE=00");

    [Fact]
    // The same, with a USE declarative on the file: the '44' runs it (§9.1.13.7 / §14.9.51.4 GR15) and the bytes
    // are the same.
    public void RecordSequential_RefusedBeforeWrite_WithADeclarative_LeavesTheOpenLineUntouched()
        => Assert2(Cn85, "OLEX2", Source("OLEX2", "", "RECORD IS VARYING IN SIZE FROM 2 TO 4 CHARACTERS DEPENDING ON VL",
                "01 VL PIC 9 VALUE 4.", Declarative, $"""
                {W("AAAA", "AFTER ADVANCING 1 LINE")}
                {W("XXXX", "BEFORE ADVANCING 1 LINE", "MOVE 1 TO VL. ")}
                {W("BBBB", "BEFORE ADVANCING 1 LINE", "MOVE 3 TO VL. ")}
            """), "\r\nAAAA\rBBB\r\n", "FS=00\nUSE=44\nFS=44\nFS=00\nCLOSE=00");

    [Fact]
    // GR23: a line sequential record area holding a character outside the line sequential character set (here the
    // control character U+0001) is unsuccessful, status '71'. Nothing of that record reaches the medium, so no
    // return is written either, and the next BEFORE write overprints AAAA once.
    public void LineSequential_RefusedBeforeWrite_LeavesTheOpenLineUntouched()
        => Assert2(Cn2023, "OLEX3", Source("OLEX3", LineSequentialOrg, "", "", "", $"""
                {W("AAAA", "AFTER ADVANCING 1 LINE")}
                {W("XXXX", "BEFORE ADVANCING 1 LINE", "MOVE X\"01\" TO P-REC(1:1). ")}
                {W("BBBB", "BEFORE ADVANCING 1 LINE")}
            """), "\r\nAAAA\rBBBB\r\n", "FS=00\nFS=71\nFS=00\nCLOSE=00");

    [Fact]
    // §13.18.34.4 GR6 b) 2: the BEFORE write whose page transition re-reads a LINAGE operand that now violates the
    // rule has ALREADY presented its line (GR25 e) puts the presentation first): BBBB overprints AAAA, the status
    // is '90' and the rest of the statement is abandoned - there is no advance. The line is still open, so CLOSE
    // ends it. LINAGE-COUNTER is 0 and the latch makes the next WRITE unsuccessful too, presenting nothing.
    public void Linage_ViolationAtTheTransitionOfABeforeWrite_KeepsTheOverprintedLine()
        => Assert2(Cn85, "OLEX4", Source("OLEX4", "", "LINAGE IS WS-SZ LINES", "01 WS-SZ PIC 9(3) VALUE 2.", "", $"""
                {W("AAAA", "AFTER ADVANCING 1 LINE")}
                {W("BBBB", "BEFORE ADVANCING 1 LINE", "MOVE 0 TO WS-SZ. ")}
                {W("CCCC", "BEFORE ADVANCING 1 LINE")}
            """), "\r\nAAAA\rBBBB\r\n", "FS=00\nFS=90\nFS=90\nCLOSE=00");

    [Fact]
    // The same violation with a USE declarative on the file: it runs once per unsuccessful WRITE.
    public void Linage_ViolationAtTheTransitionOfABeforeWrite_WithADeclarative_RunsItPerWrite()
        => Assert2(Cn85, "OLEX5", Source("OLEX5", "", "LINAGE IS WS-SZ LINES", "01 WS-SZ PIC 9(3) VALUE 2.",
                Declarative, $"""
                {W("AAAA", "AFTER ADVANCING 1 LINE")}
                {W("BBBB", "BEFORE ADVANCING 1 LINE", "MOVE 0 TO WS-SZ. ")}
                {W("CCCC", "BEFORE ADVANCING 1 LINE")}
            """), "\r\nAAAA\rBBBB\r\n", "FS=00\nUSE=90\nFS=90\nUSE=90\nFS=90\nCLOSE=00");
}
