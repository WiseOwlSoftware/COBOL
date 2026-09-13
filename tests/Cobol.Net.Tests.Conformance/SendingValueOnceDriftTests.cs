// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ ONE SENDER, MANY RECEIVERS, ONE EVALUATION — the drift net under kb/Work PB394's mechanism.
///
/// <para>ISO §14.9.25.4 GR1 (MOVE) makes the sending value an intermediate result item evaluated "only once,
/// immediately before data is moved to the first of the receiving operands"; §14.9.13.4 GR3 (EVALUATE) assigns
/// each selection subject its value "at the beginning of the execution of the EVALUATE statement"; §14.7.7 GR4
/// gives the arithmetic family its ONE initial evaluation; §14.9.39.4 GR12/GR14/GR16/GR18 store one identified
/// address into each SET receiver "in the order specified". Five clauses, one shape — and before PB394, MOVE and
/// EVALUATE were the two that did not implement it, each re-running the source EXPRESSION at every use.</para>
///
/// <para>A value-varying sender is what MEASURES the cardinality, and <c>FUNCTION RANDOM</c> is the one the
/// standard makes reproducible: §15.75.3 r3 — "If a subsequent reference specifies argument-1, a new sequence of
/// pseudo-random numbers is started" — and §15.75.4 r2 — "For a given seed value on a given implementation, the
/// sequence of pseudo-random numbers will always be the same". So every assertion below compares a RE-SEEDED run
/// against a control and never against a particular pseudo-random value: the tests pin the RULE, not this
/// implementation's sequence.</para>
/// </summary>
public sealed class SendingValueOnceDriftTests
{
    /// <summary>Every verb that takes ONE sending operand and MORE THAN ONE receiving operand, written so that
    /// re-evaluating the sender gives the receivers different values. §15.75.4 r1 puts every RANDOM value in
    /// [0, 1), so two receivers of one evaluation are equal and two evaluations are (with probability 1 for this
    /// generator) not.</summary>
    [Theory]
    // MOVE (§14.9.25.4 GR1) — the half PB394 measured wrong: MoveEmitter re-rendered BoundMove.Source in its
    // per-target loop, so the second receiver took a second RANDOM value.
    [InlineData("SVO01", "MOVE FUNCTION RANDOM TO W-A, W-B")]
    // ADD … GIVING / SUBTRACT … GIVING / MULTIPLY … GIVING (§14.7.7 GR4 + NOTE 3 — the one initial evaluation).
    [InlineData("SVO02", "ADD FUNCTION RANDOM 0 GIVING W-A, W-B")]
    [InlineData("SVO03", "SUBTRACT 0 FROM FUNCTION RANDOM GIVING W-A, W-B")]
    [InlineData("SVO04", "MULTIPLY FUNCTION RANDOM BY 1 GIVING W-A, W-B")]
    // COMPUTE (§14.7.7 GR4) — the arm that already carried the rule, kept here so the family is measured as one.
    [InlineData("SVO05", "COMPUTE W-A, W-B = FUNCTION RANDOM")]
    // DIVIDE … GIVING (§14.9.12.4 GR5 → §14.7.7 GR4).
    [InlineData("SVO06", "DIVIDE FUNCTION RANDOM BY 1 GIVING W-A, W-B")]
    public void OneSenderManyReceivers_EveryReceiverTakesTheSameValue(string pid, string stmt)
    {
        string src = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. {pid}.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 W-SEED PIC 9V9(6).
            01 W-A PIC 9V9(6).
            01 W-B PIC 9V9(6).
            PROCEDURE DIVISION.
            MAIN.
                MOVE FUNCTION RANDOM(1) TO W-SEED.
                {stmt}.
                IF W-A = W-B
                    DISPLAY "ONCE"
                ELSE
                    DISPLAY "MANY"
                END-IF.
                STOP RUN.
            """;
        var (ok, stdout, detail) = new CobolNetCompiler(2023).CompileAndRun(src);
        Assert.True(ok, detail);
        Assert.Equal("ONCE", stdout.Replace("\r\n", "\n").TrimEnd('\n'));
    }

    /// <summary>§14.9.13.4 GR3 over the EVALUATE lowering, at every edition — the subject must be evaluated
    /// EXACTLY once per statement however many WHEN phrases read it, and a THRU object (GR4 a) 5.: "selection-
    /// subject &gt;= left-part AND selection-subject &lt;= right-part") reads the ONE value twice rather than
    /// evaluating the subject twice. Measured by consumption: the control drives the identical WHEN phrases from
    /// a DATA ITEM holding one value, the test drives them from the function, and both then read the next number
    /// of the same re-seeded sequence. Equal reads ⇒ the statement consumed exactly one.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void EvaluateSubject_IsEvaluatedOncePerStatement_AtEveryEdition(int std)
    {
        string src = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SVOEV{std}.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 W-SEED PIC 9V9(6).
            01 W-SUBJ PIC 9V9(6).
            01 W-CTRL PIC 9V9(6).
            01 W-TEST PIC 9V9(6).
            01 W-CTRL-BR PIC X(5).
            01 W-TEST-BR PIC X(5).
            PROCEDURE DIVISION.
            MAIN.
                MOVE FUNCTION RANDOM(1) TO W-SEED.
                MOVE FUNCTION RANDOM TO W-SUBJ.
                EVALUATE W-SUBJ
                    WHEN 0 THRU 0.25
                        MOVE "BAND1" TO W-CTRL-BR
                    WHEN 0.25 THRU 0.5
                        MOVE "BAND2" TO W-CTRL-BR
                    WHEN OTHER
                        MOVE "BAND3" TO W-CTRL-BR
                END-EVALUATE.
                MOVE FUNCTION RANDOM TO W-CTRL.
                MOVE FUNCTION RANDOM(1) TO W-SEED.
                EVALUATE FUNCTION RANDOM
                    WHEN 0 THRU 0.25
                        MOVE "BAND1" TO W-TEST-BR
                    WHEN 0.25 THRU 0.5
                        MOVE "BAND2" TO W-TEST-BR
                    WHEN OTHER
                        MOVE "BAND3" TO W-TEST-BR
                END-EVALUATE.
                MOVE FUNCTION RANDOM TO W-TEST.
                IF W-TEST-BR = W-CTRL-BR
                    DISPLAY "BRANCH=SAME"
                ELSE
                    DISPLAY "BRANCH=DIFFERENT"
                END-IF.
                IF W-TEST = W-CTRL
                    DISPLAY "COUNT=ONE"
                ELSE
                    DISPLAY "COUNT=MANY"
                END-IF.
                STOP RUN.
            """;
        var (ok, stdout, detail) = new CobolNetCompiler(std).CompileAndRun(src);
        Assert.True(ok, detail);
        Assert.Equal("BRANCH=SAME\nCOUNT=ONE", stdout.Replace("\r\n", "\n").TrimEnd('\n'));
    }

    /// <summary>⛔ THE INTERMEDIATE MUST HOLD THE SENDER'S LENGTH, NOT A PADDED APPROXIMATION OF IT.
    /// §14.9.25.4 GR1's equivalence is a RESULT equivalence, so the one-receiver and many-receiver forms of the
    /// same MOVE have to agree — and a JUSTIFIED receiver is what MEASURES that, because §13.18.32.4 GR1 aligns
    /// the sending data at the rightmost character position and space-fills on the LEFT, which makes the sender's
    /// length observable where a left-justified receiver hides it. The sender that can get it wrong is one whose
    /// length is decided at run time: §15.4's temporary for a character-category function is carried by a
    /// dynamic-length item, "treated as a fixed-length data item whose length is the dynamic-length elementary
    /// item's current length" (§8.5.1.10.4). <c>FUNCTION TRIM</c> is the shortest sender with that property.
    /// <para>This test is the net under the residue named on GR-14.9.25.4-1: an earlier revision of kb/Work
    /// PB394 froze an OCCURS DEPENDING group sender at its MAXIMUM extent, and a JUSTIFIED receiver is exactly
    /// where that showed up.</para></summary>
    [Fact]
    public void RunTimeLengthSender_KeepsItsLength_UnderAJustifiedReceiver()
    {
        const string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SVOJUST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 W-J1 PIC X(8) JUSTIFIED RIGHT.
            01 W-J2 PIC X(8) JUSTIFIED RIGHT.
            01 W-T  PIC X(8).
            PROCEDURE DIVISION.
            MAIN.
                MOVE FUNCTION TRIM("  hi  ") TO W-J1.
                MOVE FUNCTION TRIM("  hi  ") TO W-J2, W-T.
                IF W-J1 = W-J2
                    DISPLAY "SAME"
                ELSE
                    DISPLAY "DIFFERENT"
                END-IF.
                STOP RUN.
            """;
        var (ok, stdout, detail) = new CobolNetCompiler(2023).CompileAndRun(src);
        Assert.True(ok, detail);
        Assert.Equal("SAME", stdout.Replace("\r\n", "\n").TrimEnd('\n'));
    }

    /// <summary>⛔ THE LEAF SET <c>SendingValueTemp.Model</c> ANSWERS FOR. Its switch names every
    /// <see cref="BoundOperand"/> leaf and decides, per leaf, whether the §14.9.25.4 GR1 intermediate result item
    /// exists and what its description is — so a leaf added later must be a DECISION, not a fall-through. C#
    /// cannot prove a public abstract record's leaf set closed, so the switch carries a throwing arm and this
    /// test carries the roster: adding a <see cref="BoundOperand"/> fails here, in the test that names the rule,
    /// rather than at run time inside somebody's MOVE (feedback_a_dead_lookup_is_also_unverified).</summary>
    [Fact]
    public void BoundOperandLeafSet_IsTheOneSendingValueTempAnswersFor()
    {
        string[] expected =
        [
            nameof(BoundAllLiteral),        // no intermediate — §8.3.3.6.4 GR2 sizes it from the RECEIVER
            nameof(BoundBoolOperand),       // no intermediate — the §8.8.2 boolean-expression channel
            nameof(BoundComputedOperand),   // §15.4's temporary elementary data item
            nameof(BoundCurrentRecord),     // the record area's description, cloned
            nameof(BoundFieldOperand),      // the item's description, cloned (§8.4.3.3.4 GR6 when ref-modified)
            nameof(BoundFigurative),        // no intermediate — receiver-sized, and GR1's antecedent excludes it
            nameof(BoundNumericLiteral),    // no intermediate — a literal's value cannot change
            nameof(BoundOperandError),      // no intermediate — already diagnosed
            nameof(BoundStringLiteral),     // no intermediate — a literal's value cannot change
        ];
        var actual = typeof(BoundOperand).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && typeof(BoundOperand).IsAssignableFrom(t))
            .Select(t => t.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(expected, actual);
    }
}
