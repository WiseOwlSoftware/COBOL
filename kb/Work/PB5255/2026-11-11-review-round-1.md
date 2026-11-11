**2026-11-11, review round 1 of `spec-2002-reconcile-tools` at 36b5f261bd: five blocking findings and four leads, each
fixed at its root (review record `E:\COBOL-coord\scratch\review-w20261997-f.jsonl`).**

**Correction to the note (R1-8).** The note's "seven runs of six batches each" (and the PB4813 addendum
`2026-11-11-reconcile-route.md`'s "seven runs each") is wrong: each half's 37 batches are six runs of six batches and a
seventh run of one (`2002/ranges.json`, run sizes [6, 6, 6, 6, 6, 6, 1] in both halves). The README now says "runs of up
to six batches (six of six, then one of one)".

**The mechanism.** The merge replaced a missing input with a default in three places, and the workflow in one:
- R1-1 (high): `merged_rows` counted `pages_checked or batch`, so a compare file with `pages_checked: []` swept its whole
  batch, and a page outside the batch was accepted; the agent had no way to say a page was unread.
- R1-2 (high): a claim with no verify file took a same-(page, kind) verdict from a pool that kept verdicts already matched
  by text, and `write_decisions` then decided it REFUTED, a decision every re-merge kept. A real difference could be
  closed without a verifier.
- R1-4 (medium): the workflow reported `pages_swept` as the number of pages REQUESTED, whatever the agents returned.
- `load` skipped an unreadable agent file with a printed line and merged the rest.

**The fix.**
- R1-1. A compare file states `pages_checked` and `pages_unread` (each `{page, reason}`); every batch page is in exactly
  one, no page outside the batch in either, else the file is INVALID. Only listed-checked pages are swept; unread pages are
  recorded unswept with their reasons in the ledger (`pages_unread`) and the report. The schema and prompt carry
  `pages_unread`, and the prompt says a page not fully compared is never listed checked.
- R1-2. Each finding carries `id` = `p<page>-<n>` (n counting its page's findings from 1), written by the compare agent;
  each verdict carries `claim_id`, echoed by the verify agent (whose file is now `verify-<id>.json`). A verdict attaches to
  exactly that claim, once; a verdict naming no claim, a second verdict, or one whose page and kind are not its claim's
  fails the merge by name. A claim with no verdict is UNVERIFIED and undecided. Kept decisions are keyed by id, and a kept
  decision whose claim is gone or now says something else is refused, never dropped or moved. The 455 committed 2023
  agent files took the identity their names already carried (`verify-p<page>-<kind>-<vi>` is finding `vi` of the compare
  file holding the page): all 346 verdicts mapped one to one, each agreeing with the committed LEDGER.json, inserted by a
  one-time script that checked each file loads as the old object plus the new key.
- R1-3. A run's branch lands only its `compare-*.json` and `verify-*.json` (names are batch and claim id; the halves share
  no batch, so run branches never touch one file). The merge writes nothing by default; `--write` runs once on the branch
  that closes a set of runs, and `--check` proves the committed ledger and report are the merge. README steps 4 and 5
  state the two-account procedure; "Re-running a batch" says to delete the batch's compare and verify files first.
- R1-4. The workflow holds each returned compare result to the merge's rules (`compareProblems`), counts only the pages
  of valid results, and returns `ok: false` naming `batches_without_valid_compare`, `pages_unread` and `unverified`. A
  workflow script cannot read files, so this reads the return values the prompt requires to equal the files; the merge
  reads the files.
- R1-5. The 2023 transcription keeps no `## Page N` headings (`spec_pdf.page_count` is 0), so no page slice exists to
  compare. The workflow has no 2023 row and refuses it, naming that reason. `page_workunit.py` still renders a 2023 page
  alone (md null) for spec-grammar-impact.js and spec-grammar-conformance.js, which read only the image.
- R1-6. `spec_pdf.pages_of` owns the rule that headings run 1..N once each and raises `PageHeadingError` naming the
  heading and line; `reconcile_ranges.page_count`, a second copy of the rule, is deleted, and `spec_pdf.page_count` serves
  both. The owner is the page reader, not a transcription lint: every tool that slices pages goes through it, so none can
  slice a duplicated heading. `page_workunit.py` marks `heading_only` and names those pages on their own line.
- R1-7. `REPORT.md` (2023) regenerated; with the ids added, `LEDGER.json` too (210 confirmed, 136 refuted, 0 unverified, as
  before; it differs from the old file only by each claim's `id` and an empty `pages_unread`). `--check` keeps both current.
- R1-9. Both decision ledgers carry `book_pages` (the transcription's page count), `pages_swept` and `pages_unread`;
  `pages` is gone. `verify_publishable.check_ledger` refuses a ledger whose `book_pages` is not the transcription's, that
  missed a page or names one beyond the book, or that has an unread page. `LEDGER-1985.json` carries 976, 1..976 and none.

**Sweep for the same pattern.** In the merge: `load`'s skip (now an error naming the file), `doc.get("findings") or []`,
`(v or {}).get(...)` on a missing verdict, `r.get(..., "")` defaults for a claim's text, and `r.get("page", 0)` (each now a
required field, validated). In the workflow: `!res` returning `[]` (now a named failed batch), a null verdict counted
nowhere (now unverified by id). `--expect` (a typed count) is deleted: the transcription's page count names the unswept
pages.

**Proof.** Self-tests PASS: `merge_reconciliation.py` (33 arms), `spec_pdf.py`, `page_workunit.py`, `reconcile_ranges.py`,
`verify_publishable.py`, and the new `test_wf_spec_reconcile.py` (the workflow with stubbed agents, 8 arms; every arm FAILS
on the round-0 workflow). The reviewer's `merge_probe.py`: on the round-0 merge, pages 1-12 and 500 swept and claim B
REFUTED by A's verdict; on this merge, "MERGE REFUSED: 8 problem(s) in 4 file(s)", nothing written. A dry merge over a
planted 2002 directory (one good compare file, one with page 17 unread, batch 25-36 with no file): 23 of 882 swept, p17
unread with its reason, 25-882 unswept, nothing written; `--write` then `--check` CURRENT.
