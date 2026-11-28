// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Compiler;
using WiseOwl.Cobol.Editions;
using Xunit;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// kb/Work PB5835: at --std 85 a relation between numeric operands compares ALGEBRAIC VALUES, never binary64 images.
/// §1985:VI.6.3.1.1.1 says "a comparison is made with respect to the algebraic value of the operands"
/// (cite.py --check → OK); the native-arithmetic comparison rule is a 2002 text (§2002:8.8.4.1.1.3), so a float
/// operand at 85 lifts to its exact value. FUNCTION SQRT (4) is exactly 2, so it is LESS than 2.00000000000000001 and
/// not EQUAL to it, and FUNCTION ATAN of 999999999999999999 (binary64 1.5707963267948966, below the literal's
/// 1.57079632679489662 in exact value) is LESS than it (§1989:A.2.8.4 RV1 puts the value inside the range).
/// A leg fails if the relation is evaluated in binary floating point, which reads the 18-digit literal as 2.0 or as
/// the double nearest 1.57079632679489662. The 1985 catalog holds no row for this unnumbered rule, so the witness
/// is this test rather than a conformance-suite case (a case must cite a row).
/// </summary>
public sealed class NumericRelationAlgebraicValueAt85Tests
{
    [Fact]
    public void FunctionVersusEighteenDigitLiteral_ComparesAlgebraicValues()
    {
        string dir = ScratchDirectory.Create("rel85");
        try
        {
            string src = CompiledProgramCache.StageSource(Path.Combine(dir, "PB5835REL.cob"), """
                IDENTIFICATION DIVISION.
                PROGRAM-ID. PB5835REL.
                DATA DIVISION.
                WORKING-STORAGE SECTION.
                01  HUGE  PIC 9(18) VALUE 999999999999999999.
                PROCEDURE DIVISION.
                P1.
                    IF FUNCTION ATAN (HUGE) < 1.57079632679489662
                        DISPLAY "A1 LT"
                    ELSE
                        DISPLAY "A1 NOT LT".
                    IF FUNCTION SQRT (4) < 2.00000000000000001
                        DISPLAY "S1 LT"
                    ELSE
                        DISPLAY "S1 NOT LT".
                    IF FUNCTION SQRT (4) = 2.00000000000000001
                        DISPLAY "S2 EQ"
                    ELSE
                        DISPLAY "S2 NOT EQ".
                    STOP RUN.
                """);
            string dll = Path.Combine(dir, "PB5835REL.dll");
            var compiled = CompiledProgramCache.Compile(new CompilerDriver.Options(src, EditionInfo.Of(85), dll, SourceFormat: TestSourceFormat.Inline));
            Assert.True(compiled.Success, string.Join("\n", compiled.Errors));
            var (ok, stdout, detail) = CutRunner.Run(dll, dir, null);
            Assert.True(ok, detail);
            Assert.Equal("A1 LT\nS1 LT\nS2 NOT EQ", stdout.Replace("\r\n", "\n").TrimEnd('\n'));
        }
        finally { ScratchDirectory.Remove(dir); }
    }
}
