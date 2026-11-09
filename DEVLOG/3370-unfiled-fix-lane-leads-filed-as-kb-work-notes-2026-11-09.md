## Entry 3370 — 2026-11-09 02:12 PST — Unfiled fix-lane leads filed as kb/Work notes (2026-11-09)

Five leads from merged fix-lane reports became notes, one per mechanism. PB5024: the margin-R warning COBOL6688 fires on
comment-entry text that ends at column 72 (origin PB4999). PB5025: APPLY COMMIT below 2023 draws the closed-format COBOL1970
instead of naming the edition (the last of PB2291's construct gates, after PB5007). PB5026: REMARKS is accepted at `--std 85`
although 1985 deleted it; `edition_gates.HELD_OPEN` named PB4996, which is the tooling slice, so this note owns the behaviour.
PB5027: `backfill_closes_rows` arms 1 and 2 credit a row to every note its text names (separate from PB5000). PB5028: the 1985
transcription checks PB4953 left undone, which no open note owned. The sixth lead, `CobolIntrinsics` reaching
`CollationEngine.TryGetOrderingTable` while classified non-resolving, was dropped: PB5019 already states it in its body.

