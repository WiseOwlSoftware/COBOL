// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Generated;

namespace CobolNet.Binding.Bound;

using Core = CobolParserCore;

/// <summary>An INVOKE's resolved call form (OO deep-dive D1/D5 — the binder chooses; both backends render).</summary>
public enum InvokeForm
{
    /// <summary><c>INVOKE Class "NEW" RETURNING obj</c> → <c>obj = new Class()</c> (§16.2.1 — the predefined
    /// NEW factory; the generated public ctor chains base then VALUE-initializes, deep-dive D4).</summary>
    New,
    /// <summary><c>INVOKE obj "M" …</c> → <c>RequireNonNull(obj).M(…)</c> — virtual dispatch on the runtime
    /// class (§9.3.6) behind the §14.9.23.4 GR5 null guard.</summary>
    Instance,
}

/// <summary>A bound INVOKE (ISO §14.9.23; deep-dive D5): the RESOLVED call form plus everything the backend
/// needs to render it — no name lookup happens at emit time. <paramref name="ClassCsName"/> is the emitted C#
/// type (New form); <paramref name="Receiver"/>/<paramref name="MethodCsName"/> drive the Instance form (the
/// method name is the ROSTER's exact spelling — COBOL names compare case-insensitively, §8.3.2.2, and the
/// C# override chain must reuse one spelling — the legacy trap-#2 rule); <paramref name="Returning"/> receives
/// the invocation result (NEW's created object today; method RETURNING values land with port slice 2).</summary>
public sealed record BoundInvoke(
    InvokeForm Form, string? ClassCsName, Place? Receiver, string? MethodCsName, Place? Returning)
    : BoundStatement;

/// <summary>A method-context <c>GOBACK</c> / (pre-2023) <c>EXIT METHOD</c> (ISO §14.9.18.4 GR4; deep-dive D8 —
/// the one decision that silently miscompiles if missed): terminates the executing METHOD only, returning
/// control to the INVOKE site — never the run unit (<see cref="BoundStop"/>) and never the program activation
/// (<see cref="BoundGoback"/>). Rendered as <c>throw new MethodReturn()</c>, caught at the method's public
/// entry (the ProgramReturn-pattern realization of D8: a plain <c>return</c> cannot unwind the nested bounded
/// <c>__Dispatch</c> frames an out-of-line PERFORM stacks).</summary>
public sealed record BoundMethodReturn : BoundStatement;

public sealed partial class StatementBinder
{
    /// <summary>The group's pass-1 class symbol table (deep-dive D1) — set by the run-unit emitter before
    /// binding so INVOKE resolves classes/methods defined anywhere in the group. Null ⇔ empty group.</summary>
    public OoClassTable? OoClasses { get; set; }

    /// <summary>The paragraph scope of ONE method body (ISO §11.7 — the legacy per-method scope algorithm,
    /// ported): paragraph and section names declare METHOD-LOCALLY; PERFORM/GO TO inside the method resolve
    /// against THESE maps only, so sibling methods may reuse names and a cross-method transfer fails loud
    /// (the legacy traps #4/#5/#10 are structural here, not checks).</summary>
    private sealed class OoMethodScope
    {
        public readonly Dictionary<string, int> Paras = new(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, SectionInfo> Sections = new(StringComparer.OrdinalIgnoreCase);
    }

    private OoMethodScope? _currentMethodScope;               // ambient during method collection AND binding
    private readonly List<OoMethodScope?> _paraMethod = [];   // per-pc owning method (parallel to _paras)

    /// <summary>True while binding a statement inside a METHOD body — the D8 context switch (GOBACK →
    /// method return; EXIT PROGRAM → §14.9.14.3 SR7 violation).</summary>
    private bool InMethod => _currentMethodScope is not null;

    /// <summary>Appended to unknown-procedure guards bound inside a method: names resolve METHOD-LOCALLY
    /// (§11.7), so a reference to a sibling method's paragraph fails HERE by design (the legacy trap-#10
    /// cross-method reject) — the hint tells the reader why the name a human can see is "unknown".</summary>
    private string OoScopeHint => InMethod
        ? " (method-local resolution, ISO §11.7 — paragraphs of sibling methods and of the driver program are not visible in a method)"
        : "";

    /// <summary>
    /// Bind a CLASS body: every method's paragraphs flatten into the class's ONE pc space (source order), each
    /// method holding its contiguous exit-bounded range — the emit-into-a-type spine's binding half. The part-2
    /// scope binds parameterless void methods completely; a method's own data division, PD USING/RETURNING/
    /// RAISING formals, and declaratives are recognized-but-staged loud (port slice 2), never silently skipped.
    /// </summary>
    public BoundProgram BindClassBody(OoClassSymbol cls)
    {
        var used = new HashSet<string>(StringComparer.Ordinal);
        var methods = new List<BoundMethod>(cls.Methods.Count);

        foreach (var m in cls.Methods)
        {
            var scope = new OoMethodScope();
            _currentMethodScope = scope;
            m.EntryPc = _paras.Count;
            if (m.Ctx.environmentDivision() is not null || m.Ctx.dataDivision() is not null)
                data.Edition.Error("COBOLNET0899",
                    $"class '{cls.Name}', method '{m.Name}': a method's own ENVIRONMENT/DATA DIVISION "
                    + "(LOCAL-STORAGE locals, LINKAGE formals — ISO §11.7/§13.5.3) is recognized but not yet "
                    + "implemented (owning roadmap phase: Phase 3, OO port slice 2)");
            if (m.Ctx.procedureDivision() is { } pd)
            {
                if (pd.usingClause() is not null || pd.returningClause() is not null
                    || pd.raisingClause() is not null)
                    data.Edition.Error("COBOLNET0899",
                        $"class '{cls.Name}', method '{m.Name}': PROCEDURE DIVISION USING/RETURNING/RAISING "
                        + "on a method (typed parameter marshaling, ISO §14.9.23.4 GR6/GR8) is recognized but "
                        + "not yet implemented (owning roadmap phase: Phase 3, OO port slice 2)");
                if (pd.declarativePart().Length > 0)
                    data.Edition.Error("COBOLNET0899",
                        $"class '{cls.Name}', method '{m.Name}': DECLARATIVES inside a method (ISO §14.2.1) "
                        + "are recognized but not yet implemented (owning roadmap phase: Phase 3, OO port)");
                foreach (var unit in pd.procedureUnit())
                {
                    if (unit.paragraphDefinition() is { } para)
                        AddParagraph(para.paragraphName().GetText(), para.sentence(), null, used);
                    else if (unit.sectionDefinition() is { } section)
                    {
                        // A section inside a method is a method-local pc range (the legacy COBOL0116 reject is
                        // superseded: with per-method scopes the range cannot truncate or leak — trap #5).
                        var info = new SectionInfo(section.sectionName().GetText(), _paras.Count);
                        foreach (var p in section.paragraphDefinition())
                            AddParagraph(p.paragraphName().GetText(), p.sentence(), info, used);
                        info.EndPc = _paras.Count - 1;
                        scope.Sections.TryAdd(info.Name, info);
                    }
                }
            }
            m.EndPc = _paras.Count - 1;
            methods.Add(new BoundMethod(m.Name, m.CsName, m.EntryPc, m.EndPc));
        }
        _currentMethodScope = null;

        var bound = new List<BoundParagraph>(_paras.Count);
        for (int i = 0; i < _paras.Count; i++)
        {
            _currentSection = _paraSection[i];        // in-section resolution first (§8.4.2.2)
            _currentMethodScope = _paraMethod[i];     // then the OWNING METHOD's scope — never a sibling's
            _currentBindPc = i;
            var sentences = new List<IReadOnlyList<BoundStatement>>();
            foreach (var sentence in _paras[i].Sentences)
                sentences.Add(sentence.statement().Select(BindStatement).ToList());
            bound.Add(new BoundParagraph(_paras[i].Cobol, sentences));
        }
        _currentSection = null;
        _currentMethodScope = null;
        _currentBindPc = -1;
        return new BoundProgram(bound, 0, null, BuildEcFeatures(), methods);
    }

    // ── INVOKE (ISO §14.9.23; deep-dive D5) ─────────────────────────────────────────────────────────────────

    /// <summary>Bind one INVOKE: resolve the receiver (identifier-1 first, class-name-1 second — a data-name
    /// shadows a class-name at reference resolution), the LITERAL method name, and the call form against the
    /// pass-1 symbol table. Part-2 spine scope: <c>Class "NEW" RETURNING obj</c> and the no-arg instance call
    /// are LIVE; SELF/SUPER (slice 3b), factory calls (§11.4 slice), USING/RETURNING marshaling (slice 2),
    /// universal/dynamic dispatch (D10 wave) stage loud.</summary>
    private BoundStatement OoBindInvoke(Core.InvokeStatementContext inv)
    {
        var target = inv.invokeTarget().objectReference();

        // The method selector: a nonnumeric literal binds statically; identifier-2 (a method name held in a
        // data item) is legal ONLY through a universal receiver (§14.9.23.3 SR7) — the D10 dynamic wave.
        string? methodName = inv.invokeMethodName().literal() is { } lit
            ? lit.nonNumericLiteral()?.STRINGLIT() is { } sl ? DecodeCobolString(sl.GetText()) : null
            : null;
        if (inv.invokeMethodName().dataReference() is not null)
            return new BoundUnsupported("INVOKE with identifier-2 as the method selector (a method name held "
                + "in a data item — universal/dynamic dispatch, ISO §14.9.23.3 SR7; deep-dive D10 wave)");
        if (methodName is null)
        {
            data.Edition.Error("COBOLNET0823",
                "INVOKE: literal-1 (the method name) shall be a nonnumeric literal (ISO §14.9.23.3 SR5)");
            return new BoundNop();
        }

        if (target.SELF() is not null || target.SUPER() is not null)
            return new BoundUnsupported("INVOKE SELF/SUPER (ISO §8.4.3.8 — port slice 3b)");
        if (target.dataReference() is not { } dref)
        {
            data.Edition.Error("COBOLNET0823",
                "INVOKE NULL: the receiver shall be an object-reference identifier or a class-name "
                + "(ISO §14.9.23.3 — the predefined NULL object reference cannot be a receiver)");
            return new BoundNop();
        }

        // identifier-1 vs class-name-1 (§14.9.23.2): resolve as a data item first (a data-name shadows);
        // an unresolved SIMPLE name is then a class-name candidate in the pass-1 table.
        if (refs.Resolve(dref) is { } receiver)
            return OoBindInstanceInvoke(inv, receiver, methodName);
        if (OoClasses?.Find(dref.GetText()) is { } cls)
            return OoBindClassInvoke(inv, cls, methodName);
        data.Edition.Error("COBOLNET0823",
            $"INVOKE: '{dref.GetText()}' is neither a resolvable data item nor a class of the compilation "
            + "group (ISO §14.9.23.2 — identifier-1 or class-name-1)");
        return new BoundNop();
    }

    /// <summary><c>INVOKE class-name-1 …</c>: the predefined NEW (§16.2.1) → the generated ctor; any other
    /// method through a class-name is a FACTORY invocation (§11.4) — a later slice.</summary>
    private BoundStatement OoBindClassInvoke(Core.InvokeStatementContext inv, OoClassSymbol cls, string method)
    {
        if (!string.Equals(method, "NEW", StringComparison.OrdinalIgnoreCase))
            return new BoundUnsupported($"INVOKE {cls.Name} \"{method}\" (a FACTORY/static method, ISO §11.4 "
                + "— the FACTORY slice)");
        if (inv.invokeUsing() is not null)
        {
            data.Edition.Error("COBOLNET0826",
                $"INVOKE {cls.Name} \"NEW\": the predefined NEW method takes no USING arguments "
                + "(ISO §16.2.1 — its only result is the new object reference)");
            return new BoundNop();
        }
        if (inv.invokeReturning()?.dataReference() is not { } retRef)
        {
            data.Edition.Error("COBOLNET0826",
                $"INVOKE {cls.Name} \"NEW\" without RETURNING — the created object would be lost; NEW's "
                + "result is delivered only through the RETURNING identifier (ISO §16.2.1/§14.9.23.4 GR8)");
            return new BoundNop();
        }
        if (refs.Resolve(retRef) is not { } ret)
            return new BoundUnsupported($"INVOKE … RETURNING '{retRef.GetText()}' (unresolvable receiver)");
        if (ret.Item.Pic is not { Category: PicCategory.ObjectReference } retPic)
        {
            data.Edition.Error("COBOLNET0826",
                $"INVOKE {cls.Name} \"NEW\" RETURNING '{retRef.GetText()}': the receiving item shall be a "
                + "USAGE OBJECT REFERENCE data item (ISO §14.9.23.4 GR8 / §14.8 conformance)");
            return new BoundNop();
        }
        // Receiver conformance (§14.8): universal accepts anything; a typed receiver accepts the class or a
        // subclass of its declared class (the created class must CONFORM TO the declared class).
        if (retPic.ObjectClassName is { } declared
            && (OoClasses?.Find(declared) is not { } declaredCls || !cls.ConformsTo(declaredCls)))
        {
            data.Edition.Error("COBOLNET0826",
                $"INVOKE {cls.Name} \"NEW\" RETURNING '{retRef.GetText()}': a {cls.Name} object does not "
                + $"conform to the receiver's declared class '{declared}' (ISO §14.8 — the sending object "
                + "shall be of the declared class or one of its subclasses)");
            return new BoundNop();
        }
        return new BoundInvoke(InvokeForm.New, cls.CsName, null, null, ret);
    }

    /// <summary><c>INVOKE identifier-1 "method" …</c>: virtual dispatch through a TYPED object reference; the
    /// method resolves over the declared class's hierarchy at COMPILE time (§14.9.23.3 SR4d — for the typed
    /// path a lookup failure is a compile-time diagnostic, the static analog of EC-OO-METHOD, GR7b).</summary>
    private BoundStatement OoBindInstanceInvoke(Core.InvokeStatementContext inv, Place receiver, string method)
    {
        if (receiver.Item.Pic is not { Category: PicCategory.ObjectReference } pic)
        {
            data.Edition.Error("COBOLNET0824",
                $"INVOKE '{receiver.Item.CobolName}': identifier-1 shall be a USAGE OBJECT REFERENCE data "
                + "item (ISO §14.9.23.3 SR3)");
            return new BoundNop();
        }
        if (pic.ObjectClassName is not { } className)
            return new BoundUnsupported("INVOKE through a UNIVERSAL object reference (reflection-free "
                + "__CobolInvoke dispatch, deep-dive D10 — the universal-reference wave)");
        if (OoClasses?.Find(className) is not { } cls)
        {
            // Unreachable when DataBinder validated the declared class (COBOLNET0813) — defensive, loud.
            data.Edition.Error("COBOLNET0813",
                $"INVOKE '{receiver.Item.CobolName}': its declared class '{className}' is not a class of the "
                + "compilation group (ISO §13.18.60.4)");
            return new BoundNop();
        }
        if (cls.FindMethod(method) is not { } m)
        {
            data.Edition.Error("COBOLNET0825",
                $"INVOKE '{receiver.Item.CobolName}' \"{method}\": class '{cls.Name}' (and its inheritance "
                + "chain) does not define a method named '" + method + "' (ISO §14.9.23.3 SR4d — compile-time "
                + "for a typed receiver; the runtime analog is EC-OO-METHOD, §14.9.23.4 GR7b)");
            return new BoundNop();
        }
        if (inv.invokeUsing() is not null || inv.invokeReturning() is not null || m.HasUsing || m.HasReturning)
            return new BoundUnsupported($"INVOKE \"{m.Name}\" with USING/RETURNING (typed parameter "
                + "marshaling, ISO §14.9.23.4 GR6/GR8 — OO port slice 2)");
        return new BoundInvoke(InvokeForm.Instance, null, receiver, m.CsName, null);
    }

    // ── Method-context control flow (deep-dive D8) ──────────────────────────────────────────────────────────

    /// <summary>GOBACK inside a METHOD (§14.9.18.4 GR4): terminate the METHOD, control back to the INVOKE
    /// site. The RETURNING-item delivery is the method entry's job (slice 2 — no formals yet); GOBACK's own
    /// phrases in a method context stage loud (RAISING → the EC-OO slice; the RETURNING/GIVING and 2023
    /// status phrases are activation-result forms that do not apply to a method return).</summary>
    private BoundStatement OoBindMethodGoback(Core.GobackStatementContext g)
    {
        if (g.dataReference() is not null || g.raisingPhrase() is not null)
            return new BoundUnsupported("GOBACK with a RETURNING/GIVING/RAISING phrase inside a method "
                + "(ISO §14.9.18.4 GR4 returns the METHOD's RETURNING item — port slice 2 / the EC-OO slice)");
        return new BoundMethodReturn();
    }

    /// <summary>EXIT METHOD (pre-2023 editions — REMOVED by 2023, Annex E.2; the <c>exit-method-window</c>
    /// registry row already flags 0900/0902 at the window edges): inside a method it is the method-return
    /// synonym (≡ the §14.9.18.4 GR4 GOBACK); outside one it violates its placement rule.</summary>
    private BoundStatement OoBindExitMethod(Core.ExitStatementContext e)
    {
        if (!InMethod)
        {
            data.Edition.Error("COBOLNET0827",
                "EXIT METHOD may be specified only in a method definition (ISO §14.9.14 — the method form "
                + "of the EXIT statement; this is not a method procedure division)");
            return new BoundNop();
        }
        if (e.raisingPhrase() is not null)
            return new BoundUnsupported("EXIT METHOD RAISING (exception propagation from a method — the "
                + "EC-OO slice; ISO §14.9.14)");
        return new BoundMethodReturn();
    }
}
