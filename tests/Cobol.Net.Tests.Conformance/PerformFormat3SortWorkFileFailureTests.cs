// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;
using CobolNet.Tests.Shared;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// kb/Work PB1154 / PB3353 (branch review round 2, R2-2) — a failure of the SORT's own WORK FILE (the sort-merge file
/// <c>SF</c> of the SD entry, whose storage is the implementor's: docs/CONFORMANCE.md §3 D-SWK) while the SORT sits in
/// a WHEN phrase of an exception-checking PERFORM, beside <c>PerformFormat3IoConflictAndSortTests</c> (the USING-file
/// failures: status 35, a file open, a MERGE out of sequence).
/// <para>What the standard says and what it leaves to the implementor. A SORT may sit in a WHEN phrase (§14.9.40.3
/// SR3: "A SORT statement shall not appear in imperative-statement-1 of an exception-checking PERFORM statement, in
/// an input or output procedure, or in a declarative procedure" — a WHEN phrase is none of those). The standard raises no exception
/// condition for the sort-merge file's own storage, so no WHEN, WHEN OTHER, FINALLY or USE declarative can answer
/// it; D-SWK (d) fixes the implementor-defined answer: a temporary run file that cannot be created (the directory
/// missing or not writable, the volume full) STOPS THE RUN UNIT with a message naming the directory, the host's
/// reason and the variable that sets it (<c>COBOLNET_SORT_TMPDIR</c>), "no record is dropped silently". This class pins
/// that the Format-3 PERFORM's frame does not turn the stop into anything else: no later WHEN phrase, no WHEN OTHER,
/// no FINALLY and not the statements after END-PERFORM run, the exit status is not zero, and the GIVING file was
/// never created: the run file fails while the input procedure releases records, and §14.9.40.4 GR15 a) initiates the
/// GIVING file only after the execution of any input procedure, so a stop there leaves no GIVING file behind, empty or
/// half written. The same program with a usable work directory sorts, which is what makes the stop attributable to
/// the work file and not to the program.</para>
/// </summary>
public sealed class PerformFormat3SortWorkFileFailureTests
{
    private static readonly CobolNetCompiler CobolNet = new(2023);

    /// <summary>The run unit's sort memory limit and work directory (<c>SortMemory.MemoryVariable</c> /
    /// <c>SortMemory.DirectoryVariable</c>, registered in <c>RuntimeConfig</c>; the type is internal to the runtime).</summary>
    private const string MemoryVariable = "COBOLNET_SORT_MEMORY";
    private const string DirectoryVariable = "COBOLNET_SORT_TMPDIR";

    /// <summary>Five thousand 200-byte records (about a megabyte, sixteen times the 64 KiB floor limit), released in
    /// DESCENDING key order by an INPUT PROCEDURE, SORTed ascending GIVING G — from inside the WHEN phrase the
    /// OPEN INPUT of the ABSENT file H earns (status 35, EC-I-O-PERMANENT-ERROR, answered by the WHEN).</summary>
    private static string Program(string dir) => $"""
        >>TURN EC-ALL CHECKING ON
        IDENTIFICATION DIVISION.
        PROGRAM-ID. F3SWF.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT G ASSIGN "{GivingPath(dir)}" ORGANIZATION LINE SEQUENTIAL FILE STATUS GS.
            SELECT H ASSIGN "{Path.Combine(dir, "F3SWF-H.DAT")}" ORGANIZATION LINE SEQUENTIAL FILE STATUS HS.
            SELECT SF ASSIGN "F3SWF-S".
        DATA DIVISION.
        FILE SECTION.
        FD G.
        01 GR.
           05 GK PIC 9(8).
           05 GF PIC X(192).
        FD H.
        01 HR PIC X(4).
        SD SF.
        01 SR.
           05 SK PIC 9(8).
           05 SX PIC X(192).
        WORKING-STORAGE SECTION.
        01 GS PIC XX VALUE "??".
        01 HS PIC XX VALUE "??".
        01 I  PIC 9(8).
        01 FIRST-KEY PIC 9(8).
        01 LAST-KEY  PIC 9(8).
        PROCEDURE DIVISION.
        MAIN-PARA.
            PERFORM
                OPEN INPUT H
                DISPLAY "AFTER-OPEN"
            WHEN EC-I-O-PERMANENT-ERROR
                DISPLAY "IN-WHEN " HS
                SORT SF ASCENDING KEY SK INPUT PROCEDURE RELEASE-RECORDS GIVING G
                DISPLAY "SORTED " GS
                RESUME NEXT STATEMENT
            WHEN OTHER
                DISPLAY "OTHER"
            FINALLY
                DISPLAY "FINALLY"
            END-PERFORM
            OPEN INPUT G
            READ G
            MOVE GK TO FIRST-KEY
            PERFORM UNTIL GS NOT = "00"
                MOVE GK TO LAST-KEY
                READ G
            END-PERFORM
            CLOSE G
            DISPLAY "RANGE " FIRST-KEY " " LAST-KEY
            DISPLAY "DONE"
            STOP RUN.
        RELEASE-RECORDS.
            PERFORM VARYING I FROM 5000 BY -1 UNTIL I < 1
                MOVE I TO SK
                MOVE ALL "X" TO SX
                RELEASE SR
            END-PERFORM.
        END PROGRAM F3SWF.
        """;

    /// <summary>The GIVING file, in the test's own directory, so the test can look at it after the run.</summary>
    private static string GivingPath(string dir) => Path.Combine(dir, "F3SWF-G.DAT");

    private static (bool ok, string stdout, string detail) Run(string dir, string tmpDirectory) => CobolNet.CompileAndRun(Program(dir),
        new Dictionary<string, string?>
        {
            [MemoryVariable] = "64K",
            [DirectoryVariable] = tmpDirectory,
        });

    [Fact]   // the control: with a usable work directory the SORT spills, merges and returns every record in key order
    public void SortInWhen_WithAUsableWorkDirectory_SortsAndTheFinallyRuns()
    {
        string dir = ScratchDirectory.Create("f3swf");
        try
        {
            string work = Directory.CreateDirectory(Path.Combine(dir, "sortwork")).FullName;
            var (ok, stdout, detail) = Run(dir, work);
            Assert.True(ok, $"WiseOwl COBOL failed: {detail}\nstdout:\n{stdout}");
            // ascending 1 .. 5000 (§14.9.40.4 GR3: the records are returned in key order); the FINALLY phrase runs
            // after the WHEN phrase has completed (§14.9.28.4 GR16: "the end of the PERFORM statement begins at imperative-statement-5")
            Assert.Equal("IN-WHEN 35\nSORTED 00\nAFTER-OPEN\nFINALLY\nRANGE 00000001 00005000\nDONE", stdout);
            Assert.Empty(Directory.EnumerateFileSystemEntries(work));   // D-SWK (b): no run file outlives the statement
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    [Fact]   // D-SWK (d): the missing work directory ends the run unit; the PERFORM's WHEN OTHER, FINALLY and what follows never run
    public void SortInWhen_WithAMissingWorkDirectory_StopsTheRunUnit_NoWhenOtherNoFinally()
    {
        string dir = ScratchDirectory.Create("f3swn");
        try
        {
            string missing = Path.Combine(dir, "no-such-directory");
            var (ok, stdout, detail) = Run(dir, missing);
            Assert.False(ok, $"expected the run unit to stop on the missing sort work directory; ran clean:\n{stdout}");
            Assert.Equal("IN-WHEN 35", stdout);   // everything before the SORT ran; nothing after it did
            Assert.Contains(DirectoryVariable, detail, StringComparison.Ordinal);
            Assert.Contains("no-such-directory", detail, StringComparison.Ordinal);
            // §14.9.40.4 GR15 a): the GIVING file is initiated only after the input procedure, so it was never opened
            Assert.False(File.Exists(GivingPath(dir)), "the stopped SORT left its GIVING file behind");
        }
        finally { ScratchDirectory.Remove(dir); }
    }
}
