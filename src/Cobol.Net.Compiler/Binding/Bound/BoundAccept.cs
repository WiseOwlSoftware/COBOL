// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;

namespace CobolNet.Binding.Bound;

// The ACCEPT bound nodes (P7 Step 10h: the binder half became the real Verbs/AcceptDisplayBinder;
// these types STAY here — the VersionConformancePass EndAccept2002 gate reads HasEndTerminator, and
// the source-generated visitor keys on this namespace).

/// <summary><c>ACCEPT identifier [FROM source]</c> (ISO §14.9.1). <see cref="AcceptKind.Device"/> is Format 1 —
/// the hardware-device transfer (a plain <c>ACCEPT</c>, GR5, or <c>FROM mnemonic-name</c> resolved through the
/// SPECIAL-NAMES device registry, §12.3.7 Format 4): data REPLACES the receiver's content, stored ALIGNED LEFT
/// with additional transfers requested / excess ignored by size (GR1–GR4 — explicitly NOT the MOVE rules). Every
/// other kind is Format 2 — a temporal source read from the system clock as a conceptual UNSIGNED INTEGER USAGE
/// DISPLAY item and transferred BY THE MOVE RULES (GR6), of conceptual width 6/8/5/7/8/1 (GR7–GR12).</summary>
public sealed record BoundAccept(Place Target, AcceptKind Kind) : BoundStatement
{
    /// <summary>True when the ACCEPT was written with the explicit END-ACCEPT scope terminator (ISO §14.9.1
    /// general formats — COBOL-2002; the 1985 ACCEPT has none). The edition gate (EndAccept2002) reads this in the
    /// post-bind <see cref="Validation.VersionConformancePass"/> (rearch PHASE-03 Step 14e); the terminator has no
    /// semantic effect, so only its presence is recorded.</summary>
    public bool HasEndTerminator { get; init; }
}

/// <summary>The data source of an ACCEPT (ISO §14.9.1): the Format 1 device, or one of the Format 2 temporal
/// sources — DATE (YYMMDD, GR7), DATE YYYYMMDD (GR8, 2002+), DAY (YYDDD, GR9), DAY YYYYDDD (GR10, 2002+),
/// TIME (HHMMSScc, GR11), DAY-OF-WEEK (1=Monday…7=Sunday, GR12).</summary>
public enum AcceptKind { Device, Date, DateYYYYMMDD, Day, DayYYYYDDD, DayOfWeek, Time }
