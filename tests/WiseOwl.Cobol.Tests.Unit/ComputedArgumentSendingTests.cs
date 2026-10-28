// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A COMPUTED NUMERIC VALUE IS MARKED AS HAVING NO REPRESENTATION OF ITS OWN (kb/Work PB3969; <c>MoveSending.Computed</c>). ISO
/// §15.4.1 leaves "the characteristics and representation of the returned value" of a numeric function to the implementor, so
/// §14.2.3 GR9's first regime has no image to move "without conversion": <c>ProgramTable.LandArguments</c> lands such a BY
/// CONTENT argument through the formal's own description, which these facts show end to end through the formal's registered
/// landing (<c>BoundaryItem.Land</c>): <c>FUNCTION MAX(W 0)</c>, W = 1.25, reaches a PIC S9V99 formal as 1.25, not as the leading
/// positions of its 38-digit container (+0.00).
/// </summary>
public sealed class ComputedArgumentSendingTests
{
    private static readonly NumProfile S9V99 = new()
    {
        Digits = 3, FractionDigits = 2, Signed = true, SignKind = NumericSign.TrailingOverpunch,
        Truncation = NumericTruncation.DigitCount, ByteForm = NumericByteForm.Zoned,
    };

    /// <summary>An evaluated fixed-point value (a numeric function's returned value) is a computed sender.</summary>
    [Fact]
    public void AnEvaluatedFixedPointValueIsComputed()
    {
        var evaluated = new CobolArg(CobolPassMode.Content, ManagedPointer<Int128>.Cell(125), S9V99 with { Digits = 38 });
        Assert.True(CobolArgAdapt.WithValueSending(evaluated).Sending!.Value.Computed);
    }

    /// <summary>An evaluated binary64 value is a computed sender too.</summary>
    [Fact]
    public void AnEvaluatedBinary64ValueIsComputed()
    {
        var evaluated = new CobolArg(CobolPassMode.Content, ManagedPointer<double>.Cell(1.5), null);
        Assert.True(CobolArgAdapt.WithValueSending(evaluated).Sending!.Value.Computed);
    }

    /// <summary>An SDIDI value is computed (the cell of <c>CobolArgAdapt.SdidiValue</c> is a number at a scale).</summary>
    [Fact]
    public void AnSdidiValueIsComputed() =>
        Assert.True(CobolArgAdapt.SdidiValue(CobolPassMode.Content, CobolDec.From(125, 2), SignEncoding.Ibm).Sending!.Value.Computed);

    /// <summary>A character value (a boolean expression's text, an alphanumeric function) is the representation itself.</summary>
    [Fact]
    public void ACharacterValueIsNotComputed()
    {
        var text = new CobolArg(CobolPassMode.Content, ManagedPointer<string>.Cell("ABC"), null);
        Assert.False(CobolArgAdapt.WithValueSending(text).Sending!.Value.Computed);
    }

    /// <summary>The formal's landing turns the computed cell into a record of the formal's own description: the value 1.25 into
    /// S9V99 is the formal's 125 at scale 2 (the COMPUTE of GR9's second regime), a PIC S9V99 formal's three positions.</summary>
    [Fact]
    public void TheFormalsLandingKeepsTheComputedValueAtTheFormalsDescription()
    {
        var evaluated = CobolArgAdapt.WithValueSending(
            new CobolArg(CobolPassMode.Content, ManagedPointer<Int128>.Cell(125), S9V99 with { Digits = 38 }));
        CobolArg landed = new BoundaryItem(S9V99, Landing: CarrierLanding<long>.Instance).Land(evaluated, checking: false);
        Assert.Equal(125L, Assert.IsType<ManagedPointer<long>>(landed.Carrier).Value);
        Assert.Equal(3, landed.Digits);
        Assert.Equal(2, landed.Scale);
    }
}
