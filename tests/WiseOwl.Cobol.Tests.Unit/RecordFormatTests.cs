// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Runtime.IO;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ISO §2023:13.18.24.4 GR4 - GR11 (the FORMAT clause) and §2023:13.18.51.4 GR3, GR4 (SELECT WHEN), pinned on
/// <see cref="RecordFormat"/>, the engine every connector shares. The goldens <c>2002/format_clause_*</c> and
/// <c>2002/select_when_*</c> drive the same rules through generated programs; these tests hold the engine's contract
/// where a golden cannot see it: the exact external text of each item class, which record description entry a selector
/// picks, and that reading restores what writing formatted (GR11).
/// </summary>
public sealed class RecordFormatTests
{
    private static NumProfile Zoned(int digits, bool signed = false, NumericSign sign = NumericSign.TrailingOverpunch) => new()
    {
        Digits = digits, FractionDigits = 0, Signed = signed, SignKind = sign,
        Truncation = NumericTruncation.DigitCount, ByteForm = NumericByteForm.Zoned,
    };

    private static NumProfile Packed(int digits, int bytes) => new()
    {
        Digits = digits, FractionDigits = 0, Signed = true, Truncation = NumericTruncation.PackedDecimal,
        ByteForm = NumericByteForm.Packed, StorageLength = bytes,
    };

    private static NumProfile Binary(int digits, int bytes) => new()
    {
        Digits = digits, FractionDigits = 0, Signed = true, Truncation = NumericTruncation.DigitCount,
        ByteForm = NumericByteForm.Binary, StorageLength = bytes,
    };

    private static NumProfile Float64() => new()
    {
        Digits = 0, FractionDigits = 0, Signed = true, Truncation = NumericTruncation.DigitCount,
        ByteForm = NumericByteForm.Ieee64, StorageLength = 8,
    };

    private static string Image(Int128 value, NumProfile p) => CobolNum.FormatImage(value, p);

    [Fact]
    public void CharacterAndNumeric_FormatEveryNumericItemAsASignAndItsDigits()
    {
        // GR8: "Signs are presented as SIGN IS LEADING SEPARATE"; GR4 selects display numeric, GR6 binary and packed.
        var zoned = Zoned(4, signed: true);
        var packed = Packed(5, 3);
        var binary = Binary(4, 2);
        var plan = new RecordPlan(1 + 4 + 3 + 2,
            new RecordStep(RecordStepKind.Display, 0, 1),
            new RecordStep(RecordStepKind.Zoned, 1, 4, zoned),
            new RecordStep(RecordStepKind.Numeric, 5, 3, packed),
            new RecordStep(RecordStepKind.Numeric, 8, 2, binary));
        string image = "A" + Image(-12, zoned) + Image(34567, packed) + Image(-300, binary);

        var format = new RecordFormat(RecordFormatKinds.Character | RecordFormatKinds.Numeric, [plan], -1);
        Assert.Equal(RecordFormat.Failure.None, format.ToExternal(image, out string external));
        // The binary item holds 16 bits, so its digits are those of 32768: five, one more than its picture's four.
        Assert.Equal("A-0012+34567-00300", external);

        Assert.Equal(RecordFormat.Failure.None, format.ToInternal(external, out string restored));
        Assert.Equal(image, restored);   // GR11
    }

    [Fact]
    public void AnItemNoKindSelects_IsTransferredAsIs()
    {
        // GR7: FORMAT CHARACTER alone leaves a packed item as it is.
        var packed = Packed(5, 3);
        var plan = new RecordPlan(3, new RecordStep(RecordStepKind.Numeric, 0, 3, packed));
        string image = Image(12345, packed);

        var format = new RecordFormat(RecordFormatKinds.Character, [plan], -1);
        Assert.Equal(RecordFormat.Failure.None, format.ToExternal(image, out string external));
        Assert.Equal(image, external);
    }

    [Fact]
    public void Bit_FormatsEachBitOfARunAsOneCharacter()
    {
        // GR5: usage bit. Two items share a byte (§2023:8.5.1.6.3): 3 bits at bit 0 and 4 bits at bit 3, then the filler bit.
        var plan = new RecordPlan(1, new RecordStep(RecordStepKind.BitRun, 0, 1, Bits: [0, 3, 3, 4]));
        string image = CobolBits.Pack("10101101", 8);   // 1010 1101 : 101 + 0110 + filler 1
        var format = new RecordFormat(RecordFormatKinds.Bit, [plan], -1);

        Assert.Equal(RecordFormat.Failure.None, format.ToExternal(image, out string external));
        Assert.Equal("1010110", external);
        Assert.Equal(RecordFormat.Failure.None, format.ToInternal(external, out string restored));
        // Annex A.1 item 85, the exclusion: the filler bit that completes the byte is restored as zero.
        Assert.Equal(CobolBits.Pack("10101100", 8), restored);
    }

    [Fact]
    public void Float_FormatsAFixedWidthTextThatRestoresTheValueExactly()
    {
        var p = Float64();
        var plan = new RecordPlan(8, new RecordStep(RecordStepKind.Float, 0, 8, p));
        string image = CobolNum.FormatImageFloat(-0.1, p);
        var format = new RecordFormat(RecordFormatKinds.Numeric, [plan], -1);

        Assert.Equal(RecordFormat.Failure.None, format.ToExternal(image, out string external));
        Assert.Equal(24, external.Length);
        Assert.Equal('-', external[0]);
        Assert.Equal(RecordFormat.Failure.None, format.ToInternal(external, out string restored));
        Assert.Equal(image, restored);
    }

    private static RecordFormat TwoRecords(RecordFormatKinds kinds, char[]? codeSet = null)
    {
        // 01 R-A. 05 TAG PIC X. 88 IS-A VALUE "A". 05 N PIC S9(3).   01 R-B. 05 TAG PIC X. 05 TEXT PIC X(4).
        var n = Zoned(3, signed: true);
        var a = new RecordPlan(5, new RecordStep(RecordStepKind.Display, 0, 1), new RecordStep(RecordStepKind.Zoned, 1, 4, Zoned(4, true)));
        var b = new RecordPlan(5, new RecordStep(RecordStepKind.Display, 0, 1), new RecordStep(RecordStepKind.Display, 1, 4));
        _ = n;
        return new RecordFormat(kinds, [a, b], selectorOffset: 0, codeSet)
        {
            Select = image => image[0] == 'A' ? 0 : image[0] == 'B' ? 1 : -1,
        };
    }

    [Fact]
    public void SelectWhen_PicksTheEntryWhoseConditionIsTrue_AndNoEntryIsStatus45()
    {
        var format = TwoRecords(RecordFormatKinds.Character);
        var signed = Zoned(4, true);

        // WRITE: the first entry is selected by its tag and formats its numeric item (GR3).
        Assert.Equal(RecordFormat.Failure.None, format.ToExternal("A" + Image(7, signed), out string a));
        Assert.Equal("A+0007", a);
        // The second entry's item is text and crosses as it is.
        Assert.Equal(RecordFormat.Failure.None, format.ToExternal("BHEY!", out string b));
        Assert.Equal("BHEY!", b);

        // READ: the selector sees the tag restored from the medium (SR4, SR5), then the whole record is restored by that entry.
        Assert.Equal(RecordFormat.Failure.None, format.ToInternal(a, out string readA));
        Assert.Equal("A" + Image(7, signed), readA);
        Assert.Equal(RecordFormat.Failure.None, format.ToInternal(b, out string readB));
        Assert.Equal("BHEY!", readB);

        // GR4: no entry selected, a record identification failure — I-O status 45 — on both directions.
        Assert.Equal(RecordFormat.Failure.NoRecordSelected, format.ToExternal("ZHEY!", out _));
        Assert.Equal(RecordFormat.Failure.NoRecordSelected, format.ToInternal("ZHEY!", out _));
    }

    [Fact]
    public void CodeSet_ConvertsDisplayItemsAndTheTextFormattingMade_FormattingFirstOnOutput()
    {
        // GR5 of the CODE-SET clause: formatting first, then conversion on output; conversion first, then formatting on input.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        char[] ebcdic = Encoding.GetEncoding(37).GetChars([.. Enumerable.Range(0, 256).Select(b => (byte)b)]);
        var p = Zoned(3, signed: true);
        var plan = new RecordPlan(4, new RecordStep(RecordStepKind.Display, 0, 1), new RecordStep(RecordStepKind.Zoned, 1, 3, p));
        var format = new RecordFormat(RecordFormatKinds.Character, [plan], -1, ebcdic);
        string image = "A" + Image(-42, p);

        Assert.Equal(RecordFormat.Failure.None, format.ToExternal(image, out string external));
        // EBCDIC 'A' = C1, '-' = 60, '0' = F0, '4' = F4, '2' = F2: the sign and the digits are converted too.
        Assert.Equal("Á`ðôò", external);
        Assert.Equal(RecordFormat.Failure.None, format.ToInternal(external, out string restored));
        Assert.Equal(image, restored);
    }

    [Fact]
    public void SelectedIsNational_AsksTheEntryTheClausesSelect()
    {
        // §2023:14.9.40.4 GR7 b: with several entries and SELECT WHEN clauses, the short-record fill is the SELECTED entry's.
        var national = new RecordPlan(4, new RecordStep(RecordStepKind.Raw, 0, 4)) { National = true };
        var plain = new RecordPlan(4, new RecordStep(RecordStepKind.Raw, 0, 4));
        var format = new RecordFormat(RecordFormatKinds.None, [national, plain], selectorOffset: 0)
        {
            Select = image => image[0] == 'N' ? 0 : image[0] == 'P' ? 1 : -1,
        };
        Assert.True(format.SelectedIsNational("N..."));
        Assert.False(format.SelectedIsNational("P..."));
        Assert.False(format.SelectedIsNational("?..."));   // none selected: the statement that writes it is refused with 45
    }

    [Fact]
    public void AnExternalNumberThatIsNoValue_IsNotRepresentable()
    {
        var p = Zoned(3, signed: true);
        var plan = new RecordPlan(3, new RecordStep(RecordStepKind.Zoned, 0, 3, p));
        var format = new RecordFormat(RecordFormatKinds.Character, [plan], -1);
        Assert.Equal(RecordFormat.Failure.NotRepresentable, format.ToInternal("+1X3", out _));
        Assert.Equal(RecordFormat.Failure.NotRepresentable, format.ToInternal("1234", out _));   // no sign
    }
}
