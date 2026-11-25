## Leads filed: PB6207, PB6208, PB6209 and a finding beside PB6172

PB6207 files the PB6119 lead that `IF N > 1 AND.` keeps the generic COBOL0001: the admitted set after AND holds the relational
operators, so `AdmittedKinds.Refusal` never names the missing operand.

PB6208 files, as an analysis for an adjudicator, the PB6119 probe in which bare `ALTER.` and `ENTER.` showed no error line at
--std 2023. The probe grepped for "error" only, so nothing is verified.

PB6209 files four fail-open gaps of `text_layer_diff.py`'s header and footnote classes: header words excused as a bag, `<td>`
header rows unseen, footnote renumbering judged on one page, and a stale link text in PB6204 (fixed in this change).

`kb/Work/PB6172/2026-11-24-more-numeric-display-readers.md` lists the eight files besides PB6172's three that name
`NumericDisplayForm`, from a grep of `src`.
