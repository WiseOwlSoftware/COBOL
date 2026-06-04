// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using System.Collections.Generic;

namespace CobolSharp.Runtime;

/// <summary>
/// Runtime support for the COBOL Report Writer Control System (RWCS), ISO 1989 §14.9. INITIATE/GENERATE/
/// TERMINATE maintain a per-report LINE-COUNTER and PAGE-COUNTER, compose each report line into a line
/// buffer (SOURCE fields placed by COLUMN), and write it through the report's file via
/// <see cref="FileRuntime.WriteAdvancing"/> (the same print-control path as WRITE … AFTER ADVANCING).
/// </summary>
public static class ReportWriterRuntime
{
    private sealed class ReportContext
    {
        public string FileName = "";
        public byte[] Line = Array.Empty<byte>();
        public int LineCounter;
        public int PageCounter;
        public int PageLimit;
        public int LastDetail;
    }

    private static readonly Dictionary<string, ReportContext> _reports =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Reset all report state (called from FileRuntime.Init at run-unit start).</summary>
    public static void Reset() => _reports.Clear();

    /// <summary>
    /// INITIATE report-name (§14.9.21): begin report processing. Resets PAGE-COUNTER to 1 and LINE-COUNTER
    /// to 0 and allocates a space-filled line buffer. The report's file must already be OPEN OUTPUT.
    /// </summary>
    public static void InitiateReport(string reportName, string fileName, int lineWidth, int pageLimit, int lastDetail)
    {
        if (lineWidth < 1) lineWidth = 132;
        var line = new byte[lineWidth];
        for (int i = 0; i < line.Length; i++) line[i] = (byte)' ';
        _reports[reportName] = new ReportContext
        {
            FileName = fileName,
            Line = line,
            LineCounter = 0,
            PageCounter = 1,
            PageLimit = pageLimit > 0 ? pageLimit : int.MaxValue,
            LastDetail = lastDetail > 0 ? lastDetail : (pageLimit > 0 ? pageLimit : int.MaxValue),
        };
    }

    /// <summary>Clear the report line buffer to spaces before a group's fields are placed.</summary>
    public static void BeginLine(string reportName)
    {
        if (_reports.TryGetValue(reportName, out var ctx))
            for (int i = 0; i < ctx.Line.Length; i++) ctx.Line[i] = (byte)' ';
    }

    /// <summary>
    /// Place a SOURCE field's bytes into the line buffer at COLUMN (1-based), left-justified and truncated
    /// to the report field's PIC width and the buffer extent (an alphanumeric move; the buffer is
    /// pre-filled with spaces by <see cref="BeginLine"/>, providing right padding).
    /// </summary>
    public static void PlaceField(string reportName, int column, int fieldWidth, byte[] src, int srcOffset, int srcLen)
    {
        if (!_reports.TryGetValue(reportName, out var ctx)) return;
        int dst = column >= 1 ? column - 1 : 0;
        int n = srcLen;
        if (fieldWidth > 0 && n > fieldWidth) n = fieldWidth;
        if (n > ctx.Line.Length - dst) n = ctx.Line.Length - dst;
        if (n > 0) Array.Copy(src, srcOffset, ctx.Line, dst, n);
    }

    /// <summary>
    /// GENERATE one print line of a group (§14.9.19): advance LINE-COUNTER by the LINE NUMBER value (PLUS n
    /// or NEXT PAGE), perform a page break if the line would pass LAST DETAIL, write the composed line
    /// through the report's file, and update LINE-COUNTER. (The minimal RWCS — no CONTROL/PAGE heading or
    /// footing groups — is sufficient for the foundational tests; those are layered on later.)
    /// </summary>
    public static void EmitGroup(string reportName, int advance, bool nextPage)
    {
        if (!_reports.TryGetValue(reportName, out var ctx)) return;
        if (advance < 1) advance = 1;
        int newLine = ctx.LineCounter + advance;
        if (nextPage || newLine > ctx.LastDetail)
        {
            // Page break: form-feed to a fresh page and reset LINE-COUNTER (§14.9.19 GR). The form-feed is
            // a PAGE advance (-1) carrying no record; the group line then prints at the top of the page.
            FileRuntime.WriteAdvancing(ctx.FileName, Array.Empty<byte>(), 0, 0, -1, isBefore: false);
            ctx.PageCounter++;
            ctx.LineCounter = 0;
            advance = 1;
            newLine = 1;
        }
        FileRuntime.WriteAdvancing(ctx.FileName, ctx.Line, 0, ctx.Line.Length, advance, isBefore: false);
        ctx.LineCounter = newLine;
    }

    /// <summary>TERMINATE report-name (§14.9.62): end report processing for this report.</summary>
    public static void TerminateReport(string reportName) => _reports.Remove(reportName);

    /// <summary>LINE-COUNTER special register read (§8.4.3.15): current line within the page.</summary>
    public static decimal GetLineCounter(string reportName)
        => _reports.TryGetValue(reportName, out var ctx) ? ctx.LineCounter : 0m;

    /// <summary>PAGE-COUNTER special register read (§8.4.3.15): current page number (1-based).</summary>
    public static decimal GetPageCounter(string reportName)
        => _reports.TryGetValue(reportName, out var ctx) ? ctx.PageCounter : 0m;
}
