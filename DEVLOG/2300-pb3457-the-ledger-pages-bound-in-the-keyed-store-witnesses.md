## Entry 2300 — 2026-10-20 14:45 PDT — PB3457: the ledger-pages bound in the keyed-store witnesses says it is measured, not derived

Branch review R1-1 of PB3410 found that the `LedgerPagesAtCheckpoint` doc comment in `KeyedStorePerformanceWitnessTests`
read as if 9 pages followed from 1 024 entries plus one statement's pages. It does not: a full ledger leaf holds
4 064 / (6 + 4 + 9) = 213 entries, so 1 028 entries need only 5 leaves, and because the ledger is keyed by page number a
scattered load leaves its leaves half full to full (merges start below a quarter page). The measured cell (1 028 entries
under two alternate keys) is the root over 8 leaves, about 60 percent full.

Fix: the constant's comment now calls 9 the measured maximum of the twelve load cells and names what it depends on (entry
size, page size, split rule); the `Load` comment says "the most this workload measured"; the design's 2.2.1.11 paragraph
says the ledger "held at most 9 pages ... in the measured workload". No assertion or code changed, so the witnesses are
unchanged. kb/Work PB3457 landed, `closes_rows: []` (a comment that overstated its bound; no inventory row).

Gate: LEG 1 ONLY (batched gating, PB2515) verdict recorded in the implementer's report.

