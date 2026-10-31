// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Text.Json;
using WiseOwl.Cobol.Tests.Shared;

namespace WiseOwl.Cobol.Tests.Unit.Architecture;

/// <summary>
/// The newest committed architecture census record (<c>docs/rearchitecture/evidence/arch-census/&lt;sha&gt;.json</c>,
/// kb/Work PB2115), the one reader of it the drift tests share: records are named by the commit they measured, and the
/// newest is the one with the latest commit date.
/// </summary>
internal static class CensusRecord
{
    /// <summary>The record with the latest commit date. The caller disposes it.</summary>
    public static JsonDocument Newest()
    {
        string dir = TestRepo.Docs("rearchitecture", "evidence", "arch-census");
        var records = Directory.EnumerateFiles(dir, "*.json")
            .Where(f => !f.EndsWith(".findings.json", StringComparison.Ordinal))
            .Select(f => (Path: f, Doc: JsonDocument.Parse(File.ReadAllText(f))))
            .ToList();
        if (records.Count == 0)
        {
            throw new InvalidOperationException($"no census record under {dir}: run `python scripts/arch/census.py`");
        }

        var newest = records.MaxBy(r => DateTimeOffset.Parse(r.Doc.RootElement.GetProperty("commitDate").GetString()!,
            CultureInfo.InvariantCulture));
        foreach (var r in records.Where(r => r.Path != newest.Path))
        {
            r.Doc.Dispose();
        }

        return newest.Doc;
    }

    /// <summary>The record's namespace-to-namespace edges (<c>namespaceEdges</c>: from, to, distinct type pairs).</summary>
    public static IReadOnlyDictionary<(string From, string To), int> NamespaceEdges(JsonElement record) =>
        record.GetProperty("namespaceEdges").EnumerateArray()
            .ToDictionary(e => (e[0].GetString()!, e[1].GetString()!), e => e[2].GetInt32());

    /// <summary>Each census type's generated flag, by full metadata name (the record's <c>types</c> table).</summary>
    public static IReadOnlyDictionary<string, bool> GeneratedFlags(JsonElement record)
    {
        var columns = record.GetProperty("typeColumns").EnumerateArray().Select(e => e.GetString()).ToList();
        int id = columns.IndexOf("id"), generated = columns.IndexOf("generated");
        return record.GetProperty("types").EnumerateArray()
            .ToDictionary(t => t[id].GetString()!, t => t[generated].GetBoolean(), StringComparer.Ordinal);
    }
}
