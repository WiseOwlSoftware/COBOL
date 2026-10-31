// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Frontend.Parsing;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// kb/Work PB3981, PB4120 — <see cref="ZeroSignReading"/>, the parse-informed second round of the question the token
/// rewriter answers from characters: is this figurative ZERO an arithmetic operand? A ZERO followed by a sign touching its
/// literal (<c>ZERO -1</c>) is an operand-list item, unless the grammar wants an arithmetic expression there, which the
/// parse shows by failing at the sign (§8.7.1 then refuses the operator by name, as it refuses <c>Y -1</c>); and a BARE
/// ZERO in a slot that takes only an arithmetic expression (ISO §8.8.1.1: "the figurative constant ZERO (ZEROS,
/// ZEROES)") is refused as a figurative, which the parse shows by failing at the ZERO. The end-to-end verdicts are the
/// conformance cases <c>pb3981-*</c> and <c>pb4120-*</c> and the slot census below; the rest pin the round's own
/// protocol: flip on a sign offender or a ZERO offender, take the flip back when the arithmetic reading fails at the
/// same place, and never try a refuted ZERO twice (the loop must end).
/// </summary>
public sealed class ZeroSignReadingTests
{
    private static CommonTokenStream Rewritten(string src)
    {
        var stream = new CommonTokenStream(new CobolLexer(new AntlrInputStream(src)));
        ZeroTokenRewriter.Rewrite(stream);
        return stream;
    }

    private static IToken Find(CommonTokenStream stream, params int[] types) =>
        stream.GetTokens().First(t => types.Contains(t.Type));

    private static bool HasZeroArith(CommonTokenStream stream) =>
        stream.GetTokens().Any(t => t.Type == CobolParserCore.ZERO_ARITH);

    [Theory]
    [InlineData("COMPUTE N = ZERO -1", CobolLexer.MINUS)]
    [InlineData("COMPUTE N = ZEROS +1", CobolLexer.PLUS)]
    [InlineData("IF N = ZEROES -2.5 DISPLAY N END-IF", CobolLexer.MINUS)]
    public void ASignOffenderRightAfterAFigurativeZero_MakesTheZeroArithmetic(string src, int signType)
    {
        var stream = Rewritten(src);
        Assert.False(HasZeroArith(stream));   // the operand-list reading is the first answer
        var reading = new ZeroSignReading(stream);

        Assert.True(reading.Revise([Find(stream, signType)]));

        Assert.True(HasZeroArith(stream));
    }

    [Fact]
    public void AnArithmeticReadingThatFailsAtTheZeroItself_IsTakenBackAndNeverTriedAgain()
    {
        var stream = Rewritten("MOVE ZERO -1 TO N");
        var reading = new ZeroSignReading(stream);
        var sign = Find(stream, CobolLexer.MINUS);

        Assert.True(reading.Revise([sign]));
        Assert.True(HasZeroArith(stream));

        // The next pass fails at the ZERO_ARITH (no reading makes MOVE ZERO -1 legal): the hypothesis is refuted.
        Assert.True(reading.Revise([Find(stream, CobolParserCore.ZERO_ARITH)]));
        Assert.False(HasZeroArith(stream));

        // The restored pass fails at the sign again, as it did first; the round must now end.
        Assert.False(reading.Revise([sign]));
        Assert.False(HasZeroArith(stream));
    }

    [Fact]
    public void AnArithmeticReadingThatParses_IsKept()
    {
        var stream = Rewritten("COMPUTE N = ZERO -1");
        var reading = new ZeroSignReading(stream);
        Assert.True(reading.Revise([Find(stream, CobolLexer.MINUS)]));

        // The next pass reports nothing near the ZERO or its sign: nothing changes and the loop ends.
        Assert.False(reading.Revise([]));
        Assert.True(HasZeroArith(stream));
    }

    [Theory]
    [InlineData("SET IX UP BY ZERO.")]
    [InlineData("SET IX TO ZEROS.")]
    [InlineData("CONTINUE AFTER ZEROES SECONDS.")]
    public void AFigurativeZeroOffender_IsMadeArithmetic(string src)
    {
        var stream = Rewritten(src);
        Assert.False(HasZeroArith(stream));   // no neighbour shows the arithmetic expression
        var reading = new ZeroSignReading(stream);

        Assert.True(reading.Revise([Find(stream, CobolLexer.ZERO, CobolLexer.ZEROS, CobolLexer.ZEROES)]));

        Assert.True(HasZeroArith(stream));
    }

    [Fact]
    public void AnArithmeticReadingOfABareZeroThatFailsAtTheZero_IsTakenBackAndNeverTriedAgain()
    {
        var stream = Rewritten("MOVE X TO ZERO.");
        var reading = new ZeroSignReading(stream);
        var zero = Find(stream, CobolLexer.ZERO);

        Assert.True(reading.Revise([zero]));
        Assert.True(HasZeroArith(stream));

        // The next pass fails at the ZERO_ARITH too (no reading makes a ZERO a receiving operand): refuted.
        Assert.True(reading.Revise([Find(stream, CobolParserCore.ZERO_ARITH)]));
        Assert.False(HasZeroArith(stream));

        // The restored pass fails at the figurative ZERO again, as it did first; the round must now end.
        Assert.False(reading.Revise([zero]));
        Assert.False(HasZeroArith(stream));
    }

    [Fact]
    public void ABareZeroReadingThatFailsOnlyAtTheNextToken_IsKept()
    {
        // The ZERO was taken; a fault at the token after it lies further on and says nothing about the ZERO.
        var stream = Rewritten("SET IX UP BY ZERO FOO.");
        var reading = new ZeroSignReading(stream);
        Assert.True(reading.Revise([Find(stream, CobolLexer.ZERO)]));

        Assert.False(reading.Revise([stream.GetTokens().First(t => t.Text == "FOO")]));
        Assert.True(HasZeroArith(stream));
    }

    [Theory]
    [InlineData("COMPUTE N = M -1")]            // a sign after an identifier is no ZERO's
    [InlineData("DISPLAY ZERO N")]              // an offender that is neither a ZERO nor a sign
    public void AnOffenderThatIsNoZeroOrSignAfterAZero_ChangesNothing(string src)
    {
        var stream = Rewritten(src);
        var reading = new ZeroSignReading(stream);
        var offender = stream.GetTokens().First(t => t.Type is CobolLexer.MINUS or CobolLexer.IDENTIFIER);

        Assert.False(reading.Revise([offender]));
        Assert.False(HasZeroArith(stream));
    }

    private const string SlotProgram = "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. PB4120U.\n"
        + "       ENVIRONMENT DIVISION.\n       INPUT-OUTPUT SECTION.\n       FILE-CONTROL.\n"
        + "           SELECT F ASSIGN TO \"pb4120u.dat\" ORGANIZATION INDEXED\n"
        + "               ACCESS DYNAMIC RECORD KEY K.\n"
        + "       DATA DIVISION.\n       FILE SECTION.\n       FD F.\n       01 R.\n          05 K PIC X(4).\n"
        + "       WORKING-STORAGE SECTION.\n       01 W-P USAGE POINTER.\n"
        + "       01 W-B PIC 1(8) USAGE BIT VALUE B\"10100000\".\n       01 W-C PIC 1(8) USAGE BIT.\n"
        + "       01 TB.\n          05 TE PIC X OCCURS 4 INDEXED BY IX.\n"
        + "       PROCEDURE DIVISION.\n";

    /// <summary>⛔ THE SLOTS THAT TAKE ONLY AN ARITHMETIC EXPRESSION TAKE A BARE ZERO (kb/Work PB4120; PB4172 named the same
    /// slots). ISO §8.8.1.1: "An arithmetic expression may be [...] the figurative constant ZERO (ZEROS, ZEROES)", and each
    /// position below is an arithmetic expression: SET Formats 1 and 2 (§14.9.39.2), CONTINUE AFTER (§14.9.9.2), ALLOCATE
    /// (§14.9.3.2), and the RETRY phrase, START's LENGTH phrase and the B-SHIFT operand, which the grammar types
    /// arithmeticExpression alone. No neighbour of the ZERO shows it, so these parsed only once the parse itself was asked;
    /// a slot added tomorrow is covered the same way.</summary>
    [Theory]
    [InlineData("SET IX TO ZERO.")]
    [InlineData("SET IX UP BY ZEROS.")]
    [InlineData("SET IX DOWN BY ZEROES.")]
    [InlineData("CONTINUE AFTER ZERO SECONDS.")]
    [InlineData("ALLOCATE ZERO CHARACTERS RETURNING W-P.")]
    [InlineData("READ F RETRY ZERO TIMES.")]
    [InlineData("START F KEY = K WITH LENGTH ZERO.")]
    [InlineData("COMPUTE W-C = W-B B-SHIFT-L ZERO.")]
    public void ABareZeroInAnArithmeticExpressionOnlySlot_Compiles(string statement)
    {
        string dir = ScratchDirectory.Create("WiseOwlCobol_PB4120_");
        try
        {
            string src = Path.Combine(dir, "pb4120u.cob");
            File.WriteAllText(src, SlotProgram + "           " + statement + "\n           STOP RUN.\n");
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "pb4120u.dll"), DialectLevel: 2023, CheckOnly: true,
                SourceFormat: InitialReferenceFormat.Fixed));
            Assert.True(r.Success, $"{statement} is legal COBOL-2023; got:\n{string.Join("\n", r.Errors)}");
        }
        finally { ScratchDirectory.Remove(dir); }
    }
}
