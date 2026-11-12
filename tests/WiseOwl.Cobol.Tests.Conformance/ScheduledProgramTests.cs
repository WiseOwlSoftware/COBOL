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
/// <para>kb/Work PB5373 class 4 adds the other two rules whose antecedent is a scheduled program: §1985:XIV.2.2.4 GR25 (the first RECEIVE of the INITIAL input-output CD returns the message that caused the
/// scheduling) and GR30 (the program must not change data-name-3; if it does, every statement referencing the CD is unsuccessful and the status data item says unknown source). The antecedent is the
/// implementor's to define (docs/CONFORMANCE.md, "A program scheduled by the MCS": the environment variable names the terminal, and its first queued message is the one that caused the scheduling).</para>
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

    private const string ReceiveSource = """
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PB5373R.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01  BUF PIC X(10).
               COMMUNICATION SECTION.
               CD  MAINCD FOR INITIAL I-O
                   SYMBOLIC TERMINAL IS M-TERM
                   STATUS KEY IS M-STAT.
               PROCEDURE DIVISION.
               P1.
                   MOVE SPACES TO BUF.
                   RECEIVE MAINCD MESSAGE INTO BUF NO DATA DISPLAY "NO DATA".
                   DISPLAY "FIRST " M-STAT " " M-TERM " [" BUF "]".
                   MOVE "T2" TO M-TERM.
                   MOVE SPACES TO BUF.
                   RECEIVE MAINCD MESSAGE INTO BUF NO DATA DISPLAY "NO DATA".
                   DISPLAY "CHANGED " M-STAT " [" BUF "]".
                   MOVE "T1" TO M-TERM.
                   RECEIVE MAINCD MESSAGE INTO BUF NO DATA DISPLAY "NO DATA".
                   DISPLAY "RESTORED " M-STAT " [" BUF "]".
                   STOP RUN.
               END PROGRAM PB5373R.
        """;

    /// <summary>§1985:XIV.2.2.4 GR25: the first RECEIVE referencing the INITIAL input-output CD of a scheduled program returns the message that caused the scheduling: terminal T1's head message, never T2's, with
    /// the terminal name in data-name-3 and status 00. GR30: the program then changes data-name-3 to T2, and the next RECEIVE is unsuccessful with status 21 ("Symbolic source is unknown. No action taken.",
    /// the status table of §1985:XIV.2.2.5) and delivers nothing; restoring T1 makes the statement succeed again and return T1's next message.</summary>
    [Fact]
    public void TheScheduledProgram_FirstReceiveReturnsTheSchedulingMessage_AndAChangedTerminalMakesTheStatementUnsuccessful()
    {
        string dir = ScratchDirectory.Create("WiseOwlCobol_Pb5373_");
        try
        {
            File.WriteAllText(Path.Combine(dir, "mcs.json"),
                """{"terminals":[{"name":"T1","messages":["ALPHA","BETA"]},{"name":"T2","messages":["OTHER"]}]}""");
            NonCobolElement.CompileCobol(dir, "PB5373R", ReceiveSource, dialect: 85);
            var (ok, stdout, detail) = CutRunner.Run(Path.Combine(dir, "PB5373R.dll"), dir, null,
                new Dictionary<string, string?> { ["WISEOWL_COBOL_MCS_SCHEDULED"] = "T1" });
            Assert.True(ok, detail);
            Assert.Equal("FIRST 00 T1           [ALPHA     ]\nCHANGED 21 [          ]\nRESTORED 00 [BETA      ]", stdout.TrimEnd());
        }
        finally { ScratchDirectory.Remove(dir); }
    }
}
