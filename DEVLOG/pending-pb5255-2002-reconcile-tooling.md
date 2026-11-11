## PB5255: the spec-reconcile tooling takes the 2002 edition: every Workflow argument required, the work unit slices by page heading, the merge writes LEDGER-2002.json, and the 882 pages are planned as data

**Decision.** The project owner's controller decided the 2002 reconciliation's route on 2026-11-11 (kb/Work PB4813 addendum
`2026-11-11-reconcile-route.md`): the Workflow on the plan's quota with Opus agents at effort medium, never Message
Batches; all 882 pages with the flagged and figure pages first; one adversarial verify agent per claim; the figure
refuter pass as the second phase.

**Mechanism found.** `page_workunit.py` sliced the transcription at `<a id="page-N">` anchors. None of the three
transcriptions has one (2023 dropped its pages; 1985 and 2002 open a page with `## Page N`), so every 2002 work unit would
have been an empty markdown slice with a printed warning, and an agent comparing a page with nothing can call it clean.
`render-spec-page.py` skipped a page out of range and exited 0. The workflow wrote into the main checkout whatever worktree
ran it, shared one scratch directory per edition, defaulted its page list, ran agents on the host's default model, told
every agent its pages were figure pages, and cited 2023's notation clauses for every edition.

**Fix.** `spec_pdf.PAGE_HEADING` and `spec_pdf.pages_of` are the one reader of a page (moved from `verify_publishable.py`;
`sweep_figures`, `citation` and `rule_catalog_1985` repointed). `page_workunit.py` slices by it, exits 2 naming a page the
transcription lacks, and prints the batch's figure pages (`sweep_figures.general_format_pages`). The workflow requires
`pages`, `edition`, `model`, `effort`, `outDir` and `workDir` and refuses an unknown or malformed one before any agent
starts; it builds the units in the checkout `outDir` lies in, cuts batches at gaps in the page list, and cites each
edition's own notation clauses, each checked with `cite.py --check` (2002: §2002:5.1.1, §2002:5.1.5.3, §2002:5.1.6 among
them). `merge_reconciliation.py --edition` has a row per edition: 2023 `agents/` into `LEDGER.json`, 2002 `2002/` into
`LEDGER-2002.json` in `LEDGER-1985.json`'s shape, the one `verify_publishable.check_ledger` reads, keeping decisions
across re-merges; 1985 has no row. `reconcile_ranges.py` generates and checks
`docs/rearchitecture/spec-reconciliation/2002/ranges.json`: 74 batches of 12, 29 flagged (57 ambiguous pages,
§2002:11.1.1, §2002:13.14.1), 51 with a figure (305 figure pages, 445 general formats), halves `account1` and `account2` of
37 batches each (work 662 and 665), each six runs of six batches and a run of one.

**Review round 1 (kb/Work PB5255 addendum `2026-11-11-review-round-1.md`).** The merge recorded a page swept that no
agent compared (`pages_checked or batch`), and gave a claim with no verifier a neighbour's REFUTED verdict through a
same-page pool, deciding it REFUTED for good. Every compare file now states `pages_checked` and `pages_unread` (with
reasons) covering its batch exactly; every finding carries an `id` (`p<page>-<n>`) and every verdict the `claim_id` it
judged; any missing, empty, unreadable or unmatched input makes the merge exit 1 naming the file, writing nothing. The
workflow holds each returned result to the same rules and reports only what valid results prove (`ok`,
`batches_without_valid_compare`, `pages_unread`, `unverified`), and refuses 2023, whose transcription keeps no pages. A
run's branch lands only its agent files; one closing branch writes the ledger (`--write`) and proves it (`--check`).
`spec_pdf.pages_of` refuses page headings that do not run 1..N once each (`reconcile_ranges`' copy of that rule deleted),
and `page_workunit.py` names heading-only pages. Both decision ledgers carry `book_pages`, `pages_swept` and
`pages_unread`, which `check_ledger` holds to the whole book. The 455 committed 2023 agent files took the ids their file
names already implied (346 verdicts, each matching the committed LEDGER.json's), and LEDGER.json and REPORT.md were
regenerated: 210 confirmed, 136 refuted, 0 unverified, as before.

**Review round 2 (addendum `2026-11-11-review-round-2.md`).** An id is reused when a batch is re-run, so a leftover
verdict could judge a new claim with the same id, and a kept decision was matched on `pdf_says` alone. A claim's identity
is now its content: `claim_key` fingerprints its page and every text field, the workflow hands it to the verifier to
copy, and a verdict or a kept decision attaches only to the claim with that key; a mismatch is refused by name. The rule
for a re-run is that the run on disk wins, and the merge refuses an earlier run's leftover verdict rather than anything
deleting it. Keys were compared, not quoted text, because 203 of 2023's 346 verify files quote their claim inexactly;
the 346 got the keys of the claims their ids named.

**Proof.** The self-tests of every touched script PASS, among them `test_wf_spec_reconcile.py`, a dry run of the workflow
with stubbed agents (10 arms; 8 fail on the round-0 workflow and the 2 key arms on the round-1 one); `probe2.py`'s re-run
and re-kinded cases are refused (each attached the wrong verdict or decision on round 1); the reviewer's `merge_probe.py` sweeps pages 1-12 and 500
and refutes the unverified claim on the round-0 merge, and is refused naming its four files on this one;
`verify_publishable.py --edition 1985` holds the 1985 transcription to its ledger, 976 of 976 pages compared;
`reconcile_ranges.py --check` passes; the canary `page_workunit.py 1..12 --edition 2002 --dpi 300` rendered 12 pages from
the private PDF and wrote 12 non-empty slices. The fleet was not started.
