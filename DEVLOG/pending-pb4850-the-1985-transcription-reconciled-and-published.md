## PB4850: the 1985 transcription, reconciled against the page and published

The 1985 transcription (`specs/ANSI_COBOL_1985.md`) had passed its figure gates but not its prose ones. Five attempts at
PB4850 had stopped short. This one applies the batch reconciliation of 2026-11-06 and runs no comparison again.

**The Preface and the gates' 1985 rows.** The file now opens with a Preface. It names the edition and its two
amendments, quotes the CODASYL grant of PDF page 3, reproduces the acknowledgment in its entirety, and explains the
notation. It names no person. `verify_publishable` and `verify_acknowledgment` exited for 1985 because their tables
had no 1985 row; both rows are now written from page 3. Two things in those gates were 2023's alone. First, the front
matter ended at clause 1's anchor; it now ends at each edition's own body line (`spec_pdf.EDITIONS` `body`). Second,
the acknowledgment was compared with the PDF's text layer, and a scan's text layer is OCR: it reads "or" as "OI" and
"1958, 1959" as "1958,1959", and it breaks two words at a line-end hyphen. The 1985 row lists those four misreadings,
each confirmed on the rendered page; any other difference still fails. Re-probe: both gates GREEN for 1985 and 2023.

**The claims.** Each `final` CONFIRMED and DISPUTED claim in `verdicts.json` was read against a crop of the rendered page
before it was written (a montage of crops, a dozen claims to an image). The applied repairs: the title pages'
emphasis, the Contents' section titles, the element- and difference-summary titles and group labels, the glossary terms
of pages 128 and 140, the format and rule-group labels, three running heads, Report Writer Table 1's head, Table I-5's
column span, four words, and Amendment 1's change-list heads. Every applied claim's pattern was swept. That added 76
repairs, among them every underlined summary title and Contents title and the labels on pages 165, 283 and 662. The
sweep also found where the pattern stops: Section II's Examples, page 663's FORMAT 3 and Amendment 1's summary title
are not underlined, and Amendment 2's heads are not either. Three claims were decided against the batch: p686's
"recognize" (refuted), p738's cut-off character (kept), and Figure 1's connectors (a lead, not a text repair).

**The boxes.** The 1985 standard boxes every specification unique to level 2 (§1985:I.1.3). The box pass found 722
boxes. In prose a box is now a `║` at each end of each boxed block (`spec_pdf.BOX_SIDE`), the same side FIGURE-STYLE
rule 9 draws in a format, and TRANSCRIPTION-STATE.md states the rule. 606 boxes are marked; 115 are in general formats
and were already drawn; one is a drawn flowchart. Each box is placed by its first and last words. The first placement
took the shortest span, and the page refuted it on page 257 (rule (3)'s "or 88." landed in rule (2)) and on page 545.
Placement now follows reading order: boxes.json lists a page's boxes top to bottom and they never overlap, and pages
257, 545, 668 and 685 confirm the result.

**A sibling the boxes exposed.** A boxed heading (`### ║6.13.2 General Format║`) hid 30 of the 213 general formats from
`audit_grammar_optional_words --edition 1985`, and `cite.py` could not find §1985:VII.2.7. Every heading reader
(`citation._MARK_85`, `sweep_figures.CLAUSE_LINE`/`MD_HEADING`, `audit_grammar_optional_words.MD_HEADING`) now reads
through `spec_pdf.BOX_SIDE`, with a planted case in each self-test. The audit's output is identical before and after the
boxes.

**The ledger.** `docs/rearchitecture/spec-reconciliation/LEDGER-1985.json` records every claim with its decision, every
repair, every sweep and every box. `verify_publishable.check_ledger` holds the transcription to it: a lost repair, an
undecided claim, or a box side no marked box accounts for is refused. It runs as the gate audit `SPEC CORRECTIONS 1985`
and as a CI step.

**PB4886's catalog, on the reconciled text.** PB4886 landed while this branch ran, with `rule_catalog_1985` reading a
box as `<ins>`. On the merged tree its counts fell from 1988 rules to 1502, because a boxed heading or rule no longer
parsed. It now reads the box side (`BOX_SPAN`, `ORDINAL`, its titles; `citation.MARK_85`), with a planted boxed
heading and rule. Its ordinal gaps fell from 11 to 1 once the 28 boxes the batch had drawn as a blockquote or a
one-cell table became boxed paragraphs: those hid 17 rules. The last gap is the scan's. It lacks the printed pages
XIII-54 and XIII-55 (the TYPE clause's heading and syntax rules (1)-(4)) and XIII-30 and XIII-31, read on the page
from the folios on either side. A comment marks each gap, and the Preface says so.

**Also.** PDF pages 527-528 repeat 525-526 (the scan prints folios X-11 and X-12 twice, read on the page). Both copies
stay, and each repeat is marked. `transcribe_pdf assemble` no longer overwrites the reconciled file. DOC_INDEX and
VERSION_CHANGE_REFERENCE name the `cite.py` call that works (`--check "§1985:…"`; the edition comes from the citation).
Rows 7.13, 7.19 and 7.20 agree with the 1985 text (§1985:IV.4.2.2.2.3; no ORDER TABLE; §1985:VII.2.7). PB4759 can cite
§1985:XV.3.2.4.

**Review round 1.** The branch review found one more reader of the old markup. `audit_batch.COMMON`, the preamble
of every map, verify and claims request, still told the reader that `<ins>...</ins>` marks a level-2 box, while the
transcription carries no `<ins>` and marks a box with `spec_pdf.BOX_SIDE`. The audit readers would have had no level-2
signal. The preamble now names the box side (built from `BOX_SIDE`) and a format's double lines (§1985:I.1.3), and the
self-test plants a whole-boxed rule. It checks that every pass's preamble defines the mark `rule_catalog_1985.box_of`
reads and that each pass quotes the box sides. The sweep also corrected `rule_catalog_1985`'s box citation from
§1985:I.1.2 to §1985:I.1.3, where the box sentence is.

**Leads, not fixed here.** Figure 1's two missing connectors (page 114). The element-summary tables' remaining group
labels and indentation (pages 31-84; only the claimed rows and the titles were checked). Underlines on Section headings
in the body beyond the claimed ones. Page 837's head (`| Page No. | Change |`). The four printed pages the scan lacks (they need another copy of the standard).
