// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Editions;

namespace WiseOwl.Cobol.Binding;

/// <summary>
/// THE §2023:13.16.3 SR24 CONDITION-NAME ASSOCIATION SCREEN (kb/Work PB488) — which entry a level-88 condition-name
/// entry may be associated with, and whether it follows one at all. Partial-class extension over
/// <c>DataBinder</c>, run as the declared <c>CheckConditionNameAssociations</c> pass (see <c>BindPipeline</c>).
///
/// <para><b>Why this exists.</b> <c>DataBinder.BindCondition</c> was a VALUE decoder: it captured the level-88
/// entry's literals and hung the <see cref="Condition88"/> off whatever <see cref="DataItem"/> the caller handed
/// it, and the caller handed it <c>stack.Peek()</c> unconditionally. SR24's association sentence and ALL EIGHT of
/// its lettered exclusions had no site anywhere in the compiler. Measured on c113f690, one probe per leg, every
/// one of these compiled and all but two of them then ANSWERED:</para>
/// <list type="bullet">
///   <item>b) <c>66 R1 RENAMES A THRU B.</c> followed by <c>88 COND-R VALUE "CD".</c> — a level-66 entry is never
///     pushed on the level stack, so <c>stack.Peek()</c> walked back past it and COND-R silently tested the
///     PRECEDING <c>05 B</c>. The probe printed BOUND-TO-B: a condition-name written over a renamed span that
///     answers about a different item is a WRONG ANSWER, not a permissiveness.</item>
///   <item>the association sentence — <c>88 ORPHAN VALUE "AB".</c> as the first entry of WORKING-STORAGE was
///     dropped without a word (<c>if (stack.Count > 0)</c>) and surfaced as a RUN-TIME
///     <c>NotImplementedCobolFeatureException</c> on <c>IF ORPHAN</c>.</item>
///   <item>c) an alphanumeric group containing <c>05 X PIC 9(4) USAGE BINARY</c>, d) a group containing a
///     JUSTIFIED item, f) a LINKAGE <c>01 AL PIC X ANY LENGTH.</c>, g) a <c>TYPEDEF STRONG</c> template —
///     each took a group 88 and evaluated it.</item>
///   <item>e) <c>01 IX USAGE INDEX. 88 C VALUE 1.</c> compiled and the condition took the TRUE branch after
///     <c>SET IX TO 1</c> — the compiler had IMPLEMENTED a construct the standard forbids. The pointer spelling
///     <c>01 P USAGE POINTER. 88 C VALUE NULL.</c> emitted <c>if ((P == NULLL))</c> into the generated C# and
///     handed the user <c>error CS0103: The name 'NULLL' does not exist in the current context</c>.</item>
///   <item>h) a group with a dynamic-length member took an 88 and crashed at run time in the group image.</item>
/// </list>
///
/// <para><b>⛔ THE EIGHT EXCLUSIONS ARE A SET OF ITEM SHAPES, SO THEY ARE ONE TABLE AND NOT EIGHT SCATTERED
/// TESTS</b> (CLAUDE.md rule 5 — prefer the shape that makes the NEXT case automatic). <see cref="CheckConditionNameAssociationsPass.Sr24Exclusions"/>
/// carries one row per lettered exclusion, in the standard's own order, each row holding the rule's own words;
/// <see cref="CheckConditionNameAssociationsPass.ConditionalVariableExclusion"/> returns the FIRST row that applies and the diagnostic quotes it. A
/// ninth exclusion is a row. <c>ConditionNameAssociationDriftTests</c> asserts the table is exactly the letters
/// a–h and exercises every predicate, so "automatic" stays true and no row becomes a lookup nothing reads.</para>
///
/// <para><b>⛔ WHY IT IS A POST-BIND PASS AND NOT A TEST INSIDE <c>BindCondition</c>.</b> SR24 asks about the
/// conditional variable's SUBORDINATES in four of its eight exclusions (c, d, g, h) — and a condition-name entry
/// "shall immediately follow the entry describing the item", which for a GROUP means it is written BEFORE that
/// group's subordinate entries. At <c>BindCondition</c> time <c>parent.Children</c> is still empty, so a screen
/// there would answer "not a group, no excluded member" for exactly the shapes the rule is about. The
/// association is RECORDED at bind time (where source order is the fact) and ADJUDICATED once the forest is
/// complete. The recorded list is also what carries the two shapes <see cref="ConformanceForest"/> cannot: a
/// level-66 alias, which lives on <see cref="DataItem.Renames66"/> and not in the tree, and an association with
/// NO item at all.</para>
///
/// <para><b>Once per WRITTEN entry.</b> Only <c>BindEntries</c> records, and a TYPE clone's condition-names are
/// copied by <c>CloneItem</c> rather than re-bound, so a <c>TYPEDEF</c> template's 88 is adjudicated once at the
/// template — the entry the programmer must change — however many <c>TYPE</c> reference sites exist. That is the
/// same once-per-source contract <c>CheckUsageDeclarations</c> gets from <see cref="ConformanceForest"/>.</para>
///
/// <para><b>Edition axis: UNGATED, and that direction is the safe one.</b> COBOL-85's condition-name rule already
/// excluded another 88, a level-66 item, "a group containing items with descriptions including JUSTIFIED,
/// SYNCHRONIZED or USAGE (other than USAGE IS DISPLAY)" and an index data item — the 2023 list splits c) from d),
/// narrows c) to ALPHANUMERIC groups (national and bit groups did not exist in 1985) and widens e) to classes
/// that did not exist in 1985 either. Every exclusion this table can fire on below 2023 is one the 1985 rule
/// stated too, and the shapes the later letters name (ANY LENGTH, STRONG, dynamic-length) are themselves gated
/// by their own introduction gates, so they cannot be written below the edition that introduced them. There is
/// nothing to gate.</para>
/// </summary>
public sealed partial class DataBinder
{
    /// <summary>Record one condition-name → conditional-variable association for
    /// <see cref="CheckConditionNameAssociationsPass.Run"/>. Called from <c>BindEntries</c>' level-88 arm for EVERY
    /// written 88, including the ones it cannot bind.</summary>
    private void RecordConditionAssociation(string name, DataItem? variable) =>
        State.Clauses.RecordConditionAssociation(new ConditionAssociation(name, variable, Edition.Cursor));
}
