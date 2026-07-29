// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// Spec-pinned sequential file-I/O goldens (CONFORMANCE-FIX-QUEUE CA15/CA16). Each stages a filesystem precondition
/// (an over-length input line; an absent OPTIONAL file) in a temp run directory, compiles + runs the program there at
/// --std 2023, and asserts the spec-derived stdout — behaviours the self-contained write-then-read differential
/// goldens cannot express. Expected values are DERIVED from the spec, not the legacy oracle.
/// </summary>
public sealed class SequentialFileIoSpecTests
{
    private static string Run(string source, (string Name, string Content)? stage = null)
    {
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_Seqio_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            if (stage is { } s) File.WriteAllText(Path.Combine(dir, s.Name), s.Content);
            string src = Path.Combine(dir, "prog.cob");
            File.WriteAllText(src, source);
            string dll = Path.Combine(dir, "prog.dll");
            var r = CobolNet.CompilerDriver.Compile(new CobolNet.CompilerDriver.Options(src, dll, DialectLevel: 2023));
            Assert.True(r.Success, "must compile strict: " + string.Join("\n", r.Errors));
            var (ran, stdout, detail) = CutRunner.Run(dll, dir);
            Assert.True(ran, "must run: " + detail);
            return CutRunner.Normalize(stdout);
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ } }
    }

    /// <summary>CA15 — §14.9.30.4 GR15 + NOTE 3 / §9.1.13.2 item 5: an over-length LINE-SEQUENTIAL record is truncated
    /// on the right to the record width, the READ is SUCCESSFUL with I-O status '06', the file position indicator
    /// references the next unread character in the record, and the following READ continues the remainder ('00'). The
    /// buggy path reported '00' on the truncated read and silently discarded the tail (the next READ skipped to the
    /// following physical line).</summary>
    [Fact]
    public void LineSequential_OverLengthRecord_TruncatesWith06_AndContinuesRemainder()
    {
        const string prog = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. LSLONG.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F ASSIGN "lslong.txt" ORGANIZATION LINE SEQUENTIAL FILE STATUS FS.
            DATA DIVISION.
            FILE SECTION.
            FD F.
            01 REC PIC X(5).
            WORKING-STORAGE SECTION.
            01 FS PIC XX.
            PROCEDURE DIVISION.
            MAIN.
                OPEN INPUT F.
                READ F.
                DISPLAY "1:" REC " FS=" FS.
                READ F.
                DISPLAY "2:" REC " FS=" FS.
                READ F AT END DISPLAY "E FS=" FS END-READ.
                CLOSE F.
                STOP RUN.
            """;
        // Input: one physical line 'ABCDEFGH' (8 chars) + newline. READ1 truncates 8->5 = 'ABCDE' with '06'; READ2
        // continues the remainder 'FGH' (3<=5, space-padded in the X(5) record) with '00'; READ3 hits EOF -> '10'.
        Assert.Equal("1:ABCDE FS=06\n2:FGH   FS=00\nE FS=10", Run(prog, ("lslong.txt", "ABCDEFGH\n")));
    }

    /// <summary>CA16 — §14.9.27 GR17 + §14.9.30.4 GR21 rule e / GR24: an ABSENT OPTIONAL file opened I-O is CREATED
    /// (as if OPEN OUTPUT + CLOSE; OPEN returns '05'), the FPI is 1 (GR14), and the first READ on the now-empty file
    /// finds no record → AtEnd '10'. The buggy path created only a writer, so the first READ returned '47'
    /// (not-open-for-input) — which flips the AT END control flow (status[0] != '1'), running NOT AT END instead.</summary>
    [Fact]
    public void OptionalSequential_OpenIoAbsent_Creates05_ThenFirstReadIsAtEnd10()
    {
        const string prog = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. OPTIO.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT OPTIONAL F ASSIGN "optabsent.dat" ORGANIZATION SEQUENTIAL FILE STATUS FS.
            DATA DIVISION.
            FILE SECTION.
            FD F.
            01 REC PIC X(10).
            WORKING-STORAGE SECTION.
            01 FS PIC XX.
            PROCEDURE DIVISION.
            MAIN.
                OPEN I-O F.
                DISPLAY "OPEN FS=" FS.
                READ F AT END DISPLAY "ATEND FS=" FS
                    NOT AT END DISPLAY "GOTREC FS=" FS END-READ.
                CLOSE F.
                STOP RUN.
            """;
        Assert.Equal("OPEN FS=05\nATEND FS=10", Run(prog));
    }

    /// <summary>CA18 — §14.9.35.4 GR17 (line-sequential REWRITE in place): (equal) a same-length record replaces the
    /// line, '00'; (b) a record LONGER than the one being replaced ⇒ '44', the line unchanged; (c) a SHORTER record
    /// is space-padded to the replaced length and written, '00'. The buggy path returned '30' for every
    /// line-sequential REWRITE (the seekable in-place branch was guarded by !_lineSequential).</summary>
    [Fact]
    public void LineSequential_Rewrite_Gr17_Equal00_Longer44_Shorter00()
    {
        const string prog = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. LSREW.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F ASSIGN "lsrew.txt" ORGANIZATION LINE SEQUENTIAL FILE STATUS FS.
            DATA DIVISION.
            FILE SECTION.
            FD F.
            01 REC PIC X(5).
            WORKING-STORAGE SECTION.
            01 FS PIC XX.
            PROCEDURE DIVISION.
            MAIN.
                OPEN I-O F.
                READ F. MOVE "WORLD" TO REC. REWRITE REC. DISPLAY "R1=" FS.
                READ F. MOVE "XYZ" TO REC. REWRITE REC. DISPLAY "R2=" FS.
                READ F. MOVE "HI" TO REC. REWRITE REC. DISPLAY "R3=" FS.
                CLOSE F.
                OPEN INPUT F.
                READ F. DISPLAY "L1=[" REC "]".
                READ F. DISPLAY "L2=[" REC "]".
                READ F. DISPLAY "L3=[" REC "]".
                CLOSE F.
                STOP RUN.
            """;
        // Input lines HELLO(5), AB(2), CDEFG(5). WORLD==HELLO ⇒ '00', line becomes WORLD; XYZ(3) > AB(2) ⇒ '44'
        // GR17b, AB unchanged; HI(2) < CDEFG(5) ⇒ '00' GR17c, space-padded to "HI   " within the byte span.
        Assert.Equal("R1=00\nR2=44\nR3=00\nL1=[WORLD]\nL2=[AB   ]\nL3=[HI   ]",
            Run(prog, ("lsrew.txt", "HELLO\nAB\nCDEFG\n")));
    }

    /// <summary>CA18 — §14.9.35.4 GR17a / §9.1.13.7 item 4d: a REWRITE of a record whose preceding READ transferred
    /// only PART of it (an over-length line-sequential read that returned '06') is '44'.</summary>
    [Fact]
    public void LineSequential_RewriteAfterOverLengthRead_IsPartial44()
    {
        const string prog = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. LSPART.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F ASSIGN "lspart.txt" ORGANIZATION LINE SEQUENTIAL FILE STATUS FS.
            DATA DIVISION.
            FILE SECTION.
            FD F.
            01 REC PIC X(5).
            WORKING-STORAGE SECTION.
            01 FS PIC XX.
            PROCEDURE DIVISION.
            MAIN.
                OPEN I-O F.
                READ F. DISPLAY "RD=" FS.
                MOVE "WORLD" TO REC. REWRITE REC. DISPLAY "RW=" FS.
                CLOSE F.
                STOP RUN.
            """;
        // 'ABCDEFGH' (8) into an X(5) record ⇒ READ truncates to 'ABCDE' with '06' (a PARTIAL transfer); a REWRITE
        // of that partially-read record is GR17a ⇒ '44'.
        Assert.Equal("RD=06\nRW=44", Run(prog, ("lspart.txt", "ABCDEFGH\n")));
    }
}
