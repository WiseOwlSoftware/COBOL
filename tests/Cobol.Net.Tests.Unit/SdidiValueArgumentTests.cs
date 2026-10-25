// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ AN SDIDI VALUE CROSSES A CALL AT ITS OWN EXPONENT (kb/Work PB3677; <c>CobolArgAdapt.SdidiValue</c>). ISO §14.2.3 GR9 moves
/// the argument "without conversion" and fills a numeric formal by "a COMPUTE statement without the ROUNDED phrase", so no
/// scale the activating element chose may cut the value before the formal does. The expected values are the VALUE's: its
/// text is the text a MOVE of the function's value sends (<c>CobolDec.ToFunctionText</c>, the sign not moved, GR6 a)), and
/// its group digits the owner's D-B4 determination (<c>CobolNum.GroupDigits</c>).
/// </summary>
public sealed class SdidiValueArgumentTests
{
    private static ManagedPointer<Int128> Cell(CobolArg arg) => Assert.IsType<ManagedPointer<Int128>>(arg.Carrier);

    /// <summary>0.0001234567890123 keeps all 16 fraction digits (the 6-digit floor cut it to 0.000123).</summary>
    [Fact]
    public void AFractionKeepsEveryFractionDigitOfItsValue()
    {
        CobolArg arg = CobolArgAdapt.SdidiValue(CobolPassMode.Content, CobolDec.From(1234567890123, 16), SignEncoding.Ibm);
        Assert.Equal((Int128)1234567890123, Cell(arg).Value);
        Assert.Equal(16, arg.Scale);
        Assert.Equal("0.0001234567890123", arg.Sending!.Value.Characters);
        Assert.Equal("00001234567890123", arg.Sending.Value.StorageImage);
        Assert.Equal((Int128)1234567890123, arg.Sending.Value.Unscaled);
        Assert.Equal(16, arg.Sending.Value.Scale);
    }

    /// <summary>The value 2 is "2", never 2.000000 (the floor sent 2000000).</summary>
    [Fact]
    public void AnIntegerValueCarriesNoInventedFractionDigits()
    {
        CobolArg arg = CobolArgAdapt.SdidiValue(CobolPassMode.Content, CobolDec.From(2, 0), SignEncoding.Ibm);
        Assert.Equal((Int128)2, Cell(arg).Value);
        Assert.Equal(0, arg.Scale);
        Assert.Equal("2", arg.Sending!.Value.Characters);
        Assert.Equal("2", arg.Sending.Value.StorageImage);
    }

    /// <summary>The sign rides the cell (the formal's COMPUTE needs it) and not the sending characters (GR6 a)).</summary>
    [Fact]
    public void ANegativeValueKeepsItsSignInTheCellAndNotInItsSendingCharacters()
    {
        CobolArg arg = CobolArgAdapt.SdidiValue(CobolPassMode.Content, CobolDec.From(-15, 1), SignEncoding.Ibm);
        Assert.Equal((Int128)(-15), Cell(arg).Value);
        Assert.Equal("1.5", arg.Sending!.Value.Characters);
        Assert.Equal("15", arg.Sending.Value.StorageImage);
    }

    /// <summary>A positive exponent (1E+3) is the integer 1000 at scale 0.</summary>
    [Fact]
    public void APositiveExponentCrossesAsItsInteger()
    {
        CobolArg arg = CobolArgAdapt.SdidiValue(CobolPassMode.Content, new CobolDec(1, 3), SignEncoding.Ibm);
        Assert.Equal((Int128)1000, Cell(arg).Value);
        Assert.Equal(0, arg.Scale);
        Assert.Equal("1000", arg.Sending!.Value.Characters);
    }

    /// <summary>Past the carrier's 38 fraction digits the CELL truncates and the SENDING characters stay exact, so a
    /// character formal never shows the cap (1.2345678901234567E-28 has 45 fraction digits).</summary>
    [Fact]
    public void AValueBeyondTheCellsFractionCapKeepsItsCharactersExact()
    {
        CobolArg arg = CobolArgAdapt.SdidiValue(CobolPassMode.Content, CobolDec.From(12345678901234567, 45), SignEncoding.Ibm);
        Assert.Equal(38, arg.Scale);
        Assert.Equal((Int128)1234567890, Cell(arg).Value);
        Assert.Equal("0." + new string('0', 28) + "12345678901234567", arg.Sending!.Value.Characters);
        Assert.Equal("0" + new string('0', 28) + "12345678901234567", arg.Sending.Value.StorageImage);
    }

    /// <summary>A magnitude the Int128 carrier cannot hold is the §14.7.5 case-5 size error of every intermediate landing.</summary>
    [Fact]
    public void AMagnitudePastTheCarrierRaisesTheIntermediateSizeError() =>
        Assert.Throws<CobolSizeError>(() => CobolArgAdapt.SdidiValue(CobolPassMode.Content, new CobolDec(1, 40), SignEncoding.Ibm));

    /// <summary>The pass mode and the sign encoding ride the argument; the cell's description is a signed DISPLAY item of
    /// the carrier's 38 digits at the value's scale.</summary>
    [Theory]
    [InlineData(CobolPassMode.Content)]
    [InlineData(CobolPassMode.Value)]
    public void TheModeAndTheDescriptionRideTheArgument(CobolPassMode mode)
    {
        CobolArg arg = CobolArgAdapt.SdidiValue(mode, CobolDec.From(325, 2), SignEncoding.Ascii);
        Assert.Equal(mode, arg.Mode);
        NumProfile profile = arg.Num!.Value;
        Assert.Equal(38, profile.Digits);
        Assert.Equal(2, profile.FractionDigits);
        Assert.True(profile.Signed);
        Assert.Equal(SignEncoding.Ascii, profile.SignEncoding);
    }

    /// <summary>The call site's read-back (<c>WithValueSending</c>) keeps a sending operand the value already stated.</summary>
    [Fact]
    public void TheReadBackKeepsTheSendingOperandTheValueStated()
    {
        CobolArg arg = CobolArgAdapt.SdidiValue(CobolPassMode.Content, CobolDec.From(12345678901234567, 45), SignEncoding.Ibm);
        Assert.Equal(arg.Sending, CobolArgAdapt.WithValueSending(arg).Sending);
    }
}
