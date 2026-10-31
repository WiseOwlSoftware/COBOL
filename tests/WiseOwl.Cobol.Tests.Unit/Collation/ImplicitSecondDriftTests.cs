// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Collation;

/// <summary>
/// Keeps "the second element of an implicit weight is never renumbered" (UTS #10 Table 16: BBBB = … | 0x8000) stated
/// ONCE. <c>CollationTable.IsImplicitSecond</c> is the rule: the tailoring builder's root lists and special positions
/// and <c>ElementRemap</c> all ask it. Before it did, the builder and the remap each wrote the floor as a literal
/// <c>0x8000</c> (kb/Work PB107 review R1-6), and a change to one moved the roots without moving the re-weighting.
/// </summary>
public sealed class ImplicitSecondDriftTests
{
    private static readonly TreeScope CollationSources = new(Under: ["src/WiseOwl.Cobol.Runtime/Collation/"], Kinds: [".cs"]);

    [Fact]
    public void TheImplicitSecondFloor_IsWrittenOnlyInTheTable()
    {
        var offenders = TrackedTree.Files(CollationSources)
            .Where(rel => Path.GetFileName(rel) != "CollationTable.cs")
            .Where(rel => File.ReadAllText(TestRepo.At(rel.Split('/'))).Contains("0x8000", StringComparison.OrdinalIgnoreCase))
            .ToList();
        Assert.True(offenders.Count == 0,
            "the implicit-second floor (0x8000) belongs to CollationTable.ImplicitSecondFlag / IsImplicitSecond; "
            + "these files write it themselves:\n" + string.Join("\n", offenders));
    }
}
