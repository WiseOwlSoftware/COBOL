// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Frontend.Generated;

namespace CobolNet.Compiler.Oo;

/// <summary>One INTERFACE-ID of the compilation group (§11.6 — pass-1): the PROTOTYPE roster (reusing
/// <see cref="OoMethodSymbol"/> — headers + LINKAGE formals, no bodies), the INHERITS list (repetition
/// SUPPORTED — C# interface lists are native; the deliberate asymmetry with the class-side single-base
/// restriction, SSOT §18.18), and the emitted C# interface name.</summary>
public sealed class OoInterfaceSymbol(string name, string csName, CobolParserCore.InterfaceDefinitionContext ctx)
{
    public string Name { get; } = name;
    public string CsName { get; } = csName;
    public CobolParserCore.InterfaceDefinitionContext Ctx { get; } = ctx;
    public List<string> InheritNames { get; } = [];
    public List<OoInterfaceSymbol> Inherits { get; } = [];

    private readonly Dictionary<string, OoMethodSymbol> _protos = new(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyList<OoMethodSymbol> Prototypes => _protoList;
    private readonly List<OoMethodSymbol> _protoList = [];

    internal bool TryAddPrototype(OoMethodSymbol m)
    {
        if (!_protos.TryAdd(m.Name, m)) return false;
        _protoList.Add(m);
        return true;
    }

    /// <summary>The interface's FULL method surface: own prototypes + the INHERITS closure (§9.3.8.2.2),
    /// first declaration wins per name (SR5 mutual-conformance across multi-inherit is validated at Build).</summary>
    public IEnumerable<OoMethodSymbol> AllPrototypes()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var stack = new Stack<OoInterfaceSymbol>();
        var visited = new HashSet<OoInterfaceSymbol>();
        stack.Push(this);
        while (stack.Count > 0)
        {
            var i = stack.Pop();
            if (!visited.Add(i)) continue;
            foreach (var m in i._protoList)
                if (seen.Add(m.Name))
                    yield return m;
            foreach (var b in i.Inherits) stack.Push(b);
        }
    }
}
