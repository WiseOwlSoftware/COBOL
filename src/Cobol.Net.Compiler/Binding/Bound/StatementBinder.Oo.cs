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
/// the invocation result (NEW's created object, or the method's <paramref name="ReturningSource"/> item per
/// §14.9.23.4 GR8); <paramref name="Args"/> carries the positionally-bound USING arguments (D6 — GR3).</summary>
public sealed record BoundInvoke(
    InvokeForm Form, string? ClassCsName, Place? Receiver, string? MethodCsName, Place? Returning,
    IReadOnlyList<BoundInvokeArg>? Args = null, DataItem? ReturningSource = null)
    : BoundStatement;

/// <summary>One bound INVOKE argument (deep-dive D6; §14.9.23.4 GR6): the FORMAL it corresponds to
/// positionally (its description drives the marshaling — §14.8.2's strict conformance was validated at bind,
/// so the crossing is type-preserving), the identifier source place OR the literal (decoded string / raw
/// numeric text), and whether the argument writes back (BY REFERENCE identifier — changes visible to the
/// caller; BY CONTENT and the §14.9.23.3 SR 10 object-data auto-CONTENT case do not).</summary>
public sealed record BoundInvokeArg(
    DataItem Formal, Place? Source, string? NumericLiteral, string? StringLiteral, bool WriteBack);

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
        /// <summary>The method's DATA name scope (§11.7 GR5) — activated on <c>DataBinder.ActiveMethodScope</c>
        /// while this method's statements bind (slice 2).</summary>
        public OoMethodDataScope? Data;
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
            if (OoConformanceError(m.Returning!, rp.Item) is { } err)
            {
                data.Edition.Error("COBOLNET0828",
                    $"INVOKE \"{m.Name}\" RETURNING '{retRef.GetText()}': {err} (ISO §14.8.3 returning-item "
                    + "conformance)");
                return new BoundNop();
            }
            retPlace = rp;
        }
        return new BoundInvoke(InvokeForm.Instance, null, receiver, m.CsName, retPlace, args, m.Returning);
    }

    /// <summary>Bind ONE INVOKE argument against its positional formal (§14.9.23.4 GR6 + §14.9.23.3 SR5/SR9/
    /// SR10 + §14.8.2 conformance). Null on a diagnostic (the caller drops the statement — the compile already
    /// failed).</summary>
    private BoundInvokeArg? OoBindInvokeArg(Core.InvokeArgumentContext arg, DataItem formal, string methodName)
    {
        string Err(string msg)
        {
            data.Edition.Error("COBOLNET0828", $"INVOKE \"{methodName}\": {msg}");
            return msg;
        }

        if (arg.VALUE() is not null)
        {
            // SR5b: a BY VALUE argument requires a BY VALUE formal; every formal is BY REFERENCE today (the
            // procedure-division-header BY phrases are an unparsed grammar extension — added with them).
            Err($"BY VALUE argument #{formal.CobolName}: the corresponding formal parameter is BY REFERENCE "
                + "(ISO §14.9.23.3 SR5b; header BY VALUE formals are a later slice)");
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
            // §14.9.23.3 SR 10: an argument shall not be object data (factory/instance WORKING-STORAGE) —
            // explicit BY REFERENCE violates the rule; a BARE object-data identifier is assumed BY CONTENT
            // (GR6a2: it fails SR 9/10, so BY REFERENCE cannot be assumed).
            bool objectData = data.OoIsObjectData(place.Item);
            if (explicitReference && objectData)
            {
                Err($"BY REFERENCE argument '{dref.GetText()}' references OBJECT data — factory/instance "
                    + "working-storage may not cross an INVOKE by reference (ISO §14.9.23.3 SR 10); pass it "
                    + "BY CONTENT");
                return null;
            }
            if (OoConformanceError(formal, place.Item) is { } err1)
            {
                Err($"USING argument '{dref.GetText()}' does not conform to formal parameter "
                    + $"'{formal.CobolName}': {err1} (ISO §14.8.2 — BY REFERENCE/BY CONTENT require the same "
                    + "description)");
                return null;
            }
            bool byReference = !explicitContent && !objectData;   // GR6a: REFERENCE assumed when SR9/10 hold
            return new BoundInvokeArg(formal, place, null, null, WriteBack: byReference);
        }

        // A literal argument — BY CONTENT (GR6a2; a literal never meets SR9).
        var lit = arg.literal();
        if (lit?.nonNumericLiteral()?.STRINGLIT() is { } sl)
        {
            string s = DecodeCobolString(sl.GetText());
            if (formal.Pic?.Category is not PicCategory.Alphanumeric)
            {
                Err($"nonnumeric literal argument {sl.GetText()} for the non-alphanumeric formal "
                    + $"'{formal.CobolName}' (ISO §14.8.2 literal conformance)");
                return null;
            }
            if (s.Length > formal.Pic.Length)
            {
                Err($"nonnumeric literal argument of length {s.Length} exceeds formal '{formal.CobolName}' "
                    + $"PIC X({formal.Pic.Length}) (ISO §14.8.2 — a literal shall fit the formal)");
                return null;
            }
            return new BoundInvokeArg(formal, null, null, s, WriteBack: false);
        }
        if (lit?.numericLiteral() is { } nl)
        {
            string raw = nl.GetText();
            if (formal.Pic is not { Category: PicCategory.Numeric, IsFloat: false })
            {
                Err($"numeric literal argument {raw} for the non-numeric formal '{formal.CobolName}' "
                    + "(ISO §14.8.2 literal conformance)");
                return null;
            }
            // Rescale to the formal at BIND time (exact string math) and require it to fit the formal's
            // digit positions — a literal that cannot conform is loud, never silently truncated.
            if (OoUnscaledDigitCount(raw, formal.Pic.Scale) > formal.Pic.Digits)
            {
                Err($"numeric literal argument {raw} does not fit formal '{formal.CobolName}' "
                    + $"({formal.Pic.Digits} digit(s), scale {formal.Pic.Scale}) (ISO §14.8.2)");
                return null;
            }
            return new BoundInvokeArg(formal, null, raw, null, WriteBack: false);
        }
        Err($"USING argument form for formal '{formal.CobolName}' is not yet carried across INVOKE");
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
    /// argument/receiver item — the rule set that makes the emitted marshaling TYPE-PRESERVING (no conversion
    /// crosses the boundary, so no cross-class numeric profile is ever referenced). Null when conformant, else
    /// the human-readable mismatch.</summary>
    private string? OoConformanceError(DataItem formal, DataItem arg)
    {
        if (formal.IsGroup)
        {
            // A group crosses as its character image (the CALL-boundary discipline): the argument must be a
            // group (image-capable) or an alphanumeric elementary item of the SAME character length.
            if (!(arg.IsGroup || arg.Pic?.Category is PicCategory.Alphanumeric))
                return "a group formal requires a group or alphanumeric argument";
            if (arg.IsGroup && !arg.IsImageCapable)
                return "the argument group has a float/COMP-5/INDEX leaf (no character image — Tier-C)";
            if (!formal.IsImageCapable)
                return "the formal group has a float/COMP-5/INDEX leaf (no character image — Tier-C)";
            return arg.ImageWidth != formal.ImageWidth
                ? $"character length mismatch (formal {formal.ImageWidth}, argument {arg.ImageWidth})"
                : null;
        }
        var f = formal.Pic!;
        if (arg.IsGroup)
        {
            // A GROUP argument to an elementary formal: legal only for an alphanumeric formal of the same
            // character length (the group crosses as its image).
            if (f.Category is not PicCategory.Alphanumeric)
                return "a group argument requires a group or alphanumeric formal";
            if (!arg.IsImageCapable) return "the argument group has no character image (Tier-C)";
            return arg.ImageWidth != f.Length
                ? $"character length mismatch (formal {f.Length}, argument {arg.ImageWidth})"
                : null;
        }
        var a = arg.Pic!;
        if (f.Category != a.Category)
            return $"category mismatch (formal {f.Category}, argument {a.Category})";
        switch (f.Category)
        {
            case PicCategory.ObjectReference:
                // §14.8.2 for object references: identical description — the same declared class (or both
                // universal). Subclass-to-base widening BY REFERENCE would need C# ref variance — rejected.
                return string.Equals(f.ObjectClassName, a.ObjectClassName, StringComparison.OrdinalIgnoreCase)
                    ? null
                    : $"declared class mismatch (formal '{f.ObjectClassName ?? "universal"}', argument "
                      + $"'{a.ObjectClassName ?? "universal"}')";
            case PicCategory.Numeric:
                if (f.Usage != a.Usage)
                    return $"USAGE mismatch (formal {f.Usage}, argument {a.Usage} — §14.8.2 requires the "
                        + "identical description for BY REFERENCE/BY CONTENT)";
                return f.Digits != a.Digits || f.Scale != a.Scale || f.Signed != a.Signed
                    ? $"numeric description mismatch (formal {(f.Signed ? "S" : "")}9({f.Digits}) scale "
                      + $"{f.Scale}, argument {(a.Signed ? "S" : "")}9({a.Digits}) scale {a.Scale})"
                    : null;
            case PicCategory.Alphanumeric:
                return f.Length != a.Length
                    ? $"length mismatch (formal X({f.Length}), argument X({a.Length}))"
                    : null;
            default:
                return $"formal category {f.Category} is not yet carried across INVOKE";
        }
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
