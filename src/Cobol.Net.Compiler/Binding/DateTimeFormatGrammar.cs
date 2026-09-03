// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding;

/// <summary>The three things a §15.3.1–§15.3.3 format string can be.</summary>
internal enum DateTimeFormatKind { Date, Time, Combined }

/// <summary>The ZONE of a format's time portion (ISO §15.3.3.4–§15.3.3.6): a plain common time format
/// (<c>Local</c>), one suffixed <c>Z</c> (<c>Utc</c>), or one carrying an explicit <c>+hhmm</c>/<c>+hh:mm</c>
/// subformat (<c>Offset</c>). <c>None</c> is a DATE format, which has no time portion at all — kept distinct from
/// <c>Local</c> because §15.40.3 r6 / §15.41.3 r5 are predicates on the TIME portion, and collapsing the two would
/// make a date format silently answer a question about a time.</summary>
internal enum DateTimeZone { None, Local, Utc, Offset }

/// <summary>What <see cref="DateTimeFormatGrammar.Describe"/> knows about a legal §15.3 format: its KIND (the
/// §15.39/40/41/48/79/92 rule-2 screen, COBOLNET1631) and its time portion's ZONE (the §15.40.3 r6 / §15.41.3 r5
/// screen, COBOLNET1633).</summary>
internal readonly record struct DateTimeFormatInfo(DateTimeFormatKind Kind, DateTimeZone Zone);

/// <summary>
/// THE RECOGNIZER for the ISO §15.3.1–§15.3.3 date, time and combined date-and-time formats — the answer to
/// "is this string one of the formats the standard defines?", which nothing previously asked (fix-queue PB11).
///
/// <para>⛔ <b>WHY A RECOGNIZER AND NOT MORE CHECKS.</b> <c>CobolDate.Tokenize</c> validates a format
/// CHARACTER-WISE: each character belongs to a known class and each run has a permitted width. That accepts any
/// string assembled from legal pieces, so §15.39.3 r2 ("the content of argument-1 shall be a date format"),
/// §15.41.3 r2 (a time format) and §15.40.3 r2 (a combined format) were enforced NOWHERE, and the failure mode
/// was a FABRICATED value rather than over-acceptance:
/// <code>
///   FUNCTION FORMATTED-DATE("hhmmss" 100000)             -> "000000"     a TIME format in a DATE function
///   FUNCTION FORMATTED-DATE("YYYY-MMDD" 100000)          -> "1874-1016"  an extended/basic CHIMERA
///   FUNCTION FORMATTED-DATE("NONSENSE" 100000)           -> "      "     garbage in, spaces out
///   FUNCTION FORMATTED-TIME("YYYYMMDD" 3600)             -> "16010101"   a DATE format in a TIME function
///   FUNCTION FORMATTED-DATETIME("YYYY-MM-DDThhmmss" …)   -> extended date + basic time
/// </code>
/// The set of legal formats is CLOSED and small, so membership is the right test and no amount of per-character
/// checking substitutes for it.</para>
///
/// <para><b>The grammar, read from the standard.</b> §15.3.1.1: "COBOL supports six date formats: basic and
/// extended formats for calendar dates, basic and extended formats for ordinal dates, and basic and extended
/// formats for week dates." §15.3.2: four common-time shapes × {local, UTC, offset} = twelve time formats.
/// §15.3.3.7: a combined format is a date format, an uppercase <c>T</c>, and a time format — <b>basic with basic
/// and extended with extended</b>, which is the rule the chimera above breaks.</para>
///
/// <para><b>⚠ BASIC AND EXTENDED NEVER MIX</b>, and that is the whole reason this is a recognizer rather than a
/// set of independent field checks: every wrong example above is assembled from individually legal subfields.</para>
///
/// <para>§15.39.3 r1 makes argument-1 a LITERAL, so this runs at BIND time and the diagnostic is a compile
/// error rather than a runtime surprise.</para>
/// </summary>
internal static class DateTimeFormatGrammar
{
    /// <summary>The implementor-defined maximum number of digit positions in the seconds fraction
    /// (§15.3.3.2: "The implementor defines the maximum … that maximum shall be greater than or equal to nine").
    /// <para>⛔ 18, NOT 9 AND NOT UNBOUNDED. Nine is the standard's floor and choosing it would reject formats
    /// this compiler accepts today. Unbounded is what it did before, and a fraction of 19 or more digits
    /// overflows <c>(long)Pow10.AsWide(width)</c> in the emitter and prints garbage (fix-queue PB16) — so the
    /// bound is placed exactly where the representation stops being exact, which is what makes it a real
    /// implementor choice rather than an arbitrary cap. Documented in <c>docs/CONFORMANCE.md</c>.</para></summary>
    internal const int MaxFractionDigits = 18;

    private static readonly string[] BasicDates = ["YYYYMMDD", "YYYYDDD", "YYYYWwwD"];
    private static readonly string[] ExtendedDates = ["YYYY-MM-DD", "YYYY-DDD", "YYYY-Www-D"];

    /// <summary>Classify <paramref name="format"/>, or null when it is not a legal §15.3 format at all.
    /// <paramref name="decimalComma"/> selects the seconds-fraction separator: §15.3.3.2 makes it a comma under
    /// <c>DECIMAL-POINT IS COMMA</c> and a period otherwise.</summary>
    internal static DateTimeFormatKind? Classify(string format, bool decimalComma) =>
        Describe(format, decimalComma)?.Kind;

    /// <summary>
    /// <see cref="Classify"/> plus the TIME PORTION'S ZONE — the fact §15.40.3 r6 and §15.41.3 r5 turn on:
    /// "argument-4 [resp. argument-3] shall not be specified if the time portion of the format in argument-1 is
    /// neither a UTC format nor an offset format." Those rules are decidable at BIND time, because §15.40.3 r1 /
    /// §15.41.3 r1 make argument-1 a LITERAL and the argument's presence is syntactic.
    /// <para>⛔ THE ZONE WAS ALWAYS COMPUTED HERE AND THEN THROWN AWAY. <see cref="IsTimeOfWidth"/> already
    /// decided local-vs-UTC-vs-offset in order to answer "is this a time format" at all; it simply collapsed the
    /// three into a bool. Returning what the recogniser already knows is why this closes the r6/r5 pair without
    /// a second parser — the alternative, re-inspecting the format string at the call site, would be the same
    /// rule written down twice (feedback_one_rule_one_place).</para>
    /// </summary>
    internal static DateTimeFormatInfo? Describe(string format, bool decimalComma)
    {
        char sep = decimalComma ? ',' : '.';
        // A DATE format has no time portion at all, so it has no zone — and r6/r5 are about the TIME portion,
        // which is why None is a distinct value rather than an alias for Local.
        if (IsDate(format)) return new DateTimeFormatInfo(DateTimeFormatKind.Date, DateTimeZone.None);
        if (ZoneOf(format, sep) is { } tz) return new DateTimeFormatInfo(DateTimeFormatKind.Time, tz);

        // §15.3.3.7 — a combined format is <date>T<time>, basic with basic and extended with extended. 'T' occurs
        // in no other position of any format, so the first one is the separator.
        int t = format.IndexOf('T');
        if (t > 0)
        {
            string d = format[..t], time = format[(t + 1)..];
            if (Array.IndexOf(BasicDates, d) >= 0 && ZoneOfWidth(time, extended: false, sep) is { } bz)
                return new DateTimeFormatInfo(DateTimeFormatKind.Combined, bz);
            if (Array.IndexOf(ExtendedDates, d) >= 0 && ZoneOfWidth(time, extended: true, sep) is { } xz)
                return new DateTimeFormatInfo(DateTimeFormatKind.Combined, xz);
        }
        return null;
    }

    /// <summary>The zone of a time format at either width, or null when it is not a time format.</summary>
    private static DateTimeZone? ZoneOf(string s, char sep) =>
        ZoneOfWidth(s, false, sep) ?? ZoneOfWidth(s, true, sep);

    private static bool IsDate(string s) => Array.IndexOf(BasicDates, s) >= 0 || Array.IndexOf(ExtendedDates, s) >= 0;

    /// <summary>§15.3.3.4–§15.3.3.6: a time format is a common time format, optionally followed by <c>Z</c> (UTC)
    /// or by an offset subformat — and WHICH of the three it is, since §15.40.3 r6 / §15.41.3 r5 turn on exactly
    /// that. Null when it is not a time format at this width. The offset subformat's own width must MATCH the
    /// common part's — §15.3.3.6.1 gives the basic subformat five characters (<c>+hhmm</c>) and the extended one
    /// six (<c>+hh:mm</c>).</summary>
    private static DateTimeZone? ZoneOfWidth(string s, bool extended, char sep)
    {
        if (IsCommonTime(s, extended, sep)) return DateTimeZone.Local;
        if (s.Length > 0 && s[^1] == 'Z' && IsCommonTime(s[..^1], extended, sep)) return DateTimeZone.Utc;
        string offset = extended ? "+hh:mm" : "+hhmm";
        return s.EndsWith(offset, StringComparison.Ordinal) && IsCommonTime(s[..^offset.Length], extended, sep)
            ? DateTimeZone.Offset
            : null;
    }

    /// <summary>§15.3.3.1/§15.3.3.2: <c>hhmmss</c> or <c>hh:mm:ss</c>, optionally followed by the decimal
    /// separator and at least one further <c>s</c> for the seconds fraction.</summary>
    private static bool IsCommonTime(string s, bool extended, char sep)
    {
        string head = extended ? "hh:mm:ss" : "hhmmss";
        if (s == head) return true;
        if (!s.StartsWith(head, StringComparison.Ordinal)) return false;
        string rest = s[head.Length..];
        // The separator plus at least one fraction digit, and no more than the implementor-defined maximum.
        if (rest.Length < 2 || rest[0] != sep) return false;
        string fraction = rest[1..];
        if (fraction.Length > MaxFractionDigits) return false;
        foreach (char c in fraction) if (c != 's') return false;
        return true;
    }
}
