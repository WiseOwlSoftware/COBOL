// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;

namespace CobolNet.Runtime;

/// <summary>
/// The ACCEPT system sources (ISO/IEC 1989:2023 §14.9.1): the Format 1 hardware-device read (an 80-character
/// card-image transfer from standard input, GR1–GR5) and the Format 2 temporal readings (DATE / DAY /
/// DAY-OF-WEEK / TIME as the conceptual unsigned-integer values of GR7–GR12). The COMPILED program calls these;
/// the emitter then stores the result per the statement's rules (device = aligned-left by size, GR3/GR4;
/// temporal = the MOVE rules, GR6).
/// </summary>
public static class AcceptSource
{
    /// <summary>The Format 1 transfer size (ISO §14.9.1.4 GR2 — implementor-defined per device): one
    /// 80-character card-image record, the NIST-proven convention of the legacy engine.</summary>
    public const int RecordSize = 80;

    /// <summary>The ONE clock read every temporal source makes (once per ACCEPT statement): the run unit's
    /// injectable <see cref="IClock"/> (DESIGN-runtime-library §2.7 — the former process-global mutable
    /// <c>Func&lt;DateTime&gt; Now</c> seam is deleted). The default <see cref="SystemClock"/> consults the
    /// <c>COBOLNET_CLOCK</c> environment variable so a test run can pin the clock ACROSS PROCESSES — the
    /// deterministic-clock path — falling back to the local system clock (§14.9.1.4 GR7: "the hardware clock
    /// provides the current date and time"). An in-process test sets <c>RunUnit.Current.Clock</c> instead.</summary>
    private static DateTime Now() => RunUnit.Current.Clock.Now();

    /// <summary>DATE — the conceptual 6-digit unsigned integer YYMMDD (ISO §14.9.1.4 GR7).</summary>
    public static long Date()
    {
        DateTime n = Now();
        return n.Year % 100 * 10000L + n.Month * 100L + n.Day;
    }

    /// <summary>DATE YYYYMMDD — the conceptual 8-digit unsigned integer with a four-digit year
    /// (ISO §14.9.1.4 GR8; COBOL-2002+, edition-gated at bind time).</summary>
    public static long DateYYYYMMDD()
    {
        DateTime n = Now();
        return n.Year * 10000L + n.Month * 100L + n.Day;
    }

    /// <summary>DAY — the conceptual 5-digit unsigned integer YYDDD, DDD the day of year 001–366
    /// (ISO §14.9.1.4 GR9 — the Julian-style date).</summary>
    public static long Day()
    {
        DateTime n = Now();
        return n.Year % 100 * 1000L + n.DayOfYear;
    }

    /// <summary>DAY YYYYDDD — the conceptual 7-digit unsigned integer with a four-digit year
    /// (ISO §14.9.1.4 GR10; COBOL-2002+, edition-gated at bind time).</summary>
    public static long DayYYYYDDD()
    {
        DateTime n = Now();
        return n.Year * 1000L + n.DayOfYear;
    }

    /// <summary>TIME — the conceptual 8-digit unsigned integer HHMMSScc on the 24-hour clock: hours 00–23,
    /// minutes, seconds, and HUNDREDTHS of a second (ISO §14.9.1.4 GR11 — a system unable to provide fractional
    /// seconds would supply 00; this one derives them from the millisecond reading).</summary>
    public static long Time()
    {
        DateTime n = Now();
        return n.Hour * 1000000L + n.Minute * 10000L + n.Second * 100L + n.Millisecond / 10;
    }

    /// <summary>DAY-OF-WEEK — the conceptual 1-digit unsigned integer where 1 IS MONDAY … 7 is Sunday
    /// (ISO §14.9.1.4 GR12); .NET's <c>DayOfWeek</c> starts at Sunday=0, hence the +6 mod 7 remap.</summary>
    public static long DayOfWeek() => ((int)Now().DayOfWeek + 6) % 7 + 1;

    /// <summary>
    /// The Format 1 device transfer (ISO §14.9.1.4 GR1–GR5): read 80-character card-image records from standard
    /// input until <paramref name="width"/> characters are available, and return EXACTLY <paramref name="width"/>
    /// characters. Each record shorter than 80 characters is space-padded to the full record (the card image);
    /// a receiver no larger than one record consumes exactly ONE record (GR3 / GR4b — only the leftmost
    /// characters that fit are kept, the rest of the record is ignored); a larger receiver requests ADDITIONAL
    /// records until filled (GR4a — stored aligned left, the next transfer landing in the unoccupied positions);
    /// end-of-input leaves the unfilled remainder as spaces. A zero-size receiver transfers nothing (GR4b).
    /// The standard input IS the device for both the FROM-omitted default (GR5) and the CONSOLE / SYSIN
    /// mnemonics (§12.3.7 — the implementor's device set; stdin redirection is the process-level seam).
    /// </summary>
    public static string Device(int width)
    {
        if (width <= 0) return "";
        var sb = new StringBuilder(Math.Max(width, RecordSize));
        while (sb.Length < width)
        {
            string? line = Console.In.ReadLine();
            if (line is null) break;                                       // EOF — the tail space-fills (GR4a)
            sb.Append(line.Length < RecordSize ? line.PadRight(RecordSize) : line);
            if (width <= RecordSize) break;                                // fits one record: exactly ONE transfer
        }
        return sb.Length >= width ? sb.ToString(0, width) : sb.ToString().PadRight(width);
    }
}
