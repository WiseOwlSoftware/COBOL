## The third pass is repaired: 7 confirmed, all applied; a fourth pass is owed

The third independent pass (decision PB6091: pilot on pages 227 to 466, then pages 1 to 226 and 467 to 976) read all 976 pages and
left 25 claims: 7 confirmed, 18 refuted, each with its own adversarial verdict (`1985/compare-*.json`, `1985/verify-*.json`).
It cost 10.5M subagent tokens, against pass 2's 23.7M. All 7 confirmed claims are applied, each read on the rendered page first
(`LEDGER-1985.json`, claims with `pass: 3`):

- **p114-1** Figure 1 (page 114): the first SEND reaches the left output queue and the second the right one, device 2 has two
  lines (one to each input queue), device 3 has two (one up to the left input queue, one from the left output queue with its
  arrowhead at the device). Redrawn in `specs/ANSI_COBOL_1985.md` lines 4975 to 5023; figure verdict `p114-0` follows.
- **p213-1** CLOSE (page 213): the one tall brace is whole, its point on the CLOSE row, CLOSE to its left (figure verdict
  `p213-2`). Sibling sweep of every brace in all 511 `<pre>` blocks found one more: page 289's class condition (its middle row one
  column left of the brace), repaired.
- **p224-1** and the title rule (PB6095): 11 `GENERAL FORMAT FOR` titles stood in four other forms; all are now
  `## <u>...</u>`, and `heading_levels.py` holds the rule. The claims p197-1, p200-1, p202-1, p208-1 and p225-1, refuted as
  formatting only, are decided by that rule (batch REFUTED, decision APPLIED).
- **p351-1, p354-1, p358-1** level-2 box sides on a figure caption, a `Format 2:` label and a caption with two notes. The sibling
  sweep read every unmarked `Format n:` label before a boxed format on the rendered pages: eight more sit inside the printed box
  (pages 258, 280, 303, 305, 335, 359, 404, 684) and are marked.
- **p833-1** the OPTIONAL MODULES table (page 833) is one HTML table with the row spans the print shows (RPW and ITR over the
  whole subset axis; COM, DEB, SEG split in the middle of INTERMEDIATE), as page 25 already holds the same diagram.

The other 18 claims are refuted and recorded (the 8 index claims p963-1 to p970-1; the 5 title claims p197-1, p200-1,
p202-1, p208-1, p225-1, refuted as the verifiers judged them and repaired under PB6095's title rule; p440-1's closing
brace; p934-1; p835-1; and the 2 summary-table claims p32-1 and p34-1). Not settled by the title rule, left as printed: p34 and p37 head the summary-table title as `##` and `#`
while pages 31 to 38 print it plain; the heading audit allows any level for an unnumbered underlined title.

The catalog's 11 changed rows are box markers only; their inventory records were re-recorded unchanged through
`record_verdicts.py`, and the Section II clause 7.3 not-rules block (its text holds Figure 1) was refuted again (UPHELD).

**PB6051 stays open.** It closes only on a full pass that confirms nothing, and this pass confirmed 7, so a fourth pass over the
repaired text is owed.
