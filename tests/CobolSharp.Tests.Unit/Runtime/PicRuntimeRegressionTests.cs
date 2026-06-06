using System.Text;
using Xunit;
using CobolSharp.Runtime;

namespace CobolSharp.Tests.Unit.Runtime;

/// <summary>
/// Regression tests for two spec-fidelity bugs found in the 2026-06-06 data-model ADR review
/// (see docs/DATA_MODEL_REVIEW.md, companion fixes 31 &amp; 32):
///   (1) IS ALPHABETIC accepted the Unicode-wide letter set (char.IsLetter) instead of ISO §8.8.4.4's
///       closed {A-Z, a-z, space}.
///   (2) ROUNDED MODE PROHIBITED silently truncated an inexact result instead of raising the SIZE ERROR
///       condition (EC-SIZE-TRUNCATION, ISO §14.9.4).
/// </summary>
public sealed class PicRuntimeRegressionTests
{
    private static PicDescriptor MakePic(string picBody, UsageKind usage = UsageKind.Display, bool isSigned = false)
        => PicDescriptorFactory.FromPicBody(picBody, usage, isSigned, SignStorageKind.None, false);

    // ---------- Bug 1: IS ALPHABETIC = {A-Z, a-z, space} only (ISO §8.8.4.4) ----------

    [Fact]
    public void IsAlphabeticClass_AcceptsAsciiLettersAndSpace()
    {
        byte[] buf = Encoding.ASCII.GetBytes("AB cd");
        Assert.True(PicRuntime.IsAlphabeticClass(buf, 0, buf.Length));
    }

    [Fact]
    public void IsAlphabeticClass_RejectsDigits()
    {
        byte[] buf = Encoding.ASCII.GetBytes("AB1");
        Assert.False(PicRuntime.IsAlphabeticClass(buf, 0, buf.Length));
    }

    [Fact]
    public void IsAlphabeticClass_RejectsNonAsciiLatinLetter()
    {
        // 0xC9 = 'É' in Latin-1 — a Unicode letter (char.IsLetter == true) but NOT in the COBOL
        // alphabetic class {A-Z, a-z, space}. The old char.IsLetter implementation wrongly accepted it.
        byte[] buf = { (byte)'A', 0xC9, (byte)'B' };
        Assert.False(PicRuntime.IsAlphabeticClass(buf, 0, buf.Length));
    }

    // ---------- Bug 2: ROUNDED MODE PROHIBITED ⇒ SIZE ERROR on an inexact result (ISO §14.9.4) ----------

    [Fact]
    public void RoundProhibited_InexactResult_SetsSizeError_AndLeavesReceiverUnchanged()
    {
        var pic = MakePic("9V9");                 // one fraction digit
        var dst = new byte[pic.StorageLength];
        PicRuntime.EncodeNumeric(dst, 0, dst.Length, pic, 7.7m);

        var status = new ArithmeticStatus();
        // 7.7 + 0.25 = 7.95 → at one fraction digit (79.5) this is inexact → PROHIBITED ⇒ SIZE ERROR.
        PicRuntime.AddNumericLiteral(dst, 0, dst.Length, pic, 0.25m, PicRuntime.RoundProhibited, ref status);

        Assert.True(status.SizeError);
        Assert.Equal(7.7m, PicRuntime.DecodeNumeric(dst, 0, dst.Length, pic)); // receiver unchanged
    }

    [Fact]
    public void RoundProhibited_ExactResult_NoSizeError()
    {
        var pic = MakePic("9V9");
        var dst = new byte[pic.StorageLength];
        PicRuntime.EncodeNumeric(dst, 0, dst.Length, pic, 7.7m);

        var status = new ArithmeticStatus();
        // 7.7 + 0.2 = 7.9 → exactly representable at one fraction digit → no size error.
        PicRuntime.AddNumericLiteral(dst, 0, dst.Length, pic, 0.2m, PicRuntime.RoundProhibited, ref status);

        Assert.False(status.SizeError);
        Assert.Equal(7.9m, PicRuntime.DecodeNumeric(dst, 0, dst.Length, pic));
    }

    // ---------- Bug 3: an unsigned COMP-3 item stores the magnitude (ISO §13.18.40 / §14.9.25 GR8) ----------
    // Surfaced by the data-model numeric differential oracle: EncodeComp3 set the packed sign nibble from
    // value<0 without consulting IsSigned, so an unsigned PIC 9(n) COMP-3 wrongly kept a negative sign and
    // decoded back as negative — unlike DISPLAY/COMP, which already stored the magnitude.

    [Fact]
    public void Comp3_Unsigned_StoresMagnitude_NotNegative()
    {
        var pic = MakePic("9(5)", UsageKind.Comp3, isSigned: false);
        var dst = new byte[pic.StorageLength];
        PicRuntime.EncodeNumeric(dst, 0, dst.Length, pic, -123m);
        Assert.Equal(123m, PicRuntime.DecodeNumeric(dst, 0, dst.Length, pic));
    }

    [Fact]
    public void Comp3_Signed_RetainsNegative()
    {
        var pic = MakePic("S9(5)", UsageKind.Comp3, isSigned: true);
        var dst = new byte[pic.StorageLength];
        PicRuntime.EncodeNumeric(dst, 0, dst.Length, pic, -123m);
        Assert.Equal(-123m, PicRuntime.DecodeNumeric(dst, 0, dst.Length, pic));
    }

    // ---------- Bug 4: trailing-P overflow counts the unit count, not the full magnitude (ISO §13.18.40) ----------
    // Surfaced by the data-model numeric differential oracle: WouldOverflow's COMP/COMP-3/COMP-5 arms scaled by
    // FractionDigits+LeadingScaleDigits but (unlike the DISPLAY arm) omitted the /10^TrailingScaleDigits divide,
    // so a valid trailing-P value (PIC 9(3)P stores 3 digits → max 9990) wrongly raised SIZE ERROR.

    [Theory]
    [InlineData(UsageKind.Comp)]
    [InlineData(UsageKind.Comp3)]
    [InlineData(UsageKind.Comp5)]
    public void TrailingP_ValidValue_NoSizeError(UsageKind usage)
    {
        var pic = MakePic("9(3)P", usage);   // 3 stored digits, scale +1 → represents 0..9990 in steps of 10
        var dst = new byte[pic.StorageLength];
        PicRuntime.EncodeNumeric(dst, 0, dst.Length, pic, 0m);
        var status = new ArithmeticStatus();
        PicRuntime.AddNumericLiteral(dst, 0, dst.Length, pic, 9990m, PicRuntime.RoundTruncation, ref status);
        Assert.False(status.SizeError);
        Assert.Equal(9990m, PicRuntime.DecodeNumeric(dst, 0, dst.Length, pic));
    }

    [Theory]
    [InlineData(UsageKind.Comp)]
    [InlineData(UsageKind.Comp3)]
    public void TrailingP_OverCapacity_SizeError(UsageKind usage)
    {
        var pic = MakePic("9(3)P", usage);
        var dst = new byte[pic.StorageLength];
        PicRuntime.EncodeNumeric(dst, 0, dst.Length, pic, 0m);
        var status = new ArithmeticStatus();
        PicRuntime.AddNumericLiteral(dst, 0, dst.Length, pic, 10000m, PicRuntime.RoundTruncation, ref status);
        Assert.True(status.SizeError);   // 10000 → 4 unit digits > 3 stored digits
    }
}
