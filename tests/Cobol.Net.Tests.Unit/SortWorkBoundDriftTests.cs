// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Text.RegularExpressions;
using CobolNet.Runtime.IO;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE SORT-MERGE STORE HOLDS ITS RECORDS IN ONE BOUNDED CHUNK, AND NOWHERE ELSE IN MEMORY (kb/Work PB3339;
/// docs/CONFORMANCE.md §3 D-SWK). The store used to be a <c>List&lt;StoredFrame&gt;</c> of every released record, sorted
/// into a second full list, so a SORT larger than the process's memory ended in an OutOfMemoryException. Now every
/// record field of the sort-merge types is one of three, each bounded: <c>SortWork._chunk</c> (sealed to a run file
/// before it passes the limit), <c>SortRun._records</c> (a view of that chunk) and <c>SortMergeCursor._head</c> (one
/// record per merged run, at most the fan-in). A new record collection on any of them, or an add to the chunk that does
/// not first ask the limit, is an unbounded store again.
/// </summary>
public sealed class SortWorkBoundDriftTests
{
    /// <summary>The bounded record fields, by declaring type and name.</summary>
    private static readonly HashSet<string> Allowed = ["SortWork._chunk", "SortRun._records", "SortMergeCursor._head"];

    private static bool HoldsRecords(Type t) =>
        t == typeof(StoredFrame[])
        || (t.IsGenericType && t.GetGenericArguments().Any(a => a == typeof(StoredFrame) || a == typeof(StoredFrame?)))
        || (t.IsArray && t.GetElementType() is { } e && HoldsRecords(e));

    [Fact]
    public void EveryRecordFieldOfTheSortMergeTypes_IsABoundedOne()
    {
        var types = typeof(SortWork).Assembly.GetTypes()
            .Where(t => t.Namespace == typeof(SortWork).Namespace
                && (t.Name.StartsWith("Sort", StringComparison.Ordinal) || t.DeclaringType?.Name.StartsWith("Sort", StringComparison.Ordinal) == true))
            .ToList();
        Assert.Contains(typeof(SortStore), types);
        Assert.Contains(typeof(SortFileTable), types);
        var found = types
            .SelectMany(t => t.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Where(f => HoldsRecords(f.FieldType))
                .Select(f => $"{t.Name}.{f.Name}"))
            .ToHashSet();
        Assert.True(found.SetEquals(Allowed),
            $"the record fields of the sort-merge types are [{string.Join(", ", found.Order())}], not exactly "
            + $"[{string.Join(", ", Allowed.Order())}]: a record collection outside the bounded chunk is an unbounded store "
            + "(kb/Work PB3339)");
    }

    /// <summary>The one add to the chunk asks the limit first, in the same method.</summary>
    [Fact]
    public void TheChunkGrowsOnlyBehindTheLimit()
    {
        string src = File.ReadAllText(TestRepo.Src("Cobol.Net.Runtime", "IO", "SortWork.cs"));
        Assert.Single(Regex.Matches(src, @"_chunk\.Add\("));
        var add = Regex.Match(src, @"public void Add\(in StoredFrame frame\)\s*\{(?<body>.*?)\n    \}", RegexOptions.Singleline);
        Assert.True(add.Success, "SortWork.Add is not where this test looks for it");
        string body = add.Groups["body"].Value;
        int test = body.IndexOf("_chunkBytes + bytes > _memory.LimitBytes", StringComparison.Ordinal);
        int grow = body.IndexOf("_chunk.Add(", StringComparison.Ordinal);
        Assert.True(test >= 0 && grow > test, "SortWork.Add grows the chunk without first sealing it at the limit");
    }
}
