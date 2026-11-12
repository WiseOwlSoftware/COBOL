## Entry 3546 — 2026-11-12 14:47 PST — PB479 designed and reviewed: the standard's unnumbered rules get paragraph and table rows, decided per unit, every one minted open

kb/Work PB479 said the traceability denominator misses every obligation the standard writes as unnumbered prose. This
landing measures the gap and decides how to close it; it changes no script and no catalog.

The measurement (scripts outside the repository, `E:/COBOL-coord/scratch/pb479/`) counted, in the normative clauses,
the sentences that carry 'shall', 'may' or their equivalents ('must' too for 1985) and lie in no catalog row: 432 in
223 clauses of 2023, 238 in 135 clauses of 1985. It also found 121 margin paragraphs that the PDF prints at a rule
block's margin but the extractor appends to the rule above them, in 57 rows (`SR-14.8.2.2-2` holds the
variable-length-group sentence PB479 thought had no row), and 46 tables outside rule blocks, Table 12 among them, plus
36 inside them.

Hand-checked samples settled the design. The verb test is right 73 % (2023) and 77 % (1985) of the time, but general
rules are written in the indicative, so it misses most of them. Decision PB5448 makes the margin paragraph the unit
(`<kind>-<clause>-p<n>`, PB284's form), with table units by the table's kind and Annex A's numbered items by their
printed number. Every unit gets an adjudicated disposition keyed by the digest of its whole text, and the catalog audit
refuses any unit without one. A unit that states anything normative is a row. Only a lead-in, a pure cross-reference
or data a row consumes may be ruled not a rule, each by a mechanical test and an upheld refuter record.

An adversarial review found six blocking defects, and the design now answers all thirteen findings (kb/Work PB479's
dated file `2026-11-12-design-review.md`). A paragraph carved out of a numbered row no longer inherits that row's
verdict and test, and quotations in other records order the work without closing anything. Free "definition" and
"overview" reasons are gone. The paragraph join no longer keys on page furniture, which the 2023 text lacks. And step
PB5449 now changes no catalog rule, so no stored citation goes red.

The steps are PB5449 to PB5456. Every row is minted open: the 2023 GAP, 0 today, is estimated to rise by about 500 to
1,850 when PB5451 mints the rows in one landing (the exact count is PB5450's output), and the 1985 GAP from 318 to
about 670 (515 to 820) plus tables. The fourteen held 1985 records are recorded right after the 2023 mint (PB5454).

