// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Concurrent;
using WiseOwl.Cobol.Binding.Repository;
using WiseOwl.Cobol.CodeGen;
using WiseOwl.Cobol.Frontend.Preprocessor;
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
/// <see cref="RepositoryCodecTests"/> and <see cref="RepositoryReaderTests"/> lack is still met.
/// </summary>
public sealed class RepositoryRoundTripCorpusTests
{
    [Fact]
    public void EveryOutermostUnitOfTheCorpus_RoundTripsThroughItsRecord()
    {
        var cases = SuiteManifest.Manifest.Editions
            .SelectMany(e => e.Value.Values.Where(t => t.Expect == "accept")
                .Select(t => (Edition: int.Parse(e.Key, System.Globalization.CultureInfo.InvariantCulture), Test: t)))
            .ToList();
        Assert.True(cases.Count > 2_500, $"the manifest names only {cases.Count} accepted cases — the enumeration measured nothing");
        var failures = new ConcurrentBag<string>();
        var published = new ConcurrentBag<(string Case, RepositoryRecord Record)>();
        int units = 0;
        Parallel.ForEach(cases, c =>
        {
            string path = Path.Combine(SuiteManifest.Root, c.Test.Source);
            try
            {
                if (!InitialReferenceFormatOption.TryParse(c.Test.SourceFormat, out var format))
                    throw new InvalidOperationException($"source-format '{c.Test.SourceFormat}'");
                var comp = RepositoryCodecTests.BindFile(path, c.Edition, format);
                Interlocked.Add(ref units, RepositoryCodecTests.RoundTripAll(comp).Count);
                foreach (var unit in HostEntryEmitter.EntryUnits(comp.Units))
                    published.Add(($"{c.Edition}/{c.Test.Id}", RepositoryCodec.Of(unit)));
                foreach (var (record, _) in RepositoryCodecTests.OoRecordsOf(comp))
                    published.Add(($"{c.Edition}/{c.Test.Id}", record));
            }
            catch (Exception e)
            {
                failures.Add($"{c.Edition}/{c.Test.Id}: {e.GetType().Name}: {e.Message.Split('\n')[0]}");
            }
        });
        Assert.True(failures.IsEmpty, $"{failures.Count} of {cases.Count} corpus binds failed the record round trip:\n  "
            + string.Join("\n  ", failures.Order(StringComparer.Ordinal).Take(60)));
        Assert.True(units > cases.Count, $"only {units} units were round-tripped over {cases.Count} cases");

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

    /// <summary>The records one compiled library holds: large enough that the corpus compiles in tens of libraries, small
    /// enough that each compile is a few seconds.</summary>
    private const int LibrarySize = 500;
}
