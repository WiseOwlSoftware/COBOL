// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime.Repository;

namespace WiseOwl.Cobol.Binding.Model;

/// <summary>Where an expansion lives (DESIGN-external-repository §10.5).</summary>
internal enum ExpansionWhere
{
    /// <summary>In the group's assembly: it is made here and emitted here, with its identity and key published (placement (b)).</summary>
    InGroup,
    /// <summary>In an assembly of the search space that already holds an expansion of this name and key: the group emits nothing for its
    /// own copy and names the existing types (<see cref="ExpansionPlacement.AssemblyPath"/>).</summary>
    Imported,
    /// <summary>In an assembly of its own, written beside the group's output by a compilation of its own (placement (a)); the group
    /// names its types there as it does an imported expansion's.</summary>
    OwnAssembly,
}

/// <summary>An expansion's identity (what it expands, with what, its key) and where it lives.</summary>
/// <param name="Identity">The identity the expansion publishes.</param>
/// <param name="Where">Where it lives.</param>
/// <param name="AssemblyPath">The assembly it is imported from or written to (null for an expansion in the group's own assembly).</param>
/// <param name="ClrNamespace">The namespace its types live in there.</param>
/// <param name="TypeName">The simple name of its CLR type in that assembly.</param>
/// <param name="FactoryTypeName">The simple name of its factory type there; null for an interface.</param>
/// <param name="LocalName">The simple name this module's code uses for the class or interface (its symbol's CLR name).</param>
/// <param name="LocalFactoryName">The simple name this module's code uses for the factory type; null for an interface.</param>
internal sealed record ExpansionPlacement(ExpansionIdentity Identity, ExpansionWhere Where, string? AssemblyPath = null, string? ClrNamespace = null,
    string? TypeName = null, string? FactoryTypeName = null, string? LocalName = null, string? LocalFactoryName = null);

/// <summary>A skeleton the group publishes (a parameterized class or interface it writes): the unit's names, the <c>public static
/// class</c> that carries its attributes, and the skeleton record itself.</summary>
internal sealed record PublishedSkeleton(string Name, string ExternalizedName, bool IsInterface, string ClrName, SkeletonRecord Skeleton)
{
    /// <summary>The kind the unit is published as (§5.1).</summary>
    public RepositoryUnitKind Kind => IsInterface ? RepositoryUnitKind.ParameterizedInterface : RepositoryUnitKind.ParameterizedClass;
}

/// <summary>What the group publishes for its parameterized definitions and where each of its expansions lives (§10.5): produced by
/// <c>ExpansionPlacementPass</c> once the group is bound, carried on the <see cref="BoundCompilation"/>, read by the emitter and the
/// driver.</summary>
/// <param name="Expansions">Each expansion's class or interface symbol → its placement.</param>
/// <param name="Skeletons">The group's skeletons, in source order.</param>
internal sealed record ExpansionPlacements(IReadOnlyDictionary<object, ExpansionPlacement> Expansions, IReadOnlyList<PublishedSkeleton> Skeletons)
{
    /// <summary>A group with no parameterized definition and no expansion — nearly every group.</summary>
    public static readonly ExpansionPlacements None = new(new Dictionary<object, ExpansionPlacement>(ReferenceEqualityComparer.Instance), []);

    /// <summary>The expansions to be written to assemblies of their own (placement (a)), in dependency order — an expansion before any
    /// expansion that names it — each with the symbol it is the placement of.</summary>
    public IReadOnlyList<(object Symbol, ExpansionPlacement Placement)> ToWrite { get; init; } = [];

    /// <summary>Every assembly the group's own module names a type of for an expansion: the imported ones and the ones written for it.</summary>
    public IReadOnlyList<string> ReferencedAssemblies =>
        [.. Expansions.Values.Where(p => p.AssemblyPath is not null).Select(p => p.AssemblyPath!).Distinct(StringComparer.OrdinalIgnoreCase)];   // not a COBOL word: a file path

    /// <summary>The placement of the class or interface <paramref name="symbol"/>, or null when it is no expansion.</summary>
    public ExpansionPlacement? Of(object symbol) => Expansions.GetValueOrDefault(symbol);

    /// <summary>The kind <paramref name="symbol"/> is published as: an expansion's, else <paramref name="plain"/>.</summary>
    public RepositoryUnitKind KindOf(object symbol, RepositoryUnitKind plain) => Expansions.ContainsKey(symbol) ? RepositoryUnitKind.Expansion : plain;
}
