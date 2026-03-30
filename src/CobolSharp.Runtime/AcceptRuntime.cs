// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Text;

namespace CobolSharp.Runtime;

/// <summary>
/// Runtime helpers for ACCEPT statement (Format 1: from input device, Format 2: from system source).
/// Format 1 (AcceptSourceKind.None): reads from stdin into the target field.
/// Format 2: formats current date/time into the target field's byte storage.
/// </summary>
public static class AcceptRuntime
{
    /// <summary>
    /// ACCEPT identifier FROM source.
    /// For AcceptSourceKind.None (plain ACCEPT or ACCEPT FROM mnemonic-name),
    /// reads from stdin. For system sources (DATE, TIME, etc.),
    /// formats the current date/time value.
    /// The result is written left-justified into the target storage area, space-padded on the right.
    /// </summary>
    /// <param name="area">Backing byte array.</param>
    /// <param name="offset">Field offset within the array.</param>
    /// <param name="length">Field length in bytes.</param>
    /// <param name="sourceKind">The ACCEPT source kind.</param>
    public static void Accept(byte[] area, int offset, int length, AcceptSourceKind sourceKind)
    {
        string text = sourceKind switch
        {
            AcceptSourceKind.Date => FormatDate(DateTime.Now),
            AcceptSourceKind.DateYYYYMMDD => FormatDateYYYYMMDD(DateTime.Now),
            AcceptSourceKind.Time => FormatTime(DateTime.Now, length),
            AcceptSourceKind.Day => FormatDay(DateTime.Now),
            AcceptSourceKind.DayYYYYDDD => FormatDayYYYYDDD(DateTime.Now),
            AcceptSourceKind.DayOfWeek => FormatDayOfWeek(DateTime.Now, length),
            _ => ReadFromConsole(length)
        };

        byte[] bytes = Encoding.ASCII.GetBytes(text);
        int copyLen = Math.Min(bytes.Length, length);
        Array.Copy(bytes, 0, area, offset, copyLen);

        // Pad remainder with spaces
        for (int i = offset + copyLen; i < offset + length; i++)
            area[i] = (byte)' ';
    }

    /// <summary>
    /// Reads from stdin for ACCEPT Format 1 (plain ACCEPT or ACCEPT FROM mnemonic-name).
    /// Per COBOL-85 spec section 6.5.4:
    /// - Data is read from the input device and moved to the receiving field
    ///   as if by an alphanumeric MOVE (left-justified, space-padded or truncated).
    /// - For record-oriented input, successive records are read until the field is filled.
    ///   Each record fills up to 80 characters (the standard card image size).
    /// Returns spaces at end of input.
    /// </summary>
    private static string ReadFromConsole(int length)
    {
        const int RecordSize = 80;
        var result = new StringBuilder(length);

        while (result.Length < length)
        {
            string? line = Console.ReadLine();
            if (line == null)
                break;

            // Pad short lines to record size (80-column card image semantics)
            if (line.Length < RecordSize)
                line = line.PadRight(RecordSize);

            result.Append(line);

            // If the field fits in one record, don't read more
            if (length <= RecordSize)
                break;
        }

        return result.ToString();
    }

    /// <summary>
    /// DATE (no qualifier): YYMMDD — always 6 digits per COBOL-85 spec.
    /// </summary>
    private static string FormatDate(DateTime dt)
    {
        return dt.ToString("yyMMdd", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// DATE YYYYMMDD: YYYYMMDD — always 8 digits.
    /// </summary>
    private static string FormatDateYYYYMMDD(DateTime dt)
    {
        return dt.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// TIME: HHMMSScc (hours, minutes, seconds, centiseconds), truncated to field length.
    /// </summary>
    private static string FormatTime(DateTime dt, int length)
    {
        string baseStr = dt.ToString("HHmmssff", CultureInfo.InvariantCulture);
        return baseStr.Length > length ? baseStr[..length] : baseStr;
    }

    /// <summary>
    /// DAY (no qualifier): YYDDD — always 5 digits per COBOL-85 spec.
    /// </summary>
    private static string FormatDay(DateTime dt)
    {
        string year = dt.ToString("yy", CultureInfo.InvariantCulture);
        string dayOfYear = dt.DayOfYear.ToString("000", CultureInfo.InvariantCulture);
        return year + dayOfYear;
    }

    /// <summary>
    /// DAY YYYYDDD: YYYYDDD — always 7 digits.
    /// </summary>
    private static string FormatDayYYYYDDD(DateTime dt)
    {
        string year = dt.Year.ToString("0000", CultureInfo.InvariantCulture);
        string dayOfYear = dt.DayOfYear.ToString("000", CultureInfo.InvariantCulture);
        return year + dayOfYear;
    }

    /// <summary>
    /// DAY-OF-WEEK: 1=Monday through 7=Sunday (ISO 8601 convention).
    /// </summary>
    private static string FormatDayOfWeek(DateTime dt, int length)
    {
        int dow = ((int)dt.DayOfWeek + 6) % 7 + 1;
        string s = dow.ToString(CultureInfo.InvariantCulture);
        return s.Length > length ? s[..length] : s;
    }
}
