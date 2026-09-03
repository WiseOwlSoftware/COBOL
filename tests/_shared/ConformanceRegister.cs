// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;

namespace CobolNet.Tests.Shared;

/// <summary>
/// <c>docs/CONFORMANCE.md</c> §7 — the Annex A.1 implementor-defined element register — parsed.
/// </summary>
/// <remarks>
/// <para>
/// ⛔ ONE PARSER PER LANGUAGE, and this is the C# one. Its twin is <c>section7_rows</c> /
/// <c>register_determinations</c> in <c>scripts/spec/inventory_schema.py</c>. Two engines read the same ARTIFACT,
/// which is the same shape as the <c>derived-verdicts</c> evaluator: what exists twice is a ten-line reader, not
/// the register. It lives here rather than in either test class because two of them need it —
/// <c>SpecTraceabilityInventoryDriftTests.AnchorObliged</c> asks whether an item has a determination at all, and
/// <c>DerivedVerdictDriftTests</c>'s <c>determination-prefix</c> arm reads what the determination SAYS — and
/// three sites keying on one markdown table independently is <c>feedback_one_rule_one_place</c> verbatim.
/// </para>
/// <para>
/// ⚠ THE SPLIT IS ON UNESCAPED PIPES, and that is not fastidiousness: the item-82 determination writes an
/// absolute value as <c>\|v\|</c>, so a naive <c>Split('|')</c> reports that row with two extra cells and
/// mis-places everything after them.
/// </para>
/// </remarks>
internal static class ConformanceRegister
{
    private const string Heading = "## 7. Annex A.1";

    /// <summary>One data row of the §7 table, by cell.</summary>
    /// <param name="Key">the inventory rule-id the row discharges — <c>DOC-A.1-&lt;n&gt;</c></param>
    /// <param name="Element">the A.1 element the row claims to be about</param>
    /// <param name="Determination">WHAT WE DO — the determination itself</param>
    /// <param name="Pinned">the spec-derived test-ref(s) that pin it, or an em dash</param>
    internal sealed record Row(string Key, string Element, string Determination, string Pinned);

    private static readonly Regex UnescapedPipe = new(@"(?<!\\)\|", RegexOptions.Compiled);

    private static readonly Lazy<List<Row>> Parsed = new(() => Parse(File.ReadAllText(Path)));

    /// <summary>The register file itself, so a caller can assert it exists before believing an empty parse.</summary>
    internal static string Path => TestRepo.Docs("CONFORMANCE.md");

    /// <summary>Every data row of §7, in document order — parsed once per test assembly.</summary>
    internal static IReadOnlyList<Row> Rows => Parsed.Value;

    /// <summary>
    /// <c>rule-id → determination cell</c>. An item with SEVERAL determinations (A.1-56 and A.1-92 legitimately
    /// have two rows each) keeps the FIRST, which is the one its obligation is discharged by — matching
    /// <c>register_determinations</c> on the Python side.
    /// </summary>
    internal static IReadOnlyDictionary<string, string> Determinations { get; } = Build();

    /// <summary>A determination cell with its leading markdown emphasis removed — a determination is written for
    /// a human first (<c>**Not provided.** Each usage holds …</c>), so a predicate reading the cell must not be
    /// reading the bold markers.</summary>
    internal static string Plain(string cell) => cell.TrimStart('*', '_', ' ', '\t');

    private static Dictionary<string, string> Build()
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var r in Rows) map.TryAdd(r.Key, r.Determination);
        return map;
    }

    internal static List<Row> Parse(string text)
    {
        var rows = new List<Row>();
        int start = text.IndexOf(Heading, StringComparison.Ordinal);
        if (start < 0) return rows;
        foreach (string line in text[start..].Split('\n'))
        {
            if (!line.StartsWith("| ", StringComparison.Ordinal)) continue;
            string[] cells = Cells(line);
            if (cells.Length < 2 || cells[0] == "A.1 item" || cells[0].All(c => c is '-' or ':' or ' ')) continue;
            rows.Add(new Row(cells[0], cells[1],
                             cells.Length > 2 ? cells[2] : "",
                             cells.Length > 3 ? cells[3] : ""));
        }

        return rows;
    }

    private static string[] Cells(string line)
    {
        string[] parts = UnescapedPipe.Split(line.TrimEnd('\r', '\n'));
        return parts.Length < 3 ? [] : [.. parts[1..^1].Select(c => c.Replace("\\|", "|").Trim())];
    }
}
