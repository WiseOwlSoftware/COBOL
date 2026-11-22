// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime;

/// <summary>
/// ⛔ THE §2023:15.5 DATE FORMS, OWNED BY ONE TYPE (kb/Work PB2634): the integer date form (§2023:15.5.2 — day 1 is
/// 1601-01-01 and a value "shall be less than or equal to the value of FUNCTION INTEGER-OF-DATE (99991231), which is
/// 3,067,671"), the standard date form (§2023:15.5.3) and the Julian date form (§2023:15.5.4) over the years 1601 to 9999,
/// with the Gregorian calendar of §2023:15.5.1. Every date intrinsic (<see cref="CobolDate"/>) and every LOCALE-* body
/// (<see cref="CobolLocale"/>) asks these members; the ceiling, the epoch and the day-number conversion are written
/// nowhere else (<c>IntegerDateFormDriftTests</c>). Before it the ceiling was a bare literal at six sites beside an unused
/// constant and the epoch arithmetic was written out at five, and the forgotten copy had already cost defects (kb/Work
/// R25: the UTC roll had no bound; PB23: the week path had no ceiling).
/// </summary>
internal static class IntegerDateForm
{
    /// <summary>Integer date 1 (§2023:15.5.2).</summary>
    private static readonly DateTime Epoch = new(1601, 1, 1);

    /// <summary>The largest value in integer date form, 9999-12-31 (§2023:15.5.2). Pinned against the epoch arithmetic by
    /// <c>CobolDateRangeTests</c>.
    /// <para>⛔ IT IS REACHABLE FROM VALID INPUT, WHICH IS THE WHOLE POINT (fix-queue PB23). A WEEK date's subfields are each
    /// in range (§2023:15.3.1.7: year ≤ 9999, week ≤ 52/53, day 1–7) while their COMBINATION lands past 9999-12-31 —
    /// 9999-12-31 IS ISO 9999-W52-5, so 9999-W52-6 and -7 are 10000-01-01/02. Two days, and the per-subfield range table
    /// cannot see them because no single subfield is wrong.</para></summary>
    internal const long Max = 3_067_671;

    /// <summary>The first and last years of the standard and Julian date forms (§2023:15.3.1.3: the year is "greater than
    /// 1600"; 9999 is the last year of the integer date form).</summary>
    internal const int FirstYear = 1601, LastYear = 9999;

    /// <summary>Is <paramref name="integerDate"/> in integer date form: positive and at most <see cref="Max"/>?</summary>
    internal static bool Contains(long integerDate) => integerDate is >= 1 and <= Max;

    /// <summary>The integer date form of <paramref name="date"/>'s calendar date (1601-01-01 through 9999-12-31).</summary>
    internal static long Of(DateTime date) => (date.Date - Epoch).Days + 1;

    /// <summary>The calendar date of <paramref name="integerDate"/>, which is in the form (<see cref="Contains"/>).</summary>
    internal static DateTime ToDate(long integerDate) => Epoch.AddDays(integerDate - 1);

    /// <summary><paramref name="year"/> brought inside <see cref="FirstYear"/>..<see cref="LastYear"/>.</summary>
    internal static int ClampYear(int year) => Math.Clamp(year, FirstYear, LastYear);

    /// <summary>Is (<paramref name="year"/>, <paramref name="month"/>, <paramref name="day"/>) a date in standard date form
    /// (§2023:15.5.3): a year 1601 to 9999, a month 1 to 12, and a day of that month?</summary>
    internal static bool IsStandardDate(long year, int month, int day) =>
        year is >= FirstYear and <= LastYear && month is >= 1 and <= 12 && day >= 1 && day <= DaysInMonth(year, month);

    // ── The calendar arithmetic of the four integer-date conversions (CobolDate.DateOfInteger and its three siblings,
    // whose family comment states the edition's year bound, kb/Work PB5130). System.DateTime ends at year 9999 and the 1989
    // module's form does not, so this is the calendar's own: 400 Gregorian years are 146,097 days, and 1601-01-01 starts
    // such a cycle. ──────────────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The days of a 400-year Gregorian cycle, of each of its three short centuries, of a 4-year group and of a
    /// common year.</summary>
    private const long DaysPer400Years = 146_097, DaysPer100Years = 36_524, DaysPer4Years = 1_461, DaysPerYear = 365;

    /// <summary>The days of a common year before each month (index 0 = January), and the year's total at index 12.</summary>
    private static readonly int[] DaysBeforeMonth = [0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365];

    /// <summary>The Gregorian leap-year rule (§2023:15.5.1).</summary>
    internal static bool IsLeapYear(long year) => year % 4 == 0 && (year % 100 != 0 || year % 400 == 0);

    /// <summary>The days of <paramref name="year"/>: 366 in a leap year, else 365.</summary>
    internal static int DaysInYear(long year) => IsLeapYear(year) ? 366 : 365;

    /// <summary>The days before month <paramref name="month"/> (1-12) of <paramref name="year"/>.</summary>
    internal static int DaysBefore(long year, int month) =>
        DaysBeforeMonth[month - 1] + (month > 2 && IsLeapYear(year) ? 1 : 0);

    /// <summary>The days of month <paramref name="month"/> (1-12) of <paramref name="year"/>.</summary>
    internal static int DaysInMonth(long year, int month) =>
        DaysBeforeMonth[month] - DaysBeforeMonth[month - 1] + (month == 2 && IsLeapYear(year) ? 1 : 0);

    /// <summary>The days of the years 1601 through <paramref name="year"/> − 1 (<paramref name="year"/> ≥ 1601).</summary>
    internal static long DaysBeforeYear(long year)
    {
        long y = year - FirstYear;
        return DaysPerYear * y + y / 4 - y / 100 + y / 400;
    }

    /// <summary>The year and day of year of integer date <paramref name="integerDate"/> (≥ 1).</summary>
    internal static (long Year, int DayOfYear) YearAndDayOf(long integerDate)
    {
        long n = integerDate - 1;                                     // days after 1601-01-01
        long cycles = n / DaysPer400Years;
        n %= DaysPer400Years;
        long centuries = Math.Min(n / DaysPer100Years, 3);            // the fourth century of a cycle has the extra day
        n -= centuries * DaysPer100Years;
        long groups = n / DaysPer4Years;
        n -= groups * DaysPer4Years;
        long years = Math.Min(n / DaysPerYear, 3);                    // the fourth year of a group is its leap year
        n -= years * DaysPerYear;
        return (FirstYear + 400 * cycles + 100 * centuries + 4 * groups + years, (int)n + 1);
    }

    /// <summary>Is <paramref name="integerDate"/> in integer date form for the compilation's edition: positive, and from 2002
    /// at most <see cref="Max"/>?</summary>
    internal static bool InIntegerDateForm(long integerDate, bool unbounded) =>
        unbounded ? integerDate >= 1 : Contains(integerDate);

    /// <summary>Is <paramref name="year"/> a year the edition's argument rules admit: above 1600, and from 2002 below 10000?</summary>
    internal static bool InYearRange(long year, bool unbounded) => year >= FirstYear && (unbounded || year <= LastYear);
}
