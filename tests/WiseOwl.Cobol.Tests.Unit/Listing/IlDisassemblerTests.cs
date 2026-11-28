// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using WiseOwl.Cobol.Listing;
using WiseOwl.Cobol.Compiler.Listing;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Listing;

/// <summary>
/// The one IL decoder of the object-code section (DESIGN-listing.md section 6.9), over hand-written IL whose expected instructions are
/// ECMA-335 partition III's encodings: each opcode's byte (or 0xFE pair), each operand's size and form, a branch target computed from the
/// offset after the instruction (III.1.7.1), a token resolved through the metadata it names.
/// </summary>
public sealed class IlDisassemblerTests
{
    /// <summary>The metadata of this test assembly, whose own method tokens the token operands name.</summary>
    private static readonly (PEReader Pe, MetadataReader Metadata) Self = Open();

    private static (PEReader, MetadataReader) Open()
    {
        var pe = new PEReader(File.OpenRead(typeof(IlDisassemblerTests).Assembly.Location));
        return (pe, pe.GetMetadataReader());
    }

    private static List<(int, string, string?)> Decode(byte[] il, int start = 0, int? end = null) =>
        [.. IlDisassembler.Decode(Self.Metadata, [.. il], start, end ?? il.Length).Select(i => (i.Offset, i.OpCode, i.Operand))];

    [Fact]
    public void EveryOperandForm_DecodesAsPartitionIIIEncodesIt()
    {
        byte[] il =
        [
            0x02,                                   // 0x00 ldarg.0 (III.3.38, no operand)
            0x1F, 0xF6,                             // 0x01 ldc.i4.s -10 (III.3.40, int8)
            0x20, 0x00, 0x01, 0x00, 0x00,           // 0x03 ldc.i4 256 (int32, little-endian)
            0x21, 0x01, 0, 0, 0, 0, 0, 0, 0x80,     // 0x08 ldc.i8 -9223372036854775807 (int64)
            0x22, 0x00, 0x00, 0xC0, 0x3F,           // 0x11 ldc.r4 1.5 (float32)
            0x23, 0, 0, 0, 0, 0, 0, 0x04, 0x40,     // 0x16 ldc.r8 2.5 (float64)
            0x11, 0x07,                             // 0x1F ldloc.s 7 (unsigned int8)
            0xFE, 0x0C, 0x2C, 0x01,                 // 0x21 ldloc 300 (0xFE 0x0C, unsigned int16)
            0xFE, 0x01,                             // 0x25 ceq (two-byte opcode)
            0x2B, 0x02,                             // 0x27 br.s +2 -> 0x2B
            0x38, 0xFB, 0xFF, 0xFF, 0xFF,           // 0x29 br -5 -> 0x29
            0x45, 0x02, 0, 0, 0, 0x01, 0, 0, 0, 0xF6, 0xFF, 0xFF, 0xFF,   // 0x2E switch (2 targets, from 0x3B: +1, -10)
            0x2A,                                   // 0x3B ret
        ];
        Assert.Equal(
        [
            (0x00, "ldarg.0", null),
            (0x01, "ldc.i4.s", "-10"),
            (0x03, "ldc.i4", "256"),
            (0x08, "ldc.i8", "-9223372036854775807"),
            (0x11, "ldc.r4", "1.5"),
            (0x16, "ldc.r8", "2.5"),
            (0x1F, "ldloc.s", "7"),
            (0x21, "ldloc", "300"),
            (0x25, "ceq", null),
            (0x27, "br.s", "IL_002B"),
            (0x29, "br", "IL_0029"),
            (0x2E, "switch", "(IL_003C, IL_0031)"),
            (0x3B, "ret", null),
        ], Decode(il));
    }

    /// <summary>A stretch starts at its own offset: the instructions before it are decoded (to find the boundaries) but not listed, and
    /// none at or after its end is.</summary>
    [Fact]
    public void AStretch_ListsOnlyItsOwnInstructions()
    {
        byte[] il = [0x02, 0x03, 0x58, 0x26, 0x2A];   // ldarg.0, ldarg.1, add, pop, ret
        Assert.Equal([(0x01, "ldarg.1", null), (0x02, "add", null)], Decode(il, 1, 3));
    }

    /// <summary>A method token names the method as <c>Type::Name</c> (ECMA-335's notation), read through the metadata.</summary>
    [Fact]
    public void AToken_NamesTheMemberItIs()
    {
        int token = typeof(IlDisassemblerTests).GetMethod(nameof(AToken_NamesTheMemberItIs))!.MetadataToken;
        byte[] il = [0x28, (byte)token, (byte)(token >> 8), (byte)(token >> 16), (byte)(token >> 24)];   // call <token>
        Assert.Equal([(0, "call", "WiseOwl.Cobol.Tests.Unit.Listing.IlDisassemblerTests::AToken_NamesTheMemberItIs")], Decode(il));
    }

    /// <summary>A byte that begins no instruction is no object code the decoder can name: it fails loudly.</summary>
    [Fact]
    public void AByteThatIsNoInstruction_IsRefused() =>
        Assert.Throws<BadImageFormatException>(() => Decode([0x24]));   // 0x24 is unused in partition III's table
}
