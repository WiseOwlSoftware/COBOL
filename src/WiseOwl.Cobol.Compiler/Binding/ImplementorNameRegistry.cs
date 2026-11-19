// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Frozen;
using WiseOwl.Cobol.Editions;

namespace WiseOwl.Cobol.Binding;

/// <summary>The position of the source program at which this implementation specifies an implementor-name: each is
/// one clause's fixed set of names, and each set is documented by its Annex A.1 item.</summary>
internal enum ImplementorNamePosition
{
    /// <summary>SPECIAL-NAMES device-name-1 (A.1 item 189; X3.23-1985's implementor-name of that paragraph).</summary>
    DeviceName,
    /// <summary>SPECIAL-NAMES feature-name-1 (A.1 item 190).</summary>
    FeatureName,
    /// <summary>SPECIAL-NAMES switch-name-1 (A.1 item 191).</summary>
    SwitchName,
    /// <summary>ALPHABET code-name-1 (A.1 items 183 and 184; X3.23-1985's implementor-name-2 of the ALPHABET clause).</summary>
    AlphanumericCodeName,
    /// <summary>ALPHABET code-name-2 (A.1 item 184: this implementation specifies none).</summary>
    NationalCodeName,
    /// <summary>The device class that may lead the ASSIGN clause's two-operand TO phrase (A.1 item 71).</summary>
    AssignDeviceClass,
    /// <summary>The RECORD DELIMITER clause's implementor-name-1, feature-name-1 from 2002 (A.1 item 150).</summary>
    RecordDelimiterName,
}

/// <summary>One implementor-name this implementation specifies, and the position it is specified for.</summary>
internal sealed record SpecifiedImplementorName(string Name, ImplementorNamePosition Position)
{
    /// <summary>What the name is, for a diagnostic: "SPECIAL-NAMES device-name", "ASSIGN device class", ….</summary>
    public string Description => ImplementorNameRegistry.Describe(Position);
}

/// <summary>
/// ⛔ THE ONE REGISTRY OF EVERY IMPLEMENTOR-NAME THIS IMPLEMENTATION SPECIFIES (kb/Work PB5884). X3.23-1985 makes the
/// three types of system-name disjoint: "Within a given implementation these three types of system-names form disjoint sets;
/// a given system-name may belong to one and only one of them" (§1985:IV.4.2.2.1.2), so a question that asks "is this word
/// an implementor-name of this implementation" must see every name the implementation specifies, at every position. The
/// names were in four tables, one per clause, and the one question asked of them (the 1985 computer-name gate,
/// <c>VersionConformancePass.GateComputerName</c>) read two, so <c>SOURCE-COMPUTER. DISK.</c> compiled at COBOL-85.
/// <para><b>Each clause's table stays the ONE definition of its names and of what each means</b>, and the registry holds no
/// spelling of its own: it is assembled from those tables, one source per <see cref="ImplementorNamePosition"/> —
/// <see cref="ImplementorNames"/> (device-, feature- and switch-names), <see cref="ImplementorCodeNames"/> (the ALPHABET
/// code-names), <see cref="AssignTargetRule.DeviceClasses"/> and <see cref="RecordDelimiterNames"/>. A name added to any of
/// them is a row here by construction; a NEW clause whose implementor-names are a fixed set adds its table as one more
/// source and one more position (<c>ImplementorNameRegistryDriftTests</c> holds every source to the registry and every
/// position to the computer-name gate).</para>
/// <para><b>What is not here.</b> A position whose content the implementation leaves open (one ASSIGN operand naming the
/// file, VALUE OF implementor-name-1, RERUN ON implementor-name) specifies no name, so it has no row. This implementation
/// specifies no language-name (the ENTER statement is not provided, kb/Work PB5308), so the third type of system-name has
/// no set to be disjoint from.</para>
/// </summary>
internal static class ImplementorNameRegistry
{
    /// <summary>Every specified implementor-name, in position order and within a position in its table's order.</summary>
    public static IReadOnlyList<SpecifiedImplementorName> All { get; } = Build();

    /// <summary>The rows keyed by name, on the basic letters alone (§2023:8.1.3.2 GR3a; a system-name is written in COBOL basic
    /// letters, so it is a <see cref="StandardWord"/>, a word the implementor defines, never a user word). A name specified at two
    /// positions keeps the first.</summary>
    private static readonly FrozenDictionary<StandardWord, SpecifiedImplementorName> ByName = All
        .GroupBy(r => new StandardWord(r.Name))
        .ToFrozenDictionary(g => g.Key, g => g.First());

    /// <summary>The row naming <paramref name="word"/>, or null when this implementation specifies no implementor-name so spelled.</summary>
    public static SpecifiedImplementorName? Lookup(string word) => ByName.GetValueOrDefault(new(word));

    /// <summary>The words a diagnostic uses for <paramref name="position"/>.</summary>
    public static string Describe(ImplementorNamePosition position) => position switch
    {
        ImplementorNamePosition.DeviceName => "SPECIAL-NAMES device-name",
        ImplementorNamePosition.FeatureName => "SPECIAL-NAMES feature-name",
        ImplementorNamePosition.SwitchName => "SPECIAL-NAMES switch-name",
        ImplementorNamePosition.AlphanumericCodeName => "ALPHABET code-name",
        ImplementorNamePosition.NationalCodeName => "ALPHABET FOR NATIONAL code-name",
        ImplementorNamePosition.AssignDeviceClass => "ASSIGN device class",
        ImplementorNamePosition.RecordDelimiterName => "RECORD DELIMITER name",
        _ => throw new ArgumentOutOfRangeException(nameof(position), position, "a position the registry does not describe"),
    };

    private static List<SpecifiedImplementorName> Build()
    {
        var rows = new List<SpecifiedImplementorName>();
        foreach (var r in ImplementorNames.All.Values.OrderBy(r => r.Name, StringComparer.Ordinal))
            rows.Add(new(r.Name, r.Kind switch
            {
                SystemNameKind.Device => ImplementorNamePosition.DeviceName,
                SystemNameKind.Feature => ImplementorNamePosition.FeatureName,
                SystemNameKind.Switch => ImplementorNamePosition.SwitchName,
                _ => throw new InvalidOperationException($"system-name kind {r.Kind} has no registry position"),
            }));
        foreach (var r in ImplementorCodeNames.All)
            rows.Add(new(r.Name, r.National ? ImplementorNamePosition.NationalCodeName : ImplementorNamePosition.AlphanumericCodeName));
        foreach (string n in AssignTargetRule.DeviceClasses.Order(StringComparer.Ordinal))
            rows.Add(new(n, ImplementorNamePosition.AssignDeviceClass));
        foreach (string n in RecordDelimiterNames.Names)
            rows.Add(new(n, ImplementorNamePosition.RecordDelimiterName));
        return [.. rows.OrderBy(r => r.Position)];
    }
}
