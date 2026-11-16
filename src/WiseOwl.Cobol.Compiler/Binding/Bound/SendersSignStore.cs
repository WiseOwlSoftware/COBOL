// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Binding.Bound;

/// <summary>One numeric store through the rules of the MOVE statement whose receiver can hold a sign apart from its value:
/// the sending operand as the store takes it (<see cref="MoveStore.Sender"/>) and the elementary receiving item. Collected by
/// <c>UsageCollectionPass</c> into <see cref="DataBindState.NumericStores"/> for <c>StorageFormPass</c>'s computed promotion
/// (kb/Work PB5852; DESIGN-data-model §2.1.1). Equal by the IDENTITY of the two (<see cref="Identity"/>): one sender node and
/// one item are one store, whatever their structural equality says.</summary>
public readonly record struct NumericStore(BoundOperand Sender, DataItem Receiver)
{
    /// <summary>Reference identity of both halves, the fact set's comparer.</summary>
    public static readonly IEqualityComparer<NumericStore> Identity = new IdentityComparer();

    private sealed class IdentityComparer : IEqualityComparer<NumericStore>
    {
        public bool Equals(NumericStore a, NumericStore b) =>
            ReferenceEquals(a.Sender, b.Sender) && ReferenceEquals(a.Receiver, b.Receiver);

        public int GetHashCode(NumericStore s) =>
            HashCode.Combine(RuntimeHelpers.GetHashCode(s.Sender), RuntimeHelpers.GetHashCode(s.Receiver));
    }
}

/// <summary>⛔ THE 1985 AND 2002 SIGN OF A NUMERIC STORE, WRITTEN ONCE (kb/Work PB5852; DESIGN-data-model §2.1.1).
/// §1985:VI.6.19.4 GR4b1 and §2002:14.8.24.3 GR4b1: "When a signed numeric item is the receiving item, the sign of the sending
/// operand is placed in the receiving item." The sign is the OPERAND's, so a sender whose digits all truncate away leaves a
/// negative zero; from 2014 it is "the sign of the numeric value" (§2014:14.9.24.3 GR6d2a), and nothing here applies.
/// <para>Two consumers ask these predicates and neither restates them: <c>StorageFormPass</c> promotes the native sign holders
/// that can be handed a negative zero to their image (<see cref="MayHandNegativeZero"/>, the computed set), and
/// <c>MoveEmitter.ConvertSource</c> writes the sender's sign into an image-carried sign holder
/// (<see cref="KeepsSendersSign"/>).</para></summary>
internal static class SendersSignStore
{
    /// <summary>Does the edition place the SENDING OPERAND's sign? True at 1985 and 2002 (the
    /// <see cref="DialectBehavior.MoveSignIsTheValuesSign"/> row, from 2014).</summary>
    public static bool Applies(int editionYear) =>
        !DialectBehaviors.IsActive(DialectBehavior.MoveSignIsTheValuesSign, editionYear);

    /// <summary>A SIGN HOLDER: an elementary signed numeric item of the zoned byte form (USAGE DISPLAY or NATIONAL), whose
    /// sign is a character apart from its digits and so can be negative over a zero magnitude. A BINARY item has no negative
    /// zero, and a PACKED-DECIMAL item's sign nibble is documented as the value's sign (the representation §1985:VI.5.14.4
    /// GR9 leaves to the implementor), so neither is one.</summary>
    public static bool IsSignHolder(DataItem item) =>
        !item.IsGroup && item.Pic is { Category: PicCategory.Numeric, Signed: true, IsFloat: false, ByteForm: NumericByteForm.Zoned };

    /// <summary>Can the sending operand be negative? A literal written with a minus sign and a non-zero digit (a literal
    /// is its value, "the algebraic quantity represented by the characters" (§1985:IV.4.2.2.2.2 GR4), and zero has no sign to
    /// place, so <c>-0</c> stores a positive zero as kb/Work PB2508 has it), a signed numeric
    /// item, a numeric-edited item whose mask holds a sign (its de-edited value "which may be signed", GR4b), and a numeric
    /// function-identifier's value. An unsigned sender generates "a positive sign" (GR4b1), and a
    /// figurative ZERO or an alphanumeric sender is unsigned.</summary>
    public static bool SenderMayBeNegative(BoundOperand sender) => sender switch
    {
        BoundNumericLiteral lit => lit.Text.StartsWith('-') && NonZeroDigitExponents(lit.Text).Any(),
        // A reference-modified sender is alphanumeric (§1985:VI.6.19.4 GR4b3's unsigned integer), whatever its item.
        BoundFieldOperand { Place.DenotedItem: not null, Place.Item.Pic: { Category: PicCategory.Numeric or PicCategory.NumericEdited, HoldsSign: true } } => true,
        BoundComputedOperand c => c.IsNumericFunctionValue,
        _ => false,
    };

    /// <summary>The emitter's question: does this store write the SENDER's sign into <paramref name="receiver"/>'s image?
    /// Only an image-carried sign holder holds a negative zero; a native one holds a value, and the computed set
    /// (<see cref="MayHandNegativeZero"/>) leaves native only the receivers no store can hand one.</summary>
    public static bool KeepsSendersSign(int editionYear, BoundOperand sender, DataItem receiver) =>
        Applies(editionYear) && IsSignHolder(receiver) && receiver.StoreAsImage && SenderMayBeNegative(sender);

    /// <summary>⛔ THE COMPUTED SET'S MEMBERSHIP TEST: can this store hand <paramref name="receiver"/> a negative zero? The
    /// sender may be negative AND either it can lose every non-zero digit in the store (<see cref="MayTruncateToZero"/>) or it
    /// can itself hold a negative zero (<see cref="HoldsNegativeZero"/>: the closure, since a negative zero moved on keeps the
    /// sending operand's sign). Any other store into a sign holder stores a zero only from a zero value, whose sign is
    /// positive, so its receiver stays native.</summary>
    public static bool MayHandNegativeZero(BoundOperand sender, DataItem receiver) =>
        IsSignHolder(receiver) && SenderMayBeNegative(sender)
        && (MayTruncateToZero(sender, receiver) || sender is BoundFieldOperand f && HoldsNegativeZero(f.Place.Item));

    /// <summary>Can <paramref name="item"/> hold a negative zero now? An IMAGE-carried sign holder: its characters can be
    /// negative over a zero magnitude, whether a store of the arm, a READ, a group move or INSPECT left them so. Reads the
    /// storage form, so it is asked only after <c>StorageFormPass</c> classified the item.</summary>
    public static bool HoldsNegativeZero(DataItem item) =>
        IsSignHolder(item) && (item.StoreAsImage || item.Storage is StorageForm.TierBWindow);

    /// <summary>Can the store lose every non-zero digit of the sender? The receiver keeps the digit positions of decimal
    /// exponent <c>-Scale</c> through <c>Digits - Scale - 1</c> (alignment by decimal point, §1985:VI.6.19.4 GR4b, with
    /// truncation at either end); a sender with a digit position outside that window can store zero from a non-zero value. A
    /// literal is decided on its own non-zero digits; a function-identifier's value has no picture and a numeric-edited
    /// sender's digit positions are its mask's, so each may.</summary>
    public static bool MayTruncateToZero(BoundOperand sender, DataItem receiver)
    {
        var r = receiver.Pic!;
        int lo = -r.Scale, hi = r.Digits - r.Scale - 1;
        return sender switch
        {
            BoundNumericLiteral lit => lit.Text.AsSpan().IndexOfAny('E', 'e') >= 0
                || NonZeroDigitExponents(lit.Text).All(e => e < lo || e > hi),
            BoundFieldOperand { Place.Item.Pic: { Category: PicCategory.Numeric } s } => -s.Scale < lo || s.UnscaledDigitBound - s.Scale - 1 > hi,
            _ => true,
        };
    }

    /// <summary>The decimal exponent of each non-zero digit of a fixed-point literal's text (<c>-100</c> → 2; <c>-0.4</c> → -1).</summary>
    private static IEnumerable<int> NonZeroDigitExponents(string text)
    {
        var body = text.AsSpan().TrimStart("+-");
        int point = body.IndexOf('.');
        int integerDigits = point < 0 ? body.Length : point;
        var exponents = new List<int>();
        int position = 0;
        foreach (char c in body)
        {
            if (c == '.') continue;
            if (char.IsAsciiDigit(c) && c != '0') exponents.Add(integerDigits - 1 - position);
            position++;
        }
        return exponents;
    }
}
