// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.RepoDrift;

/// <summary>
/// ⛔ A LONG-LIVED PROCESS THAT LOADED A SOURCE GENERATOR MUST NOT BLOCK THE NEXT BUILD (kb/Work PB3674): the C#
/// language server of a session (csharp-ls, the LSP tool of MANDATORY-PRACTICES P13) loads each Roslyn component's
/// assembly from its <c>bin\</c> and holds it for the life of the session, long after the agent that used it has
/// returned, and Windows refuses to overwrite a loaded image. Every project that sets <c>IsRoslynComponent</c> therefore
/// runs the <c>MoveAsideHeldOutput</c> target, which deletes a stale output and renames a held one aside before the
/// copy. The target is defined ONCE, in <c>src/Directory.Build.targets</c>, for every src project that sets
/// <c>IsRoslynComponent</c> (kb/Work PB4950 round 7: a second component, the build's analyzers, would otherwise have needed a
/// copy), so every Roslyn component lives under <c>src/</c>; <c>scripts/test_held_analyzer_output.py</c> (a script self-test, run by the self-test runner in every gate
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

    /// <summary>Every project that is a Roslyn component (<c>IsRoslynComponent</c>) lies under <c>src/</c>, where
    /// <c>src/Directory.Build.targets</c> gives it the target that keeps a held output from blocking its build; the target is
    /// defined there, for exactly those projects, and in no project file; and at least one such project exists (the sweep
    /// scans something).</summary>
    [Fact]
    public void EveryRoslynComponent_MovesAsideAHeldOutput()
    {
        IReadOnlyList<string> projects = TrackedTree.Files(Projects);
        Assert.Contains("src/WiseOwl.Cobol.Compiler.SourceGen/WiseOwl.Cobol.Compiler.SourceGen.csproj", projects);
        var components = projects.Where(p => File.ReadAllText(Path.Combine(TestRepo.Root, p))
            .Contains("<IsRoslynComponent>true</IsRoslynComponent>", StringComparison.Ordinal)).ToList();
        Assert.NotEmpty(components);
        Assert.All(components, project => Assert.True(project.StartsWith("src/", StringComparison.Ordinal),
            $"{project} is a Roslyn component outside src/, where src/Directory.Build.targets does not reach: move it under src/ "
            + "(kb/Work PB3674)"));
        string targets = File.ReadAllText(TestRepo.Src("Directory.Build.targets"));
        Assert.Contains("<Target Name=\"MoveAsideHeldOutput\" BeforeTargets=\"CopyFilesToOutputDirectory\" "
            + "Condition=\"'$(IsRoslynComponent)' == 'true'\">", targets, StringComparison.Ordinal);
        Assert.All(projects, project => Assert.DoesNotContain("MoveAsideHeldOutput",
            File.ReadAllText(Path.Combine(TestRepo.Root, project)), StringComparison.Ordinal));
    }
}
