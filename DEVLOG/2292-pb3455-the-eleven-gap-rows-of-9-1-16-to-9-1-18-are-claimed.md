## Entry 2292 — 2026-10-20 11:24 PDT — PB3455: the eleven GAP rows of 9.1.16 to 9.1.18 are claimed by the tier-2 notes

The owner's "Option A" (2026-10-19, kb/Work PB3337) gave the commit-and-rollback module to the tier-2 chain, but PB3337, PB3338 and PB3340 each kept `inventory_rows: []`, so no note claimed the eleven GAP rows and the register's burn-down could not see them. The re-probe on this branch (cut from ed4477ef2) reproduced it: all eleven rows GAP, DOCUMENTED-NON-SUPPORT, claimed by none.

Each row went to the note whose landing completes the rule. A row that quantifies over "files subject to commit and rollback" is false until sequential, line sequential and print files are covered (DESIGN-runtime-library.md 2.2.2.10), so the eight whole-facility rows (GR-9.1.16-p7, GR-9.1.16-p8, GR-9.1.17-p1, GR-9.1.18.1-p1, GR-9.1.18.2-p1, GR-9.1.18.3-p1, GR-9.1.18.4-p1, GR-9.1.18.5-p1) are PB3340's, the last of the chain. The three the compiler owns (SR-9.1.18.6-p1, GR-9.1.18.7-p1, GR-9.1.18.7-p2; 2.2.2.9 says the EC-FLOW conditions are the compiler's) are PB3338's. PB3337 claims none, and its note says why and that it supplies the keyed witnesses.

Each note carries a paragraph naming its rows and its `closes_rows_reason` says so; PB3338's stale paragraph (the inventory "has no rows for 9.1.16 to 9.1.18 until PB284", which has landed) is rewritten. Citations for 9.1.16, 9.1.17, 9.1.18.6 and 9.1.18.7 were run through `cite.py --check`. No code, grammar, golden or inventory verdict changed; the rows close through the three landings. `work.py check` passes (2747 items).

