// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime;

/// <summary>
/// ⛔ THE RETURNED VALUE OF A NUMERIC OR INTEGER FUNCTION, AS A DATA ITEM THAT DISPLAY SHOWS (ISO/IEC 1989:2023 §15.4 —
/// "The evaluation of a function produces a returned value in a temporary elementary data item"; §15.4.1 — "When
/// native arithmetic is in effect, the characteristics and representation of the returned value are defined by the
/// implementor"; §14.9.11.4 GR1 — "Any conversion of data required between literal-1 or the data item referenced by
/// identifier-1 and the device is defined by the implementor"; docs/CONFORMANCE.md DOC-A.1-92, kb/Work PB3514).
/// <para>WHAT THE FIELD IS. A function's temporary has a DESCRIPTION - digit positions, fraction digits, a sign - and
/// DISPLAY shows it exactly as it shows any other numeric item of that description
/// (<see cref="CobolNum.DisplayDecimal(Int128, in NumProfile, bool)"/>): the decimal point at the scale, a signed
/// item's leading <c>+</c> or <c>-</c>. WiseOwl COBOL's determination, GnuCOBOL 3.2.0's by the precedence of CLAUDE.md
/// rule 1 because the standard leaves the description to the implementor, is the description GnuCOBOL gives each
/// function's temporary, and it is one of THREE shapes:</para>
/// <list type="bullet">
///   <item><b>The argument's own description</b> (<see cref="OfItem"/>, <see cref="OfLiteral"/>): ABS, MAX, MIN, an odd
///         count's MEDIAN and a single argument's MEAN RETURN THE CONTENT of an argument (§15.7.1 for ABS, §15.59.4 1)
///         and §15.63.4 1) for the selections), so the temporary is a copy of that argument's item: <c>FUNCTION MAX(U S)</c>
///         shows the greater item exactly as <c>DISPLAY</c> of that item does, and ABS keeps the argument's description with
///         the sign made positive.</item>
///   <item><b>The description of the value</b> (<see cref="OfValue"/>): every other exact function, and an arithmetic
///         expression used as an argument, returns a value whose description is derived from the value alone -
///         <see cref="CobolNum.DeviceFunctionText"/> owns the rule.</item>
///   <item><b>The binary64 image</b> (<see cref="OfFloat"/>): a floating-point value keeps the shortest round-trip image a
///         floating-point item has (kb/Work PB3505 declined GnuCOBOL's 100-digit float image).</item>
/// </list>
/// <para>⛔ THE SAME ITEM IS WHAT <c>FUNCTION LENGTH</c> AND <c>FUNCTION BYTE-LENGTH</c> MEASURE (<see cref="Length"/>, <see cref="ByteLength"/>, kb/Work PB3951):
/// §8.4.3.2.1 makes the function-identifier the data item the evaluation produces, and one item has one size, so the answer is the
/// temporary's bytes by the description above and never a fixed width of the implementation's own.</para>
/// <para>ONE struct carries all three so that a function nested in another function's argument list (<c>ABS(INTEGER(X))</c>,
/// <c>MAX(ABS(X) 3)</c>) hands its CALLER its description and not only its value - the selection picks a description
/// as well as a value. The compiler builds it only for the functions that select or copy
/// (<c>FunctionDeviceText</c>); every other function's text is <see cref="CobolNum.DeviceFunctionText"/> of its value.</para>
/// </summary>
public readonly struct FunctionValueField
{
    private enum Shape : byte { Value, Item, Float }

    private readonly CobolDec _value;
    private readonly NumProfile _item;
    private readonly string? _image;
    private readonly Shape _shape;
    private readonly int _positions;
    private readonly int _bytes;

    private FunctionValueField(Shape shape, CobolDec value, in NumProfile item, int positions, int bytes, string? image = null)
    {
        _shape = shape;
        _value = value;
        _item = item;
        _positions = positions;
        _bytes = bytes;
        _image = image;
    }

    /// <summary>The temporary whose positions are its bytes: every temporary but the copy of a USAGE NATIONAL item, whose position is two bytes.</summary>
    private FunctionValueField(Shape shape, CobolDec value, in NumProfile item, int bytes, string? image = null)
        : this(shape, value, item, bytes, bytes, image) { }

    /// <summary>⛔ THE TEMPORARY'S LENGTH IN BYTES - what <c>FUNCTION LENGTH</c> and <c>FUNCTION BYTE-LENGTH</c> of the function call answer
    /// (ISO/IEC 1989:2023 §15.50.4 r3 - a data item that is neither national nor boolean is measured "in alphanumeric character positions",
    /// one per byte; §15.14.4 r1; §8.4.3.2.1 - the function-identifier references the temporary data item the evaluation produces;
    /// docs/CONFORMANCE.md DOC-A.1-92, kb/Work PB3951). It is the SAME item DISPLAY shows (<see cref="DeviceText"/>), so the two cannot
    /// disagree: a copy of an argument is the bytes of that argument's item (a packed argument's, a binary one's), an item the value
    /// describes is the 4-byte or 8-byte binary item or the DISPLAY item of its digits (<see cref="CobolNum.ReturnedValueProfile"/>), and a
    /// binary64 result is the 8-byte floating-point item (the 100-digit image GnuCOBOL gives it is not copied, kb/Work PB3505).</summary>
    public long ByteLength => _bytes;

    /// <summary>⛔ THE TEMPORARY'S LENGTH IN POSITIONS - what <c>FUNCTION LENGTH</c> of the function call answers (ISO/IEC 1989:2023 §15.50.4 r2 - a
    /// usage-national item is measured in national character positions; r3 - any other item that is neither national nor boolean in alphanumeric
    /// character positions; kb/Work PB3951). It differs from <see cref="ByteLength"/> for exactly one temporary, the copy of a USAGE NATIONAL
    /// argument (a position is two bytes); the two are stated by the compiler from the argument item's own facts (<see cref="OfItem"/>).</summary>
    public long Length => _positions;

    /// <summary>The temporary is a copy of the data item <paramref name="item"/> (an argument that is a field). <paramref name="positions"/> and
    /// <paramref name="bytes"/> are the item's own length by §15.50.4 and §15.14.4 - the compiler's one reading of the item
    /// (<c>ItemLength.Positions</c>, <c>DataItem.ByteWidth</c>), never re-derived from the profile, which does not say a national item's
    /// position is two bytes.</summary>
    public static FunctionValueField OfItem(CobolDec value, in NumProfile item, int positions, int bytes) => new(Shape.Item, value, item, positions, bytes);

    /// <summary>The temporary is a copy of a numeric LITERAL's field: <paramref name="digits"/> digit positions (every digit the
    /// literal wrote, a leading zero included), <paramref name="scale"/> fraction digits, and a sign position when the literal
    /// wrote a sign.</summary>
    public static FunctionValueField OfLiteral(CobolDec value, int digits, int scale, bool signed)
    {
        NumProfile described = Described(digits, scale, signed);
        return new(Shape.Item, value, described, digits + (signed ? 1 : 0));   // a digit position each, and the written sign's own position
    }

    /// <summary>The temporary is an unsigned integer item of <paramref name="digits"/> digit positions (DATE-OF-INTEGER's eight,
    /// DAY-OF-INTEGER's seven).</summary>
    public static FunctionValueField OfDigits(CobolDec value, int digits) => OfLiteral(value, digits, 0, signed: false);

    /// <summary>The temporary is described by its value (<see cref="CobolNum.DeviceFunctionText"/>).</summary>
    public static FunctionValueField OfValue(CobolDec value)
    {
        // The description is fixed when the temporary is made - its sign position follows the value's sign THEN, so ABS of a
        // negative expression is a signed item holding the magnitude, as GnuCOBOL's is. A value of more than 38 digits has no
        // Int128 unscaled form to show an item by; its text is composed from the value, and the description it keeps is the
        // sign position alone (DeviceText), so ABS of a negative 54-digit expression is still a signed item.
        NumProfile described = CobolNum.ReturnedValueProfile(value, out string digits, out int bytes);
        return new(digits.Length <= 38 ? Shape.Item : Shape.Value, value, described, bytes);
    }

    /// <summary>The temporary is a function's binary64 result: it shows the shortest round-trip image and orders by the value that
    /// image names (<c>CobolDec.FromApproximation</c>, the lift of an engine-produced approximation). It is the 8 bytes of a binary64
    /// item (<see cref="ByteLength"/>).</summary>
    public static FunctionValueField OfFloat(double value) =>
        new(Shape.Float, CobolDec.FromApproximation(value), default, sizeof(double), CobolFloat.Display(value));

    /// <summary>A floating-point ITEM argument: it orders by its exact value (<paramref name="exactValue"/>, the data item's one
    /// SDIDI lift, never an approximation) and shows the image its own DISPLAY writes (a binary32 item's shortest binary32
    /// round-trip, not its binary64 widening). The temporary copies the item, so it has the item's <paramref name="bytes"/>
    /// (4 for a binary32 item, 8 for a binary64 one; a floating-point item is never national, so its positions are its bytes).</summary>
    public static FunctionValueField OfFloatItem(CobolDec exactValue, in NumProfile item, string image, int bytes) =>
        new(Shape.Float, exactValue, item, bytes, image);

    /// <summary>The same description holding the magnitude of the value (§15.7.1 - ABS returns the absolute value of its
    /// argument; GnuCOBOL stores it in a copy of the argument's item, so the sign stays a position of the item and shows
    /// <c>+</c> in a signed one).</summary>
    public FunctionValueField Abs() => _shape switch
    {
        Shape.Float => new(Shape.Float, new CobolDec(Int128.Abs(_value.Sig), _value.Exp), _item, _positions, _bytes, _image![0] == '-' ? _image[1..] : _image),
        _ => new(_shape, new CobolDec(Int128.Abs(_value.Sig), _value.Exp), _item, _positions, _bytes),
    };

    /// <summary>§15.59.4 1) - the content of the argument having the greatest value; 2) - the leftmost one when several are equal
    /// (a later argument of equal value does not replace it, so the temporary is the first one's item).</summary>
    public static FunctionValueField Max(params FunctionValueField[] arguments)
    {
        CobolIntrinsics.RequireArguments(arguments.Length, "MAX");   // an ALL subscript over no occurrence (§15.3)
        FunctionValueField best = arguments[0];
        for (int i = 1; i < arguments.Length; i++)
            if (CobolDec.Compare(arguments[i]._value, best._value) > 0) best = arguments[i];
        return best;
    }

    /// <summary>§15.63.4 1) - the content of the argument having the least value; 2) - the leftmost one when several are equal.</summary>
    public static FunctionValueField Min(params FunctionValueField[] arguments)
    {
        CobolIntrinsics.RequireArguments(arguments.Length, "MIN");
        FunctionValueField best = arguments[0];
        for (int i = 1; i < arguments.Length; i++)
            if (CobolDec.Compare(arguments[i]._value, best._value) < 0) best = arguments[i];
        return best;
    }

    /// <summary>§15.61.4 - an odd count returns the content of the middle argument of the sorted list (equal values keep their
    /// written order, so the first of a run is the earlier one); an even count returns the arithmetic mean of the two middle
    /// values, a value with no item of its own.</summary>
    public static FunctionValueField Median(params FunctionValueField[] arguments)
    {
        CobolIntrinsics.RequireArguments(arguments.Length, "MEDIAN");
        var sorted = (FunctionValueField[])arguments.Clone();
        for (int i = 1; i < sorted.Length; i++)   // insertion sort: stable, and the lists are short
        {
            FunctionValueField x = sorted[i];
            int j = i - 1;
            while (j >= 0 && CobolDec.Compare(sorted[j]._value, x._value) > 0) { sorted[j + 1] = sorted[j]; j--; }
            sorted[j + 1] = x;
        }
        int n = sorted.Length;
        if ((n & 1) == 1) return sorted[n / 2];
        CobolDec sum = CobolDec.Add(sorted[n / 2 - 1]._value, sorted[n / 2]._value, CobolRounding.Truncation);
        return OfValue(CobolDec.Div(sum, CobolDec.From(2, 0), CobolRounding.Truncation));
    }

    /// <summary>The characters DISPLAY writes for this temporary. <paramref name="commaMode"/> is the unit's decimal separator
    /// (§12.3.7.4 GR14 a); a binary64 image keeps its period, as a floating-point item's does.</summary>
    public string DeviceText(bool commaMode) => _shape switch
    {
        Shape.Float => _image!,
        Shape.Item => CobolNum.DisplayDecimal(_value.ToUnscaled(_item.FractionDigits, CobolRounding.Truncation), _item, commaMode),
        _ => CobolNum.DeviceFunctionText(_value, _item.Signed, commaMode),
    };

    private static NumProfile Described(int digits, int scale, bool signed) => new()
    {
        Digits = digits,
        FractionDigits = scale,
        Signed = signed,
        SignKind = NumericSign.LeadingSeparate,
        Truncation = NumericTruncation.DigitCount,
        ByteForm = NumericByteForm.Zoned,
    };
}
