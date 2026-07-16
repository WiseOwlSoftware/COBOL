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
        CobolModule.Reset();   // the FUNCTION MODULE-NAME call-name stack (§15.65)
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
        CobolModule.PushMain(n.Name);   // TOP-LEVEL / the run-unit main (§15.65.4 r5/r10)
        try { inst.Activate(); }
        finally { n.Active--; CobolModule.Pop(); }
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
        bool siteHandlesPropagation = false, string notFoundEc = "EC-PROGRAM-NOT-FOUND")
    {
        var n = ResolveVisible(name, callerPath)
            ?? throw new CobolCallException(
                notFoundEc == "EC-FUNCTION-NOT-FOUND"
                    ? $"FUNCTION '{name?.Trim()}': the user-defined function could not be located in the run unit "
                      + "(ISO §8.4.3.2.4 GR6b — EC-FUNCTION-NOT-FOUND)"
                    : $"CALL '{name?.Trim()}': program not found in the run unit (ISO §14.9.4.4 GR3b — EC-PROGRAM-NOT-FOUND)",
                notFoundEc);
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
        CobolModule.Push(n.Name, OutermostName(n), n.ParentPath is not null);   // §15.65.4 r7/r8 frame
        try { inst.Call(args, returning); }
        finally { n.Active--; CobolModule.Pop(); }

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

    /// <summary>The outermost (top-level) program-id name of a node's compilation-unit containment chain
    /// (ISO §15.65.4 r7 — MODULE-NAME CURRENT). Equals the node's own name for a top-level program.</summary>
    private static string OutermostName(Node n)
    {
        var top = n;
        for (var p = ParentOf(top); p is not null; p = ParentOf(top)) top = p;
        return top.Name;
    }

    private static bool NameEquals(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
