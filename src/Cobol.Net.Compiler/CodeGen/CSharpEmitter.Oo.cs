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

    /// <summary>Bind one class unit: the OBJECT paragraph's data division binds through the STANDARD
    /// DataBinder over a synthetic program-unit context (the <c>CallReparent</c> discipline — direct-children
    /// accessors see exactly the class's own divisions), producing INSTANCE fields on the emitted class; the
    /// method bodies bind through <c>StatementBinder.BindClassBody</c> into the class's one pc space.</summary>
    private void OoBindClassUnit(OoClassUnit cls, EditionContext edition)
    {
        var data = new DataBinder(edition) { OoClasses = _ooClasses };
        data.CallSeedUids(_callUidBand);
        _callUidBand += 100_000;
        data.Bind(OoReparentClassData(cls.Symbol.Ctx));
        if (data.Files.Count > 0)
            edition.Error("COBOLNET0899",
                $"class '{cls.Name}': a FILE SECTION in the OBJECT paragraph is recognized but not yet "
                + "implemented (owning roadmap phase: Phase 3, OO port)");
        cls.Data = data;
        cls.Refs = new ReferenceResolver(data);
        var binder = new StatementBinder(data, cls.Refs) { OoClasses = _ooClasses };
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
            new FieldEmitter(_ctx).Emit();   // OBJECT WS → INSTANCE fields (D3); VALUE inits = field initializers (D4)
            w.Line();
            foreach (var m in cls.Bound.Methods ?? [])
            {
                // D7: virtual by default (§9.3.6 — dispatch is always on the runtime class). The exit-bounded
                // range is the trap-#4 guard: falling past the method's LAST paragraph returns from the
                // method (the implicit GOBACK), never into a sibling method's paragraphs.
                using (w.Block($"public virtual void {m.CsName}()   // METHOD-ID {m.CobolName} (ISO §11.7)"))
                {
                    if (m.EntryPc <= m.EndPc)
                        w.Line($"try {{ __Dispatch({m.EntryPc}, {m.EndPc}); }} catch (MethodReturn) {{ }}   "
                            + "// GOBACK in a method returns HERE (§14.9.18.4 GR4; deep-dive D8)");
                    // an empty method body falls straight through — the implicit method return
                }
            }
            if (cls.Bound.Paragraphs.Count > 0)
            {
                w.Line();
                w.Line($"private const int __N = {cls.Bound.Paragraphs.Count};   // paragraph count (all methods — one pc space)");
                EmitDispatchMethod(cls.Bound, w);
            }
        }
        w.Line();
    }

    /// <summary>Emit one bound INVOKE (deep-dive D5 — the binder already resolved the call form; this only
    /// renders it).</summary>
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
                // Virtual dispatch on the runtime class (§9.3.6) behind the GR5 null guard: a null receiver
                // raises EC-OO-NULL through the EC engine, never a raw NullReferenceException.
                w.Line($"CobolObject.RequireNonNull({inv.Receiver!.Read()}).{inv.MethodCsName}();   "
                    + "// INVOKE (§14.9.23; null → EC-OO-NULL, §14.9.23.4 GR5)");
                return;
            default:
                w.Line(LoudStmt($"INVOKE call form '{inv.Form}'"));
                return;
        }
    }
}
