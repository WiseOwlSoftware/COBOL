## PB479 designed: the standard's unnumbered rules get paragraph and table rows, decided per paragraph, found mechanically

kb/Work PB479 said the traceability denominator misses every obligation the standard writes as unnumbered prose. This
landing measures the gap and decides how to close it; it changes no script and no catalog.

The measurement (scripts outside the repository, `E:/COBOL-coord/scratch/pb479/`) counted, in the normative clauses,
the sentences that carry 'shall', 'may' or their equivalents ('must' too for 1985) and lie in no catalog row: 432 in
223 clauses of 2023, 238 in 135 paragraphs of 1985. It also found 121 margin paragraphs that the PDF prints at a rule
block's margin but the extractor appends to the rule above them, in 57 rows (`SR-14.8.2.2-2` holds the
variable-length-group sentence PB479 thought had no row), and 46 tables outside rule blocks, Table 12 among them.

A hand-checked sample of 30 hits and 30 non-hits per edition settled the design. The verb test is right 73 % (2023) and
77 % (1985) of the time, but half of the 2023 non-hits and 30 % of the 1985 ones are obligations too: general rules are
written in the indicative. So no sentence pattern can define the rows. Decision PB5448 makes the margin paragraph the
unit (`<kind>-<clause>-p<n>`, PB284's form), with table rows `-T<t>.<r>`. Every margin paragraph and table gets an
adjudicated disposition keyed by its text, and the catalog audit refuses any paragraph that has none. The verbs only
refuse a 'shall' paragraph disposed as no rule.

Attacking the design found three defects in today's paragraph splitter. It cuts §2023:14.6.2.2's dash list from its
lead, swallows Table 12 as paragraph text, and joins §2023:8.4.2.1's uniqueness sentences to a paragraph ending ".)".
Step PB5449 fixes all three. A transcription repair that renumbers paragraphs now writes its own move batch.

The steps are PB5449 to PB5456. The 2023 GAP, 0 today, is estimated to reach about 550 to 1,000 when PB5451 mints the
rows in one landing. The fourteen held 1985 records are recorded right after (PB5454).
