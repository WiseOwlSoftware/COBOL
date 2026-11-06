// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding.Passes;

/// <summary>
/// The ClassifyRedefinesClasses pass: row 14 of the per-unit resolve pipeline (SignResolved to RedefinesClassified).
/// </summary>
internal sealed class ClassifyRedefinesClassesPass : IBindPass
{
    /// <inheritdoc/>
    public string Name => "ClassifyRedefinesClasses";

    /// <inheritdoc/>
    public PassPhase Requires => PassPhase.SignResolved;

    /// <inheritdoc/>
    public PassPhase Produces => PassPhase.RedefinesClassified;

    /// <inheritdoc/>
    public PassViews Views => PassViews.Appends(FactSetKind.ImageForcedItems);

    /// <summary>⛔ THE CLAUSE SCREENS SCREEN THE CLAUSE (kb/Work PB836) — §13.18.44.3 SR5 (sentence 1), SR12, SR14 and
    /// SR17, asked once per WRITTEN REDEFINES clause over its subject <paramref name="item"/> and its DIRECT
    /// data-name-2 <paramref name="target"/>, never the chained anchor. An FD/SD's later level-1 records (§13.18.33.4
    /// GR3) and a SAME RECORD AREA file's record (§12.4.6.4.4 GR2) share the area IMPLICITLY: no clause, no operand,
    /// and none of these rules. Asked of a storage item by <see cref="Run"/> before it builds the
    /// item's class, and of a TYPEDEF template member, which has no class (kb/Work PB2071).</summary>
    private static void ScreenRedefinesClause(DataBinder binder, DataItem item, DataItem target)
    {
        // §13.18.44.3 SR12/SR14 (kb/Work PB179; the skeptic round moved this screen HERE, per WRITTEN
        // ENTRY and BEFORE dissolution — ClassifyRedefinesClasses' dissolve loop removes nested classes, so a per-class
        // screen let an inner entry's violation escape the diagnostic and fall to the outer class's
        // staged-loud arm: same verdict, wrong posture): SR12 bars the SUBJECT being of class
        // object/message-tag/pointer or a strongly-typed group; SR14 bars data-name-2 (the DIRECT
        // target as written) likewise, plus "subordinate to a strongly-typed group item". These are the
        // rules' LETTER — the drafting contrast with SR9's "nor any entry subordinate to it" shows they
        // name the entry-level items only; a NESTED pointer leaf is §13.18.60.3 SR14's territory — now
        // screened at its own declaration by CheckUsageDeclarations (kb/Work PB183, COBOL1724), with
        // ComputeTier's backstop arm kept as the recovery-path guard behind it.
        if (Sr12Sr14Violation(item, target) is { } srv)
            binder.Edition.Error(DiagnosticCatalog.RedefinesPointerObject, srv);
        // §13.18.44.3 SR17, the SAME per-written-entry posture and for the same reason (kb/Work PB177 arm C):
        // "Neither data-name-2 nor the subject of the entry shall be a variable-length group or a
        // dynamic-length elementary item." A SYMMETRIC rule, so BOTH sides are tested — the two-arm
        // discipline applied to the screen itself. It must run HERE, before the dissolve loop, or a nested
        // entry's violation escapes into the outer class's staged-loud arm (the PB179 skeptic round's
        // finding, which is why SR12/SR14 sit here).
        // ⛔ AND IT CLOSES A SILENT MIS-MODEL, not just an under-rejection: StorageFormPass.Classify returns
        // DynamicString for an IsDynamicLength item BEFORE reaching its Tier-B view arm, so such a view got
        // its OWN disjoint native string — two storages for one shared area (§13.18.44.4 GR1 says one), with
        // no diagnostic. Rejecting the entry makes that path unreachable.
        foreach (var (side, sideItem) in new[]
                 { ("the subject of the REDEFINES entry", item), ("data-name-2 of a REDEFINES entry", target) })
            if (VariableLengthCompatibility.DynamicLengthOrVariableLengthGroup(sideItem) is { } shape)
                binder.Edition.Error(DiagnosticCatalog.RedefinesVariableLength,
                    $"'{sideItem.CobolName ?? sideItem.ClrName}' is {side} but is {shape}: neither "
                    + "data-name-2 nor the subject of the entry shall be a variable-length group or a "
                    + "dynamic-length elementary item (ISO §13.18.44.3 SR17)");
        // §13.18.44.3 SR5 SENTENCE 1 — the OBJECT side, per written entry like its two neighbours above.
        // "The data description entry for data-name-2 shall not contain an OCCURS clause." ANY format: the
        // fixed OCCURS of Format 1, Format 2's occurs-depending table, and Format 4's dynamic-capacity table
        // are all THE OCCURS CLAUSE (§13.18.38), so the predicate is `IsTable` — the union. ⛔ NEITHER half
        // alone works, and each is a live trap this repo has already sprung once: `Occurs is not null` is
        // the FIXED physical capacity and is NULL for a Format-4 table (the CONTROL SR3 arm's defect), while
        // `OccursSpec is not null` is NULL for a plain keyless fixed table, which OdoBindOccursSpec
        // deliberately leaves allocation-free (measured: with that spelling `05 T PIC X(3) OCCURS 4. 05 R
        // REDEFINES T PIC X(12).` still compiled clean — the very shape this screen exists for).
        // ⛔ SENTENCE 2 IS THE LIMIT OF THIS ARM: "However, data-name-2 may be subordinate to an item whose
        // data description entry contains an OCCURS clause." So the test is on data-name-2's OWN entry —
        // an ancestor walk here would reject legal source.
        // ⛔ AND THE DEPENDING SHAPE IS DELIBERATELY EXCLUDED, so one entry never draws two diagnostics from
        // one rule: SENTENCE 4 ("Neither the original definition nor the redefinition shall include an
        // occurs-depending table") is COBOL0855's, whose population is WIDER (an ODO table anywhere in
        // either definition, not merely on data-name-2's own entry) and which already rejects every entry
        // this arm would also match. One rule, four sentences, two disjoint screens.
        if (target.IsTable && target.OccursSpec?.DependingName is null)
            binder.Edition.Error(DiagnosticCatalog.RedefinesTargetOccurs,
                $"'{item.CobolName ?? item.ClrName}' REDEFINES '{target.CobolName ?? target.ClrName}', "
                + "whose data description entry contains an OCCURS clause: the data description entry for "
                + "data-name-2 shall not contain an OCCURS clause (ISO §13.18.44.3 SR5) — data-name-2 may be "
                + "SUBORDINATE to an item that has one, but shall not carry one itself");
    }

    /// <summary>Group every redefining entry with the non-redefining anchor it ultimately overlays (SR7/SR11) into a
    /// <see cref="RedefinesClass"/>, mark the anchor canonical and every other member a view, then assign the class a
    /// tier (D &gt; C &gt; B &gt; A) and its class-max width, and propagate view-suppression to each view's
    /// subordinates (SR9 — no VALUE on a subordinate of a redefiner). (DESIGN §4.2.)</summary>
    public void Run(DataBinder binder)
    {
        var byAnchor = new Dictionary<DataItem, RedefinesClass>();
        foreach (var item in binder.AllItems())
        {
            if (item.RedefinesTarget is null) continue;
            using var _ = binder.Edition.At(item);
            if (item.RedefinesKind is RedefinitionKind.Clause) ScreenRedefinesClause(binder, item, item.RedefinesTarget);
            DataItem anchor = item;
            // Chase data-name-2 to the non-redefining anchor. A CHAIN (X REDEFINES Y, Y REDEFINES Z) is
            // SR7-ILLEGAL (the skeptic round corrected this comment's SR11 miscitation — SR11 is the
            // nested-under-a-redefinition PERMISSION) and diagnosed at resolve; it reaches here only under
            // --permissive, where the anchor walk still yields one shared area.
            while (anchor.RedefinesTarget is { } t) anchor = t;
            if (!byAnchor.TryGetValue(anchor, out var cls))
            {
                cls = new RedefinesClass { Canonical = anchor };
                cls.Members.Add(anchor);
                anchor.Class = cls;
                byAnchor[anchor] = cls;
            }
            cls.Members.Add(item);
            item.Class = cls;
            item.IsCanonical = false;
        }
        // A TYPEDEF template member's REDEFINES clause answers the same clause rules (kb/Work PB2071) and builds NO
        // class: a type declaration has no storage associated with it (ISO §13.18.58.4 GR2).
        foreach (var item in binder.TemplateItems())
            if (item.RedefinesKind is RedefinitionKind.Clause && item.RedefinesTarget is { } target)
            {
                using var _ = binder.Edition.At(item);
                ScreenRedefinesClause(binder, item, target);
            }

        // ONE shared area per overlay nest (ISO §13.18.44 — a REDEFINES nested under another class's member
        // shares THAT storage): a class whose ANCHOR lies inside another class's subtree — its ancestor chain
        // crosses a redefining member or another class's anchor (NC252A's `RDEF8 REDEFINES RDF8` under the view
        // `REDEF11 REDEFINES REDEF10`) — DISSOLVES into the outer class. The outer subtree walk below assigns
        // every nested item its window over the one backing (an inner redefiner starts at its target's
        // already-assigned offset), so a dissolved class needs no members of its own; keeping it would let its
        // later walk re-claim the subtree and emit a backing inside a suppressed view struct (CS0103).
        foreach (var (anchor, cls) in byAnchor.ToList())
            for (var a = anchor.Parent; a is not null; a = a.Parent)
                if (a.RedefinesTargetName is not null || (byAnchor.ContainsKey(a) && !ReferenceEquals(byAnchor[a], cls)))
                {
                    byAnchor.Remove(anchor);
                    break;
                }

        foreach (var cls in byAnchor.Values)
        {
            var tier = ComputeTier(cls, out string? reject);
            // A class joined by the file section's IMPLICIT redefinition (kb/Work PB836) is screened by the
            // implicit-area arm below, never by the REDEFINES-clause verdicts that follow — those name a clause
            // and a data-name-2 the source does not contain.
            bool implicitArea = cls.Members.Any(m => m.RedefinesImplicitly);
            // ⛔ CITATION REPAIRED **TWO-SIDEDLY**, AND THE SCREEN NARROWED TO THE SUBJECT SIDE (kb/Work PB177
            // arm C, then its follow-up). Three moves, and the middle one was itself wrong on one side:
            //  1. The arm used to cite "§13.18.44 SR5" for BOTH sides.
            //  2. The repair read SR5's FOURTH sentence ("Neither the original definition nor the redefinition
            //     shall include an occurs-depending table" — COBOL0855's rule) and concluded the whole rule
            //     was about something else, so it wrote "which NO syntax rule literally names". ⛔ THAT IS TRUE
            //     ONLY OF THE SUBJECT SIDE. SR5's FIRST sentence is "The data description entry for data-name-2
            //     shall not contain an OCCURS clause", and OCCURS DYNAMIC is Format 4 OF the OCCURS clause
            //     (§13.18.38) — so the OBJECT side is named outright, and is now COBOL1701 per written entry.
            //     Reading one sentence of a four-sentence rule and generalising is the same inheritance failure
            //     CLAUDE.md rule 1 names, one level down: the clause NUMBER was right and the SENTENCE was not.
            //  3. The dynamic-capacity SUBORDINATE case is §13.18.44.3 SR17's (a group with such a table
            //     subordinate IS a "variable-length group", §8.5.1.12.1) and is screened per WRITTEN ENTRY above
            //     as COBOL1698.
            // WHAT REMAINS HERE IS THE SUBJECT THAT IS ITSELF A DYNAMIC-CAPACITY TABLE, and for THAT side the
            // "no syntax rule names it" reading holds: §13.18.38.3 carries no REDEFINES restriction, and SR17
            // does not reach an ELEMENTARY dynamic table (§8.5.1.12.1's "variable-length group" is defined over
            // items SUBORDINATE to the group). So cite what actually decides it — §13.18.44.4 GR1 ("Storage
            // association for the subject of the entry starts at the first bit of the data item referenced by
            // data-name-2 and continues over an area sufficient to contain the number of bits required by the
            // data item referenced by the subject of the entry") against §8.5.1.9.1's dynamic-capacity model,
            // whose "physical and logical capacities may vary during execution": there is no fixed area to
            // associate. A WiseOwl COBOL storage-model rejection, honestly labelled, not a borrowed rule number.
            // (The message names the OFFENDING MEMBER, not the class canonical — it used to say "the
            // dynamic-capacity table in '<canonical>'" even when the canonical was the ordinary fixed item and
            // the dynamic table was the REDEFINING entry.)
            if (!implicitArea
                && cls.Members.FirstOrDefault(m => !ReferenceEquals(m, cls.Canonical) && m.IsDynamicTable) is { } dynSubject)
            {
                using var dynamicSubjectAt = binder.Edition.At(dynSubject);   // the REDEFINES entry as written; this pass runs post-build (kb/Work PB2034)
                binder.Edition.Error(DiagnosticCatalog.RedefinesSubjectIsDynamicTable, $"REDEFINES entry '{dynSubject.CobolName ?? dynSubject.ClrName}' is "
                    + "itself a dynamic-capacity table: its capacity \"may vary during execution\" (ISO "
                    + "§8.5.1.9.1), so it has no fixed area for the storage association §13.18.44.4 GR1 requires; "
                    + "it shall not be the subject of a REDEFINES");
                tier = RedefinesTier.Rejected;
                reject ??= "REDEFINES subject is a dynamic-capacity table (§13.18.44.4 GR1 / §8.5.1.9.1, D9)";
            }
            // The §13.18.44.3 SR5 sentence-1 TIER verdict, same posture as SR12/SR14's and SR17's below (the
            // DIAGNOSTIC fired per written entry, before dissolution — no second diagnostic here). The tier
            // matters on its own: without it the class model is built as though the OCCURS-bearing object were
            // a legal single area, and the offsets/width walk below lays a view over one occurrence's worth of
            // storage. The predicate is the RULE's (`OccursSpec is not null`, every format) even though the
            // depending shape's DIAGNOSTIC belongs to COBOL0855 — a tier is not a message.
            foreach (var m in cls.Members)
                if (m.RedefinesKind is RedefinitionKind.Clause && m.RedefinesTarget is { IsTable: true } occTarget)
                {
                    tier = RedefinesTier.Rejected;
                    reject = $"REDEFINES over '{occTarget.CobolName ?? occTarget.ClrName}', whose data "
                        + "description entry contains an OCCURS clause (ISO §13.18.44.3 SR5)";
                }
            // The SR12/SR14 TIER verdict for a retained class (the DIAGNOSTIC fired per written entry in the
            // grouping loop above, before dissolution — no second diagnostic here): the class rejects with
            // the spec-required reason, OVERRIDING ComputeTier's staged-loud one (its backstop arm also
            // matches an entry-level pointer).
            foreach (var m in cls.Members)
                if (!ReferenceEquals(m, cls.Canonical) && m.RedefinesKind is RedefinitionKind.Clause
                    && m.RedefinesTarget is { } srTarget && Sr12Sr14Violation(m, srTarget) is { } srReason)
                {
                    tier = RedefinesTier.Rejected;
                    reject = srReason;
                }
            // The §13.18.44.3 SR17 TIER verdict, same posture as SR12/SR14's above (the DIAGNOSTIC fired per
            // written entry, before dissolution — no second diagnostic here).
            // ⛔ THE TIER IS BELT-AND-BRACES, AND THE COMMENT HERE USED TO OVERCLAIM IT. It said "Rejecting the
            // CLASS makes that path structurally unreachable rather than merely unreached because a diagnostic
            // happened to stop the compile." MEASURED FALSE (RedefinesClassificationTests, once its assertion
            // was made non-inert): StorageFormPass.Classify orders its arms (1) IsDynamicTable, (1b)
            // IsDynamicLength → DynamicString, and only THEN (2) the REDEFINES-view arm — so arm 1b returns
            // before the tier is ever read, and a Rejected class's dynamic-length view still classifies as
            // DynamicString. A class DISSOLVED by the nested-anchor loop below bypasses the tier loop entirely,
            // which is the same hole from the other side. WHAT ACTUALLY PREVENTS THE DISJOINT STORAGE REACHING
            // A USER'S PROGRAM IS THE COBOL1698 DIAGNOSTIC, which is fatal. The tier verdict is a second,
            // independent verdict at the modelling layer and is worth keeping as one — every consumer that asks
            // the CLASS gets the right answer — but "structurally unreachable" was a claim about a path this
            // code does not close.
            if (!implicitArea)
                foreach (var m in cls.Members)
                    if (VariableLengthCompatibility.DynamicLengthOrVariableLengthGroup(m) is { } vlShape)
                    {
                        tier = RedefinesTier.Rejected;
                        reject = $"REDEFINES entry side '{m.CobolName ?? m.ClrName}' is {vlShape} "
                            + "(ISO §13.18.44.3 SR17)";
                    }
            // ⛔ THE IMPLICIT-AREA ARM (kb/Work PB836, narrowed by PB981). No syntax rule forbids a file's records
            // from being a dynamic-length item, a variable-length group or a pointer — §13.18.33.4 GR3 simply makes
            // them share one area — and since PB981 none of those reaches a class at all: they are OUT-OF-LINE
            // records (FileModel.IsOutOfLineRecord, determination D-FRA), which LinkImplicitRecordArea never links,
            // and they share the area at the transfer boundary (FileModel.OutOfLineRecords). What can still reach
            // here Rejected is a ComputeTier residue of a CHARACTER-window record (ByteWindowResidueOf), staged
            // LOUD rather than let through. The reason names the rule the program DID use, never §13.18.44.
            if (implicitArea)
            {
                if (tier is RedefinesTier.Rejected)
                {
                    var rule = cls.Members.Any(m => m.RedefinesKind is RedefinitionKind.SameRecordArea)
                        ? "a record-area SAME clause makes its files' records an implicit redefinition of one area "
                          + "(ISO §12.4.6.4.4 GR2)"
                        : cls.Members.Any(m => m.RedefinesKind is RedefinitionKind.ImplicitCommunicationRecord)
                        ? "the record description entries following a CD entry implicitly redefine its record area "
                          + "(ISO/IEC 1989:2002 A.1.2.2)"
                        : "multiple level 1 entries of one FD or SD are implicit redefinitions of the same area "
                          + "(ISO §13.18.33.4 GR3)";
                    using var __ = binder.Edition.At(cls.Members.FirstOrDefault(m => m.RedefinesImplicitly) ?? cls.Canonical);
                    binder.Edition.Error(DiagnosticCatalog.ImplicitRecordAreaShape,
                        $"the record area of '{cls.Canonical.CobolName ?? cls.Canonical.ClrName}' is shared — {rule} — "
                        + $"and {reject}: a shared record area of that shape is recognized but not yet implemented");
                }
            }
            // The width is a member table's FULL STORAGE extent (every occurrence) — §13.18.44.4 GR1: when the
            // subject "requires more bits than the data item referenced by data-name-2, the storage area
            // allocated … is the number of bits required by the data item referenced by the subject", i.e. the
            // maximum over the members. In BYTES, never character positions (kb/Work PB231): the two differ for
            // a NATIONAL member, whose §13.18.60.4 GR8 size is two bytes per position under D-N1, and a
            // character-position maximum would under-size the shared backing by half of it.
            // ONE verdict application (P5.11d).
            cls.Classify(tier, cls.Members.Max(m => m.ByteWidth * (m.Occurs ?? 1)), reject);
            // Each top-level member overlays the area from its start (a REDEFINES begins at the target's first
            // position, SR10); a subordinate accumulates its window offset within the member. Subordinates of any
            // member are themselves views (suppressed field, SR9).
            foreach (var member in cls.Members)
                DataBinder.AssignClassOffsets(member, 0, cls);
            // A Tier-B (string-canonical) numeric-DISPLAY view reads/writes its window through the character pipeline
            // (CobolNum.ParseDisplay / FormatDisplay) — the same image path used for whole-group numeric leaves.
            if (cls.Tier == RedefinesTier.StringCanonical)
                foreach (var leaf in cls.Members.SelectMany(DataBinder.LeavesOf))
                {
                    if (leaf.Pic is { Category: PicCategory.Numeric, IsFloat: false, Usage: Usage.Display })
                        binder.State.ImageForcedItems.Add(leaf);   // the collected image fact (a Ptr-FORCED StringCanonical class deliberately never records — its display leaves stay TierBWindow)
                    // A national-form numeric leaf reaches the arm below (kb/Work PB646): its window over the
                    // one backing is Place.NationalWindow's transcoded CHARACTERS, not its bytes, but the
                    // image-forced verdict is the same one and this is the arm that already carried it.
                    else if (leaf.Pic is { HasImageByteForm: true, Usage: not Usage.Display })   // the ONE image predicate (kb/Work PB164 — COMP-5/BINARY-* windows included)
                        // A fixed-point BINARY/PACKED leaf of a Tier-B class is image-stored too: its window over
                        // the one string backing IS its BYTES — radix-2 two's complement or BCD, of exactly
                        // PicInfo.StorageWidth (ISO §13.18.60.4 GR4/GR11 implementor representation;
                        // DESIGN §14.4, V59). Every accessor threads the leaf's OWN `_P_` profile
                        // through CobolNum.FormatImage/ParseImage, so no profile rewrite is needed or wanted:
                        // the sign lives in the bytes (two's complement / the sign nibble), and SignKind — a
                        // DISPLAY concern — stays the declared BinaryMinus, which is what DISPLAY of the leaf
                        // must still render. The former rewrite to a trailing-overpunch ZONED image existed only
                        // because the window used to be Digits characters wide.
                        binder.State.ImageForcedItems.Add(leaf);
                }
        }
    }

    /// <summary>Assign a redefines class its tier (DESIGN §4.2; the Step D arm-1 dissolution made the
    /// cascade D &gt; B &gt; A — Tier C dissolved into B: every numeric leaf kind's byte form is pinned, so a
    /// mixed-USAGE pun is an ordinary byte-window class. D rejects the nested pointer/object backstop and the
    /// NATIONAL residue.</summary>
    // (ContainsDynamicTable was deleted with kb/Work PB177 arm C: its SUBORDINATE half is §13.18.44.3 SR17's,
    // now screened per written entry through DataItem.HasVariableLengthSubordinate — §8.5.1.12.1's own
    // definition of "variable-length group" — and its SELF half is the narrowed `m.IsDynamicTable` test at the
    // COBOL1525 arm. Two spellings of one walk collapsed into the predicate the standard actually defines.)

    /// <summary>The §13.18.44.3 SR12/SR14 class test (kb/Work PB179): object, message-tag and pointer classes occupy no
    /// character positions and can neither overlay nor be overlaid.
    /// <para>⛔ CLASS INDEX IS NOT HERE and must never be added: §13.18.60.3 SR4 is the rule whose list includes
    /// INDEX, and it has its own predicate. §13.18.60.3 SR14's declaration-placement screen
    /// (<c>DataBinder.UsageDeclaration.cs</c>) does NOT ask this CLASS question: SR14 is written over the five USAGE
    /// PHRASES, so it reads the phrase (<c>Sr14PhraseOf</c>), which sees a usage the compiler refuses by name
    /// where a class cannot (kb/Work PB819). <c>UsageDeclarationPlacementDriftTests</c> pins the two readers'
    /// agreement on every profile that has a class.
    /// </para>
    /// <para>⛔ AND IT IS NOW THE STORAGE PREDICATE TOO. <see cref="SlotWindow.CarriedBySlot"/> — "does this
    /// member ride the storage area's MANAGED SLOTS rather than its bytes?" (kb/Work PB231, the pointer third)
    /// — is the same population for the same reason: a class object, message-tag or pointer item holds a managed reference and
    /// no bytes. It is the DEFINITION and this is the delegation, so the third spelling cannot drift from the
    /// first two either.</para></summary>
    private static bool PointerObjectClass(DataItem d) => SlotWindow.CarriedBySlot(d);

    /// <summary>ONE §13.18.44.3 SR12/SR14 verdict per WRITTEN entry — <paramref name="subject"/> is the item
    /// carrying the REDEFINES clause, <paramref name="target"/> its DIRECT data-name-2 (never the chained
    /// anchor). Returns the violation text (the diagnostic message AND the class RejectReason), or null.
    /// Each arm's message names the arm that actually fired (the skeptic round: a subordinate-to-strong
    /// violation was described as "is a strongly-typed group" — false of the item named).
    /// <para>⛔ The SR14 "subordinate to a strongly-typed group item" arm has NO same-root exemption (kb/Work
    /// PB1282). It once fired only when the subject sat OUTSIDE the target's strong subtree, which no entry can
    /// do — a subject and its data-name-2 are siblings, so they share one parent and therefore one strong root —
    /// so the disjunct was vacuous. The way to meet it is a REDEFINES written inside a STRONG typedef template:
    /// §13.18.58.4 GR3 makes every subordinate description of the type "assumed by data defined using the
    /// type-name" (and §13.18.57.4 GR1 codes it "as though" in the TYPE subject), and §8.5.3.1 makes that subject
    /// strongly typed ("described with a TYPE clause that references a type declaration specifying the STRONG
    /// phrase"), so in each TYPE subject data-name-2 is "an item subordinate to a strongly-typed group item".
    /// In the template itself it is not: the template root carries TYPEDEF, not a TYPE clause, so an unused STRONG
    /// template holding such a REDEFINES conforms (kb/Work PB2071). PB183's companion derivation answered
    /// §13.18.57.3 SR4 — the subject of the TYPE entry — which is a different rule from this one;
    /// <c>CheckStrongTypeDeclarations</c> still leaves the internal case to this screen so it is reported once.</para></summary>
    private static string? Sr12Sr14Violation(DataItem subject, DataItem target)
    {
        if (PointerObjectClass(subject))
            return $"'{subject.CobolName ?? subject.ClrName}' is the subject of a REDEFINES entry but is of "
                + "class object, message-tag, or pointer (ISO §13.18.44.3 SR12)";
        if (StrongTypeModel.IsStrongGroup(subject))
            return $"'{subject.CobolName ?? subject.ClrName}' is the subject of a REDEFINES entry but is a "
                + "strongly-typed group item (ISO §13.18.44.3 SR12)";
        if (PointerObjectClass(target))
            return $"'{target.CobolName ?? target.ClrName}' is redefined (data-name-2) but is of class "
                + "object, message-tag, or pointer (ISO §13.18.44.3 SR14)";
        if (StrongTypeModel.IsStrongGroup(target))
            return $"'{target.CobolName ?? target.ClrName}' is redefined (data-name-2) but is a strongly-typed "
                + "group item (ISO §13.18.44.3 SR14)";
        if (StrongTypeModel.StrongRoot(target) is { } tRoot)
            return $"'{target.CobolName ?? target.ClrName}' is redefined (data-name-2) but is subordinate to "
                + "the strongly-typed group item "
                + $"'{tRoot.CobolName ?? tRoot.ClrName}' (ISO §13.18.44.3 SR14)";
        return null;
    }

    private static RedefinesTier ComputeTier(RedefinesClass cls, out string? reject)
    {
        reject = null;
        var leaves = cls.Members.SelectMany(DataBinder.LeavesOf).ToList();

        // ⛔ THE TIER-C ARM IS GONE (the Step D arm-1 dissolution — DESIGN-data-model §2.3; kb/Work PB164's
        // LAST codegen half): every NUMERIC leaf kind carries a pinned byte form (NumericByteForm — waves
        // 1–2 + R40), the Tier-B geometry has been byte-form since V59 (§13.18.44.4 GR1 associates storage
        // at the bit over "the number of bits required"; GR2 grants every entry's name reference to that
        // storage), and the StoreAsImage loop's HasImageByteForm mark goes LIVE for float/COMP-5/BINARY-*/
        // INDEX leaves — each lane it feeds carries its own byte-level pin (the windowed-float read/store
        // arms, the unsigned ParseImage twins, the float seeds, the StorageFormPass canonical promotion).
        // A mixed class is simply Tier B: one string backing IS the shared storage area, every member an
        // (offset, StorageWidth) byte window.
        // Tier D backstop — a pointer/object-class LEAF nested inside a member (kb/Work PB179): NOT
        // §13.18.44.3 SR12/SR14's letter (those bar the ENTRY-level items — screened per written entry in
        // ClassifyRedefinesClasses). The skeptic round derived that §13.18.60.3 SR14 makes the nested
        // declaration itself NONCONFORMING (a pointer/object USAGE is legal only at level 1 or under a
        // STRONG type — and the strong case trips the entry-level screen via its enclosing group).
        // ⛔ THAT SCREEN NOW EXISTS — CheckUsageDeclarations (DataBinder.UsageDeclaration.cs, COBOL1724,
        // kb/Work PB183), which runs EARLIER in the pipeline and rejects the declaration itself, so on
        // conforming source this arm is unreachable-by-construction exactly as predicted. It is KEPT, not
        // deleted: it remains the loud guard for the recovery paths and for any future mode that admits a
        // nonconforming declaration, and without it such a leaf would reach the byte-window machinery as a
        // silent zero-width Tier-B alias. Never that.
        //
        // ⛔ Tier D IS NOW ONE CALL — ByteWindowResidueOf, the SHARED gate this screen and the cell forcer
        // (ForceStringCanonical) both route through. It used to be two hand-written lists, one per surface,
        // and they drifted: PB203 admitted USAGE BIT leaves HERE and nowhere else, so the byte-identical
        // BASED / EXTERNAL / ADDRESS-OF spelling of a conforming record still drew a bind-time reject
        // (kb/Work PB231). The residue clauses — the pointer/object backstop and the RESIDUE-11 national
        // layout — now live once, so the landing that discharges either one opens BOTH surfaces in a single
        // edit. ByteWindowResidueDriftTests pins the routing.
        // The class's area is named by the construct that shares it — a REDEFINES clause, or the file section's
        // implicit redefinition (kb/Work PB836), which has no REDEFINES to name.
        string where = cls.Members.Any(m => m.RedefinesImplicitly)
            ? $"in the shared record area of '{cls.Canonical.CobolName}'"
            : $"under REDEFINES of '{cls.Canonical.CobolName}'";
        if (leaves.Select(DataBinder.ByteWindowResidueOf).FirstOrDefault(r => r is not null) is { } residue)
        {
            reject = $"{residue} {where} — not yet implemented";
            return RedefinesTier.Rejected;
        }

        // ⛔ THE SECOND, DIFFERENT QUESTION — THE CARRIER (kb/Work PB231, the pointer third). ByteWindowResidueOf
        // answers "may this leaf ride a shared byte-window area?", and since PB231 a pointer-class leaf may: its
        // bytes are reserved and its VALUE rides the area's MANAGED SLOTS (Place.SlotWindow). Those slots live on
        // a StorageCell, which the three CELL surfaces have (RedefinesClass.IsCellBacked, set by the ONE forcer
        // ForceStringCanonical) and a REDEFINES class's plain stored string backing does NOT. That is a property
        // of the CARRIER, not of the leaf, so it is asked here rather than smuggled back into the shared gate —
        // which would re-create the very two-arm divergence PB231's collapse removed.
        // ⚠ UNREACHABLE THROUGH A REDEFINES CLAUSE on conforming source, by three rules that were re-derived
        // rather than inherited: §13.18.44.3 SR12 — "The REDEFINES clause shall not be specified for a data item
        // of class object, message-tag, or pointer or a strongly-typed group item"; SR14 — "Data-name-2 shall not
        // be of class object, message-tag, or pointer, a strongly-typed group item, or an item subordinate to a
        // strongly-typed group item"; and §13.18.60.3 SR14, which admits a pointer USAGE "only for an elementary
        // data item at level 1 or an elementary data item subordinate to a type declaration that includes the
        // STRONG phrase". The FILE SECTION's implicit area, which used to reach it (kb/Work PB836 — a level-1
        // `USAGE POINTER` record beside another record of the same FD, legal by that third rule and joined by
        // §13.18.33.4 GR3 with no REDEFINES clause for SR12/SR14 to bar), no longer does: since kb/Work PB981 a
        // pointer-class record is an OUT-OF-LINE record (FileModel.IsOutOfLineRecord, determination D-FRA) that
        // LinkImplicitRecordArea never links. The arm stays the loud guard for any path that would alias a
        // managed slot over a stored string backing — never a silent alias of reserved bytes.
        if (!cls.IsCellBacked && leaves.FirstOrDefault(SlotWindow.CarriedBySlot) is { } managed)
        {
            reject = $"'{managed.CobolName ?? "FILLER"}' is of a pointer class, whose value rides the storage "
                + "area's managed slots (the implementor-defined D-SLOT representation, DOC-A.1-210 and DOC-A.1-216), and a shared area's stored backing has none "
                + $"— {where}";
            return RedefinesTier.Rejected;
        }
        // ⛔ THE SAME CARRIER QUESTION FOR A DYNAMIC-LENGTH LEAF (kb/Work PB1026): its content rides the cell's
        // dynamic slots (Place.DynSlotWindow), which a stored backing does not have. Unreachable through a REDEFINES
        // clause on conforming source — §13.18.44.3 SR17: "Neither data-name-2 nor the subject of the entry shall be
        // a variable-length group or a dynamic-length elementary item" — and a file's implicit area never links an
        // out-of-line record (D-FRA). Kept as the loud guard: never a zero-width alias of a dynamic item.
        if (!cls.IsCellBacked && leaves.FirstOrDefault(DynSlotWindow.CarriedBySlot) is { } dynLeaf)
        {
            reject = $"'{dynLeaf.CobolName ?? "FILLER"}' is a dynamic-length item, whose content rides a storage "
                + "cell's dynamic slots (ISO §8.5.1.10.3), and a shared area's stored backing has none "
                + $"— {where}";
            return RedefinesTier.Rejected;
        }

        // Tier A — every member is an elementary item sharing the canonical's CLR storage type AND its image width:
        // one stored field, the rest pass-throughs (a numeric view reinterprets the shared value via its own scale).
        // ⛔ AND THE SAME STORAGE UNIT (kb/Work PB203). CLR type + image width is not enough to prove ONE FIELD
        // SERVES BOTH: a `USAGE BIT` leaf's stored carrier is its PICTURE 1(n) boolean positions while its
        // ImageWidth is the ceil(n/8) BYTES it occupies (§13.18.60.4 GR5 — "bits shall be used"; D19/PB43), so
        // `01 A PIC X(1). 01 B REDEFINES A PIC 1(8) USAGE BIT.` matched on both counts (string, width 1) and B
        // aliased A's one-CHARACTER field — measured: `DISPLAY B` printed `A` where the eight boolean positions
        // of 0x41 are `01000001`. Two members share one field only when they agree on the UNIT (both bit or
        // neither) and, being bit, on the boolean-position COUNT — `PIC 1(5)` and `PIC 1(8)` share a byte
        // ceiling and NOT a carrier. A mismatched pair drops to Tier B, whose windows carry the unit explicitly.
        // ⛔ AND THE SAME UNIT FOR NATIONAL TOO (kb/Work PB231 — the PB203 defect re-run one category over, and
        // the reason this arm is written as an agreement per UNIT rather than a bit special case). A national
        // leaf's stored carrier is its PICTURE N(n) character positions while it OCCUPIES 2n bytes
        // (§13.18.60.4 GR8 / D-N1), so `01 A PIC X(4). 01 B REDEFINES A PIC N(4).` matched on CLR type (string)
        // and on ImageWidth (4) and B would have aliased A's four-CHARACTER field — reading alphanumeric
        // characters as national ones, the exact shape `01 A PIC X(1). 01 B REDEFINES A PIC 1(8) USAGE BIT.`
        // took. Two members share one field only when they agree on the UNIT (all three of bit, national and
        // byte are units here) and, sharing a non-byte unit, on the POSITION COUNT. Everything else drops to
        // Tier B, whose windows carry the unit explicitly. The extent test is ByteWidth for the same reason the
        // class width is.
        DataItem canon = cls.Canonical;
        static bool IsNatLeaf(DataItem d) => NationalWindow.PositionsOf(d) is not null;
        bool allAlias = canon.IsElementary && cls.Members.All(m =>
            m.IsElementary && m.ElementType == canon.ElementType && m.ByteWidth == canon.ByteWidth
            && BitLayout.IsBitLeaf(m) == BitLayout.IsBitLeaf(canon)
            && (!BitLayout.IsBitLeaf(m) || m.Pic!.Length == canon.Pic!.Length)
            && IsNatLeaf(m) == IsNatLeaf(canon)
            && (!IsNatLeaf(m) || NationalWindow.PositionsOf(m) == NationalWindow.PositionsOf(canon)));
        if (allAlias) return RedefinesTier.Alias;

        // Tier B — DISPLAY-homogeneous: one string canonical of class-max width, each view an (offset,width) accessor.
        return RedefinesTier.StringCanonical;
    }
}
