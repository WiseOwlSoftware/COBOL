// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ A SORT AND A MERGE MANY TIMES THE SORT MEMORY LIMIT, END TO END (kb/Work PB3339; docs/CONFORMANCE.md §3 D-SWK).
/// The program releases 30,000 records from an INPUT PROCEDURE, SORTs them GIVING two files, and MERGEs the two back
/// through an OUTPUT PROCEDURE that checks the order it receives; it runs as its own process under the floor limit
/// (64 KiB — the records are about a hundred times that) with its own work directory, and again under the default
/// limit, where everything fits in memory.
/// <para>What the program reports is derived from the standard, not from a run: the SORT returns every record
/// (30,000) with no key below its predecessor and, on equal keys, release order (§14.9.40.4 GR3 b) — "The order in
/// which these records are released by an input procedure"); each GIVING file receives the whole result (GR15); the
/// MERGE returns both files (60,000) in key order and, on equal keys, all of OUTA's records before OUTB's (§14.9.24.4
/// GR4 a) "Follows the order of the associated input files as specified in the MERGE statement", b) "all records
/// associated with one input file are returned prior to the return of records from another input file") — so within
/// one key the release numbers rise, fall back ONCE where OUTB's part begins, and rise again: one fall per key group.
/// The two runs print the same lines, and the work directory is empty afterwards: no run file outlives its statement.</para>
/// </summary>
public sealed class SortWorkSpillConformanceTests
{
    /// <summary>The run unit's sort memory limit and work directory (<c>SortMemory.MemoryVariable</c> /
    /// <c>SortMemory.DirectoryVariable</c>, registered in <c>RuntimeConfig</c>; the type is internal to the runtime).</summary>
    private const string MemoryVariable = "COBOLNET_SORT_MEMORY";
    private const string DirectoryVariable = "COBOLNET_SORT_TMPDIR";

    private const string Program = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. PB3339SP.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT SW ASSIGN TO "SW".
            SELECT MW ASSIGN TO "MW".
            SELECT OUTA ASSIGN TO "PB3339A.DAT".
            SELECT OUTB ASSIGN TO "PB3339B.DAT".
        DATA DIVISION.
        FILE SECTION.
        SD SW.
        01 SR.
           05 SR-KEY  PIC X(4).
           05 SR-SEQ  PIC 9(7).
           05 SR-FILL PIC X(29).
        SD MW.
        01 MR.
           05 MR-KEY  PIC X(4).
           05 MR-SEQ  PIC 9(7).
           05 MR-FILL PIC X(29).
        FD OUTA.
        01 OA PIC X(40).
        FD OUTB.
        01 OB PIC X(40).
        WORKING-STORAGE SECTION.
        01 I        PIC 9(7).
        01 P        PIC 9.
        01 J        PIC 9.
        01 R        PIC 9(9) VALUE 12345.
        01 LETTERS  PIC X(6) VALUE "ABCDEF".
        01 N        PIC 9(7) VALUE 0.
        01 BAD      PIC 9(7) VALUE 0.
        01 GROUPS   PIC 9(7) VALUE 0.
        01 FALLS    PIC 9(7) VALUE 0.
        01 PREV-KEY PIC X(4).
        01 PREV-SEQ PIC 9(7).
        PROCEDURE DIVISION.
        MAIN-LINE.
            SORT SW ON ASCENDING KEY SR-KEY WITH DUPLICATES IN ORDER
                INPUT PROCEDURE RELEASE-RECORDS
                GIVING OUTA OUTB
            MOVE LOW-VALUE TO PREV-KEY
            MOVE 0 TO PREV-SEQ
            MERGE MW ON ASCENDING KEY MR-KEY
                USING OUTA OUTB
                OUTPUT PROCEDURE CHECK-MERGED
            DISPLAY "MERGED " N " OUT-OF-ORDER " BAD
            IF FALLS = GROUPS
                DISPLAY "ONE FALL PER KEY GROUP"
            ELSE
                DISPLAY "FALLS " FALLS " GROUPS " GROUPS
            END-IF
            STOP RUN.
        RELEASE-RECORDS.
            PERFORM VARYING I FROM 1 BY 1 UNTIL I > 30000
                PERFORM VARYING P FROM 1 BY 1 UNTIL P > 4
                    COMPUTE R = FUNCTION MOD(R * 7919 + 13, 1000003)
                    COMPUTE J = FUNCTION MOD(R, 6) + 1
                    MOVE LETTERS(J:1) TO SR-KEY(P:1)
                END-PERFORM
                MOVE I TO SR-SEQ
                MOVE ALL "X" TO SR-FILL
                RELEASE SR
            END-PERFORM.
        CHECK-MERGED.
            PERFORM UNTIL 1 = 0
                RETURN MW AT END EXIT PERFORM END-RETURN
                ADD 1 TO N
                IF MR-KEY < PREV-KEY
                    ADD 1 TO BAD
                END-IF
                IF MR-KEY = PREV-KEY
                    IF MR-SEQ NOT > PREV-SEQ
                        ADD 1 TO FALLS
                    END-IF
                ELSE
                    ADD 1 TO GROUPS
                END-IF
                MOVE MR-KEY TO PREV-KEY
                MOVE MR-SEQ TO PREV-SEQ
            END-PERFORM.
        END PROGRAM PB3339SP.
        """;

    /// <summary>The program's lines, derived from §14.9.40.4 GR3 / GR15 and §14.9.24.4 GR4 (see the class summary).</summary>
    private const string Expected = "MERGED 0060000 OUT-OF-ORDER 0000000\nONE FALL PER KEY GROUP";

    private static string Compile(string dir)
    {
        string src = CompiledProgramCache.StageSource(Path.Combine(dir, "PB3339SP.cob"), Program);
        string dll = Path.Combine(dir, "PB3339SP.dll");
        var compiled = CompiledProgramCache.Compile(new CompilerDriver.Options(src, dll, DialectLevel: 2023,
            SourceFormat: InitialReferenceFormat.Auto));
        Assert.True(compiled.Success, string.Join("\n", compiled.Errors));
        return dll;
    }

    [Fact]
    public void ASortAndAMergeAHundredTimesTheLimit_OrderAsTheyDoInMemory_AndLeaveNoFile()
    {
        string dir = CutRunner.NewTempDir("pb3339");
        try
        {
            string dll = Compile(dir);
            string work = Directory.CreateDirectory(Path.Combine(dir, "sortwork")).FullName;
            var (spilledOk, spilled, spilledDetail) = CutRunner.Run(dll, dir, null, new Dictionary<string, string?>
            {
                [MemoryVariable] = "64K",
                [DirectoryVariable] = work,
            });
            Assert.True(spilledOk, spilledDetail);
            Assert.Equal(Expected, spilled);
            Assert.Empty(Directory.EnumerateFileSystemEntries(work));   // D-SWK (b): no run file outlives its statement

            var (fitOk, fit, fitDetail) = CutRunner.Run(dll, dir, null, new Dictionary<string, string?>
            {
                [MemoryVariable] = null,
                [DirectoryVariable] = work,
            });
            Assert.True(fitOk, fitDetail);
            Assert.Equal(spilled, fit);   // the order does not depend on the limit (D-SWK (c))
        }
        finally { CutRunner.TryDelete(dir); }
    }

    /// <summary>D-SWK (d): a work directory the statement cannot write is a loud stop that names it and the variable —
    /// the run unit ends abnormally rather than lose a record.</summary>
    [Fact]
    public void AMissingWorkDirectory_StopsTheRunUnit_NamingIt()
    {
        string dir = CutRunner.NewTempDir("pb3339n");
        try
        {
            string dll = Compile(dir);
            string missing = Path.Combine(dir, "no-such-directory");
            var (code, _, detail) = CutRunner.RunExit(dll, dir, null, new Dictionary<string, string?>
            {
                [MemoryVariable] = "64K",
                [DirectoryVariable] = missing,
            });
            Assert.NotEqual(0, code);
            Assert.Contains(DirectoryVariable, detail, StringComparison.Ordinal);
            Assert.Contains("no-such-directory", detail, StringComparison.Ordinal);
        }
        finally { CutRunner.TryDelete(dir); }
    }
}
