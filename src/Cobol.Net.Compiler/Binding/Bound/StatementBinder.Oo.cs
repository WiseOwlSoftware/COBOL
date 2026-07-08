// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Frontend.Generated;

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
    /// <summary><c>INVOKE SELF "M" …</c> → <c>this.M(…)</c> — VIRTUAL dispatch on the RUNTIME class
    /// (§8.4.3.8 GR2: a subclass override wins even when the SELF call sits in an inherited base method —
    /// oo_self_polymorphic). Never null — no guard (slice 3b).</summary>
    Self,
    /// <summary><c>INVOKE SUPER "M" …</c> → <c>base.M(…)</c> — NON-virtual, resolution STARTS at the base
    /// class (§8.4.3.8 GR3 restricted search), so an override calling its base cannot recurse (slice 3b).</summary>
    Super,
    /// <summary><c>INVOKE class-name "M" …</c> (non-NEW) → <c>CLS__FACTORY.__Instance.M(…)</c> — a FACTORY
    /// method through the class's factory singleton (§11.4/§9.3.6; brief D11 — never null, no guard;
    /// virtual, so an inherited factory override dispatches).</summary>
    Factory,
    /// <summary><c>INVOKE SELF|SUPER "NEW" RETURNING r</c> inside a FACTORY method → <c>r = this.__New()</c>
    /// (§16.2.1 GR1 ACTIVE-CLASS creation: the covariant per-class <c>__New</c> override makes an inherited
    /// factory MAKE create the RUNTIME factory's class — the canonical factory pattern).</summary>
    NewSelf,
}

/// <summary>A bound INVOKE (ISO §14.9.23; deep-dive D5): the RESOLVED call form plus everything the backend
/// needs to render it — no name lookup happens at emit time. <paramref name="ClassCsName"/> is the emitted C#
/// type (New form); <paramref name="Receiver"/>/<paramref name="MethodCsName"/> drive the Instance form (the
/// method name is the ROSTER's exact spelling — COBOL names compare case-insensitively, §8.3.2.2, and the
/// C# override chain must reuse one spelling — the legacy trap-#2 rule); <paramref name="Returning"/> receives
/// the invocation result (NEW's created object, or the method's <paramref name="ReturningSource"/> item per
/// §14.9.23.4 GR8); <paramref name="Args"/> carries the positionally-bound USING arguments (D6 — GR3).</summary>
public sealed record BoundInvoke(
    InvokeForm Form, string? ClassCsName, Place? Receiver, string? MethodCsName, Place? Returning,
    IReadOnlyList<BoundInvokeArg>? Args = null, DataItem? ReturningSource = null,
    string? OwnerCsName = null)
    : BoundStatement;

/// <summary>One bound INVOKE argument (deep-dive D6; §14.9.23.4 GR6): the FORMAL it corresponds to
/// positionally (its description drives the marshaling — §14.8.2's strict conformance was validated at bind,
/// so the crossing is type-preserving), the identifier source place OR the literal (decoded string / raw
/// numeric text), and whether the argument writes back (BY REFERENCE identifier — changes visible to the
/// caller; BY CONTENT and the §14.9.23.3 SR 10 object-data auto-CONTENT case do not).</summary>
public sealed record BoundInvokeArg(
    DataItem Formal, Place? Source, string? NumericLiteral, string? StringLiteral, bool WriteBack,
    bool ByContent = false);

/// <summary>A bound UNIVERSAL-receiver INVOKE (deep-dive D10/D-U5): there is NO formal roster at compile
/// time, so the bound facts differ in KIND from <see cref="BoundInvoke"/> — the method selector is a
/// bind-normalized literal OR a data-item Place read at runtime (§14.9.23.3 SR7), and every argument
/// carries its caller-side CONFORMANCE DESCRIPTOR (OoClassTable.ConformanceDescriptor — checked by the
/// callee's generated switch at runtime per §14.9.23.4 GR7c, mismatch → EC-OO-UNIVERSAL). Every argument
/// is BY REFERENCE (SR6 — implicit), so every argument writes back through its box.</summary>
public sealed record BoundInvokeUniversal(
    Place Receiver, string? MethodLiteral, Place? MethodSource,
    IReadOnlyList<BoundUniversalArg> Args, Place? Returning, string? ReturningDescriptor) : BoundStatement;

/// <summary>One universal-dispatch argument: the storage and its conformance descriptor (D-U3).</summary>
public sealed record BoundUniversalArg(Place Source, string Descriptor);

/// <summary>SET Format 5 — object-reference assignment (ISO §14.9.39 :31162; D-U7): copy ONE sender
/// reference into each target in order (GR9/GR10). The sender is a Place, the NULL figurative, SELF
/// (legal only inside a method; renders <c>this</c>), a class-name (SR13 — renders the D11 factory
/// singleton <c>{SourceFactoryCs}.__Instance</c>), or the EXCEPTION-OBJECT register (§8.4.3.6 — the
/// EC-OO wave; implicitly UNIVERSAL, so a TYPED target takes the generated runtime narrow check,
/// §9.3.8.2 :12291 → EC-OO-UNIVERSAL).</summary>
public sealed record BoundSetObjectRef(IReadOnlyList<Place> Targets, Place? Source, bool SourceIsNull, bool SourceIsSelf) : BoundStatement
{
    public string? SourceFactoryCs { get; init; }
    public bool FromExceptionObject { get; init; }
}

/// <summary>A method-context <c>GOBACK</c> / (pre-2023) <c>EXIT METHOD</c> (ISO §14.9.18.4 GR4; deep-dive D8 —
/// the one decision that silently miscompiles if missed): terminates the executing METHOD only, returning
/// control to the INVOKE site — never the run unit (<see cref="BoundStop"/>) and never the program activation
/// (<see cref="BoundGoback"/>). Rendered as <c>throw new MethodReturn()</c>, caught at the method's public
/// entry (the ProgramReturn-pattern realization of D8: a plain <c>return</c> cannot unwind the nested bounded
/// <c>__Dispatch</c> frames an out-of-line PERFORM stacks).</summary>
public sealed record BoundMethodReturn(BoundRaising? Raising = null) : BoundStatement;
// Raising: GOBACK/EXIT METHOD … RAISING from a method (§14.9.18.4 GR1b; the EC-OO wave) — STAGED before
// the MethodReturn throw; the INVOKE site's pickup applies the §14.6.13.1.5 activator rules AFTER the
// RETURNING delivery + copy-outs (GR1b's result-before-exception ordering falls out of the throw/catch).

public sealed partial class StatementBinder
{
    /// <summary>The group's pass-1 class symbol table (deep-dive D1) — set by the run-unit emitter before
    /// binding so INVOKE resolves classes/methods defined anywhere in the group. Null ⇔ empty group.</summary>
    public OoClassTable? OoClasses { get; set; }

    /// <summary>The CLASS whose method bodies this binder is binding (set by the emitter's OoBindClassBody;
    /// null in a program unit) — the SELF/SUPER resolution root (§8.4.3.8: SELF resolves on the current
    /// class's chain, SUPER starts at its BASE; slice 3b).</summary>
    public OoClassSymbol? OoCurrentClass { get; set; }

    /// <summary>True while binding the FACTORY roster (§11.4): SELF/SUPER resolve over the FACTORY interface
    /// (§14.9.23.3 SR4f/h) and SELF|SUPER "NEW" binds the ACTIVE-CLASS creation form (§16.2.1).</summary>
    public bool OoInFactory { get; set; }

    /// <summary>The paragraph scope of ONE method body (ISO §11.7 — the legacy per-method scope algorithm,
    /// ported): paragraph and section names declare METHOD-LOCALLY; PERFORM/GO TO inside the method resolve
    /// against THESE maps only, so sibling methods may reuse names and a cross-method transfer fails loud
    /// (the legacy traps #4/#5/#10 are structural here, not checks).</summary>
    private sealed class OoMethodScope
    {
        public readonly Dictionary<string, int> Paras = new(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, SectionInfo> Sections = new(StringComparer.OrdinalIgnoreCase);
        /// <summary>The method's DATA name scope (§11.7 GR5) — activated on <c>DataBinder.ActiveMethodScope</c>
        /// while this method's statements bind (slice 2).</summary>
        public OoMethodDataScope? Data;
    }

    private OoMethodScope? _currentMethodScope;               // ambient during method collection AND binding
    private readonly List<OoMethodScope?> _paraMethod = [];   // per-pc owning method (parallel to _paras)

    /// <summary>True while binding a statement inside a METHOD body — the D8 context switch (GOBACK →
    /// method return; EXIT PROGRAM → §14.9.14.3 SR7 violation).</summary>
    private bool InMethod => _currentMethodScope is not null;

    /// <summary>Drain THIS statement's pending object-property ops (registered by the ReferenceResolver
    /// fallback while the statement bound) into the §8.4.3.9.4 GR1–GR3 desugar: classify each temp's store
    /// polarity over the BOUND statement (BoundStores — the emitter-verified taxonomy), then
    /// GR1 (pure sending) = prepend the get-invoke; GR2 (write-only receiving) = append the set-invoke, get
    /// NOT invoked; GR3 (read-modify-write) = both around ONE temp. SR3/SR4 (:7380/:7382 — the needed
    /// accessor must exist, on the instance or factory roster per the reference form) check HERE, against
    /// the classified need, both COBOLNET0843. An unclassifiable statement (a taxonomy hole) stages LOUD —
    /// never a silent guess about whether a side-effecting accessor runs.</summary>
    internal BoundStatement OoWrapPropertyOps(BoundStatement core, int mark)
    {
        var ops = data.OoPendingPropertyOps;
        if (ops.Count <= mark) return core;
        var taken = ops.GetRange(mark, ops.Count - mark);
        ops.RemoveRange(mark, ops.Count - mark);

        List<BoundStatement> pre = [], post = [];
        foreach (var op in taken)
        {
            var kind = BoundStores.StoreKindOf(core, op.Temp);
            if (kind is null)
            {
                data.Edition.Error("COBOLNET0843",
                    $"the object-property reference '{op.PropName}' OF '{op.ReceiverName}' occurs in a "
                    + $"statement ({core.GetType().Name}) outside the classified store taxonomy — the "
                    + "sending/receiving polarity (ISO §8.4.3.9.4 GR1–GR3) cannot be established; extend "
                    + "BoundStores before accepting this shape");
                continue;
            }
            bool needGet = kind == StoreKind.None || kind == StoreKind.ReadWrite;
            bool needSet = kind == StoreKind.Write || kind == StoreKind.ReadWrite;
            string where = $"'{op.PropName}' OF '{op.ReceiverName}'";
            var form = op.Factory ? InvokeForm.Factory : InvokeForm.Instance;
            var tempPlace = refs.ResolveItem(op.Temp)!;

            if (needGet)
            {
                if (op.Get is null)
                    data.Edition.Error("COBOLNET0843",
                        $"the object-property reference {where} is a SENDING operand but the class has no "
                        + "GET property method (ISO §8.4.3.9.3 SR3 — WITH NO GET, or no accessor defined)");
                else
                    pre.Add(new BoundInvoke(form, op.ClassCsName, op.Receiver, op.Get.CsName, tempPlace,
                        null, op.Get.Returning, op.Get.Owner?.CsName));
            }
            if (needSet)
            {
                if (op.Set is null)
                    data.Edition.Error("COBOLNET0843",
                        $"the object-property reference {where} is a RECEIVING operand but the class has no "
                        + "SET property method (ISO §8.4.3.9.3 SR4 — WITH NO SET, or no accessor defined)");
                else
                    post.Add(new BoundInvoke(form, op.ClassCsName, op.Receiver, op.Set.CsName, null,
                        [new BoundInvokeArg(op.Set.Formals[0].Item, tempPlace, null, null, WriteBack: false)],
                        null, op.Set.Owner?.CsName));
            }
        }
        return pre.Count + post.Count == 0 ? core : new BoundSequence([.. pre, core, .. post]);
    }

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
    public BoundProgram BindMethodRoster(OoClassSymbol cls, IReadOnlyList<OoMethodSymbol> roster)
    {
        var used = new HashSet<string>(StringComparer.Ordinal);
        var methods = new List<BoundMethod>(roster.Count);

        foreach (var m in roster)
        {
            if (m.PropertySubject is not null)
            {
                // A PROPERTY-clause-SYNTHESIZED accessor: no COBOL body exists — the emitter renders the
                // direct field read/write (D-P1; observably identical to the §13.18.42 GR1/GR2 implicit
                // MOVE methods). It still occupies a roster slot (override/implements machinery applies).
                m.EntryPc = _paras.Count;
                m.EndPc = _paras.Count - 1;   // empty body by construction
                methods.Add(new BoundMethod(m.Name, m.CsName, m.EntryPc, m.EndPc));
                continue;
            }
            // A method IS a source element (§14.9.18.3 SR2/SR4a): its OWN PD-header RAISING partition
            // (D-EO8) becomes the binder's per-element sets while its body binds.
            EcLoadPdRaising(m.RaisingEcNames, m.RaisingClasses);
            // The method's DATA (LINKAGE → params-as-locals, LOCAL-STORAGE → locals, method-WS → statics) was
            // bound by DataBinder.OoBindMethodData before any body binds; here we link its name scope so the
            // per-pc switch below activates §11.7 GR5 shadowing while this method's statements bind.
            var scope = new OoMethodScope { Data = m.DataScope };
            _currentMethodScope = scope;
            m.EntryPc = _paras.Count;
            if (m.Ctx.procedureDivision() is { } pd)
            {
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
            data.ActiveMethodScope = _currentMethodScope?.Data;   // §11.7 GR5 data shadowing (slice 2)
            _currentBindPc = i;
            var sentences = new List<IReadOnlyList<BoundStatement>>();
            foreach (var sentence in _paras[i].Sentences)
                sentences.Add(sentence.statement().Select(BindStatement).ToList());
            bound.Add(new BoundParagraph(_paras[i].Cobol, sentences));
        }
        _currentSection = null;
        _currentMethodScope = null;
        data.ActiveMethodScope = null;
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
        // INVOKE was introduced by ISO/IEC 1989:2002 (§14.9.23, OO). Bind-time introduction gate (rearch
        // bind-time migration Cluster 3 — the parse-time {is2002()}? predicate is gone).
        ConstructRegistry.Check(data.Edition.Edition, data.Edition, Constructs.Invoke2002, "the INVOKE statement");
        var target = inv.invokeTarget().objectReference();

        // The method selector: an alphanumeric/national literal binds statically (§14.9.23.3 SR2);
        // identifier-2 (a method name held in a data item) is legal ONLY through a UNIVERSAL receiver
        // (§14.9.23.3 SR7) — the D10 dynamic path, live as of the universal wave.
        if (inv.invokeMethodName().dataReference() is { } mref)
        {
            if (target.dataReference() is not { } uref || refs.Resolve(uref) is not { } urecv
                || urecv.Item.Pic is not { Category: PicCategory.ObjectReference, ObjectClassName: null })
            {
                data.Edition.Error("COBOLNET0866",
                    "INVOKE: identifier-2 (a method name held in a data item) is permitted only when "
                    + "identifier-1 is a UNIVERSAL object reference (ISO §14.9.23.3 SR7)");
                return new BoundNop();
            }
            if (refs.Resolve(mref) is not { } msrc)
            {
                data.Edition.Error("COBOLNET0866",
                    $"INVOKE: the method-name identifier '{mref.GetText()}' is not resolvable to storage");
                return new BoundNop();
            }
            if (msrc.Item.Pic?.Category is not PicCategory.Alphanumeric && !msrc.Item.IsGroup)
            {
                data.Edition.Error("COBOLNET0866",
                    $"INVOKE: identifier-2 ('{mref.GetText()}') shall be of class alphanumeric "
                    + "(ISO §14.9.23.3 SR8; national identifier-2 is a later refinement)");
                return new BoundNop();
            }
            return OoBindUniversalInvoke(inv, urecv, methodLiteral: null, methodSource: msrc);
        }
        string? methodName = OoDecodeMethodNameLiteral(inv.invokeMethodName().literal());
        if (methodName is null)
        {
            data.Edition.Error("COBOLNET0823",
                "INVOKE: literal-1 (the method name) shall be of class alphanumeric or national "
                + "(ISO §14.9.23.3 SR2)");
            return new BoundNop();
        }
        if (methodName.Length == 0)
        {
            data.Edition.Error("COBOLNET0823",
                "INVOKE: literal-1 shall not be a zero-length literal (ISO §14.9.23.3 SR2)");
            return new BoundNop();
        }

        if (target.SELF() is not null || target.SUPER() is not null)
        {
            // Slice 3b — §8.4.3.8: SELF/SUPER are the predefined object references of the CURRENT method's
            // object; legal only within a method body.
            bool isSuper = target.SUPER() is not null;
            if (!InMethod || OoCurrentClass is not { } cur)
            {
                data.Edition.Error("COBOLNET0827",
                    $"INVOKE {(isSuper ? "SUPER" : "SELF")} may be specified only within a method definition "
                    + "(ISO §8.4.3.8 — the predefined object references of the current object)");
                return new BoundNop();
            }
            // In a FACTORY method, SELF|SUPER "NEW" is the ACTIVE-CLASS creation (§16.2.1 GR1 — the
            // BaseFactoryInterface's New): bind InvokeForm.NewSelf → `this.__New()` (covariant per class;
            // SUPER restricts the METHOD SEARCH, GR3, but the found method IS the predefined New whose
            // behavior is active-class creation on the SAME runtime factory — the equivalence is deliberate).
            if (OoInFactory && string.Equals(methodName, "NEW", StringComparison.OrdinalIgnoreCase))
            {
                if (inv.invokeUsing() is not null)
                {
                    data.Edition.Error("COBOLNET0826",
                        "INVOKE SELF/SUPER \"NEW\": the predefined NEW method takes no USING arguments "
                        + "(ISO §16.2.1)");
                    return new BoundNop();
                }
                if (inv.invokeReturning()?.dataReference() is not { } nrRef)
                {
                    data.Edition.Error("COBOLNET0826",
                        "INVOKE SELF/SUPER \"NEW\" without RETURNING — the created object would be lost "
                        + "(ISO §16.2.1/§14.9.23.4 GR8)");
                    return new BoundNop();
                }
                if (refs.Resolve(nrRef) is not { } nret)
                    return new BoundUnsupported($"INVOKE … RETURNING '{nrRef.GetText()}' (unresolvable receiver)");
                if (nret.Item.Pic is not { Category: PicCategory.ObjectReference } nrp)
                {
                    data.Edition.Error("COBOLNET0826",
                        $"INVOKE SELF/SUPER \"NEW\" RETURNING '{nrRef.GetText()}': the receiving item shall "
                        + "be a USAGE OBJECT REFERENCE data item (ISO §14.9.23.4 GR8)");
                    return new BoundNop();
                }
                // The runtime class is the CONTAINING class or a subclass — the containing class's
                // conformance is the strongest compile-time guarantee (§14.8 — a subclass instance still
                // conforms downstream of anything the containing class conforms to).
                if (OoClasses?.ObjectRefWideningMismatch(PicInfo.ObjectReferenceItem(cur.Name), nrp) is { } nwerr)
                {
                    data.Edition.Error("COBOLNET0826",
                        $"INVOKE SELF/SUPER \"NEW\" RETURNING '{nrRef.GetText()}': {nwerr} (ISO §14.8)");
                    return new BoundNop();
                }
                return new BoundInvoke(InvokeForm.NewSelf, cur.CsName, null, null, nret);
            }
            OoClassSymbol searchRoot;
            if (!isSuper)
                searchRoot = cur;   // GR2 — resolve on the current class's chain; dispatch on the RUNTIME class
            else if (cur.Base is { } b)
                searchRoot = b;     // GR3 — the restricted search STARTS at the base class
            else
            {
                // Trap #7 — SUPER in a root class is a clean compile diagnostic, never an internal error
                // (applies identically to the FACTORY flavor).
                data.Edition.Error("COBOLNET0827",
                    $"INVOKE SUPER in class '{cur.Name}', which INHERITS from no class (ISO §8.4.3.8 — SUPER "
                    + "references the inherited class's methods)");
                return new BoundNop();
            }
            // Roster selection by CONTEXT (§14.9.23.3 SR4f/g/h/i): a factory method's SELF/SUPER resolve
            // over the FACTORY interface; an instance method's over the instance interface.
            var sm = OoInFactory ? searchRoot.FindFactoryMethod(methodName) : searchRoot.FindMethod(methodName);
            if (sm is null)
            {
                data.Edition.Error("COBOLNET0825",
                    $"INVOKE {(isSuper ? "SUPER" : "SELF")} \"{methodName}\": class '{searchRoot.Name}' (and "
                    + $"its inheritance chain) does not define a{(OoInFactory ? " factory" : "n instance")} "
                    + "method named '" + methodName + "' "
                    + "(ISO §14.9.23.3 SR4f–SR4i — the SELF/SUPER method-name placement rules)");
                return new BoundNop();
            }
            return OoBindResolvedInvoke(inv, sm, isSuper ? InvokeForm.Super : InvokeForm.Self, null);
        }
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
        {
            // §14.9.23.3 SR3: literal-1 names a method of the FACTORY interface of class-name-1 — resolution
            // walks the INHERITS chain over the factory rosters (§9.3.6); the lookup failure is the
            // compile-time analog of EC-OO-METHOD (GR7b).
            if (cls.FindFactoryMethod(method) is { } fm)
            {
                var bound = OoBindResolvedInvoke(inv, fm, InvokeForm.Factory, null);
                return bound is BoundInvoke bi ? bi with { ClassCsName = cls.CsName } : bound;
            }
            data.Edition.Error("COBOLNET0825",
                $"INVOKE {cls.Name} \"{method}\": class '{cls.Name}' (and its inheritance chain) does not "
                + "define a FACTORY method named '" + method + "' (ISO §14.9.23.3 SR3 — literal-1 shall name "
                + "a method of the factory interface; the runtime analog is EC-OO-METHOD, §14.9.23.4 GR7b)");
            return new BoundNop();
        }
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
        // Receiver conformance (§14.8 via the SET/widening direction): universal accepts anything; a typed
        // receiver accepts the class, a subclass, or — for an INTERFACE-typed receiver — any class whose
        // §11.8.4 closure implements it (the ONE ObjectRefWideningMismatch rule).
        if (OoClasses?.ObjectRefWideningMismatch(PicInfo.ObjectReferenceItem(cls.Name), retPic) is { } werr)
        {
            data.Edition.Error("COBOLNET0826",
                $"INVOKE {cls.Name} \"NEW\" RETURNING '{retRef.GetText()}': {werr} (ISO §14.8)");
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
            // A UNIVERSAL receiver with a literal selector (SR4 permits literal-1; it still cannot bind
            // statically — no roster exists at compile time): the D10 dynamic path.
            return OoBindUniversalInvoke(inv, receiver, methodLiteral: method, methodSource: null);
        // An INTERFACE-typed receiver: resolution over the interface's prototype closure (§14.9.23.3 SR4e);
        // the emitted call is static C# interface dispatch behind the same GR5 null guard.
        if (OoClasses?.FindInterface(className) is { } recvIface)
        {
            var proto = recvIface.AllPrototypes()
                .FirstOrDefault(pm => string.Equals(pm.Name, method, StringComparison.OrdinalIgnoreCase));
            if (proto is null)
            {
                data.Edition.Error("COBOLNET0825",
                    $"INVOKE '{receiver.Item.CobolName}' \"{method}\": interface '{recvIface.Name}' (and "
                    + "its INHERITS closure) does not declare a method named '" + method + "' "
                    + "(ISO §14.9.23.3 SR4e)");
                return new BoundNop();
            }
            var ibound = OoBindResolvedInvoke(inv, proto, InvokeForm.Instance, receiver);
            return ibound is BoundInvoke ibi ? ibi with { OwnerCsName = recvIface.CsName } : ibound;
        }
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
            string hint = cls.FindFactoryMethod(method) is not null
                ? $" ('{method}' IS a FACTORY method of class '{cls.Name}' — invoke it through the "
                  + "class-name: an instance receiver resolves the INSTANCE interface, §14.9.23.3 SR4b)"
                : "";
            data.Edition.Error("COBOLNET0825",
                $"INVOKE '{receiver.Item.CobolName}' \"{method}\": class '{cls.Name}' (and its inheritance "
                + "chain) does not define a method named '" + method + "' (ISO §14.9.23.3 SR4d — compile-time "
                + $"for a typed receiver; the runtime analog is EC-OO-METHOD, §14.9.23.4 GR7b){hint}");
            return new BoundNop();
        }
        return OoBindResolvedInvoke(inv, m, InvokeForm.Instance, receiver);
    }

    /// <summary>The shared USING + RETURNING binding tail for a RESOLVED method — the Instance / SELF / SUPER
    /// forms differ only in receiver resolution and dispatch rendering (§8.4.3.8), never in marshaling.</summary>
    private BoundStatement OoBindResolvedInvoke(
        Core.InvokeStatementContext inv, OoMethodSymbol m, InvokeForm form, Place? receiver)
    {
        // ── USING marshaling (slice 2 — D6; §14.9.23.4 GR3: positional correspondence) ──
        var argCtxs = inv.invokeUsing()?.invokeArgument() ?? [];
        if (argCtxs.Length != m.Formals.Count)
        {
            // The trap-#3 rule: an arity mismatch is LOUD — a silently dropped/extra argument would shift
            // every following slot (the legacy DEVLOG-449 blocker: the first USING bound to the RETURNING).
            data.Edition.Error("COBOLNET0828",
                $"INVOKE \"{m.Name}\": {argCtxs.Length} USING argument(s) for {m.Formals.Count} formal "
                + $"parameter(s) of the method (ISO §14.9.23.4 GR3 — correspondence is positional; "
                + "trailing-OMITTED support is a later slice)");
            return new BoundNop();
        }
        var args = new List<BoundInvokeArg>(argCtxs.Length);
        for (int i = 0; i < argCtxs.Length; i++)
        {
            if (OoBindInvokeArg(argCtxs[i], m.Formals[i].Item, m.Name) is not { } a) return new BoundNop();
            args.Add(a);
        }

        // ── RETURNING pairing + conformance (GR8; §14.8.3; the deep-dive signature-check edge case:
        // BOTH mismatch directions are compile-time diagnostics) ──
        var retRef = inv.invokeReturning()?.dataReference();
        Place? retPlace = null;
        if (retRef is not null && m.Returning is null)
        {
            data.Edition.Error("COBOLNET0828",
                $"INVOKE \"{m.Name}\" RETURNING: the method declares no RETURNING item (ISO §14.9.23.4 GR8 / "
                + "§14.8.3 — nothing to deliver)");
            return new BoundNop();
        }
        if (retRef is null && m.Returning is not null)
        {
            data.Edition.Error("COBOLNET0828",
                $"INVOKE \"{m.Name}\": the method declares a RETURNING item ('{m.Returning.CobolName}') — "
                + "the INVOKE must specify RETURNING to receive it (the binder's signature check, deep-dive "
                + "D1; ISO §14.9.23.4 GR8)");
            return new BoundNop();
        }
        if (retRef is not null)
        {
            if (refs.Resolve(retRef) is not { } rp)
            {
                data.Edition.Error("COBOLNET0828",
                    $"INVOKE \"{m.Name}\" RETURNING '{retRef.GetText()}': the receiving identifier is not "
                    + "resolvable to storage");
                return new BoundNop();
            }
            // §14.8.3.3 rule 1: the RETURNING delivery conforms "as if a SET statement were performed" —
            // for object references that is the WIDENING direction (universal receiver accepts anything; a
            // typed receiver accepts the same class or a subclass — SET SR12a2), NOT the §14.8.2.3.2
            // identity rule. Everything else keeps the strict description check.
            string? rerr = m.Returning!.Pic is { Category: PicCategory.ObjectReference } sendPic
                    && rp.Item.Pic is { Category: PicCategory.ObjectReference } recvPic
                ? OoClasses?.ObjectRefWideningMismatch(sendPic, recvPic)
                : OoConformanceError(m.Returning!, rp.Item);
            if (rerr is not null)
            {
                data.Edition.Error("COBOLNET0828",
                    $"INVOKE \"{m.Name}\" RETURNING '{retRef.GetText()}': {rerr} (ISO §14.8.3.3 "
                    + "returning-item conformance)");
                return new BoundNop();
            }
            retPlace = rp;
        }
        return new BoundInvoke(form, null, receiver, m.CsName, retPlace, args, m.Returning, m.Owner?.CsName);
    }

    /// <summary>Bind ONE INVOKE argument against its positional formal — the conformance RULE is selected
    /// by the EFFECTIVE passing mode (§14.9.23.4 GR6): BY REFERENCE takes §14.8.2.3.2 strict identity (with
    /// the §14.8.2.2 rule-1 group-prefix allowance); BY CONTENT — explicit, the §14.9.23.3 SR 10 object-data
    /// auto-CONTENT, and every literal — takes §14.8.2.3.3: COMPUTE rules for a numeric formal (any numeric
    /// argument), SET rules for an object-reference formal (widening), MOVE rules otherwise. A
    /// reference-modified argument conforms by its EFFECTIVE description (a unique elementary alphanumeric
    /// item of the window length, §8.4.2.4). Null on a diagnostic.</summary>
    private BoundInvokeArg? OoBindInvokeArg(Core.InvokeArgumentContext arg, DataItem formal, string methodName)
    {
        void Err(string msg) => data.Edition.Error("COBOLNET0828", $"INVOKE \"{methodName}\": {msg}");

        if (arg.VALUE() is not null)
        {
            // SR5b: a BY VALUE argument requires a BY VALUE formal; every formal is BY REFERENCE today (the
            // procedure-division-header BY phrases are an unparsed grammar extension — added with them).
            Err($"BY VALUE argument for formal '{formal.CobolName}': the corresponding formal parameter is "
                + "BY REFERENCE (ISO §14.9.23.3 SR5b; header BY VALUE formals are a later slice)");
            return null;
        }

        bool explicitReference = arg.REFERENCE() is not null;
        bool explicitContent = arg.CONTENT() is not null;

        if (arg.dataReference() is { } dref)
        {
            if (refs.Resolve(dref) is not { } place)
            {
                Err($"USING argument '{dref.GetText()}' is not resolvable to storage (or uses a reference "
                    + "form not yet carried across INVOKE)");
                return null;
            }
            // §14.9.23.3 SR 10: object data (factory/instance WS) cannot cross BY REFERENCE — explicit
            // BY REFERENCE violates the rule; a BARE object-data identifier is assumed BY CONTENT (GR6a2).
            bool objectData = data.OoIsObjectData(place.Item);
            if (explicitReference && objectData)
            {
                Err($"BY REFERENCE argument '{dref.GetText()}' references OBJECT data — factory/instance "
                    + "working-storage may not cross an INVOKE by reference (ISO §14.9.23.3 SR 10); pass it "
                    + "BY CONTENT");
                return null;
            }
            bool byReference = !explicitContent && !objectData;   // GR6a — REFERENCE assumed when SR9/10 hold

            // A reference-modified operand is a unique ELEMENTARY ALPHANUMERIC item of the window length
            // (§8.4.2.4): conformance goes against that effective description, never the whole inner item.
            if (place is RefModPlace rmp)
            {
                if (formal.IsGroup || formal.Pic?.Category is not PicCategory.Alphanumeric)
                {
                    Err($"reference-modified argument '{dref.GetText()}': the operand is elementary "
                        + $"alphanumeric (§8.4.2.4) and does not conform to formal '{formal.CobolName}'");
                    return null;
                }
                if (byReference)
                {
                    // Strict identity needs a PROVABLE window length equal to the formal's.
                    if (!int.TryParse(rmp.Start, out _) || rmp.Length is null
                        || !int.TryParse(rmp.Length, out int rlen))
                    {
                        Err($"BY REFERENCE reference-modified argument '{dref.GetText()}' needs a "
                            + "compile-time (start:length) to prove §14.8.2.3.2 conformance — pass it "
                            + "BY CONTENT or use literal subscripts");
                        return null;
                    }
                    if (rlen != formal.Pic.Length)
                    {
                        Err($"reference-modified argument window ({rlen}) does not match formal "
                            + $"'{formal.CobolName}' X({formal.Pic.Length}) (ISO §14.8.2.3.2)");
                        return null;
                    }
                }
                return new BoundInvokeArg(formal, place, null, null,
                    WriteBack: byReference, ByContent: !byReference);
            }

            if (byReference)
            {
                if (OoClassTable.DescriptionMismatch(formal, place.Item, byRefGroupPrefix: true) is { } err1)
                {
                    Err($"USING argument '{dref.GetText()}' does not conform to formal parameter "
                        + $"'{formal.CobolName}': {err1} (ISO §14.8.2.3.2 — BY REFERENCE requires the "
                        + "identical description)");
                    return null;
                }
                return new BoundInvokeArg(formal, place, null, null, WriteBack: true);
            }

            // Effective BY CONTENT (§14.8.2.3.3): rule-per-formal-category.
            if (OoContentMismatch(formal, place.Item) is { } cerr)
            {
                Err($"BY CONTENT argument '{dref.GetText()}' does not conform to formal "
                    + $"'{formal.CobolName}': {cerr} (ISO §14.8.2.3.3)");
                return null;
            }
            return new BoundInvokeArg(formal, place, null, null, WriteBack: false, ByContent: true);
        }

        // A literal argument — BY CONTENT (GR6a2; a literal never meets SR9). Per §9.3.6 resolution rule 5
        // a literal that would TRUNCATE still conforms (the SET/MOVE no-truncation requirements are ignored
        // for literal arguments), so length/digit overflow converts per MOVE rules rather than erroring.
        var lit = arg.literal();
        if (lit?.nonNumericLiteral()?.STRINGLIT() is { } sl)
        {
            string txt = DecodeCobolString(sl.GetText());
            if (formal.IsGroup || formal.Pic?.Category is PicCategory.Alphanumeric)
                return new BoundInvokeArg(formal, null, null, txt, WriteBack: false, ByContent: true);
            Err($"nonnumeric literal argument {sl.GetText()} for the non-alphanumeric formal "
                + $"'{formal.CobolName}' (ISO §14.8.2.3.3 MOVE-rule conformance)");
            return null;
        }
        if (lit?.numericLiteral() is { } nl)
        {
            string raw = nl.GetText();
            if (formal.Pic is { Category: PicCategory.Numeric, IsFloat: false })
                return new BoundInvokeArg(formal, null, raw, null, WriteBack: false, ByContent: true);
            if (!formal.IsGroup && formal.Pic?.Category is PicCategory.Alphanumeric
                && !raw.Contains('.') && !raw.StartsWith('-') && !raw.StartsWith('+'))
                // MOVE rules: an unsigned integer numeric literal moves to an alphanumeric receiver as its
                // digit characters (§14.9.25).
                return new BoundInvokeArg(formal, null, null, raw, WriteBack: false, ByContent: true);
            Err($"numeric literal argument {raw} for formal '{formal.CobolName}' "
                + "(ISO §14.8.2.3.3 — no conforming COMPUTE/MOVE rule applies)");
            return null;
        }
        Err($"USING argument form for formal '{formal.CobolName}' is not yet carried across INVOKE");
        return null;
    }

    /// <summary>§14.8.2.3.3 — the BY CONTENT conformance rules per formal category: COMPUTE for numeric
    /// (any fixed-point numeric argument; float formals require the identical float usage — the cross-float
    /// CONTENT conversion is a documented later refinement), SET for object references (widening — the
    /// argument's class shall be the receiver's class or a subclass), MOVE otherwise (alphanumeric/group
    /// formals take any alphanumeric/group/integer-display argument, pad/truncate per MOVE).</summary>
    private string? OoContentMismatch(DataItem formal, DataItem arg)
    {
        if (formal.IsGroup || formal.Pic?.Category is PicCategory.Alphanumeric)
        {
            if (arg.IsGroup)
                return arg.IsImageCapable ? null : "the argument group has no character image (Tier-C)";
            return arg.Pic?.Category switch
            {
                PicCategory.Alphanumeric or PicCategory.NumericEdited => null,
                // Table 16: boolean→alphanumeric is a conforming MOVE; national→alphanumeric is NOT
                // (§14.9.25.3 — DISPLAY-OF is the sanctioned narrowing), so National keeps the mismatch arm.
                PicCategory.Boolean => null,
                PicCategory.Numeric when arg.Pic is { IsFloat: false, Scale: 0 } => null,   // MOVE integer→alnum
                _ => "no conforming MOVE rule applies (ISO §14.8.2.2 rule 2 / §14.9.25)",
            };
        }
        var f = formal.Pic!;
        return f.Category switch
        {
            PicCategory.Numeric when f.IsFloat =>
                arg.Pic is { IsFloat: true } a2 && a2.Usage == f.Usage
                    ? null
                    : "a float formal takes the identical float usage BY CONTENT (cross-float COMPUTE "
                      + "conversion is a later refinement)",
            PicCategory.Numeric =>
                arg.IsGroup ? "a group argument does not conform to a numeric formal (§14.8.2.3.3)"
                : arg.Pic is { Category: PicCategory.Numeric, IsFloat: false } ? null
                : "COMPUTE-rule conformance needs a numeric argument (ISO §14.8.2.3.3 rule 2a)",
            PicCategory.ObjectReference =>
                arg.Pic is { Category: PicCategory.ObjectReference } ap
                    ? OoClasses?.ObjectRefWideningMismatch(ap, f)
                    : "an object-reference formal takes an object-reference argument (SET rules, §14.8.2.3.3)",
            _ => OoClassTable.DescriptionMismatch(formal, arg),   // edited/other: conservative strict gate
        };
    }

    /// <summary>Decode INVOKE's literal-1 (§14.9.23.3 SR2 — class alphanumeric or national): an alphanumeric
    /// STRINGLIT, a national N"…" literal (the method NAME is its character value — §8.3.2.2 comparison), or
    /// a hex X"…" literal (byte pairs decoded through the alphanumeric runtime encoding). Null for a literal
    /// class SR2 excludes (boolean B"…", figurative constants) — the caller diagnoses.</summary>
    /// <summary>Bind an INVOKE through a UNIVERSAL receiver (D10/D-U5; §14.9.23.4 GR7c): no compile-time
    /// conformance — each argument and the RETURNING item carry their CONFORMANCE DESCRIPTOR for the
    /// callee's runtime check (§9.3.8.2.1 NOTE). Argument rules, all COBOLNET0866 with citations: explicit
    /// BY CONTENT/BY VALUE are forbidden (SR6 :28435 — BY REFERENCE is assumed implicitly); a literal or
    /// arithmetic-expression argument cannot cross by reference (SR6 + GR6's non-universal-only scope);
    /// OBJECT data may not cross at all (SR10 bans by-reference and SR6 removes the typed path's GR6a2
    /// auto-CONTENT fallback); a Tier-C group (no character image) has no crossing form.</summary>
    private BoundStatement OoBindUniversalInvoke(
        Core.InvokeStatementContext inv, Place receiver, string? methodLiteral, Place? methodSource)
    {
        var argCtxs = inv.invokeUsing()?.invokeArgument() ?? [];
        var args = new List<BoundUniversalArg>(argCtxs.Length);
        foreach (var a in argCtxs)
        {
            if (a.VALUE() is not null || a.CONTENT() is not null)
            {
                data.Edition.Error("COBOLNET0866",
                    "INVOKE through a universal object reference: neither BY CONTENT nor BY VALUE may be "
                    + "specified — BY REFERENCE is assumed implicitly (ISO §14.9.23.3 SR6)");
                return new BoundNop();
            }
            if (a.dataReference() is not { } dref)
            {
                data.Edition.Error("COBOLNET0866",
                    "INVOKE through a universal object reference: a literal or arithmetic-expression "
                    + "argument cannot cross BY REFERENCE (ISO §14.9.23.3 SR6 — every universal argument "
                    + "is implicitly BY REFERENCE)");
                return new BoundNop();
            }
            if (refs.Resolve(dref) is not { } p)
            {
                data.Edition.Error("COBOLNET0866",
                    $"INVOKE: the argument '{dref.GetText()}' is not resolvable to storage");
                return new BoundNop();
            }
            if (data.OoIsObjectData(p.Item))
            {
                data.Edition.Error("COBOLNET0866",
                    $"INVOKE through a universal object reference: '{p.Item.CobolName}' is OBJECT "
                    + "(factory/instance) data — it may not cross BY REFERENCE (ISO §14.9.23.3 SR10), and "
                    + "the universal path has no BY CONTENT fallback (SR6)");
                return new BoundNop();
            }
            string d = OoClassTable.ConformanceDescriptor(p.Item);
            if (d == "T:!")
            {
                data.Edition.Error("COBOLNET0866",
                    $"INVOKE: the argument '{p.Item.CobolName}' has no crossing form (a Tier-C group or a "
                    + "not-yet-carried category — mirrors the typed path's rejection)");
                return new BoundNop();
            }
            args.Add(new BoundUniversalArg(p, d));
        }

        Place? retPlace = null;
        string? retDesc = null;
        if (inv.invokeReturning()?.dataReference() is { } retRef)
        {
            if (refs.Resolve(retRef) is not { } rp)
            {
                data.Edition.Error("COBOLNET0866",
                    $"INVOKE RETURNING '{retRef.GetText()}': the receiving identifier is not resolvable "
                    + "to storage");
                return new BoundNop();
            }
            retDesc = OoClassTable.ConformanceDescriptor(rp.Item);
            if (retDesc == "T:!")
            {
                data.Edition.Error("COBOLNET0866",
                    $"INVOKE RETURNING '{rp.Item.CobolName}': no crossing form (Tier-C / not-carried)");
                return new BoundNop();
            }
            retPlace = rp;
        }
        // GR2a/§8.3.2.2: the selector is a user-defined word — normalize the LITERAL at bind time (the
        // identifier-2 value normalizes at runtime via CobolObject.NormalizeMethodName).
        return new BoundInvokeUniversal(receiver, methodLiteral?.TrimEnd().ToUpperInvariant(), methodSource,
            args, retPlace, retDesc);
    }

    /// <summary>SET Format 5 core (§14.9.39; D-U7) — shared by the grammar's NULL/SELF/SUPER-sender rule
    /// and BindSetTo's SEMANTIC re-route (a dataReference sender parses as the Format-1 shape). Rules, all
    /// COBOLNET0867: every target an object-reference item (SR8 :31298); SUPER sender rejected (SR9
    /// :31300); SELF only inside a method, and a TYPED target requires the current class to conform
    /// (SR12c :31353); a dataReference sender must be an object-reference item, and a TYPED target
    /// requires a TYPED, conforming sender (SR12a2 :31341 — universal-into-typed is OUTSIDE SR12's closed
    /// list: the narrowing tool is an object view, the EC-OO wave); a UNIVERSAL target is unconstrained
    /// (SET universal TO typed is unconditionally legal). An unresolvable sender that names a CLASS of the
    /// group is the SR13 factory-object form — the factory singleton reference (D11 makes it directly
    /// emittable).</summary>
    internal BoundStatement OoBindSetObjectRef(
        IReadOnlyList<Core.DataReferenceContext> targetRefs,
        Core.DataReferenceContext? senderRef, bool senderNull, bool senderSelf, bool senderSuper)
    {
        // SET … TO object-reference (§14.9.39 Format 5) is a COBOL-2002 introduction — bind-time gate at the ONE
        // convergence point for both the NULL/SELF/SUPER route (BindSet) and the data-sender re-route (BindSetTo);
        // rearch bind-time migration Cluster 8a (the parse-time {is2002()}? predicate is gone).
        ConstructRegistry.Check(data.Edition.Edition, data.Edition, Constructs.SetObjectReference2002, "the SET … TO object-reference statement (Format 5)");
        if (senderSuper)
        {
            data.Edition.Error("COBOLNET0867",
                "SET … TO SUPER: SUPER shall not be the sending operand of an object-reference SET "
                + "(ISO §14.9.39.3 SR9)");
            return new BoundNop();
        }
        var targets = new List<Place>(targetRefs.Count);
        foreach (var t in targetRefs)
        {
            if (string.Equals(t.GetText(), "EXCEPTION-OBJECT", StringComparison.OrdinalIgnoreCase))
            {
                data.Edition.Error("COBOLNET0848",
                    "SET EXCEPTION-OBJECT: the predefined object reference shall not be a receiving "
                    + "operand (ISO §8.4.3.6 SR1)");
                return new BoundNop();
            }
            if (refs.Resolve(t) is not { } tp || tp.Item.Pic is not { Category: PicCategory.ObjectReference })
            {
                data.Edition.Error("COBOLNET0867",
                    $"SET '{t.GetText()}': the receiving operand of an object-reference SET shall be a "
                    + "USAGE OBJECT REFERENCE data item (ISO §14.9.39.3 SR8)");
                return new BoundNop();
            }
            targets.Add(tp);
        }

        Place? src = null;
        string? srcFactoryClassCs = null;
        if (senderSelf)
        {
            if (OoCurrentClass is not { } cur)
            {
                data.Edition.Error("COBOLNET0867",
                    "SET … TO SELF: SELF is defined only within a method of a class (ISO §14.9.39.3 SR12c)");
                return new BoundNop();
            }
            foreach (var tp in targets)
                if (tp.Item.Pic!.ObjectClassName is { } tcn
                    && OoClasses?.Find(tcn) is { } tcls && !cur.ConformsTo(tcls))
                    data.Edition.Error("COBOLNET0867",
                        $"SET '{tp.Item.CobolName}' TO SELF: class '{cur.Name}' is not '{tcls.Name}' or a "
                        + "subclass of it (ISO §14.9.39.3 SR12c2)");
        }
        else if (!senderNull)
        {
            if (senderRef is null) return new BoundUnsupported("SET object-reference sender shape");
            var sp = refs.Resolve(senderRef);
            if (sp is not null && sp.Item.Pic is { Category: PicCategory.ObjectReference } spic)
            {
                foreach (var tp in targets)
                    if (tp.Item.Pic!.ObjectClassName is not null
                        && OoClasses?.ObjectRefWideningMismatch(spic, tp.Item.Pic!) is { } werr)
                        data.Edition.Error("COBOLNET0867",
                            $"SET '{tp.Item.CobolName}' TO '{sp.Item.CobolName}': {werr} "
                            + "(ISO §14.9.39.3 SR12 — a universal sender needs an object view to narrow)");
                src = sp;
            }
            else if (string.Equals(senderRef.GetText(), "EXCEPTION-OBJECT", StringComparison.OrdinalIgnoreCase))
                // §8.4.3.6 — the predefined register (ONE per run unit, GR2; implicitly universal SR2):
                // a universal target copies the reference; a TYPED target gets the RUNTIME narrow check
                // in the emitter (§9.3.8.2 :12291 — EC-OO-UNIVERSAL on failure; the SR12 closed list is
                // satisfied through the object-view-equivalent runtime conformance this register carries).
                return new BoundSetObjectRef(targets, null, false, false) { FromExceptionObject = true };
            else if (senderRef.cobolWord()?.GetText() is { } sname && OoClasses?.Find(sname) is { } scls)
            {
                // SR13 (:31371): the sender names a CLASS → the factory object of that class. D11's
                // singleton makes it a direct reference; conformance into a TYPED target is the FACTORY
                // conformance question — v1 permits only a UNIVERSAL target (factory-class hierarchies
                // widen via FACTORY OF phrases the USAGE grammar does not carry yet — 0899-noted).
                foreach (var tp in targets)
                    if (tp.Item.Pic!.ObjectClassName is not null)
                    {
                        data.Edition.Error("COBOLNET0867",
                            $"SET '{tp.Item.CobolName}' TO {sname}: a factory-object sender (SR13) into a "
                            + "TYPED receiver needs the FACTORY OF usage phrase — not yet carried "
                            + "(universal receivers accept it)");
                        return new BoundNop();
                    }
                srcFactoryClassCs = scls.FactoryCsName;
            }
            else
            {
                data.Edition.Error("COBOLNET0867",
                    $"SET … TO '{senderRef.GetText()}': the sending operand shall be an object-reference "
                    + "data item, NULL, SELF, or a class-name (ISO §14.9.39.3 SR9/SR12/SR13)");
                return new BoundNop();
            }
        }
        return new BoundSetObjectRef(targets, src, senderNull, senderSelf) { SourceFactoryCs = srcFactoryClassCs };
    }

    /// <summary>True when an arithmetic expression is EXACTLY one bare data reference (the Format-5
    /// re-route's sender shape) — its single dataReference descendant spans the whole expression text.</summary>
    private static Core.DataReferenceContext? OoExtractBareReference(Core.ArithmeticExpressionContext e)
    {
        Core.DataReferenceContext? only = null;
        var stack = new Stack<Antlr4.Runtime.Tree.IParseTree>();
        stack.Push(e);
        while (stack.Count > 0)
        {
            var cur = stack.Pop();
            if (cur is Core.DataReferenceContext d)
            {
                if (only is not null) return null;
                only = d;
                continue;
            }
            for (int i = 0; i < cur.ChildCount; i++) stack.Push(cur.GetChild(i));
        }
        return only is not null && only.GetText() == e.GetText() ? only : null;
    }

    private static string? OoDecodeMethodNameLiteral(Core.LiteralContext? lit)
    {
        var nn = lit?.nonNumericLiteral();
        if (nn is null) return null;
        if (nn.STRINGLIT() is { } sl) return DecodeCobolString(sl.GetText());
        if (nn.NATLIT() is { } nat)
        {
            string t = nat.GetText();
            return t.Length >= 3 ? DecodeCobolString(t[1..]) : "";   // strip the N prefix, decode the body
        }
        if (nn.HEXLIT() is { } hex)
        {
            string t = hex.GetText();
            int q = t.IndexOf(t[^1]);   // the opening quote (matches the closing one)
            string digits = t[(q + 1)..^1];
            if (digits.Length % 2 != 0) return "";
            var chars = new char[digits.Length / 2];
            for (int i = 0; i < chars.Length; i++)
                chars[i] = (char)Convert.ToInt32(digits.Substring(i * 2, 2), 16);
            return new string(chars);
        }
        return null;
    }

    /// <summary>The significant-digit count of a numeric literal rescaled to <paramref name="scale"/> (the
    /// same string math as the emitter's <c>EmitText.UnscaledAtScale</c>, counting only — the bind-time
    /// fits-the-formal check for literal arguments, §14.8.2).</summary>
    private static int OoUnscaledDigitCount(string raw, int scale)
    {
        string t = raw.Trim().TrimStart('+').TrimStart('-');
        int dot = t.IndexOf('.');
        string intPart = dot < 0 ? t : t[..dot];
        string fracPart = dot < 0 ? "" : t[(dot + 1)..];
        string digits = scale >= 0
            ? intPart + (fracPart.Length < scale ? fracPart.PadRight(scale, '0') : fracPart[..scale])
            : (intPart + fracPart) is var all && all.Length > -scale ? all[..^(-scale)] : "0";
        return digits.TrimStart('0').Length;
    }

    /// <summary>The §14.8.2/§14.8.3 STRICT conformance check between a formal/returning item and an
    /// argument/receiver item — delegates to the ONE shared description-equality rule
    /// (<see cref="OoClassTable.DescriptionMismatch"/>, also the §9.3.8.2 override-signature check) that
    /// makes the emitted marshaling TYPE-PRESERVING. Null when conformant, else the mismatch.</summary>
    private static string? OoConformanceError(DataItem formal, DataItem arg)
        => OoClassTable.DescriptionMismatch(formal, arg);

    // ── Method-context control flow (deep-dive D8) ──────────────────────────────────────────────────────────

    /// <summary>GOBACK inside a METHOD (§14.9.18.4 GR4): terminate the METHOD, control back to the INVOKE
    /// site. The RETURNING-item delivery is the method entry's job (slice 2 — no formals yet); GOBACK's own
    /// phrases in a method context stage loud (RAISING → the EC-OO slice; the RETURNING/GIVING and 2023
    /// status phrases are activation-result forms that do not apply to a method return).</summary>
    private BoundStatement OoBindMethodGoback(Core.GobackStatementContext g)
    {
        if (g.dataReference() is not null)
            return new BoundUnsupported("GOBACK with a RETURNING/GIVING phrase inside a method "
                + "(ISO §14.9.18.4 GR4 returns the METHOD's RETURNING item — an activation-result form)");
        return new BoundMethodReturn(OoBindMethodRaising(g.raisingPhrase(), "GOBACK"));
    }

    /// <summary>Bind a method-context RAISING phrase (§14.9.18.4 GR1b — staged before the MethodReturn
    /// throw; the INVOKE site picks up). RAISING LAST inside a method needs method DECLARATIVES (SR5: only
    /// in a declarative/WHEN) — staged with the method-declaratives refinement.</summary>
    private BoundRaising? OoBindMethodRaising(Core.RaisingPhraseContext? raising, string verb)
    {
        if (raising is null) return null;
        if (raising.LAST() is not null)
        {
            data.Edition.Error("COBOLNET0899",
                $"{verb} RAISING LAST EXCEPTION inside a method: LAST is legal only within a declarative "
                + "or a PERFORM WHEN (ISO §14.9.18.3 SR5) — method declaratives are a later refinement of "
                + "the EC-OO wave");
            return null;
        }
        return EcBindRaising(raising, raising.Start.Line, verb);
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
        return new BoundMethodReturn(OoBindMethodRaising(e.raisingPhrase(), "EXIT METHOD"));
    }
}
