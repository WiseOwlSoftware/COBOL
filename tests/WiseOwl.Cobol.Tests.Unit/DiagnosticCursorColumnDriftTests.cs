// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A DIAGNOSTIC CURSOR TAKES THE ANTLR TOKEN'S COLUMN AS IT IS (kb/Work PB1154, branch review round 1). The
/// cursor (<c>DiagnosticCursor</c>, kb/Work PB82) holds the token's 1-based line and 0-based column and the sink
/// converts to the printed 1-based column once, where it renders. A reporter that adds 1 itself —
/// <c>sink.At(tok.Line, tok.Column + 1)</c>, <c>new DiagnosticCursor(tag.Start.Line, tag.Start.Column + 1)</c> — is
/// therefore one column to the RIGHT of its construct, and it is invisible to every test that matches a diagnostic
/// by code only. The branch fixed COBOL2927 and COBOL2158 this way and the reviewer found COBOL1719's three
/// sites (<c>ExpressionFormationPass</c>) still doing it. The form that cannot be wrong is
/// <c>sink.At(token)</c> / <c>edition.At(ctx)</c> (<c>DiagnosticCursorAt</c>), which this guard keeps as the only one:
/// no compiler source adds a literal 1 to a <c>Column</c>. <c>SourceLocation.ToString</c> (the 0-based
/// <c>SourceLocation</c> record's own rendering) is the one legitimate conversion, and it is not a cursor.
/// </summary>
[Trait("Sweep", "tree")]
public sealed class DiagnosticCursorColumnDriftTests
{
    private static readonly Regex ColumnPlusOne = new(@"\bColumn\s*\+\s*1\b", RegexOptions.Compiled);

    /// <summary>File-name → why a <c>Column + 1</c> in it is not a diagnostic cursor.</summary>
    private static readonly Dictionary<string, string> NotACursor = new(StringComparer.Ordinal)
    {
        ["SourceLocation.cs"] = "renders the 0-based SourceLocation as a 1-based file(line,column) string",
    };

    [Fact]
    public void NoCompilerSourceAddsOneToAColumn()
    {
        var offenders = new List<string>();
        foreach (string file in Directory.EnumerateFiles(TestRepo.Src(), "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                || file.Contains($"{Path.DirectorySeparatorChar}Generated{Path.DirectorySeparatorChar}")
                || NotACursor.ContainsKey(Path.GetFileName(file))) continue;
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
                if (ColumnPlusOne.IsMatch(lines[i]))
                    offenders.Add($"{Path.GetFileName(file)}:{i + 1}: {lines[i].Trim()}");
        }

        Assert.True(offenders.Count == 0,
            "A source adds 1 to a token column. The diagnostic cursor already holds the token's column and the sink "
            + "renders it 1-based, so the report lands one column right of its construct. Position with "
            + "sink.At(token) / edition.At(ctx) (DiagnosticCursorAt). Offending sites:\n  " + string.Join("\n  ", offenders));
    }

    [Fact]
    public void TheAdjudicatedExemptionsStillExist()
    {
        var present = Directory.EnumerateFiles(TestRepo.Src(), "*.cs", SearchOption.AllDirectories)
            .Select(Path.GetFileName).ToHashSet(StringComparer.Ordinal);
        var stale = NotACursor.Keys.Where(k => !present.Contains(k)).ToList();
        Assert.True(stale.Count == 0, "Exempt files that no longer exist (drop them): " + string.Join(", ", stale));
    }
}
