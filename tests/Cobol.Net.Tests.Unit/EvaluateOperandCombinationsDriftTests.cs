// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Binding;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ <see cref="EvaluateOperandCombinations"/> IS ISO Table 15, AND THIS RE-DERIVES IT FROM THE SPEC.
///
/// <para>§14.9.13.3 SR10 says "The permissible combinations of selection subject and selection object operands are
/// indicated in Table 15", and Table 15 is 54 cells. A transcription of 54 cells that nothing checks is a
/// hand-maintained list (CLAUDE.md rule 5) — and, worse, a table nothing re-derives has never been contradicted
/// (<c>feedback_a_dead_lookup_is_also_unverified</c>: PB1's <c>ArgKinds</c> column was not merely unread but
/// UNVERIFIED, and enforcing it as written rejected 12 legal corpus programs). So this parses the markdown table
/// out of <c>specs/ISO_COBOL.md</c> and compares cell for cell.</para>
///
/// <para>⚠ It asserts the SHAPE it found before comparing, so a transcription reformat that breaks the scrape
/// fails loudly instead of silently comparing nothing (the vacuous-pass trap).</para>
/// </summary>
public sealed class EvaluateOperandCombinationsDriftTests
{
    private static readonly string[] ObjectRowLabels =
    [
        "[NOT] identifier", "[NOT] literal", "[NOT] arithmetic-expression", "[NOT] boolean-expression",
        "[NOT] range-expression", "Condition", "Partial-expression", "TRUE or FALSE", "ANY",
    ];

    private static readonly string[] SubjectColumnLabels =
    [
        "Identifier", "Literal", "Arithmetic expression", "Boolean expression", "Condition", "TRUE or FALSE",
    ];

    /// <summary>The rows of Table 15, as label → the six cells, scraped from the spec.</summary>
    private static (List<string> Header, Dictionary<string, bool[]> Rows) ScrapeTable15()
    {
        string spec = File.ReadAllText(Path.Combine(TestRepo.Root, "specs", "ISO_COBOL.md"));
        int anchor = spec.IndexOf("<a id=\"table-15\">", StringComparison.Ordinal);
        Assert.True(anchor >= 0, "the `table-15` anchor is gone from specs/ISO_COBOL.md — this guard must follow it");

        // The table runs from the anchor to the first blank line after the last '|' row.
        var lines = spec[anchor..].Split('\n').Select(l => l.TrimEnd('\r')).ToList();
        var rowLines = lines.TakeWhile((l, i) => i < 40).Where(l => l.StartsWith('|')).ToList();
        Assert.True(rowLines.Count >= 12,
            $"expected the Table-15 markdown block (header + separator + label row + 9 data rows); scraped "
            + $"{rowLines.Count} pipe-rows — the transcription's shape changed and this scrape needs updating");

        static List<string> Cells(string line) =>
            [.. line.Split('|').Skip(1).SkipLast(1).Select(c => c.Trim().Trim('*').Trim())];

        // Row 0 = "Selection object | Selection subject | | …", row 1 = the |---| separator,
        // row 2 = the bolded subject labels, rows 3.. = the data.
        var header = Cells(rowLines[2]).Skip(1).ToList();
        Assert.Equal(SubjectColumnLabels, header);

        var rows = new Dictionary<string, bool[]>(StringComparer.Ordinal);
        foreach (var line in rowLines.Skip(3))
        {
            var cells = Cells(line);
            if (cells.Count < 7) continue;
            rows[cells[0]] = [.. cells.Skip(1).Take(6).Select(c => c == "Y")];
        }
        return (header, rows);
    }

    /// <summary>Every one of the 54 cells matches the standard's own table.</summary>
    [Fact]
    public void TheMatrix_EqualsTable15_CellForCell()
    {
        var (_, rows) = ScrapeTable15();
        Assert.Equal(ObjectRowLabels.Length, rows.Count);

        var mismatches = new List<string>();
        int compared = 0;
        for (int r = 0; r < ObjectRowLabels.Length; r++)
        {
            Assert.True(rows.TryGetValue(ObjectRowLabels[r], out var spec),
                $"Table 15 has no row '{ObjectRowLabels[r]}' — the row labels changed");
            for (int c = 0; c < SubjectColumnLabels.Length; c++)
            {
                bool ours = EvaluateOperandCombinations.IsPermitted((EvaluateSubjectOperand)c, (EvaluateObjectOperand)r);
                compared++;
                if (ours != spec![c])
                    mismatches.Add($"[{ObjectRowLabels[r]} × {SubjectColumnLabels[c]}]: table says "
                                   + $"{(spec[c] ? "Y" : "invalid")}, we say {(ours ? "Y" : "invalid")}");
            }
        }
        Assert.Equal(54, compared);   // the population, so a broken scrape cannot pass vacuously
        Assert.True(mismatches.Count == 0, "EvaluateOperandCombinations has drifted from ISO Table 15:\n  "
                                           + string.Join("\n  ", mismatches));
    }

    /// <summary>Table 15's two axes NAME THE SAME OPERAND KINDS, and the compiler's row→column mapping says so
    /// — derived from the spec's own labels rather than asserted from the enum spellings.
    /// <para>⛔ This is the structural half of kb/Work PB400. The binder classifies a selection subject and a
    /// selection object with ONE function and maps the result across the axes through
    /// <c>AsSubjectOperand</c>; two independently written classifiers is what let the object side recognise a
    /// switch-status condition-name while the subject side did not. If a future edition adds a row that is also
    /// a column (or renames one), this fails rather than silently leaving one side unmapped.</para></summary>
    [Fact]
    public void TheAxisMapping_NamesTheSameKindOnBothAxes()
    {
        // Normalize a label to the KIND it names, across the three spellings in play: the spec's object axis
        // prints "[NOT] " on the five negatable rows and hyphenates its two-word kinds, the spec's subject axis
        // does neither, and the compiler's diagnostic labels carry an English article ("an identifier").
        static string Kind(string label)
        {
            string t = label.Replace("[NOT] ", "", StringComparison.Ordinal).Replace('-', ' ').ToLowerInvariant().Trim();
            foreach (string article in (string[])["an ", "a ", "the "])
                if (t.StartsWith(article, StringComparison.Ordinal)) return t[article.Length..];
            return t;
        }

        var (header, rows) = ScrapeTable15();
        var columnKinds = header.Select(Kind).ToHashSet(StringComparer.Ordinal);
        Assert.Equal(6, columnKinds.Count);

        // Every ROW whose kind is also a COLUMN maps to that column; every row whose kind is not maps to null.
        int mapped = 0;
        for (int r = 0; r < ObjectRowLabels.Length; r++)
        {
            var s = EvaluateOperandCombinations.AsSubjectOperand((EvaluateObjectOperand)r);
            bool isAlsoAColumn = columnKinds.Contains(Kind(ObjectRowLabels[r]));
            Assert.True(isAlsoAColumn == (s is not null),
                $"Table 15 row '{ObjectRowLabels[r]}' {(isAlsoAColumn ? "IS" : "is NOT")} also a subject column, "
                + $"but AsSubjectOperand returns {(s is null ? "null" : s.ToString())}");
            if (s is null) continue;
            mapped++;
            // The mapped column is the one naming the SAME kind — checked through the compiler's own labels, so
            // a mapping that pointed at the wrong column would fail even though both ends exist.
            Assert.Equal(Kind(ObjectRowLabels[r]), Kind(EvaluateOperandCombinations.Label(s.Value)));
        }
        Assert.Equal(6, mapped);   // the population — all six columns are reachable, so no side is unmapped
        Assert.Equal(rows.Count, ObjectRowLabels.Length);
    }

    /// <summary>Spot-checks that state the table's CONSEQUENCES in the compiler's own terms, so the intent is
    /// readable without cross-referencing the spec — and so a scrape that silently inverted would still fail.</summary>
    [Fact]
    public void TheConsequencesThatMatter_AreWhatTheTableSays()
    {
        // A TRUE/FALSE subject admits ONLY a condition, TRUE/FALSE, or ANY — this is the PB45/PB47 row.
        Assert.True(EvaluateOperandCombinations.IsPermitted(EvaluateSubjectOperand.TrueOrFalse, EvaluateObjectOperand.Condition));
        Assert.True(EvaluateOperandCombinations.IsPermitted(EvaluateSubjectOperand.TrueOrFalse, EvaluateObjectOperand.TrueOrFalse));
        Assert.True(EvaluateOperandCombinations.IsPermitted(EvaluateSubjectOperand.TrueOrFalse, EvaluateObjectOperand.Any));
        Assert.False(EvaluateOperandCombinations.IsPermitted(EvaluateSubjectOperand.TrueOrFalse, EvaluateObjectOperand.Identifier));
        Assert.False(EvaluateOperandCombinations.IsPermitted(EvaluateSubjectOperand.TrueOrFalse, EvaluateObjectOperand.Literal));
        Assert.False(EvaluateOperandCombinations.IsPermitted(EvaluateSubjectOperand.TrueOrFalse, EvaluateObjectOperand.RangeExpression));
        // ⚠ And the converse: a CONDITION subject does NOT admit a value object.
        Assert.False(EvaluateOperandCombinations.IsPermitted(EvaluateSubjectOperand.Condition, EvaluateObjectOperand.Identifier));
        // ANY is permissible against every subject — the one full row.
        foreach (EvaluateSubjectOperand s in Enum.GetValues<EvaluateSubjectOperand>())
            Assert.True(EvaluateOperandCombinations.IsPermitted(s, EvaluateObjectOperand.Any));
        // ⚠ A LITERAL object against a LITERAL subject is INVALID — a blank cell that is easy to assume is 'Y'.
        Assert.False(EvaluateOperandCombinations.IsPermitted(EvaluateSubjectOperand.Literal, EvaluateObjectOperand.Literal));
        Assert.True(EvaluateOperandCombinations.IsPermitted(EvaluateSubjectOperand.Identifier, EvaluateObjectOperand.Literal));
    }
}
