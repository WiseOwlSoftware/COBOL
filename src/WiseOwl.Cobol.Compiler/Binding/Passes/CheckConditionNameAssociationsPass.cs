// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Editions.Diagnostics;

namespace WiseOwl.Cobol.Binding.Passes;

/// <summary>
/// The CheckConditionNameAssociations pass: row 7 of the per-unit resolve pipeline (UsageResolved to UsageResolved).
/// </summary>
/// <remarks>
/// <para><b>Placement in the pipeline.</b>
/// The §13.16.3 SR24 condition-name ASSOCIATION screen — the entry a level-88 entry may be associated with, and whether it follows one at all (kb/Work PB488). Placed HERE for the same reason as the two passes above: exclusion c) asks whether a group contains "items with a usage other than display", which §13.18.60.4 GR1 inheritance can supply, so UsageInheritancePass must have run; nothing later is needed, because every other exclusion reads declared shape the entry walk already settled. It cannot live inside BindCondition at all — four exclusions ask about SUBORDINATE entries, which a condition-name on a group is written BEFORE.
/// </para>
/// </remarks>
internal sealed class CheckConditionNameAssociationsPass : IBindPass
{
    /// <inheritdoc/>
    public string Name => "CheckConditionNameAssociations";

    /// <inheritdoc/>
    public PassPhase Requires => PassPhase.UsageResolved;

    /// <inheritdoc/>
    public PassPhase Produces => PassPhase.UsageResolved;

    /// <inheritdoc/>
    public PassViews Views => PassViews.None;

    /// <summary>ISO §13.16.3 SR24, both halves: every level-88 entry follows an entry describing a data item,
    /// and that item is not one of the eight shapes the rule excludes.</summary>
    public void Run(DataBinder binder)
    {
        foreach (var (name, variable, at) in binder.State.Clauses.ConditionAssociations)
        {
            using var _ = binder.Edition.At(at);
            if (variable is null)
            {
                binder.Edition.Error(DiagnosticCatalog.ConditionNameNoConditionalVariable, $"condition-name '{name}': "
                    + "this level-88 entry does not immediately follow a data description entry describing a "
                    + "data item — the condition-name entries for a particular conditional variable shall "
                    + "immediately follow the entry describing the item with which the condition-name is "
                    + "associated (ISO §13.16.3 SR24)");
                continue;
            }
            if (ConditionalVariableExclusion(variable) is not { } excluded) continue;
            binder.Edition.Error(DiagnosticCatalog.ConditionNameVariableExcluded, $"condition-name '{name}': the entry "
                + $"it follows describes '{variable.CobolName ?? "FILLER"}', and a condition-name may not be "
                + $"associated with {excluded.Article} — ISO §13.16.3 SR24 excludes "
                + $"\"{excluded.Letter}) {excluded.Text}\"{excluded.Also}");
        }
    }

    /// <summary>ISO §13.16.3 SR24 read as an EXCLUSION test, in the <c>RefModBinder.RefModExclusion</c>
    /// shape: the lettered exclusion that bars <paramref name="candidate"/> from being a conditional variable,
    /// or null when it may be one. The FIRST matching row wins, and the rows are in the standard's own order, so
    /// the letter in the diagnostic is the one the programmer looks up.</summary>
    internal static Sr24Exclusion? ConditionalVariableExclusion(DataItem candidate)
    {
        foreach (var row in Sr24Exclusions)
            if (row.Applies(candidate)) return row;
        return null;
    }

    /// <summary>One lettered exclusion of ISO §13.16.3 SR24. <paramref name="Text"/> is the standard's own
    /// sentence; <paramref name="Article"/> is the same shape phrased for a message ("a level 66 entry");
    /// <paramref name="Also"/> carries a second rule stating the SAME prohibition, empty for most rows.</summary>
    internal readonly record struct Sr24Exclusion(char Letter, string Text, string Article,
        Func<DataItem, bool> Applies, string Also = "");

    /// <summary>⛔ THE EIGHT EXCLUSIONS OF ISO §13.16.3 SR24, as a table — the ONE place the rule is written
    /// down. Each predicate is a static lambda held in this static array, so the walk allocates nothing.</summary>
    internal static readonly IReadOnlyList<Sr24Exclusion> Sr24Exclusions =
    [
        // a) Another level 88 entry.
        // ⛔ HELD BY CONSTRUCTION, and the row is here because the rule is. `BindEntries` leaves its
        // `lastDescribed` cursor alone on a level-88 entry, so a run of consecutive 88s all associate with the
        // one entry they follow — which is the rule's INTENT (SR24's second sentence speaks of "the
        // condition-name entries", plural, "for a particular conditional variable"). No DataItem in the forest
        // carries Level 88, so this predicate is unreachable from BindEntries today; the drift test constructs
        // the shape directly and asserts it names a), because a row nothing can contradict is a row nobody has
        // verified (and because a future model that DID give an 88 a DataItem must not silently start accepting
        // `88 A VALUE 1. 88 B VALUE 2.` as an association of B with A).
        new('a', "Another level 88 entry.", "another level-88 entry", static d => d.Level == 88),

        // b) A level 66 entry.
        // The measured wrong answer: a 66 RENAMES alias is attached to its owning record's Renames66 list and
        // never pushed on the level stack, so the old `stack.Peek()` reader walked BACK PAST it.
        new('b', "A level 66 entry.", "a level-66 RENAMES entry", static d => d.Level == 66),

        // c) An alphanumeric group containing items with a usage other than display.
        // ⛔ INHERITED CITATION, RE-DERIVED (kb/Work PB932). This cited §8.5.1.3, which is the LEVELS clause and
        // says nothing about group categories. "Alphanumeric group item" is defined in §3.11 — "group item
        // except for a bit group item, a national group item, a strongly-typed group item, or a variable-length
        // group item" — and made one by §13.18.29.4 GR3. ⚠ THE PREDICATE BELOW IS THE TWO-EXCLUSION READING the
        // wrong citation carried (kb/Work PB947): it asked `IsGroup && GroupUsage is None`, which excludes the bit
        // and national groups and not the STRONGLY-TYPED or VARIABLE-LENGTH ones, so a variable-length group was
        // refused under THIS letter and quoted a sentence it does not violate (h) is the letter it violates, and
        // the first-match walk never reached it). The subject is now asked of ItemCategory.IsAlphanumericGroup,
        // the ONE spelling of §3.11's four-exclusion definition (§13.18.29.4 GR3), so c) and h) are disjoint
        // for a variable-length group and the letter the programmer looks up is the one that applies. The
        // membership test reaches EVERY depth: the rule says "containing", not "immediately containing", and a
        // COMP item two levels down denies the group a character image exactly as one level down does.
        new('c', "An alphanumeric group containing items with a usage other than display.",
            "an alphanumeric group containing an item whose usage is not DISPLAY",
            static d => ItemCategory.IsAlphanumericGroup(d)
                && AnySubordinate(d, static x => UsageOtherThanDisplay(x))),

        // d) A group containing items described with a JUSTIFIED or SYNCHRONIZED clause.
        // ANY group, not only an alphanumeric one — the letter says "A group" where c) says "An alphanumeric
        // group", and the difference is the drafting.
        // ⛔ THE SUBJECT'S OWN SYNCHRONIZED CLAUSE COUNTS, AND IT IS READ ON ITS MEMBERS. §13.18.55.3 SR1 permits
        // SYNCHRONIZED on a group ("The SYNCHRONIZED clause may be specified for group and elementary items") and
        // §13.18.55.4 GR1 makes that a description of its members: "When this clause is specified for a group item,
        // it is treated as though it had instead been separately specified for each of the subordinate elementary
        // items for which this clause is permitted." DataBinder.InheritSynchronized writes exactly that onto each
        // such member (kb/Work PB539), so `01 G SYNCHRONIZED.` is caught through its members, and a group whose
        // clause reaches no member (every one is below a group-level VALUE, §13.18.63.3 SR14) contains none.
        // JUSTIFIED has no such arm — §13.18.31.3 SR1 confines it to the elementary item level.
        new('d', "A group containing items described with a JUSTIFIED or SYNCHRONIZED clause.",
            "a group containing an item described with a JUSTIFIED or SYNCHRONIZED clause",
            static d => d.IsGroup && AnySubordinate(d, static x => x.Justified || x.Synchronized is not null)),

        // e) A data item of the class index, message-tag, object, or pointer.
        new('e', "A data item of the class index, message-tag, object, or pointer.",
            "a data item of the class index, message-tag, object, or pointer",
            static d => ItemCategory.IsIndexMessageTagObjectOrPointer(d),
            Also: "; the same prohibition is ISO §13.18.60.3 SR11, \"An elementary data item of class index, "
                + "message-tag, object, or pointer shall not be a conditional variable\""),

        // f) A data item described with the ANY LENGTH clause.
        new('f', "A data item described with the ANY LENGTH clause.",
            "a data item described with the ANY LENGTH clause", static d => d.IsAnyLength),

        // g) A type declaration described with the STRONG phrase, or a group item subordinate to such a type
        // declaration.
        // ⛔ THE DECLARATION SIDE, not the post-expansion StrongTypeModel.StrongRoot — the rule's subject is
        // "a type declaration", i.e. the TYPEDEF STRONG template entry the programmer wrote, and a TYPE
        // REFERENCE to it is governed by a different rule (§13.18.57.3 SR2 bars an 88 immediately after any
        // TYPE-clause entry, COBOL1537). The parent walk is UnderStrongTypeDeclaration, shared with
        // §13.18.60.3 SR14, whose second arm is the same words. The second arm here is confined to GROUP items
        // because the letter says "a group item subordinate to such a type declaration": an ELEMENTARY member
        // of a strong template may carry a condition-name.
        new('g', "A type declaration described with the STRONG phrase, or a group item subordinate to such a "
                + "type declaration.",
            "a STRONG type declaration or a group item subordinate to one",
            static d => d.DeclaresStrongType || (d.IsGroup && DataBinder.UnderStrongTypeDeclaration(d))),

        // h) A variable-length group.
        // §8.5.1.12.1: "A variable-length group is a group item whose data description has at least one
        // dynamic-length elementary item or dynamic-capacity table as a subordinate item." ⛔ NOT an
        // occurs-depending-on group — the standard names the two shapes SEPARATELY wherever it means both
        // (§13.18.12.3 SR2: "Identifier-1 shall reference an alphanumeric data item that shall not be an
        // occurs-depending-on group item, a variable-length group, or a dynamic-length elementary item" —
        // three things, listed side by side), and reading ODO into this letter would reject legal COBOL-85.
        // VariableLengthCompatibility.IsVariableLength IS §8.5.1.12.1's definition, and the very test
        // ItemCategory.GroupKindsOf subtracts from the alphanumeric group, so c) and h) are disjoint for it;
        // this is the delegation.
        new('h', "A variable-length group.", "a variable-length group",
            static d => VariableLengthCompatibility.IsVariableLength(d)),
    ];

    /// <summary>"an item with a usage other than display", for SR24 c). A BIT or NATIONAL group counts: it is
    /// "treated as an elementary item of usage bit / national described with PICTURE 1(m) / N(m)"
    /// (§13.18.29.4 GR1/GR2), which is a usage other than display however deep it sits.</summary>
    private static bool UsageOtherThanDisplay(DataItem d) =>
        d.GroupUsage is not GroupUsage.None
        || (d.Children.Count == 0 && (d.Pic?.Usage ?? d.OwnUsage) is { } u && u is not Usage.Display);

    /// <summary>Does any item subordinate to <paramref name="group"/>, at any depth, satisfy
    /// <paramref name="predicate"/>? Recursion depth is the record's LEVEL depth, which ISO §13.18.33.3 caps at
    /// 49 — <c>LevelNumberPass</c> owns that rule and has already enforced it by the time this pass runs, so the
    /// walk cannot run deep and this is a pointer, not a second copy. It allocates nothing (the predicates are
    /// cached static lambdas) and runs once per WRITTEN level-88 entry, of which a program has a handful.</summary>
    private static bool AnySubordinate(DataItem group, Func<DataItem, bool> predicate)
    {
        foreach (var child in group.Children)
            if (predicate(child) || AnySubordinate(child, predicate)) return true;
        return false;
    }
}
