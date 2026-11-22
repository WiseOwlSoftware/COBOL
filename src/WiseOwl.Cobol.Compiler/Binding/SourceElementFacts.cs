// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Binding;

/// <summary>What a source element's <see cref="DataBinder"/> knows of the element itself, beside the session's facts
/// <see cref="BindSession.NewDataBinder"/> seeds (kb/Work PB2626): a program unit's kind (the ANY LENGTH placement facts of
/// §2023:13.18.2.3 SR2–SR4, §2023:12.4.6.3.3 SR5's RECURSIVE, the static working-storage discriminator) and the COPY names below
/// COBOL-2002 (kb/Work PB5482), or an OO element's role (§2023:13.18.60.4 GR22e, kb/Work PB389).</summary>
internal sealed record SourceElementFacts(
    OoUnitRole? OoRole = null,
    IReadOnlyList<CopyWord>? CopyWordList = null,
    bool IsContained = false,
    bool IsFunction = false,
    bool IsRecursive = false,
    bool StaticWs = false)
{
    /// <summary>The COPY statement's names that are the unit's user-defined words; none for an OO element.</summary>
    public IReadOnlyList<CopyWord> CopyWords => CopyWordList ?? [];
}
