## Entry 2282 — 2026-10-19 19:29 PDT — PB3337: the transaction slice is the whole commit-and-rollback module (owner 2026-10-19 18:58 PDT: "Option A")

**The hold.** Tier 2 of the SORT and file I/O completion plan (kb/Work PB3420) — PB3337, the runtime's transactions for
indexed and relative files; PB3338, the compiler's APPLY COMMIT; PB3340, the same for sequential files — was held under
`status: owner` since 2026-10-18 "until the owner decides the transaction slice's scope", and no note said what the options
were. The owner, asked to decide, answered that no one had told them the options and that they would not decide without
information. The options were then laid out with what each delivers, loses and costs, and are recorded in PB3337: (A) the
whole module, Annex A.4.3 claimed entire; (B) keyed files only, a clause naming a sequential file refused although
§9.1.18.2 admits any mass-storage file; (C) the decline kept, the eleven §9.1.16–9.1.18 inventory rows closed as documented
non-support like the 48 beside them. **The owner chose (A): "Option A".**

**What changed.** PB3337 `status: owner` → `open` (its blockers PB3335, PB284 and PB3336 have landed, so it is READY and the
campaign planner ranks it after the tier-1 members, as PB3420 orders); PB3338 `owner` → `open`, blocked by PB3337; PB3340 was
already `open`, blocked by PB3338. PB3330's route paragraph, PB3420, PB3426, plan §0 and DESIGN-runtime-library.md 2.2.1 and
2.2.2 say the scope is decided. The one question the decision releases — one COMMIT over several files with one refused by
the medium: per file, as the design states, or all-or-nothing through a durable commit record outside any one file — is
asked with it, laid out the same way, and PB3337 carries the answer. No compiler source changed; nothing was dispatched.

