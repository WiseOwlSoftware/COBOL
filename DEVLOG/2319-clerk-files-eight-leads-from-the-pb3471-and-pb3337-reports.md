## Entry 2319 — 2026-10-20 20:36 PDT — Clerk files eight leads from the PB3471 and PB3337 reports (PB3478-PB3485)

The enqueue unit's clerk filed the leads of two implementer reports as kb/Work notes, one per mechanism, after grepping the register for each (no existing note covered any of them).

From the PB3471 report (`w1136c-PB3471-report.md`): PB3478 (17 Unit classes keep a private `Tmp` or `TryDelete`), PB3479 (`FileHarness.Register` defaults keyed connectors to dynamic access, so converted OPEN EXTEND cells model a declaration §14.9.27.3 SR2 forbids).

From the PB3337 report (`w1137a-PB3337-report.md`; filed blocked on PB3337, which landed first through PR #351, so the merge of main sets each `blocked: false` and they are ready): PB3480 (`FileRegistry.SharedClose` releases record locks on CLOSE of an APPLY COMMIT file, a wrong answer against §14.9.6.4 GR9 and §9.1.16, with its witness pinning '00' where '51' is required), PB3481 (two witness gaps: the abnormal-ending wiring and the non-discriminating retake test), PB3482 (Windows: the transaction-standing INPUT re-OPEN keeps a writable handle, a wrong answer against Table 19), PB3483 (`KeyedConnector.Lock` fallback loses the kept handle on any IOException), PB3484 (`CloseDisplaced` leaves the kept handle), PB3485 (HOLDINGS keys compare as bytes).

PB3486 of the allocated range stays unused. No code changed.

