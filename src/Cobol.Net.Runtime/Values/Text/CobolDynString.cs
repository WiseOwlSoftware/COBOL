// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
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
    /// dynamic-length item. GR39 — growing initializes the ADDED positions to SPACES (the national space is
    /// U+0020 under the Latin-1 identity), NEVER restoring previously-truncated content; shrinking drops the
    /// trailing positions. GR38 — a value above <paramref name="limit"/> clamps to it; GR37 — a value below zero
    /// yields length 0. (The nonfatal EC-STORAGE-NOT-AVAIL that also becomes set in the clamp/negative cases when
    /// checking is enabled is a checking-off no-op — the stored value is identical either way — so it is a staged
    /// follow-on.) <paramref name="limit"/> below 0 means no LIMIT phrase — the implementor maximum, here unbounded.
    /// A non-integer <paramref name="newLen"/> is already truncated toward zero by the integer-typed caller (GR37).
    /// </summary>
    public static string SetSize(string? current, long newLen, int limit)
    {
        current ??= "";
        long n = newLen < 0 ? 0 : limit >= 0 && newLen > limit ? limit : newLen;
        return n <= current.Length ? current[..(int)n] : current + new string(' ', (int)n - current.Length);
    }
}
