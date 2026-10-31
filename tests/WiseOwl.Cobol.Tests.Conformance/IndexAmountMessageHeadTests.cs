// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// ⛔ THE MESSAGE OF A FATAL INDEX-AMOUNT EXCEPTION OPENS WITH THE PHRASE THE PROGRAMMER WROTE, NEVER WITH AN ELIDED
/// STATEMENT (kb/Work PB4212, PB4160's rule at the layer of the running program).
/// <para>ISO §14.9.39.4 GR2 a) 1. a) (index-assignment), GR3 (index-arithmetic) and §13.18.38.4 GR2
/// (an index outside the implementor range, set by PERFORM VARYING as well) each set a Table 13 exception condition to exist; with
/// checking on and no declarative the condition is fatal (§14.6.13.1.3) and the run unit ends abnormally
/// (<c>abnormal run-unit termination: EC-… (fatal): detail</c>). The detail used to open <c>SET … TO</c> /
/// <c>SET … UP BY</c> / <c>PERFORM VARYING … FROM</c>, an ellipsis the console renders as <c>.</c>, a statement nobody
/// wrote. The head now names the receiving operand's role in the general format (<c>SET index-name TO</c>), as the
/// capacity register's own <c>SET capacity-register TO</c> already did. The expectation is the phrase and its rule
/// citation, derived from the cited general rules and not from a run.</para>
/// <para>The sibling messages of <c>FileRegistry</c> (<c>OPEN WITH NO REWIND</c>, <c>OPEN REVERSED</c>) are raised only
/// when the binder let a verb reach the wrong connector, a compiler defect that no source reaches, so the source scan
/// <c>DiagnosticOpeningElisionDriftTests</c> is their witness.</para>
/// </summary>
public sealed class IndexAmountMessageHeadTests
{
    private static string Program(string id, string checking, string data, string statement) => $"""
        >>TURN {checking} CHECKING ON
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {id}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 T.
           05 E PIC 9 OCCURS 5 INDEXED BY IX.
        {data}
        PROCEDURE DIVISION.
            {statement}
            DISPLAY "NOT-REACHED".
            STOP RUN.
        """;

    private static void AssertFatal(string source, string expectedDetail)
    {
        var (exit, stdout, stderr) = new WiseOwlCobolCompiler(2023).CompileAndRunExit(source);
        Assert.Equal(1, exit);                                  // §14.6.12 — the abnormal-termination exit
        Assert.DoesNotContain("NOT-REACHED", stdout);           // a fatal condition ends the run unit
        Assert.Contains("abnormal run-unit termination", stderr);
        Assert.Contains(expectedDetail, stderr);
        Assert.DoesNotContain("…", stderr);                     // no elided statement at the head
        Assert.DoesNotContain("...", stderr);
    }

    /// <summary>§14.9.39.4 GR2 a) 1. a): the value of arithmetic-expression-1 is not an integer.</summary>
    [Fact]
    public void SetIndexTo_NonInteger_NamesTheIndexAssignmentPhrase() =>
        AssertFatal(Program("PB4212A", "EC-BOUND-SUBSCRIPT", "01 N PIC S9V9 VALUE 1.5.", "SET IX TO N."),
            "EC-BOUND-SUBSCRIPT (fatal): SET index-name TO — a non-integer value (ISO 14.9.39.4 GR2 a) 1. a)");

    /// <summary>§14.9.39.4 GR3: UP BY a non-integer arithmetic-expression-2.</summary>
    [Fact]
    public void SetIndexUpBy_NonInteger_NamesTheUpByPhrase() =>
        AssertFatal(Program("PB4212B", "EC-BOUND-SUBSCRIPT", "01 N PIC S9V9 VALUE 1.5.", "SET IX UP BY N."),
            "EC-BOUND-SUBSCRIPT (fatal): SET index-name UP BY — a non-integer amount (ISO 14.9.39.4 GR3)");

    /// <summary>§14.9.39.4 GR3: DOWN BY a non-integer arithmetic-expression-2.</summary>
    [Fact]
    public void SetIndexDownBy_NonInteger_NamesTheDownByPhrase() =>
        AssertFatal(Program("PB4212C", "EC-BOUND-SUBSCRIPT", "01 N PIC S9V9 VALUE 1.5.", "SET IX DOWN BY N."),
            "EC-BOUND-SUBSCRIPT (fatal): SET index-name DOWN BY — a non-integer amount (ISO 14.9.39.4 GR3)");

    /// <summary>§14.9.28.3 SR4 a) admits a 31-digit integer data item as the FROM operand; a value past the implementor
    /// index range (§13.18.38.4 GR2) is EC-RANGE-INDEX when the index is initialized.</summary>
    [Fact]
    public void PerformVaryingFrom_OutOfRange_NamesTheFromPhrase() =>
        AssertFatal(
            Program("PB4212D", "EC-RANGE-INDEX", "01 BIG PIC 9(31) VALUE 9999999999999999999999999999999.",
                "PERFORM VARYING IX FROM BIG BY 1 UNTIL IX > 2 CONTINUE END-PERFORM."),
            "EC-RANGE-INDEX (fatal): PERFORM VARYING index-name FROM — outside the implementor index range");
}
