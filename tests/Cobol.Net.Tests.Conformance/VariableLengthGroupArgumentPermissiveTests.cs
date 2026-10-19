// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// kb/Work PB3424 — the <c>--permissive</c> lane for a VARIABLE-LENGTH GROUP (ISO §8.5.1.12.1: a group with a
/// dynamic-length elementary item or dynamic-capacity table subordinate) where a character or numeric operand is
/// wanted: a FUNCTION string argument and a group in a numeric position (a numeric-argument function, an arithmetic
/// operand).
/// <para>
/// Strict conformance refuses both (§15.3 "A variable-length group shall be referenced as an argument to a function
/// only when explicitly permitted in the function definition" — only LENGTH and BYTE-LENGTH are; §8.8.1.1 for the
/// arithmetic operand), and <c>--permissive</c> accepts them with a warning "with the existing coercion". The coercion
/// the compiler chose used to compile clean and abort at run time with the Tier-C island
/// (<c>NotImplementedCobolFeatureException</c>): the group's character image was the one DISPLAY already shows
/// (CONFORMANCE.md A.1 item 57 — members in declaration order, each dynamic member at its CURRENT extent) but the
/// two coercion sites did not ask the reader DISPLAY asks. These tests pin that the function argument and the digit
/// decode see exactly the characters DISPLAY shows, member shape by member shape.
/// </para>
/// </summary>
public sealed class VariableLengthGroupArgumentPermissiveTests
{
    /// <summary>Four groups: a FIXED group (the control, an ordinary alphanumeric argument), a scalar variable-length
    /// group (one dynamic-length member), one holding an OCCURS DEPENDING table beside a dynamic-length member (the
    /// table at its CURRENT count, §13.18.38.4 GR8), and one holding a dynamic-capacity table (every occurrence at
    /// its current capacity, §8.5.1.9.1).</summary>
    private const string StringProgram = """
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3424STR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FG.
          05 FA PIC X(3) VALUE "abc".
          05 FN PIC 9(2) VALUE 7.
       01 PG.
          05 PA PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 PN PIC 9(2).
       01 OG.
          05 OC PIC 9 VALUE 3.
          05 OD PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 OT PIC X OCCURS 1 TO 5 DEPENDING ON OC.
       01 DG.
          05 DA PIC X DYNAMIC LENGTH LIMIT IS 8.
          05 DT PIC 9 OCCURS DYNAMIC CAPACITY IN DCAP INITIALIZED.
       01 WS-X PIC X(30).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "Plain" TO PA
           MOVE 6 TO PN
           MOVE "dyn" TO OD
           MOVE "k" TO OT(1)
           MOVE "l" TO OT(2)
           MOVE "m" TO OT(3)
           MOVE "xy" TO DA
           SET DCAP TO 2
           MOVE 4 TO DT(1)
           MOVE 5 TO DT(2)
           MOVE FUNCTION UPPER-CASE (FG) TO WS-X
           DISPLAY "[" WS-X "]"
           MOVE FUNCTION LOWER-CASE (PG) TO WS-X
           DISPLAY "[" WS-X "]"
           MOVE FUNCTION REVERSE (PG) TO WS-X
           DISPLAY "[" WS-X "]"
           MOVE FUNCTION TRIM (PG) TO WS-X
           DISPLAY "[" WS-X "]"
           MOVE FUNCTION UPPER-CASE (OG) TO WS-X
           DISPLAY "[" WS-X "]"
           MOVE FUNCTION UPPER-CASE (DG) TO WS-X
           DISPLAY "[" WS-X "]"
           DISPLAY PG
           DISPLAY OG
           DISPLAY DG
           STOP RUN.
       """;

    /// <summary>The numeric position: a numeric-argument function (ABS), a string-argument function with a numeric
    /// result (NUMVAL), and the two arithmetic verbs that decode a group operand's digit characters (§8.8.1.1 bars the
    /// operand; COBOLNET0844 is the warning).</summary>
    private const string NumericProgram = """
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3424NUM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PG.
          05 PA PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 PN PIC 9(2).
       01 WS-N PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "12" TO PA
           MOVE 6 TO PN
           MOVE FUNCTION ABS (PG) TO WS-N
           DISPLAY WS-N
           MOVE FUNCTION NUMVAL (PG) TO WS-N
           DISPLAY WS-N
           COMPUTE WS-N = PG + 1
           DISPLAY WS-N
           ADD PG TO WS-N
           DISPLAY WS-N
           STOP RUN.
       """;

    /// <summary>A STRONGLY-TYPED variable-length group holding an object-reference leaf: the one-way transfer
    /// capability (<c>DataItem.CurrentExtentTransferImageCapable</c>, kb/Work PB3358) admits it, its reference leaf
    /// reading as the reserved placeholder (CONFORMANCE.md A.1 item 214).</summary>
    private const string StrongProgram = """
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3424STG.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB3424C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TG IS TYPEDEF STRONG.
          05 TA PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 TR USAGE OBJECT REFERENCE PB3424C.
          05 TN PIC 9(2).
       01 WS-G TYPE TG.
       01 WS-X PIC X(30).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "hello" TO TA OF WS-G
           MOVE 5 TO TN OF WS-G
           DISPLAY "[" WS-G "]"
           MOVE FUNCTION UPPER-CASE (WS-G) TO WS-X
           DISPLAY "[" WS-X "]"
           STOP RUN.
       END PROGRAM PB3424STG.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB3424C.
       OBJECT.
       END OBJECT.
       END CLASS PB3424C.
       """;

    private static string[] Lines(string stdout) => stdout.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');

    /// <summary>Strict conformance refuses the argument (§15.3) and the arithmetic operand (§8.8.1.1); the refusal
    /// is what makes the permissive coercion an extension.</summary>
    [Theory]
    [InlineData(nameof(StringProgram), "COBOLNET1627")]
    [InlineData(nameof(NumericProgram), "COBOLNET1627")]
    [InlineData(nameof(NumericProgram), "COBOLNET0844")]
    [InlineData(nameof(StrongProgram), "COBOLNET1627")]
    public void Strict_RefusesTheVariableLengthGroupOperand(string which, string code)
    {
        string src = which switch { nameof(StringProgram) => StringProgram, nameof(NumericProgram) => NumericProgram, _ => StrongProgram };
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2023);
        Assert.False(ok, "a variable-length group is not an admissible function argument or arithmetic operand");
        EditionHarness.AssertHasDiagnostic(errors, code);
    }

    /// <summary>The string position: each function sees the group's CURRENT-extent image, the characters
    /// <c>DISPLAY</c> shows on the last three lines. <c>FG</c> is the fixed-group control (a plain alphanumeric
    /// argument), so the variable-length arms are held to the same behavior. Expected values are the A.1-57 image law
    /// applied by hand: PG = "Plain" + "06"; OG = "3" + "dyn" + the three occurrences "klm" of the OCCURS DEPENDING
    /// table at its current count 3; DG = "xy" + the two occurrences "4" "5" at the current capacity 2.</summary>
    [Fact]
    public void FunctionStringArgument_IsTheCurrentExtentImageDisplayShows()
    {
        var (ok, _, warnings) = EditionHarness.CompileFull(StringProgram, 2023, permissive: true);
        Assert.True(ok, "--permissive must accept the coercion");
        EditionHarness.AssertHasDiagnostic(warnings, "COBOLNET1627");

        var (ranOk, stdout, detail) = EditionHarness.CompileAndRun(StringProgram, 2023, permissive: true);
        Assert.True(ranOk, detail);
        Assert.Equal(
            new[]
            {
                "[ABC07                         ]",   // the fixed-group control
                "[plain06                       ]",   // LOWER-CASE over PA "Plain" + PN 06
                "[60nialP                       ]",   // REVERSE over the same image
                "[Plain06                       ]",   // TRIM over the same image (no leading or trailing space)
                "[3DYNKLM                       ]",   // UPPER-CASE over OC + OD + the table at its count 3
                "[XY45                          ]",   // UPPER-CASE over DA + the table at capacity 2
                "Plain06",                            // DISPLAY PG: the image the arguments above were cut from
                "3dynklm",
                "xy45",
            },
            Lines(stdout));
    }

    /// <summary>The numeric position decodes the SAME image as an unsigned integer (PG = "12" + "06" = 1206), so
    /// ABS and NUMVAL return 1206, and the arithmetic verbs add to it: 1206 + 1 = 1207, then 1207 + 1206 = 2413.</summary>
    [Fact]
    public void NumericPosition_DecodesTheCurrentExtentImageAsDigits()
    {
        var (ok, _, warnings) = EditionHarness.CompileFull(NumericProgram, 2023, permissive: true);
        Assert.True(ok, "--permissive must accept the coercion");
        EditionHarness.AssertHasDiagnostic(warnings, "COBOLNET1627");
        EditionHarness.AssertHasDiagnostic(warnings, "COBOLNET0844");

        var (ranOk, stdout, detail) = EditionHarness.CompileAndRun(NumericProgram, 2023, permissive: true);
        Assert.True(ranOk, detail);
        Assert.Equal(new[] { "1206", "1206", "1207", "2413" }, Lines(stdout));
    }

    /// <summary>A strongly-typed variable-length group with an object-reference leaf: the argument is the image
    /// DISPLAY shows (the reference leaf its reserved placeholder), upper-cased.</summary>
    [Fact]
    public void StronglyTypedGroupWithAReferenceLeaf_IsTheImageDisplayShows()
    {
        var (ranOk, stdout, detail) = EditionHarness.CompileAndRun(StrongProgram, 2023, permissive: true);
        Assert.True(ranOk, detail);
        string[] lines = Lines(stdout);
        Assert.Equal(2, lines.Length);
        Assert.StartsWith("[hello", lines[0], StringComparison.Ordinal);
        Assert.EndsWith("05]", lines[0], StringComparison.Ordinal);
        // The function line is the display image upper-cased and space-padded into WS-X (30 positions).
        Assert.Equal(lines[0].TrimEnd(']').ToUpperInvariant(), lines[1].TrimEnd(']').TrimEnd());
    }
}
