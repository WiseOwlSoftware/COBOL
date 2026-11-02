using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding.Passes;

/// <summary>
/// The OdoResolve pass: row 18 of the per-unit resolve pipeline (StrongTypeChecked to OccursResolved).
/// </summary>
internal sealed class OdoResolvePass : IBindPass
{
    /// <inheritdoc/>
    public string Name => "OdoResolve";

    /// <inheritdoc/>
    public PassPhase Requires => PassPhase.StrongTypeChecked;

    /// <inheritdoc/>
    public PassPhase Produces => PassPhase.OccursResolved;

    /// <inheritdoc/>
    public PassViews Views => PassViews.None;

    /// <summary>
    /// Post-build OCCURS DEPENDING ON pass (runs once the whole forest and the redefines classes exist): resolve
    /// each Format-2 table's data-name-1 and enforce the structural syntax rules the GR8 character-prefix model
    /// relies on, each with its ISO citation — SR16 bounds, SR17 integer data-name-1, SR2 unsubscripted
    /// data-name-1, SR1(b)/SR10 no "complex ODO" (an occurs-depending table never nests under another OCCURS, at
    /// EVERY edition — the legacy comment claiming 2002+ legality was wrong), §13.18.44 SR no ODO inside an
    /// explicit REDEFINES, SR22 trailing table, SR20 data-name-1 placement. Violations are bind-time rejections
    /// (<c>Edition.Error</c> fails the compile) — never a silently mis-sized table (SSOT §1.4).
    /// </summary>
    public void Run(DataBinder binder)
    {
        // §13.18.38.3 SR10 / §8.4.2.3.3 SR3 (kb/Work PB1260): "as long as the number of subscripts required does not
        // exceed seven" — a table nested under seven others needs eight. Reported ONCE, at the first entry that
        // exceeds it (an eighth dimension), not again for everything beneath it.
        // ⛔ THE DEPTH IS A PROPERTY OF THE COMPOSED ENTRY (kb/Work PB1629; the CompositionForest's contract): a TYPE or
        // SAME AS subject placed under seven tables makes the copied table an eighth dimension though its declaration
        // holds one. A copy whose IMMEDIATE source already reaches eight was reported there (or, when the source is itself
        // a copy, one level further back: the induction ends at the written entry), so it is not reported again; a copy
        // that reaches eight only by where it is PLACED is reported at the entry that placed it. The immediate source, not
        // the written definition: a copy of a copy whose eighth dimension arose one level up was reported there.
        foreach (var table in binder.CompositionForest())
        {
            // The ONE arity walk (DataItem.SubscriptArity — SubscriptAdmissionDriftTests pins that it is written nowhere else).
            if (!table.IsTable || table.SubscriptArity != 8) continue;
            if (table.ClonedFrom is { SubscriptArity: 8 }) continue;
            using var _ = binder.Edition.At(table.ReferenceSite);
            binder.Edition.Error(DiagnosticCatalog.TableNestingTooDeep,
                $"table '{table.CobolName ?? table.ClrName}' is nested under seven other OCCURS entries: a reference to "
                + "it needs eight subscripts, and \"as long as the number of subscripts required does not exceed "
                + "seven\" (ISO §13.18.38.3 SR10) — at most seven subscripts may be specified (§8.4.2.3.3 SR3)"
                + DataBinder.ViaReference(table));
        }

        foreach (var item in binder.ClauseResolutionForest())
        {
            if (item.OccursSpec is not { DependingName: { } depName } spec) continue;
            using var _ = binder.Edition.At(item);
            string subject = item.CobolName ?? item.ClrName;
            // ⛔ A TYPE DECLARATION IS SCREENED, NOT PLACED (kb/Work PB2548). Its OCCURS DEPENDING ON phrase is source (§4.3),
            // so every rule that is a fact of the declaration alone — SR16's bounds, the counter's own SR17/SR2 when it
            // resolves — is asked of it; a use site is asked again through its clone. But §13.18.57.4 GR1 makes a TYPE
            // clause "as though the data description … had been coded in place", so data-name-1 is looked up where the
            // description is PLACED (§8.4.2.2.1 rule 5: the groups superordinate to the subject): a name the declaration
            // and the program do not define may be a sibling of the use site, and an unused library type whose counter
            // each user supplies is legal source. A name the declaration does not define is therefore NOT an error here — the clone
            // that places it reports it (COBOL0851) — and the rules that read the PLACEMENT (SR18/SR21's GLOBAL and
            // EXTERNAL attributes of the record, SR20's range across the records of one storage area) are the clone's to
            // ask; SR20 and SR22 over the declaration's own entries are asked here.
            bool declaration = DataBinder.InTypeDeclaration(item);

            // SR16: 0 ≤ integer-1 < integer-2.
            if (spec.Min < 0 || spec.Min >= spec.Max)
                binder.Edition.Error("COBOL0850", $"OCCURS {spec.Min} TO {spec.Max} on '{subject}': integer-1 shall "
                    + "be greater than or equal to zero and less than integer-2 (ISO §13.18.38.3 SR16)");

            // data-name-1 resolution. A counter under a group the table is ALSO subordinate to is found first:
            // §8.4.2.2.1 rule 5 makes the groups superordinate to both the data-name and the subject IMPLICIT
            // qualifiers of a data-name referenced in a data description entry clause. That is also what binds a
            // TYPEDEF clone's internal DEPENDING to the clone's own sibling (review DEVLOG 664 fix #4; §13.18.57.4
            // GR1 — the type is "coded in place"). (This comment used to justify the own-record preference by
            // "§13.18.38.3 SR20, data-name-1 lies within the same record"; SR20 forbids data-name-1 a byte position
            // between the OCCURS entry and the end of its record and places no counter anywhere, kb/Work PB978.)
            // Otherwise the scope-aware set (M2-OO-1h): a method table's data-name-1 resolves in the owning
            // method's scope first (§11.7.4 GR5), then a visible object/program item.
            // A data-name-1 the capture REFUSED (§13.18.38.3 SR2 — subscripted, or not a data-name at all) was
            // reported there; one fault, one verdict (kb/Work PB885).
            if (binder._refusedClauseOperands.Contains(depName)) continue;
            // ⛔ THE SET IS COUNTED (kb/Work PB978) — one survivor, or §8.4.2.2.3 SR1's ambiguity through the ONE
            // verdict; never the first declared, which the unqualified fallback here used to take (`cands[0]`): with
            // CNT declared under two groups `OCCURS 1 TO 9 DEPENDING ON CNT` compiled clean and ran on the first.
            string writtenDep = DataBinder.WrittenQualified(depName, spec.DependingQualifiers);
            string depFace = $"OCCURS DEPENDING ON data-name-1 of '{subject}'";
            var tier = binder.EntryClauseCandidates(item, depName, spec.DependingQualifiers, binder.ScopeOf(item.Root));
            // A declaration's counter is judged here only when the DECLARATION defines it: an item of the same name
            // elsewhere in the program is not where a use site will find it (§8.4.2.2.1 rule 5 qualifies from the
            // placement), so neither its ambiguity nor its SR17 category is a fact of the declaration (train 1055
            // review: an unused template beside two unrelated CNTs drew COBOL1639).
            if (declaration) tier = tier.Where(c => ReferenceEquals(c.Root, item.Root));

            // ⛔ EVERY RULE FROM HERE DOWN IS A FACT OF THE PLACEMENT (kb/Work PB1629): the counter is found from where the
            // description is PLACED (above), and the nesting, REDEFINES and trailing-storage rules ask what surrounds
            // it. A table a TYPE or SAME AS clause copies in is legal in its declaration and wrong at the reference, so
            // these are positioned at the entry that wrote the clause (DataItem.ReferenceSite), never at the
            // declaration. SR16 above is a fact of the written phrase and stays at the entry that holds it.
            var placedAt = item.ReferenceSite;
            using var __ = binder.Edition.At(placedAt);
            string via = DataBinder.ViaReference(item);
            // ⛔ A FACT OF THE DECLARATION IS NOT REPORTED AGAIN AT A LEGAL REFERENCE (kb/Work PB1629 review round 1). The
            // copied description is itself screened — a TYPE declaration above, a SAME AS source as the written record it is
            // — so a fault whose every involved entry lies inside this one copy is reported where it is written; the
            // reference that merely copies it (legal) would otherwise carry the same error a second time. A fault that
            // involves the reference's own entry, or anything outside the copy, is the PLACEMENT's and is reported here.
            bool DeclarationFact(params DataItem[] involved) =>
                item.IsCopiedUnder(placedAt) && Array.TrueForAll(involved, i => i.IsCopiedUnder(placedAt));
            // A name the report section declares is not undefined (kb/Work PB1046): §13.8.6.2.3 bars this clause, of a
            // section other than the report section, from referring to a report item. Asked for a reference that reaches no
            // data item or several (a declaration's is asked by each clone, below), before the ambiguity, which it leaves
            // standing unless a report entry of a NEARER source element hides the data items (kb/Work PB3767's order, kept by the lookup itself since PB4196,
            // ISO §8.4.6.2.1 3) a)).
            if (!declaration && tier.Count != 1
                && binder.ReportEntryReferenceRefused(tier, $"OCCURS DEPENDING ON '{writtenDep}' on '{subject}'", depName, spec.DependingQualifiers))
                continue;
            if (tier.Count == 0)
            {
                if (declaration) continue;   // placed, and so checked, by each clone (above)
                // Nor is a name declared as something other than a data item (kb/Work PB1906): the one query of what it is.
                // A refused declaration was reported at its entry.
                var nonData = binder.NonDataDeclarationOf(depName, spec.DependingQualifiers, binder.ScopeOf(item.Root));
                if (nonData is { DeclarationRefused: true }) continue;
                binder.Edition.Error("COBOL0851", $"OCCURS DEPENDING ON '{writtenDep}' on '{subject}': data-name-1 "
                    + (nonData is { } declared
                        ? $"'{writtenDep}' {declared.DeclaredAs(writtenDep, depName)} (ISO §8.4.2.1: \"a statement "
                          + "shall contain a reference that uniquely identifies that resource\")"
                        : spec.DependingQualifiers.Count > 0
                        ? "is not defined under the given qualifiers (ISO §8.4.2.2.1: uniqueness shall be established "
                          + "through qualification)"
                        : "is not defined (ISO §13.18.38 Format 2)") + via);
                continue;
            }
            // The ambiguity (§8.4.2.2.3 SR1) is a fact of the declaration too when every candidate lies inside the copy that
            // holds this table: the declaration reports it, and the legal reference asks the same verdict silently (so
            // --permissive's first-declared reading still binds the clone's counter) instead of reporting it again.
            bool ambiguityIsDeclarationFact = tier.Count > 1 && item.IsCopiedUnder(placedAt)
                && !tier.Any(c => !c.IsCopiedUnder(placedAt));
            if (binder.UniqueOrReportAmbiguous(tier, depFace, writtenDep, out bool _, report: !ambiguityIsDeclarationFact)
                is not { } dep) continue;

            // ⛔ THE LINK IS RECORDED ONLY WHEN data-name-1 IS WHAT §13.18.38.3 REQUIRES OF IT (kb/Work PB3510): an
            // integer (SR17), not subscripted (SR2), and outside the range from the table's first byte to its record's
            // last (SR20). Every OccursSpec.Depending consumer — the GR8 group-operand slice
            // (ReferenceResolver.WrapIfOdoGroup), the extent freeze (SendingValueTemp), the GR7 counts — resolves
            // data-name-1's own place, and that resolution wraps a GROUP holding an occurs-depending table again. A
            // group data-name-1 that contains its table (DEPENDING ON the record that holds it) therefore resolved
            // itself without end: the binder died of a stack overflow before these diagnostics reached the user. An
            // integer is elementary, so a recorded link never leads back into a group, and the refused link leaves
            // the table where an undefined data-name-1 leaves it (COBOL0851): Depending unset, the compile failed.
            // The predicates are facts of the entries, so a TYPE declaration and its clone refuse the same link even
            // where only one of them REPORTS the fault (DeclarationFact).
            var admission = DependingAdmission.Of(item, dep);
            var (notInteger, subscripted, inTableRange) = admission;
            if (admission.Admitted) spec.Depending = dep;

            // SR17: data-name-1 shall describe an integer — §5.5 2) b) 2., so a trailing-P PIC 99P is one (an index
            // item is NOT an integer data item).
            if (notInteger && !DeclarationFact(dep))
                binder.Edition.Error("COBOL0852", $"OCCURS DEPENDING ON '{depName}' on '{subject}': data-name-1 "
                    + "shall describe an integer (ISO §13.18.38.3 SR17)" + via);

            // SR18 / SR21 (kb/Work PB1261): data-name-1 must share the table record's SCOPE and RESIDENCE attributes,
            // or the table's current extent would depend on a counter that a program sharing the record cannot see
            // (GLOBAL) or that is not shared with it (EXTERNAL — two run-unit programs would disagree about the
            // record's length). "Described in the same data division" holds by construction: this pass runs inside
            // Bind, before a contained program inherits any container's names, so data-name-1 resolved among this
            // element's own items.
            if (!declaration && DependingAttributeFault(binder, item, dep) is { } attributeFault)
                binder.Edition.Error(DiagnosticCatalog.OccursDependingAttributeMismatch,
                    $"OCCURS DEPENDING ON '{writtenDep}' on '{subject}': {attributeFault}{via}");

            // SR2: data-name-1 shall not be subscripted (it cannot lie within any table, nor BE one: a table's own
            // entry is referenced with a subscript too, kb/Work PB3510). A TABLE is any OCCURS — a Format-4 DYNAMIC one
            // leaves Occurs null (kb/Work PB1260) — and "is or lies within a table" is the ONE arity answer,
            // DataItem.SubscriptArity (a walk written nowhere else: SubscriptAdmissionDriftTests); the fault involves
            // data-name-1 and the innermost table that makes it subscripted.
            if (subscripted && !DeclarationFact(dep, dep.SubscriptLevels()[^1]))
                binder.Edition.Error("COBOL0853", $"OCCURS DEPENDING ON '{depName}' on '{subject}': "
                    + "data-name-1 shall not be subscripted (ISO §13.18.38.3 SR2)" + via);

            // SR1(b)/SR10: "complex ODO" is illegal at every edition — tables may be nested only when the
            // DEPENDING phrase is absent, and no OCCURS subject may have an occurs-depending table beneath it. The
            // nearest enclosing table (DYNAMIC included — `Occurs is not null` let an ODO table nest under OCCURS
            // DYNAMIC, kb/Work PB1260) is the last of the parent's subscript levels.
            if (item.Parent is { SubscriptArity: > 0 } enclosing && !DeclarationFact(enclosing.SubscriptLevels()[^1]))
                binder.Edition.Error("COBOL0854", $"occurs-depending table '{subject}' is subordinate to the "
                    + $"OCCURS item '{enclosing.SubscriptLevels()[^1].CobolName}': tables may be nested only when the "
                    + "DEPENDING phrase is "
                    + "absent (ISO §13.18.38.3 SR1(b)/SR10)" + via);

            // §13.18.44 SR: neither the redefined item nor a redefinition may include an occurs-depending table.
            // (The FD multi-record shared AREA is §9.1.2 record sharing — synthesized with no written REDEFINES
            // clause — and is exempt: only an explicitly-written REDEFINES anywhere in the class trips this.)
            for (DataItem? a = item; a is not null; a = a.Parent)
                if (InRedefinesArea(a))
                {
                    // The declaration reports it only when ITS copy of this entry trips the same test: a SAME AS source
                    // is a written record whose REDEFINES classes exist, a TYPE declaration's members have none, so the
                    // reference is the one place the rule is asked of a copied TYPE description. The climb goes on past a
                    // skipped one, for a REDEFINES area above the copy is the placement's.
                    if (item.IsCopiedUnder(placedAt) && a.IsCopiedUnder(placedAt) && a.ClonedFrom is { } source
                        && InRedefinesArea(source))
                        continue;
                    binder.Edition.Error("COBOL0855", $"occurs-depending table '{subject}' lies within a REDEFINES "
                        + "area: neither the original nor a redefinition may include an OCCURS DEPENDING ON "
                        + "table (ISO §13.18.44.3 SR5)" + via);
                    break;
                }

            // SR22: within its record the subject may be followed only by entries subordinate to it — the
            // variable tail is the record's TRAILING storage (the GR8 character-prefix model relies on this).
            // A later sibling that itself REDEFINES an earlier one adds no storage and does not violate SR22.
            for (DataItem? n = item; n is { Parent: { } parent }; n = parent)
            {
                if (parent.Children.SkipWhile(s => !ReferenceEquals(s, n)).Skip(1).Any(s => s.RedefinesTargetName is null))
                {
                    // Followed inside the copy (the table and its follower are both copied members): the declaration's
                    // fact, reported there. The climb goes on, for the entries above the copy are the placement's.
                    if (DeclarationFact(n)) continue;
                    binder.Edition.Error("COBOL0856", $"occurs-depending table '{subject}' is followed by a "
                        + "non-subordinate entry in its record: the subject of an OCCURS DEPENDING ON entry may "
                        + "be followed, within that record, only by data items subordinate to it "
                        + "(ISO §13.18.38.3 SR22)" + via);
                    break;
                }
            }

            // SR20: "The data item defined by data-name-1 shall not occupy a byte position within the range of the
            // first byte position defined by the data description entry containing the OCCURS clause and the last
            // byte position defined by the record description entry containing that OCCURS clause."
            if (inTableRange && !DeclarationFact(dep))
                binder.Edition.Error("COBOL0857", $"OCCURS DEPENDING ON '{depName}' on '{subject}': "
                    + "data-name-1 shall not occupy a byte position within the range from the table's first byte "
                    + "position to the last byte position of its record (ISO §13.18.38.3 SR20)"
                    + (ReferenceEquals(dep.Root, item.Root) ? ""
                        : $" — '{dep.Root.CobolName}' and '{item.Root.CobolName}' are one storage area") + via);
        }
    }

    /// <summary>Is <paramref name="a"/> a REDEFINES entry, or a member of a REDEFINES class that holds one — the entries
    /// §13.18.44.3 SR5 forbids to include an occurs-depending table. The FD multi-record shared area (§9.1.2) is a class
    /// with no written REDEFINES, so it is not one.</summary>
    private static bool InRedefinesArea(DataItem a) =>
        a.RedefinesTargetName is not null || (a.Class is { } cls && cls.Members.Any(mm => mm.RedefinesTargetName is not null));

    /// <summary>The §13.18.38.3 SR18 / SR21 screen of data-name-1's ATTRIBUTES against the table's record (kb/Work
    /// PB1261); null when they agree.
    /// <para>SR18 — "If the OCCURS clause is specified in an entry subordinate to one containing the GLOBAL clause,
    /// data-name-1, if specified, shall be a global name and shall reference a data item that is described in the
    /// same data division". The entry containing the OCCURS clause is never level 1 (SR1 a)), so it is subordinate to
    /// its record, and to the FD of a file-section record: §13.18.27.3 SR1 admits GLOBAL on both.
    /// <see cref="DataBinder.IsGlobalRecord"/> is "a global name" for data-name-1 (§8.4.6.2.2).</para>
    /// <para>SR21 — "If the OCCURS clause is specified in a data description entry included in a record description
    /// entry containing the EXTERNAL clause, data-name-1 shall reference a data item possessing the external
    /// attribute that is described in the same data division". The TABLE's side is the record entry's own clause
    /// (or the EXTERNAL type it is "subject to the same rules" as, §13.18.22.4 GR3); data-name-1's is the
    /// <see cref="HasExternalAttribute"/> attribute.</para></summary>
    private static string? DependingAttributeFault(DataBinder binder, DataItem table, DataItem dep)
    {
        if (binder.IsGlobalRecord(table.Root) && !binder.IsGlobalRecord(dep.Root))
            return $"the table's record '{table.Root.CobolName}' is global and data-name-1 is not a global name — \"If "
                + "the OCCURS clause is specified in an entry subordinate to one containing the GLOBAL clause, "
                + "data-name-1, if specified, shall be a global name\" (ISO §13.18.38.3 SR18)";
        if ((table.Root.HasExternalClause || table.Root.ExternalFromType) && !HasExternalAttribute(binder, dep.Root))
            return $"the table's record '{table.Root.CobolName}' is EXTERNAL and data-name-1 does not possess the "
                + "external attribute — \"data-name-1 shall reference a data item possessing the external attribute "
                + "that is described in the same data division\" (ISO §13.18.38.3 SR21)";
        return null;
    }

    /// <summary>Does <paramref name="record"/> possess the external attribute? ISO §8.6.3: a working-storage record
    /// "is given the external attribute by the presence of the EXTERNAL clause in its data description entry" (or
    /// by an EXTERNAL type, §13.18.22.4 GR3), and "If the EXTERNAL clause is included in the file description entry,
    /// its records and their subordinate data items attain the external attribute".</summary>
    private static bool HasExternalAttribute(DataBinder binder, DataItem record) =>
        record.HasExternalClause || record.ExternalFromType || binder.Files.Any(f => f.IsExternal && f.Records.Contains(record));
}
