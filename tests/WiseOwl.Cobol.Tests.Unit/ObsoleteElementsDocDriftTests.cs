// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// §1985:I.1.5.2.3: "Documentation associated with an implementation must identify all obsolete language elements in the
/// implementation" and "A conforming implementation of Standard COBOL must provide a warning mechanism ... to indicate ... that a
/// program contains obsolete language elements" (kb/Work PB5986). The warning is the construct registry's COBOL0903 verdict, driven
/// by a row's <c>ObsoleteIn</c>; the document is the table in <c>docs/manual/user-guide/editions.md</c>. This test holds the two to
/// one another in both directions, so a row marked obsolete at 85 without a line in the document (or a line for a row that no
/// longer warns) fails the gate, and the next 1985 obsolete element is documented by construction.
/// </summary>
public sealed class ObsoleteElementsDocDriftTests
{
    private const string Heading = "## The obsolete elements of COBOL-85";
    private const int FirstEdition = 85;

    private static readonly Regex RowId = new(@"`([a-z0-9]+(?:-[a-z0-9]+)+)`", RegexOptions.CultureInvariant);

    private static HashSet<string> DocumentedRowIds()
    {
        string text = File.ReadAllText(TestRepo.Docs("manual", "user-guide", "editions.md"));
        int start = text.IndexOf(Heading, StringComparison.Ordinal);
        Assert.True(start >= 0, $"editions.md has no '{Heading}' section (§1985:I.1.5.2.3 documentation)");
        int end = text.IndexOf("\n## ", start + Heading.Length, StringComparison.Ordinal);
        string section = end < 0 ? text[start..] : text[start..end];
        return [.. RowId.Matches(section).Select(m => m.Groups[1].Value)];
    }

    private static IEnumerable<ConstructDialectStatus> ObsoleteIn1985() =>
        ConstructRegistry.Entries.Where(e => e.ObsoleteIn == FirstEdition);

    [Fact]
    public void EveryRowObsoleteAt85_IsNamedInTheUserDocument_AndEveryNamedRowWarns()
    {
        var documented = DocumentedRowIds();
        var obsolete = ObsoleteIn1985().Select(e => e.Id).ToHashSet(StringComparer.Ordinal);
        Assert.True(obsolete.Count > 0, "no construct row is obsolete at 85: the test checked nothing");
        Assert.True(obsolete.SetEquals(documented),
            $"editions.md's COBOL-85 obsolete table and the registry's ObsoleteIn=85 rows differ: undocumented [{string.Join(", ", obsolete.Except(documented))}], "
            + $"documented but not obsolete at 85 [{string.Join(", ", documented.Except(obsolete))}]");
    }

    [Fact]
    public void EveryRowObsoleteAt85_CitesTheStandardsObsoleteElementList()
    {
        foreach (var row in ObsoleteIn1985())
            Assert.True(row.Citation.Contains("§1985:C.1 item", StringComparison.Ordinal),
                $"{row.Id} is obsolete at 85 but its citation names no item of §1985:C.1: {row.Citation}");
    }

    [Fact]
    public void AnObsoleteElementStaysInForce_WhereTheStandardRequiresItsSupport()
    {
        // §1985:I.1.5.2.3: "required to support obsolete language elements": obsolete is IN FORCE, removed is not.
        foreach (var row in ObsoleteIn1985())
        {
            Assert.Equal(ConstructAvailability.Obsolete, row.StatusAt(FirstEdition));
            Assert.True(row.IsInForceAt(FirstEdition), $"{row.Id} must remain supported at COBOL-85");
            Assert.False(row.IsInForceAt(row.RemovedIn!.Value), $"{row.Id} must be gone from COBOL-{row.RemovedIn}");
        }
    }
}
