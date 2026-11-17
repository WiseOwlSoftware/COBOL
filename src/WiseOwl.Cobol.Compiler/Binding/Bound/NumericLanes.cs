// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding.Bound;

/// <summary>⛔ THE CARRIER AN ARITHMETIC VALUE IS HELD ON WHILE AN EXPRESSION IS EVALUATED (kb/Work PB5891 review R1-3), as the
/// code generator's <c>NumericRenderer</c> holds it (<c>NumX</c>'s flags) and the type 6 classifier predicts it. The lane decides
/// the PRECISION of every operation: <see cref="Scaled"/>, <see cref="Wide"/> and <see cref="Exact"/> keep every digit (the native
/// Int128 lane, its 256-bit wide form at a compile-time scale, and the run-time-scaled exact form), <see cref="Sdidi"/> rounds each result to the SDIDI's 34 significant digits (§2023:8.8.1.5.2;
/// under native arithmetic DOC-A.1-82's published lane determination), and <see cref="Binary64"/> is IEEE binary64 (numeric design
/// D16). <see cref="Approximation"/> is a binary64 an engine produced (a function's returned value, a non-integer power): beside
/// operands that are not floating-point items it enters the SDIDI (kb/Work PB1641).</summary>
internal enum NumericLane { Scaled, Wide, Exact, Sdidi, Binary64, Approximation }

/// <summary>⛔ THE ONE RULE OF WHICH LANE AN OPERATION TAKES (kb/Work PB5891 review R1-3: the screen and the code generator ask one
/// question). <c>NumericRenderer.CombineCore</c> dispatches on <see cref="Combine"/>'s answer, and the type 6 classifier
/// (<c>IntrinsicArgumentRules.Intermediate</c>) bounds an intermediate's exactness by the precision of the same answer; the bind-time
/// carrier prediction <c>FunctionValueLane</c> folds through it too. <c>NumericLaneDriftTests</c> renders every operand kind beside
/// every other through the renderer and holds the rendered carrier to this rule's answer.</summary>
internal static partial class NumericLanes
{
    /// <summary>The lane of one binary operation (+, -, *, /) over operands on <paramref name="left"/> and <paramref name="right"/>,
    /// with a floating-point receiver (<paramref name="floatReceiver"/>) or none, under a decimal-engine arithmetic
    /// (<paramref name="decimalEngine"/>: STANDARD or STANDARD-DECIMAL, every operation on the SDIDI, §2023:8.8.1.5) or native.
    /// Native, in <c>CombineCore</c>'s order: an engine approximation beside no float item or float receiver is lifted to the SDIDI;
    /// a binary64 operand or a float receiver takes the operation onto binary64 (D16); otherwise a wide or exact operand keeps it
    /// exact (kb/Work PB1900, PB4575); otherwise an SDIDI operand takes it onto the SDIDI (kb/Work PB69); otherwise it is the
    /// Int128 lane.</summary>
    internal static NumericLane Combine(NumericLane left, NumericLane right, bool floatReceiver, bool decimalEngine)
    {
        if (decimalEngine) return NumericLane.Sdidi;
        bool floatItem = floatReceiver || left == NumericLane.Binary64 || right == NumericLane.Binary64;
        if (!floatItem)
        {
            left = Lifted(left);
            right = Lifted(right);
        }
        if (floatReceiver || IsBinary64(left) || IsBinary64(right)) return NumericLane.Binary64;
        if (left is NumericLane.Exact or NumericLane.Wide || right is NumericLane.Exact or NumericLane.Wide) return NumericLane.Exact;
        if (left == NumericLane.Sdidi || right == NumericLane.Sdidi) return NumericLane.Sdidi;
        return NumericLane.Scaled;
    }

    /// <summary>The lane of a power (<c>NumericRenderer.Power</c>, which dispatches on it): the SDIDI under a decimal engine
    /// (§2023:8.8.1.5.4); natively an exponent that is an integer BY VALUE (<see cref="IsIntegerByValue"/>) over a base that is no
    /// binary64 is the EXACT power (kb/Work PB2617, PB4575; the base itself when the exponent is the literal 1 over an Int128-lane
    /// base); every other power is the engine's binary64, a float item's content when an operand is one.
    /// <paramref name="exponentIsIntegerByValue"/> is <see cref="IsIntegerByValue"/>'s answer for the exponent's carrier.</summary>
    internal static NumericLane Power(NumericLane @base, NumericLane exponent, bool exponentIsIntegerByValue, bool literalOne, bool decimalEngine)
    {
        if (decimalEngine) return NumericLane.Sdidi;
        if (!IsBinary64(@base) && exponent == NumericLane.Scaled && exponentIsIntegerByValue)
            return literalOne && @base == NumericLane.Scaled ? NumericLane.Scaled : NumericLane.Exact;
        return @base == NumericLane.Binary64 || exponent == NumericLane.Binary64 ? NumericLane.Binary64 : NumericLane.Approximation;
    }

    /// <summary>The significant decimal digits that identify every IEEE binary64 value (the shortest decimal that round-trips any
    /// double has at most 17), and so the digits an exact value must keep for its conversion to binary64 to be the correctly rounded
    /// double of the value.</summary>
    internal const int Binary64SignificantDigits = 17;

    /// <summary>⛔ THE WORKING SCALE OF AN INTRINSIC FUNCTION'S ARGUMENT (kb/Work PB5893, the coordinator's ruling): the scale of the
    /// context <c>NumericRenderer.ArgumentReceiver</c> renders every argument in, which a nested quotient in the argument reads
    /// (<c>CobolNum.NestedQuotientScale</c>), and which <c>LaneOf</c> therefore reads too (<c>LaneContext.ReceiverScale</c>), so the
    /// renderer's lane assertion and the lane rule cannot disagree. Under a fixed-point receiver it is the receiver's scale. Under a
    /// floating-point receiver the argument is still evaluated exactly in the fixed family (its operands decide its technique, never
    /// the receiver), and its value is delivered to a binary64 or binary32 item: the scale is then at least
    /// <see cref="Binary64SignificantDigits"/>, so a nested quotient keeps 17 fraction digits beside its guard digits (31 in all) and
    /// at least 17 significant digits of any quotient of magnitude 10^-14 or more survive. The value the receiver then takes is the
    /// correctly rounded double of the exact value, as the plain expression gives it: <c>COMPUTE F2 = FUNCTION ABS (A / 3)</c> stores
    /// 2.3333333333333335, as <c>COMPUTE F2 = A / 3</c> does.</summary>
    internal static int ArgumentScale(int receiverScale, bool floatReceiver) =>
        floatReceiver ? Math.Max(receiverScale, Binary64SignificantDigits) : receiverScale;

    /// <summary>⛔ THE ONE QUESTION "IS THIS CARRIER AN INTEGER BY ITS VALUE" (kb/Work PB5891 review R2-1, R2-2; PB272's value-not-spelling
    /// rule): an Int128-lane value whose scale is 0 or NEGATIVE holds an integer (a P-scaled item, PIC 99P, is its unscaled digits
    /// times a power of ten). The renderer's exact power asks it of its exponent, the bind-time carrier prediction states a leaf's
    /// integrality by it, and the type 6 classifier passes it to <see cref="Power"/>, so the three cannot disagree on which power is
    /// exact. Before it the renderer asked for scale 0 exactly, so <c>3 ** U</c> over <c>U PIC 99P VALUE 40</c> took the binary64
    /// approximation and stored 12157665459056929000 for 12157665459056928801.</summary>
    internal static bool IsIntegerByValue(NumericLane lane, int carrierScale) => lane == NumericLane.Scaled && carrierScale <= 0;

    /// <summary>Is the lane binary64 (a float item's content or an engine's approximation)?</summary>
    internal static bool IsBinary64(NumericLane lane) => lane is NumericLane.Binary64 or NumericLane.Approximation;

    /// <summary>The significant digits an operation on <paramref name="lane"/> keeps, or null when it keeps every digit;
    /// <paramref name="decimalCap"/> is the decimal engine's (<c>ArithmeticModes.IntermediateSignificantDigits</c>: 32 for STANDARD,
    /// 34 for STANDARD-DECIMAL), null under native arithmetic, whose SDIDI lane keeps the SDIDI's 34.</summary>
    internal static int? SignificantDigits(NumericLane lane, int? decimalCap) => lane switch
    {
        NumericLane.Scaled or NumericLane.Wide or NumericLane.Exact => decimalCap,
        NumericLane.Sdidi => decimalCap ?? ArithmeticModes.SdidiDigits,
        NumericLane.Binary64 or NumericLane.Approximation => null,
        _ => throw new InvalidOperationException($"no precision for the lane {lane}"),
    };

    private static NumericLane Lifted(NumericLane lane) => lane == NumericLane.Approximation ? NumericLane.Sdidi : lane;
}
