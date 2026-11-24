## The fourth pass is repaired: 5 confirmed, all applied; a fifth pass is owed

The fourth independent pass (run wf_e5491422-f0a) read all 976 pages and left 19 claims: 5 confirmed, 14 refuted, each with
its own adversarial verdict (`1985/compare-*.json`, `1985/verify-*.json`, committed as the run's record). All 5 confirmed
claims are applied, each read on the rendered page first (`LEDGER-1985.json`, claims with `pass: 4`):

- **p729-1, p733-1, p743-1, p744-1** the Section XVII summary-of-differences tables (pages XVII-21, -25, -35, -36) set the
  ELEMENT column flat where the print indents it. Each row now carries one `&nbsp;` per printed column of indent, measured
  against the ELEMENT heading, as pages 730 and 734 to 742 already did. Sibling sweep: every page of those tables (725 to
  749) rendered and read. Ten more pages were flat: 731, whose claim p731-1 was refuted as formatting and is decided by
  the same repair, and 725, 726, 727, 732, 745, 746, 747, 748 and 749, 246 rows in sweeps `summary-indent(PB6051 p729-1)`;
  page 749's two group titles, printed underlined and set bold, are `<u>...</u>`. The C322 correction note quotes its row
  as it now stands.
- **p924-1** (normative) four shaded Amendment 2 passages on page B-12 (PERFORM GR10d, SEARCH GR2b and GR4, RECORD GR10)
  had no level-2 box sides; each is now `║...║` as pages 919, 920 and 922 mark theirs, with a `boxes` entry of 4 blocks.
  Sibling sweep: every Amendment 2 page (907 to 960) rendered and read; the shaded pages 919, 920, 922, 927, 930, 932, 935
  and 936 already carry exactly their pairs, and no other page is shaded.

The other 14 claims: **p737-1** (refuted as formatting only) showed the `SUMMARY OF DIFFERENCES IN ...` titles standing as
`#`, `##` and plain. Like the summary-of-elements titles, each is the table's own title printed underlined on every page,
so all 15 are now the plain line `<u>SUMMARY OF DIFFERENCES IN ...</u>` (8 changed, each checked underlined in print), and
`heading_levels.TITLE_FORMS` holds a third pair (selftest case 08); the eight `summary-titles(p31-1)` sweeps that named the
headed form are re-stated. p835-1, p906-1 and the ten index claims p963-1 to p972-1 are refuted as the verdicts say.

**PB6051 stays open.** It closes only on a full pass that confirms nothing; this pass confirmed 5, so a fifth pass over the
repaired text is owed.
