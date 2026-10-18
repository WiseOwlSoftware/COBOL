// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE DENOMINATOR IS COMPLETE — per clause, counted, every build.
///
/// <para><c>docs/rearchitecture/spec-rule-catalog.json</c> is the enumeration the PHASE-14 traceability
/// inventory is built from, and its row count is the denominator v1.0 is defined against (owner decision D13:
/// zero GAP = P14 done). Every completeness critic inside <c>scripts/spec/extract_rule_catalog.py</c> asks
/// "did this rule block yield SOMETHING?" — and a block that yields 16 of its 42 rules passes all of them,
/// because 16 is not zero. That is precisely what happened (fix-queue PB689): the transcription rendered the
/// in-clause group label <c>SEQUENTIAL FILES</c> as <c>## SEQUENTIAL FILES</c> inside §14.9.51.4's general
/// rules, the block scan closed the block at it, and GR17–GR42 of the WRITE statement — twenty-six normative
/// rules including the line-sequential '71' status — never became inventory rows. Five clauses were short by
/// sixty rules in total, and the published GAP was therefore measured against a denominator that flattered the
/// project without anyone being able to see it.</para>
///
/// <para>So this gate counts. For every clause the catalog holds rules for, the ordinals the TRANSCRIPTION
/// PRINTS at column 0 must equal the rows the CATALOG HOLDS — no segmentation reasoning, no vocabulary, just
/// two counts that have to agree. The extractor may decide what an ordinal is CALLED (top-level <c>GR-x-7</c>
/// versus sub-list <c>GR-x-L3.2</c>); it may never lose one, and an ordinal it cannot place is filed as a nest
/// and reported. A clause below its printed count has lost rules whatever the segmentation says, and a clause
/// above it has invented them — the more dangerous direction, because inflation looks like thoroughness.</para>
///
/// <para>⚠ This is a SPEC-side gate that deliberately does not run the Python extractor: it re-derives the
/// printed count from <c>specs/ISO_COBOL.md</c> independently, so it can contradict the code that built the
/// catalog. A critic that reuses the machinery it audits cannot see that machinery's bug.</para>
///
/// <para>⚠ <see cref="TheseChecks_ActuallyFail_OnAFabricatedSpec"/> exists because a gate that has only ever
/// been observed green is indistinguishable from one that inspects nothing
/// (<c>feedback_green_gates_arent_evidence</c>). It re-runs both predicates over a spec built to break them.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class CatalogCoverageDriftTests
{
    // ── the shapes, shared with scripts/spec/extract_rule_catalog.py ────────────────────────────────────

    /// <summary>A markdown heading of any level.</summary>
    private static readonly Regex AnyHeading = new(@"^#{1,6}\s+\S", RegexOptions.Compiled);

    /// <summary>A heading whose text OPENS WITH A CLAUSE NUMBER — with or without a title. The terminology
    /// clause transcribes 300+ terms as a bare <c>### 3.1</c> with the term on the next line, and those are
    /// numbered clauses all the same.</summary>
    private static readonly Regex NumberedHeading =
        new(@"^#{1,6}\s+(?<num>\d+(?:\.\d+)*)\s*(?<title>.*?)\s*$", RegexOptions.Compiled);

    /// <summary>A heading that ends a numbered clause without carrying a clause number: the annex, bibliography
    /// and front-matter divisions. Everything else unnumbered is INTERIOR to the clause enclosing it — which is
    /// the defect shape this gate exists to make impossible.</summary>
    private static readonly Regex StructuralHeading = new(
        @"^#{1,6}\s+\*{0,2}(?:Annex\s+[A-Z]\b|Bibliography\b|Index\b|INTERNATIONAL\s+STANDARD\b|"
        + @"Information\s+technology\b|BIBLIOGRAPHY\b|Preface\b|Foreword\b|Introduction\b|Tables\b|Figures\b)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>Page-break furniture spliced into rule prose at a page boundary; skipped before anything else,
    /// exactly as the extractor does.</summary>
    private static readonly Regex Furniture = new(
        @"^\s*(?:-{3,}|<a\s+id=""[^""]*""></a>|(?:\*\*)?ISO/IEC\s+1989:2023\s*\(E\)(?:\*\*)?"
        + @"|(?:\d+\s+)?(?:©|\(c\))?\s*ISO/IEC\s+2023|Licensed to .*)\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>A top-level rule ordinal at column 0. The transcription writes both <c>1\)</c> and <c>1\.</c>,
    /// and escapes the delimiter because an unescaped <c>1)</c> is a Markdown list marker.</summary>
    private static readonly Regex Ordinal = new(@"^(?<n>\d+)\\?[.)]\s", RegexOptions.Compiled);

    // ── the two artifacts ──────────────────────────────────────────────────────────────────────────────

    private static string[] SpecLines() =>
        File.ReadAllLines(TestRepo.Specs("ISO_COBOL.md"));

    /// <summary>Catalog rows per clause, counting only the numbered-rule kinds. FMT (general-format diagrams)
    /// and DOC (Annex A.1 items) carry no ordinal in the clause body and are not part of this comparison.</summary>
    private static Dictionary<string, int> CatalogRowsByClause()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(
            TestRepo.Docs("rearchitecture", "spec-rule-catalog.json")));
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var rule in doc.RootElement.GetProperty("rules").EnumerateArray())
        {
            string kind = rule.GetProperty("kind").GetString()!;
            if (kind is "FMT" or "DOC")
            {
                continue;
            }

            // A PROSE-PARAGRAPH row (kb/Work PB284 part 3) has no ordinal line in the transcription to count; its
            // own gate is ProseClauses_AreHeldSentenceForSentence.
            if (rule.TryGetProperty("shape", out var shape) && shape.GetString() == "paragraph")
            {
                continue;
            }

            string section = rule.GetProperty("section").GetString()!;
            counts[section] = counts.TryGetValue(section, out int n) ? n + 1 : 1;
        }

        Assert.True(counts.Count >= 600,
            $"only {counts.Count} clauses carry rules in spec-rule-catalog.json — the catalog is not the "
            + "denominator this gate was written against; regenerate it before trusting anything here");
        return counts;
    }

    /// <summary>Every FMT (general-format) row of the catalog, as (id, text).</summary>
    private static List<(string Id, string Text)> CatalogFmtRows()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(
            TestRepo.Docs("rearchitecture", "spec-rule-catalog.json")));
        var rows = new List<(string, string)>();
        foreach (var rule in doc.RootElement.GetProperty("rules").EnumerateArray())
        {
            if (rule.GetProperty("kind").GetString() == "FMT")
            {
                rows.Add((rule.GetProperty("id").GetString()!, rule.GetProperty("text").GetString()!));
            }
        }

        Assert.True(rows.Count >= 300,
            $"only {rows.Count} FMT rows in spec-rule-catalog.json — the general-format scan has stopped finding "
            + "the diagrams; regenerate the catalog before trusting the general-format gate");
        return rows;
    }

    /// <summary>A general-format DIAGRAM as the transcription writes one (<c>&lt;pre&gt;</c> today; the fenced
    /// and <c>$$</c> forms are earlier renderings).</summary>
    private static readonly Regex FmtDiagram = new(@"<pre\b|```|\$\$", RegexOptions.Compiled);

    /// <summary>The numbered-and-named label of one format of a multi-format construct (§5.2.1: "the general
    /// format is separated into multiple formats that are numbered and named").</summary>
    private static readonly Regex FmtLabel = new(@"\bFormat\s+\d+", RegexOptions.Compiled);

    // ── the predicates, over injectable inputs so the fabricated-input test can reach them ─────────────

    /// <summary>Every unnumbered, non-structural heading, with the numbered clause it sits inside.</summary>
    private static List<(int Line, string Clause, string Text)> InteriorHeadings(string[] lines)
    {
        var found = new List<(int, string, string)>();
        string? clause = null;
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (Furniture.IsMatch(line))
            {
                continue;
            }

            if (!AnyHeading.IsMatch(line))
            {
                continue;
            }

            var m = NumberedHeading.Match(line);
            if (m.Success && !line.TrimStart().StartsWith('['))
            {
                clause = m.Groups["num"].Value;
            }
            else if (StructuralHeading.IsMatch(line))
            {
                clause = null;
            }
            else if (clause is not null)
            {
                found.Add((i + 1, clause, line.TrimEnd()));
            }
        }

        return found;
    }

    /// <summary>Column-0 ordinal lines per numbered clause — what the transcription PRINTS as that clause's
    /// rules. A clause body runs to the next numbered or structural heading; an unnumbered interior heading is
    /// NOT a boundary, which is the whole point (a boundary there is how sixty rules disappeared). An in-clause
    /// group label is a heading in neither the printed standard nor this count.</summary>
    private static Dictionary<string, int> PrintedOrdinalsByClause(string[] lines)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        string? clause = null;
        foreach (string line in lines)
        {
            if (Furniture.IsMatch(line))
            {
                continue;
            }

            if (AnyHeading.IsMatch(line))
            {
                var m = NumberedHeading.Match(line);
                if (m.Success && !line.TrimStart().StartsWith('['))
                {
                    clause = m.Groups["num"].Value;
                    counts.TryAdd(clause, 0);
                }
                else if (StructuralHeading.IsMatch(line))
                {
                    clause = null;
                }

                // ⛔ ANYTHING ELSE UNNUMBERED IS NOT A BOUNDARY. Closing the clause here is the extractor bug
                // this gate exists to contradict, so the count deliberately keeps reading — a truncating
                // extractor then reports FEWER rows than the transcription prints, and this test goes red.
                continue;
            }

            if (clause is not null && Ordinal.IsMatch(line))
            {
                counts[clause] = counts.TryGetValue(clause, out int n) ? n + 1 : 1;
            }
        }

        return counts;
    }

    /// <summary>The FMT rows that carry neither a diagram nor a Format label — a row with nothing a grammar rule
    /// could be verified against.</summary>
    private static List<string> UndrawnFmtRows(IEnumerable<(string Id, string Text)> rows) =>
        [.. rows.Where(r => !FmtDiagram.IsMatch(r.Text) && !FmtLabel.IsMatch(r.Text))
                .Select(r => $"{r.Id}: {(r.Text.Length > 80 ? r.Text[..80] + "…" : r.Text)}")];

    private static List<string> Disagreements(Dictionary<string, int> printed, Dictionary<string, int> held) =>
        [.. held.Where(kv => printed.GetValueOrDefault(kv.Key, 0) != kv.Value)
                .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .Select(kv => $"§{kv.Key}: the transcription prints {printed.GetValueOrDefault(kv.Key, 0)} "
                              + $"ordinal line(s), the catalog holds {kv.Value} row(s)")];

    // ── the gates ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The count gate. A clause below its printed count has LOST rules from the denominator; a clause
    /// above it has INVENTED them.</summary>
    [Fact]
    public void EveryClause_HoldsExactlyTheOrdinals_TheTranscriptionPrints()
    {
        var disagreements = Disagreements(PrintedOrdinalsByClause(SpecLines()), CatalogRowsByClause());
        Assert.True(disagreements.Count == 0,
            "the rule catalog and the transcription disagree on how many rules a clause has. A clause SHORT of "
            + "its printed count has lost normative rules from the P14 denominator (fix-queue PB689 — sixty "
            + "rules across five clauses, invisible because none of the catalog's critics count); a clause OVER "
            + "it has absorbed text that is not a rule. Regenerate the catalog "
            + "(python scripts/spec/extract_rule_catalog.py) and, if it still disagrees, read the clause and "
            + "the printed page before changing either number:\n  "
            + string.Join("\n  ", disagreements));
    }

    /// <summary>The mechanism gate. An unnumbered heading inside a numbered clause is a transcription defect —
    /// the printed standard has no heading there — and it is the shape that hides rules from every tool that
    /// keys on the clause hierarchy, not only from this one.</summary>
    [Fact]
    public void NoUnnumberedInteriorHeading_SitsInsideANumberedClause()
    {
        var interior = InteriorHeadings(SpecLines());
        Assert.True(interior.Count == 0,
            "specs/ISO_COBOL.md renders a heading inside a numbered clause's text. The standard prints in-clause "
            + "group labels (SEQUENTIAL FILES, FORMAT 3, ALL FORMATS) in BODY type, not as headings — verified in "
            + "the PDF text layer on pp146, 463, 609, 680 and 820 — and the transcription's own convention is a "
            + "plain column-0 line (209 of 218 labels). Demote each of these to a plain line:\n  "
            + string.Join("\n  ", interior.Select(h => $"line {h.Line} in §{h.Clause}: {h.Text}")));
    }

    /// <summary>⛔ AN FMT ROW IS A FORMAT THE STANDARD PRINTS (kb/Work PB1536 Q2). The extractor used to emit
    /// one for any heading titled "General formats", and §5.2 — the description-techniques clause ABOUT general
    /// formats, whose body is only its sub-clause §5.2.1 — manufactured FMT-5.2: a permanent GAP row whose text
    /// was a bare anchor. A row with neither a diagram nor a Format label is that shape again, or a format whose
    /// diagram the transcription lost (§15.78.2 REVERSE printed as bare text until wave 60) — either way nothing
    /// a grammar rule can be verified against. Measured over the WRITTEN catalog, not the extractor's code.</summary>
    [Fact]
    public void EveryFmtRow_CarriesADiagramOrAFormatLabel()
    {
        var undrawn = UndrawnFmtRows(CatalogFmtRows());
        Assert.True(undrawn.Count == 0,
            "spec-rule-catalog.json holds general-format (FMT) rows with no diagram and no Format label. A "
            + "heading titled 'General format(s)' that is a PARENT clause (its body is its own sub-clauses) is not "
            + "a construct's format and must not be a row; a LEAF format clause without a diagram lost it in "
            + "transcription — render the printed page and restore the <pre>/<u> block, then regenerate the "
            + "catalog (python scripts/spec/extract_rule_catalog.py):\n  " + string.Join("\n  ", undrawn));
    }

    /// <summary>The clauses whose heading is a general-format heading in either spelling AND whose own body (to the
    /// next heading) carries a diagram or a Format label — a format the clause PRINTS (§5.2.1).</summary>
    private static List<string> PrintedGeneralFormats(string[] lines)
    {
        var found = new List<string>();
        for (int i = 0; i < lines.Length; i++)
        {
            var m = SpecClauseText.GeneralFormatHeading.Match(lines[i]);
            if (!m.Success)
            {
                continue;
            }

            var body = new List<string>();
            for (int j = i + 1; j < lines.Length && !AnyHeading.IsMatch(lines[j]); j++)
            {
                body.Add(lines[j]);
            }

            string text = string.Join("\n", body);
            if (FmtDiagram.IsMatch(text) || FmtLabel.IsMatch(text))
            {
                found.Add(m.Groups["num"].Value);
            }
        }

        return found;
    }

    private static List<JsonElement> CatalogRows()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(
            TestRepo.Docs("rearchitecture", "spec-rule-catalog.json")));
        return [.. doc.RootElement.GetProperty("rules").EnumerateArray().Select(r => r.Clone())];
    }

    /// <summary>A line that WRITES the general-format heading as a pattern: a regex literal or a substring test
    /// spelling the words itself. The words are built from parts so this file is not its own offender (it is also
    /// excluded by name, as the one reader allowed to talk about the pattern). A lower-case spelling counts only as a
    /// regex (<c>general\s+form…</c>): lower-case words in a keyword list are prose, not the heading.</summary>
    private static readonly Regex HeadingWordsSpelledOut =
        new(@"General(?:\\s[+*]?|\s)\(?\??:?form|general\\s[+*]?\(?\??:?form", RegexOptions.Compiled);

    private static readonly Regex PatternContext =
        new(@"\bre\.(?:compile|search|match|fullmatch|findall|sub)\(|\bcompile\(|\bnew\(@""|\bRegex\b|@""", RegexOptions.Compiled);

    /// <summary>A substring test of the heading in ANY case: <c>"General format" not in page_text</c>,
    /// <c>"general format" in title.lower()</c>, <c>title.startswith("general form")</c>,
    /// <c>title.Contains("general format", …)</c>. Case-insensitive on purpose: the audit_code_citations copy found in
    /// branch review round 2 (R2-1) was exactly the case-folded shape the capital-G-only test could not see.</summary>
    private static readonly Regex MembershipTest =
        new(@"[""']general\s?form[^""']*[""']\s+(?:not\s+)?in\b"
            + @"|\.(?:startswith|endswith|find|count|contains|indexof)\(\s*[""']general\s?form",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>A lower-case regex of the heading in a PYTHON <c>re</c> call: <c>re.compile(r"general formats?", re.I)</c>.
    /// Python-only (the call is part of the shape), so a C# keyword alternation that merely contains the words stays prose.</summary>
    private static readonly Regex PythonLowerPattern =
        new(@"\bre\.(?:compile|search|match|fullmatch|findall|sub)\(.*general(?:\\s[+*]?|\s)\(?\??:?form", RegexOptions.Compiled);

    private static bool SpellsTheHeading(string line)
    {
        string t = line.TrimStart();
        if (t.StartsWith('#') || t.StartsWith("//")) return false;
        return HeadingWordsSpelledOut.IsMatch(line) && PatternContext.IsMatch(line)
               || MembershipTest.IsMatch(line) || PythonLowerPattern.IsMatch(line);
    }

    /// <summary>The no-copy sweep can fail: each way of writing the heading pattern is flagged, and a keyword list
    /// that merely contains the words is not.</summary>
    [Theory]
    [InlineData("GF = re.compile(r\"general\\s+formats?$\", re.I)", true)]
    [InlineData("if \"General format\" not in page.get_text():", true)]
    [InlineData("var m = Regex.Match(line, @\"^#+\\s+(\\d+)\\s+General formats?\\s*$\");", true)]
    [InlineData("new(@\"^#{2,6}\\s+General\\s+form(?:at)?s?\\.?\\s*$\")", true)]
    [InlineData("in_formats = \"general format\" in title.lower()", true)]
    [InlineData("GF = re.compile(r\"general formats?\", re.I)", true)]
    [InlineData("if (title.Contains(\"general format\", StringComparison.OrdinalIgnoreCase))", true)]
    [InlineData("if title.lower().startswith(\"general form\"):", true)]
    [InlineData("@\"\\bSR\\s?\\d|syntax rule|general format|non-ISO\",", false)]
    [InlineData("    /// the clause that prints its General formats", false)]
    public void TheNoCopySweep_FlagsEachSpellingAndNotAKeywordList(string line, bool flagged)
    {
        Assert.Equal(flagged, SpellsTheHeading(line));
    }

    /// <summary>⛔ ONE RULE, ONE PLACE: NO SCRIPT AND NO TEST SPELLS THE GENERAL-FORMAT HEADING ITSELF (kb/Work PB284,
    /// branch review round 1). The extractor was repaired for the one clause headed "General forms" while seven
    /// sibling copies of the heading regex kept keying on "format" alone: the figure sweep located 0 figures for
    /// §14.9.7.2 and answered CHECK: clean, and was equally blind to §7.3.23.2 ("General format." with a period).
    /// The Python readers import <c>spec_headings.py</c>; the C# guards compose <c>SpecClauseText.GeneralFormatNoun</c>
    /// (the two are held to the same clause set by <c>spec_headings.py --self-test</c>). A pattern written anywhere
    /// else is a copy that will miss the next spelling.</summary>
    [Fact]
    public void NoScriptOrTest_SpellsTheGeneralFormatHeadingItself()
    {
        string[] owners = ["scripts/spec/spec_headings.py", "tests/_shared/SpecClauseText.cs",
                           "tests/Cobol.Net.Tests.Unit/CatalogCoverageDriftTests.cs"];
        var offenders = new List<string>();
        int scanned = 0;
        foreach (string path in TrackedTree.Files())
        {
            bool py = path.StartsWith("scripts/", StringComparison.Ordinal) && path.EndsWith(".py", StringComparison.Ordinal);
            bool cs = (path.StartsWith("tests/", StringComparison.Ordinal) || path.StartsWith("src/", StringComparison.Ordinal))
                      && path.EndsWith(".cs", StringComparison.Ordinal);
            if ((!py && !cs) || owners.Contains(path, StringComparer.Ordinal)) continue;
            scanned++;
            string[] lines = File.ReadAllLines(TestRepo.At(path));
            for (int i = 0; i < lines.Length; i++)
            {
                if (SpellsTheHeading(lines[i])) offenders.Add($"{path}:{i + 1}: {lines[i].Trim()}");
            }
        }
        Assert.True(scanned > 100, $"only {scanned} script/test sources scanned — the sweep reads nothing");
        Assert.True(offenders.Count == 0,
            "a script or test writes the general-format heading pattern itself. Import scripts/spec/spec_headings.py "
            + "(GENERAL_FORMAT_TITLE / GENERAL_FORMAT_PRINTED) or compose SpecClauseText.GeneralFormatNoun / "
            + "GeneralFormatHeading, so the one clause headed 'General forms' (14.9.7.2) and the one headed "
            + "'General format.' (7.3.23.2) are never skipped again:\n  " + string.Join("\n  ", offenders));
    }

    /// <summary>⛔ EVERY GENERAL FORMAT THE TRANSCRIPTION PRINTS HAS ITS ROW, WHATEVER THE HEADING SAYS (kb/Work
    /// PB284). §14.9.7.2 is headed "General forms" — the one such heading in the standard — and the extractor, which
    /// keyed on the word "format", never emitted FMT-14.9.7.2: no parse gap, no unrecognised heading, nothing. This
    /// reads the headings straight from the transcription in BOTH spellings and holds the written catalog to them.
    /// </summary>
    [Fact]
    public void EveryPrintedGeneralFormat_HasAnFmtRow_InEitherSpellingOfItsHeading()
    {
        var held = CatalogRows().Where(r => r.GetProperty("kind").GetString() == "FMT")
            .Select(r => r.GetProperty("section").GetString()!).ToHashSet(StringComparer.Ordinal);
        var missing = PrintedGeneralFormats(SpecLines()).Where(sec => !held.Contains(sec)).ToList();
        Assert.True(missing.Count == 0,
            "the transcription prints a general format under a 'General format(s)' / 'General forms' heading that "
            + "the catalog has no FMT row for — the denominator is short by it (kb/Work PB284). Regenerate the catalog "
            + "(python scripts/spec/extract_rule_catalog.py):\n  " + string.Join("\n  ", missing.Select(s => $"FMT-{s}")));
        Assert.Contains("14.9.7.2", held);   // the one clause headed 'General forms'
    }

    /// <summary>⛔ A PROSE CONCEPT CLAUSE ADMITTED AS PARAGRAPH ROWS IS HELD SENTENCE FOR SENTENCE (kb/Work PB284
    /// part 3). §9.1.16 (record locking), §9.1.17 and §9.1.18 (commit and rollback) state their rules as running
    /// prose with no numbered marker, so the numbered scan never opened them and the burn-down was measured against
    /// a denominator that did not hold the clause that SETS EC-FLOW-APPLY. The rows' text, in order, must be the
    /// clause's body.</summary>
    [Fact]
    public void ProseClauses_AreHeldSentenceForSentence()
    {
        string[] clauses = ["9.1.16", "9.1.17", "9.1.18.1", "9.1.18.2", "9.1.18.3", "9.1.18.4", "9.1.18.5",
                            "9.1.18.6", "9.1.18.7"];
        var rows = CatalogRows().Where(r => r.TryGetProperty("shape", out var s) && s.GetString() == "paragraph")
            .ToList();
        var lines = SpecLines();
        foreach (string clause in clauses)
        {
            var mine = rows.Where(r => r.GetProperty("section").GetString() == clause).ToList();
            Assert.True(mine.Count > 0, $"§{clause} has no paragraph rows — the commit/rollback concept clauses left the denominator");
            Assert.Equal(Enumerable.Range(1, mine.Count),
                         mine.Select(r => r.GetProperty("ordinal").GetInt32()));

            int at = Array.FindIndex(lines, l => Regex.IsMatch(l, @"^#{2,6}\s+" + Regex.Escape(clause) + @"\s"));
            Assert.True(at >= 0, $"§{clause} heading not found in the transcription");
            var body = new List<string>();
            for (int j = at + 1; j < lines.Length && !AnyHeading.IsMatch(lines[j]); j++)
            {
                if (!Furniture.IsMatch(lines[j]))
                {
                    body.Add(lines[j]);
                }
            }

            static string Squash(string s) => Regex.Replace(s.Replace("\\", ""), @"\s+", " ").Trim();
            Assert.Equal(Squash(string.Join(" ", body)),
                         Squash(string.Join(" ", mine.Select(r => r.GetProperty("text").GetString()!))));
        }

        // The sentence the whole group exists for: only §9.1.18.7 SETS EC-FLOW-APPLY.
        Assert.Contains(rows, r => r.GetProperty("section").GetString() == "9.1.18.7"
                                   && r.GetProperty("text").GetString()!.Contains("EC-FLOW-APPLY", StringComparison.Ordinal));
        // §9.1.16 paragraph 6 forbids what may be WRITTEN, so it is a syntax rule (§5.3.2); the rest of the clause is general.
        Assert.Contains(rows, r => r.GetProperty("id").GetString() == "SR-9.1.16-p6");
    }

    /// <summary>⛔ THE PRINTED FORMAT BAND IS A FIELD, AND THE RULE BEFORE IT IS NOT POLLUTED BY IT (kb/Work PB284).
    /// The standard scopes a rule by the label printed above it ("FORMAT 2", "ALL FORMATS", "SEQUENTIAL FILES").
    /// The extractor once glued the label onto the PRECEDING rule's text ("… is 88. FORMAT 5") and carried no scope;
    /// now the label is <c>band</c>, its format numbers are <c>band-formats</c>, and no SR/GR text ends in one.</summary>
    [Fact]
    public void TheFormatBand_IsACatalogField_NeverGluedOntoARulesText()
    {
        var rows = CatalogRows();
        var glued = rows.Where(r => r.GetProperty("kind").GetString() is "SR" or "GR")
            .Where(r => Regex.IsMatch(r.GetProperty("text").GetString()!, @"\b(?:ALL )?FORMATS?(?: \d+(?:(?:,| AND|, AND) \d+)*)?\s*$"))
            .Select(r => r.GetProperty("id").GetString()!).ToList();
        Assert.True(glued.Count == 0, "a printed band label leaked into a rule's text:\n  " + string.Join("\n  ", glued));

        static int[] Bands(JsonElement r) =>
            r.TryGetProperty("band-formats", out var b) ? [.. b.EnumerateArray().Select(n => n.GetInt32())] : [];
        var byId = rows.ToDictionary(r => r.GetProperty("id").GetString()!, StringComparer.Ordinal);
        Assert.Equal([5], Bands(byId["SR-13.18.63.3-36"]));      // VALUE clause FORMAT 5 (validation)
        Assert.Equal([2], Bands(byId["SR-13.18.41.3-1"]));       // PRESENT WHEN FORMAT 2 (validation)
        Assert.Equal([2], Bands(byId["GR-14.9.11.4-12"]));       // DISPLAY FORMAT 2 (screen)
        Assert.Equal([1, 2], Bands(byId["GR-13.18.41.4-1"]));    // "FORMATS 1 AND 2": a SUBSET test must see both
        Assert.Empty(Bands(byId["SR-13.18.63.3-1"]));            // "ALL FORMATS": a band, but no format number
        Assert.Equal("ALL FORMATS", byId["SR-13.18.63.3-1"].GetProperty("band").GetString());
        Assert.False(byId["SR-14.9.1.3-5"].TryGetProperty("band", out _)); // §14.9.1.3 prints no band at all
        // A band that is not about formats still scopes: WRITE's first sixteen rules are 'ALL FILES', the next 'SEQUENTIAL FILES'.
        Assert.Equal("ALL FILES", byId["GR-14.9.51.4-1"].GetProperty("band").GetString());
        Assert.Equal("SEQUENTIAL FILES", byId["GR-14.9.51.4-17"].GetProperty("band").GetString());
        Assert.Empty(Bands(byId["GR-14.9.51.4-17"]));
    }

    /// <summary>⛔ Every predicate fired against inputs built to break it. Without this, a refactor that made
    /// either scrape return nothing would leave two permanently-green assertions over an empty set.</summary>
    [Fact]
    public void TheseChecks_ActuallyFail_OnAFabricatedSpec()
    {
        // A minimal spec: one rule block of three rules, with a group label between rules 2 and 3.
        string[] good =
        [
            "#### 14.9.51 WRITE statement",
            "##### 14.9.51.4 General rules",
            @"1\) first rule.",
            @"2\) second rule.",
            "SEQUENTIAL FILES",
            @"3\) third rule.",
            "#### 14.9.52 Next statement",
        ];
        Assert.Empty(InteriorHeadings(good));
        Assert.Equal(3, PrintedOrdinalsByClause(good)["14.9.51.4"]);
        Assert.Empty(Disagreements(PrintedOrdinalsByClause(good),
            new Dictionary<string, int>(StringComparer.Ordinal) { ["14.9.51.4"] = 3 }));

        // PB689 itself: the label transcribed as a heading. The count must NOT change (the scan may not stop
        // there) and the interior-heading gate must report it.
        string[] headingified = [.. good.Select(l => l == "SEQUENTIAL FILES" ? "## SEQUENTIAL FILES" : l)];
        Assert.Equal(3, PrintedOrdinalsByClause(headingified)["14.9.51.4"]);
        var reported = InteriorHeadings(headingified);
        Assert.Single(reported);
        Assert.Equal("14.9.51.4", reported[0].Clause);

        // A catalog short of the printed count — the truncation PB689 shipped for months.
        Assert.Single(Disagreements(PrintedOrdinalsByClause(good),
            new Dictionary<string, int>(StringComparer.Ordinal) { ["14.9.51.4"] = 2 }));

        // ... and one over it, the inflation direction.
        Assert.Single(Disagreements(PrintedOrdinalsByClause(good),
            new Dictionary<string, int>(StringComparer.Ordinal) { ["14.9.51.4"] = 4 }));

        // An annex heading still ends a clause, so annex prose can never be counted as that clause's rules.
        string[] intoAnnex =
        [
            "##### 16.2.2.2 General rules",
            @"1\) only rule.",
            "## Annex A (normative)",
            "1. not a rule.",
            "2. also not a rule.",
        ];
        Assert.Equal(1, PrintedOrdinalsByClause(intoAnnex)["16.2.2.2"]);
        Assert.Empty(InteriorHeadings(intoAnnex));

        // PB1536 Q2: the row §5.2's heading manufactured (its text was only the anchor of §5.2.1) is reported; a
        // drawn format and a labelled multi-format construct are not.
        var undrawn = UndrawnFmtRows(
        [
            ("FMT-5.2", "<a id=\"section-5-2-1\"></a>"),
            ("FMT-15.78.2", "<pre style=\"line-height:1\"> <u>FUNCTION</u> <u>REVERSE</u> ( argument-1 ) </pre>"),
            ("FMT-7.2.4.2", "Format 1 (replacing): == pseudo-text-1 =="),
        ]);
        Assert.Single(undrawn);
        Assert.StartsWith("FMT-5.2:", undrawn[0], StringComparison.Ordinal);

        // PB284: a general format printed under the PLURAL heading is found, and so is the singular; a parent
        // clause with no diagram is not; and the heading scan reads the BODY, so a diagram-less leaf is not a format.
        string[] forms =
        [
            "##### 14.9.7.2 General forms",
            "<pre style=\"line-height:1\">",
            "<u>COMMIT</u>",
            "</pre>",
            "##### 14.9.7.3 Syntax rules",
            "##### 14.9.36.2 General format",
            "<pre style=\"line-height:1\">",
            "<u>ROLLBACK</u>",
            "</pre>",
            "#### 5.2 General formats",
            "##### 5.2.1 Description",
        ];
        Assert.Equal(["14.9.7.2", "14.9.36.2"], PrintedGeneralFormats(forms));
    }
}
