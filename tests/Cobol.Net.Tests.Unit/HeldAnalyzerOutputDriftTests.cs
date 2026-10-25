// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A LONG-LIVED PROCESS THAT LOADED A SOURCE GENERATOR MUST NOT BLOCK THE NEXT BUILD (kb/Work PB3674): the C#
/// language server of a session (csharp-ls, the LSP tool of MANDATORY-PRACTICES P13) loads each Roslyn component's
/// assembly from its <c>bin\</c> and holds it for the life of the session, long after the agent that used it has
/// returned, and Windows refuses to overwrite a loaded image. Every project that sets <c>IsRoslynComponent</c> therefore
/// carries the <c>MoveAsideHeldOutput</c> target, which deletes a stale output and renames a held one aside before the
/// copy, and <c>scripts/test_held_analyzer_output.py</c> (a script self-test, run by the self-test runner in every gate
/// and in CI's Windows self-tests) rebuilds the generator with a real process holding it.
/// </summary>
/// <remarks>
/// Before this rule the lock failed the landing's ABI-pin settle (read as a merge CONFLICT, PB3673) and a finisher's
/// gate in the worktree of an agent that had returned hours before; PB3339 had recorded it as a trap to remember.
/// </remarks>
[Trait("Sweep", "tree")]
public sealed class HeldAnalyzerOutputDriftTests
{
    /// <summary>The project files a Roslyn component can live in (declared, kb/Work PB3531, so the drift-rule index
    /// names this class for each of them).</summary>
    private static readonly TreeScope Projects = new(Under: ["src/", "tests/"], Kinds: [".csproj"]);

    /// <summary>Every project that is a Roslyn component (<c>IsRoslynComponent</c>) carries the target that keeps a held
    /// output from blocking its build, and at least one such project exists (the sweep scans something).</summary>
    [Fact]
    public void EveryRoslynComponent_MovesAsideAHeldOutput()
    {
        IReadOnlyList<string> projects = TrackedTree.Files(Projects);
        Assert.Contains("src/Cobol.Net.Compiler.SourceGen/Cobol.Net.Compiler.SourceGen.csproj", projects);
        var components = projects.Where(p => File.ReadAllText(Path.Combine(TestRepo.Root, p))
            .Contains("<IsRoslynComponent>true</IsRoslynComponent>", StringComparison.Ordinal)).ToList();
        Assert.NotEmpty(components);
        foreach (string project in components)
        {
            string text = File.ReadAllText(Path.Combine(TestRepo.Root, project));
            Assert.True(text.Contains("<Target Name=\"MoveAsideHeldOutput\" BeforeTargets=\"CopyFilesToOutputDirectory\">", StringComparison.Ordinal),
                $"{project} is a Roslyn component a language server loads from bin and holds: copy its MoveAsideHeldOutput target "
                + "(delete the stale output, rename a held one aside before the copy; kb/Work PB3674)");
        }
    }

    /// <summary>The self-test that builds with a real holder still declares each of its arms (held from its source, never
    /// run a second time: the self-test runner runs it, DESIGN-test-build-ci.md section 3.14.10).</summary>
    [Fact]
    public void TheHeldOutputSelfTest_DeclaresEveryArm() =>
        SelfTestArmSource.AssertDeclares(TestRepo.At("scripts/test_held_analyzer_output.py"), [
            "the generator project builds from a scratch copy",
            "premise: a process that loaded the output holds it (it cannot be opened for writing)",
            "a rebuild with a fresh output succeeds while the old output is held",
            "a rebuild with nothing new leaves bin as it was",
            "once the holder is gone the next fresh output deletes the held copy and leaves none",
        ]);
}
