// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Generated;

namespace CobolNet.Binding;

using Core = CobolParserCore;

/// <summary>The method-local NAME SCOPE of one METHOD's data (ISO §11.7 GR5 — a method-local name SHADOWS the
/// same name in object data; and it is INVISIBLE to sibling methods, the legacy trap-#6 cross-wiring guard made
/// structural): the per-method overlay <see cref="ReferenceResolver"/> and the condition-name lookup consult
/// FIRST while binding this method's statements. Built by <see cref="DataBinder.OoBindMethodData"/>, activated
/// per-pc by <c>StatementBinder.BindClassBody</c> through <see cref="DataBinder.ActiveMethodScope"/>.</summary>
public sealed class OoMethodDataScope
{
    public Dictionary<string, List<DataItem>> ByName { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, List<Condition88>> Conditions { get; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// The METHOD-data half of the data binder (OO deep-dive D3/D6 — port slice 2): a method's LINKAGE SECTION
/// (→ typed C# parameters via capturable locals), LOCAL-STORAGE SECTION (→ C# locals, re-initialized each
/// activation, §14.5.3), and — in the editions that permit it — WORKING-STORAGE (→ STATIC fields, shared across
/// instances and persistent across activations per §11.7; ILLEGAL in 2023 per §13.5.3 SR 1, gated by the
/// EditionValidator's <c>method-working-storage-window</c> row). Items bind into the CLASS's one forest (so
/// USAGE/SIGN inheritance, object-reference resolution, profiles, and struct types all apply unchanged) but
/// their NAMES move into the method's own scope.
/// </summary>
public sealed partial class DataBinder
{
    /// <summary>The name scope of the method whose statements are CURRENTLY being bound (set per-pc by
    /// <c>StatementBinder.BindClassBody</c>; null while binding program/object-level code).</summary>
    public OoMethodDataScope? ActiveMethodScope { get; set; }

    /// <summary>True when this binder binds a CLASS unit (set by the emitter's <c>OoBindClassData</c>): a root
    /// that is NOT method-scoped is then OBJECT data — §14.9.23.3 SR 10 bans it from crossing an INVOKE
    /// BY REFERENCE (a bare object-data argument is assumed BY CONTENT instead, §14.9.23.4 GR6a2).</summary>
    public bool OoIsClassUnit { get; set; }

    /// <summary>Every method-scoped root of the class (union over all methods) — the SR 10 discriminator:
    /// an item whose 01/77 root is NOT in this set, in a class unit, is object data.</summary>
    public HashSet<DataItem> OoMethodScopedRoots { get; } = [];

    /// <summary>Method WORKING-STORAGE roots (D3: STATIC fields — one copy per class, shared across instances,
    /// persistent across activations). Consumed by <see cref="CodeGen.Emit.FieldEmitter"/> to emit the root
    /// field with the <c>static</c> modifier.</summary>
    public HashSet<string> OoStaticRootFields { get; } = new(StringComparer.Ordinal);

    /// <summary>The ONE scope-aware data-name lookup (§8.4.6.2.1 rule 3a — a method-local declaration
    /// REPLACES, never unions with, the object/program-level name): consumers that read <see cref="ByName"/>
    /// directly for a USER-WRITTEN name (SEARCH/SORT table resolution, INITIALIZE) route through this so a
    /// method-local name shadows correctly. Mirrors ReferenceResolver.ResolveUnqualified.</summary>
    public List<DataItem>? LookupData(string name)
    {
        if (ActiveMethodScope is { } m && m.ByName.TryGetValue(name, out var mlist) && mlist.Count > 0)
            return mlist;
        return ByName.TryGetValue(name, out var list) && list.Count > 0 ? list : null;
    }

    /// <summary>Scope-aware INDEX-NAME lookup (§8.4.6.2.1 rule 3a / §8.4.6.2.3): a METHOD-LOCAL data-name
    /// SHADOWS an object-level index-name of the same spelling — without this, every IndexFields-first
    /// consumer would silently bind the subscript/SET target to the OBJECT's index cell (a torn read/write
    /// of the wrong storage).</summary>
    public bool TryGetVisibleIndexField(string name, out string field)
    {
        field = "";
        if (ActiveMethodScope is { } m && m.ByName.TryGetValue(name, out var mlist) && mlist.Count > 0)
            return false;   // the method-local data-name wins (§8.4.6.2.1 rule 3a)
        return IndexFields.TryGetValue(name, out field!);
    }

    /// <summary>True when <paramref name="item"/> is OBJECT data of a class unit (§14.9.23.3 SR 10): its 01/77
    /// root is not method-scoped. Always false outside a class unit.</summary>
    public bool OoIsObjectData(DataItem item)
    {
        if (!OoIsClassUnit) return false;
        DataItem root = item;
        while (root.Parent is { } p) root = p;
        return !OoMethodScopedRoots.Contains(root);
    }

    /// <summary>
    /// Bind one METHOD's data sections into the class forest and its own name scope, and resolve the method's
    /// PROCEDURE DIVISION USING/RETURNING formals (positional, §14.9.23.4 GR3). Runs between
    /// <see cref="BindDeclarations"/> and <see cref="BindResolve"/> so the post-build passes cover method items.
    /// Part-2-of-slice-2 boundaries stage LOUD (never silently drop): REDEFINES / OCCURS INDEXED BY / OCCURS
    /// DEPENDING / EXTERNAL / GLOBAL / level-66 inside method data, and a FILE or REPORT section in a method.
    /// </summary>
    internal void OoBindMethodData(OoMethodSymbol m)
    {
        string where = $"method '{m.Name}'";
        if (m.Ctx.environmentDivision() is not null)
            Edition.Error("COBOLNET0899", $"{where}: a method's own ENVIRONMENT DIVISION (ISO §11.7) is "
                + "recognized but not yet implemented (owning roadmap phase: Phase 3, OO port)");

        var dd = m.Ctx.dataDivision();
        if (dd is not null)
        {
            if (dd.fileSection() is not null || dd.reportSection() is not null || dd.screenSection() is not null)
                Edition.Error("COBOLNET0899", $"{where}: a FILE/REPORT/SCREEN SECTION in a method definition "
                    + "is recognized but not yet implemented (owning roadmap phase: Phase 3, OO port)");
            if (dd.workingStorageSection() is { } ws)
            {
                // D3: method WS → STATIC fields (per-class, shared, persistent — §11.7; the naive instance-field
                // mapping silently miscompiles a method-WS counter). The 2023 §13.5.3 SR 1 ban is the
                // EditionValidator's method-working-storage-window row — binding proceeds so `--permissive`
                // keeps the pre-removal semantics (the §10 #1 migration contract).
                foreach (var entry in ws.dataDescriptionEntry())
                {
                    var clauses = entry.dataDescriptionBody()?.dataDescriptionClauses()?.dataDescriptionClause();
                    // EXTERNAL/GLOBAL in method WS would silently miss CallBindExternalAndGlobal (it scans the
                    // synthetic unit's OBJECT section only) — gate loud rather than mis-scope run-unit storage.
                    if (clauses is not null && clauses.Any(cl =>
                            cl.externalClause() is not null || cl.globalClause() is not null))
                        Edition.Error("COBOLNET0899", $"{where}: EXTERNAL/GLOBAL on a method WORKING-STORAGE "
                            + "item is recognized but not yet implemented (Phase 3, OO port)");
                }
                var roots = BindEntries(ws.dataDescriptionEntry(), _rootNames);
                m.StaticRoots.AddRange(roots);
                foreach (var r in roots) OoStaticRootFields.Add(r.CsName);
            }
            if (dd.localStorageSection() is { } ls)
                m.LocalRoots.AddRange(BindEntries(ls.dataDescriptionEntry(), _rootNames));
            if (dd.linkageSection() is { } lk)
                m.LinkageRoots.AddRange(BindEntries(
                    lk.linkageEntry().Select(e => e.dataDescriptionEntry()).Where(e => e is not null).Select(e => e!),
                    _rootNames));
        }

        foreach (var root in m.StaticRoots.Concat(m.LocalRoots).Concat(m.LinkageRoots))
        {
            OoMethodScopedRoots.Add(root);
            OoGateUnsupportedShapes(root, where);
            OoScopeSubtree(root, m.DataScope);
        }
        // LINKAGE + LOCAL-STORAGE roots are C# LOCALS of the emitted method (their struct types and numeric
        // profiles still emit at class level) — never instance fields. Method-WS roots DO emit (as statics).
        foreach (var root in m.LocalRoots.Concat(m.LinkageRoots))
            CallSuppressedRootFields.Add(root.CsName);

        // The PD header formals (§14.2.2 SR1 — level-01/77 LINKAGE entries; correspondence is positional).
        // Every formal is BY REFERENCE (the header BY VALUE phrase is a grammar extension not yet parsed —
        // it would stage here, loud, when added).
        var pd = m.Ctx.procedureDivision();
        int pos = 0;
        foreach (var dref in pd?.usingClause()?.dataReferenceList()?.dataReference() ?? [])
        {
            string pname = dref.GetText();
            var item = m.LinkageRoots.FirstOrDefault(r =>
                string.Equals(r.CobolName, pname, StringComparison.OrdinalIgnoreCase));
            if (item is null)
                Edition.Error("COBOLNET0888", $"{where}: PROCEDURE DIVISION USING parameter '{pname}' is not "
                    + "a level-01/77 LINKAGE SECTION item of the method (ISO §14.2.2 SR1)");
            else
                m.Formals.Add(new OoFormal(item, pos, OoParamName(m, item, pos)));
            pos++;
        }
        if (pd?.returningClause()?.dataReference() is { } rref)
        {
            m.Returning = m.LinkageRoots.FirstOrDefault(r =>
                string.Equals(r.CobolName, rref.GetText(), StringComparison.OrdinalIgnoreCase));
            if (m.Returning is null)
                Edition.Error("COBOLNET0888", $"{where}: PROCEDURE DIVISION RETURNING item '{rref.GetText()}' "
                    + "is not a level-01/77 LINKAGE SECTION item of the method (ISO §14.2.2 SR1)");
            else if (m.Formals.Any(f => ReferenceEquals(f.Item, m.Returning)))
                Edition.Error("COBOLNET0888", $"{where}: '{rref.GetText()}' may not be both a USING parameter "
                    + "and the RETURNING item (ISO §14.2.2 SR4)");
        }
        if (pd?.raisingClause() is { } mrc)
            foreach (var w in mrc.cobolWord())
            {
                // The same §14.2.2 partition as program headers (D-EO8): EC-USER level-3 names and classes.
                string up = w.GetText().ToUpperInvariant();
                if (CobolNet.Runtime.Exceptions.ExceptionCatalog.TryGet(up, out var einfo))
                {
                    if (einfo.Level is 3 && einfo.Level2Parent is "EC-USER") m.RaisingEcNames.Add(up);
                    else Edition.Error("COBOLNET0858", $"{where}: METHOD-ID RAISING {up}: an exception-name "
                        + "here shall be a level-3 EC-USER name (ISO §14.2.2 SR7)");
                }
                else if (OoClasses?.Find(up) is not null) m.RaisingClasses.Add(up);
                else Edition.Error("COBOLNET0858", $"{where}: METHOD-ID RAISING {up}: not an exception-name "
                    + "or a class of the compilation group (ISO §14.2.2 SR7–SR9; interfaces are a later "
                    + "refinement)");
            }
        // A GROUP formal/RETURNING item crosses the boundary as its character image (FromImage/AsImage) —
        // register it whole-group-referenced NOW so MarkStoreAsImage flips its numeric-DISPLAY leaves to
        // image storage and untouched caller bytes round-trip unchanged (§14.2.3 GR8; the review's
        // spaces→zeros corruption finding). Mirrors the program-formal registration in CallBindUnit.
        foreach (var f in m.Formals)
            if (f.Item.IsGroup)
                WholeGroupReferenced.Add(f.Item);
        if (m.Returning is { IsGroup: true } retg)
            WholeGroupReferenced.Add(retg);
    }

    /// <summary>The C# parameter name for a formal: <c>__</c> + the sanitized COBOL name, uniquified within
    /// the method. The <c>__</c> prefix can never collide with a COBOL-derived name (a COBOL word cannot
    /// contain consecutive hyphens' image <c>__</c> — the dispatcher-internals naming rule), and in particular
    /// not with the item's own <see cref="DataItem.CsName"/>, which becomes the capturable LOCAL the body
    /// reads and writes (a C# local may not shadow a parameter — CS0136).</summary>
    private static string OoParamName(OoMethodSymbol m, DataItem item, int pos)
    {
        string name = "__" + DataItem.Sanitize(item.CobolName ?? $"P{pos}").ToUpperInvariant();
        while (m.Formals.Any(f => f.ParamName == name)) name += "_";
        return name;
    }

    /// <summary>Move a method root subtree's NAME registrations (items + level-88 conditions) out of the class
    /// globals into the method's own scope (§11.7 GR5 shadowing / sibling invisibility).</summary>
    private void OoScopeSubtree(DataItem item, OoMethodDataScope scope)
    {
        if (item.CobolName is { } n)
        {
            if (ByName.TryGetValue(n, out var list))
            {
                list.Remove(item);
                if (list.Count == 0) ByName.Remove(n);
            }
            if (!scope.ByName.TryGetValue(n, out var slist)) scope.ByName[n] = slist = [];
            slist.Add(item);
        }
        // Level-88s under this item: transfer by conditional-variable identity.
        foreach (var (condName, conds) in Conditions.ToList())
        {
            List<Condition88>? moved = null;
            foreach (var c in conds)
                if (ReferenceEquals(c.Parent, item))
                    (moved ??= []).Add(c);
            if (moved is null) continue;
            foreach (var c in moved) conds.Remove(c);
            if (conds.Count == 0) Conditions.Remove(condName);
            if (!scope.Conditions.TryGetValue(condName, out var sc)) scope.Conditions[condName] = sc = [];
            sc.AddRange(moved);
        }
        foreach (var child in item.Children) OoScopeSubtree(child, scope);
        foreach (var ren in item.Renames66) OoScopeSubtree(ren, scope);
    }

    /// <summary>One RESOLVED object-property reference awaiting its statement-level desugar (deep-dive
    /// D-P2; ISO §8.4.3.9.4): the synthesized temp the statement bound over, the receiver (a Place for the
    /// instance form, null for the <c>prop OF Class-name</c> factory form), the accessor symbols found on
    /// the pinned-name roster (either may be null — SR3/SR4 checked against the CLASSIFIED polarity, not
    /// eagerly), and the source names for diagnostics. Registered by ReferenceResolver at resolution time,
    /// drained by StatementBinder.OoWrapPropertyOps after the carrying statement binds.</summary>
    internal sealed record OoPendingPropertyOp(
        DataItem Temp, Place? Receiver, string ClassCsName, bool Factory,
        OoMethodSymbol? Get, OoMethodSymbol? Set, string PropName, string ReceiverName);

    /// <summary>The unit's un-drained property-reference ops (statement-scoped: BindStatement marks the
    /// count on entry and drains only its own suffix, so a reference in an IF condition belongs to the IF,
    /// not to an arm statement that binds later).</summary>
    internal List<OoPendingPropertyOp> OoPendingPropertyOps { get; } = [];

    /// <summary>Synthesize the GR1/GR2/GR3 compiler temp for one property reference: a level-1 elementary
    /// item CLONED from the accessor's crossing description (<paramref name="model"/> = the GET RETURNING
    /// item or the SET formal — identical by the §13.18.42 clone rule / the 0842 SR7 description-equality
    /// check). One temp per REFERENCE (GR1 temp-1 / GR2 temp-2; GR3 reuses one — the caller decides).</summary>
    internal DataItem OoCreatePropertyTemp(DataItem model, string prop) =>
        CreateCompilerTemp(model, "__PROP-TEMP-", "__prop", prop);

    /// <summary>The (temp, model) clone pairs <see cref="CreateCompilerTemp"/> produced — consumed by the
    /// run-unit emitter's post-bind re-sync: <c>StoreAsImage</c> is still MUTABLE while procedure bodies
    /// bind (a ref-mod store or a figurative MOVE inside the MODEL's own unit flips it AFTER a temp cloned
    /// it — the M2-UDF-1 review's unit-order desync), so the frozen copy must be re-read once every
    /// procedure has bound.</summary>
    internal List<(DataItem Temp, DataItem Model)> CompilerTempClones { get; } = [];

    /// <summary>The ONE synthesized-compiler-temp constructor (property-reference temps, user-function result
    /// temps): a level-1 elementary item cloned from <paramref name="model"/>'s description, appended to
    /// <see cref="Roots"/> so the FieldEmitter declares it like any other item. The pair is recorded for the
    /// post-bind <c>StoreAsImage</c> re-sync (see <see cref="CompilerTempClones"/>).</summary>
    internal DataItem CreateCompilerTemp(DataItem model, string cobolPrefix, string csPrefix, string tag)
    {
        var t = new DataItem
        {
            Level = 1,
            CobolName = cobolPrefix + _uidCounter,
            CsName = csPrefix + _uidCounter + "_" + DataItem.Sanitize(tag).ToUpperInvariant(),
            Pic = model.Pic,
            OwnSign = model.OwnSign,
            Justified = model.Justified,
            BlankWhenZero = model.BlankWhenZero,
            StoreAsImage = model.StoreAsImage,
        };
        t.Uid = _uidCounter++;
        Roots.Add(t);
        CompilerTempClones.Add((t, model));
        return t;
    }

    /// <summary>Scan the OBJECT/FACTORY WORKING-STORAGE parse entries for PROPERTY clauses (§13.18.42) and
    /// SYNTHESIZE the accessor method symbols (D-P1 — the PINNED §11.7.4 GR1a implementor naming
    /// <c>__GET_&lt;P&gt;</c>/<c>__SET_&lt;P&gt;</c>): GET returns the SUBJECT item's description; SET takes
    /// one formal of it. The emitter renders DIRECT field bodies — observably identical to the spec's
    /// implicit MOVE methods (GR1/GR2 :21214-21229) because the descriptions are identical by construction.
    /// SR checks are the 0842 family; WITH NO GET/SET suppresses the accessor; explicit GET/SET PROPERTY
    /// methods (already on the roster) take precedence — a clause + an explicit accessor for the same
    /// property is the §11.7 SR5 duplicate (0842).</summary>
    internal void OoBindPropertyClauses(OoClassSymbol cls, Core.WorkingStorageSectionContext? ws, bool factory)
    {
        foreach (var entry in ws?.dataDescriptionEntry() ?? [])
        {
            var clauses = entry.dataDescriptionBody()?.dataDescriptionClauses()?.dataDescriptionClause();
            var pc = clauses?.Select(c => c.propertyClause()).FirstOrDefault(c => c is not null);
            if (pc is null) continue;
            string where = $"class '{cls.Name}'{(factory ? " (FACTORY)" : "")}";
            if (entry.dataName()?.GetText() is not { } subjName)
            {
                Edition.Error("COBOLNET0842", $"{where}: a PROPERTY clause requires a named data item "
                    + "(ISO §13.18.42 — FILLER cannot be a property subject)");
                continue;
            }
            var subject = Roots.SelectMany(Flatten).FirstOrDefault(i =>
                string.Equals(i.CobolName, subjName, StringComparison.OrdinalIgnoreCase));
            if (subject is null) continue;   // the entry failed to bind — already diagnosed
            if (subject.Occurs is not null)
            {
                Edition.Error("COBOLNET0842", $"{where}: property subject '{subjName}' shall not carry "
                    + "OCCURS (ISO §13.18.42 SR — no table subjects)");
                continue;
            }
            // Superclass property-name collision (§13.18.42.3 SR4): walk the base chain's accessor rosters.
            for (var b = cls.Base; b is not null; b = b.Base)
                if ((factory ? b.FactoryMethods : b.Methods).Any(bm =>
                        string.Equals(bm.PropertyName, subjName, StringComparison.OrdinalIgnoreCase)))
                    Edition.Error("COBOLNET0842", $"{where}: property '{subjName}' collides with a property "
                        + $"of superclass '{b.Name}' (ISO §13.18.42.3 SR4)");

            bool noGet = pc.NO() is not null && pc.GET() is not null;
            bool noSet = pc.NO() is not null && pc.SET() is not null;
            string sanitized = DataItem.Sanitize(subjName).ToUpperInvariant();
            if (!noGet)
                AddAccessor('G', "__GET_" + sanitized);
            if (!noSet)
                AddAccessor('S', "__SET_" + sanitized);

            void AddAccessor(char kind, string csName)
            {
                var m = new OoMethodSymbol(csName,
                    HasUsing: kind == 'S', HasReturning: kind == 'G',
                    Ctx: null!)
                {
                    CsName = csName, Owner = cls, IsFactory = factory,
                    Accessor = kind, PropertyName = subjName, PropertySubject = subject,
                    IsFinal = pc.FINAL() is not null,
                };
                if (kind == 'G') m.Returning = subject; else m.Formals.Add(new OoFormal(subject, 0, "__V"));
                bool added = factory ? cls.TryAddFactoryMethod(m) : cls.TryAddMethod(m);
                if (!added)
                    Edition.Error("COBOLNET0842", $"{where}: duplicate accessor for property '{subjName}' — "
                        + "a data-name with the PROPERTY clause shall not also have an explicit GET/SET "
                        + "PROPERTY method (ISO §11.7 SR5), and only one PROPERTY clause per name");
            }
        }

        static IEnumerable<DataItem> Flatten(DataItem i)
        {
            yield return i;
            foreach (var c in i.Children)
                foreach (var d in Flatten(c))
                    yield return d;
        }
    }

    /// <summary>Stage the method-data shapes slice 2 does not carry yet — LOUD, naming the owning wave.</summary>
    private void OoGateUnsupportedShapes(DataItem item, string where)
    {
        if (item.RedefinesTargetName is not null)
            Edition.Error("COBOLNET0899", $"{where}: REDEFINES on the method data item "
                + $"'{item.CobolName ?? "FILLER"}' is recognized but not yet implemented (Phase 3, OO port)");
        if (item.IndexNames.Count > 0)
            Edition.Error("COBOLNET0899", $"{where}: OCCURS … INDEXED BY on the method data item "
                + $"'{item.CobolName ?? "FILLER"}' is recognized but not yet implemented (Phase 3, OO port)");
        if (item.OccursSpec?.Depending is not null || item.OccursSpec?.DependingName is not null)
            Edition.Error("COBOLNET0899", $"{where}: OCCURS DEPENDING ON on the method data item "
                + $"'{item.CobolName ?? "FILLER"}' is recognized but not yet implemented (Phase 3, OO port)");
        // level-66 RENAMES in method data is LIVE (M2-OO-1h step 1, DEVLOG 637): ResolveRedefines resolves the
        // alias FROM/THRU structurally via FindDescendantOrSelf over the owning record (DataBinder.cs:1128-1152),
        // so it is correct regardless of OoScopeSubtree's name re-homing — no gate needed.
        foreach (var child in item.Children) OoGateUnsupportedShapes(child, where);
        // Level-66s live OFF the children (Renames66) — without this walk the gate above is dead code and a
        // 66 in method data slips through unstaged (the 3a/3b review's dead-gate finding).
        foreach (var ren in item.Renames66) OoGateUnsupportedShapes(ren, where);
    }
}
