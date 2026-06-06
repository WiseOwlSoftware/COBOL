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
}
