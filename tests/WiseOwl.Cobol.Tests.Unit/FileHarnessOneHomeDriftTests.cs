// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

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
///   <item>No <c>try</c> statement is only a best-effort <c>File.Delete</c> (its block holds nothing but
///     <c>File.Delete</c> calls and every catch is empty), whether it is a private helper's body or sits inline in a
///     <c>finally</c>, and whichever exceptions the catches name: call <c>FileHarness.TryDelete(path)</c>. A delete
///     of a DIRECTORY, or a catch that does work (reports, rethrows, counts), is a different job and is not matched.</item>
/// </list>
/// Each detector is driven first by a fixture that MUST trip it, so a detector that silently stopped looking turns this
/// test red rather than green.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class FileHarnessOneHomeDriftTests
{
    /// <summary>The three gated test assemblies' C# sources, COMMITTED (kb/Work PB3814): never a walk of the directories
    /// from the file system, which enters every running test host's <c>bin/</c> and <c>obj/</c>.</summary>
    private static readonly TreeScope TestAssemblySources = new(
        Under: ["tests/WiseOwl.Cobol.Tests.Unit/", "tests/WiseOwl.Cobol.Tests.Conformance/", "tests/WiseOwl.Cobol.Tests.Characterization/"],
        Kinds: [".cs"]);

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
        var copies = SourceFiles().SelectMany(f => BestEffortDeletes(f.Path, f.Text)).ToList();
        Assert.True(copies.Count == 0,
            "a test keeps its own best-effort File.Delete (helper or inline); call FileHarness.TryDelete(path) "
            + "(tests/_shared/FileHarness.cs, kb/Work PB3478):\n  " + string.Join("\n  ", copies));
    }

    /// <summary>The detectors look: the seventeen classes' shapes, verbatim in kind, and the inline deletes trip both rules, and
    /// the neighbouring jobs (a directory delete, a delete whose try or catch does other work, a unique DIRECTORY name)
    /// trip neither.</summary>
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
        Assert.Single(BestEffortDeletes("fixture", privateCopies));

        const string inlineCopies = """
            class C
            {
                void A(string host)
                {
                    try { }
                    finally { try { File.Delete(host); } catch (IOException) { } }
                }

                void B(string host)
                {
                    try
                    {
                        System.IO.File.Delete(host);
                    }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { /* best-effort */ }
                }

                void C(string host)
                {
                    try { }
                    finally { try { File.Delete(host); } catch { } }
                }
            }
            """;
        Assert.Equal(3, BestEffortDeletes("fixture", inlineCopies).Count());

        const string otherJobs = """
            class C
            {
                private static string Dir() => Path.Combine(Path.GetTempPath(), "d-" + Guid.NewGuid().ToString("N"));

                private static void TryDeleteDir(string dir)
                {
                    try { Directory.Delete(dir, recursive: true); } catch (IOException) { }
                }

                private static void DeleteAndReport(string host)
                {
                    try { File.Delete(host); } catch (IOException e) { Console.WriteLine(e); }
                }

                private static void DeleteThenLog(string host)
                {
                    try { File.Delete(host); Console.WriteLine(host); } catch (IOException) { }
                }
            }
            """;
        Assert.Empty(InlineHostPaths("fixture", otherJobs));
        Assert.Empty(BestEffortDeletes("fixture", otherJobs));
    }

    private static IEnumerable<(string Path, string Text)> SourceFiles() =>
        TrackedTree.Files(TestAssemblySources)
            .Where(rel => Path.GetFileName(rel) != "FileHarnessOneHomeDriftTests.cs")
            .Select(rel => (rel, File.ReadAllText(TestRepo.At(rel.Split('/')))));

    private static IEnumerable<string> InlineHostPaths(string path, string text)
    {
        string[] lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
            if (UniqueHostName.IsMatch(lines[i]))
                yield return $"{path}:{i + 1} {lines[i].Trim()}";
    }

    /// <summary>Every <c>try { File.Delete(x); } catch ... { }</c>, wherever it sits (a helper method's body, a
    /// <c>finally</c>, a loop) and however it is laid out or which exceptions its empty catches name.</summary>
    private static IEnumerable<string> BestEffortDeletes(string path, string text)
    {
        var root = CSharpSyntaxTree.ParseText(text).GetRoot();
        foreach (var attempt in root.DescendantNodes().OfType<TryStatementSyntax>())
        {
            if (attempt.Finally is not null || attempt.Catches.Count == 0) continue;
            if (attempt.Catches.Any(c => c.Block.Statements.Count > 0)) continue;
            var body = attempt.Block.Statements;
            if (body.Count == 0 || !body.All(IsFileDelete)) continue;
            yield return $"{path}:{attempt.GetLocation().GetLineSpan().StartLinePosition.Line + 1} {attempt.Block.ToString().Trim()}";
        }
    }

    /// <summary>A statement that is only <c>File.Delete(...)</c> (or <c>System.IO.File.Delete(...)</c>).</summary>
    private static bool IsFileDelete(StatementSyntax statement) =>
        statement is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax call }
        && call.Expression.ToString() is "File.Delete" or "System.IO.File.Delete";
}
