## Entry 2351 — 2026-10-22 11:27 PDT — File the PB3498 review lead as PB3518

**What.** The branch review of PB3498 (PR #382) left one non-blocking lead, R1-1: the new exhaustion witness in `HandleColumnLockHostDriftTests` expects `Unavailable` for a SHARING WITH ALL OTHER I-O request while its setup holds a kept READ ONLY INPUT column that Table 19 refuses, and `HandleColumnLockHost.Establish` returns `Unavailable` on exhausted publication before testing the refused columns. Filed as `kb/Work/PB3518.md` (LOW, runtime/io, open). Whether a user program should see 61 or another status in that case is left to an adjudicator.

**Why.** CLAUDE.md rule 8: a finding becomes a note before it becomes a paragraph. Searched `kb/Work` by member (HandleColumnLockHost, Establish, Publish) and symptom first; only PB3482 mentions the type and it is a different defect.

**Scope.** Notes only; no code or test changed.

