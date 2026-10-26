// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// The callee half of the second alphabet a BY CONTENT argument states when the activating element cannot see the formal
/// (kb/Work PB3587; <see cref="MoveSending.StorageImage"/>). ISO §14.9.25.4 GR4 makes the record of a plain-group formal
/// the result of a move with "no conversion of data from one form of internal representation to another", so a USAGE BIT
/// or USAGE NATIONAL sender arrives as its representation; an elementary formal reads the characters. These pin what the
/// end-to-end witness (conformance:cases/14.2-Procedure-division-structure/pb3587_content_group_formal_usage_values)
/// cannot reach one adapter at a time: the group adapter takes the image, the elementary adapter does not, BY REFERENCE
/// never does (no MOVE, §14.2.3 GR8), and the caller's argument array is never written.
/// </summary>
public sealed class MoveSendingStorageImageTests
{
    private const string Characters = "10100101";   // a USAGE BIT item written as its '0'/'1' characters
    private const string Image = "¥";           // its representation: the one packed byte X"A5"

    private static CobolArg[] Argument(CobolPassMode mode, string? image) =>
    [
        new CobolArg(mode, ManagedPointer<string>.Cell(Characters), null) { Sending = new MoveSending(Characters, StorageImage: image) },
    ];

    [Fact]
    public void PlainGroupFormal_ByContent_ReadsTheRepresentation()
    {
        var cell = CobolArgAdapt.Text(Argument(CobolPassMode.Content, Image), 0, 1, global::System.Array.Empty<GroupAtom>(), () => " ");
        Assert.Equal(Image, cell.Value);
    }

    [Fact]
    public void ElementaryFormal_ByContent_ReadsTheCharacters()
    {
        var cell = CobolArgAdapt.Text(Argument(CobolPassMode.Content, Image), 0, 8, null, () => " ");
        Assert.Equal(Characters, cell.Value);
    }

    [Fact]
    public void PlainGroupFormal_ByReference_NeverTakesTheImage()
    {
        // BY REFERENCE has no MOVE (§14.2.3 GR8): the formal occupies the argument's own storage, whatever a Sending says.
        var cell = CobolArgAdapt.Text(Argument(CobolPassMode.Reference, Image), 0, 8, global::System.Array.Empty<GroupAtom>(), () => " ");
        Assert.Equal(Characters, cell.Value);
    }

    [Fact]
    public void PlainGroupFormal_WithoutAStorageImage_ReadsTheCarrier()
    {
        // A sender whose characters ARE its representation (the common case) states none.
        var cell = CobolArgAdapt.Text(Argument(CobolPassMode.Content, null), 0, 8, global::System.Array.Empty<GroupAtom>(), () => " ");
        Assert.Equal(Characters, cell.Value);
    }

    [Fact]
    public void TheCallersArgumentArray_IsNeverWritten()
    {
        var args = Argument(CobolPassMode.Content, Image);
        var carrier = args[0].Carrier;
        _ = CobolArgAdapt.Text(args, 0, 1, global::System.Array.Empty<GroupAtom>(), () => " ");
        Assert.Same(carrier, args[0].Carrier);
        Assert.Equal(Image, args[0].Sending!.Value.StorageImage);
    }

    [Fact]
    public void WithValueSending_StatesTheRepresentationOfAnEvaluatedNationalOrBitValue()
    {
        var arg = new CobolArg(CobolPassMode.Content, ManagedPointer<string>.Cell("ab"), null);
        Assert.Null(CobolArgAdapt.WithValueSending(arg).Sending!.Value.StorageImage);
        var withImage = CobolArgAdapt.WithValueSending(arg, static t => CobolBits.NatBytes(t));
        Assert.Equal(CobolBits.NatBytes("ab"), withImage.Sending!.Value.StorageImage);
        Assert.Equal("ab", withImage.Sending!.Value.Characters);
    }
}
