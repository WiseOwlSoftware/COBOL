// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Binding.Model;

/// <summary>
/// The classes and interfaces this compilation IMPORTED from the external repository (DESIGN-external-repository §10.1), in
/// the order they were first asked for: what the backend references and names in another assembly (§2023:10.2). The bind hands
/// them to the backend as data on <see cref="BoundCompilation.Imported"/> (kb/Work PB2403), so no emitter asks the class
/// table that imported them.
/// </summary>
/// <param name="Classes">The imported classes, each carrying the assembly it was imported from (<see cref="OoClassSymbol.Imported"/>).</param>
/// <param name="Interfaces">The imported interfaces, likewise.</param>
internal sealed record ImportedTypes(IReadOnlyList<OoClassSymbol> Classes, IReadOnlyList<OoInterfaceSymbol> Interfaces)
{
    /// <summary>The assemblies of the external repository this compilation imported a class or interface from, once each, in
    /// the order they were first needed: what the backend references to name an imported type
    /// (<c>BackendOptions.RepositoryReferences</c>, DESIGN-external-repository §10.2).</summary>
    public IReadOnlyList<string> Assemblies =>
        [.. Classes.Select(c => c.Imported!.AssemblyFile).Concat(Interfaces.Select(i => i.Imported!.AssemblyFile)).Distinct(StringComparer.Ordinal)];

    /// <summary>Whether <paramref name="clrName"/> is the instance or factory type of an imported class, or an imported
    /// interface: a type that lives in another assembly, whose internal members this module never names.</summary>
    public bool Declares(string clrName) =>
        Classes.Any(c => c.ClrName == clrName || c.FactoryClrName == clrName) || Interfaces.Any(i => i.ClrName == clrName);
}
