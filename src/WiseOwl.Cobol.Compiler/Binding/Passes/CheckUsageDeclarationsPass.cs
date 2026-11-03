// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Editions.Diagnostics;

namespace WiseOwl.Cobol.Binding.Passes;

/// <summary>
/// The CheckUsageDeclarations pass: row 5 of the per-unit resolve pipeline (UsageResolved to UsageResolved).
/// </summary>
/// <remarks>
/// <para><b>Placement in the pipeline.</b>
/// The §13.18.60.3 USAGE declaration-PLACEMENT screen — SR14/SR15/SR4 (kb/Work PB183). Placed HERE, and not one pass earlier or later, for two reasons. It needs UsageInheritancePass to have settled §13.18.60.4 GR1 group-usage inheritance, so an INHERITED pointer usage is screened exactly as a written one. And it must precede ClassifyRedefinesClasses, whose Tier-D backstop rejects a nested pointer/object leaf under REDEFINES with a message that names THIS rule as the missing screen: the declaration's own defect should be reported before the downstream machinery's defense against it. A plain syntax rule, so NOT the terminal VersionConformancePass — that pass is the edition gate.
/// </para>
/// </remarks>
internal sealed class CheckUsageDeclarationsPass : IBindPass
{
    /// <inheritdoc/>
    public string Name => "CheckUsageDeclarations";

    /// <inheritdoc/>
    public PassPhase Requires => PassPhase.UsageResolved;

    /// <inheritdoc/>
    public PassPhase Produces => PassPhase.UsageResolved;

    /// <inheritdoc/>
    public PassViews Views => PassViews.None;

    /// <summary>The §13.18.60.3 declaration-placement rules — SR14 (the five pointer/object usage phrases only
    /// at level 1/77 or under a STRONG type declaration), SR15 (no USAGE OBJECT REFERENCE in the file section)
    /// and SR4 (none of six usages in or under a CONSTANT RECORD). ONE verdict per offending WRITTEN entry.
    ///
    /// <para>⛔ Runs over <see cref="DataBinder.ConformanceForest"/>, NOT <see cref="DataBinder.CompositionForest"/>. These are
    /// properties of the WRITTEN clause list, and the forest choice is what makes the verdict once-per-source:
    /// it walks <see cref="ForestSlice.Roots"/> (which already holds the LINKAGE roots) + the TYPEDEF templates in <see cref="ForestSlice.TypeDecls"/>
    /// and PRUNES the TYPE-clone subtrees, so <c>01 W IS TYPEDEF. 05 WP USAGE POINTER.</c> is rejected ONCE at
    /// the template no matter how many <c>TYPE W</c> reference sites exist — the entry the programmer must
    /// change. A SAME AS copy is NOT pruned (<see cref="StrongTypeModel.TypeAnchor"/> keys on a TYPE clause,
    /// which a SAME AS subject does not carry), which is exactly right: <c>05 Q SAME AS P.</c> is its own
    /// written entry, in its own group, at its own level.</para>
    ///
    /// <para>All four usage-acquisition routes fall to this ONE enumeration rather than to a hand-written copy
    /// at each site — a written USAGE clause, §13.18.60.4 GR1 group inheritance, a TYPE clone and a SAME AS
    /// copy. That is deliberate: SR14's drafting twin §13.18.57.3 SR6 is implemented as TWO hand-written parent
    /// walks (<c>ExpandType</c> and <c>ExpandSameAs</c>), and writing a fifth copy at each of four sites would
    /// have reproduced that duplication at twice the scale. A fifth acquisition route added later is screened
    /// here for free.</para>
    ///
    /// <para>⛔ ONE route is not a clone of a judged template: the TYPE entry ITSELF (kb/Work PB3129). The pruning
    /// skips it with its clone subtree, and for a GROUP type that is right (the template's members were judged), but
    /// an ELEMENTARY pointer-class type declaration is a level-1 entry that passes SR14, while the TYPE entry that
    /// references it stands wherever it was written. So the second loop asks arm B
    /// (<see cref="ScreenSr14ElementaryPlacement"/>) of every WRITTEN entry that carries a TYPE clause, and with it
    /// every other screen of the first loop whose subject the TYPE clause composes: SR4
    /// (<see cref="ScreenConstantRecordUsage"/>) and §13.18.5.3 SR2 (<see cref="ScreenBasedSubjectShape"/>), the
    /// review's R1-1.</para></summary>
    public void Run(DataBinder binder)
    {
        foreach (var item in binder.ConformanceForest())
        {
            using var _ = binder.Edition.At(item);
            string name = item.CobolName ?? "FILLER";

            // ── §13.18.60.3 SR4 ──────────────────────────────────────────────────────────────────────────
            // SIX usage phrases, INDEX among them, barred from a CONSTANT RECORD entry "or in any item
            // subordinate to" one — IsConstantRecordItem is precisely that subtree walk (the flag lives on
            // the level-01 root). Checked before SR14 so the narrower rule names the entry's real defect: a
            // pointer member of a CONSTANT RECORD group violates both, and SR4 is the one that also governs
            // the level-1 spelling.
            if (ScreenConstantRecordUsage(binder, item, name)) continue;

            // ── §13.18.60.3 SR15 ─────────────────────────────────────────────────────────────────────────
            // The DIRECT declaration arm. Its SAME AS twin (§13.18.49.3 SR6) has been enforced since the SAME
            // AS landing while this one had no screen at all — the two-arm dispatch, with the arm that needs
            // no indirection missing. IsFileSectionItem is the twin's own root test, shared rather than
            // re-written. Reported independently of SR14: an FD record may legally be a level-1 elementary
            // item, so `01 R USAGE OBJECT REFERENCE C.` under an FD passes SR14 and violates only SR15.
            if (item.Pic is { Category: PicCategory.ObjectReference } && binder.IsFileSectionItem(item))
            {
                binder.Edition.Error(DiagnosticCatalog.UsageObjectReferenceFileSection, $"data item '{name}' is "
                    + "described with USAGE OBJECT REFERENCE and belongs to a file section record — the USAGE "
                    + "OBJECT REFERENCE clause shall not be specified in the file section "
                    + "(ISO §13.18.60.3 SR15)");
                continue;
            }

            // ── §13.18.22.3 SR4 ──────────────────────────────────────────────────────────────────────────
            // "The EXTERNAL clause shall not be specified for a data item of class object or pointer."
            // ⛔ WHY IT IS HERE AND WHY NOW (kb/Work PB231, the pointer third). Until the pointer-class storage
            // landing, `01 P USAGE POINTER EXTERNAL.` was rejected — but by the byte-window RESIDUE diagnostic
            // (COBOL1695 / the EXTERNAL twin), i.e. "recognized but not yet implemented", which is a
            // different verdict about a different thing. Opening the byte-window gate to pointer-class leaves
            // would have made this NONCONFORMING shape compile clean: an under-rejection created by a fix. The
            // rule that actually bars it is enforced in the same change set, and it is the ITEM's own class the
            // rule names — a strongly-typed EXTERNAL group holding a pointer MEMBER is of class its type-name
            // (§8.5.2.1), not pointer, and stays legal (SR5 only requires its type declaration to be external too).
            if (ScreenExternalClassItem(binder, item, name)) continue;

            // ── §13.18.5.3 SR1 ───────────────────────────────────────────────────────────────────────────
            // "The subject of the entry shall not be of class object." The EXTERNAL twin's other half, and the
            // asymmetry is the rule's own: the BASED rule names ONLY class object, so `01 P USAGE POINTER
            // BASED.` — class POINTER — is legal source and is exactly what kb/Work PB231's pointer third
            // implements. Same reason for the timing as SR4 above: COBOL1695 used to reject both, and this
            // one must keep being rejected after the gate opens.
            if (ScreenBasedClassObjectItem(binder, item, name)) continue;

            // ── §13.18.5.3 SR2 ───────────────────────────────────────────────────────────────────────────
            // "The subject of the entry shall not be a dynamic-length elementary item or a variable-length
            // group." (kb/Work PB1213.) Asked of the SHAPE, through the one screen of that pair — never of the
            // BASED cell's carriage gate (ByteWindowResidueOf) or its component walk (Place.CellComponents), which answer what
            // the shared cell can HOLD and are shared with EXTERNAL and ADDRESS OF, where a variable-length group
            // is legal. Until this arm, a BASED group with a DYNAMIC LENGTH leaf compiled and ran because the
            // cell had learned to carry one, and the dynamic-capacity-table spelling was refused only as "not yet
            // implemented". An entry that carries a TYPE clause (`01 A TYPE VT BASED.`, VT a variable-length group
            // type: a variable-length group by §13.18.57.4 GR1) is pruned from this forest, so the composed-entry
            // loop below asks it the same screen (kb/Work PB3129 review R1-1). (The elementary spelling is also §13.16.3 SR18's — DYNAMIC
            // LENGTH admits no BASED clause beside it — which clears IsDynamicLength, so it is reported once, there.)
            if (ScreenBasedSubjectShape(binder, item, name)) continue;

            // ── §13.18.60.3 SR14 ─────────────────────────────────────────────────────────────────────────
            // ARM A — a GROUP entry that WROTE one of the five phrases. §13.18.60.4 GR1: the clause "applies
            // only to each elementary item in the group", and every such item is subordinate to this group, so
            // each acquires the usage at a level greater than 1 and SR14 is violated for each. The verdict is
            // reported ONCE, at the entry carrying the clause the programmer must change — not once per leaf.
            // (Screened before arm B so the group's own header, which sheds its synthesized profile in
            // ResolveIndexItems, is not silently skipped by a Pic test that no longer sees the usage.)
            if (!Sr14Elementary(item) && ItemCategory.Sr14PhraseOf(item.OwnUsage) is { } phraseA)
            {
                binder.Edition.Error(DiagnosticCatalog.UsageDeclarationPlacement, $"data item '{name}' is a GROUP item "
                    + $"described with USAGE {phraseA} — that usage applies to each elementary item in the "
                    + "group (ISO §13.18.60.4 GR1), and each of those is subordinate to this group rather than "
                    + $"an elementary item at level 1. A USAGE clause with the {Sr14PhraseList} phrase may be "
                    + "specified only for an elementary data item at level 1 or an elementary data item "
                    + "subordinate to a type declaration that includes the STRONG phrase "
                    + "(ISO §13.18.60.3 SR14)");
                continue;
            }

            // ARM B — an ELEMENTARY item that carries one of SR14's usage phrases, however it acquired it. ⛔ BOTH ARMS
            // READ THE SAME SOURCE OF TRUTH, the phrase (Sr14PhraseOf: the written clause, else the resolved profile's
            // usage), never the resolved CATEGORY (kb/Work PB819): a usage the compiler refuses by name — MESSAGE-TAG,
            // declined non-support — binds a recovery profile with no class, so a class-keyed arm B never saw
            // `05 A USAGE MESSAGE-TAG.` inside an ordinary group while arm A reported its group spelling.
            ScreenSr14ElementaryPlacement(binder, item, name);
        }

        // ── THE COMPOSED-ENTRY HALF OF §13.18.22.3 SR4 AND §13.18.5.3 SR1 (kb/Work PB1212) ──────────────────
        // Both rules are about an entry whose CLASS comes from one place and whose clause from another: `01 A TYPE PT
        // EXTERNAL.` writes the EXTERNAL clause at the site and takes class pointer from the type declaration PT
        // (§13.18.57.4 GR1), and the first loop above never sees A — ConformanceForest PRUNES every entry that carries
        // a TYPE clause, because the template was gated once. So the SAME two screens are asked, by the same code, of
        // each entry that DIRECTLY carries a TYPE clause. Only a level-1 entry can write EXTERNAL or BASED, so the
        // entries reached here are the site entries and nothing is reported twice (the templates were judged above).
        //
        // ⛔ §13.18.60.3 SR14 IS THE THIRD RULE OF THAT KIND (kb/Work PB3129). `01 PT IS TYPEDEF USAGE POINTER.` writes
        // the phrase at level 1, which SR14 permits, so the template passes; `01 G. 05 A TYPE PT.` then places it at
        // level 05 in an ordinary group, "as though the data description identified by type-name-1 had been coded in
        // place of the TYPE clause" (§13.18.57.4 GR1) — the level-05 spelling arm B refuses. SAME AS states that effect
        // in the same words (§13.18.49.4 GR1), and its copy was always screened (a SAME AS subject is a written entry),
        // so the TYPE spelling was the one route by which a pointer- or object-class leaf reached a group that is not
        // strongly typed — a shape that then failed far from its declaration, at a CALL or a prototype pair, under a
        // rule that is not the one it breaks. So arm B is asked of every WRITTEN entry carrying a TYPE clause: a site
        // entry, or a TYPE entry written inside a template. Never of a clone a TYPE expansion composes beneath one
        // (its parent carries the TYPE clause): the entry the programmer must change is judged here or above.
        //
        // ⛔ AND SO IS EVERY OTHER RULE THE FIRST LOOP ASKS OF AN ENTRY'S COMPOSED SHAPE (review R1-1 of PB3129). The
        // pruning is a property of the FOREST, not of SR14, so each first-loop arm whose subject the TYPE clause can
        // compose is asked here too, through the same helper and in the same order: §13.18.60.3 SR4 (the CONSTANT
        // RECORD usages, `01 CR CONSTANT RECORD. 05 A TYPE IT.` with IT a USAGE INDEX type — asked FIRST, as above,
        // so the narrower rule names a CONSTANT RECORD pointer), then the EXTERNAL and BASED class rules, then
        // §13.18.5.3 SR2 (`01 A TYPE VT BASED.`, a variable-length group), and last
        // §13.18.60.3 SR14 arm B. §13.18.60.3 SR15 is not here because its TYPE twin is a rule of its own
        // (§13.18.57.3 SR8, COBOL2960), and SR14 arm A's subject is a group that WROTE its usage, which a TYPE entry
        // never does. UsageDeclarationPlacementDriftTests
        // .EveryWrittenEntryScreenIsAskedOfTheTypeEntryToo pins that list and that order.
        foreach (var item in binder.CompositionForest().Where(i => i.TypeName is not null))
        {
            using var _ = binder.Edition.At(item);
            string name = item.CobolName ?? "FILLER";
            bool written = item.Parent is null || DataBinder.IsWrittenEntry(item.Parent);
            if (written && ScreenConstantRecordUsage(binder, item, name)) continue;
            if (ScreenExternalClassItem(binder, item, name)) continue;
            if (ScreenBasedClassObjectItem(binder, item, name)) continue;
            if (ScreenBasedSubjectShape(binder, item, name)) continue;
            if (written) ScreenSr14ElementaryPlacement(binder, item, name);
        }
    }

    /// <summary>§13.18.60.3 SR4 — "The INDEX, MESSAGE-TAG, OBJECT REFERENCE, POINTER, FUNCTION-POINTER, and
    /// PROGRAM-POINTER phrases shall not be specified in a data item described with the CONSTANT RECORD clause, or in
    /// any item subordinate to a data item described with the CONSTANT RECORD clause." True when it reported. One
    /// screen for the WRITTEN-entry loop and the TYPE-entry loop of <see cref="Run"/>.
    ///
    /// <para>A written entry is judged for its OWN phrase: the loop reaches each of its subordinates in turn. An entry
    /// that carries a TYPE clause stands for its whole composed subtree (§13.18.57.4 GR1), whose clones no loop
    /// reaches, so the first of them that carries one of the six phrases is reported ONCE, at the TYPE entry: the
    /// type declaration itself may be legal (a CONSTANT RECORD never contains a TYPEDEF, §13.16.3 SR13), and the
    /// TYPE clause is what the programmer must change.</para></summary>
    private static bool ScreenConstantRecordUsage(DataBinder binder, DataItem item, string name)
    {
        if (!binder.IsConstantRecordItem(item)) return false;
        var carrier = item.TypeName is null
            ? (Sr4ConstantRecordUsage(item) is null ? null : item)
            : DataItem.SelfAndDescendantsOf(item).FirstOrDefault(d => Sr4ConstantRecordUsage(d) is not null);
        if (carrier is null) return false;

        string phrase = Sr4ConstantRecordUsage(carrier)!;
        string described = ReferenceEquals(carrier, item)
            ? $"is described with USAGE {phrase}"
            : $"has a subordinate item '{carrier.CobolName ?? "FILLER"}' described with USAGE {phrase}";
        string acquired = item.TypeName is { } typeName ? $" (by TYPE {typeName}, ISO §13.18.57.4 GR1)" : "";
        binder.Edition.Error(DiagnosticCatalog.UsageConstantRecord, $"data item '{name}' {described}{acquired} and "
            + $"is{(item.IsConstantRecord ? "" : " subordinate to")} a data item described with the CONSTANT RECORD "
            + "clause — the INDEX, MESSAGE-TAG, OBJECT REFERENCE, POINTER, FUNCTION-POINTER, and PROGRAM-POINTER "
            + "phrases shall not be specified there (ISO §13.18.60.3 SR4)");
        return true;
    }

    /// <summary>§13.18.5.3 SR2 — "The subject of the entry shall not be a dynamic-length elementary item or a
    /// variable-length group." (kb/Work PB1213.) True when it reported. Asked of the SHAPE, through the one screen of
    /// that pair. One screen for the WRITTEN-entry loop and the TYPE-entry loop of
    /// <see cref="Run"/>: the shape is the entry's COMPOSED one, however it was acquired.</summary>
    private static bool ScreenBasedSubjectShape(DataBinder binder, DataItem item, string name)
    {
        if (!item.IsBased || VariableLengthCompatibility.DynamicLengthOrVariableLengthGroup(item) is not { } shape)
            return false;
        binder.Edition.Error(DiagnosticCatalog.BasedSubjectVariableLength, $"data item '{name}' is described with "
            + $"the BASED clause and is {shape}; the subject of a BASED entry shall not be a "
            + "dynamic-length elementary item or a variable-length group (ISO §13.18.5.3 SR2)");
        // The BASED placement arms' discipline: the refusal clears the flag, so the entry binds as ordinary
        // storage under an already-failed compile and the cell classifier never adds its carriage verdict
        // ("not yet implemented") for a shape the standard forbids outright.
        item.IsBased = false;
        return true;
    }

    /// <summary>§13.18.60.3 SR14, ARM B — an ELEMENTARY item that carries one of SR14's usage phrases, however it
    /// acquired it (<see cref="Sr14PhraseOf(DataItem)"/>), outside the two placements the rule permits: "an elementary
    /// data item at level 1 or an elementary data item subordinate to a type declaration that includes the STRONG
    /// phrase". One screen for the WRITTEN-entry loop and the TYPE-entry loop of <see cref="Run"/>
    /// (kb/Work PB3129: the TYPE spelling escaped it).</summary>
    private static void ScreenSr14ElementaryPlacement(DataBinder binder, DataItem item, string name)
    {
        if (!Sr14Elementary(item) || Sr14PhraseOf(item) is not { } phrase) return;
        if (Sr14PermittedLevel(item.Level) || DataBinder.UnderStrongTypeDeclaration(item)) return;

        string acquired = item.TypeName is { } typeName ? $" (by TYPE {typeName}, ISO §13.18.57.4 GR1)" : "";
        binder.Edition.Error(DiagnosticCatalog.UsageDeclarationPlacement, $"data item '{name}' is described with "
            + $"USAGE {phrase}{acquired} at level {item.Level:00}, subordinate to "
            + $"'{item.Parent?.CobolName ?? "FILLER"}', which is not a type declaration that includes the "
            + $"STRONG phrase — a USAGE clause with the {Sr14PhraseList} phrase may be specified only for "
            + "an elementary data item at level 1 or an elementary data item subordinate to a type "
            + "declaration that includes the STRONG phrase (ISO §13.18.60.3 SR14)");
    }

    /// <summary>§13.18.22.3 SR4 — "The EXTERNAL clause shall not be specified for a data item of class object or
    /// pointer." True when it reported. One screen for the WRITTEN-entry loop and the TYPE-entry loop of
    /// <see cref="Run"/>: the class is the entry's COMPOSED one, however it was acquired.</summary>
    private static bool ScreenExternalClassItem(DataBinder binder, DataItem item, string name)
    {
        if (!(item.HasExternalClause && SlotWindow.CarriedBySlot(item))) return false;
        binder.Edition.Error(DiagnosticCatalog.ExternalPointerOrObjectItem, $"data item '{name}' is described "
            + $"with USAGE {Sr14PhraseNameOf(item)} — of class "
            + $"{(item.Pic!.Category is PicCategory.ObjectReference ? "object" : "pointer")} — and with "
            + "the EXTERNAL clause; the EXTERNAL clause shall not be specified for a data item of class "
            + "object or pointer (ISO §13.18.22.3 SR4)");
        return true;
    }

    /// <summary>§13.18.5.3 SR1 — "The subject of the entry shall not be of class object." True when it reported;
    /// the twin of <see cref="ScreenExternalClassItem"/>, shared by the same two loops.</summary>
    private static bool ScreenBasedClassObjectItem(DataBinder binder, DataItem item, string name)
    {
        if (!(item.IsBased && item.Pic is { Category: PicCategory.ObjectReference })) return false;
        binder.Edition.Error(DiagnosticCatalog.BasedSubjectOfClassObject, $"data item '{name}' is described "
            + "with USAGE OBJECT REFERENCE — of class object — and with the BASED clause; the subject "
            + "of a BASED entry shall not be of class object (ISO §13.18.5.3 SR1)");
        return true;
    }

    /// <summary>SR14's subject test — "an ELEMENTARY data item", which §8.5.1.3.2 settles structurally: an entry
    /// with subordinates is a group, one without is elementary.
    ///
    /// <para>⛔ NOT <see cref="DataItem.IsGroup"/>, and the difference is load-bearing rather than stylistic.
    /// <c>IsGroup</c> is <c>Pic is null &amp;&amp; Children.Count > 0</c>, and <c>ParseUsage</c> synthesizes a
    /// pointer profile onto an entry BEFORE its subordinates are known — so <c>01 G USAGE POINTER. 05 A PIC X.</c>
    /// binds a header that is neither <c>IsGroup</c> nor honestly elementary, and an <c>IsGroup</c>-keyed screen
    /// waves it through arm A and then blesses it in arm B as "a level-1 elementary pointer". Measured exactly
    /// that way on the first build of this screen: every other shape rejected and the group-usage shape ran
    /// clean. (<c>ResolveIndexItems</c> sheds that synthesized profile for a group header whose usage is INDEX or
    /// OBJECT REFERENCE, but not for POINTER / PROGRAM-POINTER — a real modelling asymmetry, made unreachable on
    /// conforming source by arm A rather than papered over, since the shape it describes is nonconforming.)</para>
    /// </summary>
    private static bool Sr14Elementary(DataItem d) => d.Children.Count == 0;

    /// <summary>SR14's five usage phrases, as the rule spells them — used in every message so the diagnostic
    /// quotes the rule rather than a paraphrase of it.</summary>
    private const string Sr14PhraseList = "MESSAGE-TAG, OBJECT REFERENCE, POINTER, FUNCTION-POINTER, or "
        + "PROGRAM-POINTER";

    /// <summary>⛔ THE ONE SOURCE OF TRUTH FOR "WHICH OF SR14's FIVE PHRASES DOES THIS ITEM CARRY" (kb/Work PB819): the
    /// item's own WRITTEN usage clause when it wrote one, else the usage of its resolved profile — a usage acquired
    /// by §13.18.60.4 GR1 inheritance, a TYPE clone or a SAME AS copy has no written clause of its own and is visible
    /// only there. Null for every other usage, INDEX included (SR14 names five phrases where SR4 names six; see
    /// <see cref="Sr4ConstantRecordUsage"/>).
    /// <para>It asks the PHRASE and never the resolved CATEGORY, because the rule is written over the phrase
    /// ("A USAGE clause with the MESSAGE-TAG, OBJECT REFERENCE, POINTER, FUNCTION-POINTER, or PROGRAM-POINTER
    /// phrase") and a usage the compiler REFUSES by name (MESSAGE-TAG, declined non-support, COBOL1943) binds a
    /// recovery profile whose category is no analysis: arm B used to key on the class
    /// (<c>PointerObjectClass</c>, the §13.18.44.3 REDEFINES screen's predicate), so it was blind to exactly the usages
    /// whose keyword is refused while arm A, reading the written clause, still reported them — a staged or declined
    /// usage took a different path from a landed one. FUNCTION-POINTER was the first such cell and gained a model
    /// (kb/Work PB817); MESSAGE-TAG is the live one. A usage added tomorrow is covered by
    /// <see cref="ItemCategory.Sr14PhraseOf(Usage?)"/>'s recorded verdict
    /// (<c>UsageDeclarationPlacementDriftTests</c>) instead of by this screen's author remembering it.</para></summary>
    private static string? Sr14PhraseOf(DataItem d) =>
        ItemCategory.Sr14PhraseOf(d.OwnUsage) ?? ItemCategory.Sr14PhraseOf(d.Pic?.Usage);

    /// <summary>The phrase to NAME for an item the callers already know is slot-carried (class object or pointer):
    /// <see cref="Sr14PhraseOf(DataItem)"/>, with a fall-through that says so rather than GUESSING a phrase — it is
    /// unreachable for a slot-carried item (every slot category's profile carries its usage), and a default of
    /// "POINTER" would make the one symptom of that invariant breaking look like an ordinary correct verdict.
    /// A message that cannot be false is worth one arm.</summary>
    private static string Sr14PhraseNameOf(DataItem d) => Sr14PhraseOf(d)
        ?? $"an unnamed usage of category {d.Pic?.Category.ToString() ?? "(none)"} — this is a compiler defect: a "
            + "slot-carried item whose profile names no §13.18.60.3 SR14 usage";

    /// <summary>SR14's "at level 1" arm. ⛔ 77 SATISFIES IT — see this file's header for the derivation
    /// (§8.5.1.3.2 "no true concept of level"; §13.11.1 makes the level-1 and level-77 spellings alternatives
    /// for one non-hierarchical data element). ONE named predicate so the determination is a one-line change.
    /// A level-66 RENAMES entry and a level-88 condition-name are not nodes in the forest at all — the binder's
    /// level stack attaches an 88 to its conditional variable — so this predicate never sees them.</summary>
    private static bool Sr14PermittedLevel(int level) => level is 1 or 77;

    /// <summary>Which of ISO §13.18.60.3 SR4's SIX usage phrases an entry names, or null. ⛔ INDEX IS IN THAT
    /// LIST and NOT in <see cref="Sr14PlacementClass"/>'s — SR4 reads "The INDEX, MESSAGE-TAG, OBJECT
    /// REFERENCE, POINTER, FUNCTION-POINTER, and PROGRAM-POINTER phrases", SR14 reads the same list without
    /// INDEX. Keeping them as two predicates is the whole guard against a future "unification" that would start
    /// rejecting legal <c>05 IX USAGE INDEX.</c>; the unit drift test asserts the difference is exactly INDEX.
    /// <para>⛔ BOTH PHRASE READERS NOW LIVE ON <see cref="ItemCategory"/> (kb/Work PB391), because SR4's
    /// population is also §14.7.6 rule 4's CLASS exclusion and §13.16.3 SR24 e)'s, and the CORRESPONDING binder
    /// is outside <c>DataBinder</c>. This file keeps only what is about USAGE-CLAUSE PLACEMENT.</para>
    /// <para>Reads the RESOLVED class as well as the written clause, because SR4's reach is "in a data item
    /// described with the CONSTANT RECORD clause, or in any item subordinate to" it — an inherited usage inside
    /// that subtree is as much "specified in" it as a written one (§13.18.60.4 GR1).</para></summary>
    private static string? Sr4ConstantRecordUsage(DataItem d) =>
        ItemCategory.Sr4PhraseOf(d.OwnUsage)
        ?? (Sr14Elementary(d) ? ItemCategory.Sr4PhraseOf(d.Pic?.Usage) : null);
}
