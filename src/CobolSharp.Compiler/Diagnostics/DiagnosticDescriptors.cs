// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolSharp.Compiler.Diagnostics;

/// <summary>
/// A descriptor for a specific diagnostic: code, default severity, message template.
/// </summary>
public sealed record DiagnosticDescriptor(
    string Code,
    DiagnosticSeverity DefaultSeverity,
    string MessageTemplate);

/// <summary>
/// Central registry of all CBL diagnostic descriptors.
/// Each phase adds descriptors as a partial class block.
/// </summary>
public static partial class DiagnosticDescriptors
{
    // ══════════════════════════════════════
    // CBL0901–0905: MOVE enforcement
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL0901 = new("CBL0901", DiagnosticSeverity.Error,
        "Illegal MOVE: {0} to {1}");
    public static readonly DiagnosticDescriptor CBL0902 = new("CBL0902", DiagnosticSeverity.Error,
        "MOVE CORRESPONDING: source '{0}' must be a group item");
    public static readonly DiagnosticDescriptor CBL0903 = new("CBL0903", DiagnosticSeverity.Error,
        "MOVE CORRESPONDING: target '{0}' must be a group item");
    public static readonly DiagnosticDescriptor CBL0904 = new("CBL0904", DiagnosticSeverity.Error,
        "MOVE of figurative constant {0} to numeric target not allowed");
    public static readonly DiagnosticDescriptor CBL0905 = new("CBL0905", DiagnosticSeverity.Error,
        "MOVE to level-88 condition name not allowed");

    // ══════════════════════════════════════
    // CBL1001–1004: VALUE clause enforcement
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL1001 = new("CBL1001", DiagnosticSeverity.Warning,
        "VALUE clause not allowed on group item '{0}'");
    public static readonly DiagnosticDescriptor CBL1002 = new("CBL1002", DiagnosticSeverity.Error,
        "Initial VALUE for '{0}' incompatible with data category");
    public static readonly DiagnosticDescriptor CBL1003 = new("CBL1003", DiagnosticSeverity.Warning,
        "Extra VALUE items for '{0}' are ignored");
    public static readonly DiagnosticDescriptor CBL1004 = new("CBL1004", DiagnosticSeverity.Error,
        "Condition value incompatible with parent item '{0}'");

    // ══════════════════════════════════════
    // CBL1101–1105: OCCURS / DEPENDING ON
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL1101 = new("CBL1101", DiagnosticSeverity.Error,
        "DEPENDING ON '{0}' must be integer numeric");
    public static readonly DiagnosticDescriptor CBL1102 = new("CBL1102", DiagnosticSeverity.Warning,
        "DEPENDING ON '{0}' must be declared before table '{1}'");
    public static readonly DiagnosticDescriptor CBL1103 = new("CBL1103", DiagnosticSeverity.Error,
        "OCCURS key '{0}' not subordinate to table '{1}'");
    public static readonly DiagnosticDescriptor CBL1104 = new("CBL1104", DiagnosticSeverity.Error,
        "OCCURS key '{0}' cannot be a group item");
    public static readonly DiagnosticDescriptor CBL1105 = new("CBL1105", DiagnosticSeverity.Error,
        "SEARCH on non-table item '{0}'");

    // ══════════════════════════════════════
    // CBL1201–1205: SEARCH / SEARCH ALL
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL1201 = new("CBL1201", DiagnosticSeverity.Error,
        "SEARCH VARYING '{0}' must be index or integer");
    public static readonly DiagnosticDescriptor CBL1202 = new("CBL1202", DiagnosticSeverity.Error,
        "SEARCH ALL on non-table item '{0}'");
    public static readonly DiagnosticDescriptor CBL1203 = new("CBL1203", DiagnosticSeverity.Error,
        "KEY '{0}' not an OCCURS key of table '{1}'");
    public static readonly DiagnosticDescriptor CBL1204 = new("CBL1204", DiagnosticSeverity.Error,
        "SEARCH ALL requires KEY phrase or OCCURS key for '{0}'");
    public static readonly DiagnosticDescriptor CBL1205 = new("CBL1205", DiagnosticSeverity.Error,
        "SEARCH ALL WHEN must be simple key comparison");

    // ══════════════════════════════════════
    // CBL1301–1304: STRING
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL1301 = new("CBL1301", DiagnosticSeverity.Error,
        "STRING INTO target must be alphanumeric or group");
    public static readonly DiagnosticDescriptor CBL1302 = new("CBL1302", DiagnosticSeverity.Error,
        "STRING source must be alphanumeric or group");
    public static readonly DiagnosticDescriptor CBL1303 = new("CBL1303", DiagnosticSeverity.Error,
        "STRING source cannot be numeric");
    public static readonly DiagnosticDescriptor CBL1304 = new("CBL1304", DiagnosticSeverity.Error,
        "STRING POINTER must be integer numeric");

    // ══════════════════════════════════════
    // CBL1401–1406: UNSTRING
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL1401 = new("CBL1401", DiagnosticSeverity.Error,
        "UNSTRING source must be alphanumeric or group");
    public static readonly DiagnosticDescriptor CBL1402 = new("CBL1402", DiagnosticSeverity.Error,
        "UNSTRING INTO target must be alphanumeric or group");
    public static readonly DiagnosticDescriptor CBL1403 = new("CBL1403", DiagnosticSeverity.Error,
        "UNSTRING DELIMITER must be alphanumeric");
    public static readonly DiagnosticDescriptor CBL1404 = new("CBL1404", DiagnosticSeverity.Error,
        "UNSTRING COUNT must be integer numeric");
    public static readonly DiagnosticDescriptor CBL1405 = new("CBL1405", DiagnosticSeverity.Error,
        "UNSTRING POINTER must be integer numeric");
    public static readonly DiagnosticDescriptor CBL1406 = new("CBL1406", DiagnosticSeverity.Error,
        "UNSTRING TALLYING must be integer numeric");

    // ══════════════════════════════════════
    // CBL1501–1503: INSPECT
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL1501 = new("CBL1501", DiagnosticSeverity.Error,
        "INSPECT target must be alphanumeric or group");
    public static readonly DiagnosticDescriptor CBL1502 = new("CBL1502", DiagnosticSeverity.Error,
        "INSPECT TALLYING target must be integer numeric");
    public static readonly DiagnosticDescriptor CBL1503 = new("CBL1503", DiagnosticSeverity.Error,
        "INSPECT character operand must be alphanumeric");

    // ══════════════════════════════════════
    // CBL1601–1605: START
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL1601 = new("CBL1601", DiagnosticSeverity.Error,
        "START not allowed: file not INDEXED or RELATIVE");
    public static readonly DiagnosticDescriptor CBL1602 = new("CBL1602", DiagnosticSeverity.Error,
        "START KEY must be comparison expression");
    public static readonly DiagnosticDescriptor CBL1603 = new("CBL1603", DiagnosticSeverity.Error,
        "START KEY operand not a record key of file");
    public static readonly DiagnosticDescriptor CBL1604 = new("CBL1604", DiagnosticSeverity.Error,
        "START KEY comparison operands incompatible");
    public static readonly DiagnosticDescriptor CBL1605 = new("CBL1605", DiagnosticSeverity.Error,
        "START requires KEY phrase for file");

    // ══════════════════════════════════════
    // CBL1701–1704: READ
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL1701 = new("CBL1701", DiagnosticSeverity.Error,
        "READ NEXT/PREVIOUS invalid for organization/access");
    public static readonly DiagnosticDescriptor CBL1702 = new("CBL1702", DiagnosticSeverity.Error,
        "READ KEY not allowed on non-indexed file");
    public static readonly DiagnosticDescriptor CBL1703 = new("CBL1703", DiagnosticSeverity.Error,
        "READ KEY not a record/alternate key of file");
    public static readonly DiagnosticDescriptor CBL1704 = new("CBL1704", DiagnosticSeverity.Error,
        "READ INTO target must be alphanumeric or group");

    // ══════════════════════════════════════
    // CBL1801–1803: WRITE
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL1801 = new("CBL1801", DiagnosticSeverity.Error,
        "WRITE FROM source incompatible with record");
    public static readonly DiagnosticDescriptor CBL1802 = new("CBL1802", DiagnosticSeverity.Error,
        "WRITE ADVANCING value must be numeric");
    public static readonly DiagnosticDescriptor CBL1803 = new("CBL1803", DiagnosticSeverity.Error,
        "WRITE ADVANCING item must be integer numeric");

    // ══════════════════════════════════════
    // CBL1901–1902: REWRITE
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL1901 = new("CBL1901", DiagnosticSeverity.Error,
        "REWRITE not allowed for file organization");
    public static readonly DiagnosticDescriptor CBL1902 = new("CBL1902", DiagnosticSeverity.Error,
        "REWRITE FROM source incompatible with record");

    // ══════════════════════════════════════
    // CBL2001: DELETE
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL2001 = new("CBL2001", DiagnosticSeverity.Error,
        "DELETE not allowed for file organization");

    // ══════════════════════════════════════
    // CBL2101–2102: RETURN
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL2101 = new("CBL2101", DiagnosticSeverity.Error,
        "RETURN: file is not sort/merge");
    public static readonly DiagnosticDescriptor CBL2102 = new("CBL2102", DiagnosticSeverity.Error,
        "RETURN INTO target must be alphanumeric or group");

    // ══════════════════════════════════════
    // CBL2201: RELEASE
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL2201 = new("CBL2201", DiagnosticSeverity.Error,
        "RELEASE: not a record for sort/merge file");

    // ══════════════════════════════════════
    // CBL2301–2308: PERFORM
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL2301 = new("CBL2301", DiagnosticSeverity.Error,
        "PERFORM paragraph '{0}' not found");
    public static readonly DiagnosticDescriptor CBL2302 = new("CBL2302", DiagnosticSeverity.Warning,
        "PERFORM THRU out of order: '{0}' does not precede '{1}'");
    public static readonly DiagnosticDescriptor CBL2303 = new("CBL2303", DiagnosticSeverity.Error,
        "PERFORM TIMES must be integer numeric");
    public static readonly DiagnosticDescriptor CBL2304 = new("CBL2304", DiagnosticSeverity.Error,
        "PERFORM UNTIL condition must be boolean");
    public static readonly DiagnosticDescriptor CBL2305 = new("CBL2305", DiagnosticSeverity.Error,
        "PERFORM VARYING control must be integer/index");
    public static readonly DiagnosticDescriptor CBL2306 = new("CBL2306", DiagnosticSeverity.Error,
        "PERFORM VARYING FROM must be numeric");
    public static readonly DiagnosticDescriptor CBL2307 = new("CBL2307", DiagnosticSeverity.Error,
        "PERFORM VARYING BY must be numeric");
    public static readonly DiagnosticDescriptor CBL2308 = new("CBL2308", DiagnosticSeverity.Error,
        "PERFORM VARYING UNTIL must be boolean");

    // ══════════════════════════════════════
    // CBL2401–2402: IF
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL2401 = new("CBL2401", DiagnosticSeverity.Error,
        "IF condition must be boolean");
    public static readonly DiagnosticDescriptor CBL2402 = new("CBL2402", DiagnosticSeverity.Error,
        "Comparison operands incompatible");

    // ══════════════════════════════════════
    // CBL2501–2503: EVALUATE
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL2501 = new("CBL2501", DiagnosticSeverity.Error,
        "EVALUATE WHEN type incompatible with subject");
    public static readonly DiagnosticDescriptor CBL2502 = new("CBL2502", DiagnosticSeverity.Warning,
        "EVALUATE missing WHEN OTHER");
    public static readonly DiagnosticDescriptor CBL2503 = new("CBL2503", DiagnosticSeverity.Error,
        "EVALUATE TRUE WHEN must be boolean");

    // ══════════════════════════════════════
    // CBL2601–2605: Arithmetic enforcement
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL2601 = new("CBL2601", DiagnosticSeverity.Error,
        "Arithmetic operand must be numeric");
    public static readonly DiagnosticDescriptor CBL2602 = new("CBL2602", DiagnosticSeverity.Error,
        "Arithmetic result '{0}' must be numeric");
    public static readonly DiagnosticDescriptor CBL2603 = new("CBL2603", DiagnosticSeverity.Error,
        "ROUNDED item '{0}' must be numeric");
    public static readonly DiagnosticDescriptor CBL2604 = new("CBL2604", DiagnosticSeverity.Error,
        "SIZE ERROR phrase requires a numeric operation");
    public static readonly DiagnosticDescriptor CBL2605 = new("CBL2605", DiagnosticSeverity.Error,
        "DIVIDE remainder '{0}' must be integer numeric");

    // ══════════════════════════════════════
    // CBL3001–3004: Flow analysis
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL3001 = new("CBL3001", DiagnosticSeverity.Warning,
        "Paragraph '{0}' is unreachable");
    public static readonly DiagnosticDescriptor CBL3002 = new("CBL3002", DiagnosticSeverity.Warning,
        "Fall-through from section '{0}' into '{1}'");
    public static readonly DiagnosticDescriptor CBL3003 = new("CBL3003", DiagnosticSeverity.Warning,
        "Paragraph '{0}' must terminate with EXIT");
    public static readonly DiagnosticDescriptor CBL3004 = new("CBL3004", DiagnosticSeverity.Warning,
        "PERFORM cycle: '{0}' -> '{1}'");

    // ══════════════════════════════════════
    // CBL3101–3111: Scope & symbols
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL3101 = new("CBL3101", DiagnosticSeverity.Error,
        "Duplicate data-name '{0}'");
    public static readonly DiagnosticDescriptor CBL3102 = new("CBL3102", DiagnosticSeverity.Error,
        "Duplicate condition-name '{0}' in '{1}'");
    public static readonly DiagnosticDescriptor CBL3103 = new("CBL3103", DiagnosticSeverity.Error,
        "Duplicate section name '{0}'");
    public static readonly DiagnosticDescriptor CBL3104 = new("CBL3104", DiagnosticSeverity.Error,
        "Duplicate paragraph name '{0}'");
    public static readonly DiagnosticDescriptor CBL3105 = new("CBL3105", DiagnosticSeverity.Error,
        "GLOBAL not allowed in this context");
    public static readonly DiagnosticDescriptor CBL3106 = new("CBL3106", DiagnosticSeverity.Warning,
        "LOCAL '{0}' shadows GLOBAL '{1}'");
    public static readonly DiagnosticDescriptor CBL3107 = new("CBL3107", DiagnosticSeverity.Error,
        "Name '{0}' conflicts with symbol '{1}'");
    public static readonly DiagnosticDescriptor CBL3108 = new("CBL3108", DiagnosticSeverity.Error,
        "USING parameter '{0}' not in LINKAGE SECTION");
    public static readonly DiagnosticDescriptor CBL3109 = new("CBL3109", DiagnosticSeverity.Error,
        "RETURNING item '{0}' not in LINKAGE SECTION");
    public static readonly DiagnosticDescriptor CBL3110 = new("CBL3110", DiagnosticSeverity.Error,
        "VALUE not allowed in LINKAGE item '{0}'");
    public static readonly DiagnosticDescriptor CBL3111 = new("CBL3111", DiagnosticSeverity.Error,
        "REDEFINES not allowed for LINKAGE item '{0}'");

    // ══════════════════════════════════════
    // CBL3201–3206: File status
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL3201 = new("CBL3201", DiagnosticSeverity.Error,
        "FILE STATUS must be a data-name");
    public static readonly DiagnosticDescriptor CBL3202 = new("CBL3202", DiagnosticSeverity.Error,
        "FILE STATUS must be alphanumeric length >= 2");
    public static readonly DiagnosticDescriptor CBL3203 = new("CBL3203", DiagnosticSeverity.Error,
        "FILE STATUS cannot be group item");
    public static readonly DiagnosticDescriptor CBL3204 = new("CBL3204", DiagnosticSeverity.Error,
        "FILE STATUS cannot be REDEFINES/RENAMES");
    public static readonly DiagnosticDescriptor CBL3205 = new("CBL3205", DiagnosticSeverity.Error,
        "File has more than one FILE STATUS");
    public static readonly DiagnosticDescriptor CBL3206 = new("CBL3206", DiagnosticSeverity.Warning,
        "FILE STATUS not checked before next I/O");

    // ══════════════════════════════════════
    // CBL3301–3310: CALL / USING / RETURNING
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL3301 = new("CBL3301", DiagnosticSeverity.Error,
        "CALL argument count mismatch");
    public static readonly DiagnosticDescriptor CBL3302 = new("CBL3302", DiagnosticSeverity.Error,
        "Argument not valid for parameter mode");
    public static readonly DiagnosticDescriptor CBL3303 = new("CBL3303", DiagnosticSeverity.Error,
        "Argument type incompatible with parameter");
    public static readonly DiagnosticDescriptor CBL3304 = new("CBL3304", DiagnosticSeverity.Error,
        "RETURNING item not in LINKAGE SECTION");
    public static readonly DiagnosticDescriptor CBL3305 = new("CBL3305", DiagnosticSeverity.Error,
        "CALL RETURNING type incompatible");
    public static readonly DiagnosticDescriptor CBL3310 = new("CBL3310", DiagnosticSeverity.Warning,
        "Dynamic CALL: parameter list cannot be validated");

    // ══════════════════════════════════════
    // CBL3401–3406: Report Writer
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL3401 = new("CBL3401", DiagnosticSeverity.Error,
        "Unknown REPORT TYPE");
    public static readonly DiagnosticDescriptor CBL3402 = new("CBL3402", DiagnosticSeverity.Error,
        "REPORT group missing TYPE");
    public static readonly DiagnosticDescriptor CBL3403 = new("CBL3403", DiagnosticSeverity.Error,
        "SUM source not numeric");
    public static readonly DiagnosticDescriptor CBL3404 = new("CBL3404", DiagnosticSeverity.Error,
        "CONTROL must be data-name");
    public static readonly DiagnosticDescriptor CBL3405 = new("CBL3405", DiagnosticSeverity.Error,
        "CONTROL item not defined");
    public static readonly DiagnosticDescriptor CBL3406 = new("CBL3406", DiagnosticSeverity.Warning,
        "SUM item never referenced");

    // ══════════════════════════════════════
    // CBL3501–3502: Strict COBOL-85 mode
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL3501 = new("CBL3501", DiagnosticSeverity.Error,
        "Feature not allowed in strict COBOL-85 mode");
    public static readonly DiagnosticDescriptor CBL3502 = new("CBL3502", DiagnosticSeverity.Warning,
        "Feature not part of COBOL-85");

    // ══════════════════════════════════════
    // Data item classification (Phase 1.1)
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL0801 = new("CBL0801", DiagnosticSeverity.Error,
        "OCCURS not allowed on level 01 or 77 item '{0}'");
    public static readonly DiagnosticDescriptor CBL0802 = new("CBL0802", DiagnosticSeverity.Error,
        "BLANK WHEN ZERO only allowed on numeric DISPLAY item '{0}'");
    public static readonly DiagnosticDescriptor CBL0803 = new("CBL0803", DiagnosticSeverity.Error,
        "JUSTIFIED only allowed on alphanumeric elementary item '{0}'");
}
