// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using WiseOwl.Cobol.Runtime.Exceptions;

namespace WiseOwl.Cobol.Runtime;

/// <summary>Which ordinal-position rule a position read answers to: the Table 13 exception condition a non-integer
/// position sets. The integrality test is one rule in two clauses: §2023:8.4.2.3.4 GR1b names EC-BOUND-SUBSCRIPT
/// for a subscript, §2023:8.4.3.3.4 GR5 names EC-BOUND-REF-MOD for a reference-modifier leftmost-position or length
/// (kb/Work PB2645). The use is a TYPE argument of <see cref="CobolPosition{TUse}"/>, so the one read is specialized per
/// use at JIT time and raises its own condition with no runtime dispatch.</summary>
public interface IPositionUse
{
    /// <summary>Raise this use's condition for a position whose value <paramref name="shown"/> is not an integer.
    /// Returns only where checking is off and the use's posture continues.</summary>
    static abstract void NotAnInteger(string shown);
}

/// <summary>The two <see cref="IPositionUse"/>s.</summary>
public static class PositionUse
{
    /// <summary>A subscript (§2023:8.4.2.3.4 GR1b, EC-BOUND-SUBSCRIPT), and every other integer the runtime positions
    /// by through the same read: the OCCURS DEPENDING current count, a RECORD VARYING DEPENDING length, a report
    /// DEPENDING count. With checking off a fractional position truncates toward zero and the reference continues.</summary>
    public readonly struct Subscript : IPositionUse
    {
        /// <inheritdoc/>
        public static void NotAnInteger(string shown) =>
            ExceptionState.SubscriptError($"subscript value {shown} is not an integer (ISO 8.4.2.3.4 GR1b)");
    }

    /// <summary>A reference-modifier leftmost-position or length (§2023:8.4.3.3.4 GR5, EC-BOUND-REF-MOD). A fractional
    /// position is the same violation as an out-of-range one and takes the same outcome
    /// (<see cref="ExceptionState.RefModViolation"/>: raised when checking is on, the run unit's termination when it is
    /// off, kb/Work R60).</summary>
    public readonly struct RefMod : IPositionUse
    {
        /// <inheritdoc/>
        public static void NotAnInteger(string shown) =>
            ExceptionState.RefModViolation(
                $"reference-modification position {shown} is not an integer ({ExceptionState.EditionRule("§1985:IV.4.3.8.4.4 GR4", "ISO §2023:8.4.3.3.4 GR5")})");
    }
}

/// <summary>A NON-NUMERIC position operand's value, whatever the position's use — the two carriers only a
/// <c>--permissive</c> position operand has: an index DATA item (an <c>IndexCell</c> is a <c>long</c>) and an
/// alphanumeric / national / boolean item whose digit characters the documented COBOL0844 leniency decodes as an
/// unsigned integer. A NUMERIC position operand never comes here: it reads through
/// <see cref="CobolPosition{TUse}.Read(long, in NumProfile)"/>, which is where §2023:14.6.13.2 GR2 lives.</summary>
public static class CobolPosition
{
    /// <summary>An index data item's occurrence number, as it is.</summary>
    public static long Digits(long value) => value;

    /// <summary>The digit decode of a non-numeric item's characters — <see cref="CobolNum.DigitMagnitude"/>, the
    /// extension COBOL0844's <c>--permissive</c> message promises (kb/Work PB170). Not a rule 2 read: the operand is
    /// not a numeric sending item, so there is no numeric class condition for its content to fail.</summary>
    public static Occurrence Digits(string image) => Occurrence.Of(CobolNum.DigitMagnitude(image), 0);
}

/// <summary>
/// ⛔ THE ONE NUMERIC POSITION READ (kb/Work PB2645): a numeric data item or an arithmetic expression used as an
/// ordinal position, for every use <typeparamref name="TUse"/> names. It was written twice, as
/// <c>CobolTable.Occ</c>/<c>OccValue*</c> and <c>CobolString.RefModPosition</c>/<c>RefModValue*</c>, eight-overload
/// twins that differed only in the condition they raised and that a drift test held together.
/// <para><b>The item read</b> (<see cref="Read(long, in NumProfile)"/>) decodes the item through its OWN profile. One
/// arity for every storage form: the emitted text names the field and C# overload resolution picks the carrier
/// (native <c>long</c>, the character image the post-bind whole-group analysis may select, the <see cref="Int128"/>
/// wide tier, or the unsigned <c>ulong</c> / <see cref="UInt128"/> BINARY-CAPACITY containers of §2023:13.18.60.4
/// GR12) at backend-compile time — kb/Work PB201's carrier set, which <c>PositionCarrierOverloadDriftTests</c> holds
/// to the compiler's list. The profile carries both rules the read owes. (1) §2023:14.6.13.2 GR2: a position operand
/// is item identification, which rule 1 names as checked even inside a class condition (kb/Work PB1117), so the
/// character-image arm is <see cref="CobolNum.ParseImageSending"/>, THE checked fixed-point sending read; the native
/// arms cannot hold invalid content and pay nothing. (2) The integrality of §2023:8.4.2.3.4 GR1b and §2023:8.4.3.3.4
/// GR5: the item's scale is <see cref="NumProfile.FractionDigits"/>, so a scaled item needs no second arity, and its
/// VALUE, never its unscaled storage, is the ordinal (kb/Work PB41).</para>
/// <para>Narrowing SATURATES (<see cref="CobolNum.Position(Int128)"/>): a position past <c>long.MaxValue</c> must stay
/// out of range, or the range condition is lost to a wrap. The result is an <see cref="Occurrence"/>: the saturated
/// <c>long</c> every accessor positions by, with the item's exact value riding along so the diagnostic names what the
/// program holds (kb/Work PB2695, PB2938). The FLOAT carriers are deliberately absent: a float position routes to
/// the D18 §2023:15.4 temp, where the integrality rule is applied once, to the result.</para>
/// <para><b>The expression intake</b> (<see cref="Value(Int128, int)"/>) is the statement pre-operation's, asked on
/// the carrier the expression evaluated on (kb/Work PB1890), so the test reads the EXACT intermediate, never a copy
/// stored at a fixed fraction width (which made <c>T(IX + 0.0000000001)</c> an integer). Three carriers, three names
/// (an integer literal converts to both <c>Int128</c> and <c>double</c>). The result is the EXACT integer the
/// 38-digit position temporary then holds (<see cref="CobolNum.PositionOfWide(Int128, int)"/>), not a <c>long</c>:
/// the item read narrows it, and only that narrowing saturates.</para>
/// </summary>
/// <typeparam name="TUse">The position's use: <see cref="PositionUse.Subscript"/> or <see cref="PositionUse.RefMod"/>.</typeparam>
public static class CobolPosition<TUse> where TUse : struct, IPositionUse
{
    /// <summary>A numeric item held on the native <c>long</c> carrier, as a position.</summary>
    public static Occurrence Read(long unscaled, in NumProfile item) => Scaled(unscaled, item.FractionDigits);

    /// <inheritdoc cref="Read(long, in NumProfile)"/>
    public static Occurrence Read(Int128 unscaled, in NumProfile item) => Scaled(unscaled, item.FractionDigits);

    /// <inheritdoc cref="Read(long, in NumProfile)"/>
    public static Occurrence Read(string image, in NumProfile item) => item.ImageExceedsInt128
        ? Read(CobolNum.ParseImageU128Sending(image, item), item)
        : Scaled(CobolNum.ParseImageSending(image, item), item.FractionDigits);

    /// <inheritdoc cref="Read(long, in NumProfile)"/>
    public static Occurrence Read(ulong unscaled, in NumProfile item) => Scaled(unscaled, item.FractionDigits);

    /// <inheritdoc cref="Read(long, in NumProfile)"/>
    /// <remarks>Read as the UNSIGNED-WIDE value it is — never narrowed through <see cref="Int128"/> first, which a
    /// value at or above 2^127 does not fit — so the integrality test and the position both see the value itself (a
    /// scale that divides it back inside <c>long</c>'s range makes it a valid position).</remarks>
    public static Occurrence Read(UInt128 unscaled, in NumProfile item)
    {
        if (CobolNum.HasFraction(unscaled, item.FractionDigits))
            TUse.NotAnInteger(CobolNum.PlainValue(unscaled, item.FractionDigits));
        return Occurrence.Of(unscaled, item.FractionDigits);
    }

    private static Occurrence Scaled(Int128 unscaled, int scale)
    {
        if (CobolNum.HasFraction(unscaled, scale))
            TUse.NotAnInteger(CobolNum.PlainValue(unscaled, scale));
        return Occurrence.Of(unscaled, scale);
    }

    /// <summary>An arithmetic-expression position evaluated on the exact scaled carrier.</summary>
    public static Int128 Value(Int128 unscaled, int scale)
    {
        if (CobolNum.HasFraction(unscaled, scale))
            TUse.NotAnInteger(CobolNum.PlainValue(unscaled, scale));
        return CobolNum.PositionOfWide(unscaled, scale);
    }

    /// <summary>An arithmetic-expression position evaluated on the standard-decimal carrier.</summary>
    public static Int128 ValueDec(CobolDec value)
    {
        if (value.HasFraction) TUse.NotAnInteger(value.ToDouble().ToString("R", CultureInfo.InvariantCulture));
        return CobolNum.PositionOfWide(value);
    }

    /// <summary>An arithmetic-expression position (or a floating-point item) evaluated on the binary64 carrier.</summary>
    public static Int128 ValueReal(double value)
    {
        if (CobolNum.HasFraction(value)) TUse.NotAnInteger(value.ToString("R", CultureInfo.InvariantCulture));
        return CobolNum.PositionOfWide(value);
    }
}
