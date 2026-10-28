// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Runtime;
using Xunit;

namespace WiseOwl.Cobol.Tests.Characterization;

/// <summary>
/// Gate 3 (rearchitecture Phase 0): snapshot the GENERATED C# for every positive corpus program — advisory/reviewed
/// proof that a pure-reorg refactor left emission byte-identical. An intentional emit change re-baselines with review
/// (while gate 1 proves it behavior-preserving). Negative programs emit no C# and are covered by gate 2.
/// Seed/re-baseline with <c>WISEOWL_COBOL_UPDATE_SNAPSHOTS=1</c> (local only).
/// <para>The runtime version a module records (<c>[assembly: CobolRepository(…)]</c> and
/// <c>__CobolModule.EnsureRegistered</c>) is the package version, which every runtime major bump raises
/// (RuntimeAbiPinDriftTests), and the call ABI recorded beside it is <see cref="RuntimeAbi.CallAbi"/>, which every
/// boundary-layout change raises. They are snapshotted as <see cref="RuntimeVersionToken"/> and <see cref="CallAbiToken"/>,
/// substituted only where both equal this runtime's own: a version or call-ABI bump re-records nothing, while an emitter
/// that stamps any other version or ABI still diffs. The exact text of both stamps is
/// RuntimeAbiPinDriftTests.TheEmittedRecord_StatesTheVersionsThisRuntimeDeclares' to hold. Before this, every major bump
/// (and, until PB3570's finisher on 2026-10-23, every call-ABI bump) left all of these snapshots red under a gate whose
/// leg 1 never planned them (a version change reaches no test through code impact), and only the landing's whole Linux
/// run saw it (PB1099's finisher, 2026-10-16).</para>
/// </summary>
public sealed class EmittedCSharpSnapshotTests
{
    /// <summary>What a snapshot holds in place of the runtime's own version.</summary>
    private const string RuntimeVersionToken = "{RUNTIME-VERSION}";

    /// <summary>What a snapshot holds in place of the runtime's own call ABI.</summary>
    private const string CallAbiToken = "{CALL-ABI}";

    /// <summary>The two stamps that record the runtime version and the call ABI, each followed by this runtime's own
    /// version literal and call ABI.</summary>
    private static readonly Regex VersionStamp = new(
        $"""(CobolRepositoryAttribute\(\d+, |RegisterModule\("[^"]*", )"{Regex.Escape(RuntimeAbi.Version.ToString())}", {RuntimeAbi.CallAbi}(?=, )""",
        RegexOptions.CultureInvariant);

    /// <summary>What a snapshot holds in place of the compiler's own version (the record's <c>CompilerVersion</c>: the
    /// compiler assembly's version, which every package version bump raises beside the runtime's; named in diagnostics and
    /// never compared, DESIGN-external-repository §5.4 item 2).</summary>
    private const string CompilerVersionToken = "{COMPILER-VERSION}";

    /// <summary>The record's compiler-version stamp: a dotted numeric version.</summary>
    private static readonly Regex CompilerStamp = new("(CompilerVersion = )\"[0-9]+(\\.[0-9]+){1,3}\"", RegexOptions.CultureInvariant);

    public static IEnumerable<object[]> Positive() => CharacterizationCorpus.Positive();

    [Theory]
    [MemberData(nameof(Positive))]
    public void EmittedCSharp_MatchesSnapshot(string name, int edition)
    {
        string path = CharacterizationCorpus.SourcePath(name, negative: false);
        var probe = CompilerProbe.Probe(path, edition, emit: true);
        Assert.True(probe.Ok, $"positive characterization program '{name}' must compile: "
            + string.Join("; ", probe.Diagnostics));
        Assert.NotNull(probe.EmittedCSharp);
        string file = System.IO.Path.Combine(CharacterizationCorpus.SnapshotDir, $"{name}.{edition}.g.cs.txt");
        string text = VersionStamp.Replace(probe.EmittedCSharp!, $"$1\"{RuntimeVersionToken}\", {CallAbiToken}");
        Snapshot.Assert(file, CompilerStamp.Replace(text, $"$1\"{CompilerVersionToken}\""));
    }
}
