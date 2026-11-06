// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding;

/// <summary>
/// THE group-level VALUE declaration screen (ISO §13.18.63.3 SR13/SR14; kb/Work PB184) — the syntax rules that
/// restrict what may sit UNDER an entry carrying a group-level VALUE clause. Partial-class extension over
/// <c>DataBinder</c>, run as the declared <c>CheckGroupValueDeclarations</c> pass (see <c>BindPipeline</c>).
///
/// <para><b>Why this exists as its own pass.</b> Three rules, all with the same subject, and NONE of them was
/// written down anywhere in the compiler — the inverse of the usual [[one_rule_one_place]] finding, where a rule
/// is written twice. Measured on 99bc08c8, every one of these compiled clean and ran:</para>
/// <list type="bullet">
///   <item><c>01 GV VALUE "40537". 05 GB PIC 9 COMP-5 OCCURS 5.</c> — every occurrence initialized ZERO (the
///     group VALUE dropped on the floor). This is kb/Work PB184's measured symptom.</item>
///   <item><c>01 GJ VALUE "ABCD". 05 J1 PIC X(2) JUSTIFIED RIGHT.</c> — accepted silently.</item>
///   <item><c>01 GV VALUE "ABCD". 05 B PIC X(2) VALUE "ZZ".</c> — accepted silently, B's own VALUE discarded.</item>
/// </list>
///
/// <para><b>⚠ PB184's premise, corrected.</b> The note registered PB184 as a §13.18.63.4 GR5 DISTRIBUTION gap —
/// GR5 initializes "the group area … without consideration for the individual elementary or group items
/// contained within this group", so (the reasoning went) a COMP-5 leaf should take its slice of the literal's
/// BYTES and the <c>GroupValueSlicer</c> screen that excludes byte-form leaves was predicate drift left over
/// from the Tier-C boundary. SR14 refutes that premise: an alphanumeric group item is a group for which no
/// GROUP-USAGE clause is specified or implied, that is not strongly typed and is not a variable-length group
/// (§13.18.29.4 GR3; §8.5.2.1 — "an alphanumeric group item has class and category alphanumeric"), and SR14
/// requires EVERY data item subordinate to such a group carrying a VALUE to be usage
/// DISPLAY. The measured program is therefore NOT CONFORMING; there is no conforming area for the byte-form
/// distribution to exist over. GR5 is already implemented, correctly, for the whole population SR14 admits —
/// where every subordinate is usage DISPLAY, so the character image IS the byte image and a positional
/// character slice IS the area deposit. What was missing was the DIAGNOSTIC on the complement.</para>
///
/// <para>A syntax rule is a "shall", so the only conforming response to a violation is a compile-time
/// diagnostic (the same posture as the §13.18.63.3 SR2/SR3 numeric-VALUE screen, COBOL1625) — never a
/// silent member-wise substitute, and it is NOT dialect-gated: no edition of the standard permitted this.</para>
/// </summary>
public sealed partial class DataBinder
{

    /// <summary>A GROUP entry carrying a VALUE clause — format 1 (<see cref="DataItem.RawValue"/>) or, per SR16,
    /// format 2 (<see cref="DataItem.TableValues"/>) — WRITTEN or ASSUMED.</summary>
    internal static bool HasGroupValue(DataItem item) =>
        item.IsGroup && (item.RawValue is not null || item.TableValues is not null);
}
