## Entry 2269 — 2026-10-19 15:35 PDT — PB3382 - the crash matrix and the connector tests now refuse the sync of an UNCHANGED CLOSE

**What was wrong.** A CLOSE of a keyed store that completed no statement since its last promotion (its OPEN's fence, or an
earlier CLOSE) writes nothing and only syncs (`PagedFile.Close`, DESIGN-runtime-library 2.2.1.7, PB3333). A host that refuses
that sync must be answered '30' (ISO 9.1.13.6 item 1, "A permanent error exists and no further information is available
concerning the input-output operation"), never '00': the program would believe its file durable. The engine did this right,
but no test refused that sync: since 1a2728f7a the page-layer matrix's CLOSE scene always rewrote record 1 first, and
`KeyedStoreReloadFailureDriftTests` refused only the CLOSE of a connector that had written.

**Re-probe.** On c82c9213f the code is correct. Mutation: `return TrySync() ? Ok : PermanentError` replaced by
`TrySync(); return Ok;` failed only the changed CLOSE's refused-sync cell and nothing at the connector level.

**The fix (tests and design text; no engine change).**
- `PagedFileCrashMatrixTests`: `PagePath.CloseUnchanged`, whose scene leaves A unchanged. Its dry run is exactly one sync
  (no write, no read), so the death-before, death-after, refused and size-limit loops, the three continuations (next writer,
  fresh OPEN, power loss) and the invariants run on it. Its refused-read case asserts the CLOSE reads nothing, which is the
  PB3333 rule that an unchanged CLOSE cannot meet a page 2 another run unit left torn. `EndsTheRunUnitsUse` replaces two
  hand-written `Close or Fence` lists, and `The_CLOSE_scenes_pin_a_promoting_CLOSE_and_a_CLOSE_that_is_one_sync` pins both
  scenes' operation shapes so a scene that changes A on every CLOSE fails at once.
- `KeyedStoreReloadFailureDriftTests.ACloseOfAConnectorThatChangedNothingWhoseSyncTheHostRefuses_Is30_AndKeepsTheRecords`:
  RELATIVE and INDEXED, ALL OTHER, READ ONLY and clause-less NO OTHER, with and without a READ first; each answers '30', writes
  no page, and keeps the seeded records.
- DESIGN-runtime-library 2.2.1.16 gains the row of a CLOSE that owes no promotion.

**Mutation result with the new cells.** The page-layer `CloseUnchanged`/Refuse cell and all 12 connector cells fail under the
mutation; restored, all pass.

**Rows.** None claimed or closed (`closes_rows: []`, with the reason in the note).

