using System.Text;
using Xunit;
using CobolSharp.Runtime;

namespace CobolSharp.Tests.Unit.Runtime;

public sealed class PicRuntimeMoveTests
{
    private static PicDescriptor MakePic(
        string picBody,
        UsageKind usage = UsageKind.Display,
        bool isSigned = false,
        SignStorageKind signStorage = SignStorageKind.None,
        bool blankWhenZero = false)
        => PicDescriptorFactory.FromPicBody(picBody, usage, isSigned, signStorage, blankWhenZero);

    private static byte[] NewBuffer(PicDescriptor pic) => new byte[pic.StorageLength];

    private static string GetAscii(byte[] buffer) => Encoding.ASCII.GetString(buffer);

    // ---------- Numeric → Numeric ----------

    [Fact]
    public void Move_NumericToNumeric_BasicAndScaling()
    {
        var srcPic = MakePic("S9(5)V99", isSigned: true, signStorage: SignStorageKind.TrailingOverpunch);
        var dstPic = MakePic("S9(7)", isSigned: true, signStorage: SignStorageKind.TrailingOverpunch);

        var src = NewBuffer(srcPic);
        var dst = NewBuffer(dstPic);

        PicRuntime.EncodeNumeric(src, 0, src.Length, srcPic, 123.45m);

        PicRuntime.MoveNumericToNumeric(
            src, 0, src.Length, srcPic,
            dst, 0, dst.Length, dstPic,
            0);

        var value = PicRuntime.DecodeNumeric(dst, 0, dst.Length, dstPic);
        // 123.45 truncated to integer → 123
        Assert.Equal(123m, value);
    }

    [Fact]
    public void Move_Numeric_TrailingSeparateToOverpunch_RoundTrip()
    {
        var srcPic = MakePic("S9(5)", isSigned: true, signStorage: SignStorageKind.TrailingSeparate);
        var dstPic = MakePic("S9(5)", isSigned: true, signStorage: SignStorageKind.TrailingOverpunch);

        var src = NewBuffer(srcPic);
        var dst = NewBuffer(dstPic);

        PicRuntime.EncodeNumeric(src, 0, src.Length, srcPic, -12345m);

        PicRuntime.MoveNumericToNumeric(
            src, 0, src.Length, srcPic,
            dst, 0, dst.Length, dstPic,
            0);

        var value = PicRuntime.DecodeNumeric(dst, 0, dst.Length, dstPic);
        Assert.Equal(-12345m, value);
    }

    [Fact]
    public void Move_Numeric_OverpunchToLeadingSeparate()
    {
        var srcPic = MakePic("S9(3)", isSigned: true, signStorage: SignStorageKind.TrailingOverpunch);
        var dstPic = MakePic("S9(3)", isSigned: true, signStorage: SignStorageKind.LeadingSeparate);

        var src = NewBuffer(srcPic);
        var dst = NewBuffer(dstPic);

        PicRuntime.EncodeNumeric(src, 0, src.Length, srcPic, -99m);

        PicRuntime.MoveNumericToNumeric(
            src, 0, src.Length, srcPic,
            dst, 0, dst.Length, dstPic,
            0);

        var value = PicRuntime.DecodeNumeric(dst, 0, dst.Length, dstPic);
        Assert.Equal(-99m, value);
    }

    // ---------- Numeric → NumericEdited ----------

    [Fact]
    public void Move_NumericToNumericEdited_ZZ_ZZ9_Family()
    {
        var srcPic = MakePic("9(5)");
        var dstPic = MakePic("ZZ,ZZ9");

        var src = NewBuffer(srcPic);
        var dst = NewBuffer(dstPic);

        PicRuntime.EncodeNumeric(src, 0, src.Length, srcPic, 42m);

        PicRuntime.MoveNumericToNumericEdited(
            src, 0, src.Length, srcPic,
            dst, 0, dst.Length, dstPic,
            0);

        Assert.Equal("    42", GetAscii(dst));
    }

    [Fact]
    public void Move_NumericToNumericEdited_Minus9_9_Negative()
    {
        var srcPic = MakePic("S9(9)V9(9)", isSigned: true, signStorage: SignStorageKind.TrailingOverpunch);
        var dstPic = MakePic("-9(9).9(9)");

        var src = NewBuffer(srcPic);
        var dst = NewBuffer(dstPic);

        PicRuntime.EncodeNumeric(src, 0, src.Length, srcPic, -123.45m);

        PicRuntime.MoveNumericToNumericEdited(
            src, 0, src.Length, srcPic,
            dst, 0, dst.Length, dstPic,
            0);

        var text = GetAscii(dst);
        Assert.Contains("123", text);
        Assert.Contains("-", text);
    }

    [Fact]
    public void Move_NumericToNumericEdited_Minus9_9_Positive()
    {
        var srcPic = MakePic("S9(9)V9(9)", isSigned: true, signStorage: SignStorageKind.TrailingOverpunch);
        var dstPic = MakePic("-9(9).9(9)");

        var src = NewBuffer(srcPic);
        var dst = NewBuffer(dstPic);

        PicRuntime.EncodeNumeric(src, 0, src.Length, srcPic, 123.45m);

        PicRuntime.MoveNumericToNumericEdited(
            src, 0, src.Length, srcPic,
            dst, 0, dst.Length, dstPic,
            0);

        var text = GetAscii(dst);
        Assert.Contains("123", text);
        Assert.DoesNotContain("-", text);
    }

    // ---------- Numeric → Alphanumeric ----------

    [Fact]
    public void Move_NumericToAlphanumeric_StripsSign()
    {
        var srcPic = MakePic("S9(3)", isSigned: true, signStorage: SignStorageKind.TrailingOverpunch);
        var dstPic = MakePic("X(3)");

        var src = NewBuffer(srcPic);
        var dst = NewBuffer(dstPic);

        PicRuntime.EncodeNumeric(src, 0, src.Length, srcPic, -42m);

        PicRuntime.MoveNumericToAlphanumeric(
            src, 0, src.Length, srcPic,
            dst, 0, dst.Length, dstPic,
            0);

        Assert.Equal("042", GetAscii(dst));
    }

    [Fact]
    public void Move_NumericToAlphanumericEdited_Delegates()
    {
        var srcPic = MakePic("9(3)");
        var dstPic = MakePic("X(3)");

        var src = NewBuffer(srcPic);
        var dst = NewBuffer(dstPic);

        PicRuntime.EncodeNumeric(src, 0, src.Length, srcPic, 123m);

        PicRuntime.MoveNumericToAlphanumericEdited(
            src, 0, src.Length, srcPic,
            dst, 0, dst.Length, dstPic,
            0);

        Assert.Equal("123", GetAscii(dst));
    }

    // ---------- Alphanumeric → Numeric / NumericEdited ----------

    [Fact]
    public void Move_AlphanumericToNumeric_ParsesDigits()
    {
        var srcPic = MakePic("X(5)");
        var dstPic = MakePic("S9(5)", isSigned: true, signStorage: SignStorageKind.TrailingOverpunch);

        var src = Encoding.ASCII.GetBytes("  123");
        var dst = NewBuffer(dstPic);

        PicRuntime.MoveAlphanumericToNumeric(
            src, 0, src.Length, srcPic,
            dst, 0, dst.Length, dstPic,
            0);

        var value = PicRuntime.DecodeNumeric(dst, 0, dst.Length, dstPic);
        Assert.Equal(123m, value);
    }

    [Fact]
    public void Move_AlphanumericToNumericEdited_ParseAndFormat()
    {
        var srcPic = MakePic("X(6)");
        var dstPic = MakePic("ZZ,ZZ9");

        var src = Encoding.ASCII.GetBytes(" 1234 ");
        var dst = NewBuffer(dstPic);

        PicRuntime.MoveAlphanumericToNumericEdited(
            src, 0, src.Length, srcPic,
            dst, 0, dst.Length, dstPic,
            0);

        Assert.Equal(" 1,234", GetAscii(dst));
    }

    // ---------- NumericEdited → Numeric / Alphanumeric / NumericEdited ----------

    [Fact]
    public void Move_NumericEditedToNumeric_DeEditRoundTrip()
    {
        var srcPic = MakePic("ZZ,ZZ9");
        var dstPic = MakePic("S9(5)", isSigned: true, signStorage: SignStorageKind.TrailingOverpunch);

        var src = Encoding.ASCII.GetBytes(" 1,234");
        var dst = NewBuffer(dstPic);

        PicRuntime.MoveNumericEditedToNumeric(
            src, 0, src.Length, srcPic,
            dst, 0, dst.Length, dstPic,
            0);

        var value = PicRuntime.DecodeNumeric(dst, 0, dst.Length, dstPic);
        Assert.Equal(1234m, value);
    }

    [Fact]
    public void Move_NumericEditedToAlphanumeric_Explicit()
    {
        var srcPic = MakePic("ZZ,ZZ9");
        var dstPic = MakePic("X(6)");

        var src = Encoding.ASCII.GetBytes("12,345");
        var dst = NewBuffer(dstPic);

        PicRuntime.MoveNumericEditedToAlphanumeric(
            src, 0, src.Length, srcPic,
            dst, 0, dst.Length, dstPic,
            0);

        Assert.Equal("12,345", GetAscii(dst));
    }

    [Fact]
    public void Move_NumericEditedToNumericEdited_Copy()
    {
        var srcPic = MakePic("ZZ,ZZ9");
        var dstPic = MakePic("ZZ,ZZ9");

        var src = Encoding.ASCII.GetBytes("12,345");
        var dst = NewBuffer(dstPic);

        PicRuntime.MoveNumericEditedToNumericEdited(
            src, 0, src.Length, srcPic,
            dst, 0, dst.Length, dstPic,
            0);

        Assert.Equal("12,345", GetAscii(dst));
    }

    // ---------- AlphanumericEdited → Alphanumeric ----------

    [Fact]
    public void Move_AlphanumericToAlphanumeric_JustifyPad()
    {
        var srcPic = MakePic("X(3)");
        var dstPic = MakePic("X(5)");

        var src = Encoding.ASCII.GetBytes("ABC");
        var dst = NewBuffer(dstPic);

        PicRuntime.MoveAlphanumericToAlphanumeric(
            src, 0, src.Length, srcPic,
            dst, 0, dst.Length, dstPic,
            0);

        Assert.Equal("ABC  ", GetAscii(dst));
    }

    // ---------- Figurative / ALL literal ----------

    [Fact]
    public void Move_FigurativeToField_Spaces()
    {
        var dstPic = MakePic("X(5)");
        var dst = NewBuffer(dstPic);

        PicRuntime.MoveFigurativeToField(
            dst, 0, dst.Length, dstPic,
            (int)FigurativeKind.Space);

        Assert.Equal("     ", GetAscii(dst));
    }

    [Fact]
    public void Move_FigurativeToField_Zero_Numeric()
    {
        var dstPic = MakePic("S9(5)", isSigned: true, signStorage: SignStorageKind.TrailingOverpunch);
        var dst = NewBuffer(dstPic);

        PicRuntime.MoveFigurativeToField(
            dst, 0, dst.Length, dstPic,
            (int)FigurativeKind.Zero);

        var value = PicRuntime.DecodeNumeric(dst, 0, dst.Length, dstPic);
        Assert.Equal(0m, value);
    }

    [Fact]
    public void Move_AllLiteralToField_PatternRepeat()
    {
        var dstPic = MakePic("X(7)");
        var dst = NewBuffer(dstPic);

        var pattern = Encoding.ASCII.GetBytes("AB");
        PicRuntime.MoveAllLiteralToField(
            dst, 0, dst.Length, pattern);

        Assert.Equal("ABABABA", GetAscii(dst));
    }

    // ---------- Numeric literal ----------

    [Fact]
    public void Move_NumericLiteralToNumeric_Scaling()
    {
        var dstPic = MakePic("9(3)V9(2)");
        var dst = NewBuffer(dstPic);

        PicRuntime.MoveNumericLiteral(
            dst, 0, dst.Length, dstPic,
            123.45m, 0);

        var value = PicRuntime.DecodeNumeric(dst, 0, dst.Length, dstPic);
        Assert.Equal(123.45m, value);
    }
}
