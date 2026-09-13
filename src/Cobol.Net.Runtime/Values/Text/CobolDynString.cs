// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Runtime;

/// <summary>
/// The store semantics for a DYNAMIC LENGTH elementary item (ISO/IEC 1989:2023 §8.5.1.10 / §13.18.19) — a
/// variable-length, minimum-length-zero <c>PIC X</c> or <c>PIC N</c> string whose current length varies at
/// runtime and is NEVER space-padded to a fixed width (the difference from <see cref="CobolString"/>). Typed-native:
/// the item IS a native .NET <see cref="string"/> field; this helper carries only the receiving-store rule. A
/// dynamic-length item read as a SENDER, in a comparison, under reference modification, or in FUNCTION
/// LENGTH/BYTE-LENGTH uses the plain string at its current length — no helper needed (§8.5.1.10.4).
/// </summary>
public static class CobolDynString
{
    /// <summary>
    /// Store <paramref name="value"/> into a dynamic-length receiver (ISO §8.5.1.10.4): the new content replaces the
    /// old and the new length is the sending length, TRUNCATED ON THE RIGHT to <paramref name="limit"/> characters
    /// ("if the maximum length is reached, the value is truncated on the right as necessary"). There is NO padding —
    /// the minimum length is zero (§13.18.19.4 GR1), so a short sender simply yields a short item. A
    /// <paramref name="limit"/> below zero means "no explicit LIMIT phrase" — the implementor-defined maximum
    /// (§13.18.19.4 GR2), here unbounded within the .NET string limit. A null or zero-length sender yields length 0.
    /// </summary>
    public static string Store(string? value, int limit)
    {
        value ??= "";
        return limit >= 0 && value.Length > limit ? value[..limit] : value;
    }

    /// <summary>
    /// SET [SIZE OF] data-name-3 TO n (ISO §14.9.39 Format 16, GR37–GR39): set the current length of a
    /// dynamic-length item. <paramref name="newLen"/> is the arithmetic-expression-5 value at FULL precision (the
    /// GR37 sign test precedes the GR38 clamp and the non-integer truncation, so a fractional negative in (−1,0)
    /// must be caught before the toward-zero truncation — hence a <see cref="double"/>, mirroring
    /// <see cref="CobolTiming.ContinueAfter"/>). GR39 — growing initializes the ADDED positions to SPACES (the
    /// national space is U+0020 under the Latin-1 identity), NEVER restoring previously-truncated content; shrinking
    /// drops the trailing positions. GR37 — when the evaluated value does not evaluate to a nonnegative number the
    /// length is set to 0 and the nonfatal EC-STORAGE-NOT-AVAIL is raised; a non-integer nonnegative value is
    /// truncated toward zero. GR38 — a value above <paramref name="limit"/> (the maximum size of data-name-3) is
    /// clamped to that maximum and the same nonfatal EC-STORAGE-NOT-AVAIL is raised. (GR38's third
    /// leg — the requested storage not being physically available — is N/A under the .NET managed heap: a within-LIMIT
    /// length always allocates.) The stored value is identical whether or not checking is on; checking only governs the
    /// observable exception status and the §14.6.13.1.4 #3 declarative. <paramref name="limit"/> below 0 means no
    /// LIMIT phrase — the implementor maximum, here unbounded. (The integer-2 literal form is compile-time bounded by
    /// SR34, so these runtime raises pertain to the arithmetic-expression-5 form.)
    /// <para>⛔ "Checking was enabled at this statement" used to arrive as a POSITIONAL argument, which made this the
    /// one nonfatal raise site that could not reach a USE declarative: §14.6.13.1.1's rule is applied by
    /// <c>ExceptionEngine</c> per (ambient flag, exception-name) pair, and a site outside that pair also sits outside
    /// the §14.6.13.1.4 #3 selection the pair now runs (kb/Work PB367b).</para>
    /// </summary>
    public static string SetSize(string? current, double newLen, int limit)
    {
        current ??= "";
        long n;
        if (newLen < 0.0)
        {
            n = 0;                                                       // GR37 — not nonnegative → length 0
            ExceptionState.StorageNotAvailError(
                $"SET SIZE: the evaluated length {newLen} is not a nonnegative number (ISO §14.9.39 Format 16 GR37)");
        }
        else if (limit >= 0 && newLen > limit)
        {
            n = limit;                                                  // GR38 — above the maximum → clamp to it
            ExceptionState.StorageNotAvailError(
                $"SET SIZE: the evaluated length {newLen} exceeds the item's maximum size {limit} (ISO §14.9.39 Format 16 GR38)");
        }
        else
        {
            n = (long)newLen;                                           // GR37 — non-integer truncates toward zero
        }
        return n <= current.Length ? current[..(int)n] : current + new string(' ', (int)n - current.Length);
    }
}
