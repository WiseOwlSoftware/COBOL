// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Runtime;

// ═══════════════════════════════════════════════════════════════════════════════════════════════════════════════
//  The inter-program runtime (COBOLNET_INTERPROGRAM_DESIGN D1–D5; ISO/IEC 1989:2023 §14.9.4 CALL / §14.9.5 CANCEL /
//  §14.2 procedure-division parameter passing / §8.4.6.3 program-name scope / §8.6.7 EXTERNAL sharing):
//    • ManagedPointer / ManagedPointer<T> — the ONE managed-reference carrier (design D1; the typed-native
//      re-implementation of the legacy ManagedPointer — internally the typed carrier, public name kept per the
//      settled SSOT §18 #12). Serves BY REFERENCE arguments and LINKAGE formals today; USAGE POINTER / ADDRESS OF /
//      BASED / ALLOCATE reuse the same carrier when those 2002 slices land (singular-pattern rule).
//    • ICobolProgram / CobolArg — the uniform opaque calling ABI (design D2): an ordered (mode, carrier, meta)
//      argument list every compiled program-class accepts, so dynamic CALL (identifier) and cross-assembly CALL
//      need no knowledge of the callee's LINKAGE. Same-assembly literal CALL is resolved at registry speed; the
//      direct typed fast path remains a pure optimization over this ABI.
//    • ProgramReturn — the called-program return signal (settled SSOT §18 #10): GOBACK / EXIT PROGRAM in a called
//      program raises it; it is caught at THAT program's activation entry, never crossing a CALL boundary the way
//      StopRun (run-unit termination, §14.9.43) deliberately does.
//    • ProgramRegistry — program-name resolution honoring §8.4.6.3 (containment + COMMON visibility), the
//      §14.6.2.3 state model (last-used cached instance / INITIAL fresh instance / RECURSIVE per activation),
//      and CANCEL semantics (§14.9.5 GR3/4/5/7/8/9/12).
//    • ExternalStore — one storage copy per external name per run unit (§8.6.7 / §13.18.22); NOT reset by CANCEL
//      (§14.9.5 GR8).
// ═══════════════════════════════════════════════════════════════════════════════════════════════════════════════

/// <summary>
/// The called-program return signal (ISO §14.9.18 GR2 / §14.9.14 GR3): GOBACK (or EXIT PROGRAM in a called
/// program) raises it; the raising program's activation entry catches it and returns control to the activator.
/// In a MAIN program the activation entry is the run-unit wrapper, so a main-program GOBACK terminates the run
/// unit (§14.9.18 GR3 — GOBACK in a main program acts as a STOP statement). Distinct from <see cref="StopRun"/>,
/// which unwinds the WHOLE run unit from anywhere (§14.9.43).
/// </summary>
public sealed class ProgramReturn : Exception;

/// <summary>
/// A CALL/CANCEL-machinery failure (ISO §14.9.4.4 GR3b–h): program not found (EC-PROGRAM-NOT-FOUND), a re-entry
/// of an active non-RECURSIVE program (GR3f, EC-PROGRAM-RECURSIVE-CALL), CANCEL of an active program (§14.9.5
/// GR5, EC-PROGRAM-CANCEL-ACTIVE), or a reference to an omitted argument (GR12, EC-PROGRAM-ARG-OMITTED). At
/// <c>--std 85</c> there is no EC machinery — a CALL site with an ON OVERFLOW phrase converts this to the
/// overflow branch (the 85 surface); without one, the run unit terminates loudly (abnormal termination).
/// <paramref name="ecName"/> carries the Table 13 level-3 EC-PROGRAM-* name so a CALL site compiled with
/// EC-PROGRAM checking enabled (>>TURN, §7.3.25) can set the last exception status and run the §14.9.49 F3
/// declarative selection over the precise condition.
/// </summary>
public sealed class CobolCallException(string message, string ecName = "EC-PROGRAM-IMP") : Exception(message)
{
    /// <summary>The Table 13 level-3 exception-name of this failure (uppercase).</summary>
    public string EcName { get; } = ecName;
}

/// <summary>How a CALL argument is passed (ISO §14.9.4 / §14.2.3 GR8–10).</summary>
public enum CobolPassMode
{
    /// <summary>BY REFERENCE — the callee operates as if the formal occupies the caller's storage (§14.2.3 GR8).</summary>
    Reference,
    /// <summary>BY CONTENT — a copy allocated at CALL initiation, then treated as if by reference (§14.2.3 GR9).</summary>
    Content,
    /// <summary>BY VALUE — a converted value copy (§14.2.3 GR10; COBOL-2002+).</summary>
    Value,
}

/// <summary>
/// The ONE managed-reference carrier base (COBOLNET_INTERPROGRAM_DESIGN D1). Untyped view for the opaque ABI;
/// the typed accessor lives on <see cref="ManagedPointer{T}"/>. <see cref="Null"/> is the NULL pointer state
/// (an OMITTED argument, an unset data-pointer, a freed BASED item).
/// </summary>
public abstract class ManagedPointer
{
    /// <summary>The NULL carrier (OMITTED argument / NULL pointer — ISO §14.9.4.4 GR11–12).</summary>
    public static readonly ManagedPointer Null = new NullManagedPointer();

    /// <summary>True for the NULL carrier.</summary>
    public virtual bool IsNull => false;

    private sealed class NullManagedPointer : ManagedPointer
    {
        public override bool IsNull => true;
    }
}

/// <summary>
/// The typed managed-reference carrier (design D1 — internally the typed <c>ManagedRef&lt;T&gt;</c>; the public
/// name <c>ManagedPointer</c> is kept, SSOT §18 #12). Two construction modes: <see cref="OverField"/> — an
/// accessor over the owner's NATIVE field (WORKING-STORAGE stays unboxed; only the genuine alias carries
/// indirection), and <see cref="Cell"/> — a standalone storage cell (BY CONTENT copies, literals, ALLOCATE).
/// </summary>
/// <typeparam name="T">The carried storage type — <see cref="long"/> for a native fixed-point item,
/// <see cref="string"/> for character storage (alphanumeric / edited / zoned image / whole-group image).</typeparam>
public sealed class ManagedPointer<T> : ManagedPointer
{
    private readonly Func<T> _get;
    private readonly Action<T> _set;

    private ManagedPointer(Func<T> get, Action<T> set)
    {
        _get = get;
        _set = set;
    }

    /// <summary>An accessor carrier over a native field: reads/writes go straight through to the owner's storage
    /// — the BY REFERENCE "same storage area" semantics (ISO §14.2.3 GR8) with zero indirection on the owner's
    /// own accesses.</summary>
    public static ManagedPointer<T> OverField(Func<T> get, Action<T> set) => new(get, set);

    /// <summary>A standalone storage cell seeded with <paramref name="initial"/> — the BY CONTENT copy
    /// (ISO §14.2.3 GR9: "a record allocated by the activating element"), also the ALLOCATE backing.</summary>
    public static ManagedPointer<T> Cell(T initial)
    {
        var box = new T[1] { initial };
        return new(() => box[0], v => box[0] = v);
    }

    /// <summary>The referenced storage's current value (get) / store into the referenced storage (set).</summary>
    public T Value
    {
        get => _get();
        set => _set(value);
    }
}

/// <summary>
/// One CALL argument crossing the opaque ABI (design D2): the pass mode, the carrier, and the caller-side
/// numeric meta (digit count + scale) the callee-side adapters need to reinterpret a native-<c>long</c> carrier
/// through a differently-scaled or character-shaped formal (the D5-sanctioned category boundary).
/// </summary>
/// <param name="Mode">The pass mode (ISO §14.9.4.4 GR5 transitivity resolved at bind time).</param>
/// <param name="Carrier">The storage carrier (<see cref="ManagedPointer.Null"/> for OMITTED, GR11).</param>
/// <param name="Digits">Caller PICTURE digit count for a numeric argument; 0 for character storage.</param>
/// <param name="Scale">Caller PICTURE scale for a numeric argument; 0 for character storage.</param>
public readonly record struct CobolArg(CobolPassMode Mode, ManagedPointer Carrier, int Digits, int Scale);

/// <summary>
/// The uniform program ABI every compiled program class implements (design D2 — the typed analog of the
/// rejected byte <c>Entry(ManagedPointer[])</c>). <see cref="Call"/> activates the program as a CALLed program
/// (positional formal mapping, §14.2.3 GR2); <see cref="Activate"/> runs it as the run-unit main program;
/// <see cref="CloseFiles"/> closes this program's file connectors (CANCEL §14.9.5 GR9 implicit CLOSE).
/// </summary>
public interface ICobolProgram
{
    /// <summary>Activate as a CALLed program: map <paramref name="args"/> positionally onto the LINKAGE formals
    /// (ISO §14.2.3 GR2 — correspondence is positional, never by name), run, and deliver the RETURNING value (if
    /// any) through <paramref name="returning"/> (§14.2.3 GR7).</summary>
    void Call(CobolArg[] args, ManagedPointer? returning);

    /// <summary>Activate as the run-unit's main program (no arguments; LINKAGE unbound, ISO §13.7.4 GR3).</summary>
    void Activate();

    /// <summary>Close every file connector this program owns (CANCEL GR9 / run-unit termination §14.6.11).</summary>
    void CloseFiles();
}

/// <summary>
/// Callee-side positional argument adapters (design D2/D5): each maps <c>args[i]</c> onto a formal parameter's
/// carrier shape. Same-shape carriers pass through untouched (fully typed aliasing); a category mismatch (e.g. a
/// caller <c>PIC X(4)</c> viewed by the callee as <c>PIC 9(4)</c>) builds a CONVERTING view over the caller's
/// storage — the one sanctioned transient-character boundary (design D5; legal COBOL exercised by NIST), never a
/// persisted byte image. A missing / OMITTED argument yields a carrier that fails loud on first reference
/// (ISO §14.9.4.4 GR12 — EC-PROGRAM-ARG-OMITTED when the EC subsystem lands).
/// </summary>
public static class CobolArgAdapt
{
    /// <summary>True when argument <paramref name="i"/> was supplied and is not OMITTED (ISO §14.9.4.4 GR11 —
    /// the omitted-argument condition is the negation of this).</summary>
    public static bool Present(CobolArg[] args, int i) => i < args.Length && !args[i].Carrier.IsNull;

    /// <summary>Adapt argument <paramref name="i"/> to a NUMERIC formal described by <paramref name="formal"/>
    /// (the callee's profile) at <paramref name="formalScale"/>. A native-<c>long</c> carrier at the same scale
    /// aliases directly; a different scale gets a rescaling view; a character carrier gets a zoned decode/encode
    /// view through the CALLEE's profile — the same storage characters reinterpreted (§14.2.3 GR8; design D5).</summary>
    public static ManagedPointer<long> Num(CobolArg[] args, int i, NumProfile formal, int formalScale)
    {
        if (!Present(args, i)) return Omitted<long>(i);
        switch (args[i].Carrier)
        {
            case ManagedPointer<long> lp when args[i].Scale == formalScale:
                return lp;   // same shape, same scale — pure typed aliasing (the common conforming case)
            case ManagedPointer<long> lp:
                int callerScale = args[i].Scale;
                return ManagedPointer<long>.OverField(
                    () => (long)CobolNum.Rescale(lp.Value, callerScale, formalScale, CobolRounding.Truncation),
                    v => lp.Value = (long)CobolNum.Rescale(v, formalScale, callerScale, CobolRounding.Truncation));
            case ManagedPointer<string> sp:
                // The D5 boundary: the caller's CHARACTER storage viewed as the callee's zoned numeric — decode
                // and re-encode through the callee's profile on each access (same storage area, §14.2.3 GR8).
                return ManagedPointer<long>.OverField(
                    () => (long)CobolNum.ParseDisplay(sp.Value, formal),
                    v => sp.Value = CobolNum.FormatDisplay(v, formal));
            default:
                return Omitted<long>(i);
        }
    }

    /// <summary>Adapt argument <paramref name="i"/> to a CHARACTER formal of <paramref name="width"/> characters.
    /// A character carrier gets a width-window view: reads are the first <paramref name="width"/> positions
    /// (space-padded when the caller's storage is shorter); writes SPLICE into the caller's storage, preserving
    /// the caller's own width invariant (§14.2.3 GR8 — the callee touches only its formal's character positions).
    /// A native-<c>long</c> carrier gets a digit-image view via the caller's digit meta (D5 boundary).</summary>
    public static ManagedPointer<string> Text(CobolArg[] args, int i, int width)
    {
        if (!Present(args, i)) return Omitted<string>(i);
        switch (args[i].Carrier)
        {
            case ManagedPointer<string> sp:
                return ManagedPointer<string>.OverField(
                    () => CobolString.Store(sp.Value, width),
                    v => sp.Value = CobolString.SpliceInto(sp.Value, 1, Math.Min(width, sp.Value?.Length ?? width), v));
            case ManagedPointer<long> lp:
                int digits = args[i].Digits > 0 ? args[i].Digits : width;
                var prof = new NumProfile
                {
                    Digits = digits,
                    FractionDigits = Math.Max(0, args[i].Scale),
                    Signed = false,
                    Truncation = NumericTruncation.DigitCount,
                };
                return ManagedPointer<string>.OverField(
                    () => CobolString.Store(CobolNum.FormatDisplay(lp.Value, prof), width),
                    v => lp.Value = (long)CobolNum.ParseDisplay(v, prof));
            default:
                return Omitted<string>(i);
        }
    }

    /// <summary>Deliver a RETURNING value to the caller's RETURNING carrier (ISO §14.2.3 GR7 — at termination the
    /// returning item's value transfers to the activating element's RETURNING identifier). Null-tolerant: a CALL
    /// without RETURNING discards the value (deep-dive edge case).</summary>
    public static void StoreReturn(ManagedPointer? ret, long value)
    {
        if (ret is ManagedPointer<long> lp) lp.Value = value;
        else if (ret is ManagedPointer<string> sp) sp.Value = value.ToString();
    }

    /// <summary>String-shaped RETURNING delivery (see <see cref="StoreReturn(ManagedPointer?, long)"/>).</summary>
    public static void StoreReturn(ManagedPointer? ret, string value)
    {
        if (ret is ManagedPointer<string> sp) sp.Value = value;
        else if (ret is ManagedPointer<long> lp && long.TryParse(value.Trim(), out long v)) lp.Value = v;
    }

    /// <summary>A carrier whose first reference fails loud: the formal's argument was omitted or absent
    /// (ISO §14.9.4.4 GR12 — referencing an omitted parameter is the EC-PROGRAM-ARG-OMITTED condition).</summary>
    private static ManagedPointer<T> Omitted<T>(int position) => ManagedPointer<T>.OverField(
        () => throw new CobolCallException(
            $"reference to omitted/absent CALL argument #{position + 1} (ISO §14.9.4.4 GR12 — EC-PROGRAM-ARG-OMITTED)", "EC-PROGRAM-ARG-OMITTED"),
        _ => throw new CobolCallException(
            $"store into omitted/absent CALL argument #{position + 1} (ISO §14.9.4.4 GR12 — EC-PROGRAM-ARG-OMITTED)", "EC-PROGRAM-ARG-OMITTED"));
}

/// <summary>
/// The run-unit EXTERNAL data store (ISO §8.6.7 / §13.18.22): ONE storage copy per external name for the whole
/// run unit, represented as the record's character image (the same Tier-B string-canonical shape the data model
/// uses for shared storage — never a persisted byte substrate). Every program describing the same external name
/// windows the same cell; CANCEL does NOT reset it (§14.9.5 GR8). The §13.18.22 GR6 conformance checks
/// (same byte count / same VALUE across describers) belong to the §14.8.4 EC machinery — not enforced here yet.
/// </summary>
public static class ExternalStore
{
    /// <summary>The one mutable holder per external name — <see cref="Ref"/> is a FIELD so the generated
    /// <c>ref</c>-returning bridge property can alias it (<c>ref ExternalStore.Cell(...).Ref</c>).</summary>
    public sealed class Holder
    {
        /// <summary>The external record's character image (its full width, every describer windows it).</summary>
        public string Ref = "";
    }

    private static readonly Dictionary<string, Holder> Cells = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The run-unit cell for <paramref name="name"/>, created with <paramref name="initialImage"/> on
    /// first reference (ISO §14.6.2.3.2 — external data takes its initial state once per run unit).</summary>
    public static Holder Cell(string name, string initialImage)
    {
        if (!Cells.TryGetValue(name, out var h)) Cells[name] = h = new Holder { Ref = initialImage };
        return h;
    }

    /// <summary>Drop every cell (run-unit start hygiene; called from <see cref="ProgramRegistry.Reset"/>).</summary>
    public static void Reset() => Cells.Clear();
}

/// <summary>
/// The run-unit program registry: every compiled program unit registers at run-unit start (name, containment,
/// COMMON / INITIAL / RECURSIVE attributes, instance factory); CALL resolves names per the §8.4.6.3 scope rules
/// and drives the §14.6.2.3 state model; CANCEL implements §14.9.5. Instances ARE the state: a plain program's
/// cached singleton realizes last-used persistence (§8.6.4 / §14.6.2.3.3); dropping the instance realizes
/// initial-state-on-next-CALL (§14.9.5 GR3); a fresh instance per activation realizes INITIAL (§14.6.2.3.2)
/// and RECURSIVE (deep-dive D3/D4). In-assembly static registration is the primary profile; an unresolved
/// outermost name additionally probes the application directory for a sibling compiled module
/// (<c>&lt;name&gt;.dll</c>) and invokes its public <c>__CobolModule.Register()</c> registrar — the
/// implementation-defined §14.9.4.4 GR3b "locate the program" mechanism (owner-approved resolution of the
/// deep-dive's open question; a prebuilt-static-registry profile remains possible for AOT/trimming, where the
/// probe simply never fires because every name is pre-registered).
/// </summary>
public static class ProgramRegistry
{
    private sealed class Node
    {
        public required string Path;        // containment path id, e.g. "OUTER/INNER" (unique run-unit-wide)
        public required string Name;        // the PROGRAM-ID name CALL/CANCEL resolve (per-outermost unique, §8.4.6.3)
        public string? ParentPath;
        public bool Initial, Common, Recursive;
        public required Func<ICobolProgram?, ICobolProgram> Factory;   // parent instance → new instance
        public ICobolProgram? Instance;     // the cached (last-used) instance; null = initial state on next CALL
        public int Active;                  // activation depth (GR3f recursion check; GR5 cancel-active check)
        public List<Node> Children = [];    // contained programs, source order (GR4 cancels in REVERSE)
    }

    private static readonly Dictionary<string, Node> ByPath = new(StringComparer.OrdinalIgnoreCase);
    private static readonly List<Node> Order = [];
    private static readonly HashSet<string> ProbedModules = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Clear all registrations and the external store (run-unit start).</summary>
    public static void Reset()
    {
        ByPath.Clear();
        Order.Clear();
        ProbedModules.Clear();
        ExternalStore.Reset();
    }

    /// <summary>Register one program unit (emitted once per unit at run-unit start, containers before containees).</summary>
    public static void Register(
        string path, string name, string? parentPath,
        bool initial, bool common, bool recursive,
        Func<ICobolProgram?, ICobolProgram> factory)
    {
        var node = new Node
        {
            Path = path, Name = name, ParentPath = parentPath,
            Initial = initial, Common = common, Recursive = recursive, Factory = factory,
        };
        ByPath[path] = node;
        Order.Add(node);
        if (parentPath is not null && ByPath.TryGetValue(parentPath, out var parent)) parent.Children.Add(node);
    }

    /// <summary>Run the run unit's MAIN program (the first program of the compilation group).</summary>
    public static void RunMain(string path)
    {
        var n = ByPath[path];
        var inst = n.Instance ??= n.Factory(null);
        n.Active++;
        try { inst.Activate(); }
        finally { n.Active--; }
        // A GOBACK … RAISING in the MAIN program stages a propagation whose "activator" is the run-unit
        // boundary itself — apply the activation-boundary default here (§14.9.18 GR; §14.6.13.1.3).
        ApplyPropagationDefault();
    }

    /// <summary>Apply the activation-boundary default to an exception condition staged by the returning
    /// element's <c>GOBACK / EXIT PROGRAM … RAISING</c> (ISO §14.9.18 GR) when the activating CALL site emitted
    /// no pickup of its own (an EC-free caller — checking is off there, so the condition is not raised in the
    /// activating element; the §14.6.13.1.3 #8 implementor choice, recorded in the conditions-exceptions
    /// deep-dive): a FATAL staged condition terminates the run unit loudly; a nonfatal one stands in the
    /// last-exception status (§14.6.13.1.4) and execution continues.</summary>
    private static void ApplyPropagationDefault()
    {
        if (ExceptionState.TakePropagated(out string pn, out bool pf) && pf)
            throw new CobolFatalException(pn, "exception condition propagated by GOBACK/EXIT PROGRAM RAISING "
                + "into an activator without exception checking (ISO 14.9.18; 14.6.13.1.3 #8 - this "
                + "implementation terminates)");
    }

    /// <summary>
    /// Execute one CALL (ISO §14.9.4.4): resolve <paramref name="name"/> from <paramref name="callerPath"/> per
    /// the §8.4.6.3 scope rules (GR3b), enforce the non-recursive re-entry rule (GR3f), pick the instance per the
    /// §14.6.2.3 state model, activate, and apply the INITIAL program's implicit CANCEL on return (§14.9.18 GR2).
    /// Failures raise <see cref="CobolCallException"/> — the call site's ON OVERFLOW / ON EXCEPTION phrase (when
    /// present) converts it to the exception branch (GR3h); otherwise the run unit terminates loudly.
    /// </summary>
    public static void CallProgram(string name, string callerPath, CobolArg[] args, ManagedPointer? returning,
        bool siteHandlesPropagation = false)
    {
        var n = ResolveVisible(name, callerPath)
            ?? throw new CobolCallException(
                $"CALL '{name?.Trim()}': program not found in the run unit (ISO §14.9.4.4 GR3b — EC-PROGRAM-NOT-FOUND)",
                "EC-PROGRAM-NOT-FOUND");
        if (n.Active > 0 && !n.Recursive)
            throw new CobolCallException(
                $"CALL '{n.Name}': program is already active and has no RECURSIVE attribute (ISO §14.9.4.4 GR3f — EC-PROGRAM-RECURSIVE-CALL)",
                "EC-PROGRAM-RECURSIVE-CALL");

        ICobolProgram inst;
        if (n.Initial || n.Recursive)
        {
            // INITIAL: initial state on EVERY activation (§14.6.2.3.2) — a fresh instance IS the initial state.
            // RECURSIVE: per-activation instance (deep-dive D3/D4).
            inst = n.Factory(ParentInstance(n));
            n.Instance = inst;   // contained-program factories reach their container through the registry
            if (n.Initial) CancelContained(n);   // contained programs re-initialize too (ISO §11.10.4 GR3)
        }
        else
            inst = n.Instance ??= n.Factory(ParentInstance(n));   // cached singleton — last-used state (§14.6.2.3.3)

        n.Active++;
        try { inst.Call(args, returning); }
        finally { n.Active--; }

        if (n.Initial)
        {
            // "If the program … is an initial program, an implicit CANCEL statement referencing that program is
            // executed upon return" (ISO §14.9.18 GR2): close its files (§14.9.5 GR9), cascade (GR4), drop state.
            inst.CloseFiles();
            CancelContained(n);
            n.Instance = null;
        }

        // The callee may have staged an exception condition via GOBACK/EXIT PROGRAM … RAISING (§14.9.18 GR).
        // An EC-active CALL site consumes it itself (siteHandlesPropagation — the generated pickup runs the
        // §14.9.49 F3 selection and honors RESUME); otherwise apply the boundary default here.
        if (!siteHandlesPropagation) ApplyPropagationDefault();
    }

    /// <summary>
    /// Execute one CANCEL target (ISO §14.9.5): a zero-length name is a no-op (GR12); a name not in the run unit
    /// is a no-op (the never-made-available case); an ACTIVE program raises (GR5 — EC-PROGRAM-CANCEL-ACTIVE, the
    /// program is NOT canceled); otherwise contained programs cancel in reverse source order (GR4), the
    /// program's open file connectors close implicitly (GR9 — no optional phrases, no USE procedures), and the
    /// next CALL finds the program in its initial state (GR3). EXTERNAL data is untouched (GR8). A never-called
    /// or already-canceled program is a no-op (GR7).
    /// </summary>
    public static void Cancel(string name, string callerPath)
    {
        string n = name?.Trim() ?? "";
        if (n.Length == 0) return;   // §14.9.5 GR12
        var node = ResolveVisible(n, callerPath);
        if (node is null) return;
        CancelNode(node);
    }

    private static void CancelNode(Node n)
    {
        if (n.Active > 0)
            throw new CobolCallException(
                $"CANCEL '{n.Name}': program is active (ISO §14.9.5 GR5 — EC-PROGRAM-CANCEL-ACTIVE; not canceled)",
                "EC-PROGRAM-CANCEL-ACTIVE");
        for (int i = n.Children.Count - 1; i >= 0; i--)   // GR4 — contained programs, REVERSE source order
            CancelNode(n.Children[i]);
        if (n.Instance is { } inst)
        {
            inst.CloseFiles();   // GR9 — implicit CLOSE of every open internal file connector
            n.Instance = null;   // GR3 — the next CALL finds the initial state (GR8: ExternalStore untouched)
        }
        // no instance → never called / already canceled → no-op (GR7)
    }

    private static void CancelContained(Node n)
    {
        for (int i = n.Children.Count - 1; i >= 0; i--) CancelNode(n.Children[i]);
    }

    private static ICobolProgram? ParentInstance(Node n) =>
        n.ParentPath is null ? null
        : ByPath.TryGetValue(n.ParentPath, out var p)
            ? p.Instance ?? throw new CobolCallException(
                $"internal: contained program '{n.Name}' activated while its container is not instantiated")
            : null;

    /// <summary>
    /// Resolve a CALL/CANCEL program-name from the calling program per ISO §8.4.6.3: (1) a program DIRECTLY
    /// contained in the caller; (2) the caller itself when RECURSIVE (self-call); (3) a COMMON program contained
    /// in a (transitive) container of the caller — except from within that COMMON program or its containees
    /// unless it is recursive; (4) an OUTERMOST program of the run unit (callable from anywhere).
    /// </summary>
    private static Node? ResolveVisible(string? name, string? callerPath)
    {
        string target = name?.Trim() ?? "";
        if (target.Length == 0) return null;
        Node? caller = callerPath is not null && ByPath.TryGetValue(callerPath, out var c) ? c : null;

        if (caller is not null)
        {
            foreach (var child in caller.Children)                                   // rule 1
                if (NameEquals(child.Name, target)) return child;
            if (NameEquals(caller.Name, target) && caller.Recursive) return caller;  // rule 2
            for (var anc = ParentOf(caller); anc is not null; anc = ParentOf(anc))   // rule 3 — nearest container first
                foreach (var sib in anc.Children)
                {
                    if (!sib.Common || !NameEquals(sib.Name, target)) continue;
                    bool onOwnChain = caller.Path.Equals(sib.Path, StringComparison.OrdinalIgnoreCase)
                        || caller.Path.StartsWith(sib.Path + "/", StringComparison.OrdinalIgnoreCase);
                    if (!onOwnChain || sib.Recursive) return sib;
                }
        }
        foreach (var n in Order)                                                     // rule 4 — outermost programs
            if (n.ParentPath is null && NameEquals(n.Name, target)) return n;

        // Rule-4 fallthrough: the run unit may be composed of SEPARATELY COMPILED modules ("a run unit contains
        // one or more runtime modules", ISO §14.6.1; §14.9.4.4 GR3b — the runtime system "attempts to locate"
        // the called program; the locating mechanics beyond the §8.4.6.3 name scope are implementor-defined).
        // Probe the application directory for a sibling compiled module named after the program, invoke its
        // public __CobolModule.Register() registrar (generated classes are internal — the registrar IS the
        // discovery surface), and retry rule 4 once. Probed names are cached, hit or miss — one I/O probe per
        // name per run unit.
        if (ProbeSiblingModule(target))
            foreach (var n in Order)
                if (n.ParentPath is null && NameEquals(n.Name, target)) return n;
        return null;
    }

    /// <summary>Load the sibling compiled module <c>&lt;name&gt;.dll</c> from <see cref="AppContext.BaseDirectory"/>
    /// (exact name first, then a case-insensitive scan — Linux filesystems are case-sensitive) into the default
    /// <see cref="System.Runtime.Loader.AssemblyLoadContext"/> and run its <c>__CobolModule.Register()</c>.
    /// Returns true when a registrar ran (the caller re-resolves); a missing file / foreign dll / load failure
    /// is a quiet false — the CALL then raises the ordinary EC-PROGRAM-NOT-FOUND surface.</summary>
    private static bool ProbeSiblingModule(string name)
    {
        if (!ProbedModules.Add(name)) return false;   // already probed this run unit (negative/positive cache)
        try
        {
            string dir = AppContext.BaseDirectory;
            string path = System.IO.Path.Combine(dir, name + ".dll");
            if (!System.IO.File.Exists(path))
                path = System.IO.Directory.EnumerateFiles(dir, "*.dll").FirstOrDefault(f =>
                    string.Equals(System.IO.Path.GetFileNameWithoutExtension(f), name,
                        StringComparison.OrdinalIgnoreCase)) ?? "";
            if (path.Length == 0) return false;
            var asm = System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
            var register = asm.GetType("__CobolModule")?.GetMethod("Register", Type.EmptyTypes);
            if (register is null) return false;   // not a COBOL.NET module — no registrar surface
            register.Invoke(null, null);
            return true;
        }
        catch
        {
            return false;   // an unloadable/foreign dll is simply "not found" (§14.9.4.4 GR3b)
        }
    }

    private static Node? ParentOf(Node n) =>
        n.ParentPath is not null && ByPath.TryGetValue(n.ParentPath, out var p) ? p : null;

    private static bool NameEquals(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
