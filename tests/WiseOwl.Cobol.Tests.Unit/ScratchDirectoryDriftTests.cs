// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Tests.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A TEST'S SCRATCH DIRECTORY IS MADE AND REMOVED BY <c>ScratchDirectory</c>, AND NO TEST SWALLOWS A FAILED DELETE
/// (kb/Work PB3194, review R1-2).
///
/// <para><b>What happened.</b> About a hundred test classes made a directory in the machine-wide temp directory and
/// removed it with <c>try { Directory.Delete(dir, recursive: true); } catch { }</c>. Every failed delete was silent, and
/// the witness tests that loaded the program they compiled into the test host made every one of theirs fail on Windows:
/// 16,096 <c>WiseOwlCobol_Tests_*</c> directories in one host's temp directory by 2026-10-23. <c>tests/_shared/ScratchDirectory.cs</c>
/// is now the one way (its summary gives the mechanism); this test keeps it so.</para>
///
/// <para><b>The rules, over every C# file under <c>tests/</c> (<c>ScratchDirectory.cs</c> itself excepted).</b>
/// <list type="number">
///   <item>No recursive directory delete (<c>Directory.Delete(d, true)</c>, <c>info.Delete(recursive: true)</c>): call
///     <c>ScratchDirectory.Remove(d)</c>, which removes read-only entries too and reports what it cannot.</item>
///   <item>No <c>Directory.CreateTempSubdirectory</c>, and no unique directory name under <c>Path.GetTempPath()</c>
///     (<c>Path.Combine(Path.GetTempPath(), "x-" + Guid.NewGuid()…)</c> with no file extension): call
///     <c>ScratchDirectory.Create(prefix)</c>, whose directories are reaped when their test host has ended.</item>
///   <item>No assembly loaded from a path (<c>Assembly.LoadFrom</c>, <c>Assembly.LoadFile</c>,
///     <c>LoadFromAssemblyPath</c>): call <c>ScratchDirectory.LoadAssembly(path)</c>, which holds no mapping of the
///     file.</item>
///   <item>No <c>try</c> that deletes (<c>File.Delete</c>, <c>Directory.Delete</c>, <c>….Delete()</c>) with an empty catch:
///     a failed delete is reported (<c>HarnessLog.ReportLeft</c>), never swallowed.</item>
/// </list>
/// The exceptions are named here with their reasons (<see cref="Exempt"/>). Each detector is driven first by a fixture
/// that MUST trip it, so a detector that stopped looking turns this test red rather than green.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class ScratchDirectoryDriftTests
{
    /// <summary>The sites a rule does not govern, each with its reason: (file, rule, a text the line holds).</summary>
    private static readonly (string File, int Rule, string Holds, string Why)[] Exempt =
    [
        ("tests/_shared/RunDirectoryState.cs", 1, "Directory.Delete(Path.Combine(_root, relative), recursive: true)",
         "the retry's RESTORE of a run directory, not a cleanup: a failure must raise, and its caller reports the directory "
         + "unrestorable (a HARNESS NON-OBSERVATION, kb/Work PB3388)"),
        ("tests/WiseOwl.Cobol.Tests.Conformance/NistWrittenFilesTests.cs", 2, "WiseOwlCobol_NoSuchGolden_",
         "a path that must NOT exist: the test is that a missing golden directory is refused, and nothing creates it"),
    ];

    [Fact]
    public void NoTestDeletesItsScratchDirectoryItself() => AssertNone(1, RecursiveDeletes,
        "a test deletes a directory itself; call ScratchDirectory.Remove(dir)");

    [Fact]
    public void NoTestMakesItsScratchDirectoryItself() => AssertNone(2, OwnScratchDirectories,
        "a test makes a scratch directory itself; call ScratchDirectory.Create(prefix)");

    [Fact]
    public void NoTestLoadsAnAssemblyFromItsPath() => AssertNone(3, PathLoads,
        "a test loads an assembly from its path, which holds the file until the host ends; call ScratchDirectory.LoadAssembly(path)");

    [Fact]
    public void NoTestSwallowsAFailedDelete() => AssertNone(4, SwallowedDeletes,
        "a test swallows a failed delete in an empty catch; let ScratchDirectory.Remove or FileHarness.TryDelete report it");

    /// <summary>Every exemption still names a line that exists, so a stale one cannot hide a new offender.</summary>
    [Fact]
    public void EveryExemption_NamesALiveSite()
    {
        var stale = Exempt.Where(e => !File.Exists(Path.Combine(TestRepo.Root, e.File))
                                      || !File.ReadAllText(Path.Combine(TestRepo.Root, e.File)).Contains(e.Holds, StringComparison.Ordinal))
            .Select(e => $"{e.File}: {e.Holds}").ToList();
        Assert.True(stale.Count == 0, "an exemption names no live site; remove it:\n  " + string.Join("\n  ", stale));
    }

    /// <summary>The detectors look: each rule's shapes trip it, and the neighbouring jobs trip none.</summary>
    [Fact]
    public void TheDetectors_FlagTheShapesTheyExistFor()
    {
        const string offenders = """
            class C
            {
                void A(string d) { try { Directory.Delete(d, recursive: true); } catch { } }
                void B(DirectoryInfo d) { d.Delete(recursive: true); }
                void C2(string d) { System.IO.Directory.Delete(d, true); }
                string D() => Directory.CreateTempSubdirectory("x-").FullName;
                string E() => Path.Combine(Path.GetTempPath(), "x-" + Guid.NewGuid().ToString("N")[..8]);
                string F() => Path.Combine(Path.GetTempPath(), $"x-{Guid.NewGuid():N}");
                object G(string p) => Assembly.LoadFrom(p);
                object H(string p) => AssemblyLoadContext.Default.LoadFromAssemblyPath(p);
                object I(string p) => Assembly.LoadFile(p);
                void J(string f) { try { File.Delete(f); } catch (IOException) { } }
                void K(string f) { try { Close(); File.Delete(f); } catch { /* best-effort */ } }
            }
            """;
        Assert.Equal(3, RecursiveDeletes("fixture", offenders).Count());
        Assert.Equal(3, OwnScratchDirectories("fixture", offenders).Count());
        Assert.Equal(3, PathLoads("fixture", offenders).Count());
        Assert.Equal(3, SwallowedDeletes("fixture", offenders).Count());

        const string otherJobs = """
            class C
            {
                void A(string d) { ScratchDirectory.Remove(d); Directory.Delete(d); }
                string B() => ScratchDirectory.Create("x-");
                string C2() => Path.Combine(Path.GetTempPath(), $"x-{Guid.NewGuid():N}.cob");
                string D() => Path.Combine(Path.GetTempPath(), "x-" + Guid.NewGuid().ToString("N") + ".log");
                string E() => Path.Combine(Path.GetTempPath(), $"x-{Guid.NewGuid():N}", "f");
                object F(string p) => ScratchDirectory.LoadAssembly(p);
                string G() => "Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory, \"p.dll\"))";
                void H(string f) { try { File.Delete(f); } catch (IOException e) { Report(f, e); } }
                void I(string f) { try { Parse(f); } catch (FormatException) { } }
            }
            """;
        Assert.Empty(RecursiveDeletes("fixture", otherJobs));
        Assert.Empty(OwnScratchDirectories("fixture", otherJobs));
        Assert.Empty(PathLoads("fixture", otherJobs));
        Assert.Empty(SwallowedDeletes("fixture", otherJobs));
    }

    private static void AssertNone(int rule, Func<string, string, IEnumerable<string>> detector, string what)
    {
        var found = SourceFiles()
            .SelectMany(f => detector(f.Path, f.Text).Where(hit => !Exempt.Any(e =>
                e.Rule == rule && e.File == f.Path && hit.Contains(e.Holds, StringComparison.Ordinal))))
            .ToList();
        Assert.True(found.Count == 0, $"{what} (tests/_shared/ScratchDirectory.cs, kb/Work PB3194):\n  " + string.Join("\n  ", found));
    }

    /// <summary>Every tracked C# file under tests/ but the home, which owns the rules' shapes.</summary>
    private static readonly TreeScope CSharpTests = new(Under: ["tests/"], Except: ["tests/_shared/ScratchDirectory.cs"], Kinds: [".cs"]);

    /// <summary>The files <see cref="CSharpTests"/> holds, as (repo-relative path with '/', text).</summary>
    private static IEnumerable<(string Path, string Text)> SourceFiles()
    {
        var files = TrackedTree.Files(CSharpTests);
        Assert.True(files.Count > 500, $"the test tree listing found only {files.Count} C# files; it is broken");
        return files.Select(p => (p, File.ReadAllText(Path.Combine(TestRepo.Root, p))));
    }

    private static IEnumerable<InvocationExpressionSyntax> Calls(string text) =>
        CSharpSyntaxTree.ParseText(text).GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>();

    private static string Name(InvocationExpressionSyntax call) => call.Expression switch
    {
        MemberAccessExpressionSyntax m => m.Name.Identifier.Text,
        IdentifierNameSyntax i => i.Identifier.Text,
        _ => "",
    };

    private static string Site(string path, SyntaxNode node) =>
        $"{path}:{node.GetLocation().GetLineSpan().StartLinePosition.Line + 1} {node.ToString().Split('\n')[0].Trim()}";

    /// <summary>Rule 1: a <c>Delete</c> call with a <c>true</c> argument (positional or <c>recursive:</c>).</summary>
    private static IEnumerable<string> RecursiveDeletes(string path, string text) =>
        Calls(text).Where(c => Name(c) == "Delete" && c.ArgumentList.Arguments.Any(a => a.Expression.IsKind(SyntaxKind.TrueLiteralExpression)))
            .Select(c => Site(path, c));

    /// <summary>Rule 2: <c>Directory.CreateTempSubdirectory</c>, or a two-part <c>Path.Combine(Path.GetTempPath(), …)</c>
    /// whose second part holds <c>Guid.NewGuid()</c> and names no file (no extension at its end).</summary>
    private static IEnumerable<string> OwnScratchDirectories(string path, string text) =>
        Calls(text).Where(c => Name(c) == "CreateTempSubdirectory"
                               || (Name(c) == "Combine" && c.ArgumentList.Arguments.Count == 2
                                   && c.ArgumentList.Arguments[0].ToString() == "Path.GetTempPath()"
                                   && c.ArgumentList.Arguments[1].ToString().Contains("Guid.NewGuid()", StringComparison.Ordinal)
                                   && !NamesAFile(c.ArgumentList.Arguments[1].ToString())))
            .Select(c => Site(path, c));

    private static bool NamesAFile(string part) =>
        System.Text.RegularExpressions.Regex.IsMatch(part, @"\.[A-Za-z0-9]{1,5}""\s*$");

    /// <summary>Rule 3: an assembly load from a path.</summary>
    private static IEnumerable<string> PathLoads(string path, string text) =>
        Calls(text).Where(c => Name(c) is "LoadFrom" or "LoadFile" or "LoadFromAssemblyPath").Select(c => Site(path, c));

    /// <summary>Rule 4: a <c>try</c> whose block calls a <c>Delete</c> and every one of whose catches is empty.</summary>
    private static IEnumerable<string> SwallowedDeletes(string path, string text) =>
        CSharpSyntaxTree.ParseText(text).GetRoot().DescendantNodes().OfType<TryStatementSyntax>()
            .Where(t => t.Catches.Count > 0 && t.Catches.All(c => c.Block.Statements.Count == 0)
                        && t.Block.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(c => Name(c) == "Delete"))
            .Select(t => Site(path, t));
}
