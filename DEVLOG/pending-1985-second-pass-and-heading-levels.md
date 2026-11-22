## The 1985 transcription's second pass is repaired, and its headings are derived from the printed contents (PB6051, PB6057)

The second independent pass over `specs/ANSI_COBOL_1985.md` (kb/Work PB6051, the owner's yes of 2026-11-22) had compared all
976 pages and left 236 claims, 167 of them confirmed, unrepaired on the branch `md85-pass2` with no report. This change
cherry-picks its four commits onto main and repairs every confirmed claim at its root.

**Headings (PB6057).** 121 of the 167 were heading levels, and the owner's question settled the rule: "Shouldn't the PDF Table
of Contents provide the correct heading structure?" A Section is level 1, an Appendix and a chapter `n.` level 2, a clause `n.n`
level 3, one deeper for each further number, at most 6. `scripts/spec/heading_levels.py` holds the rule once (`level_of`,
`canonical`, `normalize`) and the audit (`check`), which `verify_publishable.py --edition 1985` now runs: every numbered heading at
its number's depth plus one, no numbered title left as plain text, no running header promoted to a heading, each Section heading
once and on the page whose folio is its first, every contents entry (319, read from pages 9 to 15) a heading of that number and
title on the page its folio names. Run on the text before the change it named 1,276 findings and the pages of 112 of the 121
claims (the other nine name clauses already at their level whose siblings were the defect). The normalizer changed 1,228 heading
lines (872 levels and 148 plain titles in Sections I to XVIII, 200 levels and 2 plain titles in Amendment 1's Section A, 6 running
headers made plain) and 18 stray `# SECTION` dividers that sat after the previous page's folio are gone; each Section's title,
which the page prints at its top, is the level-1 heading. Two contents entries differ from the heading the standard prints (page
553, page 654) and stay as printed. The rule catalog has the same 3,109 rules and ids; headings are markup and `cite.py` reads a
number. Six code anchors named the old heading lines or levels (the body marker, the heading closing the reserved-word list in three
readers, the Section A `Arguments` headings of one drift test, the planted texts of one self-test) and moved; the first gate
found two of them.

**The other 46 claims**, each read on the rendered page: `CHARACTERS` in the BLOCK CONTAINS braces is not underlined (six pages
named; the sweep found two more, pages 396 and 486); level-2 box sides missing or misplaced in prose, tables and one-row general
formats (pages 237, 257, 290, 371, 413, 522, 544, 580, 665, 667, 680, 684, 703, 920) and the brackets and pipes left around a
box (378, 453, 462, 505); summary-table indentation (32, 34, 54, 738, 741) and page 738's three clipped `Z` flags, which the
first pass had kept as unreadable; page 23 `of`, page 933 `the`; page 772's correction note moved to where the deleted sentences
were printed. Pages 815 to 818 hold the index's heading and folio only, as the other index pages do: Amendment 2 replaces the
index whole (Errata C337), decided `KEPT`.

**Recorded.** `LEDGER-1985.json` gains the 236 claims with `pass: 2` (167 decided, 69 refuted), 1,228 heading sweeps or edits and
box 733; boxes 32, 193, 507, 544, 602, 609 and 703 grow. The verdicts of ten figures follow the edits. The catalog's 24 changed
rows are box and level readings the repairs corrected (rule text changed by a `║` only); their 23 inventory adjudications were
re-recorded unchanged through `record_verdicts.py` (GAP 0 to 0).

PB6051 stays open: it closes when a full pass finds nothing, and the next pass reads the repaired text. PB6057 is landed.
