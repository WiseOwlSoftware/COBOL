// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>The seven report group types (ISO/IEC 1989:2023 §13.18.57 TYPE clause Format 2). DETAIL,
/// CONTROL HEADING and CONTROL FOOTING are the BODY groups (§13.18.57.3 SR15) — the page-fit machinery applies
/// to them; the heading/footing groups are presented by the RWCS at fixed logical points.</summary>
public enum ReportGroupKind { ReportHeading, PageHeading, ControlHeading, Detail, ControlFooting, PageFooting, ReportFooting }

/// <summary>A report line's LINE clause form (ISO §13.18.35): absolute (<c>LINE n</c>) or relative
/// (<c>LINE PLUS n</c>). The <c>NEXT PAGE</c> phrases are rejected loud at bind time (legal, staged —
/// COBOLNET_REPORT_WRITER_DESIGN §6), so the engine never sees them.</summary>
public enum ReportLineKind { Absolute, Relative }

/// <summary>One report line of a report group: its LINE clause and the generated COMPOSE method that renders the
/// line's printable items against the program's live state. Composition runs AT PRESENTATION TIME — after
/// LINE-COUNTER is set to the line's number (ISO §13.18.35.4 GR6) — which is what makes <c>SOURCE IS
/// LINE-COUNTER</c> print the line's OWN number and every SOURCE an implicit MOVE executed "when the line is
/// printed" (§13.18.53.4 GR1/GR3).</summary>
public sealed class ReportGroupLine(ReportLineKind kind, int value, Func<string> compose)
{
    /// <summary>Absolute or relative (ISO §13.18.35).</summary>
    public ReportLineKind Kind { get; } = kind;

    /// <summary>integer-1 (absolute) or integer-2 (relative) of the LINE clause.</summary>
    public int Value { get; } = value;

    /// <summary>The generated compose method — the §13.18.53.4 GR1 implicit MOVEs into one line image.</summary>
    public Func<string> Compose { get; } = compose;
}

/// <summary>One report group (ISO §13.15 report group description entry): its TYPE, name (referenced by GENERATE
/// for a detail, §14.9.16 SR1), control level (CH/CF — index into the report's control hierarchy, −1 otherwise),
/// and its report lines in declaration order.</summary>
public sealed class ReportGroup(ReportGroupKind kind, string name, int controlLevel, ReportGroupLine[] lines)
{
    public ReportGroupKind Kind { get; } = kind;
    public string Name { get; } = name;

    /// <summary>The CH/CF control level — the index into the report's major→minor control list; −1 for
    /// non-control groups (ISO §13.18.16.4 GR1).</summary>
    public int ControlLevel { get; } = controlLevel;

    public ReportGroupLine[] Lines { get; } = lines;

    /// <summary>The USE BEFORE REPORTING declarative hook (ISO §14.9.49 Format 2, GR8/GR9): invoked just before
    /// this report group is produced, in the program instance's context. Null when no declarative names this
    /// group. (The SUPPRESS statement, §14.9.45, is not yet parsed — its suppression flag is staged with it.)</summary>
    public Action? BeforeReporting { get; set; }

    /// <summary>The (column, width) spans of this group's GROUP INDICATE printable items (ISO §13.18.29): they
    /// print on the first presentation after an INITIATE / page advance / control break and are blanked on
    /// every other presentation.</summary>
    public List<(int Column, int Width)> IndicateFields { get; } = [];
}

/// <summary>
/// The per-report Report Writer Control System engine (ISO/IEC 1989:2023 §14.9.16 GENERATE / §14.9.21 INITIATE /
/// §14.9.46 TERMINATE over the §13.18 report description clauses; COBOLNET_REPORT_WRITER_DESIGN). ONE mechanism
/// composes every report line: a generated compose delegate invoked at presentation time (§13.18.53.4 GR3 —
/// the implicit MOVE executes when the line is printed), after LINE-COUNTER is set to the line's number
/// (§13.18.35.4 GR6). There is no byte plan, no registration kinds — the typed-native singular pattern.
/// Physical output goes through the report file's connector via <see cref="CobolFile.WriteAdvancing"/>
/// (a print-control stream); <see cref="_physLine"/> tracks the physical position independently of
/// LINE-COUNTER so a future NEXT GROUP (which moves LINE-COUNTER, §8.4.3.15.4 GR4) cannot corrupt positioning.
/// </summary>
public sealed class CobolReport(
    string name, string fileName, int lineWidth, bool paged,
    int pageLimit, int heading, int firstDetail, int lastControlHeading, int lastDetail, int footing)
{
    /// <summary>The report-name (the RD entry's name).</summary>
    public string Name { get; } = name;

    private readonly string _fileName = fileName;   // the emit-qualified connector name ("PROG::FILE")
    private readonly int _lineWidth = lineWidth;
    private readonly bool _paged = paged;           // PAGE clause present (§13.18.39.4 GR2a — absent ⇒ one page of indefinite length)

    // Page regions (§13.18.39.4 GR2, binder-supplied GR3 defaults).
    private readonly int _pageLimit = pageLimit;
    private readonly int _heading = heading;
    private readonly int _firstDetail = firstDetail;
    private readonly int _lastControlHeading = lastControlHeading;
    private readonly int _lastDetail = lastDetail;
    private readonly int _footing = footing;

    /// <summary>The report's LINE-COUNTER (ISO §8.4.3.15): an unsigned integer; 0 after INITIATE (GR3), set to
    /// each line's number as it is printed (§13.18.35.4 GR6), reset to 0 at every page advance (GR3).</summary>
    public long LineCounter { get; private set; }

    /// <summary>The report's PAGE-COUNTER (ISO §8.4.3.15): 1 after INITIATE (GR2), +1 at each page advance.</summary>
    public long PageCounter { get; private set; }

    private bool _active;                  // INITIATE…TERMINATE state (§14.9.21.4 GR4)
    private bool _started;                 // a GENERATE has executed since INITIATE (§14.9.46.4 GR2/GR3)
    private bool _firstBodySinceInitiate;  // the §13.18.35.4 GR4 page-fit exemption
    private bool _firstBodyOnPage;         // the §13.18.35.4 GR5b3 FIRST DETAIL placement
    private bool _rhOnThisPage;            // a report heading printed on the current page (GR5b2)
    private bool _pfOnThisPage;            // a page footing printed on the current page (GR5b5)
    private bool _indicateFresh;           // GROUP INDICATE freshness (§13.18.29 — run / page / control-group start)
    private int _physLine;                 // physical line position on the current page (0 = top, nothing printed)

    private ReportGroup? _reportHeading, _pageHeading, _pageFooting, _reportFooting;
    private readonly Dictionary<string, ReportGroup> _details = new(StringComparer.OrdinalIgnoreCase);
    private readonly SortedDictionary<int, ReportGroup> _controlHeadings = [];   // by control level (0 = most major)
    private readonly SortedDictionary<int, ReportGroup> _controlFootings = [];

    /// <summary>One CONTROL operand (ISO §13.18.16): FINAL or a data item reached through generated get/set
    /// delegates over the program's typed storage (the character image is the break-compare key — §13.18.16.4
    /// GR3's prior-control save/compare, representation-faithful for every category).</summary>
    private sealed class ControlEntry(bool isFinal, Func<string> get, Action<string> set)
    {
        public bool IsFinal { get; } = isFinal;
        public Func<string> Get { get; } = get;
        public Action<string> Set { get; } = set;
        public string? Prior { get; set; }   // saved at the first GENERATE (§13.18.16.4 GR3); null until then
    }

    private readonly List<ControlEntry> _controls = [];   // major→minor (FINAL, if present, is index 0 — GR2)

    /// <summary>One SUM counter (ISO §13.18.54): an unscaled integer accumulation at the counter's scale (GR1 —
    /// digits derived from the entry's PICTURE), an addend delegate over the program's typed storage, the UPON
    /// detail filter (GR7c2), the RESET control level (GR2; −1 = reset where printed), and the group it prints in.</summary>
    private sealed class SumEntry(Func<long> addend, string[]? uponDetails, int resetLevel, ReportGroup printedIn)
    {
        public long Value;
        public Func<long> Addend { get; } = addend;
        public string[]? UponDetails { get; } = uponDetails;
        public int ResetLevel { get; } = resetLevel;
        public ReportGroup PrintedIn { get; } = printedIn;
    }

    private readonly Dictionary<string, SumEntry> _sums = new(StringComparer.OrdinalIgnoreCase);

    // ── Registration (generated by the compiler in __Activate, once per program instance) ─────────────────────

    /// <summary>Register a report group into its slot (TYPE-driven; §13.18.57.3 SR13/SR14 cap each slot at one,
    /// diagnosed at bind).</summary>
    public void AddGroup(ReportGroup g)
    {
        switch (g.Kind)
        {
            case ReportGroupKind.ReportHeading: _reportHeading = g; break;
            case ReportGroupKind.PageHeading: _pageHeading = g; break;
            case ReportGroupKind.PageFooting: _pageFooting = g; break;
            case ReportGroupKind.ReportFooting: _reportFooting = g; break;
            case ReportGroupKind.ControlHeading: _controlHeadings[g.ControlLevel] = g; break;
            case ReportGroupKind.ControlFooting: _controlFootings[g.ControlLevel] = g; break;
            default: _details[g.Name] = g; break;
        }
    }

    /// <summary>Register one CONTROL operand, major→minor order (ISO §13.18.16.4 GR1/GR2; FINAL first).</summary>
    public void AddControl(bool isFinal, Func<string> get, Action<string> set) =>
        _controls.Add(new ControlEntry(isFinal, get, set));

    /// <summary>Register a SUM counter (ISO §13.18.54). <paramref name="addend"/> yields the addends' current
    /// total, already at the counter's scale; <paramref name="uponDetails"/> restricts accumulation to the named
    /// details (GR7c2; null = every GENERATE for this report, GR7c1); <paramref name="resetLevel"/> is the RESET
    /// control level (GR2; −1 = reset at the end of the group it prints in).</summary>
    public void AddSum(string id, Func<long> addend, string[]? uponDetails, int resetLevel, ReportGroup printedIn) =>
        _sums[id] = new SumEntry(addend, uponDetails, resetLevel, printedIn);

    /// <summary>A SUM counter's current value (unscaled, at the counter's scale) — read by the generated compose
    /// of the printable item the counter is the source of (ISO §13.18.54.4 GR4).</summary>
    public long SumValue(string id) => _sums.TryGetValue(id, out var s) ? s.Value : 0;

    // ── INITIATE (ISO §14.9.21.4) ──────────────────────────────────────────────────────────────────────────────

    /// <summary>INITIATE this report (ISO §14.9.21.4 GR1): sum counters ← 0 (GR1a; size-error indicators are the
    /// EC-REPORT-SUM-SIZE seam — checking default-off, COBOLNET_DESIGN §18.16), LINE-COUNTER ← 0 (GR1b),
    /// PAGE-COUNTER ← 1 (GR1c); the report becomes active (GR4). GR2: INITIATE of an ACTIVE report has no other
    /// effect (EC-REPORT-ACTIVE seam). GR3: the file is NOT opened here — it must already be open OUTPUT/EXTEND
    /// (EC-REPORT-FILE-MODE seam; an unopened connector's writes set its FILE STATUS, never crash).</summary>
    public void Initiate()
    {
        if (_active) return;   // §14.9.21.4 GR2 — EC-REPORT-ACTIVE seam (default-off), no other effect
        foreach (var s in _sums.Values) s.Value = 0;   // GR1a
        LineCounter = 0;                               // GR1b
        PageCounter = 1;                               // GR1c
        _active = true;                                // GR4
        _started = false;
        _firstBodySinceInitiate = true;
        _firstBodyOnPage = true;
        _rhOnThisPage = false;
        _pfOnThisPage = false;
        _indicateFresh = true;                         // §13.18.29 — the run's first presentation indicates
        _physLine = 0;
        foreach (var c in _controls) c.Prior = null;   // priors are saved by the first GENERATE (§13.18.16.4 GR3)
    }

    // ── GENERATE (ISO §14.9.16.4) ─────────────────────────────────────────────────────────────────────────────

    /// <summary>GENERATE one detail (<paramref name="detailName"/>) or a summary instance (null — §14.9.16.4 GR2,
    /// same processing with no detail printed). First GENERATE (GR4): RH once → PH → CHs major→minor → detail.
    /// Subsequent (GR5): on a control break, CFs minor→break then CHs break→minor (GR5a / §13.18.16.4 GR4), then
    /// the detail. Body groups page-fit per §13.18.35.4 GR4 (the chronologically first since INITIATE exempt);
    /// an unsuccessful fit page-advances per GR6 (PF → physical advance → PAGE-COUNTER → LINE-COUNTER ← 0 → PH).</summary>
    public void Generate(string? detailName)
    {
        if (!_active) return;   // §14.9.16.4 GR7 — EC-REPORT-INACTIVE seam (checking default-off, §18.16)
        if (!_started)
        {
            _started = true;
            // GR4a: the report heading, exactly once. (An RH on a page by itself needs NEXT GROUP NEXT PAGE —
            // staged loud at bind, so the in-flow placement below is the only reachable shape.)
            if (_reportHeading is { } rh) PresentHeadingFooting(rh);
            // GR4b / GR6: the page heading precedes the chronologically first body group.
            if (_pageHeading is not null) PresentPageHeading();
            // §13.18.16.4 GR3: the first GENERATE saves each control item in its prior control.
            foreach (var c in _controls) c.Prior = c.Get();
            // GR4c: control headings, major → minor.
            foreach (var ch in _controlHeadings.Values) PresentBody(ch);
        }
        else if (_controls.Count > 0 && DetectBreakLevel() is { } breakLevel)
        {
            // §14.9.16.4 GR5a / §13.18.16.4 GR4a: save current values, restore the PRIOR values so the ending
            // groups' CFs (and any reference to a control item while they print) see the pre-break contents,
            // print CFs minor→break, then restore the new current values and print CHs break→minor.
            _indicateFresh = true;   // §13.18.29 — a control break starts a new group instance
            var current = new string[_controls.Count];
            for (int i = 0; i < _controls.Count; i++)
            {
                current[i] = _controls[i].Get();
                if (_controls[i].Prior is { } prior) _controls[i].Set(prior);
            }
            for (int i = _controls.Count - 1; i >= breakLevel; i--)
                if (_controlFootings.TryGetValue(i, out var cf)) PresentBody(cf);
            for (int i = 0; i < _controls.Count; i++)
            {
                _controls[i].Set(current[i]);
                _controls[i].Prior = current[i];   // GR4a tail — new current values become the priors
            }
            for (int i = breakLevel; i < _controls.Count; i++)
                if (_controlHeadings.TryGetValue(i, out var ch)) PresentBody(ch);
        }

        // SUM accumulation (§13.18.54.4 GR7c): on every GENERATE for the report (GR7c1) or, with UPON, on a
        // GENERATE of a named detail (GR7c2) — AFTER the control-break processing, so a control footing printed
        // above showed the ended group's total (its reset happened at the end of its printing, GR2).
        foreach (var s in _sums.Values)
            if (s.UponDetails is null
                || (detailName is not null && Array.FindIndex(s.UponDetails,
                        d => d.Equals(detailName, StringComparison.OrdinalIgnoreCase)) >= 0))
                s.Value += s.Addend();

        // GR4d / GR5b: the specified detail — unless summary reporting (GR2).
        if (detailName is not null && _details.TryGetValue(detailName, out var detail))
            PresentBody(detail);
    }

    /// <summary>The most-major control level whose CURRENT value differs from its prior (§13.18.16.4 GR3 —
    /// tested major→minor, the first change wins; FINAL never breaks mid-report, GR2). Null when no break.</summary>
    private int? DetectBreakLevel()
    {
        for (int i = 0; i < _controls.Count; i++)
        {
            if (_controls[i].IsFinal) continue;
            if (_controls[i].Prior is { } prior && !string.Equals(_controls[i].Get(), prior, StringComparison.Ordinal))
                return i;
        }
        return null;
    }

    // ── TERMINATE (ISO §14.9.46.4) ────────────────────────────────────────────────────────────────────────────

    /// <summary>TERMINATE this report (ISO §14.9.46.4). GR1: inactive → EC-REPORT-INACTIVE seam, no effect.
    /// GR2: with NO GENERATE since INITIATE, no report group is processed at all — the sole effect is
    /// active→inactive. GR3: otherwise the control items revert to their prior values (GR3a), each control
    /// footing prints minor→major as though a most-major break occurred (GR3b), the page footing of the last
    /// page prints (§13.18.57.4 GR6f — every page's last group; "immediately followed by the report footing"),
    /// the report footing prints (GR3c), and the control items are restored (GR3d). GR6: the file is NOT closed.</summary>
    public void Terminate()
    {
        if (!_active) return;   // GR1 — EC-REPORT-INACTIVE seam (default-off)
        if (_started)           // GR2 — no GENERATE ⇒ no group processing of any kind
        {
            if (_controls.Count > 0 && _controls[0].Prior is not null)
            {
                var current = new string[_controls.Count];
                for (int i = 0; i < _controls.Count; i++)
                {
                    current[i] = _controls[i].Get();
                    if (_controls[i].Prior is { } prior) _controls[i].Set(prior);   // GR3a
                }
                for (int i = _controls.Count - 1; i >= 0; i--)                      // GR3b — minor → major
                    if (_controlFootings.TryGetValue(i, out var cf)) PresentBody(cf);
                for (int i = 0; i < _controls.Count; i++) _controls[i].Set(current[i]);   // GR3d
            }
            // §13.18.57.4 GR6f: the page footing prints as the last report group on EACH page — including the
            // final page (exception: a last page occupied only by an RF on a page by itself, which requires the
            // staged LINE NEXT PAGE form). When an RF follows, the PF is "immediately followed by" it.
            if (_pageFooting is not null) PresentPageFooting();
            if (_reportFooting is { } rf) PresentHeadingFooting(rf);   // GR3c
        }
        _active = false;   // GR6: the associated file stays open
        _started = false;
    }

    // ── Group presentation (ISO §13.18.35.4 / §13.18.57.4 / §14.9.16.4 GR6) ──────────────────────────────────

    /// <summary>Present a BODY group (detail / CH / CF — §13.18.57.3 SR15): the §13.18.35.4 GR4 page-fit test
    /// (skipped for the chronologically first body group since INITIATE), a failed fit's §14.9.16.4 GR6 page
    /// advance, then each line per GR5 (first line) / GR7 (subsequent lines).</summary>
    private void PresentBody(ReportGroup group)
    {
        group.BeforeReporting?.Invoke();   // ISO §14.9.49 Format 2 GR8 — just before the group is produced
        var lines = group.Lines;
        if (lines.Length == 0) return;     // a dummy group affects no counters (§8.4.3.15.4 GR5)

        if (_paged && !_firstBodySinceInitiate)
        {
            // §13.18.35.4 GR4b (absolute): fit iff integer-1 > LINE-COUNTER. GR4c (relative): trial =
            // LINE-COUNTER + Σ integer-2 over the group's relative LINE clauses; fit iff trial ≤ the group's
            // lower limit (§13.18.57.4 GR8: detail → LAST DETAIL; CH → LAST CH; CF → FOOTING).
            // ⚠ The 2023 GR4c wording — "incremented by integer-2 for each *subsequent* LINE clause" — is
            // ambiguous about the FIRST relative line's integer-2; the NIST goldens and the legacy oracle
            // resolve it as the sum over ALL relative lines (RW103A overflows exactly at LINE-COUNTER 25 with
            // LAST DETAIL 25 and one PLUS 1 line: 25+1 > 25), and GR5b3 then IGNORES the first line's relative
            // value anyway (first body group on the new page lands at FIRST DETAIL). Encoded as Σ over all.
            bool fit;
            if (lines[0].Kind == ReportLineKind.Absolute)
                fit = lines[0].Value > LineCounter;
            else
            {
                long trial = LineCounter;
                foreach (var l in lines)
                    if (l.Kind == ReportLineKind.Relative) trial += l.Value;
                fit = trial <= LowerLimit(group);
            }
            if (!fit) AdvancePage();   // §13.18.35.4 GR4 tail → the §14.9.16.4 GR6 sequence
        }

        for (int i = 0; i < lines.Length; i++)
        {
            long target;
            if (i == 0)
            {
                // §13.18.35.4 GR5a: absolute → integer-1. GR5b3 (paged, relative): the FIRST body group on the
                // page lands at FIRST DETAIL (the relative value is IGNORED); otherwise LINE-COUNTER + integer-2.
                // GR5c (unpaged, relative): LINE-COUNTER + integer-2.
                target = lines[0].Kind == ReportLineKind.Absolute ? lines[0].Value
                    : _paged && _firstBodyOnPage ? _firstDetail
                    : LineCounter + lines[0].Value;
            }
            else
                // GR7: a subsequent absolute line → integer-1; relative → LINE-COUNTER + integer-2.
                target = lines[i].Kind == ReportLineKind.Absolute ? lines[i].Value : LineCounter + lines[i].Value;
            PresentLine(target, lines[i], group);
        }
        _firstBodySinceInitiate = false;
        _firstBodyOnPage = false;
        if (group.Kind == ReportGroupKind.Detail) _indicateFresh = false;   // §13.18.29 — repeats now suppress
        EndOfGroupSumReset(group);
    }

    /// <summary>The body group's LOWER LIMIT for the page-fit test (ISO §13.18.57.4 GR8d/e/f).</summary>
    private int LowerLimit(ReportGroup group) => group.Kind switch
    {
        ReportGroupKind.ControlHeading => _lastControlHeading,   // GR8d
        ReportGroupKind.ControlFooting => _footing,              // GR8f
        _ => _lastDetail,                                        // GR8e — detail
    };

    /// <summary>The §14.9.16.4 GR6 page advance, in the GR's order: (a) the page footing, (b) the physical
    /// advance to the next page, (c) CODE re-evaluation — the CODE clause is staged loud at bind, so this point
    /// is a cited no-op — (d) PAGE-COUNTER + 1 (the NEXT GROUP … WITH RESET reset-to-1 form is staged with NEXT
    /// GROUP), (e) LINE-COUNTER ← 0, (f) the page heading.</summary>
    private void AdvancePage()
    {
        if (_pageFooting is not null) PresentPageFooting();                  // GR6a
        CobolFile.WriteAdvancing(_fileName, "", -1, before: false);          // GR6b — form feed
        _physLine = 0;
        PageCounter += 1;                                                    // GR6d
        LineCounter = 0;                                                     // GR6e
        _firstBodyOnPage = true;
        _rhOnThisPage = false;
        _pfOnThisPage = false;
        _indicateFresh = true;                                               // §13.18.29 — a new page indicates
        if (_pageHeading is not null) PresentPageHeading();                  // GR6f
    }

    /// <summary>Present the page heading (placement ISO §13.18.35.4 GR5b2: absolute → integer-1; relative with no
    /// report heading on the page → HEADING + integer-2 − 1, with one → LINE-COUNTER + integer-2).</summary>
    private void PresentPageHeading()
    {
        var ph = _pageHeading!;
        ph.BeforeReporting?.Invoke();   // §14.9.49 Format 2 GR8
        for (int i = 0; i < ph.Lines.Length; i++)
        {
            var l = ph.Lines[i];
            long target = l.Kind == ReportLineKind.Absolute ? l.Value
                : i > 0 ? LineCounter + l.Value                              // GR7
                : _rhOnThisPage ? LineCounter + l.Value                      // GR5b2 second form
                : _heading + l.Value - 1;                                    // GR5b2 first form
            PresentLine(target, l, ph);
        }
    }

    /// <summary>Present the page footing (placement ISO §13.18.35.4 GR5b4: absolute → integer-1; relative →
    /// FOOTING + integer-2).</summary>
    private void PresentPageFooting()
    {
        var pf = _pageFooting!;
        pf.BeforeReporting?.Invoke();   // §14.9.49 Format 2 GR8
        for (int i = 0; i < pf.Lines.Length; i++)
        {
            var l = pf.Lines[i];
            long target = l.Kind == ReportLineKind.Absolute ? l.Value
                : i > 0 ? LineCounter + l.Value                              // GR7
                : _footing + l.Value;                                        // GR5b4
            PresentLine(target, l, pf);
        }
        _pfOnThisPage = true;
    }

    /// <summary>Present the report heading or report footing in flow (placement ISO §13.18.35.4 GR5b1 for RH —
    /// relative → HEADING + integer-2 − 1; GR5b5 for RF — relative → FOOTING + integer-2 unless a page footing
    /// printed on the same page, then LINE-COUNTER + integer-2; absolute → integer-1 for both).</summary>
    private void PresentHeadingFooting(ReportGroup group)
    {
        group.BeforeReporting?.Invoke();   // §14.9.49 Format 2 GR8
        for (int i = 0; i < group.Lines.Length; i++)
        {
            var l = group.Lines[i];
            long target = l.Kind == ReportLineKind.Absolute ? l.Value
                : i > 0 ? LineCounter + l.Value                              // GR7
                : group.Kind == ReportGroupKind.ReportHeading ? _heading + l.Value - 1            // GR5b1
                : _pfOnThisPage ? LineCounter + l.Value                                            // GR5b5
                : _footing + l.Value;                                                              // GR5b5
            PresentLine(target, l, group);
        }
        if (group.Kind == ReportGroupKind.ReportHeading) _rhOnThisPage = true;
    }

    /// <summary>Present ONE report line: LINE-COUNTER is set to the computed line number FIRST (ISO §13.18.35.4
    /// GR6 — load-bearing: a <c>SOURCE IS LINE-COUNTER</c> item prints THIS line's number), then the line is
    /// composed (§13.18.53.4 GR3 — the implicit MOVEs execute when the line is printed) and physically written
    /// at the line's vertical position. The single method ordering makes the GR6-before-compose sequence
    /// impossible to reorder per group. A target at/above the current physical line advances one line — the
    /// §13.18.35.4 GR3 overlap rule's EC-REPORT-LINE-OVERLAP seam (checking default-off, §18.16).</summary>
    private void PresentLine(long target, ReportGroupLine line, ReportGroup group)
    {
        if (target < 1) target = 1;
        LineCounter = target;                     // §13.18.35.4 GR6 — BEFORE the compose
        string image = line.Compose();            // §13.18.53.4 GR1/GR3 — evaluated at presentation time
        if (group.Kind == ReportGroupKind.Detail && !_indicateFresh && group.IndicateFields.Count > 0)
        {
            // GROUP INDICATE (ISO §13.18.29): on a repeated presentation the indicated items present as spaces.
            var chars = image.ToCharArray();
            foreach (var (col, width) in group.IndicateFields)
                for (int i = 0; i < width && col - 1 + i < chars.Length; i++)
                    chars[col - 1 + i] = ' ';
            image = new string(chars);
        }
        int advance = (int)(target - _physLine);
        if (advance < 1) advance = 1;             // EC-REPORT-LINE-OVERLAP seam (§13.18.35.4 GR3)
        CobolFile.WriteAdvancing(_fileName, image, advance, before: false);
        _physLine = (int)target;
    }

    /// <summary>Reset the SUM counters whose reset point is the END of <paramref name="group"/>'s processing
    /// (ISO §13.18.54.4 GR2): no RESET phrase → the group the counter prints in; RESET ON level → the control
    /// footing of that level.</summary>
    private void EndOfGroupSumReset(ReportGroup group)
    {
        foreach (var s in _sums.Values)
        {
            bool reset = s.ResetLevel >= 0
                ? group.Kind == ReportGroupKind.ControlFooting && group.ControlLevel == s.ResetLevel
                : ReferenceEquals(s.PrintedIn, group);
            if (reset) s.Value = 0;
        }
    }

    // ── Line-composition helpers (used by the generated compose methods) ──────────────────────────────────────

    /// <summary>A fresh space-filled report line buffer of the report's width.</summary>
    public static char[] NewLine(int width)
    {
        var line = new char[width];
        for (int i = 0; i < width; i++) line[i] = ' ';
        return line;
    }

    /// <summary>Place a printable item's image at COLUMN (1-based, ISO §13.18.14) — the image is already
    /// width-exact (the §13.18.53.4 GR1 implicit-MOVE result), truncated only at the line-width edge.</summary>
    public static void Place(char[] line, int column, string image)
    {
        int start = column >= 1 ? column - 1 : 0;
        for (int i = 0; i < image.Length && start + i < line.Length; i++)
            line[start + i] = image[i];
    }
}
