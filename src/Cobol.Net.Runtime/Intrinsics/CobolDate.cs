// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Text;

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

    /// <summary>The largest value in integer date form, fixed by ISO §15.5.2: a value "shall be greater than zero
    /// and shall be less than or equal to the value of FUNCTION INTEGER-OF-DATE (99991231), which is 3,067,671" —
    /// i.e. 9999-12-31. Pinned against the epoch arithmetic by <c>CobolDateRangeTests</c>.
    /// <para>⛔ IT IS REACHABLE FROM VALID INPUT, WHICH IS THE WHOLE POINT (fix-queue PB23). A WEEK date's subfields
    /// are each in range (§15.3.1.7: year ≤ 9999, week ≤ 52/53, day 1–7) while their COMBINATION lands past
    /// 9999-12-31 — 9999-12-31 IS ISO 9999-W52-5, so 9999-W52-6 and -7 are 10000-01-01/02. Two days, and the
    /// per-subfield range table cannot see them because no single subfield is wrong.</para></summary>
    private const long MaxIntegerDate = 3_067_671;

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

    // ── The §15.100/§15.23/§15.25 Y2K WINDOWING TRIO + §15.80 SECONDS-PAST-MIDNIGHT (COBOL-2002) ──────────────

    /// <summary>YEAR-TO-YYYY (ISO §15.100.4) — the ONE windowing core the composite pair delegates to
    /// (§15.23.4 r1 / §15.25.4 r1 are defined BY REFERENCE to this function; singular-pattern rule).
    /// <c>maximum-year = argument-2 + argument-3</c> (r1) is the ENDING year of the ALWAYS-100-year window —
    /// argument-2 is a SIGNED offset from argument-3, NOT a window size (the §15.100.4 NOTE-2 fixed-window
    /// guidance). The result is the unique year in <c>[maximum-year−99, maximum-year]</c> whose low-order two
    /// digits equal argument-1: r2a when <c>MOD(maximum-year,100) ≥ argument-1</c>, else r2b. Defaults:
    /// argument-2 omitted = 50 (r3 of §15.100.3); argument-3 omitted = the year at EXECUTION time
    /// (§15.100.3 r5 — <c>FUNCTION NUMVAL(FUNCTION CURRENT-DATE(1:4))</c>), read from the injectable
    /// <see cref="RunUnit.Clock"/> (the <paramref name="baseYear"/> 0 sentinel — 0 is outside the legal
    /// 1601..9999, so unambiguous). Argument rules (§15.100.3): argument-1 nonnegative &lt; 100 (r1 — 0 IS
    /// legal); argument-3 in 1601..9999 (r4); argument-2+argument-3 in 1700..9999 (r6). Violations →
    /// EC-ARGUMENT-FUNCTION (§15.3, default 0). maximum-year ≥ 1700, so truncating division ≡ the rule's
    /// FUNCTION INTEGER (floor).</summary>
    public static long YearToYyyy(long yy, long off = 50, long baseYear = 0)
    {
        if (yy is < 0 or > 99)
            return Exceptions.ExceptionState.ArgumentError($"YEAR-TO-YYYY argument-1 {yy} is not a nonnegative integer less than 100 (§15.100.3 r1)");
        long b = baseYear == 0 ? RunUnit.Current.Clock.Now().Year : baseYear;
        if (b is <= 1600 or >= 10000)
            return Exceptions.ExceptionState.ArgumentError($"YEAR-TO-YYYY argument-3 {b} is not greater than 1600 and less than 10000 (§15.100.3 r4)");
        long max = off + b;                                   // r1: maximum-year
        if (max is <= 1699 or >= 10000)
            return Exceptions.ExceptionState.ArgumentError($"YEAR-TO-YYYY argument-2 + argument-3 = {max} is not greater than 1699 and less than 10000 (§15.100.3 r6)");
        return max % 100 >= yy
            ? yy + 100 * (max / 100)                          // r2a
            : yy + 100 * (max / 100 - 1);                     // r2b
    }

    /// <summary>DATE-TO-YYYYMMDD (ISO §15.23.4 r1): <c>YEAR-TO-YYYY(YY, argument-2, argument-3) × 10000 +
    /// mmdd</c> where <c>YY = argument-1 / 10000</c> and <c>mmdd = MOD(argument-1, 10000)</c> — pure delegation
    /// to the ONE windowing core. §15.23.3 r1: argument-1 a POSITIVE integer &lt; 1,000,000 (0 illegal here,
    /// unlike YEAR-TO-YYYY's r1); the function does NOT validate the calendar date (the §15.23.3 NOTE points at
    /// TEST-DATE-YYYYMMDD). The r6 sum window is checked uniformly as argument-2+argument-3 (§15.25.3 r6 /
    /// §15.100.3 r6 wording; §15.23.3 r6's "year at the time of execution" phrasing coincides when argument-3
    /// is defaulted — the scout-noted drafting divergence, resolved to the delegated form).</summary>
    public static long DateToYyyymmdd(long date, long off = 50, long baseYear = 0)
    {
        if (date is < 1 or > 999999)
            return Exceptions.ExceptionState.ArgumentError($"DATE-TO-YYYYMMDD argument-1 {date} is not a positive integer less than 1000000 (§15.23.3 r1)");
        return YearToYyyy(date / 10000, off, baseYear) * 10000 + date % 10000;   // §15.23.4 r1
    }

    /// <summary>DAY-TO-YYYYDDD (ISO §15.25.4 r1): <c>YEAR-TO-YYYY(YY, argument-2, argument-3) × 1000 + nnn</c>
    /// where <c>YY = argument-1 / 1000</c> and <c>nnn = MOD(argument-1, 1000)</c>. §15.25.3 r1: argument-1 a
    /// POSITIVE integer &lt; 100,000; ordinal-day validity is NOT checked (the NOTE points at
    /// TEST-DAY-YYYYDDD).</summary>
    public static long DayToYyyyddd(long day, long off = 50, long baseYear = 0)
    {
        if (day is < 1 or > 99999)
            return Exceptions.ExceptionState.ArgumentError($"DAY-TO-YYYYDDD argument-1 {day} is not a positive integer less than 100000 (§15.25.3 r1)");
        return YearToYyyy(day / 1000, off, baseYear) * 1000 + day % 1000;        // §15.25.4 r1
    }

    /// <summary>TEST-DATE-YYYYMMDD (ISO §15.90.4 r1) — the if/else-if CHAIN, year before month before day
    /// (16000230 is 1, not 2): (1) argument-1 outside 16 010 000..99 999 999 — the year subfield is not
    /// 1601..9999 (r1a); (2) else MOD(argument-1, 10000) outside 100..1299 — the month is not 1..12 (r1b);
    /// (3) else the day subfield is not valid for the month of the year, Gregorian leap rules (r1c);
    /// (0) else valid (r1d). Codes confirmed by the informative D.31.3.8.</summary>
    public static long TestDateYyyymmdd(long date)
    {
        if (date is < 16010000 or > 99999999) return 1;                       // r1a — year
        long mmdd = date % 10000;
        if (mmdd is < 100 or > 1299) return 2;                                // r1b — month
        long day = date % 100;
        if (day < 1 || day > DateTime.DaysInMonth((int)(date / 10000), (int)(mmdd / 100)))
            return 3;                                                         // r1c — day
        return 0;                                                             // r1d
    }

    /// <summary>TEST-DAY-YYYYDDD (ISO §15.91.4 r1) — year before day; there is NO code 3 for this function:
    /// (1) argument-1 outside 1 601 000..9 999 999 — the year is not 1601..9999 (r1a); (2) else the ordinal
    /// day is not 1..365/366 for the year, Gregorian leap rules (r1b); (0) else valid (r1c). Codes confirmed
    /// by the informative D.31.3.9.</summary>
    public static long TestDayYyyyddd(long day)
    {
        if (day is < 1601000 or > 9999999) return 1;                          // r1a — year
        long ddd = day % 1000;
        if (ddd < 1 || ddd > (DateTime.IsLeapYear((int)(day / 1000)) ? 366 : 365))
            return 2;                                                         // r1b — day-of-year
        return 0;                                                             // r1c
    }

    /// <summary>SECONDS-PAST-MIDNIGHT (ISO §15.80.3): the current LOCAL time of day in seconds past midnight,
    /// in standard numeric time form — type NUMERIC, fractional seconds intended (r1/r2; the Annex D.31.5.4
    /// example carries 12 fraction digits). Returns the day's tick count = the UNSCALED value at SCALE 7 (the
    /// renderer's documented contract): the COBOL.NET documented precision (§15.80.3 r3, implementor item 171)
    /// is 100 ns — 7 fraction digits, the .NET <see cref="DateTime"/> resolution. Range [0, 86 400) —
    /// LEAP-SECOND is not supported (the §7.3.17 default OFF is the only mode; §15.5.5), so a returned value
    /// ≥ 86 400 is unreachable (§15.80.3 r4 answered "no"). Reads the injectable <see cref="RunUnit.Clock"/>
    /// (COBOLNET_CLOCK-deterministic — NOT the <see cref="CurrentDate"/> direct-<c>DateTimeOffset.Now</c>
    /// path, which predates the seam).</summary>
    public static long SecondsPastMidnight() => RunUnit.Current.Clock.Now().TimeOfDay.Ticks;

    // ── The §15.3 date/time FORMAT ENGINE (COBOL-2014 FORMATTED-*/INTEGER-OF-FORMATTED-DATE/etc.) ────────────────
    // One tokenizer feeds the emitter (FORMATTED-*) and the analyzer (INTEGER-OF-FORMATTED-DATE /
    // SECONDS-FROM-FORMATTED-TIME / TEST-FORMATTED-DATETIME) — ONE parse of a format string (singular pattern).

    private enum Fld { Year, Month, DayOfMonth, DayOfYear, Week, WeekDay, Hour, Minute, Second,
                       Fraction, OffSign, OffHour, OffMinute }

    /// <summary>A format segment: a data-bearing/phantom SEPARATOR (Sep + InData) or a numeric FIELD (Field + Width).</summary>
    private readonly record struct Seg(bool IsField, Fld Field, int Width, char Sep, bool InData);

    /// <summary>Tokenize a §15.3 format string. The fractional-second decimal separator is whichever of
    /// <c>.</c> / <c>,</c> literally appears in the format — §15.3.3.2 makes it ',' under DECIMAL-POINT IS COMMA,
    /// but since the separator sits in the format string itself no external mode is needed (the same char is
    /// expected in / emitted to the data). Extended (a colon/hyphen appears) ⇒ the decimal separator appears in the
    /// data; basic ⇒ it is a phantom (in the format, absent from the data). Returns null when the string is not a
    /// legal format (unknown char / bad field width).</summary>
    private static List<Seg>? Tokenize(string fmt)
    {
        bool extended = fmt.IndexOf(':') >= 0 || fmt.IndexOf('-') >= 0;
        var segs = new List<Seg>();
        bool weekMode = false, afterOffSign = false, sawSecond = false;
        int i = 0;
        int Run(char c) { int n = 0; while (i < fmt.Length && fmt[i] == c) { i++; n++; } return n; }
        while (i < fmt.Length)
        {
            char c = fmt[i];
            switch (c)
            {
                case 'Y': if (Run('Y') != 4) return null; segs.Add(new(true, Fld.Year, 4, '\0', false)); break;
                case 'M': if (Run('M') != 2) return null; segs.Add(new(true, Fld.Month, 2, '\0', false)); break;
                case 'D':
                {
                    int n = Run('D');
                    Fld f = weekMode ? Fld.WeekDay : n == 3 ? Fld.DayOfYear : Fld.DayOfMonth;
                    if ((f == Fld.DayOfMonth && n != 2) || (f == Fld.DayOfYear && n != 3) || (f == Fld.WeekDay && n != 1))
                        return null;
                    segs.Add(new(true, f, weekMode ? 1 : n, '\0', false));
                    break;
                }
                case 'W': i++; weekMode = true; segs.Add(new(false, default, 0, 'W', true)); break;   // §15.3.1.6 — W in data
                case 'w': if (Run('w') != 2) return null; segs.Add(new(true, Fld.Week, 2, '\0', false)); break;
                case 'T': i++; segs.Add(new(false, default, 0, 'T', true)); break;                     // §15.3.3.7 — T in data
                case 'Z': i++; segs.Add(new(false, default, 0, 'Z', true)); break;                     // §15.3.3.5 — UTC marker
                case 'h': if (Run('h') != 2) return null;
                          segs.Add(new(true, afterOffSign ? Fld.OffHour : Fld.Hour, 2, '\0', false)); break;
                case 'm': if (Run('m') != 2) return null;
                          segs.Add(new(true, afterOffSign ? Fld.OffMinute : Fld.Minute, 2, '\0', false)); break;
                case 's':
                {
                    int n = Run('s');
                    if (!sawSecond)
                    {
                        if (n != 2) return null;                                  // the integer seconds field is exactly 'ss'
                        sawSecond = true;
                        segs.Add(new(true, Fld.Second, 2, '\0', false));
                    }
                    else segs.Add(new(true, Fld.Fraction, n, '\0', false));       // an 's' run after the decimal separator = the fraction (§15.3.3.2)
                    break;
                }
                case '+': i++; afterOffSign = true; segs.Add(new(true, Fld.OffSign, 1, '\0', true)); break; // §15.3.3.6.1
                case '-': i++; segs.Add(new(false, default, 0, '-', true)); break;                     // extended date hyphen
                case ':': i++; segs.Add(new(false, default, 0, ':', true)); break;                     // extended time colon
                default:
                    if (c is '.' or ',') { i++; segs.Add(new(false, default, 0, c, extended)); break; }   // §15.3.3.2 basic ⇒ phantom
                    return null;
            }
        }
        return segs;
    }

    /// <summary>The fractional-second digit count of a format (0 if none) — the SECONDS-FROM-FORMATTED-TIME /
    /// FORMATTED-TIME result scale.</summary>
    public static int FormatFractionDigits(string format)
    {
        var segs = Tokenize(format);
        if (segs is null) return 0;
        foreach (var s in segs) if (s.IsField && s.Field == Fld.Fraction) return s.Width;
        return 0;
    }

    private static bool LongIsoYear(int y) => ISOWeek.GetWeeksInYear(y) == 53;   // §15.3.1.7 — a "long" ISO year has 53 weeks
    private static bool HasWeek(List<Seg> segs) { foreach (var s in segs) if (s.IsField && s.Field == Fld.Week) return true; return false; }

    /// <summary>Emit a formatted value from integer date form + seconds-past-midnight (unscaled/scale) + an
    /// optional UTC offset in minutes (§15.38–15.41). A date-only / time-only format ignores the components it
    /// does not reference. An ill-formed format sets EC-ARGUMENT-FUNCTION and yields the §15.3 default "".</summary>
    private static string EmitFormatted(string format, long integerDate, long secUnscaled, int secScale,
                                        long offsetMinutes, bool hasOffset)
    {
        var segs = Tokenize(format);
        if (segs is null)
        { Exceptions.ExceptionState.ArgumentError($"'{format}' is not a valid ISO 1989 date/time format (§15.3)"); return ""; }

        bool isUtc = segs.Any(s => !s.IsField && s.Sep == 'Z');
        bool hasDate = segs.Any(s => s.IsField && s.Field == Fld.Year);
        bool hasTime = segs.Any(s => s.IsField && s.Field == Fld.Hour);

        decimal secs = (decimal)secUnscaled / (decimal)(long)Pow10.AsWide(secScale);
        long day = integerDate;
        if (hasTime && isUtc && hasOffset)                              // §15.40/41 r2 — a UTC format displays local − offset
        {
            secs -= offsetMinutes * 60m;
            while (secs < 0) { secs += 86400m; day--; }
            while (secs >= 86400m) { secs -= 86400m; day++; }
        }

        int yy = 0, mo = 0, dm = 0, doy = 0, isoY = 0, isoW = 0, isoD = 0;
        if (hasDate)
        {
            DateTime dt = Epoch.AddDays(day - 1);                      // §15.5.2 epoch
            yy = dt.Year; mo = dt.Month; dm = dt.Day; doy = dt.DayOfYear;
            isoY = ISOWeek.GetYear(dt); isoW = ISOWeek.GetWeekOfYear(dt);
            isoD = ((int)dt.DayOfWeek + 6) % 7 + 1;                    // Mon=1..Sun=7 (§15.3.1.7)
        }
        long tot = (long)Math.Floor(secs);
        int hh = (int)(tot / 3600), mi = (int)(tot % 3600 / 60), ss = (int)(tot % 60);
        decimal frac = secs - tot;
        long offMag = Math.Abs(offsetMinutes);
        int oh = (int)(offMag / 60), om = (int)(offMag % 60);
        char osign = offsetMinutes >= 0 ? '+' : '-';                   // §15.3.3.6.1 — '+' = local at/ahead of UTC
        bool week = HasWeek(segs);

        var sb = new StringBuilder();
        foreach (var s in segs)
        {
            if (!s.IsField) { if (s.InData) sb.Append(s.Sep); continue; }
            switch (s.Field)
            {
                case Fld.Year:       sb.Append((week ? isoY : yy).ToString("0000")); break;
                case Fld.Month:      sb.Append(mo.ToString("00")); break;
                case Fld.DayOfMonth: sb.Append(dm.ToString("00")); break;
                case Fld.DayOfYear:  sb.Append(doy.ToString("000")); break;
                case Fld.Week:       sb.Append(isoW.ToString("00")); break;
                case Fld.WeekDay:    sb.Append(isoD.ToString("0")); break;
                case Fld.Hour:       sb.Append(hh.ToString("00")); break;
                case Fld.Minute:     sb.Append(mi.ToString("00")); break;
                case Fld.Second:     sb.Append(ss.ToString("00")); break;
                case Fld.Fraction:   sb.Append(((long)(frac * (decimal)(long)Pow10.AsWide(s.Width))).ToString(new string('0', s.Width))); break;
                case Fld.OffSign:    sb.Append(osign); break;
                case Fld.OffHour:    sb.Append(oh.ToString("00")); break;
                case Fld.OffMinute:  sb.Append(om.ToString("00")); break;
            }
        }
        return sb.ToString();
    }

    /// <summary>FORMATTED-DATE (§15.39): the integer date a2 rendered per the date format a1. An integer date
    /// outside 1..3,067,671 → the §15.3 default ("").</summary>
    public static string FormattedDate(string format, long integerDate)
    {
        if (integerDate is < 1 or > 3067671)
        { Exceptions.ExceptionState.ArgumentError($"FORMATTED-DATE argument {integerDate} outside 1..3,067,671 (§15.5.2)"); return ""; }
        return EmitFormatted(format, integerDate, 0, 0, 0, false);
    }

    /// <summary>FORMATTED-TIME (§15.41): seconds past midnight (a2, unscaled/scale) per the time format a1; a UTC
    /// format displays a2 adjusted by the offset a3 (r2), an offset format shows a2 direct + a3 in the offset field
    /// (r3). a3 omitted with a UTC/offset format is treated as 0 (r7).</summary>
    public static string FormattedTime(string format, long secUnscaled, int secScale, long offsetMinutes, bool hasOffset)
        => SecondsOutOfStandardForm("FORMATTED-TIME", "argument-2", secUnscaled, secScale)
           || OffsetOutOfRange("FORMATTED-TIME", "argument-3", offsetMinutes, hasOffset, "§15.41.3 r4")
            ? ""
            : EmitFormatted(format, 1, secUnscaled, secScale, offsetMinutes, hasOffset);

    /// <summary>§15.41.3 r3 / §15.40.3 r4: the seconds argument "shall be a value in STANDARD NUMERIC TIME FORM".
    /// The §7.3.17 LEAP-SECOND directive defines that range and this compiler supports only its default OFF
    /// (documented at <see cref="Tokenize"/>; §7.3.17 r5 — "When OFF is specified or implied, a standard numeric
    /// time form value shall be greater than or equal to zero and less than 86,400"). ⚠ Under ON the bound would
    /// be 86,401, so the constant is tied to the directive rather than to the number of seconds in a day —
    /// which is why it is cited here rather than written as a bare 86400.
    /// <para>Unenforced before this: a seconds argument of, say, 100000 produced <c>hh = 27</c> — a fabricated
    /// time with no exception condition, the same failure mode as the offset bound below.</para>
    /// <para>The comparison is exact and stays in <see cref="Int128"/>: the argument arrives as an unscaled
    /// value plus its scale, so scaling the BOUND up is exact where scaling the value down would truncate and
    /// silently admit a fractional overshoot.</para></summary>
    private static bool SecondsOutOfStandardForm(string fn, string argName, long secUnscaled, int secScale)
    {
        Int128 limit = (Int128)86400 * Pow10.AsWide(secScale);
        if (secUnscaled >= 0 && secUnscaled < limit) return false;
        Exceptions.ExceptionState.ArgumentError(
            $"{fn} {argName} is not in standard numeric time form: the value shall be >= 0 and < 86,400 "
            + $"(ISO §7.3.17 r5, LEAP-SECOND OFF — the only mode this compiler supports)");
        return true;
    }

    /// <summary>§15.40.3 r5 / §15.41.3 r4: "If argument-4 [argument-3] is specified, the magnitude of the value
    /// shall be less than or equal to 1439." The NOTE explains the bound — 1439 minutes is 23 hours 59 minutes,
    /// one minute short of a day.
    /// <para>⛔ A RUN-TIME check, unlike its §15.40.3 r6 / §15.41.3 r5 sibling (COBOLNET1633, bind time): the
    /// offset is an ordinary numeric argument, so its VALUE is generally not known until execution. It was
    /// enforced NOWHERE — an offset of 5000 minutes formatted a nonsense zone with no exception condition, which
    /// is the fabricated-value failure mode PB11 exists to close, not mere over-acceptance.</para></summary>
    private static bool OffsetOutOfRange(string fn, string argName, long offsetMinutes, bool hasOffset, string cite)
    {
        if (!hasOffset || Math.Abs(offsetMinutes) <= 1439) return false;
        Exceptions.ExceptionState.ArgumentError(
            $"{fn} {argName} is {offsetMinutes} minutes; the magnitude shall be <= 1439, one minute less than a "
            + $"day (ISO {cite})");
        return true;
    }

    /// <summary>FORMATTED-DATETIME (§15.40): integer date a2 + seconds a3 per the combined format a1; UTC ⇒ adjust
    /// by a4; offset ⇒ a3 direct in time and a4 direct in the offset field.</summary>
    public static string FormattedDatetime(string format, long integerDate, long secUnscaled, int secScale,
                                           long offsetMinutes, bool hasOffset)
    {
        if (integerDate is < 1 or > 3067671)
        { Exceptions.ExceptionState.ArgumentError($"FORMATTED-DATETIME argument {integerDate} outside 1..3,067,671 (§15.5.2)"); return ""; }
        if (SecondsOutOfStandardForm("FORMATTED-DATETIME", "argument-3", secUnscaled, secScale)) return "";
        if (OffsetOutOfRange("FORMATTED-DATETIME", "argument-4", offsetMinutes, hasOffset, "§15.40.3 r5")) return "";
        return EmitFormatted(format, integerDate, secUnscaled, secScale, offsetMinutes, hasOffset);
    }

    /// <summary>FORMATTED-CURRENT-DATE (§15.38): the current system date/time formatted per the combined format a1.
    /// Time accuracy is to the tick the runtime clock provides (§15.38.4 r2 — implementor-defined).</summary>
    public static string FormattedCurrentDate(string format)
    {
        DateTimeOffset now = DateTimeOffset.Now;
        long id = (now.Date - Epoch).Days + 1;
        decimal sod = (decimal)now.TimeOfDay.TotalSeconds;            // seconds past local midnight, sub-second retained
        long unscaled = (long)(sod * 1_000_000_000m);                 // 9 fraction digits (ample; §15.3.3.2)
        long offMin = (long)now.Offset.TotalMinutes;
        return EmitFormatted(format, id, unscaled, 9, offMin, true);
    }

    /// <summary>Walk <paramref name="data"/> against <paramref name="format"/>, validating every §15.3 separator
    /// and range. Returns 0 when fully valid, else the 1-based position of the first character at which an error can
    /// be determined (§15.92.4 — per-digit range narrowing: "20051314"/YYYYMMDD ⇒ 6, "15990316" ⇒ 2). On success
    /// fills the integer date form + seconds×10^f + fraction digit count.</summary>
    /// <param name="dateRepresentable">False when the data is VALID per the format but the date it denotes has no
    /// integer date form (§15.5.2's 3,067,671 ceiling — see <see cref="MaxIntegerDate"/>). Distinct from the return
    /// value on purpose: only the caller that must PRODUCE an integer date form cares, so TEST-FORMATTED-DATETIME
    /// still answers 0 (§15.92.4 — it reports "the ordinal character position at which the first error … was
    /// detected", and no CHARACTER of "9999W526" is in error; the identical '6' in "2009W526" is valid) and
    /// SECONDS-FROM-FORMATTED-TIME still returns its seconds.</param>
    private static int Analyze(string format, string data, out long integerDate, out long secondsScaled,
                               out int fracDigits, out bool dateRepresentable)
    {
        integerDate = 0; secondsScaled = 0; fracDigits = 0; dateRepresentable = true;
        var segs = Tokenize(format);
        if (segs is null) return 1;

        int pos = 0;
        int yy = 0, mo = 0, dm = 0, doy = 0, wk = 0, wd = 0, hh = 0, mi = 0, ss = 0;
        long frac = 0; bool offZero = false;

        foreach (var s in segs)
        {
            if (!s.IsField)
            {
                if (!s.InData) continue;                              // phantom separator (basic decimal) — no data char
                if (pos >= data.Length || data[pos] != s.Sep) return pos + 1;
                pos++; continue;
            }
            if (s.Field == Fld.OffSign)
            {
                if (pos >= data.Length) return pos + 1;
                char c = data[pos];
                if (c is not ('+' or '-')) { if (c == '0') offZero = true; else return pos + 1; }
                pos++; continue;
            }
            (int lo, int hi) = s.Field switch
            {
                Fld.Year       => (1601, 9999),                       // §15.3.1.3 (> 1600)
                Fld.Month      => (1, 12),
                Fld.DayOfMonth => (1, DateTime.DaysInMonth(Math.Clamp(yy, 1601, 9999), Math.Clamp(mo, 1, 12))),
                Fld.DayOfYear  => (1, DateTime.IsLeapYear(Math.Clamp(yy, 1601, 9999)) ? 366 : 365),
                Fld.Week       => (1, LongIsoYear(Math.Clamp(yy, 1601, 9999)) ? 53 : 52),
                Fld.WeekDay    => (1, 7),
                Fld.Hour       => (0, 23),
                Fld.Minute or Fld.OffMinute => (0, 59),
                Fld.Second     => (0, 59),                            // LEAP-SECOND OFF (§15.3.3.3); ON would lift to 60
                Fld.OffHour    => (0, 23),
                _              => (0, int.MaxValue),                  // Fraction — digits only
            };
            if (offZero && s.Field is Fld.OffHour or Fld.OffMinute) hi = 0;   // §15.3.3.6.1 — zero sign ⇒ zero magnitude

            int val = 0;
            for (int k = 0; k < s.Width; k++)
            {
                if (pos >= data.Length) return pos + 1;              // ran out of data
                char c = data[pos];
                if (c is < '0' or > '9') return pos + 1;             // non-digit
                val = val * 10 + (c - '0');
                if (s.Field != Fld.Fraction)                         // per-digit range narrowing (§15.92.4)
                {
                    int mn = val, mx = val;
                    for (int r = 0; r < s.Width - 1 - k; r++) { mn *= 10; mx = mx * 10 + 9; }
                    if (mx < lo || mn > hi) return pos + 1;          // provably out of range at THIS digit
                }
                pos++;
            }
            switch (s.Field)
            {
                case Fld.Year: yy = val; break;      case Fld.Month: mo = val; break;
                case Fld.DayOfMonth: dm = val; break; case Fld.DayOfYear: doy = val; break;
                case Fld.Week: wk = val; break;      case Fld.WeekDay: wd = val; break;
                case Fld.Hour: hh = val; break;      case Fld.Minute: mi = val; break;
                case Fld.Second: ss = val; break;
                case Fld.Fraction: frac = val; fracDigits = s.Width; break;
            }
        }
        if (pos != data.Length) return pos + 1;                       // trailing data — the first extra char is the error

        bool hasWeek = segs.Any(x => x.IsField && x.Field == Fld.Week);
        bool hasOrd  = segs.Any(x => x.IsField && x.Field == Fld.DayOfYear);
        bool hasCal  = segs.Any(x => x.IsField && x.Field == Fld.DayOfMonth);
        if (hasCal) integerDate = (new DateTime(yy, mo, dm) - Epoch).Days + 1;
        else if (hasOrd) integerDate = (new DateTime(yy, 1, 1).AddDays(doy - 1) - Epoch).Days + 1;
        // ⛔ THE WEEK DATE IS COMPUTED IN INTEGER ARITHMETIC, NOT VIA ISOWeek.ToDateTime, AND THAT IS THE FIX
        // (fix-queue PB23). `ISOWeek.ToDateTime(9999, 52, Saturday)` is 10000-01-01 and THROWS
        // ArgumentOutOfRangeException — a raw CLR fault escaping from three intrinsics, where §15.3 rule 14 permits
        // only EC-ARGUMENT-FUNCTION or the implementor-defined result. The input is VALID (every §15.3.1.7 subfield
        // rule is met), so the fault cannot be prevented by tightening the range table; the arithmetic must simply
        // not overflow. Week 1 is the week containing Jan 4 (§15.3.1.7: "The first week of a given year is the week
        // that includes January 4"), so its Monday is Jan 4 minus its ISO weekday-1 — always well inside range —
        // and every later day is that Monday plus a plain integer day count.
        else if (hasWeek)
        {
            var jan4 = new DateTime(yy, 1, 4);
            int isoDow = ((int)jan4.DayOfWeek + 6) % 7;                  // Monday = 0 … Sunday = 6
            long week1Monday = (jan4 - Epoch).Days - isoDow + 1;         // integer date form of week 1's Monday
            integerDate = week1Monday + ((long)wk - 1) * 7 + (wd - 1);
        }

        // §15.5.2 — a value in integer date form "shall be greater than zero and shall be less than or equal to
        // … 3,067,671". A date whose subfields are each permissible can still have NO integer date form; the caller
        // that must produce one raises EC-ARGUMENT-FUNCTION (§15.3 rule 14, "an incorrect value for that argument
        // OR FOR THE RETURNED VALUE"), while the callers that only validate the FORMAT are unaffected.
        // ⚠ Applied to EVERY path, not just the week one: the calendar and ordinal paths are bounded today only
        // because the range table correlates day-with-month and day-of-year-with-year (measured — "99990231" and
        // "9999366" are both rejected at the right character). This keeps that a property of ONE check rather than
        // of three separate range expressions that must each stay right.
        dateRepresentable = integerDate is > 0 and <= MaxIntegerDate || (!hasCal && !hasOrd && !hasWeek);

        secondsScaled = ((long)hh * 3600 + mi * 60 + ss) * (long)Pow10.AsWide(fracDigits) + frac;  // §15.79.4
        return 0;
    }

    /// <summary>INTEGER-OF-FORMATTED-DATE (§15.48): a2 analyzed per a1 → integer date form (a combined format's
    /// time is validated but does not affect the result, r1 NOTE). Invalid content → the §15.3 default 0.</summary>
    public static long IntegerOfFormattedDate(string format, string data)
    {
        int e = Analyze(format, data, out long id, out _, out _, out bool representable);
        if (e != 0)
            return Exceptions.ExceptionState.ArgumentError($"INTEGER-OF-FORMATTED-DATE: '{data}' is not valid per '{format}' (§15.48.3)");
        // §15.48.4 r1 makes the returned value "the integer date form equivalent" — and past §15.5.2's 3,067,671
        // ceiling there is none, so §15.3 rule 14's "incorrect value … for the returned value" arm applies. The
        // data itself is VALID (§15.3.1.7), which is why this is a RETURNED-VALUE error and not an argument one.
        return representable
            ? id
            : Exceptions.ExceptionState.ArgumentError(
                $"INTEGER-OF-FORMATTED-DATE: '{data}' per '{format}' denotes a date after 9999-12-31, which has no "
                + "integer date form (§15.5.2 caps it at 3,067,671) (§15.48.4)");
    }

    /// <summary>SECONDS-FROM-FORMATTED-TIME (§15.79): the hh/mm/ss subfields of a2 as (H*3600+M*60+S), scaled to
    /// the format's fractional-second count (the renderer supplies the matching result scale). Invalid → default 0.</summary>
    public static long SecondsFromFormattedTime(string format, string data, int scale)
    {
        // `dateRepresentable` is deliberately ignored: the seconds come from the TIME subfields, which §15.79.4
        // computes without reference to the date, so a combined format whose DATE lands past 9999-12-31 still has
        // a well-defined result here (PB23).
        int e = Analyze(format, data, out _, out long secs, out int f, out _);
        if (e != 0)
            return Exceptions.ExceptionState.ArgumentError($"SECONDS-FROM-FORMATTED-TIME: '{data}' invalid per '{format}' (§15.79.3)");
        return scale == f ? secs : scale > f ? secs * (long)Pow10.AsWide(scale - f) : secs / (long)Pow10.AsWide(f - scale);
    }

    /// <summary>TEST-FORMATTED-DATETIME (§15.92): 0 when a2 is valid per a1, else the 1-based position of the first
    /// character at which an error can be determined.</summary>
    /// <para>⚠ `dateRepresentable` is deliberately ignored. §15.92.1 tests validity "according to the specified
    /// format", §15.3.1.7's subfield rules are all satisfied by e.g. "9999W526", and §15.92.4 requires any non-zero
    /// answer to be "the ordinal character position at which the first error … was detected" — no CHARACTER of that
    /// value is in error, so there is no position to report and the answer is 0 (PB23).</para>
    public static long TestFormattedDatetime(string format, string data) => Analyze(format, data, out _, out _, out _, out _);

    /// <summary>COMBINED-DATETIME (§15.17.4): a1 + a2/100000 as an exact scaled value at scale (a2.scale + 5).</summary>
    public static Int128 CombinedDatetime(long integerDate, Int128 secUnscaled, int secScale)
        => (Int128)integerDate * Pow10.AsWide(secScale + 5) + secUnscaled;
}
