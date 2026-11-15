// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Text;

namespace WiseOwl.Cobol.Runtime.IO;

/// <summary>The classes of data item the FORMAT clause selects for formatting (ISO §2023:13.18.24.2 — BIT, CHARACTER and
/// NUMERIC sit inside choice indicators, so any non-empty combination is legal). <see cref="None"/> is a file with no FORMAT
/// clause whose record descriptions are still selected, by SELECT WHEN, for the item-by-item CODE-SET conversion
/// (§2023:13.18.13.4 GR4).</summary>
[Flags]
public enum RecordFormatKinds
{
    /// <summary>No FORMAT clause: no item is formatted.</summary>
    None = 0,

    /// <summary>FORMAT BIT — §2023:13.18.24.4 GR5: "data items described with class boolean and usage bit".</summary>
    Bit = 1,

    /// <summary>FORMAT CHARACTER — §2023:13.18.24.4 GR4: the alphabetic, alphanumeric, display boolean, national, display
    /// numeric and national numeric items.</summary>
    Character = 2,

    /// <summary>FORMAT NUMERIC — §2023:13.18.24.4 GR6: "class numeric and a usage other than national or display".</summary>
    Numeric = 4,
}

/// <summary>What one <see cref="RecordStep"/> of a record description is, which decides whether a FORMAT kind selects it
/// (§2023:13.18.24.4 GR4 - GR6) and what its external representation is
/// (<see cref="RecordFormat"/> documents the representation, GR10).</summary>
public enum RecordStepKind : byte
{
    /// <summary>Bytes no clause converts: a national item (its UTF-16BE pairs cross the medium as they are, both alphabets of
    /// a CODE-SET being identity over the national positions this implementation supports), slack between items, and every
    /// other item a FORMAT clause does not name. GR7: "transferred as is".</summary>
    Raw = 0,

    /// <summary>An item of class alphabetic or alphanumeric, or a boolean item of usage display: characters on the medium
    /// already (selected by CHARACTER, nothing to format), converted by an alphanumeric CODE-SET alphabet (§2023:13.18.13.4 GR4).</summary>
    Display = 1,

    /// <summary>Class numeric, usage display (CHARACTER): formatted as a leading separate sign and the digits (GR8).</summary>
    Zoned = 2,

    /// <summary>Class numeric, usage national (CHARACTER): the same text as <see cref="Zoned"/>, in national positions.</summary>
    NationalNumeric = 3,

    /// <summary>Class numeric, binary or packed decimal (NUMERIC): the same text as <see cref="Zoned"/>, over the digits the
    /// representation can hold.</summary>
    Numeric = 4,

    /// <summary>Class numeric, floating-point (NUMERIC): a fixed-width leading sign and an exponent form that restores the
    /// value exactly.</summary>
    Float = 5,

    /// <summary>A run of usage BIT items (BIT): one character <c>0</c> or <c>1</c> for each bit of each item.</summary>
    BitRun = 6,
}

/// <summary>One step of a <see cref="RecordPlan"/>: a window of the record description's byte image and what it is.
/// <paramref name="Offset"/> and <paramref name="Length"/> are in bytes of the INTERNAL image; <paramref name="Profile"/>
/// is the numeric item's own profile (the step kinds that carry a value); <paramref name="Bits"/> lists, for a
/// <see cref="RecordStepKind.BitRun"/>, the absolute start bit and the bit count of each item in the run, two integers per item.</summary>
public readonly record struct RecordStep(RecordStepKind Kind, int Offset, int Length, NumProfile Profile = default,
    int[]? Bits = null);

/// <summary>A record description entry's elementary items as the steps of its byte image, in memory order, contiguous and
/// not overlapping: every byte of <see cref="Width"/> belongs to exactly one step. The compiler builds it from the bound
/// description (<c>RecordPlans</c>), so the runtime knows no data description.</summary>
public sealed class RecordPlan
{
    /// <summary>The steps, in memory order.</summary>
    public RecordStep[] Steps { get; }

    /// <summary>The bytes of the record description's internal image.</summary>
    public int Width { get; }

    /// <summary>Whether the entry is described as national — "a national data item or … an elementary data item of usage national and of category
    /// numeric, numeric-edited, or boolean" (§2023:14.9.40.4 GR7 a): the record a SORT or MERGE transfer fills with national spaces, not
    /// alphanumeric ones, when it is shorter than a fixed-length file's records.</summary>
    public bool National { get; init; }

    /// <summary>The plan of a record description whose image is <paramref name="width"/> bytes.</summary>
    public RecordPlan(int width, params RecordStep[] steps)
    {
        Width = width;
        Steps = steps;
    }
}

/// <summary>
/// ⛔ THE ONE FORMAT AND SELECT WHEN ENGINE (ISO §2023:13.18.24 FORMAT clause, §2023:13.18.51 SELECT WHEN clause; ISO/IEC 1989:2002
/// §13.16.22 and §13.16.49, the editions that REQUIRE them): the translation of a record between its INTERNAL image — the
/// record area — and its EXTERNAL image, the bytes on the medium, item by item over the SELECTED record description entry.
/// One instance per file connector.
/// <para><b>Selection.</b> A file with several record description entries has one <see cref="RecordPlan"/> for each and a
/// selector (<see cref="Select"/>, compiled from the SELECT WHEN clauses) that evaluates their conditions in the order the
/// entries are written until one is true (SELECT WHEN GR3); none true is I-O status 45, a record identification failure
/// (GR4, §2023:9.1.13.7 item 5). A file with one entry uses it (FORMAT GR2 a).</para>
/// <para><b>The representation the FORMAT clause produces</b> (GR10 — Annex A.1 item 84, REQUIRED user documentation,
/// docs/conformance/7/DOC-A.1-84.md): a selected item becomes TEXT, and the text of every item is as wide as its item
/// needs for any value it can hold, so a record's external length is fixed by its record description entry and the
/// plan alone. A numeric item (CHARACTER for display and national, NUMERIC for binary, packed decimal and floating-point)
/// is its sign, <c>+</c> or <c>-</c> and always present for a signed item (GR8: "Signs are presented as SIGN IS LEADING
/// SEPARATE"), then its UNSCALED digits, zero-filled to the digit count; the decimal point stays implied. A floating-point
/// item is the sign and an <c>E</c> exponent form of 17 (binary64) or 9 (binary32) significant digits, which restores the
/// value exactly. A BIT item is one <c>0</c> or <c>1</c> for each bit. An alphabetic or alphanumeric item is itself. An item
/// no kind selects, and the bytes between items, cross the medium as they are (GR7). Reading restores each selected item
/// to the same internal representation (GR11); the exclusions, Annex A.1 item 85: the filler bits that complete a byte of
/// BIT items are restored as zero, and a signed zero is restored as positive.</para>
/// <para><b>CODE-SET with SELECT WHEN</b> (§2023:13.18.13.4 GR4): the alphabet converts, for the selected entry only, each
/// item described with usage display; and after FORMAT on output, before it on input (GR5), so the text an item is formatted
/// to is converted too.</para>
/// </summary>
public sealed class RecordFormat
{
    private readonly RecordFormatKinds _kinds;
    private readonly RecordPlan[] _plans;
    private readonly int _selectorOffset;

    /// <summary>The CODE-SET conversion of alphanumeric characters this file applies item by item, or null.</summary>
    private readonly CodeSetConversion? _codeSet;

    /// <summary>The SELECT WHEN selector: the index of the record description entry whose condition is true of an internal image
    /// whose bytes up to the end of the conditional variable are valid, or −1 when none is (compiled from the SELECT WHEN clauses
    /// and registered by the program that declares the file; null for a file with one entry).</summary>
    public Func<string, int>? Select { get; set; }

    /// <summary>The record format of a file whose record description entries are <paramref name="plans"/>.</summary>
    /// <param name="kinds">The FORMAT clause's kinds, <see cref="RecordFormatKinds.None"/> without the clause.</param>
    /// <param name="plans">One plan per record description entry, in the order they are written.</param>
    /// <param name="selectorOffset">The byte offset of the conditional variable of the SELECT WHEN conditions (SELECT WHEN SR4:
    /// the same in every entry), or −1 for a file with one entry.</param>
    /// <param name="codeSetToNative">The CODE-SET correspondence (<see cref="CodeSetConversion"/>), or null.</param>
    public RecordFormat(RecordFormatKinds kinds, RecordPlan[] plans, int selectorOffset, char[]? codeSetToNative = null)
    {
        ArgumentNullException.ThrowIfNull(plans);
        if (plans.Length == 0) throw new ArgumentException("a record format needs at least one record description entry", nameof(plans));
        if (plans.Length > 1 && selectorOffset < 0)
            throw new ArgumentException("a file with several record description entries selects one by its SELECT WHEN clauses (ISO §2023:13.18.51.4 GR1)", nameof(selectorOffset));
        _kinds = kinds;
        _plans = plans;
        _selectorOffset = selectorOffset;
        _codeSet = codeSetToNative is null ? null : new CodeSetConversion(codeSetToNative);
    }

    /// <summary>Why an image could not be translated.</summary>
    public enum Failure
    {
        /// <summary>No failure.</summary>
        None,

        /// <summary>No record description entry is selected — I-O status 45 (SELECT WHEN GR4).</summary>
        NoRecordSelected,

        /// <summary>An alphanumeric character the CODE-SET alphabet does not represent (§2023:13.18.13.4 GR6 b).</summary>
        CharacterWithoutImage,

        /// <summary>The external text of an item is not the representation of a value of the item (a record this
        /// implementation did not write).</summary>
        NotRepresentable,
    }

    /// <summary>Whether the record description entry SELECTED for <paramref name="internalImage"/> is described as national — the fill a short
    /// record of a SORT or MERGE transfer takes (ISO §2023:14.9.40.4 GR7 b, GR16 b; §2023:14.9.24.4 GR2 b, GR13 b: "the rules of the SELECT WHEN
    /// clause are applied to the record to select its description"). False when none is selected: that record is refused by the statement
    /// that writes it, with I-O status 45.</summary>
    public bool SelectedIsNational(string internalImage)
    {
        int index = _plans.Length == 1 ? 0 : Select!(internalImage);
        return index >= 0 && index < _plans.Length && _plans[index].National;
    }

    /// <summary>The record on the medium, padded on the right to <paramref name="width"/> as a FIXED-length file's record is
    /// (§2023:13.18.43.4 GR6): with the space of the medium's coded character set when the record is alphanumeric — the CODE-SET alphabet
    /// replaces the native space like every other character of the record (§2023:13.18.13.4 GR6 b) — and the national space when it is national
    /// (§2023:14.9.51.4 GR22).</summary>
    public string PadToWidth(string external, int width, bool national)
    {
        if (external.Length >= width) return external;
        if (national) return RecordFill.Fit(external, width, national: true);
        if (!Converted(" ", out string space)) space = " ";   // an alphabet with no space cannot have written the record (the statement refused it)
        return external.PadRight(width, space[0]);
    }

    // ── Internal → external (WRITE, REWRITE) ─────────────────────────────────────────────────────────────────────────

    /// <summary>Translate a record about to be written: select its record description entry, format its selected items and
    /// convert its display items (ISO §2023:13.18.24.4 GR2, GR8; §2023:13.18.13.4 GR4, GR5 — formatting first on output).</summary>
    /// <returns><see cref="Failure.None"/> and the external image, or the reason there is none.</returns>
    public Failure ToExternal(string internalImage, out string external)
    {
        external = "";
        int index = 0;
        if (_plans.Length > 1)
        {
            index = Select!(internalImage);
            if (index < 0 || index >= _plans.Length) return Failure.NoRecordSelected;
        }
        var plan = _plans[index];
        if (internalImage.Length < plan.Width) internalImage = internalImage.PadRight(plan.Width);
        var text = new StringBuilder(plan.Width + 16);
        foreach (var step in plan.Steps)
        {
            string piece;
            try
            {
                if (!TryEncodeStep(step, internalImage, out piece)) return Failure.CharacterWithoutImage;
            }
            catch (FormatException)
            {
                return Failure.NotRepresentable;
            }
            text.Append(piece);
        }
        external = text.ToString();
        return Failure.None;
    }

    private bool Formats(RecordStepKind kind) => kind switch
    {
        RecordStepKind.Display or RecordStepKind.Zoned or RecordStepKind.NationalNumeric => (_kinds & RecordFormatKinds.Character) != 0,
        RecordStepKind.Numeric or RecordStepKind.Float => (_kinds & RecordFormatKinds.Numeric) != 0,
        RecordStepKind.BitRun => (_kinds & RecordFormatKinds.Bit) != 0,
        _ => false,
    };

    /// <summary>The characters a step contributes to the external image; false when the CODE-SET alphabet has no character for one.</summary>
    private bool TryEncodeStep(in RecordStep step, string image, out string piece)
    {
        string window = image.Substring(step.Offset, step.Length);
        switch (step.Kind)
        {
            case RecordStepKind.Raw:
                piece = window;
                return true;
            case RecordStepKind.Display:
                return Converted(window, out piece);
            case RecordStepKind.Zoned:
                return Converted(Formats(step.Kind) ? ExternalNumber(window, step.Profile) : window, out piece);
            case RecordStepKind.NationalNumeric:
                piece = Formats(step.Kind) ? NationalText(ExternalNumber(NationalDigits(window), step.Profile)) : window;
                return true;
            case RecordStepKind.Numeric:
            case RecordStepKind.Float:
                if (!Formats(step.Kind)) { piece = window; return true; }
                return Converted(step.Kind is RecordStepKind.Float ? ExternalFloat(window, step.Profile) : ExternalNumber(window, step.Profile), out piece);
            case RecordStepKind.BitRun:
                if (!Formats(step.Kind)) { piece = window; return true; }
                var bits = new StringBuilder();
                for (int i = 0; i < step.Bits!.Length; i += 2)
                    bits.Append(CobolBits.ReadWindow(image, step.Bits[i], step.Bits[i + 1]));
                return Converted(bits.ToString(), out piece);
            default:
                throw new InvalidOperationException($"record step kind {step.Kind} has no external form");
        }
    }

    /// <summary>The alphanumeric CODE-SET alphabet's replacement of the native characters of <paramref name="text"/>
    /// (§2023:13.18.13.4 GR6 b); the text itself when the file has none.</summary>
    private bool Converted(string text, out string converted)
    {
        if (_codeSet is null) { converted = text; return true; }
        if (_codeSet.HasCharacterWithoutImage(text)) { converted = ""; return false; }
        converted = _codeSet.ToMedium(text);
        return true;
    }

    // ── External → internal (READ) ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>Translate a record just read: select its record description entry by the conditional variable of the SELECT WHEN
    /// clauses, restore its selected items and convert its display items (§2023:13.18.24.4 GR9; §2023:13.18.13.4 GR4, GR5 —
    /// code-set conversion first on input).</summary>
    /// <returns><see cref="Failure.None"/> and the internal image, or the reason there is none.</returns>
    public Failure ToInternal(string external, out string internalImage)
    {
        internalImage = "";
        int index = 0;
        try
        {
            if (_plans.Length > 1)
            {
                index = Select!(SelectorImage(external));
                if (index < 0 || index >= _plans.Length) return Failure.NoRecordSelected;
            }
            return Decode(_plans[index], external, stopAfterOffset: -1, out internalImage);
        }
        catch (FormatException)
        {
            return Failure.NotRepresentable;
        }
    }

    /// <summary>The internal image up to the end of the conditional variable of the SELECT WHEN conditions: the bytes before it
    /// are unspecified (zero), the variable's own bytes are restored. Its external position is the sum of the external widths of
    /// the steps before it in the first entry — SELECT WHEN SR4 gives every entry the same position, and SR5 makes it the first
    /// item when the FORMAT clause is written.</summary>
    private string SelectorImage(string external)
    {
        var plan = _plans[0];
        Decode(plan, external, stopAfterOffset: _selectorOffset, out string prefix);
        return prefix;
    }

    private Failure Decode(RecordPlan plan, string external, int stopAfterOffset, out string internalImage)
    {
        var image = new char[plan.Width];
        Array.Fill(image, StorageByte.ToChar(0));
        int at = 0;   // the position in the external image
        foreach (var step in plan.Steps)
        {
            int width = ExternalWidth(step);
            string text = at >= external.Length ? "" : external.Substring(at, Math.Min(width, external.Length - at));
            if (text.Length < width) text = text.PadRight(width);   // a trailing run of spaces a line sequential record lost
            at += width;
            string bytes = DecodeStep(step, text, image);
            if (bytes.Length > 0) bytes.CopyTo(0, image, step.Offset, bytes.Length);
            if (stopAfterOffset >= 0 && step.Offset == stopAfterOffset)
            {
                internalImage = new string(image, 0, step.Offset + step.Length);
                return Failure.None;
            }
        }
        internalImage = new string(image);
        return Failure.None;
    }

    private string DecodeStep(in RecordStep step, string text, char[] image)
    {
        switch (step.Kind)
        {
            case RecordStepKind.Raw:
                return text;
            case RecordStepKind.Display:
                return Restored(text);
            case RecordStepKind.Zoned:
                return Formats(step.Kind) ? InternalNumber(Restored(text), step.Profile) : Restored(text);
            case RecordStepKind.NationalNumeric:
                return Formats(step.Kind) ? NationalText(InternalNumber(NationalDigits(text), step.Profile)) : text;
            case RecordStepKind.Numeric:
                return Formats(step.Kind) ? InternalNumber(Restored(text), step.Profile) : text;
            case RecordStepKind.Float:
                return Formats(step.Kind) ? InternalFloat(Restored(text), step.Profile) : text;
            case RecordStepKind.BitRun:
                if (!Formats(step.Kind)) return text;
                var run = new char[step.Length];
                Array.Fill(run, StorageByte.ToChar(0));
                string bits = Restored(text);
                int from = 0;
                for (int i = 0; i < step.Bits!.Length; i += 2)
                {
                    int count = step.Bits[i + 1];
                    CobolBits.WriteWindowInto(run, step.Bits[i] - step.Offset * 8, bits.Substring(from, count));
                    from += count;
                }
                return new string(run);
            default:
                throw new InvalidOperationException($"record step kind {step.Kind} has no external form");
        }
    }

    /// <summary>The native characters of an alphanumeric CODE-SET alphabet's coded characters (§2023:13.18.13.4 GR6 a).</summary>
    private string Restored(string text) => _codeSet is null ? text : _codeSet.ToNative(text);

    /// <summary>How many characters of the external image a step occupies (the width of the text it is formatted to, else its own bytes).</summary>
    private int ExternalWidth(in RecordStep step)
    {
        if (!Formats(step.Kind)) return step.Length;
        return step.Kind switch
        {
            RecordStepKind.Zoned or RecordStepKind.Numeric => NumberWidth(step.Profile),
            RecordStepKind.NationalNumeric => NumberWidth(step.Profile) * CobolBits.BytesPerNational,
            RecordStepKind.Float => FloatWidth(step.Profile),
            RecordStepKind.BitRun => BitCount(step),
            _ => step.Length,
        };
    }

    private static int BitCount(in RecordStep step)
    {
        int bits = 0;
        for (int i = 1; i < step.Bits!.Length; i += 2) bits += step.Bits[i];
        return bits;
    }

    // ── The text of a number ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The digits an item's external text holds: enough for any value its representation can hold, never fewer than its
    /// PICTURE's digits (a binary item without truncation holds more than its picture says).</summary>
    private static int DigitsOf(in NumProfile profile)
    {
        int capacity = profile.ByteForm switch
        {
            NumericByteForm.Binary => DecimalDigits(profile.StorageLength * 8, profile.Signed),
            NumericByteForm.Packed => profile.StorageLength * 2 - 1,
            NumericByteForm.PackedNoSign => profile.StorageLength * 2,
            _ => 0,
        };
        return Math.Max(profile.Digits, capacity);
    }

    /// <summary>The decimal digits of the largest magnitude a two's-complement or unsigned integer of <paramref name="bits"/> bits holds.</summary>
    private static int DecimalDigits(int bits, bool signed)
    {
        var max = signed ? (UInt128)1 << (bits - 1 > 127 ? 127 : bits - 1) : bits >= 128 ? UInt128.MaxValue : ((UInt128)1 << bits) - 1;
        if (signed && bits > 128) max = UInt128.MaxValue;
        return max.ToString(CultureInfo.InvariantCulture).Length;
    }

    private static int NumberWidth(in NumProfile profile) => (profile.Signed ? 1 : 0) + DigitsOf(profile);

    private static int FloatWidth(in NumProfile profile) => profile.ByteForm is NumericByteForm.Ieee32 ? 16 : 24;

    /// <summary>A numeric item's external text: its sign (when it is signed) and its unscaled digits.</summary>
    private static string ExternalNumber(string imageWindow, in NumProfile profile)
    {
        UInt128 magnitude;
        bool negative;
        if (profile.ImageExceedsInt128)
        {
            magnitude = CobolNum.ParseImageU128(imageWindow, profile);
            negative = false;
        }
        else
        {
            var value = CobolNum.ParseImage(imageWindow, profile);
            negative = value < 0;
            magnitude = negative ? (UInt128)(-(value + 1)) + 1 : (UInt128)value;
        }
        string digits = magnitude.ToString(CultureInfo.InvariantCulture).PadLeft(DigitsOf(profile), '0');
        if (digits.Length > DigitsOf(profile)) throw new FormatException("the value does not fit the item's external digits");
        return profile.Signed ? (negative ? "-" : "+") + digits : digits;
    }

    /// <summary>The internal image of a numeric item whose external text is <paramref name="text"/>.</summary>
    private static string InternalNumber(string text, in NumProfile profile)
    {
        bool negative = false;
        int from = 0;
        if (profile.Signed)
        {
            if (text.Length == 0 || text[0] is not ('+' or '-')) throw new FormatException("a signed item's external text opens with its sign");
            negative = text[0] == '-';
            from = 1;
        }
        if (text.Length - from != DigitsOf(profile)) throw new FormatException("the external text is not as wide as the item's digits");
        UInt128 magnitude = 0;
        for (int i = from; i < text.Length; i++)
        {
            if (text[i] is < '0' or > '9') throw new FormatException("an external digit is not 0 - 9");
            magnitude = magnitude * 10 + (UInt128)(text[i] - '0');
        }
        if (profile.ImageExceedsInt128) return CobolNum.FormatImage(magnitude, profile);
        if (magnitude > (UInt128)Int128.MaxValue) throw new FormatException("the external digits exceed the item");
        var value = (Int128)magnitude;
        return CobolNum.FormatImage(negative ? -value : value, profile);
    }

    private static string ExternalFloat(string imageWindow, in NumProfile profile)
    {
        double value = CobolNum.ParseImageFloat(imageWindow, profile);
        if (!double.IsFinite(value)) throw new FormatException("a floating-point item holding a non-finite value has no external text");
        string body = profile.ByteForm is NumericByteForm.Ieee32
            ? Math.Abs((float)value).ToString("E8", CultureInfo.InvariantCulture)
            : Math.Abs(value).ToString("E16", CultureInfo.InvariantCulture);
        return (double.IsNegative(value) ? "-" : "+") + body;
    }

    private static string InternalFloat(string text, in NumProfile profile)
    {
        if (text.Length == 0 || text[0] is not ('+' or '-')) throw new FormatException("a floating-point item's external text opens with its sign");
        double value = double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
        return profile.ByteForm is NumericByteForm.Ieee32
            ? CobolNum.FormatImageSingle((float)value, profile)
            : CobolNum.FormatImageFloat(value, profile);
    }

    /// <summary>The narrow characters of a national window's UTF-16BE byte pairs (a digit or a sign).</summary>
    private static string NationalDigits(string window) => CobolBits.NatReadWindow(window, 0, window.Length / CobolBits.BytesPerNational);

    /// <summary>The UTF-16BE byte pairs of a run of national positions.</summary>
    private static string NationalText(string text) => CobolBits.NatBytes(text);
}
