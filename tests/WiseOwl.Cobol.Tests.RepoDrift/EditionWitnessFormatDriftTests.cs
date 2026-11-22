// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.RepoDrift;

/// <summary>
/// ⛔ A C# TEST WITNESSES A 1985 ROW ONLY WHEN THE PROGRAM TEXT IT COMPILES IS IN FIXED FORM (kb/Work PB6037, the C# arm of
/// kb/Work PB5754's rule). §1985:IV.7.1: "each compiler accepts source programs written in reference format", and ANSI
/// X3.23-1985 defines one reference format, the fixed one; free form is a 2002 introduction. So a <c>unit:</c> or
/// <c>conformance-test:</c> witness of a row of <c>tests/version-matrix/inventory-1985/</c> either compiles no program text
/// (it exercises a runtime, binder or lexer member directly, and the reference format is not in question) or compiles its
/// text at <c>--std 85</c> as <c>InitialReferenceFormat.Fixed</c>, or as the format a conformance-suite case states in its
/// header (<c>TestSourceFormat.OfSuiteCase</c>). Text compiled as <c>TestSourceFormat.Inline</c> (free form) at 85 is a
/// regression test of WiseOwl's <c>--source-format free</c> extension there, never a 1985 witness, and a test that compiles
/// only at a later edition witnesses no 1985 row at all.
/// <para>The check reads the witness method's source and the members of its own file that it calls (one level), as written:
/// a compile through a harness entry that defaults to <c>TestSourceFormat.Inline</c> (<see cref="InlineByDefault"/>) must
/// name <c>InitialReferenceFormat.Fixed</c> or <c>TestSourceFormat.OfSuiteCase</c>, and no such text may name
/// <c>TestSourceFormat.Inline</c> or <c>InitialReferenceFormat.Free</c>.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class EditionWitnessFormatDriftTests
{

    /// <summary>The harness entries whose reference format is <c>TestSourceFormat.Inline</c> unless the caller passes one.</summary>
    private static readonly Regex InlineByDefault = new(
        @"\b(CompileAndRun\w*|CompileRunAndReadFile|NonCobolElement\.CompileCobol|EditionHarness\.\w+)\s*\(", RegexOptions.CultureInvariant);

    private static readonly Regex FreeNamed = new(@"\bTestSourceFormat\s*\.\s*Inline\b|\bInitialReferenceFormat\s*\.\s*Free\b",
        RegexOptions.CultureInvariant);

    private static readonly Regex FixedNamed = new(@"\bInitialReferenceFormat\s*\.\s*Fixed\b|\bTestSourceFormat\s*\.\s*OfSuiteCase\b",
        RegexOptions.CultureInvariant);

    /// <summary>The 1985 inventory's rows, the scans' declared scopes (TrackedTreeScopeDriftTests reads them as literals).</summary>
    private static readonly TreeScope Inventory1985Rows = new(Under: ["tests/version-matrix/inventory-1985/"], Kinds: [".json"]);

    private static readonly TreeScope UnitSources = new(Under: ["tests/WiseOwl.Cobol.Tests.Unit/"], Kinds: [".cs"]);

    private static readonly TreeScope ConformanceTestSources = new(Under: ["tests/WiseOwl.Cobol.Tests.Conformance/"], Kinds: [".cs"]);

    private static readonly (string Prefix, string Project)[] Kinds =
    [
        ("unit:", "tests/WiseOwl.Cobol.Tests.Unit/"),
        ("conformance-test:", "tests/WiseOwl.Cobol.Tests.Conformance/"),
    ];

    [Fact]
    public void EveryCSharpWitnessOfA1985Row_CompilesFixedFormOrNoText()
    {
        var rows = TrackedTree.Files(Inventory1985Rows);
        Assert.True(rows.Count > 1000, $"the 1985 inventory listing found only {rows.Count} rows; it is broken");
        static List<string> Texts(IEnumerable<string> files) => [.. files.Select(f => File.ReadAllText(Path.Combine(TestRepo.Root, f)))];
        var sources = new Dictionary<string, List<string>>
        {
            ["unit:"] = Texts(TrackedTree.Files(UnitSources)),
            ["conformance-test:"] = Texts(TrackedTree.Files(ConformanceTestSources)),
        };
        Assert.Equal(Kinds.Select(k => k.Prefix).Order(), sources.Keys.Order());

        var offenders = new List<string>();
        int witnesses = 0;
        foreach (string row in rows)
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(TestRepo.Root, row)));
            if (!doc.RootElement.TryGetProperty("test-ref", out var refs) || refs.ValueKind != JsonValueKind.String) continue;
            foreach (string r in refs.GetString()!.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                var kind = Kinds.FirstOrDefault(k => r.StartsWith(k.Prefix, StringComparison.Ordinal));
                if (kind.Prefix is null) continue;
                witnesses++;
                string text = WitnessText(sources[kind.Prefix], r[kind.Prefix.Length..])
                              ?? throw new Xunit.Sdk.XunitException($"{row}: {r} names no test method of {kind.Project}");
                if (FreeNamed.IsMatch(text))
                    offenders.Add($"{Path.GetFileName(row)}: {r} compiles its text as free form ({FreeNamed.Match(text).Value})");
                else if (InlineByDefault.Match(text) is { Success: true } entry && !FixedNamed.IsMatch(text))
                    offenders.Add($"{Path.GetFileName(row)}: {r} compiles through {entry.Groups[1].Value}, whose default is "
                        + "TestSourceFormat.Inline (free form), and names no fixed format");
            }
        }
        Assert.True(witnesses > 20, $"only {witnesses} C# witnesses found in the 1985 inventory; the reader is broken");
        Assert.True(offenders.Count == 0, "a C# witness of a 1985 row compiles free-form text, which ANSI X3.23-1985 does not "
            + "define (kb/Work PB6037, PB5754): compile it as InitialReferenceFormat.Fixed (its text laid out in columns 8 to 72), "
            + "or as the suite case's stated format (TestSourceFormat.OfSuiteCase), or take the reference off the row:\n  "
            + string.Join("\n  ", offenders));
    }

    /// <summary>The source of test <paramref name="name"/> (<c>Class.Method</c>): its own declaration through its body, then
    /// the declarations of the members of its file it calls. Null when no file declares the class and the method.</summary>
    internal static string? WitnessText(IEnumerable<string> files, string name)
    {
        int dot = name.LastIndexOf('.');
        string cls = name[..dot], method = name[(dot + 1)..];
        var classDecl = new Regex(@"\b(class|record)\s+" + Regex.Escape(cls) + @"\b", RegexOptions.CultureInvariant);
        foreach (string file in files)
        {
            if (!classDecl.IsMatch(file)) continue;
            string? body = Member(file, method);
            if (body is null) continue;
            var called = Regex.Matches(body, @"\b([A-Z]\w*)\s*\(").Select(m => m.Groups[1].Value).Distinct()
                .Where(c => c != method).Select(c => Member(file, c)).OfType<string>();
            return string.Join("\n", called.Prepend(body));
        }
        return null;
    }

    /// <summary>The declaration of member <paramref name="name"/> in <paramref name="file"/>: from the line that declares it
    /// through its body's closing brace, or its expression body's semicolon.</summary>
    private static string? Member(string file, string name)
    {
        var decl = new Regex(@"^[ \t]*(?:(?:public|private|internal|protected|static|async|override|sealed|readonly)\s+)+"
            + @"[\w<>\[\],.?() ]+?\s" + Regex.Escape(name) + @"\s*\(", RegexOptions.Multiline | RegexOptions.CultureInvariant);
        var m = decl.Match(file);
        if (!m.Success) return null;
        int start = m.Index, depth = 0;
        for (int i = m.Index + m.Length - 1; i < file.Length; i++)
        {
            char c = file[i];
            if (c is '(' or '{') depth++;
            else if (c is ')' or '}') { depth--; if (depth == 0 && c == '}') return file[start..(i + 1)]; }
            else if (c == ';' && depth == 0) return file[start..(i + 1)];
        }
        return file[start..];
    }
}
