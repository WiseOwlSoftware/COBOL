// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using System.Collections.Generic;

namespace CobolSharp.Runtime;

/// <summary>
/// Runtime support for the COBOL Report Writer Control System (RWCS), ISO 1989 §14.9. INITIATE/GENERATE/
/// TERMINATE maintain a per-report LINE-COUNTER and PAGE-COUNTER, compose each DETAIL line in a line buffer
/// (SOURCE fields placed by COLUMN from emitted code), and present it through the report's file via
/// <see cref="FileRuntime.WriteAdvancing"/>. The RWCS auto-presents the PAGE HEADING at each page start
/// (the first GENERATE and after every page advance) and the PAGE FOOTING at page overflow, positioning the
/// first body group of a page at FIRST DETAIL (ISO §13.18.35.4.5b.3 / §13.18.39).
///
/// Page-group (heading/footing) fields whose value is a VALUE literal or SOURCE LINE-COUNTER/PAGE-COUNTER
/// are composed by the runtime from a registered field plan; page groups sourcing program data are a later
/// increment.
/// </summary>
public static class ReportWriterRuntime
{
    /// <summary>A field of a page heading/footing group the runtime composes itself: a VALUE literal
    /// (Kind 0) or SOURCE LINE-COUNTER (Kind 1) / PAGE-COUNTER (Kind 2).</summary>
    private sealed class FieldPlan
    {
        public int Column;
        public int Width;
        public int Kind;       // 0 = literal, 1 = LINE-COUNTER, 2 = PAGE-COUNTER
        public string Literal = "";
    }

    private sealed class ReportContext
    {
        public string FileName = "";
        public byte[] Line = Array.Empty<byte>();
        public int LineCounter;
        public int PageCounter;
        public int LastDetail;
        public int FirstDetail;
        public bool Started;                 // a body group has been GENERATEd (chronologically first done)
        public int HeadingLine;              // LINE of the PAGE HEADING group (0 = none)
        public List<FieldPlan>? HeadingFields;
        public int FootingLine;              // LINE of the PAGE FOOTING group (0 = none)
        public List<FieldPlan>? FootingFields;
    }

    private static readonly Dictionary<string, ReportContext> _reports =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Reset all report state (called from FileRuntime.Init at run-unit start).</summary>
    public static void Reset() => _reports.Clear();

    /// <summary>
    /// INITIATE report-name (§14.9.21): begin report processing. Resets PAGE-COUNTER to 1 and LINE-COUNTER
    /// to 0 and allocates a space-filled line buffer. The report's file must already be OPEN OUTPUT.
    /// </summary>
    public static void InitiateReport(string reportName, string fileName, int lineWidth,
        int pageLimit, int lastDetail, int firstDetail)
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
            LastDetail = lastDetail > 0 ? lastDetail : (pageLimit > 0 ? pageLimit : int.MaxValue),
            FirstDetail = firstDetail > 0 ? firstDetail : 1,
            Started = false,
        };
    }

    /// <summary>Register the LINE of a page heading (isFooting=false) or page footing (true) group.</summary>
    public static void RegisterPageGroup(string reportName, bool isFooting, int lineValue)
    {
        if (!_reports.TryGetValue(reportName, out var ctx)) return;
        if (isFooting) { ctx.FootingLine = lineValue; ctx.FootingFields = []; }
        else { ctx.HeadingLine = lineValue; ctx.HeadingFields = []; }
    }

    /// <summary>Register one runtime-composed field of a page heading/footing group (see <see cref="FieldPlan"/>).</summary>
    public static void RegisterPageField(string reportName, bool isFooting, int column, int width, int kind, string literal)
    {
        if (!_reports.TryGetValue(reportName, out var ctx)) return;
        var list = isFooting ? ctx.FootingFields : ctx.HeadingFields;
        list?.Add(new FieldPlan { Column = column, Width = width, Kind = kind, Literal = literal ?? "" });
    }

    /// <summary>Clear the report line buffer to spaces before a group's SOURCE fields are placed.</summary>
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
        Place(ctx.Line, column, fieldWidth, src, srcOffset, srcLen);
    }

    private static void Place(byte[] dstBuf, int column, int fieldWidth, byte[] src, int srcOffset, int srcLen)
    {
        int dst = column >= 1 ? column - 1 : 0;
        int n = srcLen;
        if (fieldWidth > 0 && n > fieldWidth) n = fieldWidth;
        if (n > dstBuf.Length - dst) n = dstBuf.Length - dst;
        if (n > 0) Array.Copy(src, srcOffset, dstBuf, dst, n);
    }

    /// <summary>Place a constant VALUE literal into the active line buffer at COLUMN (a body-group field whose
    /// value is a VALUE clause rather than a SOURCE — ISO §13.18.63), left-justified, truncated to the field
    /// width. The buffer is pre-filled with spaces by <see cref="BeginLine"/>.</summary>
    public static void PlaceLiteralField(string reportName, int column, int fieldWidth, string text)
    {
        if (!_reports.TryGetValue(reportName, out var ctx)) return;
        var bytes = System.Text.Encoding.ASCII.GetBytes(text ?? "");
        Place(ctx.Line, column, fieldWidth, bytes, 0, bytes.Length);
    }

    /// <summary>
    /// GENERATE a DETAIL group's line (§14.9.19). The detail's SOURCE fields are already in the line buffer.
    /// Performs the RWCS page mechanics first: on the chronologically first GENERATE, or after a page
    /// overflow, present the PAGE FOOTING (overflow only) + form-feed + PAGE-COUNTER increment, present the
    /// PAGE HEADING, and position the first body group of the page at FIRST DETAIL; otherwise advance by the
    /// LINE NUMBER value. Then write the detail line and update LINE-COUNTER.
    /// </summary>
    public static void EmitGroup(string reportName, int advance, bool nextPage)
    {
        if (!_reports.TryGetValue(reportName, out var ctx)) return;
        if (advance < 1) advance = 1;

        bool pageStart = false;
        if (!ctx.Started)
        {
            // Chronologically first body group since INITIATE: no page-fit test, no footing (§14.9.16.4).
            ctx.Started = true;
            pageStart = true;
        }
        else if (nextPage || ctx.LineCounter + advance > ctx.LastDetail)
        {
            // Page overflow (§13.18.35.4.4): present the PAGE FOOTING on the current page, advance to a
            // fresh page (form-feed), increment PAGE-COUNTER and reset LINE-COUNTER.
            PresentPageGroup(ctx, ctx.FootingFields, ctx.FootingLine);
            FileRuntime.WriteAdvancing(ctx.FileName, Array.Empty<byte>(), 0, 0, -1, isBefore: false);
            ctx.PageCounter++;
            ctx.LineCounter = 0;
            pageStart = true;
        }

        if (pageStart)
        {
            // Present the PAGE HEADING, then position the first body group of the page at FIRST DETAIL
            // (§13.18.35.4.5b.3): LINE-COUNTER is set so this group's relative advance lands on FIRST DETAIL.
            PresentPageGroup(ctx, ctx.HeadingFields, ctx.HeadingLine);
            ctx.LineCounter = ctx.FirstDetail - advance;
            if (ctx.LineCounter < 0) ctx.LineCounter = 0;
        }

        int newLine = ctx.LineCounter + advance;
        FileRuntime.WriteAdvancing(ctx.FileName, ctx.Line, 0, ctx.Line.Length, advance, isBefore: false);
        ctx.LineCounter = newLine;
    }

    /// <summary>Present a runtime-composed page heading/footing group at its absolute LINE, evaluating each
    /// field (literal / LINE-COUNTER / PAGE-COUNTER) against the counter values at presentation time.</summary>
    private static void PresentPageGroup(ReportContext ctx, List<FieldPlan>? fields, int lineValue)
    {
        if (fields == null || fields.Count == 0 || lineValue <= 0) return;
        int adv = lineValue - ctx.LineCounter;
        if (adv < 1) adv = 1;
        ctx.LineCounter = lineValue;   // set so SOURCE LINE-COUNTER on this group reflects its own line

        var buf = new byte[ctx.Line.Length];
        for (int i = 0; i < buf.Length; i++) buf[i] = (byte)' ';
        foreach (var f in fields)
        {
            string text = f.Kind switch
            {
                1 => FormatNumeric(ctx.LineCounter, f.Width),
                2 => FormatNumeric(ctx.PageCounter, f.Width),
                _ => f.Literal,
            };
            var bytes = System.Text.Encoding.ASCII.GetBytes(text);
            Place(buf, f.Column, f.Width, bytes, 0, bytes.Length);
        }
        FileRuntime.WriteAdvancing(ctx.FileName, buf, 0, buf.Length, adv, isBefore: false);
    }

    /// <summary>Format a counter value into a PIC 9(width) image (zero-padded, rightmost digits).</summary>
    private static string FormatNumeric(int value, int width)
    {
        if (width <= 0) return value.ToString();
        string s = value.ToString();
        if (s.Length > width) return s.Substring(s.Length - width);
        return s.PadLeft(width, '0');
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
