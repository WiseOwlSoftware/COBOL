// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ EC-RANGE-INVALID IS A PROPERTY OF A THROUGH RANGE'S <b>CLASS</b>, NEVER OF ITS OPERANDS' WRITTEN FORM.
///
/// <para>ISO §14.7.8 opens "This specification applies to THROUGH phrases specified in the VALUE clause and the
/// EVALUATE statement" and then splits into exactly two rules: rule 1 for a numeric range ("the range of values
/// includes literal-1, literal-2, and all algebraic values between literal-1 and literal-2" — algebraic, no
/// collating sequence, and NO exception condition) and rule 2 for an alphanumeric or national one, which owns the
/// collating sequence, the <c>IN alphabet-name</c> phrase AND the exception: "When the value of literal-1 is
/// greater than the value of literal-2 in the collating sequence in effect at runtime, the EC-RANGE-INVALID
/// exception condition is set to exist, and, upon completion of any exception processing, execution proceeds as
/// if the range of values were empty." §14.9.13.4 GR2 and §13.18.63.4 GR18 both delegate their THROUGH phrase to
/// that one clause.</para>
///
/// <para>⛔ WHAT THIS PINS, AND WHY IT IS A DRIFT TEST RATHER THAN ONE GOLDEN (kb/Work PB401). The EVALUATE gate
/// used to read <c>lo is BoundStringLiteral{…} &amp;&amp; hi is BoundStringLiteral</c>, so the exception could be
/// raised ONLY where a reader of the source can already see the inversion and NEVER in the run-time case rule 2's
/// own "in the collating sequence in effect at runtime" is written for: <c>EVALUATE X WHEN WS-M THRU WS-A</c> set
/// nothing while <c>EVALUATE X WHEN "M" THRU "A"</c> over the identical values set EC-RANGE-INVALID — and the
/// level-88 VALUE range, the OTHER clause §14.7.8's first sentence governs, already asked the CLASS. Rule 2's
/// "literals" is not an operand-form restriction: §14.7.8 introduces the phrase as "a range of values, literal-1
/// through literal-2" and EVALUATE's range-expression has no literal-1/literal-2 at all, while §14.9.13.3 SR3 —
/// a rule about THIS phrase — says "the literals <b>or identifiers</b> specified in the THROUGH phrase are of
/// class alphabetic, alphanumeric, or national".</para>
///
/// <para>⛔ EACH CASE IS ITS OWN PROCESS, AND THAT IS THE POINT. <c>FUNCTION EXCEPTION-STATUS</c> is STICKY, so a
/// single program can witness only its FIRST raising arm; writing the arms into one source is how the original
/// probe reported "both raise" from the literal arm's own carry-over and produced no evidence at all about the
/// identifier arm (<c>feedback_probe_the_shape_the_subject_hides</c>). <see cref="CobolNetCompiler"/> compiles and
/// runs each row in a fresh process, so every arm is measured against a clean status.</para>
/// </summary>
public sealed class ThroughRangeExceptionTests
{
    /// <summary>One program: a subject, a WHEN range, a WHEN OTHER, and <c>FUNCTION EXCEPTION-STATUS</c> printed
    /// AFTER the EVALUATE under <c>&gt;&gt;TURN EC-RANGE-INVALID CHECKING ON</c>. The two printed lines are the
    /// two halves of §14.7.8 rule 2 — the BRANCH (the range treated as empty) and the EXCEPTION — so a fix that
    /// produces the empty-range behaviour without the exception, which is exactly the state this test was written
    /// against, fails on the second line while passing on the first.</summary>
    private static string Prog(string pid, string env, string data, string range) => $"""
        >>TURN EC-RANGE-INVALID CHECKING ON
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {pid}.
        {env}DATA DIVISION.
        WORKING-STORAGE SECTION.
        {data}
        PROCEDURE DIVISION.
        MAIN.
            EVALUATE W-SUBJ
                WHEN {range}
                    DISPLAY "IN"
                WHEN OTHER
                    DISPLAY "OUT"
            END-EVALUATE.
            DISPLAY "EC[" FUNCTION EXCEPTION-STATUS "]".
            STOP RUN.
        """;

    private const string AlphaData = """
        01 W-SUBJ PIC X VALUE "C".
               01 W-LO   PIC X VALUE "M".
               01 W-HI   PIC X VALUE "A".
               01 W-A    PIC X VALUE "A".
               01 W-Z    PIC X VALUE "Z".
        """;

    private const string NumData = """
        01 W-SUBJ PIC 9(3) VALUE 005.
               01 W-LO   PIC 9(3) VALUE 009.
               01 W-HI   PIC 9(3) VALUE 001.
               01 W-A    PIC 9(3) VALUE 001.
               01 W-B    PIC 9(3) VALUE 008.
        """;

    private const string NatData = """
        01 W-SUBJ PIC N(1) VALUE N"C".
               01 W-LO   PIC N(1) VALUE N"M".
               01 W-HI   PIC N(1) VALUE N"A".
        """;

    private const string LowerData = """
        01 W-SUBJ PIC X VALUE "C".
               01 W-LO   PIC X VALUE "m".
               01 W-HI   PIC X VALUE "a".
        """;

    /// <summary>A TWO-character subject, so a figurative end sized to it (§8.3.3.6.4 GR2) differs observably from
    /// the one-character seed — the width at which the carrier used to answer the opposite of the relation pair
    /// it lowers to.</summary>
    private const string LowValData = """
        01 W-SUBJ PIC X(2) VALUE LOW-VALUES.
        """;

    private const string Wide2Data = """
        01 W-SUBJ PIC X(2) VALUE "CC".
        """;

    private const string GroupData = """
        01 W-SUBJ PIC X(2) VALUE "CC".
               01 G-LO.
                  05 G-LO-1 PIC X VALUE "M".
                  05 G-LO-2 PIC X VALUE "M".
               01 G-HI.
                  05 G-HI-1 PIC X VALUE "A".
                  05 G-HI-2 PIC X VALUE "A".
        """;

    /// <summary>Every operand form §14.9.13.2's range-expression admits, in both polarities of the rule-1 /
    /// rule-2 split. The expected text is DERIVED: an inverted range of class alphanumeric or national takes rule
    /// 2 (empty range + EC-RANGE-INVALID, whatever the ends are written as); a numeric range takes rule 1, whose
    /// inverted form is simply empty with NO exception; a well-ordered range is neither empty nor exceptional.
    /// <para>The status field is <c>PIC X(31)</c>-wide per §15.22, and <c>CutRunner.Normalize</c> strips the
    /// trailing spaces of the line, so an empty status prints as <c>EC[</c> + <c>]</c> with the padding
    /// collapsed only at end of line — hence the closing bracket is kept in the expectation.</para></summary>
    [Theory]
    // ── rule 2, INVERTED ("M" collates after "A"): empty range AND the exception, for every operand form ──
    // Both ends IDENTIFIERS — the shape the whole defect was about: the inversion is not visible in the source,
    // which is precisely the case rule 2's "in the collating sequence in effect at runtime" is written for.
    [InlineData("PBR01", "", AlphaData, "W-LO THRU W-HI", "OUT\nEC[EC-RANGE-INVALID               ]")]
    // MIXED — a literal start and an identifier end, and its transpose. The old gate demanded BOTH ends be
    // string literals, so each of these fell to the plain relation pair.
    [InlineData("PBR02", "", AlphaData, "\"M\" THRU W-HI", "OUT\nEC[EC-RANGE-INVALID               ]")]
    [InlineData("PBR03", "", AlphaData, "W-LO THRU \"A\"", "OUT\nEC[EC-RANGE-INVALID               ]")]
    // Both ends LITERALS — the one arm that already worked; kept so a repair cannot trade it away.
    [InlineData("PBR04", "", AlphaData, "\"M\" THRU \"A\"", "OUT\nEC[EC-RANGE-INVALID               ]")]
    // FUNCTION results as ends (§15.2 — an alphanumeric-type function is a class alphanumeric operand):
    // UPPER-CASE("m") THRU UPPER-CASE("a") is "M" THRU "A".
    [InlineData("PBR05", "", LowerData, "FUNCTION UPPER-CASE(W-LO) THRU FUNCTION UPPER-CASE(W-HI)",
        "OUT\nEC[EC-RANGE-INVALID               ]")]
    // Alphanumeric GROUP ends — §8.8.4.2.1 "For comparison, an alphanumeric group item shall be treated as an
    // elementary alphanumeric data item", so "MM" THRU "AA" is an inverted class-alphanumeric range.
    [InlineData("PBR06", "", GroupData, "G-LO THRU G-HI", "OUT\nEC[EC-RANGE-INVALID               ]")]
    // NATIONAL ends — §14.7.8 rule 2's stem names the alphanumeric and the national range in one breath, and
    // §8.8.4.2.9 is the comparison it then orders.
    [InlineData("PBR07", "", NatData, "W-LO THRU W-HI", "OUT\nEC[EC-RANGE-INVALID               ]")]
    // ── rule 2, WELL-ORDERED: no exception, and "C" is inside "A" THRU "Z" ──
    [InlineData("PBR08", "", AlphaData, "W-A THRU W-Z", "IN\nEC[                               ]")]
    [InlineData("PBR09", "", AlphaData, "\"A\" THRU \"Z\"", "IN\nEC[                               ]")]
    // ── rule 1: a NUMERIC range sets no exception in either direction ──
    // Inverted numeric identifier ends (9 THRU 1): empty by rule 1's algebraic reading, and NO exception.
    [InlineData("PBR10", "", NumData, "W-LO THRU W-HI", "OUT\nEC[                               ]")]
    // ARITHMETIC-EXPRESSION ends (§14.9.13.2's arithmetic-expression-3/-4, numeric by §8.8.1.1): 2 THRU 10
    // contains 5 — and it must still be compared ALGEBRAICALLY, which is what fails the moment a range's class
    // is read from an operand that answers "no category" (kb/Work PB401: it answered ALPHANUMERIC, and "005" is
    // not >= "2").
    [InlineData("PBR11", "", NumData, "W-A + 1 THRU W-B + 2", "IN\nEC[                               ]")]
    [InlineData("PBR12", "", NumData, "1 THRU 9", "IN\nEC[                               ]")]
    [InlineData("PBR13", "", NumData, "9 THRU 1", "OUT\nEC[                               ]")]
    // ── a FIGURATIVE end is a SEED sized to the tested item (§8.3.3.6.4 GR2), not a one-character value ──
    // PIC X(2) LOW-VALUES against LOW-VALUE THRU "AA": GR2 makes the start "\0\0", which the subject EQUALS, so
    // the range is well-ordered AND contains it. Sized as ONE character the start would be "\0" padded with a
    // SPACE — "\0 " — which the subject sorts BELOW, flipping the answer (kb/Work PB401's sibling).
    [InlineData("PBR14", "", LowValData, "LOW-VALUE THRU \"AA\"", "IN\nEC[                               ]")]
    // The same sizing decides an INVERSION: "MM" THRU SPACE is "MM" THRU "  ", which is inverted.
    [InlineData("PBR15", "", Wide2Data, "\"MM\" THRU SPACE", "OUT\nEC[EC-RANGE-INVALID               ]")]
    // ALL literal-1 ends (§8.3.3.6.3 SR2 / §8.3.3.6.4 GR2 c): "AA" THRU "ZZ" contains "CC".
    [InlineData("PBR16", "", Wide2Data, "ALL \"A\" THRU ALL \"Z\"", "IN\nEC[                               ]")]
    public void ThroughRange_RaisesRangeInvalidByClassNotByOperandForm(
        string pid, string env, string data, string range, string expected)
    {
        var (ok, stdout, detail) = new CobolNetCompiler(2002).CompileAndRun(Prog(pid, env, data, range));
        Assert.True(ok, detail);
        Assert.Equal(expected.Replace("\r\n", "\n"), stdout.Replace("\r\n", "\n").TrimEnd('\n'));
    }

    /// <summary>§14.9.13.4 GR4 a) 5. inverts a <c>NOT</c> range to "&lt; left-part OR &gt; right-part", and the
    /// exception is a property of the RANGE, not of the phrase's polarity — §14.7.8 rule 2 is reached through
    /// GR2's delegation before any negation is applied. So <c>WHEN NOT W-LO THRU W-HI</c> over an inverted range
    /// still sets EC-RANGE-INVALID, and the empty range makes the negated test TRUE.</summary>
    [Fact]
    public void NotRange_IsStillTheSameRange_ForTheException()
    {
        var (ok, stdout, detail) = new CobolNetCompiler(2002)
            .CompileAndRun(Prog("PBR20", "", AlphaData, "NOT W-LO THRU W-HI"));
        Assert.True(ok, detail);
        Assert.Equal("IN\nEC[EC-RANGE-INVALID               ]",
            stdout.Replace("\r\n", "\n").TrimEnd('\n'));
    }

    /// <summary>⛔ <c>&gt;&gt;TURN</c> IS CONSULTED PER STATEMENT, not once per compilation unit (ISO §7.3.25 —
    /// the directive's effect runs from its point in the source). The EC gate reads the WHEN item's own line, so
    /// an EVALUATE written before the directive raises nothing and one written after it raises — measured in ONE
    /// program, which the sticky status permits only in this order (kb/Work PB401's sibling sweep).</summary>
    [Fact]
    public void TurnIsHonouredPerStatement_NotOncePerUnit()
    {
        var (ok, stdout, detail) = new CobolNetCompiler(2002).CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PBR30.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 W-SUBJ PIC X VALUE "C".
            01 W-LO   PIC X VALUE "M".
            01 W-HI   PIC X VALUE "A".
            PROCEDURE DIVISION.
            MAIN.
                EVALUATE W-SUBJ
                    WHEN W-LO THRU W-HI DISPLAY "IN1"
                    WHEN OTHER          DISPLAY "OUT1"
                END-EVALUATE.
                DISPLAY "OFF[" FUNCTION EXCEPTION-STATUS "]".
            >>TURN EC-RANGE-INVALID CHECKING ON
                EVALUATE W-SUBJ
                    WHEN W-LO THRU W-HI DISPLAY "IN2"
                    WHEN OTHER          DISPLAY "OUT2"
                END-EVALUATE.
                DISPLAY "ON[" FUNCTION EXCEPTION-STATUS "]".
                STOP RUN.
            """);
        Assert.True(ok, detail);
        // The empty-range BEHAVIOUR is rule 2's regardless of checking — the exception is what the directive
        // gates, so both EVALUATEs take WHEN OTHER and only the second leaves a status.
        Assert.Equal("OUT1\nOFF[                               ]\nOUT2\nON[EC-RANGE-INVALID               ]",
            stdout.Replace("\r\n", "\n").TrimEnd('\n'));
    }
}
