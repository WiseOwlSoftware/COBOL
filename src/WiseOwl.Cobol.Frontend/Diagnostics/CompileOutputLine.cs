// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Frontend.Diagnostics;

/// <summary>The compiler's own output streams a compile-time transfer may name — the compiler process's standard output,
/// its standard error (the SYSOUT / CONSOLE and SYSERR device-names of <c>ImplementorNames</c>, DOC-A.1-54) and the source
/// listing's device.</summary>
public enum CompileOutputStream
{
    /// <summary>The compiler's standard output — the output devices CONSOLE and SYSOUT.</summary>
    Standard,

    /// <summary>The compiler's standard error — the output device SYSERR.</summary>
    Error,

    /// <summary>"The same device as that used for source listings" (§2023:7.3.12.4 GR5 a)) — where the DISPLAY directive's data
    /// goes with UPON LISTING and with no UPON phrase (GR6). A compilation that produces a listing places the line in the
    /// listing at its directive and never transfers it here (DESIGN-listing.md section 6.10); one that produces none
    /// transfers it on this stream, which the command line writes to its standard output (DOC-A.1-53, DOC-A.1-54).</summary>
    Listing,
}

/// <summary>One line the compilation transfers to its operator at compile time (ISO §2023:7.3.12.4 GR1, kb/Work PB1538).</summary>
/// <param name="Stream">The compiler stream the line is written to.</param>
/// <param name="Text">The line, without its terminator.</param>
public readonly record struct CompileOutputLine(CompileOutputStream Stream, string Text);
