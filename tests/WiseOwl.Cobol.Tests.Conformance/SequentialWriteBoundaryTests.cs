// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// §1985:VII.4.7.4 GR14 (kb/Work PB5373 class 4): "When an attempt is made to write beyond the externally defined boundaries of a sequential file, an exception condition exists and the contents of the
/// record area are unaffected": (a) the I-O status is set "to a value indicating a boundary violation" and (b) "If a USE AFTER STANDARD EXCEPTION declarative is explicitly or implicitly specified for the
/// file-name ..., that declarative procedure will then be executed." Clause (c), no declarative: "the result is undefined", so nothing is asserted for it.
/// <para>What the boundary IS belongs to the implementor, and docs/CONFORMANCE.md DOC-A.1-108 states it: the capacity of the host medium, answered at the WRITE with status 34 (§1985:VII.1.3.5 GRL2.3 b: "A permanent error exists because of a boundary violation; an attempt is made to
/// write beyond the externally defined boundaries of a sequential file"). The one host medium that is full by construction is Linux's <c>/dev/full</c>, as in
/// <see cref="ReportLineRefusalConformanceTests"/>; a host without the device must not be Linux, so this cannot pass on the CI host by not running.</para>
/// </summary>
public sealed class SequentialWriteBoundaryTests
{
    private const string Source = """
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PB5373B.
               ENVIRONMENT DIVISION.
               INPUT-OUTPUT SECTION.
               FILE-CONTROL.
                   SELECT F ASSIGN TO "/dev/full" ORGANIZATION IS SEQUENTIAL
                       FILE STATUS IS FS.
               DATA DIVISION.
               FILE SECTION.
               FD  F.
               01  REC PIC X(512).
               WORKING-STORAGE SECTION.
               01  FS PIC XX.
               01  N PIC 9(6) VALUE 0.
               01  USE-COUNT PIC 9 VALUE 0.
               PROCEDURE DIVISION.
               DECLARATIVES.
               U SECTION.
                   USE AFTER STANDARD EXCEPTION PROCEDURE ON F.
               U1.
                   ADD 1 TO USE-COUNT.
               END DECLARATIVES.
               MAIN SECTION.
               M1.
                   OPEN OUTPUT F.
                   DISPLAY "OPEN FS=" FS " USE=" USE-COUNT.
                   MOVE ALL "X" TO REC.
                   PERFORM W1 UNTIL FS NOT = "00" OR N > 100000.
                   DISPLAY "WRITE FS=" FS " USE=" USE-COUNT.
                   IF REC = ALL "X" DISPLAY "RECORD AREA UNAFFECTED".
                   STOP RUN.
               W1.
                   ADD 1 TO N.
                   WRITE REC.
        """;

    /// <summary>The write that meets the full medium ends with status 34, runs the file's USE procedure once, and leaves the record area as the program set it.</summary>
    [Fact]
    public void AWriteBeyondTheBoundary_SetsTheBoundaryStatus_RunsTheUseDeclarative_AndLeavesTheRecordAreaAlone()
    {
        const string full = "/dev/full";
        if (!File.Exists(full))
        {
            Assert.False(OperatingSystem.IsLinux(), "a Linux host provides /dev/full; the boundary was not exercised");
            return;
        }
        var (ok, stdout, detail) = new WiseOwlCobolCompiler(85).CompileAndRun(Source);
        Assert.True(ok, $"WiseOwl COBOL failed: {detail}");
        Assert.Equal("OPEN FS=00 USE=0\nWRITE FS=34 USE=1\nRECORD AREA UNAFFECTED", stdout.ReplaceLineEndings("\n").TrimEnd('\n'));
    }
}
