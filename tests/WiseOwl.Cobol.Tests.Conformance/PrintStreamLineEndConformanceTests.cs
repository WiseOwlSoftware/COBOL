// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// ⛔ THE LINE END OF A FILE'S MEDIUM IS A FUNCTION OF THE FILE'S SHAPE, IN THREE ARMS (kb/Work PB1664;
/// docs/CONFORMANCE.md DOC-A.1-146 (c′), DOC-A.1-159, DOC-A.1-114). ISO leaves the on-medium form of a record to
/// the implementor (§2023:9.1.7.2 for a record sequential record, §2023:13.4.5.4 GR4 for a report writer's logical record,
/// §2023:12.4.5.10.3 GR2 for a line sequential delimiter), so CLAUDE.md rule 1's precedence decides and GnuCOBOL 3.2
/// answers:
/// <list type="bullet">
/// <item>a LINE SEQUENTIAL file, a file with a LINAGE clause (which GnuCOBOL reclassifies LINE SEQUENTIAL), and
///   every REPORT file (reclassified the same way): the C library's text-mode newline — the HOST newline, CR LF
///   on Windows and LF on Linux and macOS;</item>
/// <item>any other RECORD SEQUENTIAL print stream — a file written with an ADVANCING phrase — a binary sequential
///   file: LF on EVERY host.</item>
/// </list>
/// Before PB1664 the second arm was CR LF on every host and a LINE SEQUENTIAL report file bypassed the first
/// (<c>FileRegistry.RegisterReport</c> registers every report file as record sequential), so it was CR LF on Linux
/// where GnuCOBOL writes LF. ADVANCING 0's bare carriage return (§2023:14.9.51.4 GR25 c), owner decision R54) and
/// ADVANCING PAGE's form feed (GR25 h)) are line CONTROLS, not line ends, and are asserted unchanged.
/// </summary>
public sealed class PrintStreamLineEndConformanceTests
{
    private static readonly WiseOwlCobolCompiler WiseOwlCobol2023 = new(2023);

    private static string Host(string s) => s.Replace("{nl}", Environment.NewLine, StringComparison.Ordinal);

    private static string Fd(string org, string fdClauses, string id, string file, string proc) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {id}.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT F1 ASSIGN TO "{file}" ORGANIZATION IS {org}.
        DATA DIVISION.
        FILE SECTION.
        FD F1 {fdClauses}.
        01 R1 PIC X(4).
        PROCEDURE DIVISION.
            OPEN OUTPUT F1
        {proc}
            CLOSE F1
            STOP RUN.
        """;

    private static void AssertMedium(string source, string file, string expected)
    {
        var (ok, _, detail, bytes) = WiseOwlCobol2023.CompileRunAndReadFile(source, file);
        Assert.True(ok, $"WiseOwl COBOL failed: {detail}");
        Assert.NotNull(bytes);
        Assert.Equal(expected, Encoding.Latin1.GetString(bytes!));
    }

    private const string TwoAfterWrites = """
            MOVE "AAAA" TO R1
            WRITE R1 AFTER ADVANCING 1 LINE
            MOVE "BBBB" TO R1
            WRITE R1 AFTER ADVANCING 1 LINE
        """;

    /// <summary>Arm 2: a record sequential print stream with no LINAGE clause ends every line with LF on every
    /// host, and its controls are unchanged — a bare CR for ADVANCING 0 and a form feed for ADVANCING PAGE.</summary>
    [Fact]
    public void RecordSequentialPrintStream_EndsLinesWithLfOnEveryHost()
        => AssertMedium(Fd("SEQUENTIAL", "", "PLE1", "ple1.dat", TwoAfterWrites), "ple1.dat", "\nAAAA\nBBBB\n");

    [Fact]
    public void RecordSequentialPrintStream_KeepsItsLineControls()
        => AssertMedium(Fd("SEQUENTIAL", "", "PLE2", "ple2.dat", """
                MOVE "AAAA" TO R1
                WRITE R1 AFTER ADVANCING PAGE
                MOVE "BBBB" TO R1
                WRITE R1 AFTER ADVANCING 0 LINES
                MOVE "CCCC" TO R1
                WRITE R1 AFTER ADVANCING 1 LINE
            """), "ple2.dat", "\fAAAA\rBBBB\nCCCC\n");

    private const string TwoShortAfterWrites = """
            MOVE "AA" TO R1
            WRITE R1 AFTER ADVANCING 1 LINE
            MOVE "BB" TO R1
            WRITE R1 AFTER ADVANCING 1 LINE
        """;

    /// <summary>⛔ A RECORD SEQUENTIAL PRINT STREAM WRITES EACH FIXED-LENGTH RECORD AT ITS WHOLE SIZE (kb/Work PB2509;
    /// DOC-A.1-146 (c′)). §2023:14.9.51.4 GR21's trailing-space strip is stated for a LINE SEQUENTIAL file alone, §2023:13.18.43.4
    /// GR6 gives every record of a fixed-length file integer-1 bytes, and §2023:9.1.7.2 leaves the on-medium form to the
    /// implementor, so GnuCOBOL's whole record is the answer: <c>AA</c> in a 4-byte record is <c>AA</c> and two spaces.
    /// The controls (AFTER 1, ADVANCING 0's bare CR, PAGE's form feed) stand between whole records.</summary>
    [Fact]
    public void RecordSequentialPrintStream_WritesTheWholeFixedRecord()
        => AssertMedium(Fd("SEQUENTIAL", "", "PLE8", "ple8.dat", TwoShortAfterWrites), "ple8.dat", "\nAA  \nBB  \n");

    [Fact]
    public void RecordSequentialPrintStream_KeepsItsLineControlsBetweenWholeRecords()
        => AssertMedium(Fd("SEQUENTIAL", "", "PLE9", "ple9.dat", """
                MOVE "AA" TO R1
                WRITE R1 AFTER ADVANCING PAGE
                MOVE "BB" TO R1
                WRITE R1 AFTER ADVANCING 0 LINES
                MOVE "CC" TO R1
                WRITE R1 BEFORE ADVANCING 1 LINE
            """), "ple9.dat", "\fAA  \rBB  \rCC  \n");

    /// <summary>The other two shapes keep GR21's strip: GnuCOBOL reclassifies a LINAGE file LINE SEQUENTIAL, and a
    /// LINE SEQUENTIAL file's rule is GR21's own — the record less its trailing spaces.</summary>
    [Theory]
    [InlineData("SEQUENTIAL", "LINAGE IS 5 LINES", "PLE10", "ple10.dat")]
    [InlineData("LINE SEQUENTIAL", "", "PLE11", "ple11.dat")]
    [InlineData("LINE SEQUENTIAL", "LINAGE IS 5 LINES", "PLE12", "ple12.dat")]
    public void LinageAndLineSequentialFiles_StillStripTrailingSpaces(string org, string fd, string id, string file)
        => AssertMedium(Fd(org, fd, id, file, TwoShortAfterWrites), file, Host("{nl}AA{nl}BB{nl}"));

    private static string VaryingSource(string id, string file, string proc) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {id}.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT F1 ASSIGN TO "{file}" ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS.
        DATA DIVISION.
        FILE SECTION.
        FD F1 RECORD IS VARYING IN SIZE FROM 2 TO 4 CHARACTERS DEPENDING ON LEN.
        01 R1 PIC X(4).
        WORKING-STORAGE SECTION.
        01 FS PIC XX.
        01 LEN PIC 9(4) COMP.
        PROCEDURE DIVISION.
            OPEN OUTPUT F1
        {proc}
            CLOSE F1
            STOP RUN.
        """;

    /// <summary>⛔ A RECORD VARYING RECORD SEQUENTIAL PRINT STREAM FRAMES EACH RECORD (kb/Work PB2509; DOC-A.1-151 (a)):
    /// the 4-byte little-endian length prefix, then the record at its §2023:13.18.43.4 GR13 length, trailing spaces
    /// included (a record of <c>"A "</c> at length 2 is two bytes). The print controls stand between frames.</summary>
    [Fact]
    public void RecordSequentialVaryingPrintStream_FramesEachRecord()
        => AssertMedium(VaryingSource("PLE13", "ple13.dat", """
                MOVE "ABC " TO R1
                MOVE 3 TO LEN
                WRITE R1 AFTER ADVANCING 1 LINE
                MOVE "A   " TO R1
                MOVE 2 TO LEN
                WRITE R1 AFTER ADVANCING 1 LINE
                MOVE "WXYZ" TO R1
                MOVE 4 TO LEN
                WRITE R1
            """), "ple13.dat", "\n\u0003\0\0\0ABC\n\u0002\0\0\0A \n\u0004\0\0\0WXYZ\n");

    /// <summary>§2023:14.9.51.4 GR14/GR15 on the framed stream: a length outside the bounds is the size violation '44'
    /// and the record reaches the medium neither framed nor bare — the stream holds only the frames of the writes that
    /// succeeded, and the refused WRITE leaves no line feed behind either.</summary>
    [Fact]
    public void RecordSequentialVaryingPrintStream_RefusedLengthWritesNoFrame()
        => AssertMedium(VaryingSource("PLE14", "ple14.dat", """
                MOVE "AB  " TO R1
                MOVE 9 TO LEN
                WRITE R1 AFTER ADVANCING 1 LINE
                IF FS NOT = "44" DISPLAY "STATUS " FS END-IF
                MOVE "AB  " TO R1
                MOVE 1 TO LEN
                WRITE R1 AFTER ADVANCING 1 LINE
                IF FS NOT = "44" DISPLAY "STATUS " FS END-IF
                MOVE "AB  " TO R1
                MOVE 2 TO LEN
                WRITE R1 AFTER ADVANCING 1 LINE
            """), "ple14.dat", "\n\u0002\0\0\0AB\n");

    /// <summary>Arm 1: the other two shapes of a record sequential file that is a print stream by its FD, and the
    /// line sequential file itself — the host newline.</summary>
    [Theory]
    [InlineData("SEQUENTIAL", "LINAGE IS 5 LINES", "PLE3", "ple3.dat")]
    [InlineData("LINE SEQUENTIAL", "", "PLE4", "ple4.dat")]
    [InlineData("LINE SEQUENTIAL", "LINAGE IS 5 LINES", "PLE5", "ple5.dat")]
    public void LinageAndLineSequentialFiles_EndLinesWithTheHostNewline(string org, string fd, string id, string file)
        => AssertMedium(Fd(org, fd, id, file, TwoAfterWrites), file, Host("{nl}AAAA{nl}BBBB{nl}"));

    /// <summary>Arm 1 for the report writer: EVERY report file — a LINE SEQUENTIAL one included (§2023:13.4.5.3 SR4
    /// makes it legal) — ends its lines with the host newline.</summary>
    [Theory]
    [InlineData("SEQUENTIAL", "PLE6", "ple6.rpt")]
    [InlineData("LINE SEQUENTIAL", "PLE7", "ple7.rpt")]
    public void ReportFiles_EndLinesWithTheHostNewline(string org, string id, string file)
    {
        string source = $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {id}.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT PFILE ASSIGN TO "{file}" ORGANIZATION IS {org}.
        DATA DIVISION.
        FILE SECTION.
        FD PFILE REPORT IS R1.
        REPORT SECTION.
        RD R1.
        01 DL TYPE IS DETAIL LINE PLUS 1.
           05 COLUMN 1 PIC X(2) VALUE "DD".
        PROCEDURE DIVISION.
            OPEN OUTPUT PFILE
            INITIATE R1
            GENERATE DL
            GENERATE DL
            TERMINATE R1
            CLOSE PFILE
            STOP RUN.
        """;
        AssertMedium(source, file, Host("DD{nl}DD{nl}"));
    }
}
