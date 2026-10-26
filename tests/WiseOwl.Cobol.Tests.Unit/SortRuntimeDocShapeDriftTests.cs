// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Runtime.IO;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A DESIGN DOC THAT WRITES A <c>CobolSort</c> CALL WITH ITS ARGUMENTS WRITES ONE THE SHIPPED RUNTIME TAKES (kb/Work
/// PB3339 review R3-1; CLAUDE.md rules 2 and 6). The deep-dives are what an implementer implements from, so a call they
/// spell in a shape the runtime no longer ships teaches a contract that does not compile. A tree-wide sweep of
/// <c>docs/</c>, kept apart from <see cref="SortWorkBoundDriftTests"/> (whose rules are specific to the store).
/// </summary>
[Trait("Sweep", "tree")]
public sealed class SortRuntimeDocShapeDriftTests
{
    /// <summary>EVERY DESIGN DOC THAT WRITES A <c>CobolSort</c> CALL WITH ITS ARGUMENTS NAMES A SHIPPED SHAPE (kb/Work
    /// PB3339 review R3-1; CLAUDE.md rule 6). The bounded store moved the key descriptors from <c>Sort</c>/<c>Merge</c> to
    /// <c>Init</c>, and DESIGN-locale-facility.md still taught the removed <c>Init(name, collation, national)</c>: a reader
    /// implementing from the deep-dive writes a call that does not compile and never learns that Init takes the keys.
    /// A prose call whose arguments are elided (<c>…</c>) names no shape and is not held. A shape is the arity, and an
    /// argument spelled as one of the member's parameter names stands at that parameter's position — the removed
    /// <c>Init(name, collation, national)</c> has an arity Init still takes, but puts <c>collation</c> where <c>keys</c> is.</summary>
    [Fact]
    public void EveryDocCallOfTheSortRuntime_NamesAShippedShape()
    {
        var methods = typeof(CobolSort).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Select(m => (m.Name, Parameters: m.GetParameters()))
            .ToList();
        static bool Takes(ParameterInfo[] ps, string[] args) =>
            ps.Count(p => !p.IsOptional) <= args.Length && args.Length <= ps.Length
            && args.Select((a, i) => Array.FindIndex(ps, p => p.Name == a) is var at && (at < 0 || at == i)).All(ok => ok);
        var mention = new Regex(@"`CobolSort\.(?<member>[A-Z]\w*)\((?<args>[^`()]*)\)`");
        var held = new List<string>();
        var stale = new List<string>();
        foreach (string doc in Directory.EnumerateFiles(TestRepo.Docs(), "*.md", SearchOption.AllDirectories))
        {
            foreach (Match m in mention.Matches(File.ReadAllText(doc)))
            {
                string args = m.Groups["args"].Value;
                if (args.Contains('…') || args.Contains("...", StringComparison.Ordinal)) continue;
                string[] given = args.Trim().Length == 0 ? [] : args.Split(',', StringSplitOptions.TrimEntries);
                string where = $"{Path.GetRelativePath(TestRepo.Root, doc)}: {m.Value}";
                held.Add(where);
                if (!methods.Any(x => x.Name == m.Groups["member"].Value && Takes(x.Parameters, given))) stale.Add(where);
            }
        }
        Assert.NotEmpty(held);   // the deep-dive's Init call is held, so the test is not vacuous
        Assert.True(stale.Count == 0,
            $"these docs write a CobolSort call no shipped member takes: {string.Join("; ", stale)} (CLAUDE.md rule 6)");
    }
}
