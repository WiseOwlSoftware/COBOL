## Entry 3936 — 2026-11-22 15:53 PST — PB3322: the refuter pass over PB3173's edition decisions overturns one row

PB3173 decided the editions of 215 inventory rows with no separate refuter. This pass is that check. The note's premise
(the repo holds no 2002 or 2014 text) is stale: `specs/ISO_COBOL_2002.md` and `specs/ISO_COBOL_2014.md` are in the tree,
so each lower-confidence decision was checked against its own edition's text.

**What the re-probe showed.** The split-key SOURCE rows, the A.4.8 FORMAT / SELECT WHEN rows, DOC-A.1-34, -37, -112, -188
and -202 are all upheld: each referent is in the edition the row starts at and absent below it (the 2002 clauses 8.1.1,
7.2.14, 13.16.22, 13.16.49, the 2002 Annex A items 87 and 151, the 2014 Annex A item 184). DOC-A.1-64 is overturned:
it documents entry-convention-names, their meanings and the default, and 2002 11.8.5 and Annex A item 44 carry all three.
PB3173 cited the construct `options-entry-convention-2014`, which is only the COBOL alternative added in 2014.

**The fix.** One `record_verdicts` record (`adjudication/pb3322-refute-pb3173/overturn-batch.json`): DOC-A.1-64 moves from
2014,2023 to 2002,2014,2023. GAP stays 4. A text sweep of all 215 decided rows against the 1985, 2002 and 2014 files
(`docsweep.py` beside the batch) finds no other row whose item text sits in an earlier edition than its band. The rows
banded 85 by the BASE85 reason are the owner's reading rule (PB3173 lead 2) and are not re-judged here.

No code, no golden: an adjudication of a store field. The gate's verdict line is in the report.

