// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Bound;
using CobolNet.CodeGen.Emit;
using CobolSharp.Compiler.Generated;

namespace CobolNet.CodeGen;

using Core = CobolParserCore;
using static CobolNet.CodeGen.Emit.EmitText;

/// <summary>
/// The OO half of the Roslyn backend (docs/COBOLNET_OO_DESIGN.md — the Phase-3 spine): one real C# class per
/// CLASS-ID (<c>public class Foo : CobolObject</c>, deep-dive D1/D2), OBJECT-paragraph WORKING-STORAGE as
/// INSTANCE fields (D3), each METHOD-ID as a real virtual C# method (D7) running its contiguous exit-bounded
/// range of the class's ONE PC-dispatch space (the emit-into-a-type parameterization — the CallUnit machinery
/// IS the template: the same per-unit emitter-state switch, FieldEmitter, and __Dispatch body render a class
/// exactly as they render a program class), and INVOKE as direct typed C# calls (D5) — the registry is
/// bypassed entirely (typed calls need no name resolution ABI).
/// </summary>
public sealed partial class CSharpEmitter
{
    /// <summary>One CLASS-ID unit of the compilation group (the ClassUnit counterpart of <see cref="CallUnit"/>):
    /// its pass-1 symbol, its OBJECT-paragraph data model, and its bound method bodies.</summary>
    private sealed class OoClassUnit
    {
        public required OoClassSymbol Symbol;
        public string Name => Symbol.Name;
        public string CsName => Symbol.CsName;
        public DataBinder Data = null!;
        public ReferenceResolver Refs = null!;
        public BoundProgram Bound = null!;
    }

    /// <summary>The group's pass-1 class symbol table (deep-dive D1) — built by <c>CallCollectUnits</c> BEFORE
    /// any unit binds, so every DataBinder (typed object references) and StatementBinder (INVOKE) resolves
    /// classes defined anywhere in the file. Never null after collection (empty table when no classes).</summary>
    private OoClassTable _ooClasses = null!;

    /// <summary>Phase A of class binding — the DATA + SIGNATURES: the OBJECT paragraph's data division binds
    /// through the STANDARD DataBinder over a synthetic program-unit context (the <c>CallReparent</c>
    /// discipline — direct-children accessors see exactly the class's own divisions) producing INSTANCE
    /// fields; each METHOD's LINKAGE/LOCAL-STORAGE/WS sections and PD USING/RETURNING formals bind between
    /// the declaration and resolve halves (slice 2 — <c>DataBinder.OoBindMethodData</c>). Runs for EVERY class
    /// before ANY body binds, so a method of class A INVOKEing class B sees B's full signature regardless of
    /// source order (the pass-1 discipline, deep-dive D1).</summary>
    private void OoBindClassData(OoClassUnit cls, EditionContext edition)
    {
        var data = new DataBinder(edition) { OoClasses = _ooClasses, OoIsClassUnit = true };
        data.CallSeedUids(_callUidBand);
        _callUidBand += 100_000;
        var synthetic = OoReparentClassData(cls.Symbol.Ctx);
        data.BindDeclarations(synthetic);
        foreach (var m in cls.Symbol.Methods)
            data.OoBindMethodData(m);
        data.BindResolve(synthetic);
        if (data.Files.Count > 0)
            edition.Error("COBOLNET0899",
                $"class '{cls.Name}': a FILE SECTION in the OBJECT paragraph is recognized but not yet "
                + "implemented (owning roadmap phase: Phase 3, OO port)");
        cls.Data = data;
        cls.Refs = new ReferenceResolver(data);
    }

    /// <summary>Phase B — the method BODIES bind into the class's one pc space (per-method paragraph AND data
    /// scopes — §11.7; <c>StatementBinder.BindClassBody</c>).</summary>
    private void OoBindClassBody(OoClassUnit cls)
    {
        var binder = new StatementBinder(cls.Data, cls.Refs) { OoClasses = _ooClasses };
        binder.ConfigureEc(_turnState, cls.Name);   // methods fold the same source-ordered >>TURN state (§7.3.25 GR6)
        cls.Bound = binder.BindClassBody(cls.Symbol);
    }

    /// <summary>Re-shape a class definition's data surface into a synthetic <c>programUnit</c> context for the
    /// per-unit DataBinder (the CallReparent pattern — accessors scan DIRECT children only): the OBJECT
    /// paragraph's environment/data divisions, then the class-level environment division (the singular
    /// <c>environmentDivision()</c> accessor returns the FIRST child, so the nearer OBJECT scope wins).</summary>
    private static Core.ProgramUnitContext OoReparentClassData(Core.ClassDefinitionContext ctx)
    {
        var unit = new Core.ProgramUnitContext(null!, -1);
        var obj = ctx.objectParagraph();
        if (obj?.environmentDivision() is { } envObj) unit.AddChild(envObj);
        if (ctx.environmentDivision() is { } envCls) unit.AddChild(envCls);
        if (obj?.dataDivision() is { } dd) unit.AddChild(dd);
        return unit;
    }

    /// <summary>
    /// Emit one COBOL class as a real C# class (deep-dive D1/D2/D3/D7): instance fields from OBJECT data
    /// (VALUE clauses become field initializers — the generated public parameterless ctor IS the predefined
    /// NEW factory, D4: C# runs base-then-derived initialization exactly like COBOL's inherited-then-own
    /// order), one <c>public virtual</c> method per METHOD-ID whose body runs its exit-bounded pc range, and
    /// ONE <c>__Dispatch</c> over the class's whole method-paragraph space (the same dispatcher body a program
    /// class gets — the emit-into-a-type reuse). Runs on the SAME per-unit emitter-state switch as
    /// <see cref="CallEmitProgramClass"/>.
    /// </summary>
    private void OoEmitClassUnit(OoClassUnit cls, CodeWriter w)
    {
        _refs = cls.Refs;
        _ctx = new EmissionContext(w, cls.Data);
        _num = new NumericRenderer(_ctx);
        _cond = new ConditionRenderer(_num, _ctx);
        _callSelfPath = cls.Name;        // a CALL from a method names the class as its calling path (§8.4.6.3)
        _callReturningPlace = null;      // methods deliver results via slice-2 RETURNING, never the program ABI
        _ecUnitHasF3 = false;            // declaratives inside methods are staged loud (no __EcDispatch here)
        _callOuterGlobalUse = false;
        _callInheritedStatusPlace.Clear();

        // v1 restricts to single inheritance and INHERITS emission is port slice 3a (staged loud at pass-1) —
        // every class emitted today roots directly at CobolObject (D2).
        using (w.Block($"public class {cls.CsName} : CobolObject"))
        {
            var fields = new FieldEmitter(_ctx);
            fields.Emit();   // OBJECT WS → INSTANCE fields (D3); method WS → statics; VALUE inits = field initializers (D4)
            if (cls.Bound.Paragraphs.Count > 0)
                w.Line($"private const int __N = {cls.Bound.Paragraphs.Count};   // paragraph count (all methods — one pc space)");
            w.Line();
            foreach (var m in cls.Symbol.Methods)
                OoEmitMethod(cls, m, fields, w);
        }
        w.Line();
    }

    /// <summary>
    /// Emit one METHOD-ID as a real typed C# method (slice 2 — deep-dive D3/D6/D7/D8): BY REFERENCE formals as
    /// <c>ref</c> parameters copied into CAPTURABLE locals (a local function cannot capture a by-ref parameter),
    /// LINKAGE/LOCAL-STORAGE roots as locals (LOCAL-STORAGE re-initializes each activation, §14.5.3), the
    /// method's paragraph slice as a LOCAL-FUNCTION dispatcher (<c>__MDispatch</c> — it captures the locals by
    /// reference, so PERFORM recursion and the implicitly-RECURSIVE method rule, §12032/:12032, are structural),
    /// the ref copy-out, and the RETURNING local as the C# return value (§14.9.23.4 GR8). D7: <c>virtual</c> by
    /// default. The exit-bounded slice is the trap-#4 guard; a group formal crosses as its character image
    /// (the CALL-boundary discipline — a caller's group struct TYPE differs from the method's).
    /// </summary>
    private void OoEmitMethod(OoClassUnit cls, OoMethodSymbol m, FieldEmitter fields, CodeWriter w)
    {
        string retType = m.Returning is { } ret ? (OoStringCarried(ret) ? "string" : ret.ElementType) : "void";
        string sig = string.Join(", ", m.Formals.Select(f =>
            $"ref {(OoStringCarried(f.Item) ? "string" : f.Item.ElementType)} {f.ParamName}"));
        using (w.Block($"public virtual {retType} {m.CsName}({sig})   // METHOD-ID {m.Name} (ISO §11.7)"))
        {
            // LINKAGE roots → locals: a formal seeds from its parameter (copy-in; the copy-out below realizes
            // the BY REFERENCE write-through at the method boundary); the RETURNING item and unattached
            // entries start at their initial state (§14.2.3 GR6 — callee-allocated).
            foreach (var root in m.LinkageRoots)
            {
                var (type, init) = fields.RootDecl(root);
                var formal = m.Formals.FirstOrDefault(f => ReferenceEquals(f.Item, root));
                if (formal is null)
                    w.Line($"{type} {root.CsName} = {init};   // LINKAGE {root.CobolName} (§14.2.3 GR6)");
                else if (root.IsGroup)
                {
                    // The image crossing: construct (arrays allocated), then distribute the caller's image.
                    w.Line($"{type} {root.CsName} = {init};   // LINKAGE formal {root.CobolName} (group — image crossing)");
                    w.Line($"{root.CsName}.FromImage({formal.ParamName});");
                }
                else
                    w.Line($"{type} {root.CsName} = {formal.ParamName};   // LINKAGE formal {root.CobolName} (BY REFERENCE copy-in)");
            }
            foreach (var root in m.LocalRoots)
            {
                var (type, init) = fields.RootDecl(root);
                w.Line($"{type} {root.CsName} = {init};   // LOCAL-STORAGE {root.CobolName} — re-initialized each activation (§14.5.3)");
            }
            if (m.EntryPc <= m.EndPc)
            {
                // The method's slice of the class's one pc space, as a LOCAL FUNCTION (captures the locals
                // above by reference — zero allocation for direct calls).
                string saved = _dispatchName;
                _dispatchName = "__MDispatch";
                EmitDispatchMethod(cls.Bound, w, "int __MDispatch(int __startPc, int __exitPc)", m.EntryPc, m.EndPc);
                _dispatchName = saved;
                w.Line($"try {{ __MDispatch({m.EntryPc}, {m.EndPc}); }} catch (MethodReturn) {{ }}   "
                    + "// GOBACK / falling off the last paragraph returns HERE (§14.9.18.4 GR4; deep-dive D8)");
            }
            foreach (var f in m.Formals)
                w.Line(f.Item.IsGroup
                    ? $"{f.ParamName} = {f.Item.CsName}.AsImage();   // BY REFERENCE copy-out (§14.2.3 GR8)"
                    : $"{f.ParamName} = {f.Item.CsName};   // BY REFERENCE copy-out (§14.2.3 GR8)");
            if (m.Returning is { } r)
                w.Line(r.IsGroup
                    ? $"return {r.CsName}.AsImage();   // the invocation result (§14.9.23.4 GR8)"
                    : $"return {r.CsName};   // the invocation result (§14.9.23.4 GR8)");
        }
        w.Line();
    }

    /// <summary>True when an item CROSSES the INVOKE boundary as a character string: groups (image crossing),
    /// image-stored numerics, alphanumeric / numeric-edited items. Native numerics and object references cross
    /// typed. The §14.8.2 strict-conformance bind rules guarantee both sides agree on the crossing form's
    /// WIDTH/description — which is what keeps the marshaling free of cross-class numeric profiles.</summary>
    private static bool OoStringCarried(DataItem item) =>
        item.IsGroup || item.StoreAsImage
        || item.Pic?.Category is PicCategory.Alphanumeric or PicCategory.NumericEdited;

    private int _ooInvokeCounter;

    /// <summary>Emit one bound INVOKE (deep-dive D5/D6 — the binder already resolved the call form and
    /// validated §14.8.2 strict conformance; this renders the type-preserving marshaling).</summary>
    private void OoEmitInvoke(BoundInvoke inv)
    {
        var w = _ctx.Writer;
        switch (inv.Form)
        {
            case InvokeForm.New:
                // §16.2.1 — the predefined NEW: the generated ctor allocates + VALUE-initializes (D4); the
                // reference is delivered through RETURNING (§14.9.23.4 GR8).
                w.Line(inv.Returning!.Write($"new {inv.ClassCsName}()") + "   // INVOKE … \"NEW\" RETURNING (§16.2.1)");
                return;
            case InvokeForm.Instance:
                OoEmitInstanceInvoke(inv);
                return;
            default:
                w.Line(LoudStmt($"INVOKE call form '{inv.Form}'"));
                return;
        }
    }

    /// <summary>The instance-call marshaling (D6; §14.9.23.4 GR6/GR7a/GR8): every formal is a <c>ref</c>
    /// parameter — a plain field of matching storage passes DIRECTLY (aliasing; subscripts evaluate once at
    /// the call, the GR7a once-only rule); anything else lowers to a copy-in temp, <c>ref</c> the temp, and —
    /// for BY REFERENCE identifier args — a copy-out (write-through at statement granularity). The §14.8.2
    /// strict-conformance bind rules make every crossing type-preserving, so the ONLY numeric profiles
    /// referenced here are the CALLING unit's own (never a private profile of the class being invoked).</summary>
    private void OoEmitInstanceInvoke(BoundInvoke inv)
    {
        var w = _ctx.Writer;
        int id = _ooInvokeCounter++;
        var argExprs = new List<string>();
        var post = new List<string>();

        var args = inv.Args ?? [];
        for (int i = 0; i < args.Count; i++)
        {
            var a = args[i];
            bool stringCarried = OoStringCarried(a.Formal);

            // The direct-ref fast path: a MemberPlace whose STORAGE form matches the parameter type exactly.
            if (a.Source is MemberPlace mp && a.WriteBack
                && (stringCarried
                    ? !mp.Item.IsGroup && OoStringCarried(mp.Item)
                    : !OoStringCarried(mp.Item))
                && !a.Formal.IsGroup && !mp.Item.IsGroup)
            {
                argExprs.Add($"ref {mp.Path}");
                continue;
            }

            // Temp lowering: copy-in per the formal's crossing form.
            string tmp = $"__iv{id}_{i}";
            if (a.Formal.IsGroup || (stringCarried && a.Source?.Item.IsGroup == true))
                w.Line($"string {tmp} = {(a.Source is { } gsp ? CallStringRead(gsp) : CsLiteral(a.StringLiteral ?? ""))};");
            else if (stringCarried)
                w.Line(a.Source is { } sp
                    ? $"string {tmp} = {(OoStringCarried(sp.Item) ? sp.Read() : new NumericImagePlace(sp).Read())};"
                    : a.StringLiteral is { } slit
                    ? $"string {tmp} = CobolString.Store({CsLiteral(slit)}, {Math.Max(1, a.Formal.Pic!.Length)});"
                    // A numeric literal for an IMAGE-STORED method formal: composing the zoned image needs the
                    // formal's profile — a private static of the INVOKED class (the one crossing the strict
                    // rules cannot type-preserve). Narrow (an image-stored formal means the method
                    // whole-group-references its own parameter) — loud, never a wrong image.
                    : $"string {tmp} = {LoudValue("string", $"numeric literal argument for the image-stored method formal '{a.Formal.CobolName}' (slice-2 residue — pass a data item instead)")};");
            else if (a.Formal.Pic is { Category: PicCategory.ObjectReference })
                w.Line($"{a.Formal.ElementType} {tmp} = {a.Source!.Read()};");
            else
                // A native numeric formal: the arg reads as its unscaled value (AsNum bridges an image-stored
                // arg through the ARG's own profile); scales are equal by the bind-time conformance rule.
                w.Line(a.Source is { } np
                    ? $"{a.Formal.ElementType} {tmp} = ({a.Formal.ElementType})({_num.AsNum(new BoundFieldOperand(np)).Expr});"
                    : $"{a.Formal.ElementType} {tmp} = ({a.Formal.ElementType})({EmitText.UnscaledAtScale(a.NumericLiteral!, a.Formal.Pic!.Scale)});");
            argExprs.Add($"ref {tmp}");

            if (!a.WriteBack || a.Source is not { } src) continue;
            // Copy-out to the CALLER's storage (BY REFERENCE — §14.2.3 GR8 at statement granularity).
            if (a.Formal.IsGroup || src.Item.IsGroup)
                post.Add(CallStringWrite(src, tmp));
            else if (stringCarried)
                post.Add(OoStringCarried(src.Item) ? src.Write(tmp) : new NumericImagePlace(src).Write(tmp));
            else
                post.Add(src.Item.StoreAsImage
                    ? src.Write($"CobolNum.FormatDisplay({tmp}, {src.Item.ProfileName})")
                    : src.Write(tmp));
        }

        string call = $"CobolObject.RequireNonNull({inv.Receiver!.Read()}).{inv.MethodCsName}("
            + string.Join(", ", argExprs) + ")";

        if (inv.ReturningSource is { } rs && inv.Returning is { } recv)
        {
            // GR8 — the method's RETURNING item value becomes the result, delivered into identifier-4. The
            // bind-time §14.8.3 conformance made the descriptions identical; only the STORAGE FORMS may
            // differ, bridged through the RECEIVER's own profile.
            string tmp = $"__ivr{id}";
            bool retString = OoStringCarried(rs);
            w.Line($"var {tmp} = {call};   // INVOKE (§14.9.23; null receiver → EC-OO-NULL, GR5)");
            if (rs.IsGroup || recv.Item.IsGroup)
                w.Line(CallStringWrite(inv.Returning, tmp));
            else if (retString == OoStringCarried(recv.Item))
                w.Line(recv.Write(tmp));
            else if (retString)   // string-carried result into native-numeric storage
                w.Line(recv.Write($"({recv.Item.ElementType})CobolNum.ParseDisplay({tmp}, {recv.Item.ProfileName})"));
            else                  // native result into image-stored numeric storage
                w.Line(recv.Write($"CobolNum.FormatDisplay({tmp}, {recv.Item.ProfileName})"));
        }
        else
            w.Line($"{call};   // INVOKE (§14.9.23; null receiver → EC-OO-NULL, §14.9.23.4 GR5)");

        foreach (var p in post) w.Line(p);
    }
}
