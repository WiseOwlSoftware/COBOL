// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime.Exceptions;

// The §2023:14.8.4.1 EC-EXTERNAL checking halves, beside CheckingFlags: the exception engine builds them
// (ExceptionEngine.ExternalActivatingMask), and the external store and the binder read them from above (kb/Work PB2743).

/// <summary>The EC-EXTERNAL conformance-check selector bits (ISO §2023:14.8.4 / §2023:14.9.4.4 GR3e's rule→condition
/// table). A bit is set in an element's mask when checking for the corresponding level-3 condition is enabled
/// there (>>TURN, §2023:7.3.25); the §2023:14.8.4.1 "both the activating and activated runtime elements" rule is realized
/// as the bitwise AND of the CALL-site mask and the activated element's before-Environment-division mask —
/// each condition pairs independently.</summary>
[Flags]
public enum ExternalChecks
{
    None = 0,
    /// <summary>EC-EXTERNAL-FORMAT-CONFLICT — §2023:14.8.4.3 / §2023:13.18.22.4 GR6 (Fatal).</summary>
    FormatConflict = 1,
    /// <summary>EC-EXTERNAL-DATA-MISMATCH — §2023:14.8.4.2 file status / linage / relative key storage identity (Fatal).</summary>
    DataMismatch = 2,
    /// <summary>EC-EXTERNAL-FILE-MISMATCH — §2023:14.8.4.4 / §2023:12.4.5.3 GR1 a–m entry consistency (Fatal).</summary>
    FileMismatch = 4,
    /// <summary>EC-EXTERNAL-IMP — implementor-defined mismatch (Imp). This implementation defines NO
    /// implementor-specific external checks, so the condition has no raise site (a conforming choice —
    /// the condition is implementor-defined by Table 13).</summary>
    Imp = 8,
}
