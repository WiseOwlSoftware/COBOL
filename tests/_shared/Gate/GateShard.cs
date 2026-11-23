// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
#nullable enable
using System.Globalization;
using System.Text.RegularExpressions;
using Xunit.Abstractions;

namespace WiseOwl.Cobol.Tests.Shared.Gate;

/// <summary>THE SHARD HOOK (kb/Work PB4329; docs/rearchitecture/DESIGN-test-build-ci.md section 1.4): CI's merge group
/// runs every assembly as <see cref="Count"/> shards, the job of shard <see cref="Index"/> setting
/// <c>WISEOWL_COBOL_SHARD=k/N</c>, and each host runs the cases of the test classes <see cref="TestWeights.Deal"/> gives
/// shard k. Discovery never sees it (the executor deals), so <c>--list-tests</c> stays the whole population that
/// <c>ci-gate</c> checks the shards' trx against (test_population.py check): a case dealt to no shard or two is a named
/// NEVER RAN or RAN TWICE, never silence.</summary>
internal sealed partial record GateShard(int Index, int Count)
{
    public const string Variable = "WISEOWL_COBOL_SHARD";

    /// <summary>The first words of every refused case's message.</summary>
    public const string RefusalPrefix = "GATE SHARD REFUSED: ";

    [GeneratedRegex(@"\A(?<k>[1-9][0-9]{0,2})/(?<n>[1-9][0-9]{0,2})\z")]
    private static partial Regex Spelling();

    /// <summary>The shard <paramref name="environment"/> names, or null when <see cref="Variable"/> is unset or empty
    /// (Windows cannot hold an empty variable). Throws <see cref="GateShardException"/> for any other spelling than
    /// <c>k/N</c> with 1 &lt;= k &lt;= N: a shard nobody can run must refuse the run, never run the whole of it.</summary>
    public static GateShard? Read(Func<string, string?> environment)
    {
        string? text = environment(Variable);
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        Match m = Spelling().Match(text);
        int k = m.Success ? int.Parse(m.Groups["k"].Value, CultureInfo.InvariantCulture) : 0;
        int n = m.Success ? int.Parse(m.Groups["n"].Value, CultureInfo.InvariantCulture) : 0;
        return m.Success && k <= n
            ? new GateShard(k, n)
            : throw new GateShardException($"{Variable} is '{text}', not k/N with 1 <= k <= N");
    }

    /// <summary>This shard's cases, in their discovery order: those whose test class the weights deal to it.</summary>
    public List<T> CasesOf<T>(IEnumerable<T> cases, string assemblyKey, TestWeights weights) where T : ITestCase
    {
        var all = cases.ToList();
        Dictionary<string, int> dealt = weights.Deal(Count, assemblyKey, all.Select(c => ClassOf(c)));
        return all.Where(c => dealt[ClassOf(c)] == Index).ToList();
    }

    /// <summary>A case's test class, as the trx's <c>className</c> and the weights file spell it.</summary>
    public static string ClassOf(ITestCase testCase) => testCase.TestMethod.TestClass.Class.Name;

    public override string ToString() => $"{Index}/{Count}";
}

/// <summary>A shard variable no host can run.</summary>
internal sealed class GateShardException(string message) : Exception(message);
