// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.Exceptions;

/// <summary>
/// The runtime backends of the last-exception interrogation functions — FUNCTION EXCEPTION-STATUS (ISO
/// §15.33), EXCEPTION-LOCATION (§15.30), EXCEPTION-STATEMENT (§15.32), and the no-argument EXCEPTION-FILE
/// (§15.28) — rendering <see cref="ExceptionState"/> per each function's returned-value rules. The national
/// twins (EXCEPTION-FILE-N §15.29 / EXCEPTION-LOCATION-N §15.31 — ISO defines no -N twin for
/// EXCEPTION-STATEMENT/-STATUS) are the SAME renderings projected to the national repertoire through the ONE
/// <see cref="CobolIntrinsics.NationalOf"/> translator (§15.66.4): each -N section's "converted at runtime to
/// the runtime national character set" IS that alphanumeric→national conversion, and under the Annex A.1
/// item-33 determination (both repertoires UTF-16; the correspondence is the total identity, PB59) every code
/// point keeps its value. The compiler-side difference is the result CATEGORY — the catalog's National rows
/// drive Table-16 MOVE/compare legality.
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
        // r1c — "the file-name exactly as specified in the SELECT clause": the connector's registered SelectName
        // (kb/Work PB63 — a "::" strip of the registry KEY used to upper-case an EXTERNAL name and keep an OBJECT
        // file's per-instance "#N" suffix).
        return (ExceptionState.LastIoStatus ?? "  ") + IO.CobolFile.SelectNameOf(file);   // r1c
    }

    /// <summary>FUNCTION EXCEPTION-FILE-N with no argument (§15.29.4 r1) — the national twin of
    /// <see cref="File"/>: two national zeros when the last exception is not an EC-I-O condition (r1a),
    /// two national spaces for a RAISE / EXIT RAISING / GOBACK RAISING origin (r1b), else the two-character
    /// I-O status "in national characters" followed by the SELECT-spelled file-name "converted at runtime to
    /// the runtime national character set" (r1c). That conversion is the ONE alphanumeric→national repertoire
    /// translation (<see cref="CobolIntrinsics.NationalOf"/>, §15.66.4) applied to the SAME <see cref="File"/>
    /// rendering — one mechanism, never a second EC formatter; digits/spaces/Latin-1 names keep their code
    /// points (D-N4). The 2023 file-connector-argument form (r2, E.3.3 item 26) is a PHASE-13 leg and renders
    /// loud compiler-side, like the base function's (VCR rows 68/69).</summary>
    public static string FileN() => CobolIntrinsics.NationalOf(File());

    /// <summary>FUNCTION EXCEPTION-FILE(file-connector-name) (§15.28.4 r2, COBOL-2023): the NAMED connector's I-O
    /// status + SELECT-spelled name (r2b), or two alphanumeric spaces when it was never opened/attempted/accessed
    /// (r2a). Unlike the no-argument form this reads the connector directly, not the last-exception register.</summary>
    public static string File(string connectorKey) => IO.CobolFile.ExceptionFile(connectorKey);

    /// <summary>FUNCTION EXCEPTION-FILE-N(file-connector-name) (§15.29.4 r2) — the national twin: the SAME
    /// <see cref="File(string)"/> rendering through the ONE alphanumeric→national repertoire translation.</summary>
    public static string FileN(string connectorKey) => CobolIntrinsics.NationalOf(File(connectorKey));

    /// <summary>FUNCTION EXCEPTION-LOCATION-N (§15.31.3) — the national twin of <see cref="Location"/>: one
    /// national space when no location information was saved (r1 — the same documented §15.30.3-r1 choice) or
    /// when no exception was raised (r2a); else the saved three-part location string "converted at runtime to
    /// the runtime national character set" (r2b) — the ONE <see cref="CobolIntrinsics.NationalOf"/> repertoire
    /// translation over the SAME <see cref="Location"/> rendering.</summary>
    public static string LocationN() => CobolIntrinsics.NationalOf(Location());
}
