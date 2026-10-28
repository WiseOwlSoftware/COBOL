// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Concurrent;
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Binding.Repository;
using WiseOwl.Cobol.CodeGen;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Runtime.Repository;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ EVERY PUBLISHED UNIT OF THE CORPUS ROUND-TRIPS THROUGH ITS REPOSITORY RECORD (DESIGN-external-repository §17.2; kb/Work
/// PB2100). The corpus is every case of the WiseOwl manifest (<see cref="SuiteManifest"/>: <c>conformance-suite/cases/**</c>
/// and <c>conformance-suite/wiseowl/cases/**</c>), bound at EVERY edition it is accepted at — 85 included, since an 85
/// program is a legal callee and publishes its record (§13) — and for each outermost program, function and prototype unit,
/// and each method of each class and interface, <see cref="RepositoryCodecTests.RoundTrip"/> holds: <c>Decode(Of(unit))</c>
/// answers every comparator and derived quantity as the bound unit's LINKAGE does, its restricted pointers included (§6.3,
/// §6.4); and every record the module publishes (§7.2: every outermost unit but a prototype its group defines, every class
/// and every interface) is rendered by <c>RepositoryAttributeRenderer</c>, compiled and read back by <c>RepositoryReader</c>
/// EQUAL to the record (§6.2, §7.4), so the attribute encoding holds for every shape the corpus has. One Fact over the whole corpus, bound in parallel, so a description shape the fixtures of
/// <see cref="RepositoryCodecTests"/> and <see cref="RepositoryReaderTests"/> lack is still met. Beside it, every prototype and
/// definition pair of one group compares the same decoded as bound (<see cref="PrototypeDefinitionPairs"/>, §17.2's SR2 pair).
/// </summary>
public sealed class RepositoryRoundTripCorpusTests
{
    /// <summary>§17.2's corpus: every case of the WiseOwl manifest at every edition it is accepted at, as (edition, id, path,
    /// reference format). The one enumeration both corpus Facts read (this class's and
    /// <c>HostEntryShapeDriftTests.EveryOutermostUnitOfTheCorpus_PublishesExactlyOneEntryAndRecord</c>).</summary>
    internal static IReadOnlyList<(int Edition, string Id, string Path, InitialReferenceFormat Format)> Corpus()
    {
        var cases = SuiteManifest.Manifest.Editions
            .SelectMany(e => e.Value.Values.Where(t => t.Expect == "accept")
                .Select(t => (Edition: int.Parse(e.Key, System.Globalization.CultureInfo.InvariantCulture), Test: t)))
            .Select(c => (c.Edition, c.Test.Id, Path.Combine(SuiteManifest.Root, c.Test.Source),
                InitialReferenceFormatOption.TryParse(c.Test.SourceFormat, out var format) ? format
                    : throw new InvalidOperationException($"{c.Test.Id}: source-format '{c.Test.SourceFormat}'")))
            .ToList();
        Assert.True(cases.Count > 2_500, $"the manifest names only {cases.Count} accepted cases — the enumeration measured nothing");
        return cases;
    }

    [Fact]
    public void EveryOutermostUnitOfTheCorpus_RoundTripsThroughItsRecord()
    {
        var cases = Corpus();
        var failures = new ConcurrentBag<string>();
        var published = new ConcurrentBag<(string Case, RepositoryRecord Record)>();
        int units = 0, pairs = 0;
        Parallel.ForEach(cases, c =>
        {
            try
            {
                var comp = RepositoryCodecTests.BindFile(c.Path, c.Edition, c.Format);
                Interlocked.Add(ref units, RepositoryCodecTests.RoundTripAll(comp).Count);
                Interlocked.Add(ref pairs, PrototypeDefinitionPairs(comp, $"{c.Edition}/{c.Id}", failures));
                foreach (var unit in HostEntryEmitter.EntryUnits(comp.Units))
                    published.Add(($"{c.Edition}/{c.Id}", RepositoryCodec.Of(unit)));
                foreach (var (record, _) in RepositoryCodecTests.OoRecordsOf(comp))
                    published.Add(($"{c.Edition}/{c.Id}", record));
            }
            catch (Exception e)
            {
                failures.Add($"{c.Edition}/{c.Id}: {e.GetType().Name}: {e.Message.Split('\n')[0]}");
            }
        });
        Assert.True(failures.IsEmpty, $"{failures.Count} of {cases.Count} corpus binds failed the record round trip:\n  "
            + string.Join("\n  ", failures.Order(StringComparer.Ordinal).Take(60)));
        Assert.True(units > cases.Count, $"only {units} units were round-tripped over {cases.Count} cases");
        Assert.True(pairs > 20, $"only {pairs} prototype and definition pairs were compared — the SR2 enumeration measured nothing");

        // Through metadata: libraries of distinct (kind, externalized name), each compiled once and read back.
        var libraries = new List<List<(string Case, RepositoryRecord Record)>>();
        var keys = new List<HashSet<string>>();
        foreach (var entry in published.OrderBy(p => p.Case, StringComparer.Ordinal).ThenBy(p => p.Record.ExternalizedName, StringComparer.Ordinal))
        {
            // One unit per (kind, externalized name), and one CLR type per factory type name (a class's factory is a type the
            // library declares by the name its record states).
            string[] entryKeys = [$"{entry.Record.Kind}:{entry.Record.ExternalizedName}", .. entry.Record.Class?.FactoryType is { } f ? [$"factory:{f}"] : Array.Empty<string>()];
            int at = keys.FindIndex(k => k.Count < LibrarySize && !entryKeys.Any(k.Contains));
            if (at < 0) { libraries.Add([]); keys.Add([]); at = libraries.Count - 1; }
            libraries[at].Add(entry);
            keys[at].UnionWith(entryKeys);
        }
        var readFailures = new ConcurrentBag<string>();
        Parallel.ForEach(libraries, library =>
        {
            try
            {
                var read = RepositoryReaderTests.ReadLibrary([.. library.Select(l => l.Record)]);
                for (int i = 0; i < library.Count; i++)
                    if (read[i] != library[i].Record) readFailures.Add($"{library[i].Case} {library[i].Record.Name}: the record read back differs");
            }
            catch (Exception e)
            {
                readFailures.Add($"a library of {library.Count} ({library[0].Case} ...): {e.GetType().Name}: {e.Message.Split('\n')[0]}");
            }
        });
        Assert.True(readFailures.IsEmpty, $"{readFailures.Count} records failed the metadata round trip:\n  "
            + string.Join("\n  ", readFailures.Order(StringComparer.Ordinal).Take(60)));
        Assert.True(published.Count > cases.Count, $"only {published.Count} records were published over {cases.Count} cases");
        int classes = published.Count(p => p.Record.Kind == RepositoryUnitKind.Class), interfaces = published.Count(p => p.Record.Kind == RepositoryUnitKind.Interface);
        Assert.True(classes > 300 && interfaces > 50, $"only {classes} class and {interfaces} interface records were published — the OO arm measured nothing");
    }

    /// <summary>§17.2's SR2-pair check for one group: for every prototype the group also defines (ISO §10.6.2 SR2 / SR3, the
    /// pairs kb/Work PB1115 compares; ISO §13.7.3 SR2: "The description of the formal parameters and the returning item that
    /// appear in the linkage section of a function prototype or a program prototype shall match the description of the
    /// formal parameters and the returning item in the corresponding function definition or program definition"),
    /// <c>PrototypeSignatures.Same</c> of the two DECODED records holds, and agrees with <c>Same</c> of the bound pair — so
    /// a later compilation that reads the prototype's record and the definition's sees the match the group saw. Returns the
    /// number of pairs compared.</summary>
    private static int PrototypeDefinitionPairs(BoundCompilation comp, string at, ConcurrentBag<string> failures)
    {
        int pairs = 0;
        foreach (var prototype in comp.Units.Where(u => u is { Parent: null, IsPrototype: true }))
            foreach (var definition in comp.Units.Where(d => d is { Parent: null, IsPrototype: false } && d.IsFunction == prototype.IsFunction
                         && ExternalizedNames.Same(d.ExternalizedName, prototype.ExternalizedName)))
            {
                bool bound = PrototypeSignatures.Same(new CalleeSignature(prototype.Data.LinkageFormals, prototype.Data.LinkageReturning),
                    new CalleeSignature(definition.Data.LinkageFormals, definition.Data.LinkageReturning));
                bool decoded = PrototypeSignatures.Same(RepositoryCodec.Decode(RepositoryCodec.Of(prototype), comp.SignatureClasses).Signature,
                    RepositoryCodec.Decode(RepositoryCodec.Of(definition), comp.SignatureClasses).Signature);
                if (!bound || !decoded)
                    failures.Add($"{at}: the prototype {prototype.Name} and the definition {definition.Name} are Same {bound} bound and {decoded} decoded");
                pairs++;
            }
        return pairs;
    }

    /// <summary>The records one compiled library holds: large enough that the corpus compiles in tens of libraries, small
    /// enough that each compile is a few seconds.</summary>
    private const int LibrarySize = 500;
}
