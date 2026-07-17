// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;
using CobolNet.Runtime;
using CobolNet.Frontend.Generated;

using CobolNet.Binding.Model;

namespace CobolNet.Binding;

using Core = CobolParserCore;

/// <summary>
/// One PROCEDURE DIVISION USING formal parameter (ISO §14.2.2 SR1 — a level-01/77 LINKAGE item) and its
/// caller-storage carrier (COBOLNET_INTERPROGRAM_DESIGN D1/D2).
/// </summary>
/// <param name="Item">The LINKAGE-section 01/77 item the header USING operand names.</param>
/// <param name="Position">The 0-based positional slot (ISO §14.2.3 GR2 — correspondence is positional, never by name).</param>
/// <param name="CarrierField">The emitted <c>ManagedPointer&lt;T&gt;</c> field name (<c>__lnkpN</c>).</param>
/// <param name="CarrierResident">True when the formal is CARRIER-RESIDENT: an elementary formal whose every
/// reference reads/writes the caller's storage through <c>carrier.Value</c> (the design's "one unavoidable
/// indirection" — per-access aliasing per ISO §14.2.3 GR8). False for a group formal or a REDEFINED elementary
/// formal: those keep a callee-local field and round-trip the caller's character image at the ACTIVATION
/// boundary (copy-in at entry, copy-out at return — the deep-dive group hard problem's whole-struct round trip,
/// realized at the call boundary).</param>
/// <param name="ByValue">True when the header names this formal in a BY VALUE phrase (ISO §14.2.2 using-phrase;
/// §14.2.3 GR4 transitivity resolved at bind). The activated element then operates on a VALUE COPY — a detached
/// cell conformed to the formal's description (§14.2.3 GR10: the argument is the sending operand of a COMPUTE
/// without ROUNDED into the allocated record); the callee's stores never reach the caller's storage.</param>
public sealed record LinkageFormal(DataItem Item, int Position, string CarrierField, bool CarrierResident,
    bool ByValue = false);

/// <summary>The synthesized run-unit backing of one EXTERNAL record (ISO §13.18.22 / §8.6.7): the emitter
/// renders <c>private ref string {BackingCsName} =&gt; ref ExternalStore.Cell({ExternalName}, {InitImage}).Ref;</c>
/// and every reference windows it through the Tier-B view machinery.</summary>
public sealed record CallExternalBacking(string BackingCsName, string ExternalName, int Width, string InitImage);

/// <summary>One FUNCTION-ID unit's activation signature (ISO §9.4 user-defined functions; M2-UDF-1): the
/// registered function name, its PROCEDURE DIVISION RETURNING item (whose description the caller-side result
/// temporary clones — §8.4.3.2.4 GR1), and its positional USING formals (§14.8.2). Built by the run-unit
/// emitter BETWEEN the DATA and PROCEDURE bind phases, so a <c>FUNCTION user-name(args)</c> reference binds
/// against signatures of function units defined ANYWHERE in the compilation group, including after the caller.
/// <paramref name="Returning"/> is null only for the ill-formed no-RETURNING function (COBOLNET1507 — already
/// diagnosed; call sites fail loud without re-reporting).</summary>
public sealed record UserFunctionSignature(string Name, DataItem? Returning, IReadOnlyList<LinkageFormal> Formals);

/// <summary>
/// The LINKAGE SECTION / EXTERNAL / GLOBAL half of the data binder (COBOLNET_INTERPROGRAM_DESIGN D1–D5).
/// LINKAGE 01/77 items bind into the ordinary storage forest (so every verb, REDEFINES tier, level-88, and
/// OCCURS path works unchanged — ISO §13.7: linkage entries are ordinary record descriptions whose STORAGE
/// belongs to the caller); the PROCEDURE DIVISION header's USING/RETURNING operands then designate which roots
/// are formal parameters (§14.2.2 SR1) and how each is carried. EXTERNAL level-01/77 records become Tier-B
/// string windows over a run-unit <c>ExternalStore</c> cell (§8.6.7 — one storage copy per name per run unit);
/// GLOBAL level-01/77 roots are collected for the emitter's containment inheritance (§13.18.27 GR1–2).
/// </summary>
public sealed partial class DataBinder
{
    /// <summary>The LINKAGE SECTION's top-level (01/77) items, in source order (ISO §13.7).</summary>
    public List<DataItem> LinkageRoots { get; } = [];

    /// <summary>True when this binder binds a CONTAINED program unit (ISO §13.18.2.3 SR2/SR3 — the ANY LENGTH
    /// placement rules differ per unit kind). Set by <c>BinderDriver.BindUnitData</c> from the unit's Parent.</summary>
    public bool UnitIsContained { get; init; }

    /// <summary>True when this binder binds a FUNCTION-ID unit (ISO §13.18.2.3 SR2/SR4). Set by
    /// <c>BinderDriver.BindUnitData</c> from <c>BoundUnit.IsFunction</c>.</summary>
    public bool UnitIsFunction { get; init; }

    /// <summary>The PROCEDURE DIVISION USING formals, positional (ISO §14.2.3 GR2). (READ-ONLY view — P6 Step 5.)</summary>
    public IReadOnlyList<LinkageFormal> LinkageFormals => _linkageFormals;
    private readonly List<LinkageFormal> _linkageFormals = [];

    /// <summary>The PROCEDURE DIVISION RETURNING item (a LINKAGE 01/77; ISO §14.2.3 GR6 — its storage is
    /// allocated in the activated element), or null. COBOL-2002+ (the grammar gates the clause).</summary>
    public DataItem? LinkageReturning { get; private set; }

    /// <summary>The C# names of class-level fields the <c>FieldEmitter</c> must NOT declare because another
    /// mechanism provides the member: a carrier-resident LINKAGE formal's "field" IS the carrier accessor
    /// (<c>__lnkpN.Value</c> — the caller owns the storage, ISO §13.7.1 / §14.2.3 GR8), and an inherited
    /// GLOBAL table's index field is a <c>ref</c>-bridge to the containing instance (ISO §13.18.27 GR2 —
    /// global index-names are shared, never duplicated). (READ-ONLY view — P6 Step 5; the inherited-index
    /// suppression writes through <see cref="SeedInheritedGlobalIndex"/>.)</summary>
    public IReadOnlySet<string> CallSuppressedRootFields => _callSuppressedRootFields;
    private readonly HashSet<string> _callSuppressedRootFields = new(StringComparer.Ordinal);

    /// <summary>WORKING-STORAGE level-01/77 roots carrying the GLOBAL clause (ISO §13.18.27) — visible to every
    /// directly/indirectly contained program (GR1–2); the emitter injects them into contained units' binders and
    /// bridges their fields into the nested classes. (READ-ONLY view — P6 Step 5.)</summary>
    public IReadOnlyList<DataItem> CallGlobalRoots => _callGlobalRoots;
    private readonly List<DataItem> _callGlobalRoots = [];

    /// <summary>The EXTERNAL records' synthesized run-unit backings (ISO §13.18.22; emitted as
    /// <c>ref</c>-properties over <c>ExternalStore</c>). (READ-ONLY view — P6 Step 5.)</summary>
    public IReadOnlyList<CallExternalBacking> CallExternalBackings => _callExternalBackings;
    private readonly List<CallExternalBacking> _callExternalBackings = [];

    /// <summary>
    /// Bind the LINKAGE SECTION and the PROCEDURE DIVISION header's USING/RETURNING operands. Runs inside
    /// <c>Bind</c> right after WORKING-STORAGE (so linkage items join the same forest, name index, and
    /// post-build passes — REDEFINES classification, SIGN inheritance, index resolution all apply, ISO §13.7.3).
    /// </summary>
    internal void CallBindLinkage(Core.ProgramUnitContext program, HashSet<string> rootNames)
    {
        if (program.dataDivision()?.linkageSection() is { } ls)
        {
            var entries = ls.linkageEntry()
                .Select(e => e.dataDescriptionEntry())
                .Where(e => e is not null)
                .Select(e => e!);
            LinkageRoots.AddRange(BindEntries(entries, rootNames, EntrySection.Linkage));
        }

        if (program.procedureDivision() is not { } pd) { AnyLengthValidateUnit(); return; }

        // The using-phrase modes (ISO §14.2.2 :23636 — { [BY REFERENCE] {[OPTIONAL] d}… | BY VALUE {d}… }…):
        // BY REFERENCE / BY VALUE are TRANSITIVE across the parameters that follow until the other phrase
        // appears; BY REFERENCE is assumed before the first phrase (§14.2.3 GR4). Threaded flat over the
        // grammar's one-parameter-per-node shape (the CALL callArgument precedent).
        int pos = 0;
        bool byValue = false;
        foreach (var prm in pd.usingClause()?.usingParameter() ?? [])
        {
            Core.DataReferenceContext dref;
            bool optional = false;
            if (prm.usingByValue() is { } vb) { byValue = true; dref = vb.dataReference(); }
            else if (prm.usingByReference() is { } rb)
            {
                byValue = false;
                dref = rb.dataReference();
                optional = rb.OPTIONAL() is not null;
            }
            else { dref = prm.dataReference(); optional = prm.OPTIONAL() is not null; }
            if (optional)
                // Conformant surface (§14.2.2 using-phrase — OPTIONAL may precede a BY REFERENCE formal),
                // staged loud: the OPTIONAL/OMITTED formal model (§14.2.3 GR3, the §8.8.4.8 omitted-argument
                // condition) is not implemented — never a silently-required formal.
                Edition.Error(DiagnosticCatalog.OptionalFormal,
                    $"formal parameter '{dref.GetText()}': the OPTIONAL phrase is recognized but OPTIONAL "
                    + "formal parameters are not yet implemented (ISO §14.2.2 / §14.2.3 GR3)");

            string pname = dref.GetText();
            var item = FindLinkageRoot(pname);
            if (item is null)
            {
                // §14.2.2 SR1: each formal parameter shall be a level-01/77 entry in the LINKAGE SECTION.
                Edition.Error("COBOLNET0888",
                    $"PROCEDURE DIVISION USING parameter '{pname}' is not a level-01/77 LINKAGE SECTION item "
                    + "(ISO §14.2.2 SR1)");
                pos++;
                continue;
            }
            if (item.RedefinesTargetName is not null)
                // §14.2.2 SR1: a formal parameter shall not include a REDEFINES clause.
                Edition.Error("COBOLNET0889",
                    $"formal parameter '{pname}' shall not contain a REDEFINES clause (ISO §14.2.2 SR1)");
            if (item.IsBased)
            {
                // The SAME SR1 sentence (:23658) bans the BASED clause on a formal — without this, a
                // carrier-resident formal's CsName rewrite would poison the based class's BackingCsName
                // into invalid C# (the review finding); the flag clears so the entry binds as an
                // ordinary (already-diagnosed) formal.
                Edition.Error("COBOLNET0889",
                    $"formal parameter '{pname}' shall not contain a BASED clause (ISO §14.2.2 SR1)");
                item.IsBased = false;
            }

            if (byValue)
            {
                // §14.2.2 SR2 (:23664): "Each data-name-1 specified in a BY VALUE phrase shall be defined as a
                // data item of class numeric, message-tag, object, or pointer." Class message-tag is the MCS
                // module (not modeled — undeclarable here, so unreachable). Fixed-point class numeric is the
                // CARRIED leg (the §14.2.3 GR10 detached-cell value copy); the remaining SR2-legal shapes
                // (object/pointer classes, floating-point usage) stage loud by name — never silently by-ref.
                if (!(item.IsElementary && item.Pic?.Category is PicCategory.Numeric or PicCategory.Pointer
                        or PicCategory.ProgramPointer or PicCategory.ObjectReference))
                    Edition.Error("COBOLNET1553",
                        $"BY VALUE formal parameter '{pname}' shall be of class numeric, message-tag, object, "
                        + "or pointer (ISO §14.2.2 SR2)");
                else if (item.Pic is not { Category: PicCategory.Numeric, IsFloat: false })
                    Edition.Error(DiagnosticCatalog.ByValueFormalCarrier,
                        $"BY VALUE formal parameter '{pname}': this class's value-copy carrier "
                        + "(ISO §14.2.3 GR10) is not yet implemented — only a fixed-point numeric BY VALUE "
                        + "formal is carried");
            }

            string carrier = $"__lnkp{pos}";
            // Carrier-resident = per-access aliasing of the caller's storage (design D1: "refs to LK-CTR
            // read/write LK_CTR.Value — the one unavoidable indirection"). Available for an elementary
            // fixed-point/character formal NOT overlaid by another linkage entry; a group formal (a different
            // C# struct type than the caller's — every cross-program group IS a category reinterpretation) and
            // a redefined formal round-trip the character image at the activation boundary instead. A BY VALUE
            // formal is carrier-resident over its DETACHED value-copy cell (§14.2.3 GR10 — same per-access
            // mechanism, different carrier: CobolArgAdapt.NumValue instead of the aliasing Num view).
            bool redefined = LinkageRoots.Any(r => !ReferenceEquals(r, item)
                && r.RedefinesTargetName is { } t
                && string.Equals(t, item.CobolName, StringComparison.OrdinalIgnoreCase));
            bool resident = item.IsElementary && !redefined && item.Pic is { IsFloat: false };
            if (resident)
            {
                // The item's C# "path" becomes the carrier's Value accessor: every Place built over it reads
                // and writes the caller's storage directly (MemberPlace path text — no new Place subtype).
                item.CsName = carrier + ".Value";
                _callSuppressedRootFields.Add(item.CsName);
            }
            _linkageFormals.Add(new LinkageFormal(item, pos, carrier, resident, byValue));
            pos++;
        }

        // PROCEDURE DIVISION RETURNING (§14.2) + RAISING (§14.2.2) — the COBOL-2002 introduction gates are now
        // VersionConformancePass ParseArm.VisitReturning/RaisingClause (14g.4, recognition, InMethodDefinition-guarded
        // so only PROGRAM-unit PDs gate — this CallBindLinkage runs for program units only, matching that scope). The
        // RETURNING §14.2.3 GR6/SR1 resolution + the RAISING EC semantics (StatementBinder.Exceptions) stay bind-time.
        if (pd.returningClause()?.dataReference() is { } rref)
        {
            // §14.2.3 GR6: the returning item's storage is allocated IN THE ACTIVATED element — it stays an
            // ordinary callee-local field; its value transfers out at termination (GR7, the Call copy-out).
            LinkageReturning = FindLinkageRoot(rref.GetText());
            if (LinkageReturning is null)
                Edition.Error("COBOLNET0888",
                    $"PROCEDURE DIVISION RETURNING item '{rref.GetText()}' is not a level-01/77 LINKAGE SECTION "
                    + "item (ISO §14.2.2 SR1)");
        }

        AnyLengthValidateUnit();
    }

    /// <summary>The ANY LENGTH placement sweep of this unit's forest (ISO §13.18.2.3 SR2/SR3/SR4) — runs at the
    /// end of <see cref="CallBindLinkage"/>, when the unit's sections AND its PD-header formals are bound but no
    /// method data has (methods sweep in <c>OoBindMethodData</c>). SR2: an elementary level-1 LINKAGE entry of a
    /// FUNCTION or a CONTAINED program — never an outermost program (the §13.18.2.3 NOTE: a prototype-less CALL
    /// cannot associate arguments with an ANY LENGTH formal) and never the object/factory paragraph of a class.
    /// SR3 (contained program): referenced in the PD header as a formal WITH THE BY REFERENCE PHRASE (stated
    /// or implied — a BY VALUE formal does not qualify, SR3a) or as the RETURNING item. SR4 (function): as a
    /// BY REFERENCE formal only — a function's ANY LENGTH RETURNING is illegal.
    /// A violation clears <see cref="DataItem.IsAnyLength"/> (the IsBased discipline: the item binds as its
    /// ordinary one-character shape under an already-failed compile — never a half-varying state).</summary>
    private void AnyLengthValidateUnit()
    {
        foreach (var root in Roots)
        {
            if (!root.IsAnyLength) continue;
            string where = $"data item '{root.CobolName ?? "FILLER"}'";
            if (!LinkageRoots.Contains(root))
                Edition.Error("COBOLNET1542", $"{where}: the ANY LENGTH clause may be specified only in an "
                    + "elementary level 1 entry in the LINKAGE SECTION (ISO §13.18.2.3 SR2)");
            else if (root.IsGroup)
                Edition.Error("COBOLNET1542", $"{where}: the subject of an ANY LENGTH clause shall be "
                    + "ELEMENTARY — this entry has subordinate items (ISO §13.18.2.3 SR2)");
            else if (OoIsClassUnit)
                // This binder's own LINKAGE is the OBJECT/FACTORY paragraph's, never a method's (method
                // linkage binds via OoBindMethodData into OoMethodBinding.LinkageRoots, swept there).
                Edition.Error("COBOLNET1542", $"{where}: the ANY LENGTH clause may be specified only in the "
                    + "linkage section of a function, a contained program, or a method that is not a property "
                    + "method — not in a factory or instance definition (ISO §13.18.2.3 SR2)");
            else if (!UnitIsContained && !UnitIsFunction)
                Edition.Error("COBOLNET1542", $"{where}: the ANY LENGTH clause may not be specified in an "
                    + "OUTERMOST program — only in a function, a contained program, or a method "
                    + "(ISO §13.18.2.3 SR2 and its NOTE)");
            else
            {
                // SR3a/SR4 require the BY REFERENCE phrase (stated or implied) — a BY VALUE formal never
                // qualifies (its length is fixed by the value copy, not the caller's argument).
                bool formal = LinkageFormals.Any(f => ReferenceEquals(f.Item, root) && !f.ByValue);
                bool returning = ReferenceEquals(LinkageReturning, root);
                if (UnitIsFunction ? !formal : !(formal || returning))
                    Edition.Error("COBOLNET1542", UnitIsFunction
                        ? $"{where}: in a FUNCTION the subject of an ANY LENGTH clause shall be referenced in "
                          + "the procedure division header as a formal parameter with the BY REFERENCE phrase "
                          + "(ISO §13.18.2.3 SR4)"
                        : $"{where}: in a contained program the subject of an ANY LENGTH clause shall be "
                          + "referenced in the procedure division header as a BY REFERENCE formal parameter or "
                          + "as the RETURNING item (ISO §13.18.2.3 SR3)");
                else if (returning && !formal)
                    // SR3b-legal, staged LOUD (never a silently-wrong length): the return crossing delivers a
                    // VALUE — it cannot carry the ACTIVATOR's receiver length that GR1 fixes n from. Deferred
                    // with the ANY-LENGTH-RETURNING wave; the formal-parameter leg is fully implemented.
                    Edition.Error(DiagnosticCatalog.AnyLengthReturning, $"{where}: ANY LENGTH on the PROCEDURE "
                        + "DIVISION RETURNING item is recognized (ISO §13.18.2.3 SR3b) but not yet implemented "
                        + "(the ANY-LENGTH-RETURNING wave); ANY LENGTH formal parameters are fully supported");
                else
                    continue;   // conformant — keep the flag
            }
            root.IsAnyLength = false;
        }
    }

    private DataItem? FindLinkageRoot(string name) =>
        LinkageRoots.FirstOrDefault(r => string.Equals(r.CobolName, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Collect the EXTERNAL and GLOBAL level-01/77 WORKING-STORAGE roots (ISO §13.18.22 SR1 / §13.18.27 SR1) and
    /// re-base each EXTERNAL record onto a run-unit <c>ExternalStore</c> cell. Runs at the END of <c>Bind</c>
    /// (after REDEFINES classification) so an EXTERNAL record that is also a redefines anchor folds its whole
    /// class onto the shared backing. The mechanism is the existing Tier-B string-canonical machinery: the
    /// backing becomes a <c>ref</c>-property over the external cell, every member an (offset,width) window —
    /// ONE storage copy per name per run unit (§8.6.7), not reset by CANCEL (§14.9.5 GR8). FD-level EXTERNAL
    /// joins here too: the FD's record area is EXTERNAL record data (ISO §13.18.22.4 GR4b — one record area per
    /// run unit, shared by every program describing the file), re-based through the SAME mechanism with the cell
    /// keyed by the file's externalized name (GR5 — so two programs' differently-named records over one EXTERNAL
    /// FD still share the one area; IC227A). FD-level GLOBAL is handled by the emitter's containment merge
    /// (<c>CallBindUnit</c> — §13.18.30 makes the file-name and record-names global names).
    /// </summary>
    internal void CallBindExternalAndGlobal(Core.ProgramUnitContext program)
    {
        if (program.dataDivision()?.workingStorageSection() is { } ws)
            foreach (var entry in ws.dataDescriptionEntry())
            {
                var clauses = entry.dataDescriptionBody()?.dataDescriptionClauses()?.dataDescriptionClause();
                if (clauses is null) continue;
                bool external = clauses.Any(cl => cl.externalClause() is not null);
                bool global = clauses.Any(cl => cl.globalClause() is not null);
                if (!external && !global) continue;
                if (!int.TryParse(entry.levelNumber().GetText(), out int lvl) || lvl is not (1 or 77))
                    continue;   // §13.18.22 SR1 / §13.18.27 SR1 — record-description level-01/77 entries only
                if (entry.dataName()?.GetText() is not { } name) continue;
                var item = Roots.FirstOrDefault(r =>
                    r.Level == lvl && string.Equals(r.CobolName, name, StringComparison.OrdinalIgnoreCase));
                if (item is null) continue;
                if (global) _callGlobalRoots.Add(item);
                if (external) CallMakeExternal(item);
            }

        // EXTERNAL FDs: the record area is ONE run-unit cell keyed by the externalized FILE name (§13.18.22.4
        // GR4b/GR5 — the records of every describer alias it; multi-01 records under the FD are already one
        // REDEFINES class, so re-basing the first record re-bases the whole area). The GR6 same-byte-count
        // conformance check across describers is EC-band work (§14.8.4) — not enforced here.
        foreach (var file in Files)
            if (file is { IsExternal: true, ExternalName: { } extName } && file.Records.Count > 0)
                CallMakeExternal(file.Records[0], "FD::" + extName);

        // GLOBAL FDs: the record-names of a GLOBAL FD are GLOBAL names (ISO §13.18.30 — the file-name and the
        // record-names described subordinate to the FD are global names): the records join the GLOBAL roots so
        // contained programs reach the OWNER's record area through the standard containment bridges
        // (§13.18.27 GR2 — container storage, contained visibility). The file-NAME half of the rule is the
        // emitter's FilesByName containment merge (CallBindUnit).
        foreach (var file in Files)
            if (file.IsGlobal)
                foreach (var rec in file.Records)
                    if (!CallGlobalRoots.Contains(rec))
                        _callGlobalRoots.Add(rec);
    }

    /// <summary>Re-base one EXTERNAL record onto the run-unit external cell (see
    /// <see cref="CallBindExternalAndGlobal"/>). <paramref name="externalName"/> overrides the cell key (the
    /// FD-record case keys by the FILE's externalized name, §13.18.22.4 GR5; a WS record keys by its own name).
    /// A record with a non-DISPLAY (COMP/float/index) leaf would need the Tier-C byte island — rejected loud,
    /// conformant-but-unimplemented.</summary>
    private void CallMakeExternal(DataItem item, string? externalName = null)
    {
        if (ForceStringCanonical(item, "EXTERNAL record") is not { } cls)
        {
            // The silent-skip left the record as ORDINARY storage — the program ran with its EXTERNAL
            // sharing semantics silently dropped (the W1-test finding, Phase 4a). Loud at bind: the
            // reason names the leaf (COMP/float/index Tier-C, or the RESIDUE-11 national/bit cell legs).
            Edition.Error(DiagnosticCatalog.ExternalRecordNotCellBacked, $"EXTERNAL record '{item.CobolName}' cannot be cell-backed — "
                + $"{item.Class?.RejectReason ?? "unsupported leaf"} — recognized but not yet implemented");
            return;
        }
        _callExternalBackings.Add(new CallExternalBacking(
            cls.BackingCsName, externalName ?? item.CobolName!.ToUpperInvariant(), cls.Width,
            CallInitialImage(item).PadRight(cls.Width)));
    }

    /// <summary>The ONE cell-backing forcer (increment-2 factoring of the proven EXTERNAL re-basing —
    /// feedback_singular_pattern): make <paramref name="item"/>'s class Tier-B StringCanonical with NO stored
    /// member, so a heap-cell-backed <c>ref</c>-property with the class's <see cref="RedefinesClass.BackingCsName"/>
    /// can supply the storage (EXTERNAL records → the run-unit <c>ExternalStore</c> cell; ADDRESS-OF-taken items →
    /// a per-instance <c>StorageCell</c>; BASED items → the pointer-deref bridge). A COMP/float/index leaf fails
    /// the shared-character-image gate — the class goes Rejected and every reference fails loud (the caller
    /// skips its bridge registration). Returns the forced class, or null on rejection.</summary>
    internal RedefinesClass? ForceStringCanonical(DataItem item, string what)
    {
        var cls = item.Class;
        if (cls is null)
        {
            cls = new RedefinesClass { Canonical = item };
            cls.Members.Add(item);
            item.Class = cls;
        }

        var leaves = cls.Members.SelectMany(LeavesOf).ToList();
        if (leaves.Any(l => l.Pic is not { IsFloat: false, Usage: Usage.Display }))
        {
            // The gate is usage-keyed, so it also refuses NATIONAL (two bytes per position — a byte-addressed
            // cell window over it would break ADDRESS-OF/BASED/F10 byte arithmetic, RESIDUE-11) and BIT
            // (kept out of cells until the packing residue closes — one leg, one posture). Display-form
            // BOOLEAN leaves PASS deliberately: one '0'/'1' char = one byte (D-B1), cell-safe.
            cls.Classify(RedefinesTier.Rejected, cls.Width,
                $"{what} '{item.CobolName}' has a COMP/float/index/national/bit leaf — the "
                + "shared single-byte character image cannot carry it (Tier-C byte island / the RESIDUE-11 "
                + "2-byte national layout, deferred)");
            return null;
        }

        // The sanctioned RE-classification (see RedefinesClass.Classify): the cell re-base overrides any prior
        // stored-member verdict with the cell-backed Tier-B form (§13.18.22.4 GR5).
        cls.Classify(RedefinesTier.StringCanonical, cls.Members.Max(m => m.ImageWidth * (m.Occurs ?? 1)),
            rejectReason: null);
        foreach (var member in cls.Members)
        {
            AssignClassOffsets(member, 0, cls);
            member.IsCanonical = false;   // NO local stored field — the backing is the cell bridge
        }
        // A numeric-DISPLAY leaf windowed over a string backing decodes/encodes its zoned image (the same
        // image pipeline Tier-B uses — ClassifyRedefinesClasses' rule, applied here for the synth class).
        foreach (var leaf in leaves)
            if (leaf.Pic is { Category: PicCategory.Numeric, IsFloat: false, Usage: Usage.Display })
                MarkImageForced(leaf);   // the collected image fact
        return cls;
    }

    /// <summary>The compile-time initial character image of a record: zoned zeros for a numeric-DISPLAY leaf,
    /// spaces elsewhere, each OCCURS position repeated (ISO §14.6.2.3.2 initial state; VALUE clauses across
    /// EXTERNAL describers must agree per §13.18.22 GR6 — the default image is used uniformly here, the GR6
    /// conformance check being §14.8.4 EC territory).</summary>
    private static string CallInitialImage(DataItem item)
    {
        if (item.IsElementary)
        {
            // Zoned zeros for numeric-DISPLAY; boolean zeros for a boolean leaf (§13.18.63 — its initial
            // state; byte=char under D-B1 so it sits in the cell image directly); spaces elsewhere.
            char fill = item.Pic is { Category: PicCategory.Numeric, IsFloat: false }
                or { Category: PicCategory.Boolean } ? '0' : ' ';
            string one = new(fill, item.ImageWidth);
            return item.Occurs is { } n ? string.Concat(Enumerable.Repeat(one, n)) : one;
        }
        string image = string.Concat(item.Children.Select(CallInitialImage));
        return item.Occurs is { } o ? string.Concat(Enumerable.Repeat(image, o)) : image;
    }

    /// <summary>Seed the unique-id counter so every unit of a multi-program compilation gets a disjoint
    /// <c>_T_n</c> / <c>_P_n</c> band — a nested program class would otherwise SHADOW its container's struct and
    /// profile names, silently re-typing inherited GLOBAL references (the emitter seeds one band per unit).</summary>
    internal void CallSeedUids(int start) => _uidCounter = start;
}
