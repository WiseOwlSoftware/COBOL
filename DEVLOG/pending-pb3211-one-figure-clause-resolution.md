## render_figure resolves the clause that owns a figure once, and the review sheet titles each figure by it (PB3211)

**The mechanism.** `render_figure.py` decided which clause owns a general-format figure in three places with three
rules: `find_figures` took the heading above a band on its own page, `figure_key` walked back for a continuation band to
the LAST General format heading of the nearest page carrying one (what `sweep_figures` keys on), `continues_from_previous`
walked back a fourth time to decide whether a page opens inside a region, and the HTML review sheet threw the per-band
titles away (`find_bands`) and labelled every figure on a page with `clause_title`, which took the page's FIRST General
format heading.

**Re-probe (licensed PDFs, every page, before the change).** Sheet title against the sweep's key: 2023 9 of 486 figures
under another clause (PDF pages 166, 173, 227, 231, 302, 305, 306 twice, 363: page 166's second figure shown as 8.4.3.7.2,
keyed 8.4.3.8.2), 2002 16 of 446, 2014 16 of 464. And before unifying the two walk-backs, every untitled band (2023 75,
2002 47, 2014 64): the key's walk and the continuation walk named the same heading in every one.

**The fix.** One walk-back, `open_region(page)` (the heading of the general-format region the previous page leaves open,
or None), resolves the leading region's clause inside `find_figures`, which now returns a `Figure` (y0, y1, title,
label, continued) per band. `figure_key(figure)` parses that title; `sheet_title(figure, folio)` shows it, marking a
continuation. `find_bands`, `clause_title` and `continues_from_previous` are deleted and every caller changed
(`sheet`, `main`, `sweep_figures.generated`); the plan's figure paragraph names the one resolution.

**Neutrality.** After, on all three vector editions: every figure's page, band, label and sweep key identical
(486/446/464), and the sheet's title changed for exactly the 9/16/16 figures that diverged, each now naming the key's
clause. No compiler code changed, so the C# oracle has nothing to compare. Witness: `test_render_figure.pb3211_failures`
(two regions on one page, a continuation page, a closed region), red with `open_region` stubbed out.

**Self-review.** One resolution, one walk (the window constant named, `CONTINUATION_REACH`); no second copy of the rule
left (grep for `headings(doc[` finds only `open_region`); `Figure` keeps index access for `build`'s callers.

**Gate.** See the report of wave 20262139 H.
