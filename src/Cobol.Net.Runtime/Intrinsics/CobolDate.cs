// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;

namespace CobolNet.Runtime;

/// <summary>
/// Family F4 — date/time intrinsics (ISO §15; deep-dive runtime home <c>CobolNet.Runtime.CobolDate</c>). The
/// integer date form (§15.5.2) starts at Monday 1601-01-01 = integer date 1 and runs through
/// INTEGER-OF-DATE(99991231) = 3,067,671; the Gregorian rules are §15.5.1 (.NET's <see cref="DateTime"/> IS the
/// proleptic Gregorian calendar over this whole range). Invalid date arguments yield the EC-ARGUMENT-FUNCTION
/// default result 0 (§15.3 — checking disabled; the legacy-proven behavior the NIST goldens encode).
/// </summary>
public static class CobolDate
{
    private static readonly DateTime Epoch = new(1601, 1, 1);   // integer date 1 (ISO §15.5.2)

    /// <summary>The §15.21.3 / §15.99.3 21-character timestamp layout: positions 1–16 <c>yyyyMMddHHmmss</c> +
    /// hundredths; 17 the offset sign ('+' when local time is at or ahead of UTC, '−' behind — '0' reserved for
    /// systems without an offset facility, which .NET always has); 18–19 offset hours; 20–21 offset minutes.
    /// The ONE formatter — CURRENT-DATE renders the runtime clock through it and the compiler bakes
    /// WHEN-COMPILED's compile-time constant through it (singular-pattern rule).</summary>
    public static string Format21(DateTimeOffset t) =>
        t.ToString("yyyyMMddHHmmssff", CultureInfo.InvariantCulture)
        + (t.Offset >= TimeSpan.Zero ? "+" : "-")
        + Math.Abs(t.Offset.Hours).ToString("00", CultureInfo.InvariantCulture)
        + Math.Abs(t.Offset.Minutes).ToString("00", CultureInfo.InvariantCulture);

    /// <summary>CURRENT-DATE (§15.21.3): the 21-character calendar date / time of day / local-offset value at the
    /// moment of evaluation. IF107A validates plausibility ranges and that successive references never decrease.</summary>
    public static string CurrentDate() => Format21(DateTimeOffset.Now);

    /// <summary>DATE-OF-INTEGER (§15.22.4): integer date form → standard date form YYYYMMDD. An argument outside
    /// 1..3,067,671 (§15.5.2) → 0 (EC default, §15.3).</summary>
    public static long DateOfInteger(long integerDate)
    {
        if (integerDate is < 1 or > 3067671)                 // EC-ARGUMENT-FUNCTION raise point / §15.3 default 0
            return Exceptions.ExceptionState.ArgumentError($"DATE-OF-INTEGER argument {integerDate} outside 1..3,067,671 (§15.5.2)");
        DateTime d = Epoch.AddDays(integerDate - 1);
        return d.Year * 10000L + d.Month * 100L + d.Day;
    }

    /// <summary>DAY-OF-INTEGER (§15.24.4): integer date form → Julian date form YYYYDDD (§15.5.4).</summary>
    public static long DayOfInteger(long integerDate)
    {
        if (integerDate is < 1 or > 3067671)                 // EC-ARGUMENT-FUNCTION raise point / §15.3 default 0
            return Exceptions.ExceptionState.ArgumentError($"DAY-OF-INTEGER argument {integerDate} outside 1..3,067,671 (§15.5.2)");
        DateTime d = Epoch.AddDays(integerDate - 1);
        return d.Year * 1000L + d.DayOfYear;
    }

    /// <summary>INTEGER-OF-DATE (§15.46.4): standard date form YYYYMMDD → integer date form. An invalid calendar
    /// date or a year before 1601 → 0 (EC default, §15.3).</summary>
    public static long IntegerOfDate(long yyyymmdd)
    {
        long year = yyyymmdd / 10000, month = yyyymmdd / 100 % 100, day = yyyymmdd % 100;
        if (year is < 1601 or > 9999 || month is < 1 or > 12 || day < 1
            || day > DateTime.DaysInMonth((int)year, (int)month))
            return Exceptions.ExceptionState.ArgumentError($"INTEGER-OF-DATE argument {yyyymmdd} is not a valid standard date (§15.46.3)");
        return (new DateTime((int)year, (int)month, (int)day) - Epoch).Days + 1;
    }

    /// <summary>INTEGER-OF-DAY (§15.47.4): Julian date form YYYYDDD → integer date form. An invalid ordinal day
    /// (0, or past the year's length) or a year before 1601 → 0 (EC default, §15.3).</summary>
    public static long IntegerOfDay(long yyyyddd)
    {
        long year = yyyyddd / 1000, day = yyyyddd % 1000;
        if (year is < 1601 or > 9999 || day < 1 || day > (DateTime.IsLeapYear((int)year) ? 366 : 365))
            return Exceptions.ExceptionState.ArgumentError($"INTEGER-OF-DAY argument {yyyyddd} is not a valid Julian date (§15.47.3)");
        return (new DateTime((int)year, 1, 1).AddDays(day - 1) - Epoch).Days + 1;
    }
}
