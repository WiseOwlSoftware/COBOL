**Landing paragraph (2026-11-14, w20262039a): the register check resolves a claim in every held edition.** `work_inventory.held_rows`
is the rows `work.py check` checks `inventory_rows` and `closes_rows` against: every held edition's view
(`inventory_view.edition_views`), each joined from that edition's catalog, so an open 1985 or 2002 row with no store file is a
row and a claim on it resolves; a `closes_rows` claim on an older row is judged by that row's own state (its change record's
chain). An id no held catalog has (a typo, a 2014 id while 2014 is not held) still fails by name. Measured on the clerk's
uncommitted plan-2002 notes PB5574-PB5586 copied into a scratch copy of the register: 221 unresolved claims with the
newest-only rows, 0 with the fix (`livecase`, w20262039a report). `backfill_closes_rows.py` reads the same rows.
**Still owed by this note:** COVERAGE (every defective row claimed by a live note) is asked of the newest edition only
(`coverage_rows`): widened to every held edition it names 111 1985 rows today whose notes list them in prose. Moving those
ids from the notes' bodies into `inventory_rows` (the PB5069-PB5091 and PB5158-PB5203 lists above), then passing every
held edition's rows as `coverage_rows` in `live_problems`, closes this note.
