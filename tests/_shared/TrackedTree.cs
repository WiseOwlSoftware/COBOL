// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System.Diagnostics;

namespace CobolNet.Tests.Shared;

/// <summary>
/// ⭐ THE COMMITTED TREE — the files git tracks, as repo-root-relative paths with <c>/</c> separators (kb/Work PB735,
/// PB927). Every byte-level sweep of the repository (<c>ConflictMarkerDriftTests</c>, <c>NulByteDriftTests</c>) draws
/// its population from here, and from git only: the alternative, walking the filesystem with a hand-written exclusion
/// list, needs that list because gitignored corpora (<c>tests/external/gnucobol/</c>) carry marker-shaped lines and
/// binary bytes as TEST DATA, whereas <c>git ls-files</c> excludes them structurally.
/// <para>
/// ⛔ A sweep reads the tree ONLY through a declared <see cref="TreeScope"/> (kb/Work PB3531): <c>scripts/spec/drift_rules.py</c>
/// reads the same declaration from the test's source, so <c>drift_rules.py &lt;file&gt;</c> names the sweep for every file
/// it reads, and the gate's planner puts the sweep in leg 1 for a change to one (tier 0d, <c>scripts/gate_plan.py</c>).
/// A sweep that filtered the whole list in code was invisible to both, and one such sweep stopped PB3523's landing on
/// Linux after the Windows gate had left it in leg 2.
/// </para>
/// <para>
/// It RAISES when git cannot be run: an empty list from a missing <c>git</c> would read as "the tree is clean"
/// (<c>feedback_verdict_evidence_invariant</c>). It lives beside <see cref="TestRepo"/> rather than in it because it
/// needs <see cref="ProcessObserver"/>, which the benchmark project (it links only <c>TestRepo.cs</c>) does not have.
/// </para>
/// </summary>
internal static class TrackedTree
{
    private static readonly Lazy<IReadOnlyList<string>> s_files = new(List);

    /// <summary>The tracked paths <paramref name="scope"/> holds, in git's order; git is asked once per test assembly.</summary>
    public static IReadOnlyList<string> Files(TreeScope scope) => [.. s_files.Value.Where(scope.Holds)];

    /// <summary><c>git ls-files -z</c> at the repo root. NUL-separated so a path with a space, a quote or a non-ASCII
    /// character arrives verbatim rather than in git's C-quoted form. Launched through the ONE observer, not another
    /// private launcher: it drains both pipes asynchronously and RAISES on a launch failure or a timeout instead of
    /// returning an empty string (<c>ProcessObservationDriftTests</c> keeps it collapsed).</summary>
    private static IReadOnlyList<string> List()
    {
        ProcessObservation obs = ProcessObserver.ObserveOrThrow(
            new ProcessStartInfo("git", "ls-files -z") { WorkingDirectory = TestRepo.Root });

        if (obs.ExitCode != 0)
            throw new InvalidOperationException($"`git ls-files -z` in {TestRepo.Root} exited {obs.ExitCode}: {obs.Stderr}");

        return obs.Stdout.Split('\0', StringSplitOptions.RemoveEmptyEntries);
    }
}

/// <summary>
/// ⛔ WHAT A SWEEP OF THE COMMITTED TREE READS, DECLARED AS DATA (kb/Work PB3531): the tracked paths under one of
/// <see cref="Under"/>, under none of <see cref="Except"/>, and ending in one of <see cref="Kinds"/> when it is given.
/// <list type="bullet">
/// <item><see cref="Under"/>: path prefixes, compared ordinally; <c>""</c> is the whole tree, and a directory ends in
/// <c>/</c>.</item>
/// <item><see cref="Except"/>: a directory (ending in <c>/</c>) and everything beneath it, or one exact path. A path
/// set aside BY NAME is the rule's owner (where its one pattern lives, or the test itself): the sweep does not read it,
/// and the drift-rule index lists the class as governing it specifically (<c>drift_rules.owners</c>).</item>
/// <item><see cref="Kinds"/>: file-name suffixes such as <c>.cs</c>, compared case-insensitively.</item>
/// </list>
/// <para>
/// ⛔ ONE DECLARATION SHAPE, so the drift-rule index reads it from the source without running anything: a field
/// <c>static readonly TreeScope Name = new(Under: [...], Except: [...], Kinds: [...]);</c> holding string literals only,
/// with named arguments. <c>scripts/spec/drift_rules.py</c> mirrors <see cref="Holds"/> (its <c>in_scope</c>), and
/// <c>TrackedTreeScopeDriftTests</c> holds every declaration the index reads equal to the value the compiler builds, and
/// every <see cref="TrackedTree.Files"/> call to a declared field. A scope may hold more than its sweep reads (a sweep
/// that narrows further by a list it reads at run time, as <c>SelfTestDiscoveryDriftTests</c> narrows to the live
/// tools' roots): the index then names the sweep for a file it skips, and never misses a file it reads.
/// </para>
/// </summary>
internal sealed record TreeScope(IReadOnlyList<string> Under, IReadOnlyList<string>? Except = null, IReadOnlyList<string>? Kinds = null)
{
    /// <summary>Whether the repo-root-relative <paramref name="path"/> is in the scope: the one filter, applied to the
    /// tracked tree by <see cref="TrackedTree.Files"/> and to a planted tree by a sweep's failure-branch test.</summary>
    public bool Holds(string path) =>
        Under.Any(u => path.StartsWith(u, StringComparison.Ordinal))
        && !(Except ?? []).Any(e => e.EndsWith('/') ? path.StartsWith(e, StringComparison.Ordinal) : path == e)
        && (Kinds is null || Kinds.Any(k => path.EndsWith(k, StringComparison.OrdinalIgnoreCase)));
}
