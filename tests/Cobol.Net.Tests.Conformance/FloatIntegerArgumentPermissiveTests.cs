// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The <c>--permissive</c> lane for a FLOATING-POINT operand at an ISO §15.3 type-6 (integer) argument
/// position — the coercion extension the strict screen names, and the home of everything kb/Work PB2 and PB21
/// proved once their programs stopped being conforming under strict.
/// </summary>
/// <remarks>
/// <para>
/// ⛔ WHY THESE PROGRAMS MOVED (kb/Work PB248). <c>tests/conformance/2023/pb21_float_arg_integer_family</c> was
/// a STRICT positive golden whose header derived its legality as "a COMP-2 item is category numeric hence class
/// numeric, and §15.3's INTEGER type resolves through CLASS numeric — the integer-ness is a VALUE property, not
/// a class the argument screen can reject on". That premise contradicts the landed PB40 reading of the SAME
/// clause, which rejects a <c>PIC 9V9</c> operand at an integer position for exactly the reason it would admit a
/// COMP-2 one; the two could not both be right. Re-derived, §15.3 type 6 admits "an arithmetic expression that
/// will always result in an integer value <b>or</b> an integer data item" (--check verified) and a floating-point
/// item is neither — §14.6.8.3 sets its content to "the algebraic value of the sending operand", so its DECLARED
/// value set contains non-integers whatever a given reference holds. Strict now rejects (COBOLNET1627), and the
/// four <c>negative/pb248-*</c> fixtures pin that.
/// </para>
/// <para>
/// ⚠ WHAT THEY REALLY PROVED IS PRESERVED HERE, WHICH IS THE POINT OF MOVING THEM RATHER THAN DELETING THEM.
/// PB21's subject was a CRASH — a float operand at an integer position emitted a call to a runtime member that
/// DOES NOT EXIST and failed Roslyn with a raw CS0117 — and PB2's was the sibling CS1503 from handing a
/// <c>double</c> to an <c>Int128</c> parameter. Both paths are still REACHED under <c>--permissive</c>, so both
/// crashes are still live risks and still need a guard; only the strict verdict changed. The invariant each
/// asserted — a function's value must not depend on how its argument happened to be stored — is asserted here as
/// the float lane agreeing with the fixed-point lane value for value.
/// </para>
/// </remarks>
public sealed class FloatIntegerArgumentPermissiveTests
{
    /// <summary>kb/Work PB21's program, verbatim in its permissive home: six §15.3 type-6 functions over COMP-2
    /// operands. The expected values are the fixed-point ones — that IS the invariant.</summary>
    private const string Pb21Program = """
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB21FLOATPERMISSIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D-DAY  COMP-2 VALUE 1995046.
       01 D-DATE COMP-2 VALUE 19950215.
       01 D-INT  COMP-2 VALUE 143951.
       01 D-YY   COMP-2 VALUE 95.
       01 D-FAC  COMP-2 VALUE 5.
       01 R      PIC 9(9).
       01 F      PIC 9(12).
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION INTEGER-OF-DAY(D-DAY)
           DISPLAY "IODAY=" R
           COMPUTE R = FUNCTION INTEGER-OF-DATE(D-DATE)
           DISPLAY "IODATE=" R
           COMPUTE R = FUNCTION DAY-OF-INTEGER(D-INT)
           DISPLAY "DAYOFINT=" R
           COMPUTE R = FUNCTION DATE-OF-INTEGER(D-INT)
           DISPLAY "DATEOFINT=" R
           COMPUTE R = FUNCTION YEAR-TO-YYYY(D-YY, 50, 2000)
           DISPLAY "Y2YYYY=" R
           COMPUTE F = FUNCTION FACTORIAL(D-FAC)
           DISPLAY "FACT=" F
           COMPUTE R = FUNCTION INTEGER-OF-DAY(1995046)
           DISPLAY "IODAY-FIXED=" R
           STOP RUN.
       """;

    /// <summary>The MOD pair kb/Work PB2 lost to the strict screen — §15.64.3 r1 is "Argument-1 and argument-2
    /// shall be integers", so the FLOAT operands live here while the corpus golden keeps the integer ones.
    /// §15.64.4's equivalent arithmetic expression makes MOD FLOORED, so the result takes the sign of
    /// argument-2: MOD(−7, 3) = 2. The float lane must produce the same 2.</summary>
    private const string Pb2ModProgram = """
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2MODFLOATPERMISSIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A    USAGE COMP-2 VALUE -7.0.
       01 B    USAGE COMP-2 VALUE 3.0.
       01 R    USAGE COMP-2.
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION MOD(A B)
           DISPLAY R
           STOP RUN.
       """;

    /// <summary>Strict REJECTS both, with the §15.3 type-6 diagnostic — the half the negative corpus pins,
    /// asserted here too so the pair of verdicts is readable in one place.</summary>
    [Theory]
    [InlineData(nameof(Pb21Program))]
    [InlineData(nameof(Pb2ModProgram))]
    public void FloatAtAnIntegerPosition_IsRejectedUnderStrict(string which)
    {
        string src = which == nameof(Pb21Program) ? Pb21Program : Pb2ModProgram;
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2023);
        Assert.False(ok, "a floating-point operand at an ISO §15.3 type-6 position is not conforming");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1627");
        EditionHarness.AssertHasDiagnostic(errors, "floating-point numeric data item");
    }

    /// <summary>
    /// <c>--permissive</c> WARNS and computes, and the computed values are the FIXED-POINT ones. This is PB21's
    /// original assertion, unchanged: every value below is what the same call with an integer operand yields, so
    /// a coercion that drifted — or a missing runtime member, which is the CS0117 PB21 was opened for — fails
    /// here. <c>IODAY-FIXED</c> is the in-program control: the same function over a literal integer operand.
    /// </summary>
    [Fact]
    public void Pb21IntegerFamily_UnderPermissive_WarnsAndAgreesWithTheFixedPointPath()
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull(Pb21Program, 2023, permissive: true);
        Assert.True(ok, $"--permissive must accept the coercion: {string.Join("\n", errors)}");
        EditionHarness.AssertHasDiagnostic(warnings, "COBOLNET1627");

        var (ranOk, stdout, detail) = EditionHarness.CompileAndRun(Pb21Program, 2023, permissive: true);
        Assert.True(ranOk, detail);
        Assert.Equal(
            new[]
            {
                "IODAY=000143951",       // §15.45.4 — the integer date of ordinal date 1995046
                "IODATE=000143951",      // §15.46.4 — the same day reached through the calendar date 19950215
                "DAYOFINT=001995046",    // §15.24.4 — the inverse of INTEGER-OF-DAY
                "DATEOFINT=019950215",   // §15.22.4 — the inverse of INTEGER-OF-DATE
                "Y2YYYY=000001995",      // §15.100.4 — 95 windowed on base 2000 − 50 = 1950 → 1995
                "FACT=000000000120",     // §15.36.4 — 5! = 120
                "IODAY-FIXED=000143951", // the FIXED-POINT control: the float lane must equal it
            },
            stdout.Replace("\r\n", "\n").TrimEnd('\n').Split('\n'));
    }

    /// <summary>MOD's float pair under permissive: §15.64.4's equivalent arithmetic expression is floored, so
    /// MOD(−7, 3) = 2 — the same value the corpus golden now computes from integer operands.</summary>
    [Fact]
    public void Pb2Mod_UnderPermissive_IsFlooredJustAsTheFixedPointPathIs()
    {
        var (ok, _, warnings) = EditionHarness.CompileFull(Pb2ModProgram, 2023, permissive: true);
        Assert.True(ok, "--permissive must accept the coercion");
        EditionHarness.AssertHasDiagnostic(warnings, "COBOLNET1627");

        var (ranOk, stdout, detail) = EditionHarness.CompileAndRun(Pb2ModProgram, 2023, permissive: true);
        Assert.True(ranOk, detail);
        Assert.Equal("2", stdout.Replace("\r\n", "\n").TrimEnd('\n'));
    }
}
