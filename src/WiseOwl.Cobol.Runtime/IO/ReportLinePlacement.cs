namespace WiseOwl.Cobol.Runtime.IO;

/// <summary>The vertical regions of a report's page that the line placement reads (ISO §2023:13.18.39.4): whether the
/// report is divided into pages at all, and the HEADING, FIRST DETAIL and FOOTING integers of its PAGE clause.</summary>
public readonly struct ReportPageRegions(bool paged, int heading, int firstDetail, int footing)
{
    /// <summary>The report has a PAGE clause (§2023:13.18.39.4 GR2a: without one it is one page of indefinite length).</summary>
    public bool Paged { get; } = paged;

    /// <summary>The HEADING integer: the first line on which a heading may be printed.</summary>
    public int Heading { get; } = heading;

    /// <summary>The FIRST DETAIL integer: the first line on which a body group may be printed.</summary>
    public int FirstDetail { get; } = firstDetail;

    /// <summary>The FOOTING integer: the last line on which a control footing may be printed.</summary>
    public int Footing { get; } = footing;
}

/// <summary>⛔ THE ONE LINE-PLACEMENT ARITHMETIC OF A REPORT GROUP (kb/Work PB1003). Where each line of a report group
/// is printed is one rule — §2023:13.18.35.4 GR5 for the first line, GR7 with §2023:13.18.38.4 GR12c/GR12d for every
/// later one, and §2023:13.18.37.4 GR3/GR5 for the LINE-COUNTER a report heading's or page footing's NEXT GROUP clause
/// leaves for the group placed after it — and it was written twice: once in the run-time engine
/// (<c>CobolReport.FirstLineTarget</c>, <c>SubsequentTarget</c>, <c>ApplyNextGroup</c>) and once in the binder's
/// compile-time screens (<c>ReportSectionBinder.MinimumLastLine</c>), and the two drifted: the binder never placed a page
/// heading after the report heading or a report footing after the page footing, so at COBOL-85, where the description fixes
/// both, two descriptions the 1985 rules forbid compiled. Both now ask these pure functions: the engine with its live
/// LINE-COUNTER, the binder with the final LINE-COUNTER the description fixes for the group placed before.</summary>
public static class ReportLinePlacement
{
    /// <summary>The line number of a report group's FIRST line (ISO §2023:13.18.35.4 GR5; §1985:XIII.3.10.10 GR3 is
    /// the 1985 report footing's rule): absolute → integer-1 (GR5a); in a report not divided into pages, LINE-COUNTER +
    /// integer-2 (GR5c); otherwise, per group type (GR5b), a report heading at HEADING + integer-2 − 1 (1), a page
    /// heading at the report heading's final LINE-COUNTER + integer-2 when one is on the page, else HEADING + integer-2
    /// − 1 (2), a body group at LINE-COUNTER + integer-2 unless it is the first body group on the page, then FIRST
    /// DETAIL whatever integer-2 says (3), a page footing at FOOTING + integer-2 (4), a report footing at the page
    /// footing's final LINE-COUNTER + integer-2 when one is on the page, else FOOTING + integer-2 (5). A report footing
    /// whose first line is the bare <c>ON NEXT PAGE</c> is on a page by itself and starts at the HEADING integer, its
    /// upper limit (§2023:13.18.57.4 GR7 f), a determination, docs/CONFORMANCE.md).
    /// <para><paramref name="preceding"/> is the final LINE-COUNTER setting of the report group this one is placed
    /// after when GR5 places it after one: the report heading on the page of a page heading, the page footing on the
    /// page of a report footing, the previous body group on the page of a body group, the previous group of a report
    /// not divided into pages (zero, INITIATE's setting, before the first); null when there is none. A report heading
    /// and a page footing are placed from their own regions whatever precedes them.</para></summary>
    public static long FirstLine(ReportGroupKind group, ReportLineKind kind, int value, int relativeBase, bool nextPage,
        ReportPageRegions page, long? preceding)
    {
        if (kind == ReportLineKind.Absolute) return value;                               // GR5a — integer-1
        long relative = RelativeOperand(kind, value, relativeBase);
        if (!page.Paged) return (preceding ?? 0) + relative;                             // GR5c
        return group switch
        {
            ReportGroupKind.ReportHeading => page.Heading + relative - 1,                // GR5b1
            ReportGroupKind.PageHeading => preceding is { } rh
                ? rh + relative                                                          // GR5b2, after the report heading
                : page.Heading + relative - 1,                                           // GR5b2
            ReportGroupKind.PageFooting => page.Footing + relative,                      // GR5b4
            ReportGroupKind.ReportFooting => nextPage
                ? page.Heading                                                           // §2023:13.18.57.4 GR7f
                : preceding is { } pf
                    ? pf + relative                                                      // GR5b5, after the page footing
                    : page.Footing + relative,                                           // GR5b5
            _ => preceding is { } body ? body + relative : page.FirstDetail,             // GR5b3
        };
    }

    /// <summary>The line number of a LATER line of a report group, <paramref name="previous"/> being the line number of
    /// the line printed before it (ISO §2023:13.18.35.4 GR7): absolute → integer-1 (GR7a); a STEP line of a vertically
    /// repeating entry → integer-3 lines beneath the line it occupies in the preceding occurrence, its seeded
    /// <paramref name="anchor"/> plus its displacement (§2023:13.18.38.4 GR12c/GR12d); relative, or a STEP line whose
    /// anchor was never seeded (0: the preceding occurrence was absent, §2023:13.18.41.4 GR2b), → the previous line +
    /// integer-2 (GR7b).</summary>
    public static long SubsequentLine(ReportLineKind kind, int value, int relativeBase, long previous, long anchor) => kind switch
    {
        ReportLineKind.Absolute => value,                                                // GR7a
        ReportLineKind.Step when anchor > 0 => anchor + value,                           // GR12c/GR12d
        _ => previous + RelativeOperand(kind, value, relativeBase),                      // GR7b
    };

    /// <summary>The anchor a line seeds when it lands on <paramref name="target"/> (§2023:13.18.38.4 GR12c/GR12d): the
    /// page line the first occurrence of the line stands on, so a STEP line that re-anchors (its own anchor unseeded)
    /// seeds its line less its displacement.</summary>
    public static long AnchorSeed(ReportLineKind kind, int value, long target) =>
        target - (kind == ReportLineKind.Step ? value : 0);

    /// <summary>The LINE-COUNTER a report heading's or page footing's NEXT GROUP clause leaves after the group's last line
    /// <paramref name="lastLine"/> (ISO §2023:13.18.37.4 GR3 a)/b) and GR5 a)/b); §1985:XIII.3.10.6 GR5 and
    /// §1985:XIII.3.10.9 GR5): absolute →
    /// integer-1; relative → the last line + integer-2; no clause → the last line. The NEXT PAGE form is not a line
    /// number: the report heading is then alone on its page (GR3 c)) and the caller handles the page.</summary>
    public static long AfterHeadingOrFootingNextGroup(ReportNextGroup? nextGroup, long lastLine) => nextGroup switch
    {
        { Kind: ReportNextGroupKind.Absolute, Value: var v } => v,                       // GR3a / GR5a
        { Kind: ReportNextGroupKind.Relative, Value: var v } => lastLine + v,            // GR3b / GR5b
        _ => lastLine,
    };

    /// <summary>The relative operand a line places by when it is measured from the line before it: its own integer-2,
    /// or, for a <see cref="ReportLineKind.Step"/> line whose anchor was never seeded, the integer-2 written on the
    /// entry (<see cref="ReportGroupLine.RelativeBase"/>).</summary>
    public static int RelativeOperand(ReportLineKind kind, int value, int relativeBase) =>
        kind == ReportLineKind.Step ? relativeBase : value;
}
