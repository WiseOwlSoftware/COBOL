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
    public static decimal Mod(decimal value, decimal divisor) => value % divisor;
    public static decimal Rem(decimal value, decimal divisor) => value - divisor * Math.Truncate(value / divisor);
    public static decimal Integer(decimal value) => Math.Truncate(value);
    public static decimal IntegerPart(decimal value) => Math.Truncate(value);
    public static decimal FractionPart(decimal value) => value - Math.Truncate(value);
    public static decimal Sign(decimal value) => value > 0 ? 1 : value < 0 ? -1 : 0;
    public static decimal Pi() => 3.14159265358979323846m;

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

    // ═══════════════════════════════════════════════════
    // String functions
    // ═══════════════════════════════════════════════════

    public static string LowerCase(string value) => value.ToLowerInvariant();
    public static string UpperCase(string value) => value.ToUpperInvariant();
    public static string Reverse(string value) => new string(value.Reverse().ToArray());
    public static string Trim(string value) => value.Trim();
    public static string TrimLeading(string value) => value.TrimStart();
    public static string TrimTrailing(string value) => value.TrimEnd();
    public static decimal Length(string value) => value.Length;
    public static decimal ByteLength(string value) => Encoding.ASCII.GetByteCount(value);
    public static string Concatenate(params string[] values) => string.Concat(values);
    public static string Substitute(string source, string from, string to) =>
        source.Replace(from, to, StringComparison.Ordinal);
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

    public static decimal DateToYyyymmdd(decimal yymmdd, decimal windowBase)
    {
        int d = (int)yymmdd;
        int yy = d / 10000;
        int window = (int)windowBase;
        int century = yy >= (window % 100) ? (window / 100) * 100 : ((window / 100) + 1) * 100;
        return (century + yy) * 10000 + d % 10000;
    }

    public static decimal YearToYyyy(decimal yy, decimal windowBase)
    {
        int year2 = (int)yy;
        int window = (int)windowBase;
        return year2 >= (window % 100) ? (window / 100) * 100 + year2 : ((window / 100) + 1) * 100 + year2;
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

    public static string WhenCompiled() => DateTime.Now.ToString("yyyyMMddHHmmssff", CultureInfo.InvariantCulture);
    public static decimal NumericValue(string text)
    {
        if (decimal.TryParse(text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            return result;
        return 0;
    }

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

            // Aggregates
            "MAX" => Max(numArgs),
            "MIN" => Min(numArgs),
            "SUM" => Sum(numArgs),
            "MEAN" => Mean(numArgs),
            "MEDIAN" => Median(numArgs),
            "MIDRANGE" => Midrange(numArgs),
            "RANGE" => Range(numArgs),
            "VARIANCE" => Variance(numArgs),
            "STANDARD-DEVIATION" => StandardDeviation(numArgs),
            "ORD-MAX" => OrdMax(numArgs),
            "ORD-MIN" => OrdMin(numArgs),

            // String
            "LOWER-CASE" => LowerCase(strArgs[0]),
            "UPPER-CASE" => UpperCase(strArgs[0]),
            "REVERSE" => Reverse(strArgs[0]),
            "TRIM" => strArgs.Length > 1 ? Trim(strArgs[0]) : Trim(strArgs[0]),
            "LENGTH" => Length(strArgs[0]),
            "BYTE-LENGTH" => ByteLength(strArgs[0]),
            "CONCATENATE" => Concatenate(strArgs),
            "SUBSTITUTE" when strArgs.Length >= 3 => Substitute(strArgs[0], strArgs[1], strArgs[2]),
            "CHAR" => Char(numArgs[0]),
            "ORD" => Ord(strArgs[0]),

            // Date/Time
            "CURRENT-DATE" => CurrentDate(),
            "DATE-OF-INTEGER" => DateOfInteger(numArgs[0]),
            "INTEGER-OF-DATE" => IntegerOfDate(numArgs[0]),
            "DAY-OF-INTEGER" => DayOfInteger(numArgs[0]),
            "INTEGER-OF-DAY" => IntegerOfDay(numArgs[0]),
            "DATE-TO-YYYYMMDD" => DateToYyyymmdd(numArgs[0], numArgs[1]),
            "YEAR-TO-YYYY" => YearToYyyy(numArgs[0], numArgs[1]),
            "WHEN-COMPILED" => WhenCompiled(),

            // Financial
            "ANNUITY" => Annuity(numArgs[0], numArgs[1]),
            "PRESENT-VALUE" when numArgs.Length > 1 => PresentValue(numArgs[0], numArgs[1..]),

            // Numeric
            "NUMVAL" when strArgs.Length > 0 => NumericValue(strArgs[0]),
            "NUMVAL-C" when strArgs.Length > 0 => NumericValue(strArgs[0].Replace("$", "").Replace(",", "")),

            _ => 0m // Unknown function returns 0
        };
    }

    private static int year4(DateTime date) => date.Year;
}
