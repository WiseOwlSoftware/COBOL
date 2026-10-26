// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

namespace WiseOwl.Cobol.Binding.Model;

/// <summary>Which of the three forms of the SYNCHRONIZED clause (ISO §13.18.55.2:
/// <c>{SYNCHRONIZED | SYNC} [LEFT | RIGHT]</c>) describes an item — <see cref="DataItem.Synchronized"/>, null when
/// no clause describes it.
/// <para>⛔ THE PHRASE IS RECORDED, NOT COLLAPSED (kb/Work PB538). The three forms LAY OUT identically here: the
/// natural boundary WiseOwl COBOL recognizes is one byte (docs/CONFORMANCE.md DOC-A.1-197), so an item always begins
/// at the left byte (§13.18.55.4 GR4) and ends on the right byte (GR5) of the boundary it occupies, and the bare form's
/// positioning (GR3) is that same one. But they are three different CLAUSES, and §8.5.3.1 compares clauses: "Each pair
/// of corresponding elementary items shall have the same ALIGNED, BLANK WHEN ZERO, DYNAMIC LENGTH, JUSTIFIED, PICTURE,
/// SIGN, SYNCHRONIZED, and USAGE clauses". A single bool made <c>SYNC LEFT</c> and <c>SYNC RIGHT</c> the same type
/// declaration, so a CALL passing one to a formal of the other compiled clean.</para></summary>
public enum SynchronizedPhrase
{
    /// <summary><c>SYNCHRONIZED</c> with neither phrase (§13.18.55.4 GR3).</summary>
    Unqualified,

    /// <summary><c>SYNCHRONIZED LEFT</c> (§13.18.55.4 GR4).</summary>
    Left,

    /// <summary><c>SYNCHRONIZED RIGHT</c> (§13.18.55.4 GR5).</summary>
    Right,
}
