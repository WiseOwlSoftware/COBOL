// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime.Communication;

/// <summary>
/// One communication description entry of one program instance: what the program's activation holds about that CD (§1985:XIV.2.2.4 GR31), so it dies with the
/// instance. The generated program owns one per CD and passes it to every statement that references the CD; a CANCELled program, and each CALL of a program with the
/// INITIAL attribute, is a fresh instance and so has none of the old one's, and two programs that name a CD alike share nothing (§1985:X.5.3.4 GR2).
/// </summary>
public sealed class CdActivation
{
    /// <summary>The content the data item of SYMBOLIC TERMINAL held when the first statement referencing the CD ran, or that the MCS gave it when it scheduled the run unit;
    /// null before either. "After executing the first statement referencing cd-name-1, the content of the data item referenced by data-name-3 must not be changed by the program."</summary>
    internal string? Terminal { get; set; }
}
