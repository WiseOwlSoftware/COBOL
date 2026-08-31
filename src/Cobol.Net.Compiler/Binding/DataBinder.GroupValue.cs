// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;

namespace CobolNet.Binding;

/// <summary>
/// THE group-level VALUE declaration screen (ISO §13.18.63.3 SR13/SR14; kb/Work PB184) — the syntax rules that
/// restrict what may sit UNDER an entry carrying a group-level VALUE clause. Partial-class extension over
/// <c>DataBinder</c>, run as the declared <c>CheckGroupValueDeclarations</c> pass (see <c>BindPipeline</c>).
///
/// <para><b>Why this exists as its own pass.</b> Three rules, all with the same subject, and NONE of them was
/// written down anywhere in the compiler — the inverse of the usual [[one_rule_one_place]] finding, where a rule
/// is written twice. Measured on 8ca74a3d, every one of these compiled clean and ran:</para>
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
/// diagnostic (the same posture as the §13.18.63.3 SR2/SR3 numeric-VALUE screen, COBOLNET1625) — never a
/// silent member-wise substitute, and it is NOT dialect-gated: no edition of the standard permitted this.</para>
/// </summary>
public sealed partial class DataBinder
{
    /// <summary>The §13.18.63.3 SR1/SR13/SR14 group-level VALUE restrictions — ONE verdict per offending
    /// entry (the entry the programmer must change), anchored at that entry.
    ///
    /// <para>⛔ Runs over <see cref="CompositionForest"/>, NOT <see cref="ConformanceForest"/>. These rules are
    /// properties of the COMPOSED entry, not of the written clause list: `01 R TYPE T VALUE "ABCD".` composes an
    /// SR14 violation out of a VALUE the reference site wrote and a usage the TEMPLATE wrote, and neither entry
    /// carries it alone. The first landing of this screen took the written-entry forest, whose final filter drops
    /// every item with a non-null <c>TypeAnchor</c> — and <c>ExpandType</c> sets <c>TypeName</c> on the SUBJECT of
    /// the TYPE clause — so the reference site AND its whole clone subtree fell out. Measured: that program
    /// compiled clean (A = 0000, B = spaces) while its byte-identical inline spelling was rejected. §13.18.57.4
    /// GR3 makes the reference-site VALUE meaningful ("the content of the literal associated with that VALUE
    /// clause is used for the initial value associated with the subject of the entry"), and its NOTE points at
    /// the VALUE clause's own syntax rules for exactly this composition.</para>
    ///
    /// <para><b>Once per WRITTEN VALUE clause.</b> The subject test reads <see cref="DataItem.ValueIsCopied"/>:
    /// a template's VALUE assumed by a reference site (§13.18.57.4 GR1) or a SAME AS subject (§13.18.49 GR1)
    /// names the SAME source entry the template already answered for. Measured: `01 A VALUE "ABCD".
    /// 05 X PIC 9(4) COMP. 01 B SAME AS A.` reported COBOLNET1702 twice, both anchored at X's one declaration.
    /// (A SAME AS entry can never carry its OWN VALUE — §13.16.3 SR12, enforced as COBOLNET1555 — so the SAME AS
    /// population is covered entirely by its target.)</para>
    ///
    /// <para><b>SR1, both SR14 conjuncts, and SR13's second sentence.</b> SR14 is a single sentence carrying TWO
    /// independent restrictions and this repo's most reproducible defect shape is a two-arm rule with one arm
    /// implemented ([[two_arm_dispatch]], 9 instances); SR13's second sentence restricts the same subject's
    /// subtree, and SR1 restricts what the subject may BE. All four live here rather than in four places. SR16
    /// makes SR13/SR14 apply to the format 2 (table) VALUE as well, so the subject test reads BOTH VALUE
    /// carriers.</para></summary>
    internal void CheckGroupValueDeclarations()
    {
        foreach (var item in CompositionForest())
        {
            // The SUBJECT: a GROUP entry that WROTE a VALUE clause — format 1 (RawValue) or, per SR16,
            // format 2 (TableValues). A level-88 entry is a condition-name, not a node in the forest
            // (DataBinder's level stack attaches it to its conditional variable), so every descendant
            // reached below is a real data item — SR13/SR14's "data items subordinate to" exactly.
            if (!IsGroupValueSubject(item)) continue;
            string subject = item.CobolName ?? item.CsName;

            // SR1 — the subject shall not be a strongly-typed group item or a variable-length group. Reported
            // at the subject and the subtree walk is SKIPPED: the entry itself has to change, and SR14's usage
            // conjunct does not govern either shape (§13.18.29.4 GR3 excludes both from "alphanumeric group
            // item"), so continuing would name a rule that does not apply.
            if (Sr1SubjectShape(item) is { } shape)
            {
                using var _ = Edition.At(item);
                Edition.Error(DiagnosticCatalog.GroupValueSubjectShape, $"data item '{subject}' specifies a "
                    + $"group-level VALUE clause but is {shape} — the subject of the entry shall not be a "
                    + "strongly-typed group item or a variable-length group (ISO §13.18.63.3 SR1)");
                continue;
            }

            // SR13 sentence 2 already rejects a VALUE at subordinate levels within a VALUE-carrying group, so a
            // nested subject re-walks ground its ancestor covered: `01 A VALUE "XX". 05 B VALUE "YY".
            // 10 C PIC 9 COMP.` reported C's usage violation once per VALUE-carrying ancestor. One offender,
            // one verdict — the outer subject's walk reaches the whole subtree.
            if (AncestorIsGroupValueSubject(item)) continue;

            // ⛔ PB207: a group whose area is BIT-PACKED has no character area for §13.18.63.4 GR5 to deposit
            // into — its width is ceil(bits/8) laid out by the §8.5.1.6.3 walk, and the members do not tile it.
            // Staged LOUD here rather than left to the emitter, which crashed on the multi-member shape and
            // silently stored one boolean position on the single-member one. The predicate is HasBitDescendant
            // — the FACT that switches DataItem.ImageWidth to the bit walk — not GROUP-USAGE BIT, which is only
            // the commonest way to acquire it.
            if (item.HasBitDescendant)
            {
                using var _ = Edition.At(item);
                Edition.Error(DiagnosticCatalog.BitGroupLevelValue, $"data item '{subject}' specifies a "
                    + "group-level VALUE clause and has a USAGE BIT item subordinate to it, so its area is "
                    + "bit-packed (ceil(bits/8) characters, ISO §8.5.1.6.3) rather than a character run — "
                    + "the §13.18.63.4 GR5 area deposit for a bit-packed group is not yet implemented "
                    + "(kb/Work PB207)");
                continue;
            }

            // SR14's usage conjunct is scoped to an ALPHANUMERIC group item — a group for which no GROUP-USAGE
            // clause is specified or implied, that is not strongly typed and is not a variable-length group
            // (§13.18.29.4 GR3; the last two are unreachable here, SR1 rejected them above, and the predicate
            // states the rule rather than the residue). A national group's / bit group's subordinates take
            // usage NATIONAL / BIT by their OWN rules (§13.18.29.3), which is why SR14 names only the
            // alphanumeric one.
            bool alphanumericGroup = IsAlphanumericGroup(item);

            foreach (var sub in Subordinates(item))
            {
                // Anchor at the offending entry — except where that entry is a CLONE of a type declaration's
                // member: its DeclaredAt points into the TYPEDEF, which is legal on its own, and the violation
                // exists only at the reference site that added the VALUE. Anchor there instead.
                using var _ = Edition.At(StrongTypeModel.TypeAnchor(sub) == item ? item : sub);
                string name = sub.CobolName ?? "FILLER";

                // SR14 conjunct 1 — no JUSTIFIED, no SYNCHRONIZED anywhere in the subtree. (Unlike the usage
                // conjunct this one is NOT scoped to alphanumeric groups: the sentence's first clause says
                // "subordinate items within that group", full stop.)
                if (sub.Justified || sub.Synchronized)
                    Edition.Error(DiagnosticCatalog.GroupValueSubordinate, $"data item '{name}' is subordinate to "
                        + $"'{subject}', which specifies a group-level VALUE clause, and is described with a "
                        + $"{(sub.Justified ? "JUSTIFIED" : "SYNCHRONIZED")} clause — subordinate items within that "
                        + "group shall not be described with a JUSTIFIED or SYNCHRONIZED clause "
                        + "(ISO §13.18.63.3 SR14)");

                // SR13 sentence 2 — no VALUE clause at subordinate levels within this group. GR5 initializes
                // the AREA "without consideration for the individual elementary or group items contained
                // within this group", so a subordinate VALUE has no defined effect; the rule forbids writing
                // one rather than leaving the two initializations to race.
                if (sub.RawValue is not null || sub.TableValues is not null)
                    Edition.Error(DiagnosticCatalog.GroupValueSubordinate, $"data item '{name}' specifies a VALUE "
                        + $"clause and is subordinate to '{subject}', which specifies a group-level VALUE clause — "
                        + "the VALUE clause shall not be specified at subordinate levels within this group "
                        + "(ISO §13.18.63.3 SR13)");

                // SR14 conjunct 2 — every data item subordinate to an ALPHANUMERIC group item carrying a VALUE
                // shall be explicitly or implicitly usage DISPLAY. "Implicitly" is why this pass runs after
                // UsageInheritancePass: a group-level USAGE has already propagated to its leaves (§13.18.60.4
                // GR1), so an inherited COMP is caught on the leaf exactly as a written one is. A nested GROUP
                // is itself a data item — it is usage DISPLAY when it is an alphanumeric group ("An
                // alphanumeric group item is treated as though it had a usage of display", §8.5.2), and is
                // NOT when it carries GROUP-USAGE NATIONAL or BIT.
                if (!alphanumericGroup) continue;
                if (SubordinateUsageOf(sub) is not { } usage || usage is Usage.Display) continue;
                Edition.Error(DiagnosticCatalog.GroupValueSubordinate, $"data item '{name}' is subordinate to "
                    + $"'{subject}', an alphanumeric group item specifying a group-level VALUE clause, and is "
                    + $"described with usage {UsageWord(usage)} — all data items subordinate to an alphanumeric "
                    + "group item shall be explicitly or implicitly described with usage DISPLAY "
                    + "(ISO §13.18.63.3 SR14)");
            }
        }
    }

    /// <summary>The subject of §13.18.63.3 SR1/SR13/SR14: a GROUP entry that WROTE a VALUE clause — format 1
    /// (<see cref="DataItem.RawValue"/>) or, per SR16, format 2 (<see cref="DataItem.TableValues"/>). A VALUE
    /// this entry only ASSUMED from a type declaration or a SAME AS target belongs to the entry that wrote it
    /// (see <see cref="DataItem.ValueIsCopied"/>), which is screened where it is declared.</summary>
    private static bool IsGroupValueSubject(DataItem item) =>
        item.IsGroup && !item.ValueIsCopied && (item.RawValue is not null || item.TableValues is not null);

    /// <summary>Whether any ancestor of <paramref name="item"/> is itself a group-level VALUE subject — the
    /// SR13-sentence-2 shape, already rejected once at the outer subject, whose subtree walk covers this one.</summary>
    private static bool AncestorIsGroupValueSubject(DataItem item)
    {
        for (var p = item.Parent; p is not null; p = p.Parent)
            if (IsGroupValueSubject(p)) return true;
        return false;
    }

    /// <summary>The §13.18.63.3 SR1 shapes barred from being the subject of a VALUE clause, as the clause of the
    /// diagnostic naming which one was found, or null. A "variable-length group" is §8.5.1.12.1's — "a group item
    /// whose data description has at least one dynamic-length elementary item or dynamic-capacity table as a
    /// subordinate item" — NOT an occurs-depending group, whose size is its maximum.</summary>
    private static string? Sr1SubjectShape(DataItem item) =>
        StrongTypeModel.IsStrongGroup(item) ? "a strongly-typed group item (ISO §8.5.3)"
        : ReferenceResolver.HasVariableLengthSubordinate(item)
            ? "a variable-length group (ISO §8.5.1.12.1 — a dynamic-length elementary item or a "
              + "dynamic-capacity table is subordinate to it)"
        : null;

    /// <summary>§13.18.29.4 GR3: "If a GROUP-USAGE clause is not specified or implied for a group item that is
    /// not strongly typed and is not a variable-length group, that group item is an alphanumeric group item."
    /// ⛔ All THREE conjuncts, not just the GROUP-USAGE one — dropping the qualifiers made a strongly-typed or
    /// variable-length group answer SR14's usage arm, a rule §13.18.29.4 GR3 says does not reach it (measured
    /// this landing; §13.18.63.3 SR1 now rejects both shapes first, and this states the rule anyway so the next
    /// caller of the predicate inherits the whole of it).</summary>
    private static bool IsAlphanumericGroup(DataItem item) =>
        item.GroupUsage is GroupUsage.None
        && !StrongTypeModel.IsStronglyTyped(item)
        && !ReferenceResolver.HasVariableLengthSubordinate(item);

    /// <summary>The effective USAGE of one subordinate entry for §13.18.63.3 SR14: an elementary item's own
    /// (already inherited) usage; a nested group's <c>DISPLAY</c> unless it carries GROUP-USAGE NATIONAL or BIT.
    /// Null for an entry with neither (a recovery leaf with no PICTURE and no children) — SR14 cannot speak
    /// about an item the binder failed to describe, and a second diagnostic there would be noise.</summary>
    private static Usage? SubordinateUsageOf(DataItem sub) =>
        sub.IsGroup
            ? sub.GroupUsage switch
            {
                GroupUsage.National => Usage.National,
                GroupUsage.Bit => Usage.Bit,
                _ => Usage.Display,     // §8.5.2 — an alphanumeric group is treated as though usage display
            }
            : sub.Pic?.Usage;

    /// <summary>The §13.18.60 USAGE keyword for a usage, for the §13.18.63.3 SR14 diagnostic text — always a
    /// spelling the programmer could have WRITTEN.
    ///
    /// <para>⛔ DERIVED, not enumerated. <see cref="Usage"/> has 24 members and the enum name hyphenated on its
    /// case/digit boundaries IS the §13.18.60 keyword for all but five of them, so the default arm covers every
    /// member a future usage adds. The bare <c>ToString().ToUpperInvariant()</c> this replaced rendered them as
    /// 'PROGRAMPOINTER', 'FLOATBINARY32' and 'BINARYCHAR' — words no COBOL program contains. The five whose enum
    /// name is not the COBOL word carry an explicit spelling, and <c>UsageWordDriftTests</c> asserts every
    /// member's rendering is made of RESERVED COBOL words, so "automatic" stays true.</para></summary>
    internal static string UsageWord(Usage usage) => usage switch
    {
        Usage.Binary => "BINARY",                   // COMP / COMPUTATIONAL are the §13.18.60.2 synonyms
        Usage.Packed => "PACKED-DECIMAL",           // COMP-3 / COMPUTATIONAL-3 are the dialect synonyms
        Usage.Comp5 => "COMPUTATIONAL-5",           // no §13.18.60.2 spelling — the dialect word IS the name
        Usage.Float => "FLOAT-SHORT",               // §13.18.60.2's word; COMP-1 / COMPUTATIONAL-1 the dialect one
        Usage.Double => "FLOAT-LONG",               // COMP-2 / COMPUTATIONAL-2
        Usage.ObjectReference => "OBJECT REFERENCE",   // TWO words in the general format, not one hyphenated
        _ => HyphenateEnumName(usage.ToString()),
    };

    /// <summary>PascalCase (with trailing digit groups) → the COBOL hyphenated word: a hyphen before each
    /// interior capital and before each digit run that follows a non-digit. <c>FloatBinary32</c> →
    /// <c>FLOAT-BINARY-32</c>, <c>BinaryChar</c> → <c>BINARY-CHAR</c>, <c>Display</c> → <c>DISPLAY</c>.</summary>
    private static string HyphenateEnumName(string name)
    {
        var sb = new System.Text.StringBuilder(name.Length + 4);
        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && (char.IsUpper(name[i]) || (char.IsDigit(name[i]) && !char.IsDigit(name[i - 1]))))
                sb.Append('-');
            sb.Append(char.ToUpperInvariant(name[i]));
        }
        return sb.ToString();
    }

    /// <summary>Every data item subordinate to <paramref name="group"/>, at any depth, in declaration order.
    /// SR13/SR14 say "subordinate to", not "immediately subordinate to", so the walk is the whole subtree —
    /// a COMP leaf two levels down is as much a violation as one directly under the subject.</summary>
    private static IEnumerable<DataItem> Subordinates(DataItem group)
    {
        foreach (var c in group.Children)
        {
            yield return c;
            foreach (var d in Subordinates(c)) yield return d;
        }
    }
}
