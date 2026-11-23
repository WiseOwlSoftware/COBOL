## Entry 3932 — 2026-11-22 15:30 PST — The landing refuses a change of rule text that stales a stored code read (PB6090)

PR #1953 corrected the 1985 transcription of six rules. Each row's `rule-digest` changed, so each stored code read went `stale`
(`code_read_store.outcome`), and `edition_status.py` 1985 gained six code-read terms with every landing check green. PB6056's landing
check covered only the other way a read turns owed (moved code); its sweep had filed the stale arm as visible in the diff, which is true of
the catalog edit and not of its effect on the reads.

Re-probe: `python scripts/spec/code_read_landing.py --rev 239834a8bb --base 09e224d38f` (the merge of #1953 against its first parent)
passed at the PB6056 head ("no stored code read names code this change moves") and, with this change, exits 1 naming exactly six rows
(1985-FMT-VI.2.4.2, 1985-GR-VI.6.27.4-19, -20, 1985-GR-VIII.4.6.4-13, 1985-GR-IX.4.6.4-16, 1985-GR-X.5.2.4-4), each with the digest the
read covers and the digest now.

Fix at the same check, with no second one: `code_reads.staled_by(head_ctx, base_ctx, records)` takes a record that decided its row on main
(`outcome` confirmed or contradicted at main's rows) and leaves the row `owed` at the head, the audit's and `edition_status.py`'s own
predicates, so a deleted row or one closed without a read is not refused. `code_read_landing.check` asks it beside `moved_by`, with main's
context from `context_at(year, root, base)`; the refusal names the rows with both digests and the remedy (`import --replace` against the
corrected text). A branch that carries the re-read passes; a change of no rule text passes.

Self-tests `case_23` to `case_25` (predicate; digest change refused; carried re-read and no text change pass). Docs: the code_reads.py and
code_read_landing.py docstrings, enqueue.py's check list, DESIGN-spec-conformance-review 10.6.1, DESIGN-test-build-ci, DOC_INDEX,
MANDATORY-PRACTICES L1, and a note, kb/Work/PB6090.md.

