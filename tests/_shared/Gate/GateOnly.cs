// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
#nullable enable
using Xunit.Abstractions;

namespace WiseOwl.Cobol.Tests.Shared.Gate;

/// <summary>THE RETRY HOOK (kb/Work PB5099; docs/rearchitecture/DESIGN-test-build-ci.md section 3.14.12): a re-gate of a
/// tree an earlier run of this worktree already measured runs only the cases that run did not pass. The gate writes
/// their display names, one per line, to the file <see cref="Variable"/> names, and the host runs exactly the discovered
/// cases of those names. Discovery never sees it, like the shard, so <c>--list-tests</c> stays the whole population the
/// gate checks the composition against (<c>scripts/test_population.py</c> <c>check</c>, its <c>carried</c> passes): a
/// case neither run nor carried is a named NEVER RAN, never silence. Its prefix is one the population tool scrubs, so
/// only the gate hands it to a host.</summary>
internal sealed record GateOnly(IReadOnlySet<string> Names)
{
    public const string Variable = "WISEOWL_COBOL_GATE_ONLY";

    /// <summary>The retry <paramref name="environment"/> names, or null when <see cref="Variable"/> is unset or empty
    /// (Windows cannot hold an empty variable). Throws when the file cannot be read, or names no case: a retry nobody can
    /// run must refuse the run, never run the whole of it. A name is a whole line (display names carry no line break:
    /// xunit escapes one), kept exactly, so only an empty line is skipped.</summary>
    public static GateOnly? Read(Func<string, string?> environment)
    {
        string? path = environment(Variable);
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        HashSet<string> names = File.ReadAllLines(path).Where(l => l.Length > 0).ToHashSet(StringComparer.Ordinal);
        return names.Count > 0
            ? new GateOnly(names)
            : throw new GateOnlyException($"{Variable} names the file '{path}', which names no case");
    }

    /// <summary>The cases of <paramref name="cases"/> the retry names, in their order: by the display name the host runs
    /// under, or by its LISTED form. A listing (<c>--list-tests</c>, and the trx's test definitions) names a long theory
    /// row by its display name cut short and ended with <see cref="Truncation"/> (a 474-character
    /// <c>UniversalCrossingShapeDriftTests</c> row is listed in 447), while the host's case keeps the whole name, so a
    /// retry written from the listing named eight such rows that never ran (kb/Work PB4333, run
    /// 000004-20261116T085419Z). A listed name ending in the mark names every case whose display name it begins; a row's
    /// own argument cut short (<c>"abc"···)</c>) ends in a parenthesis, never in the mark.</summary>
    public List<T> CasesOf<T>(IEnumerable<T> cases) where T : ITestCase
    {
        string[] stems = [.. Names.Where(n => n.EndsWith(Truncation, StringComparison.Ordinal)).Select(n => n[..^Truncation.Length])];
        return cases.Where(c => Names.Contains(c.DisplayName)
            || stems.Any(s => c.DisplayName.StartsWith(s, StringComparison.Ordinal))).ToList();
    }

    /// <summary>The mark a listing ends a display name it cut short with (three U+00B7 MIDDLE DOTs, xunit's).</summary>
    public const string Truncation = "···";
}

/// <summary>A retry file no host can run.</summary>
internal sealed class GateOnlyException(string message) : Exception(message);
