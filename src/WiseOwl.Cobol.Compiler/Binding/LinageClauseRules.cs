// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Binding;

/// <summary>The COBOL-85 designations of the LINAGE clause's syntax rules that <c>FileControlBinder.ResolveLinage</c> screens inline
/// (kb/Work PB5361): §1985:VII.3.7.3 SR1 is §2023:13.18.34.3 SR2 and §1985:VII.3.7.3 SR3 is §2023:13.18.34.3 SR3, so a refusal at
/// <c>--std 85</c> cites the rule that edition prints (<see cref="Rule1985.CitationAt"/>). <c>RecordClauseRuleDriftTests</c>
/// re-derives both from <c>specs/ANSI_COBOL_1985.md</c>. §2023:13.18.34.3 SR1 (no OCCURS) has no 1985 counterpart.</summary>
internal static class LinageClauseRules
{
    /// <summary>§1985:VII.3.7.3 SR1, which §2023:13.18.34.3 SR2 re-numbers.</summary>
    internal static readonly Rule1985 Operand = new("VII.3.7.3", 1, "must reference elementary unsigned numeric integer data items");

    /// <summary>§1985:VII.3.7.3 SR3, which §2023:13.18.34.3 SR3 re-numbers.</summary>
    internal static readonly Rule1985 Footing = new("VII.3.7.3", 3, "Integer-2 must not be greater than integer-1");
}
