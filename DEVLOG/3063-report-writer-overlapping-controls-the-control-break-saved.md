## Entry 3063 — 2026-11-03 17:31 PST — Report Writer overlapping controls: the control break saved and stored per control, corrupting the user's item (PB4513)

Re-probe on the branch build: `CONTROLS ARE WS-REGION WS-KEY` (WS-REGION the first two bytes of WS-KEY), `MOVE "WEQRS"`, second GENERATE, then `DISPLAY WS-KEY` printed `EAQRS`, expected `WEQRS`; the report printed `EAQRS` for the detail line.

Cause: the control break (ISO 13.18.16.4 GR4 a) and TERMINATE GR3 a) both saved each control's current value and stored its prior in ONE loop. Two controls may refer to overlapping data items (13.18.16.3 SR6), so the second control's "current" was read after the first control's prior had been stored into the shared bytes; the later restore then wrote the corrupted value back and the corrupted prior caused a spurious later break.

Fix: one helper in `ReportWriter.cs`, `SaveCurrentRestorePriors`, in two passes (every current saved first, then every prior stored), used by `GenerateCore`'s break arm and `Terminate`. The other `Get()` on a control, the first-GENERATE priors snapshot, stores nothing between reads, so it needed no change.

Golden: `conformance-suite/cases/13.18.16-CONTROL/pb4513_control_overlap_restore` (accepted at 85, 2002, 2014, 2023): four GENERATEs and a TERMINATE, expected report lines and DISPLAYs derived from GR4 a) and TERMINATE GR3. No refusal golden: the CONTROL clause exists from 85. No inventory row closed; PB4513 claimed none.

Self-review: the helper is the only copy of the rule; no allocation beyond the one array each site already made; the restore order (major to minor over a consistent snapshot) is value-safe for overlapping items.

