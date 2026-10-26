// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using WiseOwl.Cobol.Tests.Shared;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// The differential golden comparison. Every <c>*DifferentialTests</c> funnel routes through <see cref="Assert"/>,
/// which compiles and runs the case with WiseOwl COBOL and compares its stdout to a <b>committed golden</b>
/// (<c>tests/differential/&lt;TestClass&gt;/&lt;hash&gt;.out</c>). There is one mode: the comparison. A golden is never
/// generated from a compiler's own output; a new case's expected stdout is derived from the ISO standard and
/// committed with the test (the goldens on disk were recorded once, from an oracle since retired, kb/Work R69).
/// A golden is named by a content hash of <c>(edition, source)</c> so many <c>[InlineData]</c> sources sharing one
/// funnel each get a distinct <c>.out</c>, and the same source at two editions never collides. The folder is the
/// calling test file's base name (via <see cref="CallerFilePathAttribute"/>), stable across machines.
/// </summary>
public static class DifferentialGolden
{
    /// <summary>
    /// Assert WiseOwl COBOL's output for <paramref name="source"/> equals its committed golden.
    /// </summary>
    /// <param name="source">The complete COBOL program source.</param>
    /// <param name="edition">The ISO edition to compile at (default 85, the edition most goldens were recorded at).</param>
    /// <param name="file">Caller file path (compiler-supplied) — selects the golden sub-folder by base name.</param>
    /// <param name="goldenName">Optional stable golden name; defaults to the content hash of (edition, source).</param>
    public static void Assert(string source, int edition = 85,
        [CallerFilePath] string file = "", string? goldenName = null)
    {
        string path = GoldenPath(file, goldenName ?? Hash(edition, source));

        var (cok, cout, cdetail) = new WiseOwlCobolCompiler(edition).CompileAndRun(source);
        Xunit.Assert.True(cok, $"WiseOwl COBOL failed to compile/run:\n{cdetail}\n--- source ---\n{source}");

        if (!File.Exists(path))
            throw new Xunit.Sdk.XunitException(
                $"no committed differential golden: {path}\nDerive the case's expected stdout from the ISO standard " +
                "and commit it at that path; a golden is never copied from a compiler's own output.");
        Xunit.Assert.Equal(File.ReadAllText(path).ReplaceLineEndings("\n"), cout.ReplaceLineEndings("\n"));
    }

    /// <summary><c>tests/differential/&lt;TestClassFromFileName&gt;/&lt;name&gt;.out</c>.</summary>
    private static string GoldenPath(string callerFile, string name)
    {
        string cls = callerFile.Length == 0 ? "Unknown" : Path.GetFileNameWithoutExtension(callerFile);
        return TestRepo.Tests("differential", cls, name + ".out");
    }

    /// <summary>A stable, OS-independent 16-hex-char SHA-1 of the edition, a NUL separator and the newline-normalized
    /// source. Line-ending normalization keeps the name identical whether the test file is checked out LF or CRLF.</summary>
    private static string Hash(int edition, string source)
    {
        byte[] bytes = Encoding.UTF8.GetBytes($"{edition}\0{source.ReplaceLineEndings("\n")}");
        return Convert.ToHexStringLower(SHA1.HashData(bytes))[..16];
    }
}
