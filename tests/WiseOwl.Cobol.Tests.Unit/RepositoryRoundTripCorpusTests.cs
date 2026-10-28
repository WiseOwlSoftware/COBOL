// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Concurrent;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ EVERY PUBLISHED UNIT OF THE CORPUS ROUND-TRIPS THROUGH ITS REPOSITORY RECORD (DESIGN-external-repository §17.2; kb/Work
/// PB2100). The corpus is every case of the WiseOwl manifest (<see cref="SuiteManifest"/>: <c>conformance-suite/cases/**</c>
/// and <c>conformance-suite/wiseowl/cases/**</c>), bound at EVERY edition it is accepted at — 85 included, since an 85
/// program is a legal callee and publishes its record (§13) — and for each outermost program, function and prototype unit
/// <see cref="RepositoryCodecTests.RoundTrip"/> holds: <c>Decode(Of(unit))</c> answers every comparator and derived quantity
/// as the bound unit's LINKAGE does, its restricted pointers included (§6.3, §6.4). One Fact over the whole corpus, bound in
/// parallel, so a description shape the fixture of <see cref="RepositoryCodecTests"/> lacks is still met.
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
            }
            catch (Exception e)
            {
                failures.Add($"{c.Edition}/{c.Test.Id}: {e.GetType().Name}: {e.Message.Split('\n')[0]}");
            }
        });
        Assert.True(failures.IsEmpty, $"{failures.Count} of {cases.Count} corpus binds failed the record round trip:\n  "
            + string.Join("\n  ", failures.Order(StringComparer.Ordinal).Take(60)));
        Assert.True(units > cases.Count, $"only {units} units were round-tripped over {cases.Count} cases");
    }
}
