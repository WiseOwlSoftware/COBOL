// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Editions.Diagnostics;

namespace WiseOwl.Cobol.Binding.Passes;
using WiseOwl.Cobol.Compiler.Oo;

/// <summary>
/// The DynamicResolve pass: row 19 of the per-unit resolve pipeline (OccursResolved to OccursResolved).
/// </summary>
internal sealed class DynamicResolvePass : IBindPass
{
    /// <inheritdoc/>
    public string Name => "DynamicResolve";

    /// <inheritdoc/>
    public PassPhase Requires => PassPhase.OccursResolved;

    /// <inheritdoc/>
    public PassPhase Produces => PassPhase.OccursResolved;

    /// <inheritdoc/>
    public PassViews Views => PassViews.None;

    /// <summary>
    /// Post-build OCCURS DYNAMIC pass (ISO §13.18.38 Format 4 / §8.5.1.9; data-model D9): for each dynamic-capacity
    /// table carrying a <c>CAPACITY IN data-name-3</c> phrase, synthesize the IMPLICITLY-defined CAPACITY register
    /// (SR30) — a VIEW over the table's current capacity, an unsigned integer (SR31) — and index it by name so the
    /// <see cref="ReferenceResolver"/> can build a <see cref="CapacityRegisterPlace"/>. The register is NOT a stored
    /// field (no <c>FieldEmitter</c> entry): its value IS the runtime <c>CobolDynTable&lt;T&gt;.Capacity</c>. A
    /// register-name that duplicates an explicit data-name (or another table's register) violates the
    /// implicit-definition rule → COBOL1523. Placement/declaration guards: SR28 FROM ≤ TO (1522); the FILE
    /// SECTION prohibition §8.5.1.9.1 (1526).
    /// <para>⛔ A FORMAT 1 VALUE ON OR UNDER A DYNAMIC ENTRY IS NOT THIS PASS'S BUSINESS — see the note below the
    /// SR28 guard. It carries no capacity derivation to stage, so there is nothing here to guard (kb/Work
    /// PB500).</para>
    /// </summary>
    public void Run(DataBinder binder)
    {
        // §8.5.1.9.1 item 3 (:8195) — the roots of every FILE SECTION record, so a dynamic table in one is rejected.
        var fileRecordRoots = new HashSet<DataItem>(binder.State.Files.Declared.SelectMany(f => f.Records));

        foreach (var item in binder.ClauseResolutionForest())
        {
            if (item.OccursSpec is not { IsDynamic: true } spec) continue;
            using var _ = binder.Edition.At(item);
            string subject = item.CobolName ?? item.ClrName;

            // §8.5.1.9.1 item 3 (:8195) — a dynamic-capacity table "may be defined in any place, OTHER THAN the file
            // section" (its out-of-line CobolDynTable has no place in a record image). Reject a dynamic table whose
            // storage root is an FD/SD record.
            // ⛔ A fault of PLACEMENT is positioned at the entry that placed it (kb/Work PB1629): a table that reaches the
            // record through a TYPE or SAME AS clause is legal in the declaration it is copied from, so the error is
            // at the clause's subject (DataItem.ReferenceSite) — the FD record or the group inside it — never at the
            // working-storage declaration, whose line the user would be told to change.
            DataItem root = item; while (root.Parent is { } p) root = p;
            if (fileRecordRoots.Contains(root))
            {
                using var __ = binder.Edition.At(item.ReferenceSite);
                binder.Edition.Error(DiagnosticCatalog.DynamicCapacityTableInFileSection, $"OCCURS DYNAMIC on '{subject}': a dynamic-capacity table shall not be "
                    + "defined in the FILE SECTION (ISO §8.5.1.9.1)" + DataBinder.ViaReference(item));
            }

            // SR28 (:19987): integer-4 (FROM) shall be nonnegative and integer-5 (TO), if both present, shall be
            // GREATER THAN integer-4. (FROM<0 cannot be written — the grammar takes an unsigned integerLiteral.)
            if (spec.InitialCap is { } from && spec.ExpectedMax is { } to && to <= from)
                binder.Edition.Error(DiagnosticCatalog.DynamicCapacityBoundsInvalid, $"OCCURS DYNAMIC FROM {from} TO {to} on '{subject}': the expected "
                    + $"capacity (TO integer-5) shall be greater than the minimum capacity (FROM integer-4) "
                    + "(ISO §13.18.38.3 SR28)");

            // ⛔ A TYPE DECLARATION DEFINES NO REGISTER (kb/Work PB2548): SR28 above is a fact of the written phrase and is
            // asked of the declaration too (§4.3), but the CAPACITY register is "implicitly defined" by an OCCURS entry that
            // IS placed (SR30), and a type declaration has no storage (§13.18.58.4 GR2) — each clone mints its own.
            if (DataBinder.InTypeDeclaration(item)) continue;

            // ⛔ NO GUARD BELONGS HERE FOR A **FORMAT 1** VALUE ON OR UNDER A DYNAMIC ENTRY, AND THE RULE THAT
            //    LOOKS LIKE ONE DOES NOT REACH IT (kb/Work PB500 — this is the SECOND arm of the same two-arm
            //    dispatch the fixed-capacity lane already got right).
            //
            //    A COBOL1528 refusal stood here, citing §13.18.63.4 GR16 ("If an OCCURS clause with the DYNAMIC
            //    phrase is specified in the same entry as the VALUE clause, or in any entry superordinate to it,
            //    the initial capacity of the associated dynamic-capacity table is calculated according to the
            //    following subrules") and reading its subrule (b) ("If no TO phrase is specified in the VALUE
            //    clause, the initial capacity is set equal to the expected capacity specified in the OCCURS
            //    clause") as reaching a Format 1 `VALUE IS literal-1`, which trivially has no TO phrase. IT DOES
            //    NOT. GR16 sits under the **FORMAT 2** general-rule heading (GR11–GR16), and §13.18.63 states
            //    cross-band application EXPLICITLY and in ONE direction only — GR11 "General rules 1, 2, 3, 4, 5,
            //    6, 7, 8, and 10 above apply", GR17, GR21, GR24 all import FORMAT 1 rules INTO another band, and
            //    nothing imports GR12–GR16 into FORMAT 1. The syntax rules settle it independently: §13.18.63.3
            //    SR22 ("A VALUE clause without the TO phrase shall not be specified in the same entry as an OCCURS
            //    clause with a DYNAMIC phrase but no TO phrase, or in any entry subordinate to such an OCCURS
            //    clause") is the rule that keeps GR16b from having no operand, and IT TOO is in the FORMAT 2 band
            //    (SR16–SR23) with no FORMAT 1 counterpart. Were GR16 to reach Format 1, `05 A PIC X OCCURS DYNAMIC
            //    FROM 2 VALUE "Z".` would hit GR16b with no expected capacity to set — a hole the standard would
            //    have had to close and did not, because there is no hole.
            //
            //    So a Format 1 VALUE here is CONFORMING SOURCE with a fully determined meaning, and refusing it
            //    was rejecting legal COBOL (§13.18.38.3 has no rule forbidding VALUE on a Format 4 entry):
            //      • CAPACITY — §14.6.2.3.2 item 6, "For each dynamic-capacity table, except where the table is
            //        defined by an elementary entry with a VALUE clause, the capacity of the table is set to the
            //        minimum capacity specified in the corresponding OCCURS clause", with §13.18.38.4 GR16
            //        "Integer-4 is the minimum capacity of the table. If integer-4 is absent, a value of zero is
            //        assumed for it." The item-6 EXCEPTION is the carve-out that lets a GR16-derived (Format 2)
            //        capacity survive this step, and it changes NOTHING for a Format 1 VALUE either way: §8.5.1.9.1
            //        gives the same number from the other side — "The current capacity of a dynamic-capacity table
            //        may be initialized explicitly in the FROM phrase of the OCCURS clause or implicitly in the
            //        VALUE clause. If neither is specified, the current capacity is initialized to zero" — and
            //        GR16 is the only "implicitly in the VALUE clause" mechanism there is. FROM (or zero) both ways.
            //      • CONTENT — §13.18.63.4 GR9, "A VALUE clause specified in a data description entry that contains
            //        an OCCURS clause or in an entry that is subordinate to an OCCURS clause causes every occurrence
            //        of the associated data item to be assigned the specified value", reinforced by §13.18.38.4 GR1
            //        (band "FORMATS 1, 2 AND 4" — Format 4 IS the dynamic-capacity table): "Except for the OCCURS
            //        clause itself, all data description clauses associated with an item whose description includes
            //        an OCCURS clause apply to each occurrence of the item described."
            //
            //    Both are already what the emitter does with no code of its own: ValueInitializer.FieldInit opens a
            //    CobolDynTable at `OccursSpec.InitialCap ?? 0` and seeds EVERY occurrence from the one-occurrence
            //    initializer, which for a Format 1 VALUE is DataItem.ValueAt → RawValue. The refusal's two arms
            //    disagreed with each other, too, which is what gave the defect away: the GROUP arm fired only when
            //    the OCCURS carried a TO, so `OCCURS DYNAMIC FROM 3.` with subordinate VALUEs compiled and seeded
            //    correctly while `OCCURS DYNAMIC FROM 3 TO 9.` — the SAME construct, one optional phrase apart —
            //    was refused, and the ELEMENTARY arm ignored the TO entirely and refused both.

            // The register: an unsigned-integer VIEW over the table's Capacity (SR31) — a native-binary PicInfo so
            // the numeric pipeline reads {tablePath}.Capacity (a long) as a scale-0 integer; no stored field. The
            // implementor digit count is 10 (unsigned BinaryLong): the CobolDynTable implementor maximum,
            // 0x3FFF_FFFF ≈ 1.07e9, fits in 10 digits (§8.5.1.9.1 — "a number of digits sufficient to hold the
            // maximum"). Kept off ByName/Roots — reachable ONLY through a register-name map, which only
            // SymbolTable.CapacityCandidates reads.
            // ⛔ MINTED FOR EVERY DYNAMIC TABLE, NAMED ONLY WHEN `CAPACITY IN data-name-3` IS WRITTEN (kb/Work
            // PB61): the view over the table's current capacity is what FUNCTION LENGTH / BYTE-LENGTH read for a
            // variable-length group (§15.50.4 r7c / §15.14.4 r6c — "based on their current capacity"), whether or
            // not the program gave the register a name. An unnamed register has no CobolName and no
            // CapacityRegisters entry, so no COBOL reference can reach it.
            // ⛔ IT CARRIES A REAL POSITION IN THE HIERARCHY (kb/Work PB457): §13.18.38.3 SR30 — "If qualifiers are
            // required for uniqueness, it shall be treated as though implicitly defined at the same level as the
            // entry containing the OCCURS clause" — so its Parent is the OCCURS entry's Parent, making it a SIBLING
            // of the table. That one assignment is what lets the ONE §8.4.2.2 qualifier matcher
            // (DataBinder.QualifierChainMatches) answer `WS-CAP OF WS-TABLE`, and what makes the register of a table
            // NESTED under another table answer SubscriptArity > 0 — the §8.4.2.3.3 SR3/SR5-vs-§13.18.38.3 SR31 conflict that
            // ReferenceResolver.CapacityRegisterFor reports, instead of the flat name-dictionary's false "not
            // defined". It is NOT added to Parent.Children: the register has no storage and takes no record slot.
            var reg = new DataItem
            {
                Level = 49,
                ClrName = NamingConvention.CapacityRegisterName(item.ClrName),
                CobolName = spec.CapacityName,
                Pic = PicInfo.BinaryItem(Usage.BinaryLong, signed: false),
                Parent = item.Parent,
                Uid = binder.TakeUid(),
            };
            spec.CapacityRegister = reg;

            if (spec.CapacityName is not { } capName) continue;

            // SR30 — data-name-3 is implicitly defined at the OCCURS entry, so it must not also be an explicit
            // data-name (a duplicate definition) nor the CAPACITY register of another dynamic table.
            // ⛔ "ELSEWHERE IN THE SOURCE ELEMENT" IS THIS ELEMENT'S OWN NAMES (kb/Work PB3766 review R2-1): a contained
            // program inherits its containers' GLOBAL data-names and CAPACITY registers before this pass runs
            // (InheritContainerGlobalData), and those are defined in ANOTHER source element — this element's own
            // declaration of the spelling hides them (§8.4.6.2.1 3) — so only the depth-0 names and registers are the
            // duplicates SR30 forbids (DeclaresOwnDataName; DeclaringDepth).
            // ⛔ "DEFINED ELSEWHERE" COMPARES DEFINITIONS, NOT EXPANSIONS (kb/Work PB2988): data-name-3 written once in a
            // type declaration (or under a SAME AS operand) is reproduced by every TYPE / SAME AS copy of its entry,
            // exactly as the table's own data-name is, and SR30's second sentence — "If qualifiers are required for
            // uniqueness, it shall be treated as though implicitly defined at the same level as the entry containing
            // the OCCURS clause" — is how those copies are told apart (`DC OF A`, `DC OF B`). So a register whose
            // table is a copy of the same written entry as every table already listed under the name joins them; only
            // a register of a DIFFERENT written entry, or an explicit data-name, is the duplicate SR30 forbids.
            // ⛔ "THE SOURCE ELEMENT" OF A METHOD'S TABLE IS THE METHOD (kb/Work PB3165): a method definition is a source
            // element of its own (§10.5, the census of kb/Work PB2921), so its register is judged against ITS data-names
            // and registers only — never against the object's, which §11.7.4 GR5 makes inaccessible to the method when a
            // word is declared in both, and never against a sibling method's. The name lands in the same scope.
            var methodScope = binder.ScopeOf(item.Root).Method;
            var registers = methodScope?.CapacityRegisters ?? binder.State.Forest.CapacityRegistersSoFar;
            if ((methodScope is null ? binder.State.Forest.DeclaresOwnDataName(capName) : methodScope.ByName.ContainsKey(capName))
                || (registers.TryGetValue(capName, out var sameName)
                    && !sameName.TrueForAll(t => binder.State.Forest.DeclaringDepth(t) != 0
                                                 || ReferenceEquals(t.WrittenDefinition, item.WrittenDefinition))))
            {
                binder.Edition.Error(DiagnosticCatalog.CapacityRegisterNameDuplicated, $"CAPACITY IN '{capName}' on '{subject}': data-name-3 is implicitly "
                    + "defined by the OCCURS DYNAMIC entry and shall not duplicate another data-name or "
                    + "CAPACITY register (ISO §13.18.38.3 SR30)");
                continue;
            }
            // "Defined elsewhere" is any user-defined word of ANOTHER type too (§8.3.2.2: a given user-defined word "may be
            // used as only one type of user-defined word"): the register IS a data-name (SR30 defines it as one), so it
            // is announced through the ONE declaration funnel (kb/Work PB1264, PB1083), which holds it to the
            // one-type-per-word rule against a file-name, an index-name, a condition-name, a section- or paragraph-name
            // declared before OR after it — the procedure-division binder declares those later and its declaration is
            // the one refused. This used to be a hand-written file-name scan beside the data-name test.
            // (A method's word lands in the method's own census — the funnel reads it off the declaration scope.)
            if (methodScope is null) binder.DeclareUserWord(capName, UserWordKind.DataName);
            else using (binder.Oo.EnterMethodDeclarations(methodScope)) binder.DeclareUserWord(capName, UserWordKind.DataName);
            binder.State.Forest.AddCapacityRegister(methodScope, capName, item);
        }
    }
}
