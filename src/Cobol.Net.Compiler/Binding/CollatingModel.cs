// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding;

/// <summary>
/// The PROGRAM COLLATING SEQUENCE model (ISO §12.3.6 / §12.3.7 ALPHABET, GR7 k): a 256-entry POSITION table over
/// the Latin-1 native set (index = native char code, value = 0-based ordinal position in the user sequence), plus
/// the PCS-derived figurative extremes. Built once per alphabet by the SPECIAL-NAMES binder; an identity table
/// (NATIVE / STANDARD-1 / STANDARD-2 — ISO/IEC 646 order IS the native order here) normalizes to "no table"
/// at the PCS resolution so the native fast path costs nothing.
/// </summary>
/// <param name="Positions">Native char code → 0-based collating position. ALSO members share one position
/// (§12.3.7 GR7 k6); unspecified characters take DISTINCT ascending positions above the highest specified, in
/// native relative order (GR7 k3 — never a shared bucket; ORD over them must stay distinct).</param>
/// <param name="HighValue">The runtime HIGH-VALUE character under this sequence (§12.3.7 GR8 + §8.3.3.6 GR6/7):
/// the character at the HIGHEST position; a tie (an ALSO group at the top) takes the LAST character specified.</param>
/// <param name="LowValue">The runtime LOW-VALUE character (§12.3.7 GR9): lowest position; tie takes the FIRST
/// character specified.</param>
public sealed record CollatingTable(ushort[] Positions, char HighValue, char LowValue);
