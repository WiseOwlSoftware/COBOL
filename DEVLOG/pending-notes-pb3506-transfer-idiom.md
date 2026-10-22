## File PB3506, the bare completion-idiom transfer renderers (PB3491 review leads)

The PB3491 branch review left two low, non-blocking leads in one mechanism. `EmitterState.TransferOut`'s doc comment still names it the renderer for GO TO, DEPENDING and RESUME AT although those now leave through `ExplicitTransferOut`, and `DispatcherTransferIdiomDriftTests.OnlyACompletionCallsTheBareTransferJump` pins the callers of bare `TransferJump()` but not those of bare `TransferOut(...)` (EXIT PARAGRAPH, NEXT SENTENCE). This entry files them as one note, `kb/Work/PB3506.md`, with line numbers re-checked on main. No compiler change.

The same landing also records a third recurrence of PB3124 (PR #360: two merge_conflict removals, one successful merge-group run, so `landing_measure.py complete` refused the record until the merge time was entered by hand); the evidence is in `kb/Work/PB3124.md`.
