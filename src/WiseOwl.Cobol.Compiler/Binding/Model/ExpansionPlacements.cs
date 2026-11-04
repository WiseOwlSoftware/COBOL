// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend.Common;
using WiseOwl.Cobol.Runtime.Repository;

namespace WiseOwl.Cobol.Binding.Model;

/// <summary>Where an expansion lives (DESIGN-external-repository §10.5).</summary>
internal enum ExpansionWhere
{
    /// <summary>In the group's assembly: it is made here and emitted here, with its identity and key published (placement (b)).</summary>
    InGroup,
    /// <summary>In an assembly of the search space that already holds an expansion of this name and key: the group emits nothing for its
    /// own copy and names the existing types (<see cref="ExpansionPlacement.HoldingFile"/>).</summary>
    Imported,
    /// <summary>In an assembly of its own, written beside the group's output by a compilation of its own (placement (a)); the group
    /// names its types there as it does an imported expansion's.</summary>
    OwnAssembly,
}

/// <summary>An expansion's identity (what it expands, with what, its key) and where it lives.</summary>
/// <param name="Identity">The identity the expansion publishes.</param>
/// <param name="Where">Where it lives.</param>
/// <param name="HoldingFile">The assembly it is imported from or written to (null for an expansion in the group's own assembly).</param>
/// <param name="ClrNamespace">The namespace its types live in there.</param>
/// <param name="TypeName">The simple name of its CLR type in that assembly.</param>
/// <param name="FactoryTypeName">The simple name of its factory type there; null for an interface.</param>
/// <param name="LocalName">The simple name this module's code uses for the class or interface (its symbol's CLR name).</param>
/// <param name="LocalFactoryName">The simple name this module's code uses for the factory type; null for an interface.</param>
internal sealed record ExpansionPlacement(ExpansionIdentity Identity, ExpansionWhere Where, string? HoldingFile = null, string? ClrNamespace = null,
    string? TypeName = null, string? FactoryTypeName = null, string? LocalName = null, string? LocalFactoryName = null);

/// <summary>A skeleton the group publishes (a parameterized class or interface it writes): the unit's names, the <c>public static
/// class</c> that carries its attributes, and the skeleton record itself.</summary>
internal sealed record PublishedSkeleton(string Name, string ExternalizedName, bool IsInterface, string ClrName, SkeletonRecord Skeleton)
{
    /// <summary>The kind the unit is published as (§5.1).</summary>
    public RepositoryUnitKind Kind => IsInterface ? RepositoryUnitKind.ParameterizedInterface : RepositoryUnitKind.ParameterizedClass;
}

/// <summary>An expansion to be written to an assembly of its own: its text, the compile state it is compiled under and where it goes.</summary>
/// <param name="Text">The expansion's source text (the skeleton's tokens with the header renamed and each formal replaced by its actual).</param>
/// <param name="State">The compile state of the skeleton it is made from.</param>
/// <param name="Placement">Where it goes.</param>
/// <param name="RequestedAt">The EXPANDS entry that asked for it, as the user's source names it (file, line, column), which a diagnostic of its compilation is reported at. It is where the group prints a diagnostic about the text of an expansion of a skeleton it read from the repository too (<c>DiagnosticRegion.ReportedAt</c>), so the placements agree on all three (kb/Work PB4469, PB4483).</param>
/// <param name="Region">The region the group's bind of the text reported under (<c>ExpansionToken</c>), which says who answers for the text: for a skeleton read from the repository its own compilation does, and the group withdraws what it raised there once that compilation has run (<c>EditionContext.WithdrawWarningsOf</c>); for a skeleton written in the group the group does, at the positions the user wrote it at, and that compilation's warnings are the same facts again (<c>ExpansionAssemblies.Write</c>, kb/Work PB4483).</param>
internal sealed record ExpansionToWrite(string Text, CompileState State, ExpansionPlacement Placement, SourceLocation RequestedAt, DiagnosticRegion Region);

/// <summary>What the group publishes for its parameterized definitions and where each of its expansions lives (§10.5): produced by
/// <c>ExpansionPlacementPass</c> once the group is bound, carried on the <see cref="BoundCompilation"/>, read by the emitter and the
/// driver.</summary>
/// <param name="ByName">Each expansion's placement, by the CLR name its class or interface has in this module (a module's type names are
/// unique across its classes and interfaces).</param>
/// <param name="Skeletons">The group's skeletons, in source order.</param>
internal sealed record ExpansionPlacements(IReadOnlyDictionary<string, ExpansionPlacement> ByName, IReadOnlyList<PublishedSkeleton> Skeletons)
{
    /// <summary>A group with no parameterized definition and no expansion — nearly every group.</summary>
    public static readonly ExpansionPlacements None = new(new Dictionary<string, ExpansionPlacement>(StringComparer.Ordinal), []);

    /// <summary>The expansions to be written to assemblies of their own (placement (a)), in dependency order — an expansion before any
    /// expansion that names it.</summary>
    public IReadOnlyList<ExpansionToWrite> ToWrite { get; init; } = [];

    /// <summary>Every placement.</summary>
    public IEnumerable<ExpansionPlacement> All => ByName.Values;

    /// <summary>Every assembly the group's own module names a type of for an expansion: the imported ones and the ones written for it.</summary>
    public IReadOnlyList<string> ReferencedAssemblies =>
        [.. All.Where(p => p.HoldingFile is not null).Select(p => p.HoldingFile!).Distinct(StringComparer.OrdinalIgnoreCase)];   // not a COBOL word: a file path

    /// <summary>The placement of the class or interface this module names <paramref name="clrName"/>, or null when it is no expansion.</summary>
    public ExpansionPlacement? Of(string clrName) => ByName.GetValueOrDefault(clrName);

    /// <summary>Whether this module emits the class or interface it names <paramref name="clrName"/>: every one but an expansion
    /// placed in another assembly (§10.5). The one answer: the binder publishes a record for exactly these, and the backend emits
    /// exactly these and lists them in the module's <c>Units</c> index.</summary>
    public bool EmittedHere(string clrName) => Of(clrName) is not { Where: ExpansionWhere.Imported or ExpansionWhere.OwnAssembly };

    /// <summary>Whether the class, factory type or interface this module names <paramref name="clrName"/> is an expansion that lives in
    /// another assembly (imported, or written to one of its own), which this module names through a <c>using</c> alias.</summary>
    public bool LivesElsewhere(string clrName) =>
        All.Any(p => p.Where is not ExpansionWhere.InGroup && (p.LocalName == clrName || p.LocalFactoryName == clrName));

    /// <summary>The kind the class or interface named <paramref name="clrName"/> is published as: an expansion's, else <paramref name="plain"/>.</summary>
    public RepositoryUnitKind KindOf(string clrName, RepositoryUnitKind plain) => ByName.ContainsKey(clrName) ? RepositoryUnitKind.Expansion : plain;
}
