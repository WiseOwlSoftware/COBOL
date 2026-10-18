// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>kb/Work PB1571 (campaign PB3353, review round 2) — the DISPOSITION of the EC-SORT-MERGE-ACTIVE / EC-FLOW-*
/// order when NO USE procedure handles the condition (docs/CONFORMANCE.md 3, D-SMA (a) and (b)). The golden
/// <c>wiseowl/cases/14.9.40-SORT/pb1571_sort_merge_active_vs_flow</c> witnesses the order through one declarative; the
/// disposition of an UNDECLARED raise is an exit code and a stderr diagnostic a golden's standard output cannot show, so
/// it is asserted here through a separate process.
/// <list type="bullet">
/// <item>A RETURN written in an input procedure (14.9.40.4 GR10) or a RELEASE written in an output procedure (GR13) is a
/// statement that is neither a SORT nor a MERGE, so §14.6.13.1.3 2) does not apply and the general fatal disposition does
/// (5) and 7), "Fatal exception conditions"): the run unit terminates abnormally (§14.6.12) naming the condition the
/// order chose — EC-SORT-MERGE-ACTIVE where it is enabled, the EC-FLOW name where only that is.</item>
/// <item>A format 1 SORT executed in an output procedure raises the same EC-SORT-MERGE-ACTIVE, but the executed statement
/// IS a SORT, so §14.6.13.1.3 2) hands the condition to the SORT's own rules: the inner SORT terminates and the run unit
/// continues.</item>
/// </list></summary>
public sealed class SortMergeActiveUndeclaredDispositionTests
{
    private const string Header = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {0}.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT SF ASSIGN TO "{0}-f.srt".
            SELECT SG ASSIGN TO "{0}-g.srt".
        DATA DIVISION.
        FILE SECTION.
        SD SF.
        01 SR PIC X(3).
        SD SG.
        01 GR PIC X(3).
        WORKING-STORAGE SECTION.
        01 EOFF PIC 9 VALUE 0.
        PROCEDURE DIVISION.
        MAIN-P.
            SORT SF ON ASCENDING KEY SR
               INPUT PROCEDURE IP1 OUTPUT PROCEDURE OP1
            DISPLAY "DONE"
            STOP RUN.
        """;

    private const string ReturnAll = """
            PERFORM UNTIL EOFF = 1
              RETURN SF AT END MOVE 1 TO EOFF
                NOT AT END DISPLAY "GOT " SR
              END-RETURN
            END-PERFORM.
        """;

    /// <summary>The whole program in fixed form (every line indented to column 8, the paragraph names stay in area A
    /// because the raw literal is dedented to them), led by the >>TURN directive.</summary>
    private static string Program(string id, string turn, string procs)
    {
        string body = string.Format(Header, id) + "\n" + procs;
        var lines = body.Split('\n').Select(l => l.Length == 0 ? l : "       " + l);
        return $"      >>TURN {turn} CHECKING ON\n" + string.Join("\n", lines);
    }

    private static (int Exit, string Stdout, string Stderr) Run(string id, string turn, string procs) =>
        new CobolNetCompiler(2002).CompileAndRunExit(Program(id, turn, procs));

    private const string InputProcedureReturn = """
        IP1.
            MOVE "BBB" TO SR
            RELEASE SR
            RETURN SF AT END DISPLAY "STRAY-END" END-RETURN
            DISPLAY "IP1 END".
        OP1.
        """;

    private const string OutputProcedureRelease = """
        IP1.
            MOVE "BBB" TO SR
            RELEASE SR.
        OP1.
            MOVE "ZZZ" TO SR
            RELEASE SR
            DISPLAY "OP1 NEXT".
        """;

    /// <summary>A RETURN written in the input procedure, -ACTIVE enabled: abnormal run-unit termination naming
    /// EC-SORT-MERGE-ACTIVE (not EC-FLOW-RETURN, D-SMA (a)); nothing after the RETURN and not the "DONE" runs.</summary>
    [Fact]
    public void ReturnWrittenInTheInputProcedure_NoUse_TerminatesNamingActive()
    {
        var (exit, stdout, stderr) = Run("PB1571UR1", "EC-SORT-MERGE-ACTIVE EC-FLOW-RETURN",
            InputProcedureReturn + "\n" + ReturnAll);
        Assert.Equal(1, exit);
        Assert.Contains("abnormal run-unit termination", stderr);
        Assert.Contains("EC-SORT-MERGE-ACTIVE", stderr);
        Assert.Equal("", stdout);
    }

    /// <summary>A RELEASE written in the output procedure, -ACTIVE enabled: abnormal termination naming
    /// EC-SORT-MERGE-ACTIVE (not EC-FLOW-RELEASE); the DISPLAY after the RELEASE never runs.</summary>
    [Fact]
    public void ReleaseWrittenInTheOutputProcedure_NoUse_TerminatesNamingActive()
    {
        var (exit, stdout, stderr) = Run("PB1571UR2", "EC-SORT-MERGE-ACTIVE EC-FLOW-RELEASE", OutputProcedureRelease);
        Assert.Equal(1, exit);
        Assert.Contains("abnormal run-unit termination", stderr);
        Assert.Contains("EC-SORT-MERGE-ACTIVE", stderr);
        Assert.Equal("", stdout);
    }

    /// <summary>-ACTIVE not enabled: the EC-FLOW name the statement is set to raise is the one the run unit terminates
    /// on, so the order's second arm has its undeclared disposition too (RETURN, then RELEASE).</summary>
    [Fact]
    public void ActiveNotEnabled_NoUse_TerminatesNamingTheFlowCondition()
    {
        var (exit, stdout, stderr) = Run("PB1571UR3", "EC-FLOW-RETURN", InputProcedureReturn + "\n" + ReturnAll);
        Assert.Equal(1, exit);
        Assert.Contains("EC-FLOW-RETURN", stderr);
        Assert.DoesNotContain("EC-SORT-MERGE-ACTIVE", stderr);
        Assert.Equal("", stdout);

        (exit, stdout, stderr) = Run("PB1571UR4", "EC-FLOW-RELEASE", OutputProcedureRelease);
        Assert.Equal(1, exit);
        Assert.Contains("EC-FLOW-RELEASE", stderr);
        Assert.DoesNotContain("EC-SORT-MERGE-ACTIVE", stderr);
        Assert.Equal("", stdout);
    }

    /// <summary>A format 1 SORT executed (PERFORMed) in the output procedure with no USE procedure: EC-SORT-MERGE-ACTIVE
    /// is disposed of by the verb (§14.6.13.1.3 2)): the inner SORT ends without running its procedures, the output
    /// procedure carries on and the run unit continues to a normal end, the outer SORT's record still returned.</summary>
    [Fact]
    public void NestedSortInTheOutputProcedure_NoUse_TerminatesOnlyTheInnerSort()
    {
        const string procs = """
            IP1.
                MOVE "BBB" TO SR
                RELEASE SR.
            OP1.
                PERFORM INNER
                DISPLAY "OP1 AFTER INNER"
                PERFORM UNTIL EOFF = 1
                  RETURN SF AT END MOVE 1 TO EOFF
                    NOT AT END DISPLAY "GOT " SR
                  END-RETURN
                END-PERFORM.
            INNER.
                SORT SG ON ASCENDING KEY GR
                   INPUT PROCEDURE FG OUTPUT PROCEDURE OG.
            FG.
                DISPLAY "FG MUST NOT RUN".
            OG.
                DISPLAY "OG MUST NOT RUN".
            """;
        var (exit, stdout, stderr) = Run("PB1571UN1", "EC-SORT-MERGE-ACTIVE", procs);
        Assert.Equal(0, exit);
        Assert.Equal("OP1 AFTER INNER\nGOT BBB\nDONE", stdout.Replace("\r\n", "\n"));
        Assert.Equal("", stderr);
    }
}
