// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Runtime;

/// <summary>
/// The run-unit program registry (one INSTANCE per run unit, owned by <see cref="RunUnit"/> — the verbatim port
/// of the pre-P8 static <c>ProgramRegistry</c> bodies): every compiled program unit registers at run-unit start
/// (name, containment, COMMON / INITIAL / RECURSIVE attributes, instance factory); CALL resolves names per the
/// §8.4.6.3 scope rules and drives the §14.6.2.3 state model; CANCEL implements §14.9.5. Instances ARE the
/// state: a plain program's cached singleton realizes last-used persistence (§8.6.4 / §14.6.2.3.3); dropping the
/// instance realizes initial-state-on-next-CALL (§14.9.5 GR3); a fresh instance per activation realizes INITIAL
/// (§14.6.2.3.2) and RECURSIVE (deep-dive D3/D4). In-assembly static registration is the primary profile; an
/// unresolved outermost name additionally probes the application directory for a sibling compiled module
/// (<c>&lt;name&gt;.dll</c>) and invokes its public <c>__CobolModule.Register()</c> registrar — the
/// implementation-defined §14.9.4.4 GR3b "locate the program" mechanism (owner-approved; a
/// prebuilt-static-registry profile remains possible for AOT/trimming, where the probe never fires).
/// </summary>
public sealed class ProgramTable
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

    private readonly RunUnit _owner;
    private readonly Dictionary<string, Node> _byPath = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Node> _order = [];
    private readonly HashSet<string> _probedModules = new(StringComparer.OrdinalIgnoreCase);

    public ProgramTable(RunUnit owner) => _owner = owner;

    /// <summary>Clear all registrations, the external store, and the MODULE-NAME stack (run-unit start — the
    /// exact pre-P8 <c>ProgramRegistry.Reset()</c> semantics).</summary>
    public void Reset()
    {
        _byPath.Clear();
        _order.Clear();
        _probedModules.Clear();
        _owner.External.Reset();
        _owner.Modules.Reset();   // the FUNCTION MODULE-NAME call-name stack (§15.65)
    }

    /// <summary>Register one program unit (emitted once per unit at run-unit start, containers before containees).</summary>
    public void Register(
        string path, string name, string? parentPath,
        bool initial, bool common, bool recursive,
        Func<ICobolProgram?, ICobolProgram> factory)
    {
        var node = new Node
        {
            Path = path, Name = name, ParentPath = parentPath,
            Initial = initial, Common = common, Recursive = recursive, Factory = factory,
        };
        _byPath[path] = node;
        _order.Add(node);
        if (parentPath is not null && _byPath.TryGetValue(parentPath, out var parent)) parent.Children.Add(node);
    }

    /// <summary>Run the run unit's MAIN program (the first program of the compilation group).</summary>
    public void RunMain(string path)
    {
        var n = _byPath[path];
        var inst = n.Instance ??= n.Factory(null);
        n.Active++;
        _owner.Modules.PushMain(n.Name);   // TOP-LEVEL / the run-unit main (§15.65.4 r5/r10)
        try { inst.Activate(); }
        finally { n.Active--; _owner.Modules.Pop(); }
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
    private void ApplyPropagationDefault()
    {
        if (_owner.Exceptions.TakePropagated(out string pn, out bool pf) && pf)
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
    public void CallProgram(string name, string callerPath, CobolArg[] args, ManagedPointer? returning,
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
        _owner.Modules.Push(n.Name, OutermostName(n), n.ParentPath is not null);   // §15.65.4 r7/r8 frame
        try { inst.Call(args, returning); }
        finally { n.Active--; _owner.Modules.Pop(); }

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

    /// <summary>Resolve a program-address-identifier's ENTRY operand (ISO §8.4.3.13): locate the OUTERMOST
    /// program <paramref name="name"/> names (GR1/GR2 — "the address is that of the outermost program
    /// identified by the externalized program-name"; the §8.4.6.3 rule-4 scope, including the separately-
    /// compiled sibling-module probe). Not locatable → GR4: <paramref name="notFound"/> is set (the emitted
    /// site raises EC-PROGRAM-NOT-FOUND per its checking state) and the result is the NULL program address.
    /// The returned pointer carries the CANONICAL registered name, so pointer equality (§8.8.4.1.3) holds
    /// across differently-cased ENTRY spellings.</summary>
    public ProgramPointer EntryOf(string name, out bool notFound)
    {
        string target = name?.Trim() ?? "";
        foreach (var n in _order)
            if (n.ParentPath is null && NameEquals(n.Name, target)) { notFound = false; return new ProgramPointer(n.Name); }
        if (ProbeSiblingModule(target))
            foreach (var n in _order)
                if (n.ParentPath is null && NameEquals(n.Name, target)) { notFound = false; return new ProgramPointer(n.Name); }
        notFound = true;
        return ProgramPointer.Null;   // §8.4.3.13 GR4 — the value is the predefined address NULL
    }

    /// <summary>Execute a CALL through a program-pointer (ISO §14.9.4 SR1 — identifier-1 references a
    /// program-pointer item; GR at :26177 — the item "contains the location of the program being called").
    /// A NULL pointer has no program to call: §14.9.4.4's "invalid program address" execution is undefined —
    /// this implementation defines it as the EC-PROGRAM-NOT-FOUND loud failure (never a silent no-op). The
    /// held name is an OUTERMOST program's identity, so the §8.4.6.3 rule-4 leg of the SAME
    /// <see cref="CallProgram"/> resolution finds it from any caller (the singular-pattern rule).</summary>
    public void CallPointer(ProgramPointer target, string callerPath, CobolArg[] args, ManagedPointer? returning,
        bool siteHandlesPropagation = false)
    {
        if (target.IsNull)
            throw new CobolCallException(
                "CALL through a NULL program-pointer: the pointer contains no program address "
                + "(ISO §14.9.4.4 GR — an invalid program address; this implementation raises "
                + "EC-PROGRAM-NOT-FOUND)", "EC-PROGRAM-NOT-FOUND");
        CallProgram(target.Name!, callerPath, args, returning, siteHandlesPropagation);
    }

    /// <summary>
    /// Execute one CANCEL target (ISO §14.9.5): a zero-length name is a no-op (GR12); a name not in the run unit
    /// is a no-op (the never-made-available case); an ACTIVE program raises (GR5 — EC-PROGRAM-CANCEL-ACTIVE, the
    /// program is NOT canceled); otherwise contained programs cancel in reverse source order (GR4), the
    /// program's open file connectors close implicitly (GR9 — no optional phrases, no USE procedures), and the
    /// next CALL finds the program in its initial state (GR3). EXTERNAL data is untouched (GR8). A never-called
    /// or already-canceled program is a no-op (GR7).
    /// </summary>
    public void Cancel(string name, string callerPath)
    {
        string n = name?.Trim() ?? "";
        if (n.Length == 0) return;   // §14.9.5 GR12
        var node = ResolveVisible(n, callerPath);
        if (node is null) return;
        CancelNode(node);
    }

    private void CancelNode(Node n)
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
            n.Instance = null;   // GR3 — the next CALL finds the initial state (GR8: the external store untouched)
        }
        // no instance → never called / already canceled → no-op (GR7)
    }

    private void CancelContained(Node n)
    {
        for (int i = n.Children.Count - 1; i >= 0; i--) CancelNode(n.Children[i]);
    }

    private ICobolProgram? ParentInstance(Node n) =>
        n.ParentPath is null ? null
        : _byPath.TryGetValue(n.ParentPath, out var p)
            ? p.Instance ?? throw new CobolCallException(
                $"internal: contained program '{n.Name}' activated while its container is not instantiated")
            : null;

    /// <summary>
    /// Resolve a CALL/CANCEL program-name from the calling program per ISO §8.4.6.3: (1) a program DIRECTLY
    /// contained in the caller; (2) the caller itself when RECURSIVE (self-call); (3) a COMMON program contained
    /// in a (transitive) container of the caller — except from within that COMMON program or its containees
    /// unless it is recursive; (4) an OUTERMOST program of the run unit (callable from anywhere).
    /// </summary>
    private Node? ResolveVisible(string? name, string? callerPath)
    {
        string target = name?.Trim() ?? "";
        if (target.Length == 0) return null;
        Node? caller = callerPath is not null && _byPath.TryGetValue(callerPath, out var c) ? c : null;

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
        foreach (var n in _order)                                                    // rule 4 — outermost programs
            if (n.ParentPath is null && NameEquals(n.Name, target)) return n;

        // Rule-4 fallthrough: the run unit may be composed of SEPARATELY COMPILED modules ("a run unit contains
        // one or more runtime modules", ISO §14.6.1; §14.9.4.4 GR3b — the runtime system "attempts to locate"
        // the called program; the locating mechanics beyond the §8.4.6.3 name scope are implementor-defined).
        // Probe the application directory for a sibling compiled module named after the program, invoke its
        // public __CobolModule.Register() registrar (generated classes are internal — the registrar IS the
        // discovery surface), and retry rule 4 once. Probed names are cached, hit or miss — one I/O probe per
        // name per run unit.
        if (ProbeSiblingModule(target))
            foreach (var n in _order)
                if (n.ParentPath is null && NameEquals(n.Name, target)) return n;
        return null;
    }

    /// <summary>Load the sibling compiled module <c>&lt;name&gt;.dll</c> from <see cref="AppContext.BaseDirectory"/>
    /// (exact name first, then a case-insensitive scan — Linux filesystems are case-sensitive) into the default
    /// <see cref="System.Runtime.Loader.AssemblyLoadContext"/> and run its <c>__CobolModule.Register()</c>.
    /// Returns true when a registrar ran (the caller re-resolves); a missing file / foreign dll / load failure
    /// is a quiet false — the CALL then raises the ordinary EC-PROGRAM-NOT-FOUND surface.</summary>
    private bool ProbeSiblingModule(string name)
    {
        if (!_probedModules.Add(name)) return false;   // already probed this run unit (negative/positive cache)
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

    private Node? ParentOf(Node n) =>
        n.ParentPath is not null && _byPath.TryGetValue(n.ParentPath, out var p) ? p : null;

    /// <summary>The outermost (top-level) program-id name of a node's compilation-unit containment chain
    /// (ISO §15.65.4 r7 — MODULE-NAME CURRENT). Equals the node's own name for a top-level program.</summary>
    private string OutermostName(Node n)
    {
        var top = n;
        for (var p = ParentOf(top); p is not null; p = ParentOf(top)) top = p;
        return top.Name;
    }

    private static bool NameEquals(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
