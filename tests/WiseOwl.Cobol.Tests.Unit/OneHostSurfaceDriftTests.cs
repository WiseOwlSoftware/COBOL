// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ ONE HOST SURFACE (DESIGN-external-repository §4.4, §17.3; owner decision 2026-10-05; kb/Work PB2099). A .NET host
/// activates a WiseOwl COBOL program through the program's generated host entry, <c>Cobol.&lt;assembly&gt;.&lt;NAME&gt;.Call</c>
/// and <c>Cancel</c>, and through nothing else: <c>ProgramRegistry</c> is the generated code's ABI, public only because
/// generated code in other assemblies calls it. This class is red when a test's code (its host sources included) or a
/// user-facing document names an untyped activation route — <c>ProgramRegistry.CallProgram</c>, <c>Cancel</c>,
/// <c>RunMain</c> or <c>RunModule</c>, the run unit's <c>Programs.CallProgram</c>/<c>Cancel</c>/<c>RunMain</c>/<c>RunModule</c>,
/// <c>ICobolProgram.Call</c> or <c>Activate</c>, or a module's <c>__CobolModule.Register</c> — so a host built from them
/// cannot grow back beside the entry.
/// <para><b>What it scans.</b> Every test <c>.cs</c> file's CODE, read as Roslyn tokens so comments are not scanned and
/// string literals (the C# hosts a test compiles) are. The documents a user reads — <c>README.md</c>,
/// <c>docs/CONFORMANCE.md</c>, <c>docs/manual/</c> and <c>samples/</c> — are held to the same pattern (the one rule,
/// <c>tests/doc-facts/untyped-host-routes.json</c>) by <c>scripts/docchecks/design.py</c>, because no compiler test opens a
/// Markdown file (kb/Work PB4453). The generated code and the runtime are the
/// ABI's own home and are not scanned, and neither are the maintainer design documents, which describe that ABI, nor
/// the history (<c>DEVLOG/</c>, <c>kb/</c>, <c>adjudication/</c>). The ABI's own tests — the classes that assert what
/// generated code emits or that pin the boundary — are listed in <see cref="AbiTests"/>, each with its reason.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class OneHostSurfaceDriftTests
{
    /// <summary>The untyped activation routes. A member access on the facade, on the run unit's program table, on the
    /// program interface, or the module's registrar; <c>GetMethod("Register")</c> is the same registrar reached by
    /// reflection.</summary>
    private static readonly Regex Route = new(
        DocFacts.Load("untyped-host-routes.json").GetProperty("pattern").GetString()!, RegexOptions.CultureInvariant);

    /// <summary>The ABI's own tests: each names a route because its subject is the generated code that calls it.</summary>
    private static readonly Dictionary<string, string> AbiTests = new(StringComparer.Ordinal)
    {
        ["HostEntryShapeDriftTests.cs"] = "asserts that a generated host entry's body makes exactly one boundary call and that its Cancel is one boundary cancel",
        ["RuntimeAbiPinDriftTests.cs"] = "pins the generated boundary's CallProgram and Register calls of the ABI fixture",
        ["OperandActivationDriftTests.cs"] = "asserts which generated statements activate a program through the boundary",
    };

    /// <summary>The COMMITTED C# under <c>tests/</c> (kb/Work PB3814): a build output (<c>bin/</c>, <c>obj/</c>) and the
    /// compiler's generated C# the corpus runs write beside the tests (<c>tests/nist/output/</c> by the NIST guard, which the
    /// Linux gate runs in the same clone as this leg) are untracked, so a walk of the directory from the file system is
    /// never needed.</summary>
    private static readonly TreeScope TestSourceScope = new(Under: ["tests/"], Kinds: [".cs"]);

    private static IEnumerable<string> TestSources() => TrackedTree.Files(TestSourceScope).Select(rel => TestRepo.At(rel.Split('/')));

    /// <summary>A C# file's code without its comments: the text of every token, string literals included.</summary>
    private static string Code(string file) =>
        string.Join(" ", CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetRoot()
            .DescendantTokens().Select(t => t.Text));

    [Fact]
    public void NoTest_ActivatesAProgramThroughAnUntypedRoute()
    {
        var offenders = TestSources()
            .Where(f => !AbiTests.ContainsKey(Path.GetFileName(f)))
            .Select(f => (File: Path.GetRelativePath(TestRepo.Root, f), Hit: Route.Match(Code(f))))
            .Where(x => x.Hit.Success)
            .Select(x => $"{x.File}: {x.Hit.Value}")
            .ToList();
        Assert.True(offenders.Count == 0,
            "a .NET host activates a program through its generated host entry, Cobol.<assembly>.<NAME>.Call/Cancel "
            + "(DESIGN-external-repository §4.4); these tests name an untyped route instead:\n" + string.Join("\n", offenders));
    }

    [Fact]
    public void EveryAbiTest_StillNamesTheAbi()
    {
        // A listed file that no longer names a route is a stale exemption, which would let a future host slip in.
        var stale = AbiTests.Keys
            .Where(name => TestSources().Where(f => Path.GetFileName(f) == name).All(f => !Route.IsMatch(Code(f))))
            .ToList();
        Assert.True(stale.Count == 0, "listed as ABI tests but naming no route (remove them): " + string.Join(", ", stale));
    }
}
