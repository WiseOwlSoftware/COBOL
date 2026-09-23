// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE DRIFT GUARD FOR "PLUS AND + ARE SYNONYMS" (kb/Work PB951).
///
/// <para><b>Why it exists.</b> ISO/IEC 1989:2023 prints the same sentence at three report-writer clauses —
/// §13.18.35.3 SR1 (LINE), §13.18.14.3 SR2 (COLUMN) and §13.18.37.3 SR2 (NEXT GROUP): "PLUS and + are
/// synonyms." — and the grammar wrote the word <c>PLUSWORD</c> alone into all three productions, so
/// <c>LINE + 1</c> was a bare COBOL0001. One synonym written down three times was the defect; the fix is ONE
/// fragment, <c>reportRelativeSign : PLUSWORD | PLUS</c>, that every relative operand references.</para>
///
/// <para><b>What it pins, mechanically.</b> (1) In every parser grammar file, a parser rule whose body names the
/// word token <c>PLUSWORD</c> also names the symbol token <c>PLUS</c> — the next clause that takes a relative
/// operand cannot be written with one spelling (the screen section's LINE/COLUMN clauses, §13.17 format 2, carry
/// both alongside MINUS and satisfy it as written). (2) In the report writer grammar, <c>PLUSWORD</c> appears in
/// exactly one rule, the fragment — a hand-repeated <c>(PLUSWORD | PLUS)</c> would reproduce the defect's own
/// cause.</para>
/// </summary>
public sealed class GrammarRelativeSignDriftTests
{
    private static string[] GrammarFiles() =>
        Directory.GetFiles(TestRepo.Src("Cobol.Net.Frontend", "Grammar"), "*.g4", SearchOption.AllDirectories)
                 .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                          && !p.Contains($"{Path.DirectorySeparatorChar}Generated{Path.DirectorySeparatorChar}")
                          && !Path.GetFileName(p).Contains("Lexer", StringComparison.Ordinal))
                 .ToArray();

    /// <summary>The file's text with line and block comments stripped, so a token NAMED in prose does not count.</summary>
    private static string CodeOf(string path) =>
        Regex.Replace(Regex.Replace(File.ReadAllText(path), @"/\*.*?\*/", "", RegexOptions.Singleline), @"//[^\n]*", "");

    /// <summary>Every parser rule of a grammar file as (name, body) — a rule is <c>name : body ;</c> starting at a
    /// line start.</summary>
    private static IEnumerable<(string Name, string Body)> Rules(string code) =>
        Regex.Matches(code, @"(?m)^([a-z][A-Za-z0-9_]*)\s*(?:\n\s*)?:(.*?)^\s*;", RegexOptions.Singleline)
             .Select(m => (m.Groups[1].Value, m.Groups[2].Value));

    [Fact]
    public void TheProbe_FindsTheFragment()
    {
        // A probe that finds nothing would make the obligations below vacuously green.
        var rw = GrammarFiles().Single(f => Path.GetFileName(f) == "CobolReportWriter.g4");
        var rules = Rules(CodeOf(rw)).ToList();
        Assert.True(rules.Count > 20, $"only {rules.Count} rule(s) parsed out of CobolReportWriter.g4 — the rule regex is broken");
        var frag = Assert.Single(rules, r => r.Name == "reportRelativeSign");
        Assert.Matches(@"\bPLUSWORD\b", frag.Body);
        Assert.Matches(@"\bPLUS\b", frag.Body);
    }

    [Fact]
    public void EveryRuleNamingTheWordPlus_AlsoNamesTheSymbol()
    {
        var offenders = new List<string>();
        int seen = 0;
        foreach (var path in GrammarFiles())
            foreach (var (name, body) in Rules(CodeOf(path)))
            {
                if (!Regex.IsMatch(body, @"\bPLUSWORD\b")) continue;
                seen++;
                if (!Regex.IsMatch(body, @"\bPLUS\b"))
                    offenders.Add($"{Path.GetFileName(path)}: {name}");
            }
        Assert.True(seen >= 3, $"only {seen} rule(s) name PLUSWORD — the probe is not seeing the grammar");
        Assert.True(offenders.Count == 0,
            "these rules admit the word PLUS but not '+', which ISO §13.18.35.3 SR1 / §13.18.14.3 SR2 / "
            + "§13.18.37.3 SR2 make its synonym — reference reportRelativeSign instead: " + string.Join("; ", offenders));
    }

    [Fact]
    public void TheReportWriterGrammar_SpellsTheRelativeSignInOneRule()
    {
        var rw = GrammarFiles().Single(f => Path.GetFileName(f) == "CobolReportWriter.g4");
        var naming = Rules(CodeOf(rw)).Where(r => Regex.IsMatch(r.Body, @"\bPLUSWORD\b")).Select(r => r.Name).ToList();
        Assert.Equal(["reportRelativeSign"], naming);
    }
}
