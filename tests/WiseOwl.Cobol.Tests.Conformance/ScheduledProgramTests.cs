// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// Which program the message control system scheduled (kb/Work PB5282). §1985:XIV.2.2.4 GR28: "Whenever a program is scheduled by the MCS to process a message, that program establishes a run
/// unit and the symbolic name of the communication terminal that is the source of the message ... is placed in the data item referenced by data-name-3 of the input-output CD associated with the
/// INITIAL clause"; "In all other cases" that item is "initialized to spaces". A CALLed program is not scheduled, so its INITIAL CD holds spaces and the message stays for the program that is.
/// The scheduling terminal is the environment's (WISEOWL_COBOL_MCS_SCHEDULED), which a conformance-suite case cannot set, so the witness is a program run here.
/// </summary>
public sealed class ScheduledProgramTests
{
    private const string Source = """
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PB5282M.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01  BUF PIC X(10).
               COMMUNICATION SECTION.
               CD  MAINCD FOR INITIAL I-O
                   SYMBOLIC TERMINAL IS M-TERM
                   STATUS KEY IS M-STAT.
               PROCEDURE DIVISION.
               P1.
                   DISPLAY "MAIN TERM [" M-TERM "]".
                   CALL "PB5282S".
                   MOVE SPACES TO BUF.
                   RECEIVE MAINCD MESSAGE INTO BUF NO DATA DISPLAY "NO DATA".
                   DISPLAY "MAIN RECV " M-STAT " [" BUF "]".
                   STOP RUN.
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PB5282S.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01  SBUF PIC X(10).
               COMMUNICATION SECTION.
               CD  SUBCD FOR INITIAL I-O
                   SYMBOLIC TERMINAL IS S-TERM
                   STATUS KEY IS S-STAT.
               PROCEDURE DIVISION.
               S1.
                   DISPLAY "SUB TERM [" S-TERM "]".
                   EXIT PROGRAM.
               END PROGRAM PB5282S.
               END PROGRAM PB5282M.
        """;

    /// <summary>The scheduled main program's INITIAL CD names the terminal and its RECEIVE returns the message; the CALLed program's INITIAL CD is spaces, and its activation takes no message.</summary>
    [Fact]
    public void ACalledProgramsInitialCd_IsSpaces_AndTheScheduledProgramKeepsTheMessage()
    {
        string dir = ScratchDirectory.Create("WiseOwlCobol_Pb5282_");
        try
        {
            File.WriteAllText(Path.Combine(dir, "mcs.json"), """{"terminals":[{"name":"T1","messages":["HELLO"]},{"name":"T2"}]}""");
            NonCobolElement.CompileCobol(dir, "PB5282M", Source, dialect: 85);
            var (ok, stdout, detail) = CutRunner.Run(Path.Combine(dir, "PB5282M.dll"), dir, null,
                new Dictionary<string, string?> { ["WISEOWL_COBOL_MCS_SCHEDULED"] = "T1" });
            Assert.True(ok, detail);
            Assert.Equal("MAIN TERM [T1          ]\nSUB TERM [            ]\nMAIN RECV 00 [HELLO     ]", stdout.TrimEnd());
        }
        finally { ScratchDirectory.Remove(dir); }
    }
}
