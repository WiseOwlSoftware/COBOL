// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Text;

namespace CobolSharp.Runtime.Intrinsics;

/// <summary>
/// Implementation of COBOL intrinsic functions per ISO/IEC 1989:2023 §15.
/// Each method corresponds to a FUNCTION keyword in COBOL source.
/// All functions take and return decimals or strings as appropriate.
/// </summary>
public static class IntrinsicFunctions
{
    // ═══════════════════════════════════════════════════
    // Math functions
    // ═══════════════════════════════════════════════════

    public static decimal Abs(decimal value) => Math.Abs(value);
    public static decimal Acos(decimal value) => (decimal)Math.Acos((double)value);
    public static decimal Asin(decimal value) => (decimal)Math.Asin((double)value);
    public static decimal Atan(decimal value) => (decimal)Math.Atan((double)value);
    public static decimal Cos(decimal value) => (decimal)Math.Cos((double)value);
    public static decimal Sin(decimal value) => (decimal)Math.Sin((double)value);
    public static decimal Tan(decimal value) => (decimal)Math.Tan((double)value);
    public static decimal Sqrt(decimal value) => (decimal)Math.Sqrt((double)value);
    public static decimal Log(decimal value) => (decimal)Math.Log((double)value);
    public static decimal Log10(decimal value) => (decimal)Math.Log10((double)value);
    public static decimal Exp(decimal value) => (decimal)Math.Exp((double)value);
    public static decimal Exp10(decimal value) => (decimal)Math.Pow(10, (double)value);
    public static decimal Factorial(decimal value)
    {
        int n = (int)value;
        decimal result = 1;
        for (int i = 2; i <= n; i++) result *= i;
        return result;
    }
    public static decimal Mod(decimal value, decimal divisor) => value - divisor * Math.Floor(value / divisor);
    public static decimal Rem(decimal value, decimal divisor) => value - divisor * Math.Truncate(value / divisor);
    public static decimal Integer(decimal value) => Math.Floor(value);
    public static decimal IntegerPart(decimal value) => Math.Truncate(value);
    public static decimal FractionPart(decimal value) => value - Math.Truncate(value);
    public static decimal Sign(decimal value) => value > 0 ? 1 : value < 0 ? -1 : 0;
    public static decimal Pi() => 3.14159265358979323846m;

    /// <summary>
    /// RANDOM (§15.75): Returns pseudo-random number 0 &lt;= result &lt; 1.
    /// Optional integer seed argument creates a new Random instance for determinism.
    /// </summary>
    private static Random s_sharedRandom = new Random();
    public static decimal Random(decimal? seed = null)
    {
        if (seed.HasValue)
        {
            var rng = new Random((int)seed.Value);
            return (decimal)rng.NextDouble();
        }
        return (decimal)s_sharedRandom.NextDouble();
    }

    // ═══════════════════════════════════════════════════
    // Numeric aggregate functions
    // ═══════════════════════════════════════════════════

    public static decimal Max(params decimal[] values) => values.Max();
    public static decimal Min(params decimal[] values) => values.Min();
    public static decimal Sum(params decimal[] values) => values.Sum();
    public static decimal Mean(params decimal[] values) => values.Average();
    public static decimal Median(params decimal[] values)
    {
        var sorted = values.OrderBy(v => v).ToArray();
        int mid = sorted.Length / 2;
        return sorted.Length % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2
            : sorted[mid];
    }
    public static decimal Midrange(params decimal[] values) => (values.Min() + values.Max()) / 2;
    public static decimal Range(params decimal[] values) => values.Max() - values.Min();
    public static decimal Variance(params decimal[] values)
    {
        decimal mean = values.Average();
        return values.Select(v => (v - mean) * (v - mean)).Average();
    }
    public static decimal StandardDeviation(params decimal[] values) =>
        (decimal)Math.Sqrt((double)Variance(values));
    public static decimal OrdMax(params decimal[] values)
    {
        decimal max = values[0];
        int idx = 1;
        for (int i = 1; i < values.Length; i++)
            if (values[i] > max) { max = values[i]; idx = i + 1; }
        return idx;
    }
    public static decimal OrdMin(params decimal[] values)
    {
        decimal min = values[0];
        int idx = 1;
        for (int i = 1; i < values.Length; i++)
            if (values[i] < min) { min = values[i]; idx = i + 1; }
        return idx;
    }

    /// <summary>MAX for string arguments — returns lexicographic maximum.</summary>
    public static string MaxString(params string[] values)
    {
        string max = values[0];
        for (int i = 1; i < values.Length; i++)
            if (string.Compare(values[i], max, StringComparison.Ordinal) > 0)
                max = values[i];
        return max;
    }

    /// <summary>MIN for string arguments — returns lexicographic minimum.</summary>
    public static string MinString(params string[] values)
    {
        string min = values[0];
        for (int i = 1; i < values.Length; i++)
            if (string.Compare(values[i], min, StringComparison.Ordinal) < 0)
                min = values[i];
        return min;
    }

    /// <summary>ORD-MAX for string arguments — returns 1-based position of lexicographic maximum.</summary>
    public static decimal OrdMaxString(params string[] values)
    {
        string max = values[0];
        int idx = 1;
        for (int i = 1; i < values.Length; i++)
            if (string.Compare(values[i], max, StringComparison.Ordinal) > 0) { max = values[i]; idx = i + 1; }
        return idx;
    }

    /// <summary>ORD-MIN for string arguments — returns 1-based position of lexicographic minimum.</summary>
    public static decimal OrdMinString(params string[] values)
    {
        string min = values[0];
        int idx = 1;
        for (int i = 1; i < values.Length; i++)
            if (string.Compare(values[i], min, StringComparison.Ordinal) < 0) { min = values[i]; idx = i + 1; }
        return idx;
    }

    // ═══════════════════════════════════════════════════
    // String functions
    // ═══════════════════════════════════════════════════

    public static string LowerCase(string value) => value.ToLowerInvariant();
    public static string UpperCase(string value) => value.ToUpperInvariant();
    public static string Reverse(string value) => new string(value.Reverse().ToArray());
    public static string Trim(string value) => value.Trim();
    public static string TrimLeading(string value) => value.TrimStart();
    public static string TrimTrailing(string value) => value.TrimEnd();
    /// <summary>
    /// LENGTH (§15.41): Returns declared size of the data item.
    /// For alphanumeric items passed as strings, string.Length is correct.
    /// For numeric or other categories, this would need the PIC size from the data description.
    /// </summary>
    public static decimal Length(string value) => value.Length;
    public static decimal ByteLength(string value) => Encoding.ASCII.GetByteCount(value);
    public static string Concatenate(params string[] values) => string.Concat(values);
    /// <summary>
    /// SUBSTITUTE (§15.87): Replaces all occurrences of from/to pairs in source.
    /// Arguments: (source, from1, to1, from2, to2, ...).
    /// </summary>
    public static string Substitute(string source, params string[] fromToPairs)
    {
        var result = source;
        for (int i = 0; i + 1 < fromToPairs.Length; i += 2)
        {
            result = result.Replace(fromToPairs[i], fromToPairs[i + 1], StringComparison.Ordinal);
        }
        return result;
    }
    public static string Char(decimal code) => ((char)(int)code).ToString();
    public static decimal Ord(string value) => value.Length > 0 ? (decimal)value[0] : 0;

    // ═══════════════════════════════════════════════════
    // Date/Time functions
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// CURRENT-DATE: returns 21-character string YYYYMMDDHHMMSSssGMTdiff
    /// </summary>
    public static string CurrentDate()
    {
        var now = DateTimeOffset.Now;
        var offset = now.Offset;
        string sign = offset >= TimeSpan.Zero ? "+" : "-";
        return now.ToString("yyyyMMddHHmmssff", CultureInfo.InvariantCulture)
            + sign
            + Math.Abs(offset.Hours).ToString("00")
            + Math.Abs(offset.Minutes).ToString("00");
    }

    /// <summary>DATE-OF-INTEGER: convert integer date (days since epoch) to YYYYMMDD</summary>
    public static decimal DateOfInteger(decimal integerDate)
    {
        var date = new DateTime(1601, 1, 1).AddDays((double)integerDate - 1);
        return decimal.Parse(date.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
    }

    /// <summary>INTEGER-OF-DATE: convert YYYYMMDD to integer date</summary>
    public static decimal IntegerOfDate(decimal yyyymmdd)
    {
        int d = (int)yyyymmdd;
        int year = d / 10000;
        int month = (d / 100) % 100;
        int day = d % 100;
        var date = new DateTime(year, month, day);
        var epoch = new DateTime(1601, 1, 1);
        return (decimal)(date - epoch).Days + 1;
    }

    /// <summary>DAY-OF-INTEGER: convert integer date to YYYYDDD</summary>
    public static decimal DayOfInteger(decimal integerDate)
    {
        var date = new DateTime(1601, 1, 1).AddDays((double)integerDate - 1);
        return year4(date) * 1000 + date.DayOfYear;
    }

    /// <summary>INTEGER-OF-DAY: convert YYYYDDD to integer date</summary>
    public static decimal IntegerOfDay(decimal yyyyddd)
    {
        int d = (int)yyyyddd;
        int year = d / 1000;
        int dayOfYear = d % 1000;
        var date = new DateTime(year, 1, 1).AddDays(dayOfYear - 1);
        var epoch = new DateTime(1601, 1, 1);
        return (decimal)(date - epoch).Days + 1;
    }

    /// <summary>
    /// DATE-TO-YYYYMMDD (§15.20): Convert 2-digit year date to 4-digit year.
    /// Args: (yymmdd, [window=50], [century-offset=0]).
    /// Window: 2-digit years >= window map to previous century, &lt; window map to current century.
    /// </summary>
    public static decimal DateToYyyymmdd(decimal yymmdd, decimal windowBase = 50m, decimal offset = 0m)
    {
        int d = (int)yymmdd;
        int yy = d / 10000;
        int currentCentury = DateTime.Now.Year / 100 * 100;
        int window = (int)windowBase;
        int centuryOffset = (int)offset;
        int baseCentury = currentCentury + centuryOffset * 100;
        int yyyy = yy >= window ? baseCentury - 100 + yy : baseCentury + yy;
        return yyyy * 10000 + d % 10000;
    }

    /// <summary>
    /// YEAR-TO-YYYY (§15.96): Convert 2-digit year to 4-digit year.
    /// Args: (yy, [window=50], [century-offset=0]).
    /// </summary>
    public static decimal YearToYyyy(decimal yy, decimal windowBase = 50m, decimal offset = 0m)
    {
        int year2 = (int)yy;
        int currentCentury = DateTime.Now.Year / 100 * 100;
        int window = (int)windowBase;
        int centuryOffset = (int)offset;
        int baseCentury = currentCentury + centuryOffset * 100;
        return year2 >= window ? baseCentury - 100 + year2 : baseCentury + year2;
    }

    /// <summary>
    /// DAY-TO-YYYYDDD (§15.22): Convert 2-digit year Julian date (YYDDD) to 4-digit year (YYYYDDD).
    /// Same windowing logic as DATE-TO-YYYYMMDD.
    /// Args: (yyddd, [window=50], [century-offset=0]).
    /// </summary>
    public static decimal DayToYyyyddd(decimal yyddd, decimal windowBase = 50m, decimal offset = 0m)
    {
        int d = (int)yyddd;
        int yy = d / 1000;
        int ddd = d % 1000;
        int currentCentury = DateTime.Now.Year / 100 * 100;
        int window = (int)windowBase;
        int centuryOffset = (int)offset;
        int baseCentury = currentCentury + centuryOffset * 100;
        int yyyy = yy >= window ? baseCentury - 100 + yy : baseCentury + yy;
        return yyyy * 1000 + ddd;
    }

    // ═══════════════════════════════════════════════════
    // Financial functions
    // ═══════════════════════════════════════════════════

    public static decimal Annuity(decimal rate, decimal periods)
    {
        if (rate == 0) return 1m / periods;
        double r = (double)rate;
        double n = (double)periods;
        return (decimal)(r / (1 - Math.Pow(1 + r, -n)));
    }

    public static decimal PresentValue(decimal rate, params decimal[] amounts)
    {
        double r = (double)rate;
        double pv = 0;
        for (int i = 0; i < amounts.Length; i++)
        {
            pv += (double)amounts[i] / Math.Pow(1 + r, i + 1);
        }
        return (decimal)pv;
    }

    // ═══════════════════════════════════════════════════
    // General functions
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// WHEN-COMPILED (§15.95): Returns the compile-time timestamp.
    /// TODO: This should be baked in at compile time (i.e., the CIL emitter should emit
    /// a constant string instead of calling this at runtime). Currently returns DateTime.Now
    /// as a placeholder, which means it returns the runtime time, not the compile time.
    /// Returns 21-character string with UTC offset (same format as CURRENT-DATE).
    /// </summary>
    public static string WhenCompiled()
    {
        var now = DateTimeOffset.Now;
        var offset = now.Offset;
        string sign = offset >= TimeSpan.Zero ? "+" : "-";
        return now.ToString("yyyyMMddHHmmssff", CultureInfo.InvariantCulture)
            + sign
            + Math.Abs(offset.Hours).ToString("00")
            + Math.Abs(offset.Minutes).ToString("00");
    }
    /// <summary>
    /// NUMVAL (§15.60): Converts COBOL numeric string to decimal.
    /// Handles leading/trailing spaces, optional leading/trailing sign (+/-), CR/DB suffix.
    /// </summary>
    public static decimal NumericValue(string text)
    {
        var s = text.Trim();
        if (s.Length == 0) return 0;

        bool negative = false;

        // Check for trailing CR or DB (debit indicators)
        if (s.EndsWith("CR", StringComparison.OrdinalIgnoreCase) ||
            s.EndsWith("DB", StringComparison.OrdinalIgnoreCase))
        {
            negative = true;
            s = s[..^2].Trim();
        }

        // Check for leading sign
        if (s.StartsWith('+'))
        {
            s = s[1..].Trim();
        }
        else if (s.StartsWith('-'))
        {
            negative = !negative;
            s = s[1..].Trim();
        }

        // Check for trailing sign
        if (s.EndsWith('+'))
        {
            s = s[..^1].Trim();
        }
        else if (s.EndsWith('-'))
        {
            negative = !negative;
            s = s[..^1].Trim();
        }

        if (decimal.TryParse(s, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite,
            CultureInfo.InvariantCulture, out var result))
        {
            return negative ? -result : result;
        }
        return 0;
    }

    /// <summary>
    /// NUMVAL-C (§15.61): Converts COBOL numeric string with currency to decimal.
    /// Strips the currency symbol and grouping separators before parsing.
    /// Supports CR/DB suffix for negative values.
    /// </summary>
    public static decimal NumericValueC(string text, string currencySymbol = "$")
    {
        var s = text.Replace(currencySymbol, "", StringComparison.Ordinal)
                    .Replace(",", "");
        return NumericValue(s);
    }

    // ═══════════════════════════════════════════════════
    // COBOL-2002+ functions
    // ═══════════════════════════════════════════════════

    /// <summary>SECONDS-PAST-MIDNIGHT: seconds since midnight as decimal (§15.84)</summary>
    public static decimal SecondsPastMidnight() => (decimal)DateTime.Now.TimeOfDay.TotalSeconds;

    /// <summary>E: Euler's number (§15.29)</summary>
    public static decimal E() => 2.71828182845904523536028747135266m;

    /// <summary>FIND-STRING: 1-based position of target in source, 0 if not found (§15.37)</summary>
    public static decimal FindString(string source, string target)
    {
        int idx = source.IndexOf(target, StringComparison.Ordinal);
        return idx < 0 ? 0m : (decimal)(idx + 1);
    }

    /// <summary>TEST-DATE-YYYYMMDD: 0 if valid date, non-zero if invalid (§15.90)</summary>
    public static decimal TestDateYyyymmdd(decimal yyyymmdd)
    {
        string s = ((int)yyyymmdd).ToString("00000000");
        return DateTime.TryParseExact(s, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
            ? 0m : 1m;
    }

    /// <summary>TEST-DAY-YYYYDDD: 0 if valid Julian date, non-zero if invalid (§15.91)</summary>
    public static decimal TestDayYyyyddd(decimal yyyyddd)
    {
        int d = (int)yyyyddd;
        int year = d / 1000;
        int dayOfYear = d % 1000;
        if (year < 1 || year > 9999 || dayOfYear < 1) return 1m;
        int maxDay = DateTime.IsLeapYear(year) ? 366 : 365;
        return dayOfYear > maxDay ? 1m : 0m;
    }

    /// <summary>TEST-NUMVAL: 0 if valid NUMVAL format, non-zero if invalid (§15.93)</summary>
    public static decimal TestNumval(string text)
    {
        string trimmed = text.Trim();
        if (trimmed.Length == 0) return 1m;
        return decimal.TryParse(trimmed, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint
            | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite,
            CultureInfo.InvariantCulture, out _) ? 0m : 1m;
    }

    /// <summary>TEST-NUMVAL-C: 0 if valid NUMVAL-C format, non-zero if invalid (§15.94)</summary>
    public static decimal TestNumvalC(string text)
    {
        string trimmed = text.Trim().Replace("$", "").Replace(",", "").Replace("CR", "").Replace("DB", "");
        if (trimmed.Length == 0) return 1m;
        return decimal.TryParse(trimmed, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint
            | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite,
            CultureInfo.InvariantCulture, out _) ? 0m : 1m;
    }

    /// <summary>NUMVAL-F: parse floating-point string to decimal (§15.69)</summary>
    public static decimal NumvalF(string text)
    {
        string trimmed = text.Trim();
        if (decimal.TryParse(trimmed, NumberStyles.Float | NumberStyles.AllowExponent,
            CultureInfo.InvariantCulture, out var result))
            return result;
        return 0m;
    }

    /// <summary>COMBINED-DATETIME: combine integer date and time (§15.17)</summary>
    public static decimal CombinedDatetime(decimal integerDate, decimal time)
        => integerDate * 1000000m + time;

    /// <summary>BOOLEAN-OF-INTEGER: convert integer to binary string (§15.13)</summary>
    public static string BooleanOfInteger(decimal value, decimal length)
    {
        string bits = Convert.ToString((int)value, 2);
        int len = (int)length;
        return bits.Length >= len ? bits : bits.PadLeft(len, '0');
    }

    /// <summary>INTEGER-OF-BOOLEAN: convert binary string to integer (§15.45)</summary>
    public static decimal IntegerOfBoolean(string boolStr)
        => (decimal)Convert.ToInt32(boolStr.Trim(), 2);

    /// <summary>FORMATTED-CURRENT-DATE: current date/time in ISO 8601 (§15.38)</summary>
    public static string FormattedCurrentDate(string format)
    {
        var now = DateTimeOffset.Now;
        // COBOL format argument specifies the pattern; use ISO 8601 as default
        return now.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);
    }

    /// <summary>FORMATTED-DATE: integer date to ISO 8601 date string (§15.39)</summary>
    public static string FormattedDate(string format, decimal integerDate)
    {
        var date = new DateTime(1601, 1, 1).AddDays((double)integerDate - 1);
        return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    /// <summary>FORMATTED-TIME: numeric time to ISO 8601 time string (§15.41)</summary>
    public static string FormattedTime(string format, decimal time)
    {
        // Time is HHMMSS or HHMMSSss
        int t = (int)time;
        int hours = t / 10000;
        int minutes = (t / 100) % 100;
        int seconds = t % 100;
        return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
    }

    /// <summary>HIGHEST-ALGEBRAIC: max value for a field with given digits (§15.43)</summary>
    public static decimal HighestAlgebraic(decimal digits)
    {
        int d = (int)digits;
        decimal max = 1m;
        for (int i = 0; i < d; i++) max *= 10m;
        return max - 1m;
    }

    /// <summary>LOWEST-ALGEBRAIC: min value for a signed field (§15.58)</summary>
    public static decimal LowestAlgebraic(decimal digits)
    {
        int d = (int)digits;
        decimal max = 1m;
        for (int i = 0; i < d; i++) max *= 10m;
        return -(max - 1m);
    }

    /// <summary>MODULE-NAME: return current module/program name (§15.65)</summary>
    public static string ModuleName()
    {
        var assembly = System.Reflection.Assembly.GetEntryAssembly();
        return assembly?.GetName().Name ?? "UNKNOWN";
    }

    // ═══════════════════════════════════════════════════
    // COBOL-2002+ stubs (complex / not yet implemented)
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// LOCALE-COMPARE (§15.55): Compare two strings using locale-sensitive ordering.
    /// Returns 1 if s1 > s2, -1 if s1 &lt; s2, 0 if equal.
    /// </summary>
    public static decimal LocaleCompare(string s1, string s2)
    {
        int cmp = string.Compare(s1, s2, StringComparison.CurrentCulture);
        return cmp > 0 ? 1m : cmp < 0 ? -1m : 0m;
    }

    /// <summary>
    /// LOCALE-DATE (§15.56): Format integer-date using current locale.
    /// Converts integer date (days since 1601-01-01 epoch) to DateTime,
    /// then formats using the current culture's short date pattern.
    /// </summary>
    public static string LocaleDate(decimal integerDate)
    {
        var date = new DateTime(1601, 1, 1).AddDays((double)integerDate - 1);
        return date.ToString("d", CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// LOCALE-TIME (§15.57): Format numeric time (HHMMSS or HHMMSSff) using current locale.
    /// </summary>
    public static string LocaleTime(decimal time)
    {
        int t = (int)time;
        int hours = t / 10000;
        int minutes = (t / 100) % 100;
        int seconds = t % 100;
        var ts = new TimeSpan(hours, minutes, seconds);
        return new DateTime(1, 1, 1).Add(ts).ToString("T", CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// LOCALE-TIME-FROM-SECONDS (§15.58): Convert seconds since midnight to locale-formatted time.
    /// </summary>
    public static string LocaleTimeFromSeconds(decimal seconds)
    {
        var ts = TimeSpan.FromSeconds((double)seconds);
        return new DateTime(1, 1, 1).Add(ts).ToString("T", CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// STANDARD-COMPARE (§15.87): Compare two strings using the standard (ordinal) character ordering.
    /// Returns 1 if s1 > s2, -1 if s1 &lt; s2, 0 if equal.
    /// </summary>
    public static decimal StandardCompare(string s1, string s2)
    {
        int cmp = string.CompareOrdinal(s1, s2);
        return cmp > 0 ? 1m : cmp < 0 ? -1m : 0m;
    }

    /// <summary>
    /// DISPLAY-OF (§15.28): Convert national (UTF-16) string to alphanumeric display representation.
    /// TODO: Full national character support not implemented; returns input string as-is.
    /// </summary>
    public static string DisplayOf(string value)
    {
        // TODO: full national-to-display conversion requires national data type support
        return value;
    }

    /// <summary>
    /// NATIONAL-OF (§15.66): Convert alphanumeric to national (UTF-16).
    /// TODO: Full national character support not implemented; returns input string as-is.
    /// </summary>
    public static string NationalOf(string value)
    {
        // TODO: full display-to-national conversion requires national data type support
        return value;
    }

    /// <summary>
    /// CHAR-NATIONAL (§15.16): Return the national character at a given ordinal position.
    /// </summary>
    public static string CharNational(decimal code)
        => ((char)(int)code).ToString();

    /// <summary>
    /// CONVERT (§15.19): General format conversion between encodings.
    /// Converts the input string from one encoding to another using .NET's Encoding support.
    /// Falls back to returning the input unchanged for unrecognized encodings.
    /// </summary>
    public static string ConvertEncoding(string value, string fromEncoding, string toEncoding)
    {
        try
        {
            var srcEnc = Encoding.GetEncoding(fromEncoding);
            var dstEnc = Encoding.GetEncoding(toEncoding);
            byte[] srcBytes = srcEnc.GetBytes(value);
            byte[] dstBytes = Encoding.Convert(srcEnc, dstEnc, srcBytes);
            return dstEnc.GetString(dstBytes);
        }
        catch
        {
            // Unrecognized encoding — return input unchanged
            return value;
        }
    }

    /// <summary>
    /// BASECONVERT (§15.10): Convert numeric string between number bases (2-36).
    /// Parses the input string in fromBase, then formats the result in toBase.
    /// </summary>
    public static string Baseconvert(string value, decimal fromBase, decimal toBase)
    {
        int fb = (int)fromBase;
        int tb = (int)toBase;
        long number = Convert.ToInt64(value.Trim(), fb);
        return tb switch
        {
            2 => Convert.ToString(number, 2),
            8 => Convert.ToString(number, 8),
            10 => number.ToString(),
            16 => number.ToString("X"),
            _ => ConvertToBase(number, tb)
        };
    }

    /// <summary>Convert a long to an arbitrary base (2-36) string representation.</summary>
    private static string ConvertToBase(long number, int toBase)
    {
        if (number == 0) return "0";
        const string digits = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        bool negative = number < 0;
        if (negative) number = -number;
        var chars = new System.Collections.Generic.List<char>();
        while (number > 0)
        {
            chars.Add(digits[(int)(number % toBase)]);
            number /= toBase;
        }
        if (negative) chars.Add('-');
        chars.Reverse();
        return new string(chars.ToArray());
    }

    /// <summary>
    /// EXCEPTION-FILE (§15.30): Return name of file that caused last I/O exception.
    /// Returns empty string when no exception is active.
    /// </summary>
    public static string ExceptionFile() => "";

    /// <summary>
    /// EXCEPTION-LOCATION (§15.31): Return location of last exception.
    /// Returns empty string when no exception is active.
    /// </summary>
    public static string ExceptionLocation() => "";

    /// <summary>
    /// EXCEPTION-STATEMENT (§15.32): Return statement name of last exception.
    /// Returns empty string when no exception is active.
    /// </summary>
    public static string ExceptionStatement() => "";

    /// <summary>
    /// EXCEPTION-STATUS (§15.33): Return exception status name.
    /// Returns empty string when no exception is active (no exception = "").
    /// </summary>
    public static string ExceptionStatus() => "";

    // ═══════════════════════════════════════════════════
    // Dispatch — call function by name
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// Dispatch a COBOL intrinsic function call by name.
    /// Returns the result as object (decimal or string).
    /// </summary>
    public static object Call(string functionName, object[] args)
    {
        decimal[] numArgs = args.Where(a => a is decimal).Cast<decimal>().ToArray();
        string[] strArgs = args.Select(a => a?.ToString() ?? "").ToArray();

        // Determine if all arguments are strings (for MAX/MIN/ORD-MAX/ORD-MIN string dispatch)
        bool allStrings = args.Length > 0 && args.All(a => a is string);

        return functionName.ToUpperInvariant() switch
        {
            // Math
            "ABS" => Abs(numArgs[0]),
            "ACOS" => Acos(numArgs[0]),
            "ASIN" => Asin(numArgs[0]),
            "ATAN" => Atan(numArgs[0]),
            "COS" => Cos(numArgs[0]),
            "SIN" => Sin(numArgs[0]),
            "TAN" => Tan(numArgs[0]),
            "SQRT" => Sqrt(numArgs[0]),
            "LOG" => Log(numArgs[0]),
            "LOG10" => Log10(numArgs[0]),
            "EXP" => Exp(numArgs[0]),
            "EXP10" => Exp10(numArgs[0]),
            "FACTORIAL" => Factorial(numArgs[0]),
            "MOD" => Mod(numArgs[0], numArgs[1]),
            "REM" => Rem(numArgs[0], numArgs[1]),
            "INTEGER" => Integer(numArgs[0]),
            "INTEGER-PART" => IntegerPart(numArgs[0]),
            "FRACTION-PART" => FractionPart(numArgs[0]),
            "SIGN" => Sign(numArgs[0]),
            "PI" => Pi(),
            "E" => E(),
            "RANDOM" when numArgs.Length > 0 => Random(numArgs[0]),
            "RANDOM" => Random(),

            // Aggregates (string-aware dispatch)
            "MAX" when allStrings => (object)MaxString(strArgs),
            "MAX" => Max(numArgs),
            "MIN" when allStrings => (object)MinString(strArgs),
            "MIN" => Min(numArgs),
            "SUM" => Sum(numArgs),
            "MEAN" => Mean(numArgs),
            "MEDIAN" => Median(numArgs),
            "MIDRANGE" => Midrange(numArgs),
            "RANGE" => Range(numArgs),
            "VARIANCE" => Variance(numArgs),
            "STANDARD-DEVIATION" => StandardDeviation(numArgs),
            "ORD-MAX" when allStrings => OrdMaxString(strArgs),
            "ORD-MAX" => OrdMax(numArgs),
            "ORD-MIN" when allStrings => OrdMinString(strArgs),
            "ORD-MIN" => OrdMin(numArgs),

            // String
            "LOWER-CASE" => LowerCase(strArgs[0]),
            "UPPER-CASE" => UpperCase(strArgs[0]),
            "REVERSE" => Reverse(strArgs[0]),
            "TRIM" when strArgs.Length > 1 && strArgs[1].Equals("LEADING", StringComparison.OrdinalIgnoreCase)
                => TrimLeading(strArgs[0]),
            "TRIM" when strArgs.Length > 1 && strArgs[1].Equals("TRAILING", StringComparison.OrdinalIgnoreCase)
                => TrimTrailing(strArgs[0]),
            "TRIM" => Trim(strArgs[0]),
            "LENGTH" => Length(strArgs[0]),
            "BYTE-LENGTH" => ByteLength(strArgs[0]),
            "CONCATENATE" => Concatenate(strArgs),
            "CONCAT" => Concatenate(strArgs),
            "SUBSTITUTE" when strArgs.Length >= 3 => Substitute(strArgs[0], strArgs[1..]),
            "CHAR" => Char(numArgs[0]),
            "ORD" => Ord(strArgs[0]),

            // Date/Time
            "CURRENT-DATE" => CurrentDate(),
            "DATE-OF-INTEGER" => DateOfInteger(numArgs[0]),
            "INTEGER-OF-DATE" => IntegerOfDate(numArgs[0]),
            "DAY-OF-INTEGER" => DayOfInteger(numArgs[0]),
            "INTEGER-OF-DAY" => IntegerOfDay(numArgs[0]),
            "DATE-TO-YYYYMMDD" when numArgs.Length >= 3 => DateToYyyymmdd(numArgs[0], numArgs[1], numArgs[2]),
            "DATE-TO-YYYYMMDD" when numArgs.Length >= 2 => DateToYyyymmdd(numArgs[0], numArgs[1]),
            "DATE-TO-YYYYMMDD" => DateToYyyymmdd(numArgs[0]),
            "YEAR-TO-YYYY" when numArgs.Length >= 3 => YearToYyyy(numArgs[0], numArgs[1], numArgs[2]),
            "YEAR-TO-YYYY" when numArgs.Length >= 2 => YearToYyyy(numArgs[0], numArgs[1]),
            "YEAR-TO-YYYY" => YearToYyyy(numArgs[0]),
            "DAY-TO-YYYYDDD" when numArgs.Length >= 3 => DayToYyyyddd(numArgs[0], numArgs[1], numArgs[2]),
            "DAY-TO-YYYYDDD" when numArgs.Length >= 2 => DayToYyyyddd(numArgs[0], numArgs[1]),
            "DAY-TO-YYYYDDD" => DayToYyyyddd(numArgs[0]),
            "WHEN-COMPILED" => WhenCompiled(),
            "SECONDS-PAST-MIDNIGHT" => SecondsPastMidnight(),

            // Financial
            "ANNUITY" => Annuity(numArgs[0], numArgs[1]),
            "PRESENT-VALUE" when numArgs.Length > 1 => PresentValue(numArgs[0], numArgs[1..]),

            // Numeric
            "NUMVAL" when strArgs.Length > 0 => NumericValue(strArgs[0]),
            "NUMVAL-C" when strArgs.Length >= 2 => NumericValueC(strArgs[0], strArgs[1]),
            "NUMVAL-C" when strArgs.Length > 0 => NumericValueC(strArgs[0]),

            // COBOL-2002+ functions
            "FIND-STRING" when strArgs.Length >= 2 => FindString(strArgs[0], strArgs[1]),
            "TEST-DATE-YYYYMMDD" => TestDateYyyymmdd(numArgs[0]),
            "TEST-DAY-YYYYDDD" => TestDayYyyyddd(numArgs[0]),
            "TEST-NUMVAL" when strArgs.Length > 0 => TestNumval(strArgs[0]),
            "TEST-NUMVAL-C" when strArgs.Length > 0 => TestNumvalC(strArgs[0]),
            "NUMVAL-F" when strArgs.Length > 0 => NumvalF(strArgs[0]),
            "COMBINED-DATETIME" => CombinedDatetime(numArgs[0], numArgs[1]),
            "BOOLEAN-OF-INTEGER" => BooleanOfInteger(numArgs[0], numArgs[1]),
            "INTEGER-OF-BOOLEAN" when strArgs.Length > 0 => IntegerOfBoolean(strArgs[0]),
            "FORMATTED-CURRENT-DATE" => FormattedCurrentDate(strArgs.Length > 0 ? strArgs[0] : ""),
            "FORMATTED-DATE" when numArgs.Length > 0 => FormattedDate(strArgs.Length > 0 ? strArgs[0] : "", numArgs[0]),
            "FORMATTED-TIME" when numArgs.Length > 0 => FormattedTime(strArgs.Length > 0 ? strArgs[0] : "", numArgs[0]),
            "HIGHEST-ALGEBRAIC" => HighestAlgebraic(numArgs[0]),
            "LOWEST-ALGEBRAIC" => LowestAlgebraic(numArgs[0]),
            "MODULE-NAME" => ModuleName(),

            // COBOL-2002+ stubs
            "LOCALE-COMPARE" when strArgs.Length >= 2 => LocaleCompare(strArgs[0], strArgs[1]),
            "LOCALE-DATE" => LocaleDate(numArgs[0]),
            "LOCALE-TIME" => LocaleTime(numArgs[0]),
            "LOCALE-TIME-FROM-SECONDS" => LocaleTimeFromSeconds(numArgs[0]),
            "STANDARD-COMPARE" when strArgs.Length >= 2 => StandardCompare(strArgs[0], strArgs[1]),
            "DISPLAY-OF" when strArgs.Length > 0 => DisplayOf(strArgs[0]),
            "NATIONAL-OF" when strArgs.Length > 0 => NationalOf(strArgs[0]),
            "CHAR-NATIONAL" => CharNational(numArgs[0]),
            "CONVERT" when strArgs.Length >= 3 => ConvertEncoding(strArgs[0], strArgs[1], strArgs[2]),
            "BASECONVERT" when strArgs.Length > 0 && numArgs.Length >= 2 => Baseconvert(strArgs[0], numArgs[0], numArgs[1]),
            "EXCEPTION-FILE" => ExceptionFile(),
            "EXCEPTION-LOCATION" => ExceptionLocation(),
            "EXCEPTION-STATEMENT" => ExceptionStatement(),
            "EXCEPTION-STATUS" => ExceptionStatus(),

            _ => 0m // Unknown function returns 0
        };
    }

    private static int year4(DateTime date) => date.Year;
}
