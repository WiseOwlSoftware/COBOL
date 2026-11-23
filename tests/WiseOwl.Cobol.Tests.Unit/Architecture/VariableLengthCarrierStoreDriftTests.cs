// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Architecture;

/// <summary>
/// ⛔ A VARIABLE-LENGTH GROUP'S CARRIER RULES ARE WRITTEN ONCE, IN THE RUNTIME, FOR BOTH LANES (kb/Work PB2746). A declared
/// group's emitted <c>FromVarImage</c> and layout field, and a cell-backed group's <c>StorageCell.StoreVarGroupAt</c> and
/// layout, used to spell the same three rules twice, once as emitted C# text and once as runtime code: the recreation of a
/// carried dynamic-capacity table (ISO §2023:14.6.9.2) against the space fill of one the sender did not carry
/// (§2023:14.6.9.4, kb/Work PB393), the receiving truncation of a dynamic-length component (§2023:8.5.1.10.4), and the units of
/// the contiguous record layout. Each is now one runtime operation: <c>CobolVarGroup.StoreTableComponent</c>,
/// <c>StoreGroupTableComponent</c> and <c>ReceivedDyn</c>, and <c>CobolContiguousLayout.Of</c>. This pins the structure: a
/// source file that calls a table's recreation or space fill itself, or constructs a layout itself, is a second copy.
/// </summary>
[Trait("Sweep", "tree")]
public sealed class VariableLengthCarrierStoreDriftTests
{
    /// <summary>The product sources, less the files that may spell the table operations: the table itself and the
    /// carrier's one store.</summary>
    private static readonly TreeScope TableCallers = new(Under: ["src/"],
        Except: ["src/WiseOwl.Cobol.Runtime/Values/Tables/CobolDynTable.cs", "src/WiseOwl.Cobol.Runtime/Values/CobolVarGroup.cs"],
        Kinds: [".cs"]);

    /// <summary>The product sources, less the layout itself.</summary>
    private static readonly TreeScope LayoutBuilders = new(Under: ["src/"],
        Except: ["src/WiseOwl.Cobol.Runtime/Values/CobolContiguousLayout.cs"], Kinds: [".cs"]);

    [Fact]
    public void OnlyTheCarrier_DecidesBetweenRecreatingAndSpaceFillingATable()
    {
        var rx = new Regex(@"\.(FromCurrentImage|SpaceFillElements)\(|nameof\([^)]*\.(FromCurrentImage|SpaceFillElements)\)");
        var offenders = Scan(rx, TrackedTree.Files(TableCallers));
        // Make the scan able to FAIL: the carrier's store must still spell both arms.
        string carrier = File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Runtime", "Values", "CobolVarGroup.cs"));
        Assert.Contains("table.FromCurrentImage(", carrier, StringComparison.Ordinal);
        Assert.Contains("table.SpaceFillElements(", carrier, StringComparison.Ordinal);
        Assert.True(offenders.Count == 0,
            "A file decides between a dynamic-capacity table's recreation (ISO §2023:14.6.9.2) and its space fill "
            + "(§2023:14.6.9.4) itself instead of calling CobolVarGroup.StoreTableComponent / StoreGroupTableComponent "
            + "(kb/Work PB2746):\n" + string.Join("\n", offenders));
    }

    [Fact]
    public void OnlyTheLayoutBuilder_ConstructsAContiguousLayout()
    {
        var rx = new Regex(@"new CobolContiguousLayout\(|new \{nameof\(CobolContiguousLayout\)\}\(");
        var offenders = Scan(rx, TrackedTree.Files(LayoutBuilders));
        string layout = File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Runtime", "Values", "CobolContiguousLayout.cs"));
        Assert.Contains("return new CobolContiguousLayout(", layout, StringComparison.Ordinal);
        Assert.True(offenders.Count == 0,
            "A file builds a variable-length record layout itself instead of calling CobolContiguousLayout.Of, the one "
            + "builder of each component's unit and maximum (kb/Work PB2746):\n" + string.Join("\n", offenders));
    }

    /// <summary>Every line of the tracked <paramref name="files"/> that matches <paramref name="rx"/>, comments
    /// aside.</summary>
    private static List<string> Scan(Regex rx, IReadOnlyList<string> files)
    {
        var offenders = new List<string>();
        foreach (string path in files)
        {
            string[] lines = File.ReadAllLines(TestRepo.Within("src/", path));
            for (int i = 0; i < lines.Length; i++)
                if (rx.IsMatch(lines[i]) && !lines[i].TrimStart().StartsWith("//"))
                    offenders.Add($"{path}:{i + 1}: {lines[i].Trim()}");
        }
        return offenders;
    }
}
