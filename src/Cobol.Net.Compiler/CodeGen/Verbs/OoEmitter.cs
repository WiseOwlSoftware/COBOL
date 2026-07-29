// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Binding.Bound;
using CobolNet.CodeGen.Emit;
using CobolNet.Runtime;

using CobolNet.Compiler.Oo;

namespace CobolNet.CodeGen;

using static CobolNet.CodeGen.Emit.EmitText;

/// <summary>The OO EMIT half (P7 Step 9m, BATCH-3b; direct-wired at 9n — the BIND half stays on the
/// CSharpEmitter bind-host facade behind the P6→P9 <c>IOoBindHost</c> seam): class/factory/interface unit
/// emission, INVOKE (typed · instance · D10 universal), SET object-ref, and the per-method LOCAL dispatcher.
/// RUN-UNIT scope — reads the per-unit collaborators through the LIVE <see cref="ProgramEmitter.Current"/>
/// root: class-unit emission RE-CREATES the whole per-unit set mid-run (<see cref="ProgramEmitter.BeginUnit"/>),
/// so captured copies would go stale (the coupling-census hazard). The class table and interface-data forests
/// arrive from the immutable <c>BoundCompilation</c> (never the bind host's session state).</summary>
internal sealed class OoEmitter(DispatchState dispatch, EcState ecState, CallUnitState callState,
    ProgramEmitter program,
    IReadOnlyDictionary<OoInterfaceSymbol, DataBinder> ifaceData,
    IReadOnlyList<AdapterPair> adapters)
{
    private UnitEmitters U => program.Current;
    private EmitContext Ctx => U.Ctx;
    private NumericRenderer Num => U.Num;
    private ConditionRenderer Cond => U.Cond;
    private ReferenceResolver Refs => U.Refs;

    /// <summary>Emit the EXTERNAL record-area backings for a data forest (ISO §13.18.22.4 GR4b / §8.6.7): each
    /// <c>FD … IS EXTERNAL</c> record 01 re-bases onto a run-unit <c>ExternalStore</c> cell keyed by the FD name, so
    /// every describer (a program AND an object/factory) sees ONE shared record area. Shared by the program emit path
    /// (<c>ProgramEmitter.EmitProgramClass</c>) and the OO type-half (M2-OO-1i inc 5) — a class EXTERNAL FD needs the same
    /// backing property, and <c>CallBindExternalAndGlobal</c> already populates <c>CallExternalBackings</c> on the
    /// class binder (it runs in <c>BindResolve</c>).</summary>
    public void EmitExternalBackings(DataBinder data, CodeWriter w)
    {
        foreach (var ext in data.CallExternalBackings)
        {
            // §13.18.63 GR4a: a PLAIN external item's VALUE takes effect only during INITIALIZE, so its cell seeds
            // with the blank/zoned initial image. §11.9.10.4 GR7: a CONSTANT RECORD is the ONE external item initialized
            // at initial state — seed its cell with the VALUE-composed image via the SAME Tier-B seeder
            // (GroupImageCodec.ImageInitOf) that RecordStructEmitter/ProgramEmitter use for every other string backing.
            string init = ext.Record.IsConstantRecord
                ? RuntimeApi.StrStore(new DataEmitter(Ctx).ImageInitOf(ext.Record), $"{ext.Width}")
                : CsLiteral(ext.InitImage);
            w.Line($"private ref string {ext.BackingCsName} => ref ExternalStore.Cell({CsLiteral(ext.ExternalName)}, "
                + $"{init}).Ref;   // EXTERNAL — ONE storage copy per run unit (ISO §8.6.7); survives CANCEL (§14.9.5 GR8)");
        }
    }

    /// <summary>True when this unit must emit a <c>__DescribeExternals()</c> activation-entry registration
    /// (ISO §14.8.4): the compilation group has an enabling EC-EXTERNAL <c>&gt;&gt;TURN</c> somewhere AND the
    /// unit describes any external record or external file connector. False keeps the generated source
    /// byte-identical to a pre-VCR-15 build (zero-scaffolding).</summary>
    public static bool WantsExternalDescribes(DataBinder data) =>
        data.ExternalDescribe && (data.CallExternalBackings.Count > 0 || data.Files.Any(f => f.IsExternal));

    /// <summary>Emit the <c>__DescribeExternals()</c> method — one <c>ExternalStore.Describe</c> per external
    /// record (§14.8.4.3 / §13.18.22 GR6 facts: byte count, record-name VALUE clause spec, strong TYPE name,
    /// CONSTANT RECORD presence) and per external file connector (§14.8.4.2 file-referencing control-item
    /// identities + the §12.4.5.3 GR1 a–m entry fingerprint), each carrying the unit's before-Environment-
    /// division mask (§14.8.4.1). Called FIRST at every activation entry (<c>Call</c> and <c>Activate</c>) —
    /// the §14.9.4.4 GR3e check precedes execution; a raise unwinds to the CALL site before any statement or
    /// formal adoption runs. A complete-record REDEFINES contributes nothing (GR6's explicit exemption — the
    /// descriptor is built from the base record only). RECORD DELIMITER / RESERVE / COLLATING SEQUENCE are not
    /// modeled by <see cref="FileModel"/>, so they are identical by construction and absent from the
    /// fingerprint.</summary>
    public void EmitExternalDescribes(DataBinder data, string unitPath, CodeWriter w)
    {
        using (w.Block("private void __DescribeExternals()"))
        {
            foreach (var ext in data.CallExternalBackings)
            {
                // §13.18.22 GR6: the VALUE identity is the RECORD NAME's own VALUE clause specification ("the
                // VALUE clause specification, if any, for each record name ... shall be identical") — the
                // record-level clause text, not the subordinate items' clauses.
                string valueSpec = ext.Record.RawValue is { } rv ? CsLiteral(rv.ToUpperInvariant()) : "null";
                string strongKey = ext.Record.StrongType && ext.Record.TypeName is { } tn ? CsLiteral(tn.ToUpperInvariant()) : "null";
                w.Line($"ExternalStore.Describe({CsLiteral(unitPath)}, {CsLiteral(ext.ExternalName)}, "
                    + $"new ExternalDescriptor(\"record\", ByteCount: {ext.Width}, ValueImage: {valueSpec}, "
                    + $"StrongTypeKey: {strongKey}, ConstantRecord: {(ext.Record.IsConstantRecord ? "true" : "false")}), "
                    + $"{data.ExternalCheckMask});   // §14.8.4.3 / §13.18.22 GR6");
            }
            foreach (var f in data.Files.Where(f => f.IsExternal))
            {
                string ItemRef(string? clauseName, DataItem? item) => clauseName is null ? "null"
                    : CsLiteral(BinderDriver.ExternalItemIdentity(item) ?? "!");   // "!" = present but NOT an external item (§14.8.4.2 violation face)
                string linage = f.Linage is null ? "null"
                    : CsLiteral(string.Join(";", f.Linage.Operands.Select(op => op.DataName is null
                        ? $"={op.Literal}"
                        : BinderDriver.ExternalItemIdentity(op.Item) ?? "!")));
                string fingerprint = CsLiteral(
                    $"OPT={f.Optional}|ASSIGN={f.AssignTarget.ToUpperInvariant()}|ORG={f.Organization}|ACC={f.AccessMode}"
                    + $"|KEY={(f.RecordKeyName?.ToUpperInvariant() ?? "")}"
                    + $"|ALT={string.Join(",", f.AlternateKeyNames.Select(a => $"{a.Name.ToUpperInvariant()}:{a.Duplicates}"))}"
                    + $"|SHARE={f.Sharing}|LOCK={(f.LockMode is { } lm ? $"{lm.Kind},{lm.Multiple}" : "")}");
                w.Line($"ExternalStore.Describe({CsLiteral(unitPath)}, {CsLiteral(f.ExternalName!)}, "
                    + $"new ExternalDescriptor(\"file\", FileStatusRef: {ItemRef(f.FileStatusName, f.FileStatusItem)}, "
                    + $"RelativeKeyRef: {ItemRef(f.RelativeKeyName, f.RelativeKeyItem)}, LinageRef: {linage}, "
                    + $"SelectFingerprint: {fingerprint}), {data.ExternalCheckMask});   // §14.8.4.2 / §14.8.4.4 / §12.4.5.3 GR1");
            }
        }
    }

    private void EmitFileMembers(string csName, DataBinder data, BoundProgram bound, CodeWriter w)
    {
        var hostFiles = data.Files.Where(f => !f.IsSortMerge).ToList();   // an SD is the in-memory sort store, never a host connector
        if (hostFiles.Count == 0) return;
        w.Line();
        // A per-object minted-key field for each instance file (§9.1.4 — one connector per object): initialized once
        // per object (field initializers run before the ctor body), so the ctor's Register/track see it live. A
        // factory / EXTERNAL file has a static literal key (InstanceKeyField null) and emits no field.
        foreach (var f in hostFiles.Where(f => f.InstanceKeyField is not null))
            w.Line($"private readonly string {f.InstanceKeyField} = {RuntimeApi.FileMintInstanceKey(CsLiteral(f.CobolName))};");
        using (w.Block($"public {csName}()"))
        {
            U.SeqIo.EmitFileRegistration(w);   // each file registers under FileKeyExpr(f): a factory literal, or this.__fkey_X
            // A REPORT SECTION in this object/factory (Report Writer is a complete subsystem — the class emit path
            // just has to CALL it, the same class-emit-gap shape as inc 3/5): the engines construct AFTER their FDs
            // register (COBOLNET_REPORT_WRITER_DESIGN §4). Early-returns when Reports.Count == 0.
            U.ReportWriter.EmitReportConstruction(bound, w);
            foreach (var f in hostFiles.Where(f => f.InstanceKeyField is not null))
                w.Line($"__TrackInstanceFile({FileKeyExpr(f)});");   // closed + dropped when the object is deleted (§9.1.4)
        }
        w.Line();
    }

    /// <summary>
    /// Emit one COBOL class as a real C# class (deep-dive D1/D2/D3/D7): instance fields from OBJECT data
    /// (VALUE clauses become field initializers — the generated public parameterless ctor IS the predefined
    /// NEW factory, D4: C# runs base-then-derived initialization exactly like COBOL's inherited-then-own
    /// order), one <c>public virtual</c> method per METHOD-ID whose body runs its exit-bounded pc range, and
    /// ONE <c>__Dispatch</c> over the class's whole method-paragraph space (the same dispatcher body a program
    /// class gets — the emit-into-a-type reuse). Runs on the SAME per-unit emitter-state switch as
    /// <c>ProgramEmitter.EmitProgramClass</c>.
    /// </summary>
    public void EmitClassUnit(OoClassUnit cls, CodeWriter w)
    {
        // The INSTANCE class (D1/D2 + slice 3a: `: BASE` when the class INHERITS — single inheritance v1,
        // SSOT §18.18 — else the CobolObject runtime root; Roslyn needs no declaration ordering). The DIRECT
        // IMPLEMENTS list joins the base list (§11.8 — the closure arrives transitively at the C# level);
        // covariant-return conformances render as EXPLICIT interface implementations (D-I1's adapter cure:
        // C# forbids covariant interface implementations that §9.3.8.2.3 5a/5c2 permit).
        string instBase = string.Join(", ", new[] { cls.Symbol.Base?.CsName ?? "CobolObject" }
            .Concat(cls.Symbol.Implements.Select(i => i.CsName)));
        var instExtras = adapters
            .Where(a => !a.Factory && ReferenceEquals(a.Impl.Owner, cls.Symbol))
            .Select(a =>
            {
                var (protoRet, protoSig) = OoSignatureOf(a.Proto);
                string args = string.Join(", ", a.Proto.Binding!.Formals.Select(f => $"ref {f.ParamName}"));
                return $"{protoRet} {a.Iface.CsName}.{a.Proto.CsName}({protoSig}) => this.{a.Impl.CsName}({args});   // covariant-return adapter (§9.3.8.2.3 5c2)";
            })
            .ToList();
        EmitTypeHalf(cls.Name, cls.CsName, instBase,
            cls.Data, cls.Refs, cls.Bound, cls.Symbol.Methods, w,
            headerExtras: instExtras.Count > 0 ? instExtras : null,
            sealedType: cls.Symbol.IsFinal);

        // The FACTORY class (brief D11 — a REAL sibling singleton, NEVER statics: §8.6.4 per-class copies of
        // inherited factory data; SELF-in-factory polymorphism SR4f + GR2; §9.3.6 chain resolution). Every
        // CLASS-ID emits one — a class with no FACTORY paragraph still needs its own factory object and a
        // chain node for inherited factory methods.
        string facBase = string.Join(", ", new[] { cls.Symbol.Base?.FactoryCsName ?? "CobolObject" }
            .Concat(cls.Symbol.FactoryImplements.Select(i => i.CsName)));
        var extras = new List<string>
        {
            // The singleton (§9.3.14.2 "created before it is first referenced" — .NET static-readonly type
            // initialization satisfies it exactly). A derived factory needs `new` to shadow the base's.
            $"public {(cls.Symbol.Base is not null ? "new " : "")}static readonly {cls.Symbol.FactoryCsName} __Instance = new();",
            // The predefined New as a COVARIANT virtual (§16.2.1 GR1 ACTIVE-CLASS creation — an inherited
            // factory MAKE reached via INVOKE DOG "…" creates a DOG through the runtime override). A FINAL
            // class's factory is SEALED: its root __New emits NON-virtual (a virtual member in a sealed type
            // is Roslyn CS0549 on emitted code — the same trap the method-modifier table guards).
            cls.Symbol.Base is not null
                ? $"public override {cls.CsName} __New() => new {cls.CsName}();"
                : $"public {(cls.Symbol.IsFinal ? "" : "virtual ")}{cls.CsName} __New() => new {cls.CsName}();",
        };
        EmitTypeHalf(cls.Name, cls.Symbol.FactoryCsName, facBase,
            cls.FactoryData, cls.FactoryRefs, cls.FactoryBound, cls.Symbol.FactoryMethods, w, extras,
            sealedType: cls.Symbol.IsFinal);
    }

    /// <summary>The emit-into-a-type parameterization, realized (deep-dive Summary): ONE routine renders
    /// fields + methods + dispatch into a named type — called for the instance class and the factory class
    /// of every CLASS-ID (identical machinery; only the type identity, base, data forest, roster, and header
    /// extras differ).</summary>
    private void EmitTypeHalf(string cobolName, string csName, string baseCsName,
        DataBinder data, ReferenceResolver refs, BoundProgram bound, IReadOnlyList<OoMethodSymbol> roster,
        CodeWriter w, IReadOnlyList<string>? headerExtras, bool sealedType = false)
    {
        program.BeginUnit(w, data, refs);
        callState.SelfPath = cobolName;       // a CALL from a method names the class as its calling path (§8.4.6.3)
        callState.ReturningPlace = null;      // methods deliver results via slice-2 RETURNING, never the program ABI
        ecState.UnitHasF3 = false;            // declaratives inside methods are staged loud (no __EcDispatch here)
        ecState.UnitHasF3Perform = false;     // an F3 PERFORM inside a method is loud-rejected (§9.1-B) — never emitted here
        dispatch.UseDecls = false;               // a class owns no USE declaratives — clear any bleed from a prior unit (M2-OO-1i review)
        dispatch.OuterGlobalUse = false;
        dispatch.DebugActive = false;            // a class owns no USE FOR DEBUGGING facility — clear any bleed (VCR 7.17)
        callState.InheritedStatusPlace.Clear();

        using (w.Block($"public {(sealedType ? "sealed " : "")}class {csName} : {baseCsName}"))
        {
            foreach (string line in headerExtras ?? [])
                w.Line(line);
            var fields = new DataEmitter(Ctx);
            fields.Emit();   // WS → INSTANCE fields (D3/D11); method WS → statics; VALUE inits = field initializers (D4)
            EmitExternalBackings(data, w);       // M2-OO-1i inc 5: a class EXTERNAL FD record → the shared run-unit cell
            U.ReportWriter.EmitReportMembers(w);              // M2-OO-1i review: a class REPORT SECTION's engine fields + compose methods (Report Writer is complete)
            EmitFileMembers(csName, data, bound, w);   // M2-OO-1i: object/factory file connectors + report construction register in an emitted ctor
            // A method file verb under >>TURN EC-I-O … CHECKING emits an __IoCheckEc call (§9.1.13.1 fatal-status
            // default); the class type must declare it. A class has no USE declaratives (Declaratives == null), so
            // EcEmitIoCheckEc reduces to the status→EC bridge — no __RunUse/__EcDispatch needed (M2-OO-1i review).
            // C1 (design SSOT §9.10.1): when ANY method has an F3 PERFORM (bound.Ec.HasF3Perform is class-level true),
            // the class-member __IoCheckEc must carry its frame-first branch (GR17 — a WHEN preempts the USE) and the
            // class needs the __EcPerform raise-site funnel. Both read ecState.UnitHasF3Perform AT EMIT TIME, and it
            // was cleared for the class at BeginUnit (set true only PER METHOD, later) — so set it for the duration of
            // these two class-member emissions. Byte-identical when no method has F3 (HasF3Perform false ⇒ flag false).
            bool classHasF3Perform = bound.Ec is { HasF3Perform: true };
            bool savedUnitF3P = ecState.UnitHasF3Perform;
            ecState.UnitHasF3Perform = classHasF3Perform;
            if (bound.Ec is { HasIoChecked: true }) U.Ec.EmitIoCheckEc(bound, w);
            if (classHasF3Perform) U.Ec.EmitEcPerformMember(w);   // the raise-site funnel, once per class (§9.10.1-C1)
            ecState.UnitHasF3Perform = savedUnitF3P;
            if (bound.Paragraphs.Count > 0)
                w.Line($"private const int __N = {bound.Paragraphs.Count};   // paragraph count (all methods — one pc space)");
            w.Line();
            foreach (var m in roster)
                EmitMethod(bound, m, fields, w);
            EmitCobolInvoke(cobolName, roster, w);   // D10: the universal-dispatch switch (BOTH halves —
                                                       // a universal reference can hold a factory object)
        }
        w.Line();
    }

    /// <summary>Emit the class's <c>__CobolInvoke</c> override (D10/D-U2/D-U4): a switch over the methods
    /// this type DECLARES that are NOT overrides (an override needs no case — the BASE class's case calls
    /// <c>this.M(…)</c> and C# virtual dispatch delivers the override; 0829 guarantees identical
    /// descriptors), <c>default:</c> chains <c>base.__CobolInvoke</c> — the chain IS §9.3.6 resolution
    /// order, and the CobolObject root raises EC-OO-METHOD (GR7b). Each case enforces §14.9.23.4 GR7c at
    /// runtime — arity, per-argument conformance-descriptor equality (D-U3: the SAME rule as the
    /// compile-time strict check), RETURNING presence BOTH directions — raising EC-OO-UNIVERSAL (Table 13,
    /// fatal; unconditionally — the EC-OO-NULL/METHOD precedent: proceeding with a nonconforming crossing
    /// in a typed-native model is never an option). Box forms are CANONICAL BY DESCRIPTOR (D-U6a — never
    /// by either side's StoreAsImage — the read-only projection of the Storage the group-tail StorageFormPass computes): S:* → string; N:Display:* →
    /// the display IMAGE string (bridged by the FormatDisplay/StoreDisplay overload pair); other N:* →
    /// the native value; O:* → the CobolObject reference. A type declaring zero non-override methods
    /// emits no override.</summary>
    /// <summary>Render the §14.9.23.4 GR7c two-arm stop for one <c>__CobolInvoke</c> conformance check.
    ///
    /// <para>GR7c sets EC-OO-UNIVERSAL "if checking for it is enabled in BOTH the activated method and the
    /// activating runtime element". The activator's half is <c>ExceptionState.OoUniversalChecking</c>, set by the
    /// emitted statement guard around the INVOKE; the METHOD's half is a compile-time literal folded at bind time
    /// (<see cref="OoMethodSymbol.OoUniversalCheckingHere"/>) — it is a property of the callee's SOURCE, not of
    /// run-unit state, so it cannot be a flag.</para>
    ///
    /// <para>When it is not enabled in both, no exception condition exists and none may be attributed — but a
    /// nonconforming crossing still cannot proceed into typed-native code, so the stop is a
    /// <c>CobolImplementorFatalException</c>, which carries NO EC name and therefore cannot be selected by any
    /// statement guard's <c>EcName ==</c> match (§14.6.13.1.1 NOTE 3 undefined-results latitude).</para></summary>
    private static string OoUnivStop(OoMethodSymbol m, string cond, string detailExpr)
    {
        string both = m.OoUniversalCheckingHere ? "ExceptionState.OoUniversalChecking" : "false";
        return $"if ({cond}) {{ if ({both}) throw new CobolFatalException(\"EC-OO-UNIVERSAL\", {detailExpr}); "
            + $"throw new CobolImplementorFatalException({detailExpr}); }}";
    }

    private void EmitCobolInvoke(string cobolName, IReadOnlyList<OoMethodSymbol> roster, CodeWriter w)
    {
        var cases = roster.Where(m => m.OverrideOf is null).ToList();
        if (cases.Count == 0) return;
        w.Line();
        using (w.Block("public override void __CobolInvoke(string __name, CobolInvokeArg[] __a, CobolInvokeArg? __ret)"))
        using (w.Block("switch (__name)"))
        {
            foreach (var m in cases)
            {
                using (w.Block($"case {Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(m.Name.ToUpperInvariant(), quote: true)}:"))
                {
                    w.Line(OoUnivStop(m, $"__a.Length != {m.Binding!.Formals.Count}",
                        $"$\"INVOKE '{cobolName}' '{m.Name}': {{__a.Length}} argument(s) for {m.Binding!.Formals.Count} formal(s) "
                        + "(ISO §14.9.23.4 GR7c/§14.8.2 — runtime conformance through a universal receiver)\""));
                    for (int i = 0; i < m.Binding!.Formals.Count; i++)
                    {
                        var f = m.Binding!.Formals[i];
                        string want = OoConformance.ConformanceDescriptor(f.Item);
                        string wantLit = Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(want, quote: true);
                        w.Line(OoUnivStop(m, $"__a[{i}].Descriptor != {wantLit}",
                            $"$\"INVOKE '{cobolName}' '{m.Name}': argument {i + 1} does not conform to the formal "
                            + $"(caller {{__a[{i}].Descriptor}}, formal {want.Replace('"', '\'')}) (ISO §14.9.23.4 GR7c/§14.8.2)\""));
                        w.Line($"var __p{i} = {OoUnivUnbox(f.Item, $"__a[{i}].Value")};");
                    }
                    if (m.Binding!.Returning is null)
                        w.Line(OoUnivStop(m, "__ret is not null",
                            $"\"INVOKE '{cobolName}' '{m.Name}': RETURNING specified but the method declares none "
                            + "(ISO §14.8.3/GR7c)\""));
                    else
                    {
                        string rl = Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(
                            OoConformance.ConformanceDescriptor(m.Binding!.Returning), quote: true);
                        w.Line(OoUnivStop(m, $"__ret is null || __ret.Descriptor != {rl}",
                            $"\"INVOKE '{cobolName}' '{m.Name}': the RETURNING item is absent or does not conform "
                            + "(ISO §14.8.3/GR7c)\""));
                    }
                    string argList = string.Join(", ", Enumerable.Range(0, m.Binding!.Formals.Count).Select(i => $"ref __p{i}"));
                    w.Line(m.Binding!.Returning is null
                        ? $"this.{m.CsName}({argList});"
                        : $"var __rv = this.{m.CsName}({argList});");
                    for (int i = 0; i < m.Binding!.Formals.Count; i++)
                        w.Line($"__a[{i}].Value = {OoUnivRebox(m.Binding!.Formals[i].Item, $"__p{i}")};   // SR6 BY REFERENCE write-back");
                    if (m.Binding!.Returning is not null)
                        w.Line($"__ret!.Value = {OoUnivRebox(m.Binding!.Returning, "__rv")};");
                    w.Line("return;");
                }
            }
            w.Line("default: base.__CobolInvoke(__name, __a, __ret); return;");
        }
    }

    /// <summary>D-U6a: true when the item's canonical UNIVERSAL box form is the display IMAGE string while
    /// its local crossing form is native — the FormatDisplay/StoreDisplay bridge applies both directions.</summary>
    private static bool OoUnivImageBridged(DataItem item) =>
        !OoStringCarried(item)
        && item.Pic is { Category: PicCategory.Numeric, IsFloat: false, Usage: Usage.Display };

    /// <summary>The callee-side unbox: box value → a local in the FORMAL's own crossing form.</summary>
    private static string OoUnivUnbox(DataItem item, string box) =>
        OoStringCarried(item) ? $"(string){box}!"
        : OoUnivImageBridged(item) ? RuntimeApi.NumStoreDisplay($"(string){box}!", item.ProfileName, $"({item.ElementType})0")
        : item.Pic is { Category: PicCategory.ObjectReference } p ? $"({p.ClrType}){box}"
        : $"({item.ElementType}){box}!";

    /// <summary>The callee-side re-box: a local in the formal's crossing form → the canonical box form.</summary>
    private static string OoUnivRebox(DataItem item, string local) =>
        OoUnivImageBridged(item) ? RuntimeApi.NumFormatDisplay(local, item.ProfileName) : $"(object?){local}";

    /// <summary>Caller-side universal dispatch (D-U6): box every argument per ITS OWN descriptor's canonical
    /// form, dispatch through the GR5 null guard with the bind-normalized literal or the runtime-normalized
    /// identifier-2 value, then copy out every argument (SR6 — all BY REFERENCE) and deliver RETURNING (GR8)
    /// through the receiver's own storage form. No direct-<c>ref</c> fast path BY DESIGN — the box IS the
    /// crossing (the abstract dispatch signature cannot take refs without per-signature generics).</summary>
    public void EmitUniversalInvoke(BoundInvokeUniversal u)
    {
        var w = Ctx.Writer;
        int id = Ctx.Names.NextStoreTmp();
        string boxes = string.Join(", ", u.Args.Select(a =>
            $"new CobolInvokeArg({Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(a.Descriptor, quote: true)}, {OoUnivCallerRead(a.Source)})"));
        w.Line($"var __ua{id} = new CobolInvokeArg[] {{ {boxes} }};");
        w.Line(u.Returning is not null
            ? $"var __ur{id} = new CobolInvokeArg({Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(u.ReturningDescriptor!, quote: true)});"
            : $"CobolInvokeArg? __ur{id} = null;");
        string selector = u.MethodLiteral is { } lit
            ? Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(lit, quote: true)
            : RuntimeApi.ObjNormalizeMethodName(PlaceRenderer.Read(u.MethodSource!));
        w.Line($"{RuntimeApi.ObjRequireNonNull(PlaceRenderer.Read(u.Receiver))}.__CobolInvoke({selector}, __ua{id}, __ur{id});");
        for (int i = 0; i < u.Args.Count; i++)
            w.Line(OoUnivCallerWrite(u.Args[i].Source, $"__ua{id}[{i}].Value") + "   // BY REFERENCE copy-out (SR6)");
        if (u.Returning is { } ret)
            w.Line(OoUnivCallerWrite(ret, $"__ur{id}!.Value") + "   // RETURNING delivery (§14.9.23.4 GR8)");
        EmitInvokePickup();   // §14.6.13.1.5 — the universal path propagates identically (D-EO6)
    }

    /// <summary>SET Format 5 (D-U7; §14.9.39 GR9/GR10): copy the ONE sender reference into each target in
    /// order. The cast renders total: conformance was bind-checked (0867), and C# reference conversions
    /// cover the widening directions (typed→universal, subclass→base, null, this).</summary>
    public void EmitSetObjectRef(BoundSetObjectRef s)
    {
        var w = Ctx.Writer;
        if (s.FromExceptionObject)
        {
            // §8.4.3.6 — the register is implicitly UNIVERSAL: a universal target copies the reference;
            // a TYPED target takes the runtime narrow check (§9.3.8.2 :12291 — conformance through a
            // universal source is a RUNTIME question; failure = EC-OO-UNIVERSAL, Table 13).
            foreach (var tp in s.Targets)
            {
                if (tp.Item.Pic!.ObjectClassName is null)
                {
                    w.Line(PlaceRenderer.Write(tp, "ExceptionState.ExceptionObject") + "   // SET universal TO EXCEPTION-OBJECT (§8.4.3.6)");
                    continue;
                }
                string clr = tp.Item.Pic!.ClrType.TrimEnd('?');
                int id = Ctx.Names.NextStoreTmp();
                w.Line($"var __xo{id} = ExceptionState.ExceptionObject;");
                w.Line($"if (__xo{id} is not null && __xo{id} is not {clr}) throw new CobolFatalException(\"EC-OO-UNIVERSAL\", "
                    + $"\"SET {tp.Item.CobolName} TO EXCEPTION-OBJECT: the current exception object is not a "
                    + $"{tp.Item.Pic!.ObjectClassName} (ISO 9.3.8.2 runtime conformance; Table 13)\");");
                w.Line(PlaceRenderer.Write(tp, $"({clr}?)__xo{id}") + "   // SET typed TO EXCEPTION-OBJECT (runtime-narrowed)");
            }
            return;
        }
        string src = s.SourceIsNull ? "null"
            : s.SourceIsSelf ? "this"
            : s.SourceFactoryCs is { } fac ? $"{fac}.__Instance"
            : PlaceRenderer.Read(s.Source!);
        foreach (var tp in s.Targets)
            w.Line(PlaceRenderer.Write(tp, $"({tp.Item.Pic!.ClrType})({src})") + "   // SET F5 (ISO §14.9.39 GR9 — reference copy)");
    }

    private static string OoUnivCallerRead(Place p) =>
        p is RefModPlace ? PlaceRenderer.Read(p)
        : OoUnivImageBridged(p.Item) ? PlaceRenderer.Read(new NumericImagePlace(p))
        : PlaceRenderer.Read(p);

    private static string OoUnivCallerWrite(Place p, string box) =>
        p is RefModPlace ? PlaceRenderer.Write(p, $"(string){box}!")
        : OoStringCarried(p.Item) ? PlaceRenderer.Write(p, $"(string){box}!")
        : OoUnivImageBridged(p.Item) ? PlaceRenderer.Write(new NumericImagePlace(p), $"(string){box}!")
        : p.Item.Pic is { Category: PicCategory.ObjectReference } pic ? PlaceRenderer.Write(p, $"({pic.ClrType}){box}")
        : PlaceRenderer.Write(p, $"({p.Item.ElementType}){box}!");

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
    private void EmitMethod(BoundProgram bound, OoMethodSymbol m, DataEmitter fields, CodeWriter w)
    {
        var (retType, sig) = OoSignatureOf(m);
        if (m.PropertySubject is { } subject)
        {
            // A PROPERTY-clause-synthesized accessor (D-P1): a DIRECT field body — identical descriptions
            // make the spec's implicit MOVE a straight copy (§13.18.42 GR1/GR2 :21214-21229).
            string pmod = m.OverrideOf is not null
                ? (m.IsFinal && !m.Owner.IsFinal ? "sealed override" : "override")
                : (m.IsFinal || m.Owner.IsFinal) ? "" : "virtual";
            string pmods = pmod.Length == 0 ? "" : pmod + " ";
            if (m.Accessor == 'G')
                w.Line($"public {pmods}{retType} {m.CsName}() => {subject.CsName};   // PROPERTY {m.PropertyName} GET (§13.18.42 GR1)");
            else
                w.Line($"public {pmods}void {m.CsName}(ref {(OoStringCarried(subject) ? "string" : subject.ElementType)} __V) {{ {subject.CsName} = __V; }}   // PROPERTY {m.PropertyName} SET (GR2)");
            w.Line();
            return;
        }
        // D7's TOTAL modifier table (the OVERRIDE/FINAL wave): virtual by default (§9.3.6 runtime-class
        // dispatch); an override emits `override` — `sealed override` when ITS FINAL and the class is not
        // already sealed; a FINAL root method (or ANY fresh slot in a FINAL class) emits NON-virtual — a
        // `virtual` member inside a `sealed` class is Roslyn CS0549 on EMITTED code (the loud-failure trap
        // this table exists for). COBOL never expresses C# `new`/hiding (SR4a), so the set is total.
        string modifier = m.OverrideOf is not null
            ? (m.IsFinal && !m.Owner.IsFinal ? "sealed override" : "override")
            : (m.IsFinal || m.Owner.IsFinal) ? ""
            : "virtual";
        using (w.Block($"public {(modifier.Length == 0 ? "" : modifier + " ")}{retType} {m.CsName}({sig})   // METHOD-ID {m.Name} (ISO §11.7)"))
        {
            // LINKAGE roots → locals: a formal seeds from its parameter (copy-in; the copy-out below realizes
            // the BY REFERENCE write-through at the method boundary); the RETURNING item and unattached
            // entries start at their initial state (§14.2.3 GR6 — callee-allocated).
            foreach (var root in m.Binding!.LinkageRoots)
            {
                // A Tier-A (alias) view root forwards to its canonical's field — no local (symmetry with
                // BuildPhysicals; COBOLNET_DESIGN §4.1; M2-OO-1h review C).
                if (root.Class is { Tier: RedefinesTier.Alias } && !root.IsCanonical) continue;
                // A method Tier-B REDEFINES canonical's storage is its string backing, not the root struct (M2-OO-1h
                // step 3) — emit that as the local; a LINKAGE formal seeds it from the caller's image, width-normalized
                // to the class width (a wider redefiner needs the full backing — review D), else from the initializer.
                if (fields.MethodRedefinesBackingDecl(root) is { } bkl)
                {
                    var formalB = m.Binding!.Formals.FirstOrDefault(f => ReferenceEquals(f.Item, root));
                    w.Line($"string {bkl.Name} = {(formalB is null ? bkl.Init : RuntimeApi.StrStore(formalB.ParamName, $"{root.Class!.Width}"))};   "
                        + $"// LINKAGE Tier-B REDEFINES backing for {root.CobolName}");
                    continue;
                }
                var (type, init) = fields.RootDecl(root);
                var formal = m.Binding!.Formals.FirstOrDefault(f => ReferenceEquals(f.Item, root));
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
            foreach (var root in m.Binding!.LocalRoots)
            {
                if (root.Class is { Tier: RedefinesTier.Alias } && !root.IsCanonical) continue;   // Tier-A view → no local (review C)
                if (fields.MethodRedefinesBackingDecl(root) is { } bkl)   // Tier-B canonical → the string backing local (M2-OO-1h step 3)
                {
                    w.Line($"string {bkl.Name} = {bkl.Init};   // LOCAL-STORAGE Tier-B REDEFINES backing for {root.CobolName} (§14.5.3)");
                    continue;
                }
                var (type, init) = fields.RootDecl(root);
                w.Line($"{type} {root.CsName} = {init};   // LOCAL-STORAGE {root.CobolName} — re-initialized each activation (§14.5.3)");
            }
            // A method LOCAL/LINKAGE table's INDEXED BY cell is a per-activation local (§14.5.3; M2-OO-1h step 4) —
            // the method's own cell (§11.7.4 GR5), reset to 1 each activation, never the shared class index field.
            foreach (var root in m.Binding!.LocalRoots.Concat(m.Binding!.LinkageRoots))
                foreach (var idx in DataBinder.IndexNamesUnder(root))
                    if (m.DataScope.IndexFields.TryGetValue(idx, out var cell))
                        w.Line($"long {cell} = 1;   // INDEX-NAME {idx} (LOCAL/LINKAGE table cell, §14.5.3)");
            // Per-method Format-3 (exception-checking) PERFORM context (design SSOT §9.10) — set ONLY for a method
            // that HAS an F3 PERFORM (its handler pc-ranges were appended to the class pc space), restored after, so
            // a non-F3 method is byte-identical. F3HandlerBasePc is the CLASS handler base (region marking in
            // EmitDispatchMethod + HandlerUseId = DeclCount + (pc − base)); DeclCount 0 (a method has no USE decls).
            bool methodF3 = m.Binding!.HandlerCount > 0;
            bool savedUnitF3P = ecState.UnitHasF3Perform;
            int savedDeclCount = dispatch.DeclCount;
            int? savedBase = dispatch.F3HandlerBasePc;
            if (methodF3)
            {
                ecState.UnitHasF3Perform = true;                    // raise sites in this method emit __EcPerform
                dispatch.DeclCount = 0;
                dispatch.F3HandlerBasePc = bound.F3HandlerBasePc;   // the class handler base
            }
            if (m.Binding!.EntryPc <= m.Binding!.EndPc)
            {
                // The method's slice of the class's one pc space, as a LOCAL FUNCTION (captures the locals
                // above by reference — zero allocation for direct calls).
                string saved = dispatch.DispatchName;
                dispatch.DispatchName = "__MDispatch";
                if (methodF3)
                {
                    // Method-LOCAL Format-3 machinery (§9.10): __useActive re-entrancy guards sized to the CLASS total
                    // handler count (the ONE HandlerUseId formula; a method uses only its own contiguous sub-range of
                    // slots). __MDispatch carries the method's real slice AND its handler sub-range as cases; __RunUse/
                    // __RunF3 (local functions calling __MDispatch) are reached only by the frame Matcher emitted inline
                    // in imp-1, which the method-local capture reaches (design verified SOUND, C# emission lens).
                    int hTotal = bound.F3HandlerBasePc is int cb ? bound.Paragraphs.Count - cb : 0;
                    w.Line($"bool[] __useActive = new bool[{hTotal}];   // method-local Format-3 handler re-entrancy guards (§14.9.49.4 GR2)");
                    U.Dispatch.EmitDispatchMethod(bound, w, "int __MDispatch(int __startPc, int __exitPc)",
                        m.Binding!.EntryPc, m.Binding!.EndPc,
                        m.Binding!.HandlerStartPc, m.Binding!.HandlerStartPc + m.Binding!.HandlerCount - 1);
                    using (w.Block("int __RunUse(int __id, int __startPc, int __endPc)")) U.Dispatch.EmitRunUseBody(w, ecModel: true);
                    U.Ec.EmitRunF3(w, asLocal: true);
                }
                else
                    U.Dispatch.EmitDispatchMethod(bound, w, "int __MDispatch(int __startPc, int __exitPc)", m.Binding!.EntryPc, m.Binding!.EndPc);
                dispatch.DispatchName = saved;
                if (methodF3)
                {
                    // The F3-method entry FLOOR (§9.10.1-C2): a method is a separate source element — its own unmatched
                    // raises must NOT be intercepted by the ACTIVATOR's WHEN. Raise the floor to the entry depth; on
                    // exit restore it and defensively balance the stack (matching the CALL boundary, ProgramTable).
                    w.Line("int __f3fl = ExceptionState.RaisePerformFloor(); int __f3pd = ExceptionState.PerformDepth;   // §9.10.1-C2 — isolate from the activator's frames");
                    w.Line($"try {{ __MDispatch({m.Binding!.EntryPc}, {m.Binding!.EndPc}); }} catch (MethodReturn) {{ }} "
                        + "finally {{ ExceptionState.RestorePerformFloor(__f3fl); ExceptionState.TrimPerformTo(__f3pd); }}   "
                        + "// GOBACK returns HERE (§14.9.18.4 GR4)");
                }
                else
                    w.Line($"try {{ __MDispatch({m.Binding!.EntryPc}, {m.Binding!.EndPc}); }} catch (MethodReturn) {{ }}   "
                        + "// GOBACK / falling off the last paragraph returns HERE (§14.9.18.4 GR4; deep-dive D8)");
            }
            ecState.UnitHasF3Perform = savedUnitF3P;
            dispatch.DeclCount = savedDeclCount;
            dispatch.F3HandlerBasePc = savedBase;
            // BY REFERENCE copy-out (§14.2.3 GR8) / RETURNING (§14.9.23.4 GR8). A Tier-B REDEFINES canonical's
            // storage IS its string backing (a width-correct image), not the suppressed root struct — write that
            // back / return that, else the generated C# names an undeclared local (review A/emission).
            foreach (var f in m.Binding!.Formals)
            {
                string src = fields.MethodRedefinesBackingDecl(f.Item) is { } bk ? bk.Name
                    : f.Item.IsGroup ? $"{f.Item.CsName}.AsImage()" : f.Item.CsName;
                w.Line($"{f.ParamName} = {src};   // BY REFERENCE copy-out (§14.2.3 GR8)");
            }
            if (m.Binding!.Returning is { } r)
            {
                string src = fields.MethodRedefinesBackingDecl(r) is { } bk ? bk.Name
                    : r.IsGroup ? $"{r.CsName}.AsImage()" : r.CsName;
                w.Line($"return {src};   // the invocation result (§14.9.23.4 GR8)");
            }
        }
        w.Line();
    }

    /// <summary>The C# (return-type, parameter-list) of a method or prototype — ONE builder shared by class
    /// method emission, interface member emission, and the covariant adapters, so the three can never drift
    /// (the same reasoning as the ONE DescriptionMismatch).</summary>
    private static (string RetType, string Sig) OoSignatureOf(OoMethodSymbol m)
    {
        string retType = m.Binding!.Returning is { } ret ? (OoStringCarried(ret) ? "string" : ret.ElementType) : "void";
        string sig = string.Join(", ", m.Binding!.Formals.Select(f =>
            $"ref {(OoStringCarried(f.Item) ? "string" : f.Item.ElementType)} {f.ParamName}"));
        return (retType, sig);
    }

    /// <summary>Emit one INTERFACE-ID as a C# interface (§11.6; D-I1): members are the prototypes' signatures
    /// (the SAME builder class methods use); the prototypes' numeric profiles and group struct types emit as
    /// interface STATICS (C# 8+) so cross-unit CONTENT conversions can qualify them.</summary>
    public void EmitInterfaceUnit(OoInterfaceSymbol iface, CodeWriter w)
    {
        var data = ifaceData[iface];
        // Interface units emit no statements, so the per-unit resolver is never consulted — and none can
        // exist: interfaces emit FIRST, before any program/class unit begins (the pre-9n code passed the
        // host's null-or-stale _refs field here, equally unread). The fresh renderers are unused but harmless.
        program.BeginUnit(w, data, null!);
        string bases = iface.Inherits.Count > 0
            ? " : " + string.Join(", ", iface.Inherits.Select(b => b.CsName))
            : "";
        using (w.Block($"public interface {iface.CsName}{bases}"))
        {
            new DataEmitter(Ctx).Emit();   // profiles + struct types only (LINKAGE roots are suppressed)
            foreach (var proto in iface.Prototypes)
            {
                var (retType, sig) = OoSignatureOf(proto);
                w.Line($"{retType} {proto.CsName}({sig});   // METHOD-ID {proto.Name} (prototype, §10.6.2 SR4)");
            }
        }
        w.Line();
    }

    /// <summary>Thin forward to THE one crossing-form predicate, <see cref="OoClassTable.StringCarried"/>
    /// (relocated to Binding in P6 Step 5 — the bind-phase override harmonize in <c>StorageFormPass</c> and these
    /// emit-side signature/marshaling renders must consult the SAME definition).</summary>
    private static bool OoStringCarried(DataItem item) => OoClassTable.StringCarried(item);


    /// <summary>Emit one bound INVOKE (deep-dive D5/D6 — the binder already resolved the call form and
    /// validated §14.8.2 strict conformance; this renders the type-preserving marshaling).</summary>
    public void EmitInvoke(BoundInvoke inv)
    {
        var w = Ctx.Writer;
        switch (inv.Form)
        {
            case InvokeForm.New:
                // §16.2.1 — the predefined NEW: the generated ctor allocates + VALUE-initializes (D4); the
                // reference is delivered through RETURNING (§14.9.23.4 GR8).
                w.Line(PlaceRenderer.Write(inv.Returning!, $"new {inv.ClassCsName}()") + "   // INVOKE … \"NEW\" RETURNING (§16.2.1)");
                return;
            case InvokeForm.NewSelf:
                // §16.2.1 GR1 — ACTIVE-CLASS creation in a factory method: the covariant __New override on
                // the RUNTIME factory creates the runtime class (SUPER "NEW" deliberately identical — the
                // restricted search finds the same predefined New, GR3/GR1).
                w.Line(PlaceRenderer.Write(inv.Returning!, "this.__New()")
                    + "   // INVOKE SELF|SUPER \"NEW\" (§16.2.1 — active-class creation via the covariant __New)");
                return;
            case InvokeForm.Instance:
            case InvokeForm.Self:
            case InvokeForm.Super:
            case InvokeForm.Factory:
                EmitInstanceInvoke(inv);
                return;
            default:
                w.Line(LoudStmt($"INVOKE call form '{inv.Form}'"));
                return;
        }
    }

    /// <summary>The instance-call marshaling (D6; §14.9.23.4 GR6/GR7a/GR8): every formal is a <c>ref</c>
    /// parameter — a plain field of matching storage passes DIRECTLY (aliasing; subscripts evaluate once at
    /// the call, the GR7a once-only rule); anything else lowers to a copy-in temp, <c>ref</c> the temp, and —
    /// for BY REFERENCE identifier args — a copy-out. BY REFERENCE crossings are TYPE-PRESERVING (the strict
    /// §14.8.2.3.2 bind rules); BY CONTENT crossings CONVERT into the formal's description per §14.8.2.3.3
    /// (COMPUTE/MOVE/SET), composing the formal's value/image through the OWNER class's internal profiles
    /// (<c>{OWNER}._P_n</c>). Order per GR8: the call, the BY REFERENCE copy-outs, then the RETURNING
    /// delivery — identifier-4's store is the FINAL effect (the review's overlap finding).</summary>
    private void EmitInstanceInvoke(BoundInvoke inv)
    {
        var w = Ctx.Writer;
        int id = Ctx.Names.NextOoInvoke();
        var argExprs = new List<string>();
        var post = new List<string>();

        var args = inv.Args ?? [];
        for (int i = 0; i < args.Count; i++)
        {
            var a = args[i];
            bool stringCarried = OoStringCarried(a.Formal);
            string qualProfile = a.Formal.Pic is { Category: PicCategory.Numeric, IsFloat: false }
                ? $"{inv.OwnerCsName}{(inv.Form is InvokeForm.Factory ? NamingConvention.FactorySuffix : "")}.{a.Formal.ProfileName}" : "";

            // The direct-ref fast path: a MemberPlace whose STORAGE form matches the parameter type exactly
            // (BY REFERENCE identifiers only — CONTENT always copies).
            if (a.Source is MemberPlace mp && a.WriteBack
                && (stringCarried
                    ? !mp.Item.IsGroup && OoStringCarried(mp.Item)
                    : !OoStringCarried(mp.Item))
                && !a.Formal.IsGroup && !mp.Item.IsGroup)
            {
                argExprs.Add($"ref {PlaceRenderer.Read(mp)}");
                continue;
            }

            string tmp = $"__iv{id}_{i}";
            if (a.Formal.IsGroup || (stringCarried && a.Source?.Item.IsGroup == true))
            {
                // The image crossing. BY REFERENCE allows a SMALLER formal (§14.8.2.2 rule 1 — a PREFIX of
                // the argument): pass the leading formal-width characters; the write-back below splices the
                // prefix back, preserving the argument's tail. CONTENT pads/truncates per MOVE.
                int fw = a.Formal.IsGroup ? a.Formal.ImageWidth : Math.Max(1, a.Formal.Pic!.Length);
                string read = a.Source is { } gsp ? CallEmitter.CallStringRead(gsp) : CsLiteral(a.StringLiteral ?? "");
                w.Line($"string {tmp} = {RuntimeApi.StrStore(read, $"{fw}")};");
            }
            else if (stringCarried)
                w.Line(a.Source is { } sp
                    ? $"string {tmp} = {OoStringReadOf(sp, a)};"
                    : a.StringLiteral is { } slit
                    // An ANY LENGTH formal sees the literal AT ITS OWN length (§13.18.2 GR1) — no width-fit.
                    ? $"string {tmp} = {(a.Formal.IsAnyLength ? CsLiteral(slit) : RuntimeApi.StrStore(CsLiteral(slit), $"{Math.Max(1, a.Formal.Pic!.Length)}"))};"
                    // A numeric literal into an image-stored numeric formal: compose the zoned image through
                    // the OWNER's internal profile (the review's cross-class rule — qualified, never bare).
                    : $"string {tmp} = {RuntimeApi.NumFormatDisplay(EmitText.UnscaledAtScale(a.NumericLiteral!, a.Formal.Pic!.Scale), qualProfile)};");
            else if (a.Formal.Pic is { Category: PicCategory.ObjectReference })
                w.Line($"{a.Formal.ElementType} {tmp} = {PlaceRenderer.Read(a.Source!)};");
            else if (a.Formal.Pic is { IsFloat: true })
                // Same-usage float (bind-enforced): read the float value directly — never through the
                // scaled-integer path (the review's silent-truncation finding).
                w.Line($"{a.Formal.ElementType} {tmp} = {PlaceRenderer.Read(a.Source!)};");
            else if (a.ByContent && a.Source is { } cp
                     && Num.AsNum(new BoundFieldOperand(cp), ReceiverContext.None) is var cx
                     && (cp.Item.Pic?.Digits != a.Formal.Pic!.Digits || cp.Item.Pic?.Scale != a.Formal.Pic.Scale
                         // …and the SIGN rule, which the digit/scale pair does not imply. §14.9.25.4 GR6d2b:
                         // "When an unsigned numeric item is the receiving item, the ABSOLUTE VALUE of the
                         // sending value is used, and no operational sign is generated for the receiving
                         // item." A signed argument whose description otherwise matches an UNSIGNED formal
                         // used to fall to the plain arm below, which copies the native value verbatim —
                         // ClrType is `long` for every fixed-point usage, so −7 arrived as −7. The reverse
                         // (unsigned → signed) needs no conversion: GR6d2a makes the sign positive and the
                         // value is unchanged, so testing `!=` here would convert a provable identity.
                         || (cp.Item.Pic is { Signed: true } && !a.Formal.Pic!.Signed)))
                // CONTENT numeric conversion (COMPUTE rules, §14.8.2.3.3 2a): rescale + truncate into the
                // formal's description through the OWNER's internal profile.
                w.Line($"{a.Formal.ElementType} {tmp} = ({a.Formal.ElementType}){RuntimeApi.NumStore(cx.Expr, $"{cx.Scale}", qualProfile)};");
            else
                w.Line(a.Source is { } np
                    ? $"{a.Formal.ElementType} {tmp} = ({a.Formal.ElementType})({Num.AsNum(new BoundFieldOperand(np), ReceiverContext.None).Expr});"
                    : $"{a.Formal.ElementType} {tmp} = ({a.Formal.ElementType}){RuntimeApi.NumStore(UnscaledLit(a.NumericLiteral!).Expr, $"{UnscaledLit(a.NumericLiteral!).Scale}", qualProfile)};");
            argExprs.Add($"ref {tmp}");

            if (!a.WriteBack || a.Source is not { } src) continue;
            // Copy-out to the CALLER's storage (BY REFERENCE — §14.2.3 GR8 at statement granularity).
            if (a.Formal.IsGroup || src.Item.IsGroup)
            {
                int fw = a.Formal.IsGroup ? a.Formal.ImageWidth : Math.Max(1, a.Formal.Pic!.Length);
                // The §14.8.2.2 rule-1 prefix: splice the formal's characters back over the argument's
                // LEADING positions, preserving the tail beyond the formal's width.
                post.Add(CallEmitter.CallStringWrite(src,
                    // to-the-end read from fw+1 — the OMITTED-length sentinel (NOT −1, which now denotes a specified
                    // negative length that raises EC-BOUND-REF-MOD; review C14).
                    $"{tmp} + {RuntimeApi.StrRefMod(CallEmitter.CallStringRead(src), $"{fw + 1}", RuntimeApi.OmittedRefModLength)}"));
            }
            else if (src is RefModPlace)
                post.Add(PlaceRenderer.Write(src, tmp));   // RefModPlace.Write splices the window (§8.4.2.4)
            else if (stringCarried)
                post.Add(OoStringCarried(src.Item) ? PlaceRenderer.Write(src, tmp) : PlaceRenderer.Write(new NumericImagePlace(src), tmp));
            else
                post.Add(src.Item.StoreAsImage
                    ? PlaceRenderer.Write(src, RuntimeApi.NumFormatDisplay(tmp, src.Item.ProfileName))
                    : PlaceRenderer.Write(src, tmp));
        }

        string target = inv.Form switch
        {
            InvokeForm.Self => "this",
            InvokeForm.Super => "base",
            // The factory singleton is never null — no GR5 guard (brief D11); virtual dispatch through the
            // factory hierarchy realizes §9.3.6 factory resolution.
            InvokeForm.Factory => $"{inv.ClassCsName}{NamingConvention.FactorySuffix}.{NamingConvention.FactoryInstanceField}",
            _ => RuntimeApi.ObjRequireNonNull(PlaceRenderer.Read(inv.Receiver!)),
        };
        string call = $"{target}.{inv.MethodCsName}(" + string.Join(", ", argExprs) + ")";

        if (inv.ReturningSource is { } rs && inv.Returning is { } recv)
        {
            // GR8 — capture the result AT RETURN, flush the BY REFERENCE copy-outs, and store into
            // identifier-4 LAST (the final effect of the INVOKE — a receiver overlapping a temp-lowered
            // argument must see the argument's write-back first).
            string tmp = $"__ivr{id}";
            bool retString = OoStringCarried(rs);
            w.Line($"var {tmp} = {call};   // INVOKE (§14.9.23; null receiver → EC-OO-NULL, GR5)");
            foreach (var pLine in post) w.Line(pLine);
            if (rs.IsGroup || recv.Item.IsGroup)
                w.Line(CallEmitter.CallStringWrite(inv.Returning, tmp));
            else if (recv is RefModPlace)
                w.Line(PlaceRenderer.Write(recv, tmp));
            else if (retString == OoStringCarried(recv.Item))
                // ANY LENGTH at the delivery boundary (ISO §14.8.3.3 rules 4/5; §13.18.2 GR1): a varying-length
                // SENDER delivers width-fitted into a fixed receiver (rule 5 — its length "considered to match");
                // a varying-length RECEIVER stores at its own current length (its n is fixed by ITS activation).
                w.Line(PlaceRenderer.Write(recv,
                    recv.Item.IsAnyLength
                        ? RuntimeApi.StrStore(tmp, $"{PlaceRenderer.Read(recv)}.Length")
                    : rs.IsAnyLength && recv.Item.Pic is { } rvp
                        ? RuntimeApi.StrStore(tmp, $"{Math.Max(1, rvp.Length)}")
                    : tmp));
            else if (retString)   // string-carried result into native-numeric storage
                w.Line(PlaceRenderer.Write(recv, $"({recv.Item.ElementType}){RuntimeApi.NumParseDisplay(tmp, recv.Item.ProfileName)}"));
            else                  // native result into image-stored numeric storage
                w.Line(PlaceRenderer.Write(recv, RuntimeApi.NumFormatDisplay(tmp, recv.Item.ProfileName)));
        }
        else
        {
            w.Line($"{call};   // INVOKE (§14.9.23; null receiver → EC-OO-NULL, §14.9.23.4 GR5)");
            foreach (var pLine in post) w.Line(pLine);
        }
        EmitInvokePickup();   // §14.6.13.1.5 — a method GOBACK RAISING obj is consumed HERE (after GR8)
    }

    /// <summary>The copy-in read of an identifier argument for a STRING-CARRIED formal: a reference-modified
    /// place reads its window verbatim (§8.4.2.4 — the operand IS elementary alphanumeric); a string-stored
    /// item reads directly; a native display-numeric item formats through its OWN profile (caller-side). A
    /// CONTENT crossing normalizes to the formal's width (MOVE pad/truncate).</summary>
    /// <summary>The INVOKE-site propagation pickup (D-EO6): a method GOBACK/EXIT … RAISING stages; the
    /// ACTIVATING site consumes — after the RETURNING delivery and copy-outs (GR1b ordering). Instance/
    /// Self/Super/Factory + UNIVERSAL dispatches all pick up; NEW needs none (the generated ctor runs no
    /// user statements, D4). Gated on <c>EcState.Active</c>, which spans class units.</summary>
    private void EmitInvokePickup() => U.Call.EmitPropagationPickup();

    private string OoStringReadOf(Place sp, BoundInvokeArg a)
    {
        string read = sp is RefModPlace ? PlaceRenderer.Read(sp)
            : OoStringCarried(sp.Item) ? PlaceRenderer.Read(sp)
            : PlaceRenderer.Read(new NumericImagePlace(sp));
        // An ANY LENGTH formal takes the argument's characters AT the argument's length (ISO §13.18.2 GR1 —
        // n = the length of the corresponding argument), so the CONTENT copy must NOT width-normalize to the
        // formal's Pic.Length (1); the raw read IS the width-correct crossing.
        return a.ByContent && a.Formal.Pic is { } fp && fp.Category is PicCategory.Alphanumeric
                && !a.Formal.IsAnyLength
            ? RuntimeApi.StrStore(read, $"{Math.Max(1, fp.Length)}")
            : read;
    }

}
