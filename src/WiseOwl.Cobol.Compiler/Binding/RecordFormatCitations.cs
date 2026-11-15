// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Binding;

/// <summary>
/// ⛔ THE RULES OF THE FORMAT AND SELECT WHEN CLAUSES, AS THE EDITION BEING COMPILED NUMBERS THEM. The clauses entered the standard in
/// ISO/IEC 1989:2002 (§2002:13.16.22 and §2002:13.16.49), kept their rules through the 2014 and 2023 editions, and were renumbered
/// twice: 2002 prints a clause's syntax rules at <c>.2</c> and its general rules at <c>.3</c>, 2014 the same under 13.18, and 2023
/// inserts a general subclause, so its syntax rules are <c>.3</c> and its general rules <c>.4</c>. A diagnostic cites the number in
/// force for the compile (each edition stands on its own text).
/// </summary>
internal static class RecordFormatCitations
{
    /// <summary>A syntax rule of the FORMAT clause, <paramref name="rule"/> like <c>SR1</c>.</summary>
    public static string Format(int dialectLevel, string rule) => dialectLevel switch
    {
        < 2014 => $"ISO §2002:13.16.22.2 {rule}",
        < 2023 => $"ISO §2014:13.18.24.2 {rule}",
        _ => $"ISO §2023:13.18.24.3 {rule}",
    };

    /// <summary>A general rule of the FORMAT clause, <paramref name="rule"/> like <c>GR4</c>.</summary>
    public static string FormatGeneral(int dialectLevel, string rule) => dialectLevel switch
    {
        < 2014 => $"ISO §2002:13.16.22.3 {rule}",
        < 2023 => $"ISO §2014:13.18.24.3 {rule}",
        _ => $"ISO §2023:13.18.24.4 {rule}",
    };

    /// <summary>A syntax rule of the SELECT WHEN clause, <paramref name="rule"/> like <c>SR3</c>.</summary>
    public static string SelectWhen(int dialectLevel, string rule) => dialectLevel switch
    {
        < 2014 => $"ISO §2002:13.16.49.2 {rule}",
        < 2023 => $"ISO §2014:13.18.51.2 {rule}",
        _ => $"ISO §2023:13.18.51.3 {rule}",
    };

    /// <summary>The file description entry's rule "If the FORMAT clause is specified, variable-length records shall be specified for the
    /// file."</summary>
    public static string VariableLength(int dialectLevel) => dialectLevel switch
    {
        < 2014 => "ISO §2002:13.3.4.2 SR5",
        < 2023 => "ISO §2014:13.4.4.2 SR5",
        _ => "ISO §2023:13.4.5.3 SR6",
    };

    /// <summary>The REDEFINES clause's rule over the entries of a FORMAT file (SR3: not in a level 1 entry of the file section, nor in
    /// any entry subordinate to a file description entry that contains a FORMAT clause).</summary>
    public static string Redefines(int dialectLevel) => dialectLevel switch
    {
        < 2014 => "ISO §2002:13.16.42.2 SR3",
        < 2023 => "ISO §2014:13.18.44.2 SR3",
        _ => "ISO §2023:13.18.44.3 SR3",
    };
}
