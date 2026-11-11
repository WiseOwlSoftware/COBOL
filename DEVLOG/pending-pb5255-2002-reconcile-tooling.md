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
them). `merge_reconciliation.py --edition` has a row per edition: 2023 `agents/` into `LEDGER.json` (re-merging reproduces
the committed file byte for byte), 2002 `2002/` into `LEDGER-2002.json` in `LEDGER-1985.json`'s shape, the one
`verify_publishable.check_ledger` reads, keeping decisions across re-merges; 1985 has no row. `reconcile_ranges.py`
generates and checks `docs/rearchitecture/spec-reconciliation/2002/ranges.json`: 74 batches of 12, 29 flagged (57
ambiguous pages, §2002:11.1.1, §2002:13.14.1), 51 with a figure (305 figure pages, 445 general formats), halves `account1`
and `account2` of 37 batches each (work 662 and 665), seven runs per half.

**Proof.** The self-tests of every touched script PASS; `verify_publishable.py --edition 1985` still holds the 1985
transcription to its ledger through the moved `pages_of`; `sweep_figures.py --edition 2002 --check` is clean;
`reconcile_ranges.py --check` fails on a planted change and passes on the file; a stubbed run of the workflow refused 12
malformed argument sets by name; the canary `page_workunit.py 1..12 --edition 2002 --dpi 300` rendered 12 pages from the
private PDF and wrote 12 non-empty slices. The fleet was not started.
