// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding;

/// <summary>
/// ⛔ <b>THE IMPLEMENTOR-NAMES OF THE RECORD DELIMITER CLAUSE, AND THE METHOD EACH SELECTS — ONE TABLE</b> (kb/Work PB5844).
/// The clause's operand is "STANDARD-1" or a name the implementor defines: §1985:VII.2.8.4 GR3 makes the method "that
/// associated with implementor-name-1 by the implementor", and §2023:12.4.5.11.3 SR2 says "The implementor shall specify
/// the names available for use as feature-name-1" (§2023:12.4.5.11.4 GR4 then reads the method off the name as 1985 does).
/// A word this table does not hold is no implementor-name and is refused (COBOL7598): accepting it would accept source the
/// general format does not print.
/// <para>THE NAMES ARE GNUCOBOL'S (CLAUDE.md rule 1's precedence: the standard leaves the names to the implementor, so
/// GnuCOBOL decides, and its <c>cobc/parser.y</c> rule <c>record_delimiter_option</c> defines these two with these
/// meanings). Each names one of the two record-delimiting methods this implementation has (docs/conformance/7/DOC-A.1-151):
/// <list type="bullet">
/// <item><c>BINARY-SEQUENTIAL</c> — the record sequential file's method, each record framed by its 4-byte length prefix.
/// It is the method a sequential file without the clause has already, so naming it changes nothing.</item>
/// <item><c>LINE-SEQUENTIAL</c> — the line sequential file's method, each record ended by the line delimiter. Naming it
/// makes the file a LINE SEQUENTIAL file, at every edition (<see cref="FileModel.LineSequentialByDelimiter"/>).</item>
/// </list>
/// GnuCOBOL's fourth arm, a warning "not recognized; will be ignored" for any other word, is a leniency and not an
/// implementor-name, so it is not taken. Neither name is a reserved word: each is matched here as the text of the
/// clause's <c>cobolWord</c>, so a program may still declare a data-name or a paragraph spelled BINARY-SEQUENTIAL. Each is an
/// implementor-name, so the table is a source of <see cref="ImplementorNameRegistry"/> (kb/Work PB5884).</para>
/// </summary>
internal static class RecordDelimiterNames
{
    /// <summary>Each implementor-name and the sequential organization whose record-delimiting method it names.</summary>
    private static readonly Dictionary<string, FileOrganization> Methods = new(StringComparer.Ordinal)
    {
        ["BINARY-SEQUENTIAL"] = FileOrganization.Sequential,
        ["LINE-SEQUENTIAL"] = FileOrganization.LineSequential,
    };

    /// <summary>The names, in the order a diagnostic lists them.</summary>
    public static IReadOnlyList<string> Names { get; } = [.. Methods.Keys.Order(StringComparer.Ordinal)];

    /// <summary>Whether <paramref name="name"/> (upper-folded) is an implementor-name of the clause.</summary>
    public static bool IsName(string name) => Methods.ContainsKey(name);

    /// <summary>The organization whose method <paramref name="name"/> (upper-folded) names, or false when it is no
    /// implementor-name of the clause.</summary>
    public static bool TryMethod(string name, out FileOrganization organization) => Methods.TryGetValue(name, out organization);
}
