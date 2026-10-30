## Entry 2786 — 2026-10-30 10:58 PDT — SET diagnostics name what the user wrote: the implicit-SET data-pointer arm and the Format 4 TRUE/FALSE phrase (PB3966, PB1314)

Two diagnostic-quality defects in one binder family (`SetBinder`, `PtrBinder`, `StatementValidation`), fixed at their
roots in wave 20261769 group G. Both are message-only: no program that compiled before is refused now and none that
was refused is accepted.

**PB1314.** `StatementValidation.RejectSetConditionName` hard-coded `TO TRUE` into both of its messages, so
`SET U TO FALSE` (U a plain item) was reported as `SET 'U' TO TRUE — 'U' is not a condition-name`. Re-probed on main
802352531: still reproduced. The helper now takes the phrase the operand's own group wrote (`bool toTrue`), because
ISO 14.9.39.2 Format 4 repeats the whole `{ condition-name-1 } … TO { TRUE | FALSE }` unit and a statement may mix
the two. Swept the other SET helpers the note names (Format 3 ON/OFF, Format 2/10/14 UP BY/DOWN BY/TO): they print
the written phrase already. The one other defect of the kind, a U+2026 in `PtrBinder.BindSetUpDown`'s no-receiver
message (the renderer transliterates it to a stray period), is reworded.

**PB3966.** `SetBinder.ScreenImplicitSet`, the screen INITIALIZE … REPLACING asks per receiving operand (PB3817), named
the statement and the receiver in its program-pointer, function-pointer and object-reference arms but not in its
data-pointer arm, which delegated to `PtrBinder.ScreenPointerReceiverRestriction` and reported
`'PU': the receiving data-pointer is restricted …` — no INITIALIZE, and no sign of which subordinate `PR` was
refused. `ScreenPointerReceiverRestriction` now takes the lead of an implicit SET's message, `RejectRestriction`
takes its lead as a parameter (its five other callers quote their operand), and `ScreenImplicitSet` builds ONE lead
for all four arms. Its sibling defect: `SetBinder.PointerAssignmentReason`'s `PicCategory.Pointer` arm was a second copy
of SR19 (14.9.39.3) reachable only through `ScreenImplicitSet`'s own guard order, under a doc comment that claimed the
BY CONTENT / BY VALUE argument lane calls it (it asks `ActivationRelations.MaySet`). The arm is deleted and the
comment says what is true.

**Goldens.** `pb1314-set-false-operand-not-condition-name` (a conforming TRUE group, then `U TO FALSE`: a message that
took its phrase from the first group or the statement would still misquote), `pb1314-set-false-switch-status-condition-name`,
`pb3966-initialize-replacing-data-pointer-names-receiver` and
`pb3966-initialize-replacing-data-pointer-restricted-sender-names-receiver` (the two sentences of SR19), each with its
`wiseowl/diagnostics.json` entry pinning the message text. No inventory row is closed (`closes_rows: []`).

