## Entry 2247 — 2026-10-19 01:31 PDT — PB3417 and PB3418 retired as duplicates of PB3410, PB3414 and PB3415

The enqueue unit's leads clerk (PR #278) filed four leads at 00:15. Two of them were already in the register:
- PB3417 repeats PB3410 (the ledger's "at most 9 pages" bound has no assertion). PB3410 had landed with PR #275 at
  00:14, the base the clerk's branch was cut from.
- PB3418 repeats PB3414 and PB3415 (the READ ONLY keyed writer's mutex has no behavioural witness; two comments name the
  old release predicate). The operator had filed both from the same `-326-19` review at 23:23, on a branch then still
  waiting for its review (PR #276).

Both are retired by the register's convention (`status: retired`, "retired unlanded: a duplicate of ..."), and their
extra evidence is folded into the notes they duplicate: PB3417's doc-comment site and re-check command, and PB3418's
`ReadOnlyWriterReleaseDriftTests` case, the round-4 finisher's commit fe7a5cd58, the row's CONFORMS verdict and its
re-check command.

PB3416 stays open. It names PB2749 and sequences against it: a small replay-rule fix of its own, while PB2749's broader
extraction is blocked on PB2759. PB3419 (the `orchestrate.ps1` LaneTick comment that still names `STOP-UNIT`) is new.

