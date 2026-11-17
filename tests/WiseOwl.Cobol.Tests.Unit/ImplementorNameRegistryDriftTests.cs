// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ EVERY IMPLEMENTOR-NAME THIS IMPLEMENTATION SPECIFIES IS A ROW OF THE ONE REGISTRY, AND THE COBOL-85 COMPUTER-NAME GATE
/// READS EVERY ROW (kb/Work PB5884). §1985:IV.4.2.2.1.2: "Within a given implementation these three types of system-names form
/// disjoint sets; a given system-name may belong to one and only one of them." The gate read two of the four clause tables,
/// so <c>SOURCE-COMPUTER. DISK.</c> compiled at COBOL-85. These tests hold the registry to each clause table (a name added to a
/// clause's table that is not a row here is RED) and every position of the registry to the gate (a position the gate stops
/// reading is RED), with the complement: each name compiles as a computer-name from 2002.
/// </summary>
public sealed class ImplementorNameRegistryDriftTests
{
    /// <summary>Each clause table, by the position (an <c>ImplementorNamePosition</c> member's name) its names take in the
    /// registry. Each table's names are sorted: they are part of the case's display name, which the gate lists before
    /// the run and matches after it, and a dictionary's or a set's order is not stable between the two processes.</summary>
    public static TheoryData<string, string[]> ClauseTables() => new()
    {
        { nameof(ImplementorNamePosition.DeviceName), Names(SystemNameKind.Device) },
        { nameof(ImplementorNamePosition.FeatureName), Names(SystemNameKind.Feature) },
        { nameof(ImplementorNamePosition.SwitchName), Names(SystemNameKind.Switch) },
        { nameof(ImplementorNamePosition.AlphanumericCodeName), Sorted(ImplementorCodeNames.All.Where(r => !r.National).Select(r => r.Name)) },
        { nameof(ImplementorNamePosition.NationalCodeName), Sorted(ImplementorCodeNames.All.Where(r => r.National).Select(r => r.Name)) },
        { nameof(ImplementorNamePosition.AssignDeviceClass), Sorted(AssignTargetRule.DeviceClasses) },
        { nameof(ImplementorNamePosition.RecordDelimiterName), Sorted(RecordDelimiterNames.Names) },
    };

    private static string[] Names(SystemNameKind kind) =>
        Sorted(ImplementorNames.All.Values.Where(r => r.Kind == kind).Select(r => r.Name));

    private static string[] Sorted(IEnumerable<string> names) => [.. names.Order(StringComparer.Ordinal)];

    /// <summary>The registry's rows at a position are exactly the names of that position's clause table.</summary>
    [Theory]
    [MemberData(nameof(ClauseTables))]
    public void EveryClauseTableName_IsARegistryRowOfItsPosition(string positionName, string[] tableNames)
    {
        var position = Enum.Parse<ImplementorNamePosition>(positionName);
        var rows = ImplementorNameRegistry.All.Where(r => r.Position == position).Select(r => r.Name).Order(StringComparer.Ordinal);
        Assert.Equal(tableNames.Order(StringComparer.Ordinal), rows);
        foreach (string n in tableNames)
            Assert.Equal(position, ImplementorNameRegistry.Lookup(n)?.Position);
    }

    /// <summary>Every position has a clause table above, so a position added to the enum without its source is RED.</summary>
    [Fact]
    public void EveryPosition_HasItsClauseTable() =>
        Assert.Equal(Enum.GetNames<ImplementorNamePosition>().Order(StringComparer.Ordinal),
            ClauseTables().Select(row => (string)row[0]).Order(StringComparer.Ordinal));

    /// <summary>The gate reads every row: each row is refused as a SOURCE-COMPUTER and an OBJECT-COMPUTER computer-name at
    /// COBOL-85 (COBOL7624, naming the row's position), and compiles as one at 2002 (§2002:8.3.1.1.2 leaves computer-name out of
    /// the disjoint types).</summary>
    [Fact]
    public void EveryRow_IsRefusedAsAComputerNameAt85_AndAcceptedAt2002()
    {
        Assert.NotEmpty(ImplementorNameRegistry.All);
        foreach (var row in ImplementorNameRegistry.All)
        {
            string paragraph = row.Name.Length % 2 == 0 ? "SOURCE-COMPUTER" : "OBJECT-COMPUTER";
            var (ok85, errors85) = Compile(paragraph, row.Name, 85);
            Assert.False(ok85, $"{paragraph}. {row.Name}. compiled at 85");
            Assert.Contains(errors85, e => e.Contains("COBOL7624", StringComparison.Ordinal)
                && e.Contains($"computer-name {CobolNames.UpperFold(row.Name)} is one of this implementation's implementor-names ({row.Description})",
                    StringComparison.Ordinal));
            var (ok2002, errors2002) = Compile(paragraph, row.Name, 2002);
            Assert.True(ok2002, $"{paragraph}. {row.Name}. at 2002: " + string.Join(" | ", errors2002));
        }
    }

    private static (bool Ok, IReadOnlyList<string> Errors) Compile(string paragraph, string word, int edition)
    {
        string source = $"""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. IREG.
                   ENVIRONMENT DIVISION.
                   CONFIGURATION SECTION.
                   {paragraph}. {word}.
                   PROCEDURE DIVISION.
                   P1.
                       DISPLAY "RAN".
                       STOP RUN.
            """;
        string dir = ScratchDirectory.Create("WiseOwlCobol_Ireg_");
        try
        {
            string src = Path.Combine(dir, "prog.cob");
            File.WriteAllText(src, source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "prog.dll"), DialectLevel: edition, CheckOnly: true, SourceFormat: InitialReferenceFormat.Fixed));
            return (r.Success, r.Success ? [] : [.. r.Errors]);
        }
        finally { ScratchDirectory.Remove(dir); }
    }
}
