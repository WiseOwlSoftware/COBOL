## Entry 3193 — 2026-11-05 21:25 PST — The spec files carry their edition; one PDF locator; the per-copy stamp redacted from the tree

The owner asked (2026-11-05) that every spec file name its edition, and that the reseller's per-copy stamp and any reference
to the purchaser be removed from what the repository publishes, history included.

- `specs/ISO_COBOL.md` is now `specs/ISO_COBOL_2023.md`, beside the planned `specs/ISO_COBOL_2002.md` (kb/Work PB4813) and
  `specs/ANSI_COBOL_1985.md` (PB4812). Every reference outside history moved in the same change (450 files: scripts,
  tests, workflows, skills, docs and kb/Work); there is no alias.
- The licensed PDFs in `specs-private` are `ISO_COBOL_2023.pdf`, `ISO_COBOL_2002.pdf` and `ANSI_COBOL_1985.pdf`. The 2023
  file's old name carried the reseller order number.
- `scripts/spec/spec_pdf.py` is the ONE locator (kb/Work PB3208, landed). The arrival of the 1985 PDF had made
  `clause_page.find_pdf`'s sorted `*.pdf` glob return the 1985 file and the `*COBOL*.pdf` glob ambiguous; all 16 callers
  moved to `spec_pdf.path(edition)` / `require(edition)`.
- The stamp text a reconciliation agent had quoted (purchaser's name and order number) is redacted in the spec-reconciliation
  ledger, its agents' files, two evidence files and DEVLOG 0581. `verify_publishable.py` now also reads every tracked file in
  the index (`check_tree`) and refuses an order number, a download date or "Licensed to" followed by a name; it leaves out
  the files no landing may carry (`coord.NEVER_WORK`), whose committed `.claude/settings.local.json` the history rewrite
  redacts.
- PB4759 (the cd-name debugging trigger) is open again: the 1985 Debug module is its source.

