// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

namespace WiseOwl.Cobol.Compiler.Binding.Model;

/// <summary>One INTERFACE-ID of the compilation group (§2023:11.6 — pass-1): the PROTOTYPE roster (reusing
/// <see cref="OoMethodSymbol"/> — headers + LINKAGE formals, no bodies), the INHERITS list (repetition
/// SUPPORTED — C# interface lists are native; the deliberate asymmetry with the class-side single-base
/// restriction, SSOT §18.18), and the emitted C# interface name.</summary>
public sealed class OoInterfaceSymbol
{
    /// <summary>An interface of origin <paramref name="origin"/>: <see cref="OoSymbolOrigin.Source"/> (an INTERFACE-ID of this group,
    /// whose definition the bind session's source map holds, kb/Work PB2401) or <see cref="OoSymbolOrigin.Imported"/> (ISO §16 defines
    /// no standard interface).</summary>
    public OoInterfaceSymbol(string name, string clrName, OoSymbolOrigin origin)
    {
        (Name, ExternalizedName, ClrName, Origin) = (name, name, clrName, origin);
    }

    public string Name { get; }
    /// <summary>The name externalized to the operating environment (ISO §2023:11.6.4 GR1: "literal-1, if
    /// specified, is the name of the interface that is externalized to the operating environment"), from
    /// the INTERFACE-ID <c>AS literal-1</c>; else <see cref="Name"/>. An interface-name is only ever
    /// referenced by a user-defined WORD (§2023:8.4.6.4), so nothing in COBOL resolves against this — it is
    /// the operating-environment identity this implementation records for the definition (kb/Work
    /// PB303).</summary>
    public string ExternalizedName { get; init; }

    /// <summary>The repository record an IMPORTED interface was decoded from (null for an interface of this group).</summary>
    internal Binding.Model.RepositoryRecord? Record { get; init; }
    /// <summary>The CLR type name of the interface — the module's ABI, the same for every backend (kb/Work
    /// PB4265).</summary>
    public string ClrName { get; }
    /// <summary>Where this symbol came from (<see cref="OoSymbolOrigin"/>): <see cref="OoSymbolOrigin.Source"/> (its INTERFACE-ID
    /// definition and what made it an expansion are in the bind session's <c>SourceMap</c>) or <see cref="OoSymbolOrigin.Imported"/>;
    /// ISO §16 defines no standard interface.</summary>
    public OoSymbolOrigin Origin { get; }

    /// <summary>The assembly an <see cref="OoSymbolOrigin.Imported"/> interface was taken from (DESIGN-external-repository §10.6);
    /// null for a source one. Its <see cref="ClrName"/> is the type's full name under <c>global::</c>.</summary>
    public ImportedFrom? Imported { get; init; }
    public List<OoInterfaceSymbol> Inherits { get; } = [];

    /// <summary>The emitted C# types a COBOL reference to THIS interface selects when the rule asks whether an
    /// object IMPLEMENTS it — ISO/IEC 1989:2023 §2023:14.9.49.4 GR14b: "the exception object that was raised is
    /// described with an IMPLEMENTS clause that references interface-name-1". The twin of
    /// <see cref="OoClassSymbol.FactoryOrInstanceClrNames"/>, and the answer is a SET for the same reason: one
    /// COBOL name is not one C# type by construction, it is one by MEASUREMENT. Today it measures ONE — a
    /// COBOL INTERFACE-ID emits exactly one C# interface (<c>OoEmitter.EmitInterfaceUnit</c>), which BOTH
    /// emitted halves of an implementing class carry (the instance half from the OBJECT paragraph's IMPLEMENTS,
    /// the factory half from the FACTORY paragraph's, §2023:11.8.2), so a single <c>is</c> test covers both object
    /// kinds where a class-name needs two. If the interface ever emits a second half, this is the one line that
    /// changes and <c>Format4UseObjectSelectorDriftTests</c> is what fails first.
    /// <para>The three legs of §2023:11.8.4 GR2 / §2023:11.4.4 GR2 ("a) defined with an IMPLEMENTS clause specifying
    /// intf-1, b) implements an interface that inherits intf-1, c) the class … inherits a class whose instance
    /// object implements intf-1") ride C#'s <c>is</c> rather than this census: the emitted interface carries its
    /// own INHERITS as C# bases and the emitted class carries its base's implementations.</para></summary>
    public IReadOnlyList<string> ImplementedClrNames => [ClrName];

    private readonly Dictionary<ExternalizedMethodName, OoMethodSymbol> _protos = [];
    public IReadOnlyList<OoMethodSymbol> Prototypes => _protoList;
    private readonly List<OoMethodSymbol> _protoList = [];

    internal bool TryAddPrototype(OoMethodSymbol m)
    {
        if (!_protos.TryAdd(m.RosterKey, m)) return false;   // the roster key (PB303)
        _protoList.Add(m);
        return true;
    }

    /// <summary>This interface's OWN prototype of the given roster key (<see cref="OoMethodSymbol.RosterKey"/>),
    /// or null — the question §2023:11.7.3 SR4b asks of every interface in the INHERITS closure.</summary>
    internal OoMethodSymbol? FindOwnPrototype(ExternalizedMethodName key) => _protos.GetValueOrDefault(key);

    /// <summary>Every interface this one inherits from, directly or indirectly (§2023:9.3.10 — "the inheriting interface has
    /// all the method specifications defined for the inherited interface definition or definitions"), each once and never
    /// this interface itself, in discovery order. Terminates over a cyclic graph (§2023:11.6.3 SR3 reports the cycle).</summary>
    public IEnumerable<OoInterfaceSymbol> InheritedClosure()
    {
        var visited = new HashSet<OoInterfaceSymbol> { this };
        var queue = new Queue<OoInterfaceSymbol>(Inherits);
        while (queue.Count > 0)
        {
            var i = queue.Dequeue();
            if (!visited.Add(i)) continue;
            yield return i;
            foreach (var b in i.Inherits) queue.Enqueue(b);
        }
    }

    /// <summary>The prototypes this interface PRESENTS for a name it inherits from several interfaces (§2023:11.6.3 SR5; chosen
    /// by <c>OoConformance.ValidateInterfaceInheritance</c> through <c>SymbolTable.ClassNames.ChooseInheritedPrototype</c>, which
    /// keys the choice by method-name and sets this) — each needs a C# member of its own on the emitted interface, because C#
    /// member lookup over two base interfaces that both declare <c>SPEAK()</c> is ambiguous (CS0121) at every call through a
    /// reference to THIS interface, where §2023:11.6.3 SR5 says the inheriting interface has one method of that name
    /// (<c>OoEmitter.EmitInterfaceUnit</c>). The interface's full method surface, one prototype per method-name, is
    /// <c>SymbolTable.ClassNames.AllPrototypes</c>.</summary>
    internal IReadOnlyCollection<OoMethodSymbol> PresentedInherited { get; private set; } = [];

    /// <summary>Set <see cref="PresentedInherited"/> (the name tier's chosen set, in the order first chosen).</summary>
    internal void Present(IReadOnlyCollection<OoMethodSymbol> chosen) => PresentedInherited = chosen;
}
