// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System.Text;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ EVERY DIAGNOSTIC CODE IS SPELLED <c>COBOL####</c>: no tracked file outside the history writes a code, a code
/// placeholder or a code-matching regular expression with the former prefix <c>COBOLNET</c> (owner decision
/// 2026-10-23, kb/Work PB3575, "COBOL####"; restated 2026-10-26: "All the error codes are incorrect because days ago I
/// ordered you to change from COBOLNET#### to COBOL####"). The compiler's codes from 0400 up were renamed with every
/// number kept, so the parser's series (COBOL0001-COBOL0313) and the rest share one prefix and one number space.
/// <para>
/// ⭐ The rename is <c>scripts/rename_diagnostic_prefix.py</c>, re-runnable and idempotent: a branch cut before it
/// landed runs it over its own tree after merging main. This sweep is what makes that automatic rather than
/// remembered: a golden, a note, a doc or a test that still writes the old spelling fails here, at the gate's tier 0c,
/// before it reaches main. What the sweep sets aside is the SCRIPT's <c>EXCEPT</c> list, the one list
/// (<see cref="TheSetAsidePathsAreTheRenameScriptsOwn"/> holds <see cref="Scope"/> to it): the history and frozen
/// records that keep the spelling of their day (DEVLOG/, adjudication/, docs/rearchitecture/evidence/; CLAUDE.md rule 6,
/// kb/Work PB785) and the three files that own the rule (the decision note quoting the owner, the script, this test).
/// Inside a file, a witness the traceability inventory records as RETIRED keeps the spelling it was retired under (the
/// rename re-spelled 14 witnesses through a <c>record_verdicts.py</c> batch, and the witness-loss audit excuses exactly
/// the retired spelling).
/// </para>
/// <para>
/// Only the code SHAPES are the rule: the old prefix followed by a digit, a Markdown-bold digit, a placeholder
/// (<c>nnnn</c>, <c>####</c>, <c>&lt;n&gt;</c>, an ellipsis) or a regular-expression atom, and the optional group that
/// named both prefixes. The same letters as a NAME stay legal: the <c>docs/COBOLNET_*.md</c> design-doc file names, a
/// COBOL program named COBOLNET in a test, the CobolNet namespaces.
/// </para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class DiagnosticCodePrefixDriftTests
{
    /// <summary>The files swept (kb/Work PB3531: declared, so the drift-rule index names this class for each of them):
    /// every tracked text kind of the whole tree but the paths the rename script never rewrites.</summary>
    private static readonly TreeScope Scope = new(Under: [""],
        Except: ["DEVLOG/", "adjudication/", "docs/rearchitecture/evidence/", "kb/Work/PB3575.md",
            "scripts/rename_diagnostic_prefix.py", "tests/Cobol.Net.Tests.Unit/DiagnosticCodePrefixDriftTests.cs"],
        Kinds: [".cs", ".cob", ".cbl", ".cpy", ".g4", ".json", ".jsonl", ".md", ".txt", ".out", ".err", ".csproj",
            ".props", ".ps1", ".py", ".sh", ".yml", ".tsv", ".base"]);

    /// <summary>The former prefix in a code's shape: the rename script's rules, as one pattern.</summary>
    private static readonly Regex OldCode = new(
        @"COBOLNET(?:[0-9]|\*\*[0-9]|nnnn|####|<n>|…|\\+d|\[0-9\]|\()|COBOL\(\?:NET\)\?",
        RegexOptions.CultureInvariant);

    /// <summary>A witness the traceability inventory records as RETIRED (<c>retired-witness: &lt;ref&gt;</c>, the mark's text
    /// read from the inventory schema, its one definition) keeps the spelling it was retired under: the witness-loss
    /// audit excuses the loss of exactly that spelling, so the rename script leaves it and the sweep forgives it.</summary>
    private static readonly Lazy<Regex> s_retiredWitness = new(() =>
    {
        using var doc = System.Text.Json.JsonDocument.Parse(
            File.ReadAllText(TestRepo.At("tests", "version-matrix", "inventory-schema.json")));
        string mark = doc.RootElement.GetProperty("notes").GetProperty("retired-witness-mark").GetString()!;
        return new Regex(Regex.Escape(mark) + @"\s*\S+", RegexOptions.CultureInvariant);
    });

    /// <summary>What a sweep saw: the offending lines AND the population they were drawn from.</summary>
    internal sealed record SweepResult(IReadOnlyList<string> Offenders, int FilesScanned);

    /// <summary>The one sweep of the committed tree, read once per test assembly.</summary>
    private static readonly Lazy<SweepResult> s_tree = new(() => Sweep(TestRepo.Root, TrackedTree.Files(Scope)));

    /// <summary>Every <c>path:line: text</c> under <paramref name="root"/> among <paramref name="paths"/> that writes a
    /// code with the former prefix; a path absent from the checkout is skipped and not counted.</summary>
    internal static SweepResult Sweep(string root, IEnumerable<string> paths)
    {
        var offenders = new List<string>();
        int scanned = 0;
        foreach (string rel in paths)
        {
            string full = Path.Combine(root, rel);
            if (!File.Exists(full))
                continue;
            scanned++;
            string text = File.ReadAllText(full, Encoding.UTF8);
            if (!text.Contains("COBOL", StringComparison.Ordinal))
                continue;
            string[] lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
                if (OldCode.IsMatch(lines[i]) && OldCode.IsMatch(s_retiredWitness.Value.Replace(lines[i], "")))
                    offenders.Add($"{rel}:{i + 1}: {lines[i].Trim()}");
        }
        return new SweepResult(offenders, scanned);
    }

    /// <summary>No tracked file in <see cref="Scope"/> writes a diagnostic code with the former prefix.</summary>
    [Fact]
    public void NoTrackedFileSpellsADiagnosticCodeWithTheFormerPrefix()
    {
        SweepResult r = s_tree.Value;
        Assert.True(r.Offenders.Count == 0,
            $"{r.Offenders.Count} line(s) spell a diagnostic code with the former prefix COBOLNET ({r.FilesScanned} files "
            + "scanned); the codes are COBOL#### with every number kept (kb/Work PB3575). A branch cut before the rename "
            + "converts itself with `python scripts/rename_diagnostic_prefix.py`:" + Environment.NewLine
            + string.Join(Environment.NewLine, r.Offenders.Take(40).Select(o => "    " + o)));
    }

    /// <summary>The sweep read the tree it claims to have swept: a broken enumeration would otherwise pass as a clean
    /// tree.</summary>
    [Fact]
    public void TheSweepActuallyReadsTheTrackedTree()
    {
        Assert.True(s_tree.Value.FilesScanned >= 10000,
            $"the prefix sweep read only {s_tree.Value.FilesScanned} file(s) of the tracked tree under {TestRepo.Root}; "
            + "the enumeration is broken, so the sweep is scanning nothing.");
    }

    /// <summary>The sweep FIRES: each shape of the former prefix planted in a scratch tree is reported, and the names
    /// that are no diagnostic prefix are not.</summary>
    [Fact]
    public void TheSweepFiresOnEveryShapeOfTheFormerPrefix()
    {
        string old = "COBOL" + "NET";
        string[] offending =
        [
            $"error {old}1985: x", $"a {old}**1520** row", $"bare <c>{old}nnnn</c>", $"a {old}#### code",
            $"{old}<n> from the rest", $"error {old}…:", $"@\"{old}\\d{{4}}\"", $"\"{old}\\\\d{{4}}\"",
            $"{old}[0-9]{{4}}", $"\"{old}(159[7-9])\"", @"COBOL(?:NET)?\d{4}",
        ];
        string[] innocent =
        [
            $"docs/{old}_DESIGN.md", $"CALL \"{old}\" USING W-N", $"DISPLAY \"HELLO FROM {old}\".", "error COBOL1985: x",
            $"\"notes\": \"retired-witness: unit:T.X_Is{old}1934 (re-spelled)\"",
        ];
        string dir = ScratchDirectory.Create("prefix-sweep");
        try
        {
            File.WriteAllText(Path.Combine(dir, "bad.md"), string.Join("\n", offending) + "\n");
            File.WriteAllText(Path.Combine(dir, "good.md"), string.Join("\n", innocent) + "\n");
            SweepResult r = Sweep(dir, ["bad.md", "good.md", "absent.md"]);
            Assert.Equal(2, r.FilesScanned);
            Assert.Equal(Enumerable.Range(1, offending.Length).Select(n => $"bad.md:{n}:"),
                r.Offenders.Select(o => o[..(o.IndexOf(": ", StringComparison.Ordinal) + 1)]));
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>The sweep sets aside exactly what the rename script never rewrites (its <c>EXCEPT</c> tuple), so the
    /// history the script keeps is the history the sweep forgives, and nothing else.</summary>
    [Fact]
    public void TheSetAsidePathsAreTheRenameScriptsOwn()
    {
        string script = File.ReadAllText(TestRepo.Scripts("rename_diagnostic_prefix.py"));
        Match m = Regex.Match(script, @"^EXCEPT = \(([^)]*)\)", RegexOptions.Multiline);
        Assert.True(m.Success, "scripts/rename_diagnostic_prefix.py no longer declares `EXCEPT = (...)`");
        var fromScript = Regex.Matches(m.Groups[1].Value, @"""([^""]+)""").Select(x => x.Groups[1].Value).ToList();
        Assert.Equal(fromScript, Scope.Except);
    }
}
