// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Compiler.Oo;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Runtime.Repository;

namespace WiseOwl.Cobol.Binding.Repository;

using Core = CobolParserCore;

/// <summary>
/// ⛔ WHAT THE GROUP PUBLISHES FOR PARAMETERIZED DEFINITIONS, AND WHERE EACH EXPANSION OF ONE LIVES (ISO §9.3.12, §9.3.13;
/// DESIGN-external-repository §10.5; kb/Work PB2104). It runs once the group is bound — a class's record is its surface, which exists only
/// then — and does three things in order: it states each skeleton the group writes (its source, formals, the identity every specifier of
/// its own REPOSITORY paragraph stands for, and the <see cref="CompileState"/> it was compiled under); it computes each expansion's
/// <see cref="ExpansionKey"/> from the records of everything the expansion names; and it places the expansion:
/// <list type="number">
/// <item><b>Import before expand.</b> An expansion of its name and key that the search space already holds is the class: the group
/// emits nothing for its own copy and names the existing types (the copy it bound is the same text over the same surfaces, which is
/// what the key says). That holds when the existing assembly references this group's own previous build — a reference cycle the CLR and
/// Roslyn accept.</item>
/// <item><b>Otherwise (a) an assembly of its own</b> when its code names no class or interface of THIS group — every actual and every
/// resolution lies outside it: the driver compiles the expansion's text under the skeleton's recorded state into
/// <c>&lt;externalized name&gt;.dll</c> beside the output; <b>or (b) this group's assembly</b>, because an assembly of its own would need
/// the group's NEW assembly.</item>
/// <item><b>A stale expansion</b> — the same name and the same definition and actuals under another key — is never imported: it is named
/// by REPO-9 with the first component of its key that changed and every assembly of the search space that references it, and replaced.</item>
/// </list>
/// </summary>
internal static class ExpansionPlacementPass
{
    public static ExpansionPlacements Run(OoClassTable table, SourceMap sources, IReadOnlyList<OoClassUnit> classes,
        IReadOnlyDictionary<OoInterfaceSymbol, DataBinder> interfaceData, MetadataRepositoryProvider external, EditionContext edition,
        CompileInputs inputs, ExpansionIdentity? publishAs)
    {
        // The compilation of an expansion's own assembly: its text is the expansion's, so the one class or interface it defines IS the
        // expansion the parent asked for, published under the identity and key the parent computed.
        if (publishAs is not null)
        {
            var only = new ExpansionPlacement(publishAs, ExpansionWhere.InGroup);
            return (classes.Count, table.Interfaces.Count) switch
            {
                (1, 0) => new ExpansionPlacements(new Dictionary<string, ExpansionPlacement> { [classes[0].Symbol.ClrName] = only }, []),
                (0, 1) => new ExpansionPlacements(new Dictionary<string, ExpansionPlacement> { [table.Interfaces[0].ClrName] = only }, []),
                _ => ExpansionPlacements.None,
            };
        }
        if (table.Skeletons.Count == 0 && !classes.Any(c => sources.ExpansionOf(c.Symbol) is not null) && !table.Interfaces.Any(i => sources.ExpansionOf(i) is not null))
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

        var skeletons = table.Skeletons.Select(source => new PublishedSkeleton(source.Name, source.Identity, source.IsInterface,
            DataItem.WordIdentifier(source.Name), source.ToRecord(Resolution))).ToList();

        var placements = new Dictionary<object, ExpansionPlacement>(ReferenceEqualityComparer.Instance);
        var order = new List<ExpansionToWrite>();
        var inProgress = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var reportedStale = new HashSet<Guid>();
        foreach (object symbol in classes.Select(c => (object)c.Symbol).Concat(table.Interfaces))
            Place(symbol);
        return new ExpansionPlacements(placements.ToDictionary(e => LocalNameOf(e.Key), e => e.Value, StringComparer.Ordinal), skeletons) { ToWrite = order };

        // One expansion's placement, after the placement of every expansion it names (an actual may itself be an expansion).
        ExpansionPlacement? Place(object symbol)
        {
            var origin = symbol is OoClassSymbol cs ? sources.ExpansionOf(cs) : sources.ExpansionOf((OoInterfaceSymbol)symbol);
            if (origin is null) return null;
            if (placements.TryGetValue(symbol, out var done)) return done;
            if (!inProgress.Add(symbol)) return null;   // an expansion that names itself: its text is refused elsewhere

            var skeleton = origin.Skeleton.ToRecord(Resolution);
            var resolutionEntries = origin.Skeleton.ResolutionEntries;
            var paragraph = ((Core.RepositoryParagraphContext)origin.Entry.Parent).repositoryEntry();
            var actualEntries = origin.ActualWords
                .Select(w => paragraph.First(r => CobolNames.Same(r.className()?.GetText() ?? r.interfaceName()?.GetText(), w))).ToList();
            bool namesGroup = false;
            foreach (var entry in resolutionEntries.Concat(actualEntries))
            {
                var (c, i) = table.ResolveSpecifier(entry);
                object? named = c is not null ? c : i;
                if (named is OoClassSymbol { Origin: OoSymbolOrigin.Source } or OoInterfaceSymbol { Origin: OoSymbolOrigin.Source })
                    namesGroup |= Place(named) is not { Where: ExpansionWhere.Imported or ExpansionWhere.OwnAssembly };
            }
            var resolutions = resolutionEntries.Select(NamedBy).ToList();
            var actuals = actualEntries.Select(NamedBy).ToList();
            var (key, parts) = ExpansionKey.Of(origin.Skeleton.Identity, skeleton, resolutions, actuals);
            string externalized = symbol is OoClassSymbol c0 ? c0.ExternalizedName : ((OoInterfaceSymbol)symbol).ExternalizedName;
            var identity = new ExpansionIdentity(origin.Skeleton.Identity, new RecordList<string>([.. actuals.Select(a => a.Identity)]), key,
                new RecordList<string>([.. parts]), OwnAssembly: false);
            string mine = ExpansionKey.SurfaceText(RecordOf(symbol));

            // Import before expand; a stale copy is named and replaced; a same-name expansion of another definition or other actuals is
            // another class under the same name, which §9.3.12 forbids to share a run unit. It is an ERROR where the two classes will
            // necessarily share one: the other lives in an assembly this group references, or is the file placement (a) would overwrite.
            ExpansionPlacement? placement = null;
            string target = Path.Combine(external.OutputDirectory, ExternalizedNames.Form(externalized) + ".dll");
            var referenced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);   // not a COBOL word: file paths
            if (origin.Skeleton.From is { } definedIn) referenced.Add(definedIn.AssemblyFile);
            foreach (var entry in resolutionEntries.Concat(actualEntries))
            {
                var (c, i) = table.ResolveSpecifier(entry);
                if ((c?.Imported ?? i?.Imported) is { } importedFrom) referenced.Add(importedFrom.AssemblyFile);
                object? named = c is not null ? c : i;
                if (named is not null && placements.GetValueOrDefault(named) is { HoldingFile: { } held }) referenced.Add(held);
            }
            foreach (var other in external.ExpansionsNamed(externalized))
            {
                var theirs = other.Identity;
                bool sameTriple = ExternalizedNames.Same(theirs.DefinitionIdentity, identity.DefinitionIdentity)
                    && theirs.ActualIdentities.SequenceEqual(identity.ActualIdentities, ExternalizedNames.Comparer);
                if (sameTriple && theirs.Key == key && ExpansionKey.SurfaceText(other.Record) == mine)
                    placement ??= new ExpansionPlacement(identity, ExpansionWhere.Imported, other.From.AssemblyFile, other.From.ClrNamespace,
                        other.Record.Class!.ClrName, other.Record.Class.FactoryType, LocalNameOf(symbol), LocalFactoryNameOf(symbol));
                else if (sameTriple)
                {
                    if (reportedStale.Add(other.From.Mvid)) ReportStale(origin, externalized, other, identity, theirs.Key == key);
                }
                else ReportSameName(symbol, origin, externalized, other, shallNot: referenced.Contains(other.From.AssemblyFile)
                    || (!namesGroup && string.Equals(Path.GetFullPath(other.From.AssemblyFile), Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase)));   // not a COBOL word: file paths
            }
            if (placement is null)
            {
                if (!namesGroup) placement = WillWrite(symbol, origin, externalized, identity, target);
                else
                {
                    if (origin.Skeleton.From is not null && !origin.Skeleton.State.BindsAs(inputs.At(origin.Entry.Start.Line)))
                        ReportForeignState(symbol, origin, externalized);
                    placement = new ExpansionPlacement(identity, ExpansionWhere.InGroup);
                }
            }
            placements[symbol] = placement;
            // An imported expansion's text was compiled, and its warnings reported, by the compilation that made it; this one compiles
            // nothing of it and binds it only to type its callers, as it reads any class it imports (ISO §9.3.12 "treated in all respects
            // the same as if it were a class that is not a parameterized class"; DiagnosticRegion; kb/Work PB4483).
            if (placement.Where is ExpansionWhere.Imported) edition.WithdrawWarningsOf(origin.Region);
            if (placement.Where is ExpansionWhere.OwnAssembly)
                order.Add(new ExpansionToWrite(origin.Text, origin.Skeleton.State, placement,
                    edition.LocationOf(origin.Entry.Start.Line, origin.Entry.Start.Column), origin.Region));
            inProgress.Remove(symbol);
            return placement;
        }

        // Placement (a): the file <externalized name>.dll beside the output. Whatever is there is replaced only when it is an expansion of
        // this name (stale, REPO-9) or an assembly of another schema or version (REPO-12); anything else is not ours to overwrite.
        ExpansionPlacement WillWrite(object symbol, ExpansionOrigin origin, string externalized, ExpansionIdentity identity, string path)
        {
            string simple = ExternalizedNames.Form(externalized);
            var (state, why) = external.StateOfExpansionFile(path, externalized);
            switch (state)
            {
                case MetadataRepositoryProvider.TargetFile.Skewed:
                    edition.Warning(DiagnosticCatalog.RepositoryPreviousBuildSkew,
                        $"the expansion assembly '{path}' for \"{externalized}\" is replaced: {why} (ISO §8.13; DESIGN-external-repository §10.5)");
                    break;
                case MetadataRepositoryProvider.TargetFile.Foreign:
                    using (edition.At(origin.Entry))
                        edition.Error(DiagnosticCatalog.ExpandsPhraseInvalid,
                            $"{RepositoryWord(symbol)} '{externalized}' EXPANDS {origin.Skeleton.Name}: the expansion's own "
                            + $"assembly would be '{path}', which {why}; two classes with the same externalized name that are not expansions of one definition "
                            + "with the same actuals shall not share a run unit (ISO §9.3.12, §9.3.13)");
                    break;
            }
            var own = identity with { OwnAssembly = true };
            return new ExpansionPlacement(own, ExpansionWhere.OwnAssembly, path, CsNames.ModuleNamespaceOf(simple).ClrName,
                LocalNameOf(symbol), LocalFactoryNameOf(symbol), LocalNameOf(symbol), LocalFactoryNameOf(symbol));
        }

        void ReportStale(ExpansionOrigin origin, string externalized, MetadataRepositoryProvider.PublishedExpansion stale, ExpansionIdentity mine,
            bool sameKey)
        {
            string changed = sameKey ? "members" : ExpansionKey.FirstDifference(mine.Parts, stale.Identity.Parts);
            var referencers = external.ReferencersOf(stale.AssemblyName);
            using var at = edition.At(origin.Entry);
            edition.Warning(DiagnosticCatalog.RepositorySignatureDiffers,
                $"the expansion \"{externalized}\" of {stale.Identity.DefinitionIdentity} USING {string.Join(" ", stale.Identity.ActualIdentities)} in '{stale.From.AssemblyFile}' is stale: "
                + $"its {changed} changed since it was made, so it is not imported and this compilation makes the expansion. "
                + (referencers.Count == 0 ? "No assembly of the search space references it."
                    : $"These were compiled against the earlier class and are its stale callers, to be recompiled: {string.Join(", ", referencers.Select(r => $"'{r}'"))}.")
                + " (ISO §9.3.12, §8.13; DESIGN-external-repository §10.5)");
        }

        // REPO-2 (a warning) for a same-name, different-triple expansion that is merely elsewhere in the search space; COBOL2240 where it
        // is an assembly this group references or the file placement (a) would overwrite with a different class (§9.3.12's "shall not").
        void ReportSameName(object symbol, ExpansionOrigin origin, string externalized, MetadataRepositoryProvider.PublishedExpansion other,
            bool shallNot)
        {
            using var at = edition.At(origin.Entry);
            string what = $"the expansion \"{externalized}\" in '{other.From.AssemblyFile}' expands {other.Identity.DefinitionIdentity} USING "
                + $"{string.Join(" ", other.Identity.ActualIdentities)}, which is not what this compilation's expansion of that name is: two classes "
                + "with different actual parameters are not the same class instance and shall not have the same externalized object-class-name "
                + "within a run unit (ISO §9.3.12, §9.3.13; DESIGN-external-repository §10.5)";
            if (!shallNot) edition.Warning(DiagnosticCatalog.RepositoryEntryShadowedDiffers, what);
            else
                edition.Error(DiagnosticCatalog.ExpandsPhraseInvalid,
                    $"{RepositoryWord(symbol)} '{externalized}' EXPANDS {origin.Skeleton.Name}: {what}; the two necessarily share a run unit, because that "
                    + "assembly is one this compilation references or the file this expansion would be written to");
        }

        void ReportForeignState(object symbol, ExpansionOrigin origin, string externalized)
        {
            using var at = edition.At(origin.Entry);
            edition.Error(DiagnosticCatalog.ExpandsPhraseInvalid,
                $"{RepositoryWord(symbol)} '{externalized}' EXPANDS {origin.Skeleton.Name}: the skeleton was compiled under another compile state than this "
                + "compilation's, so its expansion must be compiled apart under that state, in an assembly of its own; but its code names a class "
                + "or interface of this compilation group, which an assembly of its own cannot reference (ISO §9.3.12, §12.3.8.4 GR5; "
                + "DESIGN-external-repository §10.5)");
        }
    }

    /// <summary>The REPOSITORY entry's leading words for the class or interface <paramref name="symbol"/> an expansion makes.</summary>
    private static string RepositoryWord(object symbol) => symbol is OoInterfaceSymbol ? "REPOSITORY INTERFACE" : "REPOSITORY CLASS";

    private static string LocalNameOf(object symbol) => symbol is OoClassSymbol c ? c.ClrName : ((OoInterfaceSymbol)symbol).ClrName;

    private static string? LocalFactoryNameOf(object symbol) => symbol is OoClassSymbol c ? c.FactoryClrName : null;

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
