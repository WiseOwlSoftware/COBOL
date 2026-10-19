// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO;
using CobolNet.Runtime.IO.Store;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE KEYED STORE ENGINE'S '30' AND '24' ON AN INDEXED FILE THROUGH A COMPILED PROGRAM (kb/Work PB3334, review finding
/// R1-4; the relative twin is the Conformance project's <c>RelativeEnginePermanentErrorWitnessTests</c>). The unit cases
/// (<see cref="IndexedEngineTests"/>, <see cref="FileBoundaryTests"/>) see only the connector's status; these run the
/// program, so the emitted transfer of control — the INVALID KEY, NOT INVALID KEY, AT END and NOT AT END phrases, the USE
/// procedure, the FILE STATUS item and whether the run unit continues — is checked for each status the engine adds to
/// INDEXED, in every combination of a USE procedure and a FILE STATUS clause, with EC-I-O checking off and on.
///
/// <para>Two files, each built through the registry exactly as the program declares it (an 8-character record, the
/// prime key in 1-4, an alternate key WITH DUPLICATES in 5-8). The TORN file's record leaves carry one flipped byte,
/// so a tree read meets a failed checksum (2.2.1.6 R3): §9.1.13.6 item 1's '30'. The BOUNDARY file's meta pages record
/// all but the few pages an OPEN I-O's fence needs of the format's 2^32 as used, and its FREE tree and LEDGER as empty
/// (docs/rearchitecture/DESIGN-runtime-library.md 2.2.1.9: the boundary of the physical file is the format's 2^32 pages
/// or the host's maximum size; the precondition is measured through the connector, <see cref="BoundaryFile"/>), so
/// every changing statement needs a page past the boundary: a WRITE or REWRITE answers §14.9.51.4 GR42 d)'s '24'
/// (§9.1.13.5 item 4, <i>"An
/// attempt is made to write outside the externally-defined boundaries of a physical relative or indexed file"</i>) and a
/// DELETE '30' (DOC-A.1-107: §14.9.10.4 gives DELETE no boundary condition), while a READ needs no page and succeeds.</para>
///
/// <para><b>Every expected value is derived from the rules, not observed.</b> '24' is the invalid key condition, so
/// §9.1.14's first list applies: with the INVALID KEY phrase, item 2, <i>"any applicable exception processing statements
/// are not executed, and control is transferred to the imperative-statement specified in the INVALID KEY phrase"</i>;
/// without it, item 3, the USE procedure runs, and item 4, with neither, <i>"control is transferred to the end of the
/// input-output statement"</i>. EC-I-O-INVALID-KEY is nonfatal, so the run unit continues in every combination, checking
/// on or off. The file is not affected (§9.1.14's first paragraph): a later READ finds the records as they were. '30' is
/// neither the invalid key nor the at end condition, so §9.1.14's second list and §14.9.30.4 GR13 ignore the phrases and
/// the USE procedure runs; §9.1.13.1's permanent error stays in effect until the CLOSE ('00', DOC-A.1-105). Whether the
/// run unit continues after a '30' is DOC-A.1-103's: with checking off it continues when a FILE STATUS clause or a USE
/// procedure covers the statement and terminates abnormally when neither does; with checking on and no USE procedure it
/// terminates even with a FILE STATUS clause (§14.6.13.1.3).</para>
/// </summary>
public sealed class IndexedEnginePermanentErrorWitnessTests : CobolNetTestBase
{
    private string NewFile(string tag) => Path.Combine(TempDir, $"{tag}.dat");

    /// <summary>The file as the program declares it: an 8-character record, the prime key in 1-4, an alternate key WITH
    /// DUPLICATES in 5-8, dynamic access.</summary>
    private static void Register(FileRegistry reg, string host)
    {
        reg.RegisterIndexed("IX", host, recordWidth: 8, optional: false, accessMode: (int)KeyedAccess.Dynamic,
            primeKey: [new(0, 4)], varyMin: -1, varyMax: -1);
        reg.AddAlternateKey("IX", [new(4, 4)], duplicates: true);
    }

    /// <summary><paramref name="records"/> written and CLOSEd through the registry.</summary>
    private static void Seed(string host, params string[] records)
    {
        var reg = new FileRegistry();
        Register(reg, host);
        reg.OpenStatic("IX", FileOpenMode.Output);
        Assert.Equal("00", reg.Status("IX"));
        foreach (var record in records)
            Assert.Equal("00", reg.WriteShared("IX", record, -1, FileRecordLock.None, FileRetryKind.None, 0, page: null));
        reg.Close("IX");
        Assert.Equal("00", reg.Status("IX"));
    }

    /// <summary>Every tree page that carries the record's bytes gets one flipped byte, so its checksum fails.</summary>
    private string TornFile(string tag)
    {
        string host = NewFile(tag);
        Seed(host, "0001AAAA", "0004DDDD");
        byte[] file = File.ReadAllBytes(host);
        int torn = 0;
        for (int page = (int)MetaPage.MetaPages; page < file.Length / PageHeader.PageSize; page++)
        {
            if (file.AsSpan(page * PageHeader.PageSize, PageHeader.PageSize).IndexOf("0004DDDD"u8) < 0) continue;
            file[page * PageHeader.PageSize + 100] ^= 0xFF;
            torn++;
        }
        Assert.True(torn > 0, "the record's leaf was found");
        File.WriteAllBytes(host, file);
        return host;
    }

    /// <summary>Every meta page of <paramref name="file"/> rewritten to record all but <paramref name="headroom"/> of the
    /// format's 2^32 pages as used, an empty FREE tree and an empty LEDGER: no page to reuse, the records' pages still
    /// reachable, the pages the FREE tree and the LEDGER held reached by nothing.</summary>
    private static byte[] AtTheBoundary(byte[] file, ulong headroom)
    {
        var forged = (byte[])file.Clone();
        for (uint p = 0; p < MetaPage.MetaPages; p++)
        {
            var page = forged.AsSpan((int)p * PageHeader.PageSize, PageHeader.PageSize);
            if (MetaPage.Decode(page, p) is not { } meta) continue;
            (meta with { PageCount = PagedFile.FormatPages - headroom, FreeRoot = PagePointer.None, LedgerRoot = PagePointer.None })
                .Encode(p).CopyTo(page);
        }
        return forged;
    }

    /// <summary>A file AT ITS BOUNDARY (2.2.1.9): the store's records 0001AAAA and 0002BBBB, with the fewest pages left
    /// below the format's 2^32 that let an <c>OPEN I-O</c>'s fence (its reservation is a bound, 2.2.1.6 R2) complete.
    /// The precondition is measured through the connector on a copy, never assumed: the OPEN I-O answers '00', and then
    /// a WRITE of a new record answers the boundary's '24' and a DELETE '30' — every changing statement needs a page past
    /// the boundary — while a READ answers '00'.</summary>
    private string BoundaryFile(string tag)
    {
        string host = NewFile(tag), probe = NewFile(tag + "-probe");
        Seed(host, "0001AAAA", "0002BBBB");
        byte[] file = File.ReadAllBytes(host);
        for (ulong headroom = 0; headroom <= 64; headroom++)
        {
            File.WriteAllBytes(probe, AtTheBoundary(file, headroom));
            var reg = new FileRegistry();
            Register(reg, probe);
            reg.OpenStatic("IX", FileOpenMode.IO);
            if (reg.Status("IX") != "00") continue;   // the fence's reservation does not fit yet
            Assert.Equal("00", reg.ReadKeyedShared("IX", -1, "0001    ", FileRecordLock.None, ignoringLock: false,
                FileRetryKind.None, 0, out _));
            Assert.Equal("24", reg.WriteShared("IX", "0003CCCC", -1, FileRecordLock.None, FileRetryKind.None, 0, page: null));
            Assert.Equal("30", reg.DeleteShared("IX", "0002    ", FileRetryKind.None, 0));
            reg.Close("IX");
            File.WriteAllBytes(host, AtTheBoundary(file, headroom));
            return host;
        }
        Assert.Fail("no headroom below 64 pages lets the OPEN I-O's fence complete");
        return host;
    }

    private static string Header(string id, string host, bool fileStatus, bool use, bool checking) => $"""
        {(checking ? ">>TURN EC-I-O CHECKING ON\n" : "")}IDENTIFICATION DIVISION.
        PROGRAM-ID. {id}.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT IX ASSIGN TO "{host}"
                ORGANIZATION INDEXED ACCESS DYNAMIC
                RECORD KEY IK
                ALTERNATE RECORD KEY AK WITH DUPLICATES{(fileStatus ? " FILE STATUS FS" : "")}.
        DATA DIVISION.
        FILE SECTION.
        FD IX.
        01 IXR.
           05 IK PIC X(4).
           05 AK PIC X(4).
        WORKING-STORAGE SECTION.
        01 FS PIC XX VALUE "--".
        01 BR PIC X(8).
        PROCEDURE DIVISION.
        {(use ? "DECLARATIVES.\nERR-S SECTION.\n    USE AFTER STANDARD ERROR PROCEDURE ON IX.\nERR-P.\n    DISPLAY \"USE \" FS.\nEND DECLARATIVES.\nMAIN-S SECTION.\n" : "")}MAIN-P.
            OPEN I-O IX.
            DISPLAY "OPEN " FS.
        """;

    private static string TornProgram(string id, string host, bool fileStatus, bool use, bool checking) =>
        Header(id, host, fileStatus, use, checking) + """

            MOVE "0004" TO IK.
            MOVE "NONE" TO BR.
            READ IX INVALID KEY MOVE "INVALID" TO BR
                NOT INVALID KEY MOVE "VALID" TO BR
            END-READ.
            DISPLAY "READ " FS " " BR.
            MOVE "NONE" TO BR.
            READ IX NEXT RECORD AT END MOVE "ATEND" TO BR
                NOT AT END MOVE "NOTATEND" TO BR
            END-READ.
            DISPLAY "NEXT " FS " " BR.
            MOVE "0009IIII" TO IXR.
            MOVE "NONE" TO BR.
            WRITE IXR INVALID KEY MOVE "INVALID" TO BR
                NOT INVALID KEY MOVE "VALID" TO BR
            END-WRITE.
            DISPLAY "WRITE " FS " " BR.
            CLOSE IX.
            DISPLAY "CLOSE " FS.
            STOP RUN.
        """;

    /// <summary>The boundary program: a READ that needs no page (NOT INVALID KEY taken); a WRITE with the INVALID KEY
    /// phrase and one without; a REWRITE with it; a READ of the record the WRITE did not release; a DELETE ('30'); the
    /// CLOSE; then the next OPEN reads the file as it was: READ NEXT onto the last record takes NOT AT END, and past it
    /// AT END (§14.9.30.4 GR24's at end condition, '10').</summary>
    private static string BoundaryProgram(string id, string host, bool fileStatus, bool use, bool checking) =>
        Header(id, host, fileStatus, use, checking) + """

            MOVE "0001" TO IK.
            MOVE "NONE" TO BR.
            READ IX INVALID KEY MOVE "INVALID" TO BR
                NOT INVALID KEY MOVE "VALID" TO BR
            END-READ.
            DISPLAY "READ " FS " " BR.
            MOVE "0003CCCC" TO IXR.
            MOVE "NONE" TO BR.
            WRITE IXR INVALID KEY MOVE "INVALID" TO BR
                NOT INVALID KEY MOVE "VALID" TO BR
            END-WRITE.
            DISPLAY "WRITE " FS " " BR.
            MOVE "0005EEEE" TO IXR.
            WRITE IXR.
            DISPLAY "WRITE2 " FS.
            MOVE "0001ZZZZ" TO IXR.
            MOVE "NONE" TO BR.
            REWRITE IXR INVALID KEY MOVE "INVALID" TO BR
                NOT INVALID KEY MOVE "VALID" TO BR
            END-REWRITE.
            DISPLAY "REWRITE " FS " " BR.
            MOVE "0003" TO IK.
            MOVE "NONE" TO BR.
            READ IX INVALID KEY MOVE "INVALID" TO BR
                NOT INVALID KEY MOVE "VALID" TO BR
            END-READ.
            DISPLAY "READ3 " FS " " BR.
            MOVE "0002" TO IK.
            MOVE "NONE" TO BR.
            DELETE IX RECORD INVALID KEY MOVE "INVALID" TO BR
                NOT INVALID KEY MOVE "VALID" TO BR
            END-DELETE.
            DISPLAY "DELETE " FS " " BR.
            CLOSE IX.
            DISPLAY "CLOSE " FS.
            OPEN INPUT IX.
            MOVE "0001" TO IK.
            READ IX KEY IS IK.
            DISPLAY "AFTER " IXR.
            MOVE "NONE" TO BR.
            READ IX NEXT RECORD AT END MOVE "ATEND" TO BR
                NOT AT END MOVE "NOTATEND" TO BR
            END-READ.
            DISPLAY "AFTER " IXR " " BR.
            MOVE "NONE" TO BR.
            READ IX NEXT RECORD AT END MOVE "ATEND" TO BR
                NOT AT END MOVE "NOTATEND" TO BR
            END-READ.
            DISPLAY "END " FS " " BR.
            CLOSE IX.
            STOP RUN.
        """;

    private (bool ok, string stdout, string detail) Torn(string id, bool fileStatus, bool use, bool checking = false) =>
        Trimmed(CompileAndRun(TornProgram(id, TornFile(id), fileStatus, use, checking), 2023));

    private (bool ok, string stdout, string detail) Boundary(string id, bool fileStatus, bool use, bool checking = false) =>
        Trimmed(CompileAndRun(BoundaryProgram(id, BoundaryFile(id), fileStatus, use, checking), 2023));

    /// <summary>The run with each DISPLAY line's trailing spaces (BR's PIC X(8) padding) removed.</summary>
    private static (bool ok, string stdout, string detail) Trimmed((bool ok, string stdout, string detail) run) =>
        (run.ok, string.Join("\r\n", run.stdout.Split("\r\n").Select(line => line.TrimEnd())), run.detail);

    private static string Lines(params string[] lines) => string.Join("\r\n", lines);

    // ── '30': a torn tree page ─────────────────────────────────────────────────────────────────────────────────

    [Fact]   // A USE procedure and a FILE STATUS clause: each statement runs the procedure, no phrase runs, and the run continues.
    public void TornPage_UseAndFileStatus_RunsTheUseProcedureAtEveryStatement_AndContinues()
    {
        var (ok, stdout, detail) = Torn("IXTORNA", fileStatus: true, use: true);
        Assert.True(ok, detail);
        Assert.Equal(Lines("OPEN 00", "USE 30", "READ 30 NONE", "USE 30", "NEXT 30 NONE", "USE 30", "WRITE 30 NONE", "CLOSE 00"), stdout);
    }

    [Fact]   // A USE procedure alone (FS is then an ordinary item the I-O never stores into): the run continues.
    public void TornPage_UseWithoutFileStatus_RunsTheUseProcedure_AndContinues()
    {
        var (ok, stdout, detail) = Torn("IXTORNB", fileStatus: false, use: true);
        Assert.True(ok, detail);
        Assert.Equal(Lines("OPEN --", "USE --", "READ -- NONE", "USE --", "NEXT -- NONE", "USE --", "WRITE -- NONE", "CLOSE --"), stdout);
    }

    [Fact]   // A FILE STATUS clause alone: no phrase runs, the status is the program's to see, and the run continues.
    public void TornPage_FileStatusWithoutUse_StoresThe30_AndContinues()
    {
        var (ok, stdout, detail) = Torn("IXTORNC", fileStatus: true, use: false);
        Assert.True(ok, detail);
        Assert.Equal(Lines("OPEN 00", "READ 30 NONE", "NEXT 30 NONE", "WRITE 30 NONE", "CLOSE 00"), stdout);
    }

    [Fact]   // Neither: the INVALID KEY phrase covers only the invalid key condition, so the READ's '30' ends the run unit.
    public void TornPage_NeitherUseNorFileStatus_TerminatesAtTheRead()
    {
        var (ok, stdout, detail) = Torn("IXTORND", fileStatus: false, use: false);
        Assert.False(ok, $"expected abnormal termination; ran clean with stdout:\n{stdout}");
        Assert.Contains("EC-I-O-PERMANENT-ERROR", detail);
        Assert.Contains("(READ)", detail);
        Assert.Equal("OPEN --", stdout);
    }

    [Fact]   // Checking enabled and no USE procedure: §14.6.13.1.3 applies after the I-O rules; the FILE STATUS clause does not continue it.
    public void TornPage_CheckingEnabled_FileStatusWithoutUse_TerminatesAtTheRead()
    {
        var (ok, stdout, detail) = Torn("IXTORNE", fileStatus: true, use: false, checking: true);
        Assert.False(ok, $"expected abnormal termination; ran clean with stdout:\n{stdout}");
        Assert.Contains("EC-I-O-PERMANENT-ERROR", detail);
        Assert.Equal("OPEN 00", stdout);
    }

    // ── '24': the boundary of the physical file (and the DELETE's '30' there) ────────────────────────────────────

    [Fact]   // §9.1.14 item 2: the INVALID KEY phrase runs and the USE procedure does not; item 3: without the phrase the USE procedure runs.
    public void Boundary_UseAndFileStatus_InvalidKeyTakenOrUseRun_TheDeleteIs30_AndTheFileIsUnchanged()
    {
        var (ok, stdout, detail) = Boundary("IXBNDA", fileStatus: true, use: true);
        Assert.True(ok, detail);
        Assert.Equal(Lines("OPEN 00", "READ 00 VALID", "WRITE 24 INVALID", "USE 24", "WRITE2 24", "REWRITE 24 INVALID",
            "READ3 23 INVALID", "USE 30", "DELETE 30 NONE", "CLOSE 00",
            "AFTER 0001AAAA", "AFTER 0002BBBB NOTATEND", "END 10 ATEND"), stdout);
    }

    [Fact]   // A USE procedure alone: the same transfers of control, FS untouched.
    public void Boundary_UseWithoutFileStatus_InvalidKeyTakenOrUseRun_AndContinues()
    {
        var (ok, stdout, detail) = Boundary("IXBNDB", fileStatus: false, use: true);
        Assert.True(ok, detail);
        Assert.Equal(Lines("OPEN --", "READ -- VALID", "WRITE -- INVALID", "USE --", "WRITE2 --", "REWRITE -- INVALID",
            "READ3 -- INVALID", "USE --", "DELETE -- NONE", "CLOSE --",
            "AFTER 0001AAAA", "AFTER 0002BBBB NOTATEND", "END -- ATEND"), stdout);
    }

    [Fact]   // A FILE STATUS clause alone: §9.1.14 item 4 for the WRITE without the phrase; the DELETE's '30' continues (DOC-A.1-103).
    public void Boundary_FileStatusWithoutUse_StoresThe24_AndContinues()
    {
        var (ok, stdout, detail) = Boundary("IXBNDC", fileStatus: true, use: false);
        Assert.True(ok, detail);
        Assert.Equal(Lines("OPEN 00", "READ 00 VALID", "WRITE 24 INVALID", "WRITE2 24", "REWRITE 24 INVALID",
            "READ3 23 INVALID", "DELETE 30 NONE", "CLOSE 00",
            "AFTER 0001AAAA", "AFTER 0002BBBB NOTATEND", "END 10 ATEND"), stdout);
    }

    [Fact]   // Neither: every '24' continues (item 4; EC-I-O-INVALID-KEY is nonfatal); the DELETE's '30' ends the run unit.
    public void Boundary_NeitherUseNorFileStatus_ContinuesPastThe24s_AndTerminatesAtTheDelete()
    {
        var (ok, stdout, detail) = Boundary("IXBNDD", fileStatus: false, use: false);
        Assert.False(ok, $"expected abnormal termination; ran clean with stdout:\n{stdout}");
        Assert.Contains("EC-I-O-PERMANENT-ERROR", detail);
        Assert.Contains("(DELETE)", detail);
        Assert.Equal(Lines("OPEN --", "READ -- VALID", "WRITE -- INVALID", "WRITE2 --", "REWRITE -- INVALID",
            "READ3 -- INVALID"), stdout);
    }

    [Fact]   // Checking enabled and no USE procedure: the nonfatal '24's continue; the fatal '30' terminates despite FILE STATUS.
    public void Boundary_CheckingEnabled_FileStatusWithoutUse_ContinuesPastThe24s_AndTerminatesAtTheDelete()
    {
        var (ok, stdout, detail) = Boundary("IXBNDE", fileStatus: true, use: false, checking: true);
        Assert.False(ok, $"expected abnormal termination; ran clean with stdout:\n{stdout}");
        Assert.Contains("EC-I-O-PERMANENT-ERROR", detail);
        Assert.Equal(Lines("OPEN 00", "READ 00 VALID", "WRITE 24 INVALID", "WRITE2 24", "REWRITE 24 INVALID",
            "READ3 23 INVALID"), stdout);
    }
}
