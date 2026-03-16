using System.Text;
using CobolSharp.Runtime;
using Xunit;

namespace CobolSharp.Tests.Unit.Runtime;

public class PicRuntimeMoveTests
{
    // ── Helpers ──

    private static PicDescriptor MakeNumeric(int totalDigits, int fractionDigits = 0,
        bool isSigned = false, SignStorageKind signStorage = SignStorageKind.None)
    {
        int storageLength = totalDigits;
        if (signStorage is SignStorageKind.LeadingSeparate or SignStorageKind.TrailingSeparate)
            storageLength++;
        return new PicDescriptor(
            totalDigits: totalDigits, fractionDigits: fractionDigits, isSigned: isSigned,
            isNumeric: true, isAlphanumeric: false, hasEditing: false,
            storageLength: storageLength, usage: UsageKind.Display,
            category: CobolCategory.Numeric, signStorage: signStorage,
            editing: EditingKind.None, blankWhenZero: false,
            leadingScaleDigits: 0, trailingScaleDigits: 0);
    }

    private static PicDescriptor MakeAlphanumeric(int length)
    {
        return new PicDescriptor(
            totalDigits: 0, fractionDigits: 0, isSigned: false,
            isNumeric: false, isAlphanumeric: true, hasEditing: false,
            storageLength: length, usage: UsageKind.Display,
            category: CobolCategory.Alphanumeric, signStorage: SignStorageKind.None,
            editing: EditingKind.None, blankWhenZero: false,
            leadingScaleDigits: 0, trailingScaleDigits: 0);
    }

    private static PicDescriptor MakeEdited(string pattern, int totalDigits, int fractionDigits = 0,
        bool isSigned = false)
    {
        return new PicDescriptor(
            totalDigits: totalDigits, fractionDigits: fractionDigits, isSigned: isSigned,
            isNumeric: true, isAlphanumeric: false, hasEditing: true,
            storageLength: pattern.Length, usage: UsageKind.Display,
            category: CobolCategory.NumericEdited, signStorage: SignStorageKind.None,
            editing: EditingKind.ZeroSuppress, blankWhenZero: false,
            leadingScaleDigits: 0, trailingScaleDigits: 0, editPattern: pattern);
    }

    // ── FormatByEditPattern tests ──

    [Fact]
    public void FormatEdited_ZZ_ZZ9_Value42()
    {
        var pic = MakeEdited("ZZ,ZZ9", totalDigits: 5);
        string result = PicRuntime.FormatNumericEdited(42m, pic);
        Assert.Equal("    42", result);
    }

    [Fact]
    public void FormatEdited_ZZ_ZZ9_Value0()
    {
        var pic = MakeEdited("ZZ,ZZ9", totalDigits: 5);
        string result = PicRuntime.FormatNumericEdited(0m, pic);
        Assert.Equal("     0", result);
    }

    [Fact]
    public void FormatEdited_ZZ_ZZ9_Value12345()
    {
        var pic = MakeEdited("ZZ,ZZ9", totalDigits: 5);
        string result = PicRuntime.FormatNumericEdited(12345m, pic);
        Assert.Equal("12,345", result);
    }

    [Fact]
    public void FormatEdited_Minus9_9_Negative()
    {
        // PIC -999999999.999999999 (expanded from -9(9).9(9))
        var pic = MakeEdited("-999999999.999999999", totalDigits: 18, fractionDigits: 9, isSigned: true);
        string result = PicRuntime.FormatNumericEdited(-123.456m, pic);
        Assert.Contains("123", result);
        Assert.Contains("-", result);
    }

    [Fact]
    public void FormatEdited_Minus9_9_Positive()
    {
        var pic = MakeEdited("-999999999.999999999", totalDigits: 18, fractionDigits: 9, isSigned: true);
        string result = PicRuntime.FormatNumericEdited(123.456m, pic);
        Assert.Contains("123", result);
        Assert.DoesNotContain("-", result);
    }

    // ── MoveNumericToAlphanumeric: sign stripped ──

    [Fact]
    public void MoveNumericToAlpha_StripsSign()
    {
        var srcPic = MakeNumeric(3, isSigned: true, signStorage: SignStorageKind.TrailingOverpunch);
        var dstPic = MakeAlphanumeric(5);

        byte[] src = new byte[3];
        byte[] dst = new byte[5];
        PicRuntime.EncodeNumeric(src, 0, 3, srcPic, -42m);

        PicRuntime.MoveNumericToAlphanumeric(src, 0, 3, srcPic, dst, 0, 5, dstPic, 0);

        string text = Encoding.ASCII.GetString(dst);
        Assert.Equal("042  ", text); // absolute value, right-justified in source digits, left-justified in dest
    }

    // ── MoveAlphanumericToNumeric ──

    [Fact]
    public void MoveAlphaToNumeric_ParsesDigits()
    {
        var srcPic = MakeAlphanumeric(5);
        var dstPic = MakeNumeric(5, isSigned: true, signStorage: SignStorageKind.TrailingOverpunch);

        byte[] src = Encoding.ASCII.GetBytes("  123");
        byte[] dst = new byte[5];

        PicRuntime.MoveAlphanumericToNumeric(src, 0, 5, srcPic, dst, 0, 5, dstPic, 0);

        decimal value = PicRuntime.DecodeNumeric(dst, 0, 5, dstPic);
        Assert.Equal(123m, value);
    }

    // ── MoveNumericEditedToNumeric ──

    [Fact]
    public void MoveEditedToNumeric_DeEdits()
    {
        var srcPic = MakeEdited("ZZ,ZZ9", totalDigits: 5);
        var dstPic = MakeNumeric(5, isSigned: true, signStorage: SignStorageKind.TrailingOverpunch);

        byte[] src = Encoding.ASCII.GetBytes(" 1,234");
        byte[] dst = new byte[5];

        PicRuntime.MoveNumericEditedToNumeric(src, 0, 6, srcPic, dst, 0, 5, dstPic, 0);

        decimal value = PicRuntime.DecodeNumeric(dst, 0, 5, dstPic);
        Assert.Equal(1234m, value);
    }

    // ── MoveNumericToNumeric cross-sign-format ──

    [Fact]
    public void MoveNumeric_TrailingSeparateToOverpunch()
    {
        var srcPic = MakeNumeric(5, isSigned: true, signStorage: SignStorageKind.TrailingSeparate);
        var dstPic = MakeNumeric(5, isSigned: true, signStorage: SignStorageKind.TrailingOverpunch);

        byte[] src = new byte[6]; // 5 digits + 1 sign byte
        byte[] dst = new byte[5];
        PicRuntime.EncodeNumeric(src, 0, 6, srcPic, -12345m);

        PicRuntime.MoveNumericToNumeric(src, 0, 6, srcPic, dst, 0, 5, dstPic, 0);

        decimal value = PicRuntime.DecodeNumeric(dst, 0, 5, dstPic);
        Assert.Equal(-12345m, value);
    }

    [Fact]
    public void MoveNumeric_OverpunchToLeadingSeparate()
    {
        var srcPic = MakeNumeric(3, isSigned: true, signStorage: SignStorageKind.TrailingOverpunch);
        var dstPic = MakeNumeric(3, isSigned: true, signStorage: SignStorageKind.LeadingSeparate);

        byte[] src = new byte[3];
        byte[] dst = new byte[4]; // 3 digits + 1 sign byte
        PicRuntime.EncodeNumeric(src, 0, 3, srcPic, -99m);

        PicRuntime.MoveNumericToNumeric(src, 0, 3, srcPic, dst, 0, 4, dstPic, 0);

        decimal value = PicRuntime.DecodeNumeric(dst, 0, 4, dstPic);
        Assert.Equal(-99m, value);
    }
}
