// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Frozen;

namespace WiseOwl.Cobol.Runtime.IO;

/// <summary>⛔ THE I-O STATUS A COBOL-85 PROGRAM IS TOLD (kb/Work PB5882; docs/FILES_DESIGN.md D33; the conformance
/// statement's section 3.13). The connectors keep their CONDITION in the runtime's one vocabulary,
/// <see cref="FileStatusCode"/>; at <c>--std 85</c> <see cref="FileConnector.ProgramStatus"/> reports it through
/// <see cref="Report"/>, so the FILE STATUS item never receives a value the 1985 text does not list. 1985 gives, for each
/// I-O module, "a list of the values placed in the I-O status for the previously named conditions" (§1985:VII.1.3.5,
/// §1985:VIII.1.3.4, §1985:IX.1.3.4), and makes a condition the implementor adds an "Implementor-Defined Condition With
/// Unsuccessful Completion", 9x, which "must not duplicate any condition specified by the I-O status values 00 through 49",
/// the value of x the implementor's (§1985:C.2 item 48).
/// <para><b>The order of the projection</b> (<see cref="Report"/>): a condition no 1985 module names has its own 9x
/// (<see cref="OwnValues"/>, one per condition); a condition 1985 names under another value is reported by that value
/// (also <see cref="OwnValues"/>); a value the file's own module lists is itself; a '0x' or '3x' value 1985 lists only for
/// another module is the file's module's "no further information" value of that class, '00' or '30'; a '1x' or '2x'
/// value 1985 lists only for another module is the condition of a key only that module's file has, so it is the 1985 text's
/// own value.</para>
/// <para><b>Every projection keeps the value's class and its criticality</b> (<c>IoStatus1985Tests</c> pins both): the
/// runtime's returned statuses steer the AT END and INVALID KEY phrases by class, the program's status steers the hooks,
/// and the two must agree.</para></summary>
internal static class IoStatus1985
{
    /// <summary>§1985:VII.1.3.5 (Sequential I-O), its 9x aside.</summary>
    private static readonly FrozenSet<string> Sequential = FrozenSet.Create(StringComparer.Ordinal,
        "00", "04", "05", "07", "10", "30", "34", "35", "37", "38", "39", "41", "42", "43", "44", "46", "47", "48", "49");

    /// <summary>§1985:VIII.1.3.4 (Relative I-O).</summary>
    private static readonly FrozenSet<string> Relative = FrozenSet.Create(StringComparer.Ordinal,
        "00", "04", "05", "10", "14", "22", "23", "24", "30", "35", "37", "38", "39", "41", "42", "43", "44", "46", "47", "48", "49");

    /// <summary>§1985:IX.1.3.4 (Indexed I-O).</summary>
    private static readonly FrozenSet<string> Indexed = FrozenSet.Create(StringComparer.Ordinal,
        "00", "02", "04", "05", "10", "21", "22", "23", "24", "30", "35", "37", "38", "39", "41", "42", "43", "44", "46", "47", "48", "49");

    /// <summary>⛔ THE 9x VALUES THIS IMPLEMENTATION DEFINES (§1985:C.2 item 48), each with the condition it names and
    /// whether it is critical — §1985:VII.1.3.5: "any that begin with the digit 9 that the implementor defines as critical".
    /// A 9x is critical exactly when the condition it reports is fatal at the later editions (a '4x' or '7x'), so the
    /// projection keeps the criticality of every value. One value per condition; '90' and '91' are the runtime's own
    /// (<see cref="FileStatusCode.LinageValueViolation"/>, <see cref="FileStatusCode.CharacterWithoutByteImage"/>).</summary>
    internal static readonly FrozenDictionary<string, bool> Defined9x = new Dictionary<string, bool>(StringComparer.Ordinal)
    {
        [FileStatusCode.LinageValueViolation] = true,           // '90' the LINAGE value rules (DOC-A.1-110)
        [FileStatusCode.CharacterWithoutByteImage] = true,      // '91' a character with no byte image (DOC-A.1-110)
        ["92"] = true,    // '45' the record identification failure (FORMAT / SELECT WHEN)
        ["93"] = false,   // '51' a record locked by another file connector
        ["94"] = false,   // '52' the implementor-detected deadlock
        ["95"] = false,   // '53' the run unit's record lock limit
        ["96"] = false,   // '54' the file connector's record lock limit
        ["97"] = false,   // '61' an OPEN's file sharing conflict
        ["98"] = false,   // '62' a DELETE FILE's file sharing conflict
        ["99"] = true,    // '71' a line sequential WRITE or REWRITE whose record holds a character outside the line set
    }.ToFrozenDictionary(StringComparer.Ordinal);

    /// <summary>⛔ THE CONDITIONS NO 1985 MODULE LISTS UNDER THEIR OWN VALUE, and the 1985 value each is reported by. A
    /// condition 1985 names under another value takes that value, because a 9x "must not duplicate" it: '06' is the record
    /// length conflict of §1985:VII.4.4.4 GR13 ("the READ statement is successful and an I-O status is set indicating a
    /// record length conflict has occurred", '04'); '09' is a successful READ of which 1985 says no more ("successfully
    /// executed and no further information is available", '00'); '31' is a permanent error of which it says no more ('30').
    /// Every other condition here has its own 9x of <see cref="Defined9x"/>. A <see cref="FileStatusCode"/> constant that
    /// is neither listed by a 1985 module nor here fails <c>IoStatus1985Tests</c>.</summary>
    internal static readonly FrozenDictionary<string, string> OwnValues = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [FileStatusCode.LineRecordTooLong] = FileStatusCode.RecordLengthShortLong,   // '06' → '04'
        [FileStatusCode.LineRecordInvalidCharRead] = FileStatusCode.Success,         // '09' → '00'
        [FileStatusCode.AssignNotConsistent] = FileStatusCode.PermanentError,        // '31' → '30'
        [FileStatusCode.RecordIdentificationFailure] = "92",                         // '45'
        [FileStatusCode.RecordLocked] = "93",                                        // '51'
        [FileStatusCode.Deadlock] = "94",                                            // '52'
        [FileStatusCode.RunUnitLockLimit] = "95",                                    // '53'
        [FileStatusCode.ConnectorLockLimit] = "96",                                  // '54'
        [FileStatusCode.FileSharingConflict] = "97",                                 // '61'
        [FileStatusCode.DeleteFileSharing] = "98",                                   // '62'
        [FileStatusCode.LineRecordInvalidChar] = "99",                               // '71'
    }.ToFrozenDictionary(StringComparer.Ordinal);

    /// <summary>The values the 1985 module of <paramref name="organization"/> (a <see cref="FixedFileAttributes"/>
    /// organization name) lists, its 9x aside.</summary>
    internal static FrozenSet<string> ModuleList(string organization) => organization switch
    {
        FixedFileAttributes.Sequential => Sequential,
        FixedFileAttributes.Relative => Relative,
        FixedFileAttributes.Indexed => Indexed,
        _ => throw new ArgumentOutOfRangeException(nameof(organization), organization, "not one of the three organizations"),
    };

    /// <summary>The I-O status a COBOL-85 program is told for the runtime's <paramref name="condition"/> on a file of
    /// <paramref name="organization"/>: see the class summary for the order.</summary>
    internal static string Report(string condition, string organization)
    {
        if (OwnValues.TryGetValue(condition, out var own)) return own;
        if (Defined9x.ContainsKey(condition) || ModuleList(organization).Contains(condition)) return condition;
        return condition[0] switch
        {
            '0' => FileStatusCode.Success,          // the module's "no further information" successful completion
            '3' => FileStatusCode.PermanentError,   // the module's "no further information" permanent error
            _ => condition,                         // a key condition 1985 lists for the module whose file has the key
        };
    }

    /// <summary>Whether the 9x value <paramref name="status"/> is critical (§1985:VII.1.3.5); a 9x this implementation does
    /// not define is critical, the conservative answer for a value no program of it can be told.</summary>
    internal static bool IsCritical(string status) => !Defined9x.TryGetValue(status, out bool critical) || critical;
}
