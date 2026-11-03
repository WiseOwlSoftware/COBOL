// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Compiler.Oo;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Runtime.Repository;

namespace WiseOwl.Cobol.Binding.Repository;

using Core = CobolParserCore;

/// <summary>Where an expansion lives (DESIGN-external-repository §10.5).</summary>
internal enum ExpansionWhere
{
    /// <summary>In the group's assembly: it is made here and emitted here, with its identity and key published (placement (b)).</summary>
    InGroup,
    /// <summary>In an assembly of the search space that already holds an expansion of this name and key: the group emits nothing for it
    /// and names its types through <see cref="ExpansionPlacement.From"/>.</summary>
    Imported,
    /// <summary>In an assembly of its own, written beside the group's output (placement (a)).</summary>
    OwnAssembly,
}

/// <summary>An expansion's identity (what it expands, with what, its key) and where it lives.</summary>
/// <param name="Identity">The identity the expansion publishes.</param>
/// <param name="Where">Where it lives.</param>
/// <param name="From">The assembly it is imported from or written to (null for an expansion in the group's own assembly).</param>
internal sealed record ExpansionPlacement(ExpansionIdentity Identity, ExpansionWhere Where, ImportedFrom? From = null);

/// <summary>A skeleton the group publishes: its source and its record.</summary>
internal sealed record PublishedSkeleton(SkeletonSource Source, RepositoryRecord Record)
{
    /// <summary>The <c>public static class</c> that carries the skeleton's attributes (§7.2), named by the one mapping a class's CLR name is.</summary>
    public string ClrName => DataItem.WordIdentifier(Source.Name);
}

/// <summary>What the group publishes for its parameterized definitions and where each of its expansions lives (§10.5): produced by
/// <see cref="ExpansionPlacementPass"/> once the group is bound, carried on the <see cref="BoundCompilation"/>, read by the emitter.</summary>
/// <param name="Expansions">Each expansion's class or interface symbol → its placement.</param>
/// <param name="Skeletons">The group's skeletons, in source order.</param>
internal sealed record ExpansionPlacements(IReadOnlyDictionary<object, ExpansionPlacement> Expansions, IReadOnlyList<PublishedSkeleton> Skeletons)
{
    /// <summary>A group with no parameterized definition and no expansion — nearly every group.</summary>
    public static readonly ExpansionPlacements None = new(new Dictionary<object, ExpansionPlacement>(ReferenceEqualityComparer.Instance), []);

    /// <summary>The placement of the class or interface <paramref name="symbol"/>, or null when it is no expansion.</summary>
    public ExpansionPlacement? Of(object symbol) => Expansions.GetValueOrDefault(symbol);

    /// <summary>The kind <paramref name="symbol"/> is published as: an expansion's, else <paramref name="plain"/>.</summary>
    public RepositoryUnitKind KindOf(object symbol, RepositoryUnitKind plain) => Expansions.ContainsKey(symbol) ? RepositoryUnitKind.Expansion : plain;
}

/// <summary>
/// ⛔ WHAT THE GROUP PUBLISHES FOR PARAMETERIZED DEFINITIONS, AND WHERE EACH EXPANSION OF ONE LIVES (ISO §9.3.12, §9.3.13;
/// DESIGN-external-repository §10.5; kb/Work PB2104). It runs once the group is bound — a class's record is its surface, which exists only
/// then — and does three things in order: it states each skeleton the group writes (its source, formals, the identity every specifier of
/// its own REPOSITORY paragraph stands for, and the <see cref="CompileState"/> it was compiled under); it computes each expansion's
/// <see cref="ExpansionKey"/> from the records of everything the expansion names; and it places the expansion.
/// </summary>
internal static class ExpansionPlacementPass
{
    public static ExpansionPlacements Run(OoClassTable table, IReadOnlyList<OoClassUnit> classes,
        IReadOnlyDictionary<OoInterfaceSymbol, DataBinder> interfaceData)
    {
        if (table.Skeletons.Count == 0 && !classes.Any(c => c.Symbol.Expansion is not null) && !table.Interfaces.Any(i => i.Expansion is not null))
            return ExpansionPlacements.None;

        var records = new Dictionary<object, RepositoryRecord>(ReferenceEqualityComparer.Instance);
        RepositoryRecord RecordOf(object symbol)
        {
            if (records.TryGetValue(symbol, out var known)) return known;
            return records[symbol] = symbol switch
            {
                OoClassSymbol c => RepositoryCodec.Of(classes.First(u => ReferenceEquals(u.Symbol, c))),
                OoInterfaceSymbol i => RepositoryCodec.Of(i, interfaceData[i]),
                _ => throw new InvalidOperationException($"{symbol.GetType().Name} is no class or interface"),
            };
        }

        // What a class or interface a specifier or an actual names contributes to a key: its identity and its record.
        ExpansionKey.Named NamedBy(Core.RepositoryEntryContext specifier)
        {
            var (cls, iface) = table.ResolveSpecifier(specifier);
            return cls is not null ? NamedOf(cls)
                : iface is not null ? NamedOfInterface(iface)
                : new ExpansionKey.Named(WrittenIdentity(table, specifier), null);
        }
        ExpansionKey.Named NamedOf(OoClassSymbol c) =>
            c.Origin switch
            {
                OoSymbolOrigin.Standard => new(c.ExternalizedName, null),
                OoSymbolOrigin.Imported => new(c.ExternalizedName, c.Record),
                _ => new(c.ExternalizedName, RecordOf(c)),
            };
        ExpansionKey.Named NamedOfInterface(OoInterfaceSymbol i) =>
            i.Origin is OoSymbolOrigin.Imported ? new(i.ExternalizedName, i.Record) : new(i.ExternalizedName, RecordOf(i));

        ResolutionRecord Resolution(Core.RepositoryEntryContext specifier)
        {
            bool isInterface = specifier.INTERFACE() is not null;
            string word = (specifier.className()?.GetText() ?? specifier.interfaceName()!.GetText());
            return new ResolutionRecord(word, isInterface, NamedBy(specifier).Identity);
        }

        var skeletons = new List<PublishedSkeleton>();
        foreach (var source in table.Skeletons)
        {
            var record = RepositoryCodec.Of(source.Name, source.Identity, source.IsInterface, source.ToRecord(Resolution));
            skeletons.Add(new PublishedSkeleton(source, record));
        }

        var placements = new Dictionary<object, ExpansionPlacement>(ReferenceEqualityComparer.Instance);
        foreach (object symbol in classes.Select(c => (object)c.Symbol).Concat(table.Interfaces))
        {
            var origin = symbol is OoClassSymbol cs ? cs.Expansion : ((OoInterfaceSymbol)symbol).Expansion;
            if (origin is null) continue;
            var skeleton = origin.Skeleton.ToRecord(Resolution);
            var resolutions = origin.Skeleton.ResolutionEntries.Select(NamedBy).ToList();
            var paragraph = ((Core.RepositoryParagraphContext)origin.Entry.Parent).repositoryEntry();
            var actuals = origin.ActualWords
                .Select(w => NamedBy(paragraph.First(r => CobolNames.Same(r.className()?.GetText() ?? r.interfaceName()?.GetText(), w)))).ToList();
            var (key, parts) = ExpansionKey.Of(origin.Skeleton.Identity, skeleton, resolutions, actuals);
            var identity = new ExpansionIdentity(origin.Skeleton.Identity, new RecordList<string>([.. actuals.Select(a => a.Identity)]), key,
                new RecordList<string>([.. parts]), OwnAssembly: false);
            placements[symbol] = new ExpansionPlacement(identity, ExpansionWhere.InGroup);
        }
        return new ExpansionPlacements(placements, skeletons);
    }

    /// <summary>The identity a specifier states when nothing answers it: its AS literal, else its word (ISO §12.3.8.4 GR2).</summary>
    private static string WrittenIdentity(OoClassTable table, Core.RepositoryEntryContext specifier)
    {
        var scope = table.RepositoryScopeFor(specifier);
        string word = specifier.className()?.GetText() ?? specifier.interfaceName()!.GetText();
        string? ext = null;
        if (specifier.className() is not null) scope.ClassTarget(word, out ext);
        else scope.InterfaceTarget(word, out ext);
        return ext ?? word;
    }
}
