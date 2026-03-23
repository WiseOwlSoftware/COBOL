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
    public static readonly DiagnosticDescriptor CBL1204 = new("CBL1204", DiagnosticSeverity.Warning,
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
    public static readonly DiagnosticDescriptor CBL3112 = new("CBL3112", DiagnosticSeverity.Error,
        "REDEFINES level mismatch: '{0}' is level {1}, target '{2}' is level {3}");
    public static readonly DiagnosticDescriptor CBL3113 = new("CBL3113", DiagnosticSeverity.Error,
        "Cannot REDEFINES special-level item '{0}' (level {1})");
    public static readonly DiagnosticDescriptor CBL3114 = new("CBL3114", DiagnosticSeverity.Error,
        "REDEFINES target '{0}' is subordinate to OCCURS item '{1}'");

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
    // CBL0601–0602: SELECT/FD consistency
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL0601 = new("CBL0601", DiagnosticSeverity.Warning,
        "FD '{0}' has no matching SELECT in FILE-CONTROL");

    // ══════════════════════════════════════
    // CBL0701: OPEN enforcement
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL0701 = new("CBL0701", DiagnosticSeverity.Error,
        "OPEN EXTEND not allowed on non-sequential file '{0}'");

    // ══════════════════════════════════════
    // Data item classification (Phase 1.1)
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL0801 = new("CBL0801", DiagnosticSeverity.Error,
        "OCCURS not allowed on level 01 or 77 item '{0}'");
    public static readonly DiagnosticDescriptor CBL0802 = new("CBL0802", DiagnosticSeverity.Error,
        "BLANK WHEN ZERO only allowed on numeric DISPLAY item '{0}'");
    public static readonly DiagnosticDescriptor CBL0803 = new("CBL0803", DiagnosticSeverity.Error,
        "JUSTIFIED only allowed on alphanumeric elementary item '{0}'");

    // ══════════════════════════════════════
    // CBL0810–0812: RENAMES (level 66) validation
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL0810 = new("CBL0810", DiagnosticSeverity.Error,
        "RENAMES FROM target '{0}' not found in item '{1}'");
    public static readonly DiagnosticDescriptor CBL0811 = new("CBL0811", DiagnosticSeverity.Error,
        "RENAMES THRU target '{0}' not found in item '{1}'");
    public static readonly DiagnosticDescriptor CBL0812 = new("CBL0812", DiagnosticSeverity.Error,
        "RENAMES cannot reference level-66 or level-88 item '{0}'");

    // ══════════════════════════════════════
    // COBOL0001: Generic parse error
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor COBOL0001 = new("COBOL0001", DiagnosticSeverity.Error,
        "{0}");

    // ══════════════════════════════════════
    // COBOL0100–0109: Parser — unsupported feature warnings
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor COBOL0100 = new("COBOL0100", DiagnosticSeverity.Warning,
        "ASCENDING/DESCENDING KEY clause in OCCURS is not yet supported. Table created without sort key.");
    public static readonly DiagnosticDescriptor COBOL0101 = new("COBOL0101", DiagnosticSeverity.Warning,
        "BLANK WHEN ZERO may not be recognized. Check that it appears as a single clause on the data item.");
    public static readonly DiagnosticDescriptor COBOL0102 = new("COBOL0102", DiagnosticSeverity.Warning,
        "This SET form may not be supported. Supported forms: SET identifier TO value, SET condition TO TRUE/FALSE, SET index UP/DOWN BY integer.");
    public static readonly DiagnosticDescriptor COBOL0103 = new("COBOL0103", DiagnosticSeverity.Warning,
        "SEARCH statement may not be fully supported.");
    public static readonly DiagnosticDescriptor COBOL0104 = new("COBOL0104", DiagnosticSeverity.Warning,
        "OCCURS DEPENDING ON (variable-length tables) is not yet supported.");
    public static readonly DiagnosticDescriptor COBOL0105 = new("COBOL0105", DiagnosticSeverity.Warning,
        "INSPECT CONVERTING is not yet supported.");
    public static readonly DiagnosticDescriptor COBOL0106 = new("COBOL0106", DiagnosticSeverity.Warning,
        "INITIALIZE REPLACING is not yet supported.");
    public static readonly DiagnosticDescriptor COBOL0107 = new("COBOL0107", DiagnosticSeverity.Warning,
        "EVALUATE with ALSO (multi-subject) may not be fully supported.");
    public static readonly DiagnosticDescriptor COBOL0108 = new("COBOL0108", DiagnosticSeverity.Warning,
        "Multi-target SET (SET id1 id2 TO value) is not yet supported. Use separate SET statements for each target.");
    public static readonly DiagnosticDescriptor COBOL0109 = new("COBOL0109", DiagnosticSeverity.Warning,
        "PERFORM VARYING with AFTER clause (nested varying) is not yet supported.");
    public static readonly DiagnosticDescriptor COBOL0110 = new("COBOL0110", DiagnosticSeverity.Warning,
        "Statement not recognized or not yet implemented: '{0}'.");

    // ══════════════════════════════════════
    // COBOL0200–0201: Parser — reserved word conflicts
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor COBOL0200 = new("COBOL0200", DiagnosticSeverity.Warning,
        "STATUS is a reserved word here. For file status, use 'FILE STATUS IS <data-name>'.");
    public static readonly DiagnosticDescriptor COBOL0201 = new("COBOL0201", DiagnosticSeverity.Warning,
        "PROGRAM is a reserved word. If this is a paragraph name, it cannot be named PROGRAM.");

    // ══════════════════════════════════════
    // COBOL0300–0312: Parser — syntax guidance
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor COBOL0300 = new("COBOL0300", DiagnosticSeverity.Warning,
        "THROUGH/THRU is not recognized in this context. Check PERFORM or VALUE THROUGH syntax.");
    public static readonly DiagnosticDescriptor COBOL0301 = new("COBOL0301", DiagnosticSeverity.Warning,
        "Missing space before string literal.");
    public static readonly DiagnosticDescriptor COBOL0302 = new("COBOL0302", DiagnosticSeverity.Warning,
        "Missing space after string literal.");
    public static readonly DiagnosticDescriptor COBOL0303 = new("COBOL0303", DiagnosticSeverity.Warning,
        "In a MOVE statement, did you forget TO before the target?");
    public static readonly DiagnosticDescriptor COBOL0304 = new("COBOL0304", DiagnosticSeverity.Warning,
        "Missing period after paragraph name — the parser is treating it as a qualified reference.");
    public static readonly DiagnosticDescriptor COBOL0305 = new("COBOL0305", DiagnosticSeverity.Warning,
        "Unexpected token in SPECIAL-NAMES. Check implementor-name or mnemonic-name syntax.");
    public static readonly DiagnosticDescriptor COBOL0306 = new("COBOL0306", DiagnosticSeverity.Warning,
        "{0} appears without a matching {1} statement.");
    public static readonly DiagnosticDescriptor COBOL0307 = new("COBOL0307", DiagnosticSeverity.Warning,
        "A period may be missing at the end of the previous sentence.");
    public static readonly DiagnosticDescriptor COBOL0308 = new("COBOL0308", DiagnosticSeverity.Warning,
        "A data-name is expected here, not a literal.");
    public static readonly DiagnosticDescriptor COBOL0309 = new("COBOL0309", DiagnosticSeverity.Warning,
        "A literal value is expected here, not a data-name.");
    public static readonly DiagnosticDescriptor COBOL0310 = new("COBOL0310", DiagnosticSeverity.Warning,
        "Missing BY keyword. INDEXED BY requires 'INDEXED BY <index-name>'.");
    public static readonly DiagnosticDescriptor COBOL0311 = new("COBOL0311", DiagnosticSeverity.Warning,
        "NOT {0} (abbreviated condition) is not yet supported. Use the word form instead.");
    public static readonly DiagnosticDescriptor COBOL0312 = new("COBOL0312", DiagnosticSeverity.Warning,
        "Unexpected token in FILE-CONTROL paragraph. Check SELECT/ASSIGN TO syntax.");

    // ══════════════════════════════════════
    // COBOL0400–0412: Bound tree builder
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor COBOL0400 = new("COBOL0400", DiagnosticSeverity.Warning,
        "Procedure name '{0}' is used as both a section and a paragraph; resolving as paragraph.");
    public static readonly DiagnosticDescriptor COBOL0401 = new("COBOL0401", DiagnosticSeverity.Warning,
        "Section '{0}' contains no paragraphs.");
    public static readonly DiagnosticDescriptor COBOL0402 = new("COBOL0402", DiagnosticSeverity.Error,
        "Paragraph or section '{0}' not found. Check spelling or verify it is defined in the PROCEDURE DIVISION.");
    public static readonly DiagnosticDescriptor COBOL0403 = new("COBOL0403", DiagnosticSeverity.Error,
        "{0} CORRESPONDING: '{1}' must be a group item.");
    public static readonly DiagnosticDescriptor COBOL0404 = new("COBOL0404", DiagnosticSeverity.Error,
        "PERFORM VARYING index '{0}' must not be subscripted.");
    public static readonly DiagnosticDescriptor COBOL0405 = new("COBOL0405", DiagnosticSeverity.Error,
        "Item '{0}' is not defined with OCCURS and cannot be subscripted.");
    public static readonly DiagnosticDescriptor COBOL0406 = new("COBOL0406", DiagnosticSeverity.Error,
        "Item '{0}' has {1} OCCURS level(s) but was referenced with {2} subscript(s).");
    public static readonly DiagnosticDescriptor COBOL0407 = new("COBOL0407", DiagnosticSeverity.Error,
        "Item '{0}' exceeds the COBOL-85 limit of 3 OCCURS levels (found {1}).");
    public static readonly DiagnosticDescriptor COBOL0408 = new("COBOL0408", DiagnosticSeverity.Error,
        "A maximum of 3 subscripts is permitted in COBOL-85; found {0}.");
    public static readonly DiagnosticDescriptor COBOL0409 = new("COBOL0409", DiagnosticSeverity.Error,
        "Item '{0}' requires {1} subscript(s) but was referenced with {2}.");
    public static readonly DiagnosticDescriptor COBOL0410 = new("COBOL0410", DiagnosticSeverity.Warning,
        "{0} CORRESPONDING: field '{1}' is ambiguous in target group '{2}'.");
    public static readonly DiagnosticDescriptor COBOL0411 = new("COBOL0411", DiagnosticSeverity.Error,
        "{0} CORRESPONDING: '{1}' and '{2}' have incompatible OCCURS clauses.");
    public static readonly DiagnosticDescriptor COBOL0412 = new("COBOL0412", DiagnosticSeverity.Warning,
        "{0} CORRESPONDING: no matching elementary items between '{1}' and '{2}'.");

    // ══════════════════════════════════════
    // COBOL0500–0513: Binder (IR lowering)
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor COBOL0500 = new("COBOL0500", DiagnosticSeverity.Error,
        "PERFORM VARYING index '{0}' has no storage location.");
    public static readonly DiagnosticDescriptor COBOL0501 = new("COBOL0501", DiagnosticSeverity.Error,
        "PERFORM target paragraph '{0}' not found in paragraph dispatch table.");
    public static readonly DiagnosticDescriptor COBOL0502 = new("COBOL0502", DiagnosticSeverity.Error,
        "PERFORM TIMES has no target paragraph and no inline statements.");
    public static readonly DiagnosticDescriptor COBOL0503 = new("COBOL0503", DiagnosticSeverity.Error,
        "Unsupported condition shape: {0}");
    public static readonly DiagnosticDescriptor COBOL0504 = new("COBOL0504", DiagnosticSeverity.Error,
        "Cannot normalize comparison operands: left={0}, right={1}");
    public static readonly DiagnosticDescriptor COBOL0505 = new("COBOL0505", DiagnosticSeverity.Error,
        "Unhandled comparison combination: {0} vs {1}");
    public static readonly DiagnosticDescriptor COBOL0506 = new("COBOL0506", DiagnosticSeverity.Error,
        "GO TO target '{0}' not found in paragraph dispatch table.");
    public static readonly DiagnosticDescriptor COBOL0507 = new("COBOL0507", DiagnosticSeverity.Error,
        "GO TO DEPENDING ON requires a selector variable.");
    public static readonly DiagnosticDescriptor COBOL0508 = new("COBOL0508", DiagnosticSeverity.Error,
        "GO TO DEPENDING ON selector '{0}' has no storage location.");
    public static readonly DiagnosticDescriptor COBOL0509 = new("COBOL0509", DiagnosticSeverity.Error,
        "EXIT {0} used outside of any active {1}.");
    public static readonly DiagnosticDescriptor COBOL0510 = new("COBOL0510", DiagnosticSeverity.Error,
        "SET target '{0}' has no storage location.");
    public static readonly DiagnosticDescriptor COBOL0511 = new("COBOL0511", DiagnosticSeverity.Error,
        "SET '{0}' TO: cannot resolve value expression ({1}).");
    public static readonly DiagnosticDescriptor COBOL0512 = new("COBOL0512", DiagnosticSeverity.Error,
        "SET '{0}' UP BY: cannot resolve delta expression ({1}).");
    public static readonly DiagnosticDescriptor COBOL0513 = new("COBOL0513", DiagnosticSeverity.Error,
        "SET '{0}' DOWN BY: cannot resolve delta expression ({1}).");

    // ══════════════════════════════════════
    // COBOL0600: Internal compiler error
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor COBOL0600 = new("COBOL0600", DiagnosticSeverity.Error,
        "Internal compiler error while generating code for '{0}': {1}. Please report this.");

    // ══════════════════════════════════════
    // CBL3601–3606: ALTER and bare GO TO (obsolete features)
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL3601 = new("CBL3601", DiagnosticSeverity.Error,
        "ALTER statement deleted from COBOL standard in 2002; not allowed in {0} mode");
    public static readonly DiagnosticDescriptor CBL3602 = new("CBL3602", DiagnosticSeverity.Warning,
        "ALTER statement is obsolete; removed from COBOL-2002 and later standards");
    public static readonly DiagnosticDescriptor CBL3603 = new("CBL3603", DiagnosticSeverity.Error,
        "ALTER target '{0}' is not a paragraph name");
    public static readonly DiagnosticDescriptor CBL3604 = new("CBL3604", DiagnosticSeverity.Error,
        "ALTER target paragraph '{0}' does not contain a GO TO statement");
    public static readonly DiagnosticDescriptor CBL3605 = new("CBL3605", DiagnosticSeverity.Error,
        "Bare GO TO (without target) deleted from COBOL standard in 2002; not allowed in {0} mode");
    public static readonly DiagnosticDescriptor CBL3606 = new("CBL3606", DiagnosticSeverity.Warning,
        "Bare GO TO (without target) is obsolete; removed from COBOL-2002 and later standards");
}
