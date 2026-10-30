// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A FUNCTION'S TYPE IS DECIDED BY ITS ARGUMENTS AS WRITTEN, NEVER BY HOW THEIR EVALUATION ORDER WAS SETTLED
/// (kb/Work PB4036). ISO §15.59.1's result-type table keys the type of <c>FUNCTION MAX</c> on its arguments ("All
/// arguments integer" is Integer, "Numeric (some arguments may be integer)" is Numeric), and §8.4.3.2.4 GR2 only
/// orders their evaluation. A later argument that activates a user-defined function makes the binder hold each
/// earlier argument in a compiler temporary (<c>ArgumentOrder.Window.Settle</c>), and an arithmetic expression held in
/// a scale-0 temporary is an integer DATA ITEM where the expression it replaced was not — so a type re-read from the
/// settled arguments made <c>FUNCTION MAX (A * B, 5)</c> and <c>FUNCTION MAX (A * B, FUNCTION F (A))</c> two different
/// types, and an integer-only position (§14.9.28.3 SR2, §8.4.3.2.3 SR11) accepted the second and refused the first.
/// <para>The assertion is deliberately the AGREEMENT of the two forms, whichever way §15.3 type 6 is read for an
/// always-integral expression: both are accepted, or both are refused with the same code.</para>
/// </summary>
public sealed class FunctionTypeSettlementTests : WiseOwlCobolTestBase
{
    private const string Udf = """
        IDENTIFICATION DIVISION.
        FUNCTION-ID. FTSBUMP.
        DATA DIVISION.
        LINKAGE SECTION.
        01 L-X PIC 9(4).
        01 L-R PIC 9(4).
        PROCEDURE DIVISION USING L-X RETURNING L-R.
        P.
            MOVE L-X TO L-R.
            GOBACK.
        END FUNCTION FTSBUMP.

        """;

    private static string Program(string statement) => Udf + $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. FTSMAIN.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            FUNCTION FTSBUMP
            FUNCTION ALL INTRINSIC.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 WS-A PIC 9(4) VALUE 7.
        01 WS-B PIC 9(4) VALUE 3.
        01 WS-N PIC 9(4) VALUE 0.
        01 WS-I PIC 9(4) VALUE 1.
        PROCEDURE DIVISION.
        MAIN.
            {statement}
            STOP RUN.
        END PROGRAM FTSMAIN.

        """;

    /// <summary>The call written plain, and the same call with a later argument that activates a user-defined function
    /// (so every earlier argument is frozen). Each pair sits in a position that asks the call's type.</summary>
    public static TheoryData<string, string> Pairs() => new()
    {
        // An integer-only position (§14.9.28.3 SR2): PERFORM ... TIMES.
        { "PERFORM FUNCTION MAX (WS-A * WS-B, 5) TIMES CONTINUE END-PERFORM",
          "PERFORM FUNCTION MAX (WS-A * WS-B, FUNCTION FTSBUMP (WS-A)) TIMES CONTINUE END-PERFORM" },
        { "PERFORM FUNCTION MIN (WS-A - WS-B, 5) TIMES CONTINUE END-PERFORM",
          "PERFORM FUNCTION MIN (WS-A - WS-B, FUNCTION FTSBUMP (WS-A)) TIMES CONTINUE END-PERFORM" },
        // A numeric expression (a quotient is not exact): Numeric both ways.
        { "PERFORM FUNCTION MAX (WS-A / WS-B, 5) TIMES CONTINUE END-PERFORM",
          "PERFORM FUNCTION MAX (WS-A / WS-B, FUNCTION FTSBUMP (WS-A)) TIMES CONTINUE END-PERFORM" },
        // A data item argument is an integer in both forms.
        { "PERFORM FUNCTION MAX (WS-A, 5) TIMES CONTINUE END-PERFORM",
          "PERFORM FUNCTION MAX (WS-A, FUNCTION FTSBUMP (WS-A)) TIMES CONTINUE END-PERFORM" },
        // A selection function over an expression, as an unsigned-integer function argument (§8.4.3.2.3 SR11/SR12).
        { "MOVE FUNCTION CHAR (FUNCTION MAX (WS-A * WS-B, 5)) TO WS-N",
          "MOVE FUNCTION CHAR (FUNCTION MAX (WS-A * WS-B, FUNCTION FTSBUMP (WS-A))) TO WS-N" },
    };

    [Theory]
    [MemberData(nameof(Pairs))]
    public void AFrozenArgumentDoesNotChangeTheCallsType(string plain, string frozen)
    {
        var (plainOk, _, plainDetail) = CompileAndRun(Program(plain), dialectLevel: 2023);
        var (frozenOk, _, frozenDetail) = CompileAndRun(Program(frozen), dialectLevel: 2023);

        Assert.Equal(plainOk, frozenOk);
        if (!plainOk)
            Assert.Equal(CodesOf(plainDetail), CodesOf(frozenDetail));
    }

    /// <summary>The diagnostic codes a failed compile reported, in order, with the source-specific text removed.</summary>
    private static string CodesOf(string detail) =>
        string.Join(",", System.Text.RegularExpressions.Regex.Matches(detail, @"COBOL\d{4}").Select(m => m.Value));
}
