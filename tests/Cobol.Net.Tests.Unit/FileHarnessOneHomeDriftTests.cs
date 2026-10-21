// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A TEST'S HOST FILE PATH AND ITS BEST-EFFORT DELETE LIVE IN <c>FileHarness</c> AND NOWHERE ELSE (kb/Work PB3478).
///
/// <para><b>What happened.</b> PB3459 and PB3471 made <c>tests/_shared/FileHarness.cs</c> the one place that registers,
/// seeds and reads a test's file, and left the two smallest helpers of that job behind: <c>Tmp</c> (a fresh path in the
/// temp directory) and <c>TryDelete</c> (delete it, ignoring the host's refusal). Seventeen Unit classes kept their own
/// copies, differing only in the tag prefix and in which of the two refusals (<see cref="IOException"/>,
/// <see cref="UnauthorizedAccessException"/>) the delete swallowed, and a dozen more test sites built the same
/// <c>Path.Combine(Path.GetTempPath(), $"...{Guid.NewGuid():N}.dat")</c> inline. A change to either took one edit per
/// copy and the copies had already drifted (two different catch sets, and a guard on the file name in one).</para>
///
/// <para><b>The rules, over every C# file of the three test assemblies (<c>tests/_shared</c> is not scanned: it is the
/// home).</b>
/// <list type="number">
///   <item>No line builds a unique <c>.dat</c> or <c>.txt</c> file name from <c>Guid.NewGuid()</c>: call
///     <c>FileHarness.Tmp(tag)</c>.</item>
///   <item>No method is only a best-effort <c>File.Delete</c> (a method whose every invocation is <c>File.Delete</c>
///     and whose body is a try/catch): call <c>FileHarness.TryDelete(path)</c>. A method that deletes a DIRECTORY, or
///     that does other work around the delete, is a different job and is not matched.</item>
/// </list>
/// Each detector is driven first by a fixture that MUST trip it, so a detector that silently stopped looking turns this
/// test red rather than green.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class FileHarnessOneHomeDriftTests
{
    private static readonly string[] TestAssemblies =
        ["Cobol.Net.Tests.Unit", "Cobol.Net.Tests.Conformance", "Cobol.Net.Tests.Characterization"];

    /// <summary>A unique file name: <c>Guid.NewGuid()</c> and a <c>.dat</c> or <c>.txt</c> literal end on one line.</summary>
    private static readonly Regex UniqueHostName = new(@"Guid\.NewGuid\(\).*\.(dat|txt)""", RegexOptions.Compiled);

    [Fact]
    public void NoTestBuildsItsOwnUniqueHostPath()
    {
        var inline = SourceFiles().SelectMany(f => InlineHostPaths(f.Path, f.Text)).ToList();
        Assert.True(inline.Count == 0,
            "a test builds its own unique host path; call FileHarness.Tmp(tag) (tests/_shared/FileHarness.cs, "
            + "kb/Work PB3478):\n  " + string.Join("\n  ", inline));
    }

    [Fact]
    public void NoTestKeepsItsOwnBestEffortFileDelete()
    {
        var copies = SourceFiles().SelectMany(f => PrivateDeletes(f.Path, f.Text)).ToList();
        Assert.True(copies.Count == 0,
            "a test keeps its own best-effort File.Delete helper; call FileHarness.TryDelete(path) "
            + "(tests/_shared/FileHarness.cs, kb/Work PB3478):\n  " + string.Join("\n  ", copies));
    }

    /// <summary>The detectors look: the seventeen classes' shapes, verbatim in kind, trip both rules, and the neighbouring
    /// jobs (a directory delete, a delete with work around it, a unique DIRECTORY name) trip neither.</summary>
    [Fact]
    public void TheDetectors_FlagTheShapesTheyExistFor()
    {
        const string privateCopies = """
            class C
            {
                private static string Tmp(string tag) =>
                    Path.Combine(Path.GetTempPath(), $"pb1-{tag}-{Guid.NewGuid():N}.dat");

                private static void TryDelete(string host)
                {
                    try { File.Delete(host); } catch (IOException) { }
                }
            }
            """;
        Assert.Single(InlineHostPaths("fixture", privateCopies));
        Assert.Single(PrivateDeletes("fixture", privateCopies));

        const string otherJobs = """
            class C
            {
                private static string Dir() => Path.Combine(Path.GetTempPath(), "d-" + Guid.NewGuid().ToString("N"));

                private static void TryDeleteDir(string dir)
                {
                    try { Directory.Delete(dir, recursive: true); } catch (IOException) { }
                }

                private static void DeleteAndLog(string host)
                {
                    try { File.Delete(host); } catch (IOException) { }
                    Console.WriteLine(host);
                }
            }
            """;
        Assert.Empty(InlineHostPaths("fixture", otherJobs));
        Assert.Empty(PrivateDeletes("fixture", otherJobs));
    }

    private static IEnumerable<(string Path, string Text)> SourceFiles() =>
        TestAssemblies.SelectMany(a => Directory.EnumerateFiles(TestRepo.Tests(a), "*.cs", SearchOption.AllDirectories))
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                && Path.GetFileName(p) != "FileHarnessOneHomeDriftTests.cs")
            .Select(p => (Path.GetRelativePath(TestRepo.Root, p), File.ReadAllText(p)));

    private static IEnumerable<string> InlineHostPaths(string path, string text)
    {
        string[] lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
            if (UniqueHostName.IsMatch(lines[i]))
                yield return $"{path}:{i + 1} {lines[i].Trim()}";
    }

    private static IEnumerable<string> PrivateDeletes(string path, string text)
    {
        var root = CSharpSyntaxTree.ParseText(text).GetRoot();
        foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            if (method.Body is not { Statements: [TryStatementSyntax] } body) continue;
            var calls = body.DescendantNodes().OfType<InvocationExpressionSyntax>().ToList();
            if (calls.Count > 0 && calls.All(c => c.Expression.ToString() == "File.Delete"))
                yield return $"{path}:{method.GetLocation().GetLineSpan().StartLinePosition.Line + 1} {method.Identifier.Text}";
        }
    }
}
