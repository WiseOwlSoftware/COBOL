// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE STORAGE-FORM TABLE (V59 step 1). Every USAGE has exactly ONE byte representation, stated here as a
/// table so that adding a usage to the compiler cannot silently inherit a representation it was never given.
/// <para>
/// This is the discriminator the record image had been missing. The compiler already pinned each usage's byte
/// WIDTH (<see cref="PicInfo.StorageWidth"/>, reported by <c>FUNCTION BYTE-LENGTH</c>) but nothing carried the
/// FORM those bytes take, and <see cref="NumericTruncation"/> cannot stand in for it — USAGE DISPLAY and USAGE
/// BINARY are both <see cref="NumericTruncation.DigitCount"/> and occupy entirely different bytes. That is how a
/// <c>PIC 9(4) COMP</c> came to reach a file as the four ASCII bytes <c>31 32 33 34</c>.
/// </para>
/// <para>
/// The representations themselves are implementor-defined and therefore OURS to pin and document (ISO/IEC
/// 1989:2023 §13.18.60.4 GR4 "a radix of 2 is used", GR11 "a radix of 10 … the minimum possible configuration",
/// GR7 for DISPLAY, GR12 for the fixed-width binary usages; §4.2.16 and Annex A.1 items 205/215 make documenting
/// them a REQUIRED item). <see cref="NumericStorageForm"/>'s members carry that documentation.
/// </para>
/// </summary>
public sealed class NumericStorageFormDriftTests
{
    /// <summary>The whole table, usage by usage: the byte representation and the capacity discipline. Two
    /// ORTHOGONAL axes — a row where they seem to track each other (DISPLAY/BINARY both DigitCount; PACKED both
    /// packed) is a coincidence of this table, never a rule to lean on.</summary>
    public static readonly IReadOnlyDictionary<Usage, (NumericStorageForm Form, NumericTruncation Truncation)> Table =
        new Dictionary<Usage, (NumericStorageForm, NumericTruncation)>
        {
            // ── The profile-carrying fixed-point usages: these reach a byte boundary ──
            [Usage.Display] = (NumericStorageForm.Zoned, NumericTruncation.DigitCount),
            [Usage.Binary] = (NumericStorageForm.Binary, NumericTruncation.DigitCount),
            [Usage.Packed] = (NumericStorageForm.Packed, NumericTruncation.PackedDecimal),
            [Usage.Comp5] = (NumericStorageForm.Binary, NumericTruncation.BinaryCapacity),
            [Usage.BinaryChar] = (NumericStorageForm.Binary, NumericTruncation.BinaryCapacity),
            [Usage.BinaryShort] = (NumericStorageForm.Binary, NumericTruncation.BinaryCapacity),
            [Usage.BinaryLong] = (NumericStorageForm.Binary, NumericTruncation.BinaryCapacity),
            [Usage.BinaryDouble] = (NumericStorageForm.Binary, NumericTruncation.BinaryCapacity),
            // ── USAGE INDEX carries a profile (category numeric, PICTURE-less) but reaches NO image: an
            // occurrence-number carrier only SET, SEARCH and relation conditions may reference (§13.18.60.4 GR10).
            // None is the honest answer, and it makes a codec handed one fail loud instead of inventing bytes. ──
            [Usage.Index] = (NumericStorageForm.None, NumericTruncation.DigitCount),
            // ── No profile is emitted for these (RecordStructEmitter.EmitProfiles takes non-float numerics only),
            // so their storage form is never consulted; None states that rather than defaulting to a lie. ──
            [Usage.Float] = (NumericStorageForm.None, NumericTruncation.DigitCount),
            [Usage.Double] = (NumericStorageForm.None, NumericTruncation.DigitCount),
            [Usage.FloatShort] = (NumericStorageForm.None, NumericTruncation.DigitCount),
            [Usage.FloatLong] = (NumericStorageForm.None, NumericTruncation.DigitCount),
            [Usage.FloatExtended] = (NumericStorageForm.None, NumericTruncation.DigitCount),
            [Usage.FloatBinary32] = (NumericStorageForm.None, NumericTruncation.DigitCount),
            [Usage.FloatBinary64] = (NumericStorageForm.None, NumericTruncation.DigitCount),
            [Usage.FloatBinary128] = (NumericStorageForm.None, NumericTruncation.DigitCount),
            [Usage.FloatDecimal16] = (NumericStorageForm.None, NumericTruncation.DigitCount),
            [Usage.FloatDecimal34] = (NumericStorageForm.None, NumericTruncation.DigitCount),
            [Usage.National] = (NumericStorageForm.None, NumericTruncation.DigitCount),
            [Usage.Bit] = (NumericStorageForm.None, NumericTruncation.DigitCount),
            [Usage.Pointer] = (NumericStorageForm.None, NumericTruncation.DigitCount),
            [Usage.ProgramPointer] = (NumericStorageForm.None, NumericTruncation.DigitCount),
            [Usage.FunctionPointer] = (NumericStorageForm.None, NumericTruncation.DigitCount),
            [Usage.ObjectReference] = (NumericStorageForm.None, NumericTruncation.DigitCount),
        };

    private static PicInfo Pic(Usage usage, int digits = 4, bool signed = false) =>
        // Category numeric throughout: StorageForm/Truncation read the USAGE (and the WITH NO SIGN phrase), never
        // the category, so one shape exercises every row.
        new(PicCategory.Numeric, usage, Length: digits, Digits: digits, Scale: 0, Signed: signed);

    /// <summary>A NEW usage lands here first: the table must name it, so its byte representation is a decision
    /// somebody made rather than whatever the fallback arm happened to return.</summary>
    [Fact]
    public void EveryUsage_IsInTheTable()
    {
        var missing = Enum.GetValues<Usage>().Where(u => !Table.ContainsKey(u)).ToList();
        Assert.True(missing.Count == 0,
            "USAGE without a stated storage form — add it to the table (and decide its byte representation): "
            + string.Join(", ", missing));
        var stale = Table.Keys.Where(u => !Enum.IsDefined(u)).ToList();
        Assert.True(stale.Count == 0, "table row for a usage that no longer exists: " + string.Join(", ", stale));
    }

    [Theory]
    [MemberData(nameof(AllUsages))]
    public void EveryUsage_MapsToItsStatedFormAndDiscipline(Usage usage)
    {
        var (form, truncation) = Table[usage];
        Assert.Equal(form, Pic(usage).StorageForm);
        Assert.Equal(truncation, Pic(usage).Truncation);
    }

    public static TheoryData<Usage> AllUsages()
    {
        var data = new TheoryData<Usage>();
        foreach (var u in Enum.GetValues<Usage>()) data.Add(u);
        return data;
    }

    /// <summary>PACKED-DECIMAL WITH NO SIGN (§13.18.60.4 GR11, COBOL-2023) is a DIFFERENT byte form, not a
    /// narrower one: it reserves no sign nibble at all, so a decoder that assumed one would read the last digit
    /// as a sign. The width follows.</summary>
    [Fact]
    public void PackedWithNoSign_IsItsOwnForm()
    {
        var withSign = Pic(Usage.Packed);
        var noSign = Pic(Usage.Packed) with { PackedNoSign = true };
        Assert.Equal(NumericStorageForm.Packed, withSign.StorageForm);
        Assert.Equal(NumericStorageForm.PackedNoSign, noSign.StorageForm);
        Assert.Equal(3, withSign.StorageWidth);   // 4 digits + sign nibble → 3 bytes
        Assert.Equal(2, noSign.StorageWidth);     // 4 digit nibbles → 2 bytes
    }

    /// <summary>The two properties must agree about whether the item HAS bytes of its own: a byte form is
    /// exactly a positive pinned width, and <see cref="NumericStorageForm.Zoned"/>'s width is its digit run
    /// (<see cref="PicInfo.StorageWidth"/> stays 0 — the zoned digits ARE the bytes).</summary>
    [Theory]
    [MemberData(nameof(AllUsages))]
    public void ByteForm_AndPinnedWidth_Agree(Usage usage)
    {
        var pic = Pic(usage);
        bool hasOwnBytes = pic.StorageForm is NumericStorageForm.Binary or NumericStorageForm.Packed
            or NumericStorageForm.PackedNoSign;
        Assert.Equal(hasOwnBytes, pic.StorageWidth > 0);
    }

    /// <summary>Nothing that reaches the record image may be formless — that is the state the image codec must
    /// never be handed, and the arm it will reject loudly.</summary>
    [Theory]
    [MemberData(nameof(AllUsages))]
    public void EveryImageCapableLeaf_HasAByteForm(Usage usage)
    {
        var item = new DataItem { Level = 5, CobolName = "X", CsName = "X", Pic = Pic(usage) };
        if (!item.IsImageCapable) return;
        Assert.NotEqual(NumericStorageForm.None, item.Pic!.StorageForm);
    }

    /// <summary>The emitted profile carries BOTH axes to the runtime — the record-image codec reads the form,
    /// the store path reads the discipline. A profile that stated only one would put the codec back where V59
    /// found it.</summary>
    [Fact]
    public void EmittedProfile_StatesTheFormAndItsWidth()
    {
        Assert.Contains("StorageForm = NumericStorageForm.Binary", Pic(Usage.Binary).ProfileInitializer);
        Assert.Contains("StorageLength = 2", Pic(Usage.Binary).ProfileInitializer);
        Assert.Contains("StorageForm = NumericStorageForm.Packed", Pic(Usage.Packed).ProfileInitializer);
        Assert.Contains("StorageLength = 3", Pic(Usage.Packed).ProfileInitializer);
        Assert.Contains("StorageForm = NumericStorageForm.Zoned", Pic(Usage.Display).ProfileInitializer);
        Assert.Contains("Truncation = NumericTruncation.PackedDecimal", Pic(Usage.Packed).ProfileInitializer);
    }

    /// <summary>The conflation V59 exists to retire, asserted directly: one capacity discipline, two byte
    /// representations. Any future attempt to derive the representation from the discipline fails here.</summary>
    [Fact]
    public void OneTruncationDiscipline_CarriesTwoRepresentations()
    {
        Assert.Equal(Pic(Usage.Display).Truncation, Pic(Usage.Binary).Truncation);
        Assert.NotEqual(Pic(Usage.Display).StorageForm, Pic(Usage.Binary).StorageForm);
    }
}
