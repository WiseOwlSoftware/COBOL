// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Binding.Model;

/// <summary>
/// ⛔ A RULE A DIAGNOSTIC CITES, AS THE EDITION BEING COMPILED NUMBERS IT (kb/Work PB5352). A message names the rule in force for
/// the compile, so a rule whose COBOL-85 home is another clause carries both: <see cref="Iso2023"/>, the number every edition from
/// 2002 is cited by in this compiler, and the 1985 clause of each I-O module that states it, because ANSI X3.23-1985 restates a
/// file statement's rules once per organization (sequential VII, relative VIII, indexed IX; the sort-merge module XI has one).
/// A rule the 1985 text does not state has no 1985 arm, and is cited by its 2023 number at every edition.
/// </summary>
/// <param name="Iso2023">The citation from COBOL-2002 on, as written in the message (<c>ISO §2023:14.9.30.3 SR1</c>).</param>
/// <param name="Sequential1985">The 1985 citation in the sequential I-O module, or the module's only one (sort-merge); null when the
/// 1985 text states no such rule.</param>
/// <param name="Relative1985">The 1985 citation in the relative I-O module; null where the sequential one serves.</param>
/// <param name="Indexed1985">The 1985 citation in the indexed I-O module; null where the sequential one serves.</param>
public sealed record EditionCite(string Iso2023, string? Sequential1985 = null, string? Relative1985 = null, string? Indexed1985 = null)
{
    /// <summary>The citation of the rule in force at <paramref name="dialectLevel"/> for a file of <paramref name="organization"/>.</summary>
    public string At(int dialectLevel, FileOrganization organization) =>
        dialectLevel >= 2002 || Sequential1985 is not { } sequential ? Iso2023
        : organization switch
        {
            FileOrganization.Relative => Relative1985 ?? sequential,
            FileOrganization.Indexed => Indexed1985 ?? sequential,
            _ => sequential,
        };
}
