// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ THE KEYED STORE ENGINE'S '30' THROUGH A COMPILED PROGRAM (kb/Work PB3333, review finding R1-3;
/// docs/rearchitecture/DESIGN-runtime-library.md section 2.2.1.12's tree row "a record statement whose tree read meets a
/// torn or stale page"). The engine answers a record statement whose tree read meets a TORN page with §9.1.13.6 item 1's
/// '30' — <i>"A permanent error exists and no further information is available concerning the input-output operation"</i>
/// — and the unit cases (<c>RelativeEngineTests</c>) see only the status. These run the program: a relative file is
/// built and its record's leaf torn, then a program reads, reads next and writes through it, and each combination of a
/// USE procedure and a FILE STATUS clause is checked against the rules for what happens next.
///
/// <para><b>Every expected value is derived from the rules, not observed.</b> '30' is not the invalid key condition, so
/// §9.1.14 applies its second list: <i>"If the invalid key condition does not exist after the execution of the
/// input-output operation specified by an input-output statement, the INVALID KEY phrase is ignored, if specified"</i>,
/// and <i>"If the I-O status indicates an unsuccessful completion that is not an invalid key condition, control is
/// transferred according to the rules of any applicable exception processing statements"</i> — the USE procedure, and
/// then the end of the statement: neither INVALID KEY nor NOT INVALID KEY runs. '30' is not the at end condition either,
/// so §14.9.30.4 GR13 ignores AT END likewise (<i>"If neither an at end nor an invalid key condition occurs during the
/// execution of a READ statement, the AT END phrase or the INVALID KEY phrase is ignored"</i>). §9.1.13.1's permanent
/// error stays in effect for the connector's later statements ('30' replayed, the USE procedure run again), and the
/// CLOSE ends it ('00', DOC-A.1-105). Whether the run unit continues is §9.1.13.1's choice (<i>"The implementor may
/// either continue or terminate the execution of the run unit"</i>), which docs/CONFORMANCE.md DOC-A.1-103 makes: with
/// EC-I-O checking not enabled it continues when a FILE STATUS clause or a USE procedure covers the statement and
/// terminates abnormally when neither does (the INVALID KEY phrase covers only the invalid key condition); with checking
/// enabled and no USE procedure it terminates even with a FILE STATUS clause.</para>
/// </summary>
public sealed class RelativeEnginePermanentErrorWitnessTests
{
    private static readonly CobolNetCompiler CobolNet = new(2023);

    /// <summary>A relative file of one record (RRN 4, <c>0004DDDD</c>) built through the registry exactly as the
    /// program declares it, whose record leaf is then torn: every page that carries the record's bytes has one byte
    /// flipped, so its checksum fails at the next read (2.2.1.6 R3).</summary>
    private static string TornFile()
    {
        string host = Path.Combine(Path.GetTempPath(), $"pb3333-torn-{Guid.NewGuid():N}.dat");
        FileHarness.Seed(host, FileOrg.Relative, [(4, "DDDD")]);
        FileHarness.TearPagesCarrying(host, "0004DDDD"u8);
        return host;
    }

    private static string Program(string id, string host, bool fileStatus, bool use, bool checking) => $"""
        {(checking ? ">>TURN EC-I-O CHECKING ON\n" : "")}IDENTIFICATION DIVISION.
        PROGRAM-ID. {id}.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT RV ASSIGN TO "{host}"
                ORGANIZATION RELATIVE ACCESS DYNAMIC
                RELATIVE KEY RK{(fileStatus ? " FILE STATUS FS" : "")}.
        DATA DIVISION.
        FILE SECTION.
        FD RV.
        01 RVR PIC X(8).
        WORKING-STORAGE SECTION.
        01 FS PIC XX VALUE "--".
        01 RK PIC 9(4).
        01 BR PIC X(8).
        PROCEDURE DIVISION.
        {(use ? "DECLARATIVES.\nERR-S SECTION.\n    USE AFTER STANDARD ERROR PROCEDURE ON RV.\nERR-P.\n    DISPLAY \"USE \" FS.\nEND DECLARATIVES.\nMAIN-S SECTION.\n" : "")}MAIN-P.
            OPEN I-O RV.
            DISPLAY "OPEN " FS.
            MOVE 4 TO RK.
            MOVE "NONE" TO BR.
            READ RV INVALID KEY MOVE "INVALID" TO BR
                NOT INVALID KEY MOVE "VALID" TO BR
            END-READ.
            DISPLAY "READ " FS " " BR.
            MOVE "NONE" TO BR.
            READ RV NEXT RECORD AT END MOVE "ATEND" TO BR
                NOT AT END MOVE "NOTATEND" TO BR
            END-READ.
            DISPLAY "NEXT " FS " " BR.
            MOVE 9 TO RK.
            MOVE "0009IIII" TO RVR.
            MOVE "NONE" TO BR.
            WRITE RVR INVALID KEY MOVE "INVALID" TO BR
                NOT INVALID KEY MOVE "VALID" TO BR
            END-WRITE.
            DISPLAY "WRITE " FS " " BR.
            CLOSE RV.
            DISPLAY "CLOSE " FS.
            STOP RUN.
        """;

    private static (bool ok, string stdout, string detail) Run(string id, bool fileStatus, bool use, bool checking = false)
    {
        string host = TornFile();
        try { return CobolNet.CompileAndRun(Program(id, host, fileStatus, use, checking)); }
        finally { File.Delete(host); }
    }

    [Fact]   // A USE procedure and a FILE STATUS clause: each statement runs the procedure, no phrase runs, and the run continues.
    public void TornPage_UseAndFileStatus_RunsTheUseProcedureAtEveryStatement_AndContinues()
    {
        var (ok, stdout, detail) = Run("RLTORNA", fileStatus: true, use: true);
        Assert.True(ok, detail);
        Assert.Equal(
            "OPEN 00\nUSE 30\nREAD 30 NONE\nUSE 30\nNEXT 30 NONE\nUSE 30\nWRITE 30 NONE\nCLOSE 00", stdout);
    }

    [Fact]   // A USE procedure alone (FS is then an ordinary item the I-O never stores into): the run continues.
    public void TornPage_UseWithoutFileStatus_RunsTheUseProcedure_AndContinues()
    {
        var (ok, stdout, detail) = Run("RLTORNB", fileStatus: false, use: true);
        Assert.True(ok, detail);
        Assert.Equal(
            "OPEN --\nUSE --\nREAD -- NONE\nUSE --\nNEXT -- NONE\nUSE --\nWRITE -- NONE\nCLOSE --", stdout);
    }

    [Fact]   // A FILE STATUS clause alone: no phrase runs, the status is the program's to see, and the run continues.
    public void TornPage_FileStatusWithoutUse_StoresThe30_AndContinues()
    {
        var (ok, stdout, detail) = Run("RLTORNC", fileStatus: true, use: false);
        Assert.True(ok, detail);
        Assert.Equal("OPEN 00\nREAD 30 NONE\nNEXT 30 NONE\nWRITE 30 NONE\nCLOSE 00", stdout);
    }

    [Fact]   // Neither: the INVALID KEY phrase covers only the invalid key condition, so the READ's '30' ends the run unit.
    public void TornPage_NeitherUseNorFileStatus_TerminatesAtTheRead()
    {
        var (ok, stdout, detail) = Run("RLTORND", fileStatus: false, use: false);
        Assert.False(ok, $"expected abnormal termination; ran clean with stdout:\n{stdout}");
        Assert.Contains("EC-I-O-PERMANENT-ERROR", detail);
        Assert.Contains("(READ)", detail);
        Assert.Equal("OPEN --", stdout);
    }

    [Fact]   // Checking enabled and no USE procedure: §14.6.13.1.3 applies after the I-O rules, and the FILE STATUS clause does not continue it.
    public void TornPage_CheckingEnabled_FileStatusWithoutUse_TerminatesAtTheRead()
    {
        var (ok, stdout, detail) = Run("RLTORNE", fileStatus: true, use: false, checking: true);
        Assert.False(ok, $"expected abnormal termination; ran clean with stdout:\n{stdout}");
        Assert.Contains("EC-I-O-PERMANENT-ERROR", detail);
        Assert.Equal("OPEN 00", stdout);
    }
}
