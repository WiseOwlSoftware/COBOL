// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.CodeGen.Emit;

/// <summary>
/// ⛔ WHAT DISPLAY WRITES FOR A NUMERIC FUNCTION'S RETURNED VALUE under <c>--numeric-display decimal</c> (kb/Work PB3514;
/// ISO/IEC 1989:2023 §15.4 - "The evaluation of a function produces a returned value in a temporary elementary data item";
/// §15.4.1 - "When native arithmetic is in effect, the characteristics and representation of the returned value are defined
/// by the implementor"; §14.9.11.4 GR1; docs/CONFORMANCE.md DOC-A.1-92). A function's temporary is a data item, DISPLAY shows a
/// data item by its description (<c>CobolNum.DisplayDecimal</c>, DOC-A.1-56), and the standard leaves the description to
/// the implementor - so the precedence of CLAUDE.md rule 1 gives it to GnuCOBOL 3.2.0, whose temporary is
/// <list type="bullet">
///   <item>for ABS, MAX, MIN, an odd count's MEDIAN and a single argument's MEAN (<see cref="FunctionDeviceForm.Argument"/>), a COPY of
///         the argument whose content is returned: <c>FUNCTION MAX(U S)</c> shows what DISPLAY of the greater item shows;</item>
///   <item>for DATE-OF-INTEGER and DAY-OF-INTEGER (<see cref="FunctionDeviceForm.FixedDigits"/>), an unsigned DISPLAY integer of eight
///         and seven digits;</item>
///   <item>for every other exact function, an item its VALUE describes (<c>CobolNum.DeviceFunctionText</c>).</item>
/// </list>
/// A binary64 result keeps the image a floating-point item has, and a call the binder FOLDED into a literal shows as that
/// literal when GnuCOBOL folds the same call (<see cref="FunctionDeviceForm.Literal"/>: LENGTH, BYTE-LENGTH) and as the value
/// of the call it replaced otherwise (the ALGEBRAIC family).
/// <para>⛔ DISPLAY ONLY, like <see cref="OperandText.AsDeviceString"/> that asks it: <see cref="OperandText.AsString"/> serves MOVE, STRING
/// and the comparisons, whose text §14.9.25.4 GR6 fixes, and keeps DOC-A.1-92's literal form there.</para>
/// <para>The arguments are evaluated ONCE: a selecting function builds one <c>FunctionValueField</c> per argument (its value and
/// its description) and the runtime picks, so a nested function, a RANDOM argument and a table(ALL) are each read a single time.</para>
/// </summary>
internal sealed class FunctionDeviceText(NumericRenderer num)
{
    /// <summary>The C# string expression DISPLAY evaluates for <paramref name="op"/> when it is a numeric function's returned value
    /// (a call, the binder's run-time LENGTH sum, or a folded call GnuCOBOL does not fold), else null.</summary>
    public static string? Of(BoundOperand op, NumericRenderer num) => op switch
    {
        BoundComputedOperand { Expr: BoundIntrinsicCall { ResultCategory: PicCategory.Numeric } ic } => new FunctionDeviceText(num).CallText(ic),
        // FUNCTION LENGTH / BYTE-LENGTH of a variable-length group: the binder's arithmetic sum, the run-time LENGTH GnuCOBOL does not fold.
        BoundComputedOperand { Expr: BoundBinary sum } => new FunctionDeviceText(num).ValueText(num.Render(sum, ReceiverContext.None)),
        // A folded value written as a floating-point literal (the algebraic extreme of a floating-point item: 999E+999) is a
        // floating-point value, which keeps the image a floating-point literal has.
        BoundNumericLiteral { FunctionValue: true, FoldedFunction: { } folded } lit when !FoldsToLiteral(folded) && !IsFloatingPoint(lit.Text) =>
            new FunctionDeviceText(num).ValueText(num.AsNum(lit, ReceiverContext.None)),
        _ => null,
    };

    private static bool IsFloatingPoint(string literal) => literal.AsSpan().IndexOfAny('E', 'e') >= 0;

    /// <summary>Does GnuCOBOL fold this function's call into a literal, so that DISPLAY shows the literal (LENGTH, BYTE-LENGTH)?</summary>
    private static bool FoldsToLiteral(string function) =>
        IntrinsicCatalog.TryGet(function, out IntrinsicSig sig) && sig.DeviceForm == FunctionDeviceForm.Literal;

    private bool Comma => num.UnitDecimalPointIsComma;

    private string CallText(BoundIntrinsicCall ic) => Describes(ic)
        ? RuntimeApi.FunctionFieldText(CallField(ic), Comma)
        : ValueText(num.Render(ic, ReceiverContext.None));

    /// <summary>Does the call's temporary come from its arguments' items rather than from its value alone?</summary>
    private static bool Describes(BoundIntrinsicCall ic) => ic.Sig.DeviceForm switch
    {
        FunctionDeviceForm.FixedDigits => true,
        FunctionDeviceForm.Argument => ic.Sig.RuntimeMethod != "MeanScaled" || SingleArgument(ic),
        _ => false,
    };

    /// <summary>MEAN of one argument returns that argument's content; of several, a computed value.</summary>
    private static bool SingleArgument(BoundIntrinsicCall ic) =>
        ic.Args is [var only] && only is not BoundFieldOperand { Place: TableAllPlace };

    /// <summary>The text of a value whose temporary the value alone describes.</summary>
    /// <remarks>INTAKE(EXACT) - the value is read as the renderer carries it, a scaled integer or an SDIDI, never lifted or rounded.</remarks>
    private string ValueText(NumX x)
    {
        if (x.Real) return RuntimeApi.FloatDisplay(x.Expr);
        if (x.Dec) return RuntimeApi.FunctionDeviceTextDec(x.Expr, Comma);
        if (x.Wide || x.U) return RuntimeApi.FunctionDeviceTextDec(num.DecOperand(x), Comma);
        return RuntimeApi.FunctionDeviceTextScaled(x.Expr, x.Scale, Comma);
    }

    /// <summary>The <c>FunctionValueField</c> a call evaluates to, as a C# expression.</summary>
    private string CallField(BoundIntrinsicCall ic)
    {
        if (Describes(ic))
        {
            switch (ic.Sig.DeviceForm, ic.Sig.RuntimeMethod)
            {
                case (FunctionDeviceForm.FixedDigits, _):
                    return RuntimeApi.FunctionFieldOfDigits(num.DecOperand(num.Render(ic, ReceiverContext.None)), ic.Sig.DeviceDigits);
                case (_, "AbsScaled"): return RuntimeApi.FunctionFieldAbs(ArgField(ic.Args[0]));
                case (_, "MaxScaled"): return RuntimeApi.FunctionFieldMax(ArgumentList(ic));
                case (_, "MinScaled"): return RuntimeApi.FunctionFieldMin(ArgumentList(ic));
                case (_, "MedianScaled"): return RuntimeApi.FunctionFieldMedian(ArgumentList(ic));
                case (_, "MeanScaled"): return ArgField(ic.Args[0]);
            }
        }
        return ValueField(num.Render(ic, ReceiverContext.None));
    }

    /// <summary>A value that describes its own temporary, as a field.</summary>
    /// <remarks>INTAKE(LIFTED) - an exact scaled value is lifted to the SDIDI through the one operand lift; an SDIDI and a binary64 pass through.</remarks>
    private string ValueField(NumX x) =>
        x.Real ? RuntimeApi.FunctionFieldOfFloat(x.Expr)
        : x.Dec ? RuntimeApi.FunctionFieldOfValue(x.Expr)
        : RuntimeApi.FunctionFieldOfValue(num.DecOperand(x));

    /// <summary>The argument list of a selecting function: the comma list, or one array expression when a table(ALL) is among
    /// the arguments (each of its elements is an argument).</summary>
    private string ArgumentList(BoundIntrinsicCall ic) =>
        num.Intrinsics.ArgArray(ic, 0, RuntimeApi.FunctionFieldType, ArgField)
        ?? string.Join(", ", ic.Args.Select(ArgField));

    /// <summary>One argument as the field the returned value would copy.</summary>
    /// <remarks>INTAKE(EXACT) - the argument's value and description are read once, unlanded and unrounded within the SDIDI's lift.</remarks>
    private string ArgField(BoundOperand a)
    {
        switch (a)
        {
            case BoundComputedOperand { Expr: BoundIntrinsicCall { ResultCategory: PicCategory.Numeric } nested }:
                return CallField(nested);
            case BoundNumericLiteral lit:
                return LiteralField(lit);
            // A whole data item (a reference-modified slice is no declared item, Place.DenotedItem); a floating-point item orders by
            // its exact value (the one float-item lift) and shows its own DISPLAY image.
            case BoundFieldOperand { Place: { DenotedItem: not null, Item.IsGroup: false, Item.Pic: { Category: PicCategory.Numeric } pic } place }:
                NumX read = num.AsNum(a, ReceiverContext.None);
                return pic.IsFloat
                    ? RuntimeApi.FunctionFieldOfFloatItem(num.DecOperand(read), OperandText.AsTransferString(a, num, "DISPLAY of"))
                    : RuntimeApi.FunctionFieldOfItem(num.DecOperand(read), place.Item.ProfileName);
            default:
                // An arithmetic expression (GnuCOBOL evaluates it into a temporary), a register, an index: described by its value.
                return RuntimeApi.FunctionFieldOfValue(num.DecOperand(num.AsNum(a, ReceiverContext.None)));
        }
    }

    /// <summary>A numeric literal argument: GnuCOBOL's literal field has a digit position for every digit written (a leading
    /// zero too), the fraction digits written, and a sign position when a sign was written. A floating-point literal is described
    /// by its value. <see cref="BoundNumericLiteral.Text"/> is always the canonical dot-decimal spelling.</summary>
    private string LiteralField(BoundNumericLiteral lit)
    {
        string value = num.DecOperand(num.AsNum(lit, ReceiverContext.None));
        string text = lit.Text;
        if (IsFloatingPoint(text)) return RuntimeApi.FunctionFieldOfValue(value);
        bool signed = text.Length > 0 && text[0] is '+' or '-';
        string magnitude = signed ? text[1..] : text;
        int point = magnitude.IndexOf('.');
        int digits = magnitude.Count(char.IsAsciiDigit);
        int scale = point < 0 ? 0 : magnitude.Length - point - 1;
        return RuntimeApi.FunctionFieldOfLiteral(value, digits, scale, signed);
    }
}
