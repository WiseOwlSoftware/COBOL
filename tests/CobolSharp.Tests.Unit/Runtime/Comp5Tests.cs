using Xunit;
using CobolSharp.Runtime;

namespace CobolSharp.Tests.Unit.Runtime;

public sealed class Comp5Tests
{
    private static PicDescriptor MakePic(
        string picBody,
        bool isSigned = false,
        SignStorageKind signStorage = SignStorageKind.None)
        => PicDescriptorFactory.FromPicBody(picBody, UsageKind.Comp5, isSigned, signStorage);

    private static byte[] NewBuffer(PicDescriptor pic) => new byte[pic.StorageLength];

    // ═══════════════════════════════════════════
    // Storage sizing (identical to COMP)
    // ═══════════════════════════════════════════

    [Theory]
    [InlineData("9(1)", 2)]
    [InlineData("9(4)", 2)]
    [InlineData("9(5)", 4)]
    [InlineData("9(9)", 4)]
    [InlineData("9(10)", 8)]
    [InlineData("9(18)", 8)]
    public void StorageSize_MatchesComp(string picBody, int expectedBytes)
    {
        var pic = MakePic(picBody);
        Assert.Equal(expectedBytes, pic.StorageLength);
    }

    // ═══════════════════════════════════════════
    // Encode → Decode round-trip
    // ═══════════════════════════════════════════

    [Fact]
    public void RoundTrip_Signed_2Byte()
    {
        var pic = MakePic("S9(4)", isSigned: true);
        var buf = NewBuffer(pic);
        PicRuntime.EncodeNumeric(buf, 0, buf.Length, pic, -1234m);
        var result = PicRuntime.DecodeNumeric(buf, 0, buf.Length, pic);
        Assert.Equal(-1234m, result);
    }

    [Fact]
    public void RoundTrip_Unsigned_2Byte()
    {
        var pic = MakePic("9(4)");
        var buf = NewBuffer(pic);
        PicRuntime.EncodeNumeric(buf, 0, buf.Length, pic, 50000m);
        var result = PicRuntime.DecodeNumeric(buf, 0, buf.Length, pic);
        Assert.Equal(50000m, result);
    }

    [Fact]
    public void RoundTrip_Signed_4Byte()
    {
        var pic = MakePic("S9(9)", isSigned: true);
        var buf = NewBuffer(pic);
        PicRuntime.EncodeNumeric(buf, 0, buf.Length, pic, -2000000000m);
        var result = PicRuntime.DecodeNumeric(buf, 0, buf.Length, pic);
        Assert.Equal(-2000000000m, result);
    }

    [Fact]
    public void RoundTrip_Signed_8Byte()
    {
        var pic = MakePic("S9(18)", isSigned: true);
        var buf = NewBuffer(pic);
        PicRuntime.EncodeNumeric(buf, 0, buf.Length, pic, -9876543210m);
        var result = PicRuntime.DecodeNumeric(buf, 0, buf.Length, pic);
        Assert.Equal(-9876543210m, result);
    }

    // ═══════════════════════════════════════════
    // Little-endian byte order verification
    // ═══════════════════════════════════════════

    [Fact]
    public void Encode_LittleEndian_2Byte()
    {
        var pic = MakePic("S9(4)", isSigned: true);
        var buf = NewBuffer(pic);
        PicRuntime.EncodeNumeric(buf, 0, buf.Length, pic, 256m);
        // 256 in little-endian int16 = [0x00, 0x01]
        Assert.Equal(0x00, buf[0]);
        Assert.Equal(0x01, buf[1]);
    }

    [Fact]
    public void Encode_LittleEndian_4Byte()
    {
        var pic = MakePic("S9(9)", isSigned: true);
        var buf = NewBuffer(pic);
        PicRuntime.EncodeNumeric(buf, 0, buf.Length, pic, 1m);
        // 1 in little-endian int32 = [0x01, 0x00, 0x00, 0x00]
        Assert.Equal(0x01, buf[0]);
        Assert.Equal(0x00, buf[1]);
        Assert.Equal(0x00, buf[2]);
        Assert.Equal(0x00, buf[3]);
    }

    // ═══════════════════════════════════════════
    // No PIC-based truncation (full binary range)
    // ═══════════════════════════════════════════

    [Fact]
    public void NoTruncation_ValueExceedsPicDigits_Unsigned()
    {
        // PIC 9(4) COMP-5 = 2 bytes unsigned; can hold 0..65535
        // Standard COMP would truncate 30000 % 10000 = 0
        var pic = MakePic("9(4)");
        var buf = NewBuffer(pic);
        PicRuntime.EncodeNumeric(buf, 0, buf.Length, pic, 30000m);
        var result = PicRuntime.DecodeNumeric(buf, 0, buf.Length, pic);
        Assert.Equal(30000m, result);
    }

    [Fact]
    public void NoTruncation_ValueExceedsPicDigits_Signed()
    {
        // PIC S9(4) COMP-5 = 2 bytes signed; can hold -32768..32767
        // Standard COMP would truncate to -9999..9999
        var pic = MakePic("S9(4)", isSigned: true);
        var buf = NewBuffer(pic);
        PicRuntime.EncodeNumeric(buf, 0, buf.Length, pic, 20000m);
        var result = PicRuntime.DecodeNumeric(buf, 0, buf.Length, pic);
        Assert.Equal(20000m, result);
    }

    // ═══════════════════════════════════════════
    // Full unsigned range
    // ═══════════════════════════════════════════

    [Fact]
    public void FullRange_Unsigned_2Byte()
    {
        var pic = MakePic("9(4)");
        var buf = NewBuffer(pic);
        PicRuntime.EncodeNumeric(buf, 0, buf.Length, pic, 65535m);
        var result = PicRuntime.DecodeNumeric(buf, 0, buf.Length, pic);
        Assert.Equal(65535m, result);
    }

    [Fact]
    public void FullRange_Unsigned_4Byte()
    {
        var pic = MakePic("9(9)");
        var buf = NewBuffer(pic);
        PicRuntime.EncodeNumeric(buf, 0, buf.Length, pic, 4294967295m);
        var result = PicRuntime.DecodeNumeric(buf, 0, buf.Length, pic);
        Assert.Equal(4294967295m, result);
    }

    // ═══════════════════════════════════════════
    // WouldOverflow — binary capacity checks
    // ═══════════════════════════════════════════

    [Fact]
    public void Overflow_Signed_2Byte_InRange()
    {
        // 20000 exceeds PIC 9(4) digit count but is within short.MaxValue
        var pic = MakePic("S9(4)", isSigned: true);
        var buf = NewBuffer(pic);
        var status = new ArithmeticStatus();
        PicRuntime.AddNumericLiteral(buf, 0, buf.Length, pic, 20000m, 0, ref status);
        Assert.False(status.SizeError);
    }

    [Fact]
    public void Overflow_Signed_2Byte_ExceedsBinaryCapacity()
    {
        // 32768 exceeds short.MaxValue → SIZE ERROR
        var pic = MakePic("S9(4)", isSigned: true);
        var buf = NewBuffer(pic);
        var status = new ArithmeticStatus();
        PicRuntime.AddNumericLiteral(buf, 0, buf.Length, pic, 32768m, 0, ref status);
        Assert.True(status.SizeError);
    }

    [Fact]
    public void Overflow_Unsigned_2Byte_InRange()
    {
        // 65535 is within ushort.MaxValue
        var pic = MakePic("9(4)");
        var buf = NewBuffer(pic);
        var status = new ArithmeticStatus();
        PicRuntime.AddNumericLiteral(buf, 0, buf.Length, pic, 65535m, 0, ref status);
        Assert.False(status.SizeError);
    }

    [Fact]
    public void Overflow_Unsigned_2Byte_ExceedsBinaryCapacity()
    {
        // 65536 exceeds ushort.MaxValue → SIZE ERROR
        var pic = MakePic("9(4)");
        var buf = NewBuffer(pic);
        var status = new ArithmeticStatus();
        PicRuntime.AddNumericLiteral(buf, 0, buf.Length, pic, 65536m, 0, ref status);
        Assert.True(status.SizeError);
    }

    [Fact]
    public void Overflow_Signed_4Byte_InRange()
    {
        var pic = MakePic("S9(9)", isSigned: true);
        var buf = NewBuffer(pic);
        var status = new ArithmeticStatus();
        PicRuntime.AddNumericLiteral(buf, 0, buf.Length, pic, 2000000000m, 0, ref status);
        Assert.False(status.SizeError);
    }

    [Fact]
    public void Overflow_Signed_4Byte_ExceedsBinaryCapacity()
    {
        // int.MaxValue + 1 = 2147483648 → SIZE ERROR
        var pic = MakePic("S9(9)", isSigned: true);
        var buf = NewBuffer(pic);
        var status = new ArithmeticStatus();
        PicRuntime.AddNumericLiteral(buf, 0, buf.Length, pic, 2147483648m, 0, ref status);
        Assert.True(status.SizeError);
    }
}
