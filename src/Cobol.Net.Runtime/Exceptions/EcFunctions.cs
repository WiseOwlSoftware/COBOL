// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.Exceptions;

/// <summary>
/// The runtime backends of the last-exception interrogation functions — FUNCTION EXCEPTION-STATUS (ISO
/// §15.33), EXCEPTION-LOCATION (§15.30), EXCEPTION-STATEMENT (§15.32), and the no-argument EXCEPTION-FILE
/// (§15.28) — rendering <see cref="ExceptionState"/> per each function's returned-value rules. The national
/// twins (EXCEPTION-FILE-N §15.29 / EXCEPTION-LOCATION-N §15.31) stay catalogued-loud: the greenfield has no
/// national runtime, and faking national as UTF-16 alphanumeric would be the wrong class (scout hazard H8).
/// </summary>
public static class EcFunctions
{
    /// <summary>FUNCTION EXCEPTION-STATUS (§15.33.3 r1): a 31-character left-justified uppercase
    /// exception-name (space-filled), or 31 spaces when the last exception status indicates no exception.</summary>
    public static string Status() =>
        (ExceptionState.LastName ?? "").PadRight(31)[..31];

    /// <summary>FUNCTION EXCEPTION-LOCATION (§15.30.3): one space when no location information was saved (r1 —
    /// the enabling TURN lacked WITH LOCATION; this implementation's documented §15.30.3-r1 choice is to save
    /// none) or when no exception was raised (r2a); else the saved three-part location string (r2b:
    /// "element-name; paragraph[ OF section]|section; line-id", built by the compiler at the raise site).</summary>
    public static string Location() =>
        ExceptionState.LastName is null ? " " : ExceptionState.LastLocation ?? " ";

    /// <summary>FUNCTION EXCEPTION-STATEMENT (§15.32.3): 63 spaces when no location information was saved (r1);
    /// else the 63-character uppercase statement name, left-justified space-filled (r2).</summary>
    public static string Statement() =>
        ExceptionState.LastName is null || ExceptionState.LastStatement is null
            ? new string(' ', 63)
            : ExceptionState.LastStatement.ToUpperInvariant().PadRight(63)[..63];

    /// <summary>FUNCTION EXCEPTION-FILE with no argument (§15.28.4 r1): <c>"00"</c> when the last exception is
    /// not an EC-I-O condition (r1a — including the no-exception state); two spaces when the EC-I-O condition
    /// originated from RAISE / EXIT RAISING / GOBACK RAISING (r1b — there is no file connector); else the
    /// two-character I-O status followed by the file-name as specified in the SELECT clause (r1c). The stored
    /// connector key carries the per-program qualification prefix (<c>PROG::NAME</c> / <c>::EXT::NAME</c> — an
    /// emit-side namespace, CSharpEmitter.Call.cs); the SELECT-spelled name is the part after the last
    /// <c>::</c>, stripped here so the one stored key serves both dispatch and display.</summary>
    public static string File()
    {
        string? name = ExceptionState.LastName;
        if (name is null || !ExceptionCatalog.IsIoName(name)) return "00";          // r1a
        if (ExceptionState.LastFile is not { } file) return "  ";                   // r1b — RAISE-originated
        int sep = file.LastIndexOf("::", StringComparison.Ordinal);
        string display = sep >= 0 ? file[(sep + 2)..] : file;
        return (ExceptionState.LastIoStatus ?? "  ") + display;                     // r1c
    }
}
