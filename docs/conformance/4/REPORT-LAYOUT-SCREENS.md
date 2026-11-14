---
order: 23
---
> ⚖ **DETERMINATION — the compile-time screens of the report layout rules refuse what no presentation can excuse
> (§2023:13.18.14.3 SR7/SR8, §2023:13.18.35.3 SR6, §2023:13.18.57.4 GR7/GR8)** (2026-10-04; kb/Work PB1222, PB1270). Several of these
> rules are stated over a condition only the run can settle, and the standard leaves how a compiler judges them to
> the implementor. WiseOwl COBOL screens a rule only where **every presentation of the program breaks it**, and leaves
> the remainder to the run-time condition the standard pairs with it (EC-REPORT-COLUMN-OVERLAP §2023:13.18.14.4 GR4,
> EC-REPORT-PAGE-WIDTH GR5, EC-REPORT-LINE-OVERLAP §2023:13.18.35.4 GR3, EC-REPORT-PAGE-LIMIT GR2). (1) **"Each subject to a
> different PRESENT WHEN clause"** (SR7, SR8 a), SR6 a)–e)) means each of the two items carries a clause the other does
> not — the entry's own or an enclosing entry's, or a GROUP INDICATE clause, which has "the same effect as a PRESENT
> WHEN clause" (§2023:13.18.28.4 GR1). The standard does not require the clauses to exclude each other, and the
> conditions of two different clauses may both hold: that is how a conforming program reaches the run-time
> conditions above (`2002/pb1059_page_columns_width`, `2002/pb1188_report_nonfatal_raise`). (2) **The limits of a
> report group (GR7, GR8) that depend on the run** — whether a report heading shares its page with a page heading, whether
> a report heading or footing stands on a page by itself, how far the OR PAGE control headings reach — are taken at the
> WIDEST region they allow: a report heading, page heading and report footing at the HEADING integer and a body group at
> FIRST DETAIL for the upper limit; a report heading, page footing and report footing at the page limit for the lower.
> The limits that do not depend on the run are exact: a page heading's lower limit FIRST DETAIL − 1, a control heading's
> LAST CONTROL HEADING, a detail's LAST DETAIL, a control footing's FOOTING, a page footing's upper limit FOOTING + 1.
> (3) **SR8 c) and SR6 d)** ("shall not cause the page width / the lower limit to be exceeded unless each of them is
> subject to a different PRESENT WHEN clause, in which case this rule applies only to the largest of them") are read
> literally over the tail of the line or group — the relative items after the last absolute one. The tail is judged as
> a whole, every item present, unless EACH item of it carries a clause every other lacks; then only the largest is
> judged, the one that ends furthest right (or lowest) when it alone of the tail is present, after everything before the
> tail. An item subject to no clause excuses nothing. SR6 d) is asked of a group whose last absolute line is
> unconditional, as its text says ("one or more absolute lines, not subject to any PRESENT WHEN clause"). (4) **The default FIRST DETAIL** (§2023:13.18.39.4 GR3 b: the HEADING integer, 1 when HEADING is
> omitted) is the integer the page heading's lower limit and the body groups' upper limit read, so `PAGE LIMIT 10 LINES`
> with `TYPE PH LINE 1` is refused: the page heading must terminate before FIRST DETAIL (GR2 d), and with the default
> there is no line before it. (5) **The FIRST line of a relative heading or footing, and the reach of a relative page or
> report footing** (§2023:13.18.39.4 GR2 a), c), g); kb/Work PB1270): where §2023:13.18.35.4 GR5 b puts a relative first line is a
> number the description fixes (HEADING + integer-2 − 1 for a report heading and a page heading with no report heading
> before it; FOOTING + integer-2 for a page footing and a report footing with no page footing before it; the previous
> line plus integer-2 otherwise, which only moves it LOWER), so a first line that GR5 b prints above the HEADING integer
> or not after the FOOTING integer — in practice a `LINE PLUS 0` — and a relative page or report footing whose last line
> is past the page limit are refused: they leave the region on every page. A report footing whose first LINE clause is
> the bare `ON NEXT PAGE` is on a page by itself and starts at the HEADING integer (GR7 f), as the run-time engine
> places it. Pinned by `negative/pb1270-*-relative-*`, `85/pb1270_report_relative_first_boundaries`,
> `negative/pb1222-*`, `negative/pb1270-*`, `85/pb1270_report_page_region_boundaries`
> and `2002/pb1222_report_arrangement_present_when`. The mechanism is in `docs/REPORT_WRITER_DESIGN.md` (the
> ARRANGEMENT rules bullet).
