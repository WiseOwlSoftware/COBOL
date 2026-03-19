// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Text;

namespace CobolSharp.Runtime;

/// <summary>
/// Runtime helpers for ACCEPT FROM DATE/TIME/DAY/DAY-OF-WEEK.
/// Formats current date/time into the target field's byte storage.
/// </summary>
public static class AcceptRuntime
{
    /// <summary>
    /// ACCEPT identifier FROM source.
    /// Formats the current date/time value and writes it into the target storage area.
    /// </summary>
    /// <param name="area">Backing byte array.</param>
    /// <param name="offset">Field offset within the array.</param>
    /// <param name="length">Field length in bytes.</param>
    public static void Accept(byte[] area, int offset, int length, AcceptSourceKind sourceKind)
    {
        string text = sourceKind switch
        {
            AcceptSourceKind.Date => FormatDate(DateTime.Now, length),
            AcceptSourceKind.Time => FormatTime(DateTime.Now, length),
            AcceptSourceKind.Day => FormatDay(DateTime.Now, length),
            AcceptSourceKind.DayOfWeek => FormatDayOfWeek(DateTime.Now, length),
            _ => new string(' ', length) // Plain ACCEPT: blank fill (console input stub)
        };

        byte[] bytes = Encoding.ASCII.GetBytes(text);
        int copyLen = Math.Min(bytes.Length, length);
        Array.Copy(bytes, 0, area, offset, copyLen);

        // Pad remainder with spaces
        for (int i = offset + copyLen; i < offset + length; i++)
            area[i] = (byte)' ';
    }

    /// <summary>
    /// DATE: YYYYMMDD if field ≥ 8 chars, else YYMMDD.
    /// </summary>
    private static string FormatDate(DateTime dt, int length)
    {
        if (length >= 8)
            return dt.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        return dt.ToString("yyMMdd", CultureInfo.InvariantCulture);
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
    /// DAY: YYYYDDD (year + day-of-year), truncated if needed.
    /// </summary>
    private static string FormatDay(DateTime dt, int length)
    {
        string year = dt.Year.ToString("0000", CultureInfo.InvariantCulture);
        string dayOfYear = dt.DayOfYear.ToString("000", CultureInfo.InvariantCulture);
        string s = year + dayOfYear;
        return s.Length > length ? s[..length] : s;
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
