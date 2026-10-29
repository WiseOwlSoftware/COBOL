// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime.Exceptions;

namespace WiseOwl.Cobol.Runtime;

/// <summary>
/// The store semantics for a DYNAMIC LENGTH elementary item (ISO/IEC 1989:2023 §8.5.1.10 / §13.18.19) — a
/// variable-length, minimum-length-zero <c>PIC X</c> or <c>PIC N</c> string whose current length varies at
/// runtime and is NEVER space-padded to a fixed width (the difference from <see cref="CobolString"/>). Typed-native:
/// the item IS a native .NET <see cref="string"/> field; this helper carries only the receiving-store rule. A
/// dynamic-length item read as a SENDER, in a comparison, under reference modification, or in FUNCTION
/// LENGTH/BYTE-LENGTH uses the plain string at its current length (§8.5.1.10.4) — through <see cref="Agree"/> when
/// the read's description does not own the storage (a LINKAGE formal, a shared cell-backed member), whose
/// DYNAMIC LENGTH clause the content may not agree with (§14.6.13.2 rule 5, kb/Work PB1118).
/// <para>⛔ EVERY entry point takes the item's MAXIMUM SIZE (§8.5.1.10.1), never "the LIMIT phrase or a sentinel".
/// <see cref="MaxSizeOf"/> is the ONE place that rule is written, and <see cref="MaxLength"/> is the implementor
/// maximum it falls back to — so a dynamic-length item ALWAYS has a real bound and the <c>(int)</c> narrowings
/// below are safe BY CONSTRUCTION. They used to be safe only by inspection, and they were not: a <c>-1</c>
/// "no LIMIT phrase" sentinel skipped the clamp entirely, so a SET SIZE request of 2³²+k silently wrapped to k
/// and a request between <see cref="int.MaxValue"/> and 2³² threw a raw <c>ArgumentOutOfRangeException</c> out of
/// generated code (kb/Work PB463).</para>
/// </summary>
public static class CobolDynString
{
    /// <summary>THE IMPLEMENTOR MAXIMUM number of characters a dynamic-length elementary item can contain —
    /// §8.5.1.10.1's third candidate ("the maximum permitted by the implementor"), §13.18.19.4 GR2's
    /// implementor-defined maximum when no LIMIT phrase is written, and Annex A.1 required-and-documented item 62.
    /// It is the CARRIER's own ceiling: the item IS a .NET <see cref="string"/>, whose maximum length is
    /// <c>0x3FFFFFDF</c> characters — the same "the carrier's headroom IS the implementor maximum" determination
    /// <see cref="CobolDynTable{T}.MaxOccurrences"/> records for a dynamic-capacity table. Documented
    /// in <c>docs/CONFORMANCE.md</c> §7 row <c>DOC-A.1-62</c>, which <c>DynamicLengthTests</c> pins against
    /// this constant so the number and its determination cannot drift apart.</summary>
    public const int MaxLength = 0x3FFF_FFDF;

    /// <summary>§8.5.1.10.1 — THE maximum size of a dynamic-length elementary item, which "is smallest of": "the value
    /// declared in the LIMIT phrase", "the largest integer that can be stored in an item of the usage specified in
    /// the PREFIXED phrase", "the maximum permitted by the implementor". <paramref name="limitPhrase"/> is
    /// integer-1 of the LIMIT phrase (§13.18.19.4 GR2) or null when the phrase is absent; it arrives as an
    /// <see cref="Int128"/> so a literal far past <see cref="int"/> is COMPARED at its written value rather than
    /// narrowed first. <paramref name="prefixedMaximum"/> is the PREFIXED candidate — the largest integer the length
    /// field of the item's dynamic-length-structure-name can hold (§12.3.7.4 GR18), or null when the item names no
    /// structure or its structure has no PREFIXED phrase (kb/Work PB829). ⛔ ONE producer, so every consumer — the
    /// binder, the emitters and the two helpers below — sees a real bound in <c>[0, MaxLength]</c> and nothing needs
    /// a "no LIMIT" special case.</summary>
    public static int MaxSizeOf(Int128? limitPhrase, long? prefixedMaximum = null)
    {
        Int128 bound = MaxLength;
        if (limitPhrase is { } lim && lim < bound) bound = lim;
        if (prefixedMaximum is { } pre && pre < bound) bound = pre;
        return bound <= 0 ? 0 : (int)bound;
    }

    /// <summary>
    /// Store <paramref name="value"/> into a dynamic-length receiver (ISO §8.5.1.10.4): the new content replaces the
    /// old and the new length is the sending length, TRUNCATED ON THE RIGHT to <paramref name="maxSize"/> characters
    /// ("if the maximum length is reached, the value is truncated on the right as necessary"). There is NO padding —
    /// the minimum length is zero (§13.18.19.4 GR1), so a short sender simply yields a short item. A null or
    /// zero-length sender yields length 0. <paramref name="maxSize"/> is the item's §8.5.1.10.1 MAXIMUM SIZE from
    /// <see cref="MaxSizeOf"/> — always a real bound in <c>[0, MaxLength]</c>, never a sentinel.
    /// </summary>
    public static string Store(string? value, int maxSize)
    {
        value ??= "";
        return value.Length > maxSize ? value[..maxSize] : value;
    }

    /// <summary>
    /// The SENDING read of a dynamic-length item whose storage another description may have written (ISO
    /// §14.6.13.2 rule 5, kb/Work PB1118): "When the internal format of a dynamic-length elementary item is not
    /// correctly formed or does not agree with the corresponding DYNAMIC LENGTH clause an EC-DATA-INCOMPATIBLE
    /// exception condition is set to exist." <paramref name="maxSize"/> is THIS description's §8.5.1.10.1 maximum
    /// size; content longer than it does not agree, and with EC-DATA-INCOMPATIBLE checking enabled the fatal
    /// condition is raised through the one raise helper (with checking off the content stands — the result is
    /// undefined, §14.6.13.1.1). The content is returned unchanged either way.
    /// <para>Length is the ONLY agreement fact the carrier can violate: the item IS a managed
    /// <see cref="string"/>, so its "internal format" — a length and that many characters — is always correctly
    /// formed, and the minimum length is zero (§13.18.19.4 GR1), so no content is too short. Content stored
    /// through the SAME description always agrees, because every store truncates to the maximum
    /// (<see cref="Store"/>), which is why only a read through a description that does not own the storage — a
    /// LINKAGE formal, a member of a shared cell-backed area — renders through here.</para>
    /// </summary>
    public static string Agree(string? value, int maxSize)
    {
        value ??= "";
        if (value.Length > maxSize && ExceptionState.DataIncompatibleChecking)
            ExceptionState.DataIncompatibleError(
                $"a dynamic-length item holds {value.Length} characters and its DYNAMIC LENGTH clause allows at most "
                + $"{maxSize} (ISO §14.6.13.2 rule 5)");
        return value;
    }

    /// <summary>
    /// SET [SIZE OF] data-name-3 TO n (ISO §14.9.39 Format 16, GR37–GR39): set the current length of a
    /// dynamic-length item. The amount arrives on the carrier arithmetic-expression-5 EVALUATED on (the
    /// <see cref="double"/> here, a <see cref="CobolDec"/>, or a scaled <see cref="Int128"/>/<see cref="UInt128"/>
    /// below), never narrowed first: the GR37 sign test and GR38's comparison with the maximum size are asked of the
    /// EVALUATED value, so a fractional negative in (−1,0) still takes GR37's leg and 2.5 against a maximum of 2 is
    /// still above it, and the non-integer truncation reads the value in its own domain — a binary64 image of
    /// 2.99999999999999999 is 3.0, whose truncation is the wrong length (kb/Work PB464; the same discipline as
    /// <see cref="CobolTiming.ContinueAfter(double, bool)"/>, kb/Work PB1529).
    /// GR39 — growing initializes the ADDED positions to SPACES (the national space is U+0020 under the Latin-1
    /// identity), NEVER restoring previously-truncated content; shrinking drops the trailing positions.
    /// GR37 — when the evaluated value does not evaluate to a nonnegative number (a NaN is no number) the length is
    /// set to 0 and the nonfatal EC-STORAGE-NOT-AVAIL is raised; GR37's second sentence, "truncated to the nearest
    /// integer", is applied as TRUNCATION TOWARD ZERO and nothing is raised for the discarded fraction — the
    /// standard's own TRUNCATION rounding mode is defined by the same words (§14.7.4.3 GR10 "rounded to the nearest
    /// value nearer to zero"; docs/CONFORMANCE.md §7, the SET SIZE OF determination). GR38 — a value above
    /// <paramref name="maxSize"/> (the §8.5.1.10.1 maximum size of data-name-3) is clamped to that maximum and the
    /// same nonfatal EC-STORAGE-NOT-AVAIL is raised. The stored value is identical whether or not checking is on;
    /// checking only governs the observable exception status and the §14.6.13.1.4 #3 declarative.
    /// <para>GR38's THIRD leg is implemented, not assumed away: "If the amount of storage required to expand the
    /// size of data-name-3 is not available, the size of data-name-3 is not changed, and an EC-STORAGE-NOT-AVAIL
    /// exception condition is set to exist." <see cref="MaxLength"/> is the carrier's ceiling, not a promise that
    /// the host can hand over two gigabytes, so the growth allocation is the place where "not available" becomes
    /// observable — an <see cref="OutOfMemoryException"/> there returns the item UNCHANGED with the condition set,
    /// rather than escaping into generated code. The condition is raised ONCE per statement: a request that already
    /// took the clamp leg has set it to exist and does not set it twice (kb/Work PB463).</para>
    /// <para>⛔ "Checking was enabled at this statement" used to arrive as a POSITIONAL argument, which made this the
    /// one nonfatal raise site that could not reach a USE declarative: §14.6.13.1.1's rule is applied by
    /// <c>ExceptionEngine</c> per (ambient flag, exception-name) pair, and a site outside that pair also sits outside
    /// the §14.6.13.1.4 #3 selection the pair now runs (kb/Work PB367b).</para>
    /// </summary>
    public static string SetSize(string? current, double newLen, int maxSize)
    {
        // A NaN is not a number, so it "does not evaluate to a nonnegative number" — GR37's leg — yet it fails BOTH
        // comparisons a plain `< 0` / `> max` pair would make and used to fall to a narrowing that saturates it to 0
        // SILENTLY: the length came out right and the condition was never set (kb/Work PB2647). The infinities need
        // no screen: −∞ is below zero (GR37) and +∞ is above every maximum size (GR38's clamp).
        bool negative = double.IsNaN(newLen) || newLen < 0.0;
        long whole = CobolNum.PositionOf(newLen);
        var leg = Classify(negative, whole, CobolNum.HasFraction(newLen), maxSize);
        if (leg != Leg.InRange) RaiseLeg(leg, newLen.ToString("R", System.Globalization.CultureInfo.InvariantCulture), maxSize);
        return Resize(current, leg, whole, maxSize);
    }

    /// <summary>The <see cref="SetSize(string?, double, int)"/> twin for an amount that evaluated as a standard
    /// decimal (§8.8.1.5 SDIDI): the sign is the significand's and the whole part is the saturating truncation
    /// <see cref="CobolNum.PositionOf(CobolDec)"/> reads, so 10^40 is above every maximum size and not the zero its
    /// 38 low-order digits make.</summary>
    public static string SetSize(string? current, CobolDec newLen, int maxSize)
    {
        long whole = CobolNum.PositionOf(newLen);
        var leg = Classify(newLen.Sig < 0, whole, newLen.HasFraction, maxSize);
        if (leg != Leg.InRange)
            RaiseLeg(leg, newLen.ToDouble().ToString("R", System.Globalization.CultureInfo.InvariantCulture), maxSize);
        return Resize(current, leg, whole, maxSize);
    }

    /// <summary>The <see cref="SetSize(string?, double, int)"/> twin for an amount held as a scaled integer (a
    /// fixed-point item or expression: <paramref name="unscaled"/> × 10^-<paramref name="scale"/>). The sign and the
    /// fraction are the exact value's, never a binary64 image's.</summary>
    public static string SetSize(string? current, Int128 unscaled, int scale, int maxSize)
    {
        long whole = CobolNum.PositionOf(unscaled, scale);
        var leg = Classify(unscaled < 0, whole, CobolNum.HasFraction(unscaled, scale), maxSize);
        if (leg != Leg.InRange) RaiseLeg(leg, CobolNum.PlainValue(unscaled, scale), maxSize);
        return Resize(current, leg, whole, maxSize);
    }

    /// <summary>The <see cref="SetSize(string?, Int128, int, int)"/> twin for an UNSIGNED-WIDE amount (a 16-byte
    /// unsigned COMP-5 item's [0, 2^128) range): never negative, and every value past the <see cref="long"/> range is
    /// above every maximum size, where an <see cref="Int128"/> cast of a value at or above 2^127 would go negative.</summary>
    public static string SetSize(string? current, UInt128 unscaled, int scale, int maxSize)
    {
        long whole = CobolNum.PositionOf(unscaled, scale);
        var leg = Classify(false, whole, CobolNum.HasFraction(unscaled, scale), maxSize);
        if (leg != Leg.InRange) RaiseLeg(leg, CobolNum.PlainValue(unscaled, scale), maxSize);
        return Resize(current, leg, whole, maxSize);
    }

    /// <summary>Which of GR37 / GR38 the evaluated amount takes. <paramref name="whole"/> is the toward-zero
    /// truncation GR37's second sentence calls for, saturated to <see cref="long"/> so an out-of-range request stays
    /// out of range. GR37's test is on the EVALUATED value (<paramref name="negative"/>); so is GR38's "greater than
    /// the maximum size": the truncation exceeds the maximum, or, at the maximum, a nonzero fraction lifts the
    /// evaluated value above it.</summary>
    private static Leg Classify(bool negative, long whole, bool hasFraction, int maxSize) =>
        negative ? Leg.NotNonnegative
        : whole > maxSize || (whole == maxSize && hasFraction) ? Leg.AboveMaximum
        : Leg.InRange;

    private enum Leg { InRange, NotNonnegative, AboveMaximum }

    /// <summary>The nonfatal EC-STORAGE-NOT-AVAIL of GR37 or GR38's second sentence. <paramref name="shown"/> is the
    /// amount as the exception detail names it, formatted by the caller only when a leg raises.</summary>
    private static void RaiseLeg(Leg leg, string shown, int maxSize)
    {
        if (leg == Leg.NotNonnegative)
            ExceptionState.StorageNotAvailError(
                $"SET SIZE: the evaluated length {shown} is not a nonnegative number (ISO §14.9.39.4 Format 16 GR37)");
        else
            ExceptionState.StorageNotAvailError(
                $"SET SIZE: the evaluated length {shown} exceeds the item's maximum size {maxSize} "
                + "(ISO §14.9.39.4 Format 16 GR38 / §8.5.1.10.1)");
    }

    /// <summary>GR37 – GR39 once the leg is known: length 0 (GR37), the maximum (GR38), or the truncated whole
    /// part; shrinking drops the trailing positions, growing adds spaces (GR39) unless the host cannot hand over the
    /// storage (GR38 third sentence).</summary>
    private static string Resize(string? current, Leg leg, long whole, int maxSize)
    {
        current ??= "";
        // 0 ≤ whole ≤ maxSize ≤ MaxLength on the in-range leg, so the narrowing is exact — GR37's toward-zero
        // truncation of a non-integer value, and nothing else. This is the whole point of MaxSizeOf: the bound is
        // applied BEFORE any narrowing, on the value as written.
        int n = leg switch { Leg.NotNonnegative => 0, Leg.AboveMaximum => maxSize, _ => (int)whole };
        if (n <= current.Length) return current[..n];                    // shrink — drop the trailing positions
        try
        {
            return current + new string(' ', n - current.Length);        // GR39 — the added positions are spaces
        }
        catch (OutOfMemoryException)
        {
            // GR38 third sentence — the host cannot hand over the storage the expansion needs: the size is NOT
            // changed, and the condition is set to exist. A request that already took a leg (the clamp) has set it
            // to exist; raising again would run the USE declarative twice for one statement (kb/Work PB463).
            if (leg == Leg.InRange)
                ExceptionState.StorageNotAvailError(
                    $"SET SIZE: the storage required to expand the item to {n} characters is not available "
                    + "(ISO §14.9.39.4 Format 16 GR38)");
            return current;
        }
    }
}
