// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Frontend.Parsing;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// kb/Work PB3981 — <see cref="ZeroSignReading"/>, the parse-informed second round of the question PB3962 answered
/// from characters: a figurative ZERO followed by a sign touching its literal (<c>ZERO -1</c>) is an operand-list item,
/// unless the grammar wants an arithmetic expression there, which the parse shows by failing at the sign (§8.7.1 then
/// refuses the operator by name, as it refuses <c>Y -1</c>). The end-to-end verdicts are the conformance cases
/// <c>pb3981-*</c>; these pin the round's own protocol: flip on a sign offender, take the flip back when the arithmetic
/// reading fails at the same place, and never try a refuted ZERO twice (the loop must end).
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
    [InlineData("COMPUTE N = M -1")]            // a sign after an identifier is no ZERO's
    [InlineData("DISPLAY ZERO N")]              // an offender that is no sign
    public void AnOffenderThatIsNoSignAfterAZero_ChangesNothing(string src)
    {
        var stream = Rewritten(src);
        var reading = new ZeroSignReading(stream);
        var offender = stream.GetTokens().First(t => t.Type is CobolLexer.MINUS or CobolLexer.IDENTIFIER);

        Assert.False(reading.Revise([offender]));
        Assert.False(HasZeroArith(stream));
    }
}
