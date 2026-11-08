// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime;

/// <summary>
/// ⛔ THE RETURNED VALUE OF A NUMERIC OR INTEGER FUNCTION, AS A DATA ITEM THAT DISPLAY SHOWS (ISO/IEC 1989:2023 §2023:15.4 —
/// "The evaluation of a function produces a returned value in a temporary elementary data item"; §2023:15.4.1 — "When
/// native arithmetic is in effect, the characteristics and representation of the returned value are defined by the
/// implementor"; §2023:14.9.11.4 GR1 — "Any conversion of data required between literal-1 or the data item referenced by
/// identifier-1 and the device is defined by the implementor"; docs/CONFORMANCE.md DOC-A.1-92, kb/Work PB3514).
/// <para>WHAT THE FIELD IS. A function's temporary has a DESCRIPTION - digit positions, fraction digits, a sign - and
/// DISPLAY shows it exactly as it shows any other numeric item of that description
/// (<see cref="CobolNum.DisplayDecimal(Int128, in NumProfile, bool)"/>): the decimal point at the scale, a signed
/// item's leading <c>+</c> or <c>-</c>. WiseOwl COBOL's determination, GnuCOBOL 3.2.0's by the precedence of CLAUDE.md
/// rule 1 because the standard leaves the description to the implementor, is the description GnuCOBOL gives each
/// function's temporary, and it is one of THREE shapes:</para>
/// <list type="bullet">
///   <item><b>The argument's own description</b> (<see cref="OfItem"/>, <see cref="OfLiteral"/>): ABS, MAX, MIN, an odd
///         count's MEDIAN and a single argument's MEAN RETURN THE CONTENT of an argument (§2023:15.7.1 for ABS, §2023:15.59.4 1)
///         and §2023:15.63.4 1) for the selections), so the temporary is a copy of that argument's item: <c>FUNCTION MAX(U S)</c>
///         shows the greater item exactly as <c>DISPLAY</c> of that item does, and ABS keeps the argument's description with
///         the sign made positive.</item>
///   <item><b>The description of the value</b> (<see cref="OfValue"/>): every other exact function, and an arithmetic
///         expression used as an argument, returns a value whose description is derived from the value alone -
///         <see cref="CobolNum.DeviceFunctionText(CobolDec, bool)"/> owns the rule. A value of more than 38 digits on the exact
///         wide intermediate keeps every digit under the same rule (<see cref="OfWide"/>, kb/Work PB3953).</item>
///   <item><b>The binary64 image</b> (<see cref="OfFloat"/>): a floating-point value keeps the shortest round-trip image a
///         floating-point item has (kb/Work PB3505 declined GnuCOBOL's 100-digit float image).</item>
/// </list>
/// <para>⛔ THE SAME ITEM IS WHAT <c>FUNCTION LENGTH</c> AND <c>FUNCTION BYTE-LENGTH</c> MEASURE (<see cref="Length"/>, <see cref="ByteLength"/>, kb/Work PB3951):
/// a function-identifier "references the unique data item that results from the evaluation of a function" (§2023:8.4.3.2.1), so both
/// functions measure the temporary by the description above and never a fixed width of the implementation's own. They measure it in
/// different units: BYTE-LENGTH in bytes (§2023:15.14.4 r1), LENGTH in national character positions for a usage-national item (§2023:15.50.4 r2)
/// and in alphanumeric character positions otherwise (r3). The two differ only for the copy of a USAGE NATIONAL argument.</para>
/// <para>ONE struct carries all three so that a function nested in another function's argument list (<c>ABS(INTEGER(X))</c>,
/// <c>MAX(ABS(X) 3)</c>) hands its CALLER its description and not only its value - the selection picks a description
/// as well as a value. The compiler builds it only for the functions that select or copy
/// (<c>FunctionDeviceText</c>); every other function's text is <see cref="CobolNum.DeviceFunctionText"/> of its value.</para>
/// </summary>
public readonly struct FunctionValueField
{
    /// <summary>What the temporary is: a value the value describes, a copy of an item, a binary64 image, or a value of more than
    /// 38 digits held EXACTLY, on the wide intermediate or past it (<see cref="OfWide"/>, <see cref="OfExact"/>, kb/Work PB3953, PB4750).</summary>
    private enum Shape : byte { Value, Item, Float, Exact }

    /// <summary>The temporary's SDIDI value. For a <see cref="Shape.Exact"/> temporary it is only the round-to-odd IMAGE of
    /// <see cref="_exact"/>, and <see cref="Compare"/> never orders by that image: a pair holding an exact value is ordered by the
    /// exact values.</summary>
    private readonly CobolDec _value;
    private readonly NumProfile _item;
    private readonly string? _image;
    private readonly Shape _shape;
    private readonly int _positions;
    private readonly int _bytes;
    private readonly CobolExact _exact;

    private FunctionValueField(Shape shape, CobolDec value, in NumProfile item, int positions, int bytes, string? image = null)
    {
        _shape = shape;
        _value = value;
        _item = item;
        _positions = positions;
        _bytes = bytes;
        _image = image;
    }

    /// <summary>A temporary holding a value of more than 38 digits exactly (<paramref name="exact"/>, on the wide form or past it).</summary>
    private FunctionValueField(CobolExact exact, in NumProfile item, int bytes)
        : this(Shape.Exact, exact.Image, item, bytes, bytes) => _exact = exact;

    /// <summary>The temporary whose positions are its bytes: every temporary but the copy of a USAGE NATIONAL item, whose position is two bytes.</summary>
    private FunctionValueField(Shape shape, CobolDec value, in NumProfile item, int bytes, string? image = null)
        : this(shape, value, item, bytes, bytes, image) { }

    /// <summary>⛔ THE TEMPORARY'S LENGTH IN BYTES - what <c>FUNCTION BYTE-LENGTH</c> of the function call answers (ISO/IEC 1989:2023
    /// §2023:15.14.4 r1 - "the length of argument-1 in number of bytes"; §2023:8.4.3.2.4 r1 - "A function-identifier references a temporary data
    /// item"; docs/CONFORMANCE.md DOC-A.1-92, kb/Work PB3951). <c>FUNCTION LENGTH</c> reads <see cref="Length"/>, which is the same
    /// number for every temporary but the copy of a USAGE NATIONAL argument. It is the SAME item DISPLAY shows (<see cref="DeviceText"/>), so the two cannot
    /// disagree: a copy of an argument is the bytes of that argument's item (a packed argument's, a binary one's), an item the value
    /// describes is the 4-byte or 8-byte binary item or the DISPLAY item of its digits (<see cref="CobolNum.ReturnedValueProfile"/>), and a
    /// binary64 result is the 8-byte floating-point item (the 100-digit image GnuCOBOL gives it is not copied, kb/Work PB3505).</summary>
    public long ByteLength => _bytes;

    /// <summary>⛔ THE TEMPORARY'S LENGTH IN POSITIONS - what <c>FUNCTION LENGTH</c> of the function call answers (ISO/IEC 1989:2023 §2023:15.50.4 r2 - a
    /// usage-national item is measured in national character positions; r3 - any other item that is neither national nor boolean in alphanumeric
    /// character positions; kb/Work PB3951). It differs from <see cref="ByteLength"/> for exactly one temporary, the copy of a USAGE NATIONAL
    /// argument (a position is two bytes); the two are stated by the compiler from the argument item's own facts (<see cref="OfItem"/>).</summary>
    public long Length => _positions;

    /// <summary>The temporary is a copy of the data item <paramref name="item"/> (an argument that is a field). <paramref name="positions"/> and
    /// <paramref name="bytes"/> are the item's own length by §2023:15.50.4 and §2023:15.14.4 - the compiler's one reading of the item
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
        // sign position alone (DeviceText), so ABS of a negative 54-digit expression is still a signed item. A value of more
        // than 38 digits that is EXACT (an argument expression past the Int128 carrier) is OfWide's, which keeps every digit;
        // this member holds the value it is given. The temporary's size (ByteLength, Length) is the storage of the item that
        // description names (kb/Work PB3951).
        NumProfile described = CobolNum.ReturnedValueProfile(value, out string digits, out int bytes);
        return new(digits.Length <= 38 ? Shape.Item : Shape.Value, value, described, bytes);
    }

    /// <summary>⛔ THE TEMPORARY OF A VALUE CARRIED ON THE EXACT WIDE INTERMEDIATE (<paramref name="value"/> at <paramref name="scale"/>
    /// fraction digits; kb/Work PB3953, docs/CONFORMANCE.md DOC-A.1-123): an arithmetic expression argument past the
    /// <see cref="Int128"/> carrier, or an exact function's value over one. GnuCOBOL's temporary is arbitrary-precision, so it holds
    /// every digit: <c>FUNCTION ABS (BIG * BIG * K3)</c> shows the 39-digit product, and MAX, MIN and MEDIAN order such values exactly.
    /// A value that fits <see cref="Int128"/> is exactly an SDIDI of at most 39 digits and is <see cref="OfValue"/>'s; a wider one keeps
    /// the description the same rule derives from its digits (<see cref="CobolNum.ReturnedValueProfile(CobolWide, int, out string, out int)"/>).</summary>
    public static FunctionValueField OfWide(CobolWide value, int scale)
    {
        if (value.TryToInt128(out Int128 narrow)) return OfValue(CobolDec.From(narrow, scale));
        return OfExactForm(CobolExact.FromWide(value, scale));
    }

    /// <summary>⛔ THE TEMPORARY OF AN EXACT NATIVE VALUE, ON THE WIDE FORM OR PAST IT (<see cref="CobolExact"/>, kb/Work PB4575, PB4750:
    /// an argument expression in which an exact wide intermediate met an SDIDI-carried operand, an arbitrary-precision value of an
    /// expression past the 256-bit form, a selection or mean over such values): <see cref="OfWide"/> for a value on the wide form, the
    /// same temporary over the arbitrary-precision value for one past it - every digit, as GnuCOBOL's arbitrary-precision temporary
    /// shows it, never the value's 34-digit image - and <see cref="OfValue"/> for one the SDIDI carries. ⛔ A BINARY64 IS A BINARY64
    /// TEMPORARY (kb/Work PB4891): an exact value that is a float item's content or an engine's approximation (<see cref="CobolExact.TryBinary64"/>,
    /// a nested selection's returned content and its negation, which only the run time knows) is <see cref="OfBinary64"/> in
    /// <paramref name="mode"/> or <see cref="OfFloat"/>, as the renderer describes a binary64 it knows at compile time, so an even
    /// <see cref="Median"/> reads the same middle kind on DISPLAY as <c>CobolIntrinsics.MedianExact</c> on COMPUTE.</summary>
    public static FunctionValueField OfExact(CobolExact value, CobolRounding mode) =>
        !value.TryBinary64(out double binary64) ? OfExactValue(value)
        : value.TryApproximation(out _) ? OfFloat(binary64)
        : OfBinary64(binary64, mode);

    /// <summary><see cref="OfExact"/> of a value that is no binary64.</summary>
    private static FunctionValueField OfExactValue(CobolExact value) =>
        value.IsDec ? OfValue(value.Image)
        : value.TryWide(out CobolWide wide, out int scale) ? OfWide(wide, scale)
        : OfExactForm(value);   // past the 256-bit form, or a wide value at a negative scale that cannot be written out on it

    /// <summary>The temporary of a value of more than 38 digits held exactly, described by its own digits.</summary>
    private static FunctionValueField OfExactForm(CobolExact value)
    {
        NumProfile described = CobolNum.ReturnedValueProfile(value, out _, out int bytes);
        return new(value, described, bytes);
    }

    /// <summary>This temporary's value exactly: its own exact value, or its SDIDI value at its own exponent.</summary>
    private CobolExact Exact() => _shape == Shape.Exact ? _exact : CobolExact.FromDec(_value);

    /// <summary>⛔ THE ONE ORDERING OF TWO TEMPORARIES (§2023:8.8.4.2.4 - a comparison of the algebraic values; §2023:15.59.4, §2023:15.63.4, §2023:15.61.4
    /// compare "according to the rules for simple conditions"). Two temporaries neither of which is a wide value are ordered by their
    /// SDIDI values, which ARE their values. ⛔ When EITHER is a wide value the pair is ordered by the EXACT values, never by the wide
    /// value's 34-digit image (kb/Work PB3953): the image orders it only against another 34-digit image, and the other temporary may
    /// hold up to 39 exact digits off that grid (an Int128 value <see cref="OfWide"/> hands to <see cref="OfValue"/>), so
    /// <c>MAX (X * Y + C, X * Y + C + 1000)</c> with the first value just under <see cref="Int128.MaxValue"/> and the second just over
    /// it has the second's image BELOW the first. The exact order is <see cref="CobolExact.Compare"/>, the one the native SDIDI lane's
    /// selections take over the same mixed carriers (kb/Work PB4568), so DISPLAY and an operand select the same argument.</summary>
    private static int Compare(in FunctionValueField a, in FunctionValueField b) =>
        a._shape != Shape.Exact && b._shape != Shape.Exact
            ? CobolDec.Compare(a._value, b._value)
            : CobolExact.Compare(a.Exact(), b.Exact());

    /// <summary>The temporary is a function's binary64 result: it shows the shortest round-trip image and orders by the value that
    /// image names (<c>CobolDec.FromApproximation</c>, the lift of an engine-produced approximation). It is the 8 bytes of a binary64
    /// item (<see cref="ByteLength"/>).</summary>
    public static FunctionValueField OfFloat(double value) =>
        new(Shape.Float, CobolDec.FromApproximation(value), default, sizeof(double), CobolFloat.Display(value));

    /// <summary>A binary64 the program computed (an expression such as <c>F + 0</c>): no approximation, so it orders by its EXACT value
    /// (<see cref="CobolDec.FromBinary64"/> in <paramref name="mode"/>, the lift <c>DecOperandExact</c> gives the same operand on
    /// COMPUTE) and shows the shortest image of its own DISPLAY. A pair holding it is ordered as COMPUTE orders it.</summary>
    public static FunctionValueField OfBinary64(double value, CobolRounding mode) =>
        new(Shape.Float, CobolDec.FromBinary64(value, mode), default, sizeof(double), CobolFloat.Display(value));

    /// <summary>A floating-point ITEM argument: it orders by its exact value (<paramref name="exactValue"/>, the data item's one
    /// SDIDI lift, never an approximation) and shows the image its own DISPLAY writes (a binary32 item's shortest binary32
    /// round-trip, not its binary64 widening). The temporary copies the item, so it has the item's <paramref name="bytes"/>
    /// (4 for a binary32 item, 8 for a binary64 one; a floating-point item is never national, so its positions are its bytes).</summary>
    public static FunctionValueField OfFloatItem(CobolDec exactValue, in NumProfile item, string image, int bytes) =>
        new(Shape.Float, exactValue, item, bytes, image);

    /// <summary>The same description holding the magnitude of the value (§2023:15.7.1 - ABS returns the absolute value of its
    /// argument; GnuCOBOL stores it in a copy of the argument's item, so the sign stays a position of the item and shows
    /// <c>+</c> in a signed one).</summary>
    public FunctionValueField Abs() => _shape switch
    {
        Shape.Float => new(Shape.Float, new CobolDec(Int128.Abs(_value.Sig), _value.Exp), _item, _positions, _bytes, _image![0] == '-' ? _image[1..] : _image),
        Shape.Exact => new(_exact.Sign < 0 ? CobolExact.Negate(_exact) : _exact, _item, _bytes),
        _ => new(_shape, new CobolDec(Int128.Abs(_value.Sig), _value.Exp), _item, _positions, _bytes),
    };

    /// <summary>§2023:15.59.4 1) - the content of the argument having the greatest value; 2) - the leftmost one when several are equal
    /// (a later argument of equal value does not replace it, so the temporary is the first one's item).</summary>
    public static FunctionValueField Max(params FunctionValueField[] arguments)
    {
        CobolIntrinsics.RequireArguments(arguments.Length, "MAX");   // an ALL subscript over no occurrence (§2023:15.3)
        FunctionValueField best = arguments[0];
        for (int i = 1; i < arguments.Length; i++)
            if (Compare(arguments[i], best) > 0) best = arguments[i];
        return best;
    }

    /// <summary>§2023:15.63.4 1) - the content of the argument having the least value; 2) - the leftmost one when several are equal.</summary>
    public static FunctionValueField Min(params FunctionValueField[] arguments)
    {
        CobolIntrinsics.RequireArguments(arguments.Length, "MIN");
        FunctionValueField best = arguments[0];
        for (int i = 1; i < arguments.Length; i++)
            if (Compare(arguments[i], best) < 0) best = arguments[i];
        return best;
    }

    /// <summary>§2023:15.61.4 - an odd count returns the content of the middle argument of the sorted list (equal values keep their
    /// written order, so the first of a run is the earlier one); an even count returns the arithmetic mean of the two middle
    /// values, a value with no item of its own. ⛔ A binary64 mean is a float item's content on both channels (kb/Work PB4924):
    /// <see cref="OfBinary64"/> in <paramref name="mode"/>, ordered by its exact value as <c>CobolIntrinsics.MedianExact</c>'s
    /// <see cref="CobolExact.FromBinary64"/> orders it, never <see cref="OfFloat"/>'s shortest image.</summary>
    public static FunctionValueField Median(CobolRounding mode, params FunctionValueField[] arguments)
    {
        (FunctionValueField low, FunctionValueField high, bool even) = MedianMiddles(arguments);
        if (!even) return low;
        // ⛔ THE MEAN IS THE ONE VALUE EVERY NATIVE CHANNEL GIVES (§2023:15.61.4 2), §2023:15.4.1; kb/Work PB3953, PB4750, PB4826 review R1-2, PB4859),
        // so DISPLAY shows what COMPUTE stores. When a MIDDLE value is binary64 the function's equivalent expression is evaluated in
        // binary64 (docs/CONFORMANCE.md DOC-A.1-92b), as the renderer's float lane and CobolIntrinsics.MedianExact forms it: the mean
        // of the two middles, each as its double; a binary64 elsewhere in the list is no operand of the mean. Otherwise it is the exact sum
        // times the exact one half, whatever form the sum takes (a sum truncated at 34 digits and then halved was 4.5 units off in its 34th
        // digit, and MEDIAN (A B) = (A + B) / 2 was true on COMPUTE alone). Under a standard mode the mean is the SDIDI's
        // (<see cref="MedianStandard"/>).
        if (low._shape == Shape.Float || high._shape == Shape.Float)
            return OfBinary64(CobolIntrinsics.MedianReal(low.Double(), high.Double()), mode);
        return OfExactValue(CobolExact.Mul(CobolExact.Add(low.Exact(), high.Exact()), CobolExact.From(5, 1)));
    }

    /// <summary>This temporary's value as a binary64, nearest-even (a float temporary's own double, whose SDIDI lift round-trips).</summary>
    private double Double() => FloatResultant.FromExact(Exact(), CobolRounding.NearestEven, single: false);

    /// <summary>MEDIAN under a STANDARD arithmetic mode (§2023:15.61.4 2): the mean of the two middle values is the arithmetic expression
    /// <c>(A + B) / 2</c> whose every step is an SDIDI (§2023:15.4.1), rounded by the mode's intermediate rounding
    /// (<paramref name="mode"/>) exactly as <c>CobolIntrinsics.MedianDec</c> forms it for COMPUTE, so DISPLAY shows what COMPUTE stores
    /// (kb/Work PB4827, review R2-1: the exact mean of <see cref="Median"/> showed a 35th digit the standard form cannot hold).</summary>
    public static FunctionValueField MedianStandard(CobolRounding mode, params FunctionValueField[] arguments)
    {
        (FunctionValueField low, FunctionValueField high, bool even) = MedianMiddles(arguments);
        if (!even) return low;
        return OfValue(CobolDec.Div(CobolDec.Add(low._value, high._value, mode), CobolDec.From(2, 0), mode));
    }

    /// <summary>The middle of the stable-sorted list (an odd count: <c>low</c>, and <c>even</c> is false) or its two middles.</summary>
    private static (FunctionValueField Low, FunctionValueField High, bool Even) MedianMiddles(FunctionValueField[] arguments)
    {
        CobolIntrinsics.RequireArguments(arguments.Length, "MEDIAN");
        var sorted = (FunctionValueField[])arguments.Clone();
        for (int i = 1; i < sorted.Length; i++)   // insertion sort: stable, and the lists are short
        {
            FunctionValueField x = sorted[i];
            int j = i - 1;
            while (j >= 0 && Compare(sorted[j], x) > 0) { sorted[j + 1] = sorted[j]; j--; }
            sorted[j + 1] = x;
        }
        int n = sorted.Length;
        return (n & 1) == 1 ? (sorted[n / 2], sorted[n / 2], false) : (sorted[n / 2 - 1], sorted[n / 2], true);
    }

    /// <summary>The characters DISPLAY writes for this temporary. <paramref name="commaMode"/> is the unit's decimal separator
    /// (§2023:12.3.7.4 GR14 a); a binary64 image keeps its period, as a floating-point item's does.</summary>
    public string DeviceText(bool commaMode) => _shape switch
    {
        Shape.Float => _image!,
        Shape.Item => CobolNum.DisplayDecimal(_value.ToUnscaled(_item.FractionDigits, CobolRounding.Truncation), _item, commaMode),
        Shape.Exact => CobolNum.DeviceFunctionText(_exact, _item.Signed, commaMode),
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
