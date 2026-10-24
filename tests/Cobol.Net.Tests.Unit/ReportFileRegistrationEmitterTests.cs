// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet;
using CobolNet.Frontend.Preprocessor;
using Xunit;
using CobolNet.Tests.Shared;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A REPORT FILE REGISTERS THROUGH <c>CobolFile.RegisterReport</c>, AND ONLY A REPORT FILE DOES (kb/Work PB677,
/// PB1880 item 1). A report file has no record description (ISO §13.4.5.3 SR8; §9.1.22), so its connector's width is the
/// widest report's LINE WIDTH, not a record length, and the connector must be told it has no record description
/// (<c>recordDescribed: false</c>): otherwise the V59 layout notice ("not a whole multiple of the record length", which
/// compares the file's bytes with a record length) fires on every OPEN EXTEND of a report written by an earlier run,
/// where a line width is not a record length and the notice's "reads will be misaligned" is false. The runtime half is
/// <c>RecordLayoutNoticeTests.ReportFile_ExtendOverAnExistingReport_IsSilent_...</c>; this class pins the EMITTER's half,
/// the one place that decides which registration a SELECT gets, over both arms (the report file, and the file that has
/// records — the control, which must NOT take the report registration), so that a record-less FD that falls back to the
/// record arm, or a record FD that takes the report arm, fails here even if no golden crosses it.
/// <para>The conformance witness of the same behavior is <c>pb1880_report_file_open_extend</c> (OPEN EXTEND of a report
/// file answers '00', §9.1.13.2 item 1).</para>
/// </summary>
public sealed class ReportFileRegistrationEmitterTests
{
    private static string EmitCSharp(string source)
    {
        string dir = ScratchDirectory.Create("pb1880report");
        try
        {
            string src = Path.Combine(dir, "p.cob");
            File.WriteAllText(src, source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "p.dll"), DialectLevel: 2023, CheckOnly: false, SourceFormat: InitialReferenceFormat.Auto));
            Assert.True(r.Success, "the program must compile: " + string.Join("; ", r.Errors));
            Assert.NotNull(r.GeneratedCsPath);
            return File.ReadAllText(r.GeneratedCsPath!);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    private const string ReportAndRecordFiles = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. PB1880REG.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT RPT ASSIGN TO "pb1880reg.rpt".
            SELECT OPTIONAL RPT2 ASSIGN TO "pb1880reg2.rpt".
            SELECT REC ASSIGN TO "pb1880reg.dat" ORGANIZATION IS LINE SEQUENTIAL.
        DATA DIVISION.
        FILE SECTION.
        FD RPT REPORT IS R-ONE.
        FD RPT2 REPORT IS R-TWO.
        FD REC.
        01 REC-R PIC X(30).
        WORKING-STORAGE SECTION.
        01 WS-N PIC 9 VALUE 4.
        REPORT SECTION.
        RD R-ONE PAGE LIMIT IS 20 LINES HEADING 1 FIRST DETAIL 3 LAST DETAIL 15.
        01 DET-ONE TYPE DE LINE PLUS 1.
           02 COLUMN 1 PIC X(2) VALUE "D=".
           02 COLUMN 3 PIC 9 SOURCE IS WS-N.
        RD R-TWO PAGE LIMIT IS 20 LINES HEADING 1 FIRST DETAIL 3 LAST DETAIL 15.
        01 DET-TWO TYPE DE LINE PLUS 1.
           02 COLUMN 1 PIC X(12) VALUE "A LONGER ROW".
        PROCEDURE DIVISION.
        MAIN.
            OPEN OUTPUT RPT.
            OPEN OUTPUT REC.
            STOP RUN.
        """;

    [Fact]
    public void RecordLessReportFile_RegistersThroughRegisterReport_WithItsOwnLineWidthAndOptionalFlag()
    {
        string cs = EmitCSharp(ReportAndRecordFiles);
        // RPT: the widest line of R-ONE is 3 columns; RPT2 is OPTIONAL and R-TWO's line is 12 columns wide.
        Assert.Matches(new Regex(@"CobolFile\.RegisterReport\(""PB1880REG::RPT"", ""pb1880reg\.rpt"", 3, false\b"), cs);
        Assert.Matches(new Regex(@"CobolFile\.RegisterReport\(""PB1880REG::RPT2"", ""pb1880reg2\.rpt"", 12, true\b"), cs);
    }

    [Fact]
    public void ReportFile_IsNeverRegisteredAsARecordFile_AndARecordFileNeverAsAReportFile()
    {
        string cs = EmitCSharp(ReportAndRecordFiles);
        Assert.DoesNotMatch(new Regex(@"CobolFile\.Register\(""PB1880REG::RPT2?"""), cs);   // a record-file registration would carry a record length the report does not have
        Assert.Matches(new Regex(@"CobolFile\.Register\(""PB1880REG::REC"", ""pb1880reg\.dat"", 30, true, false\b"), cs);   // the control: the file WITH records registers as one
        Assert.DoesNotMatch(new Regex(@"CobolFile\.RegisterReport\(""PB1880REG::REC"""), cs);
        Assert.Equal(2, Regex.Matches(cs, @"CobolFile\.RegisterReport\(").Count);   // exactly one registration per report file
    }
}
