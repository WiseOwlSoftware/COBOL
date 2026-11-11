## PB5121: at --std 85 a format 1 SEARCH starts the scan when the index is below the table

Re-probe on a fresh Debug build: the note's program (a PERFORM VARYING IX FROM 2 BY -1 leaves IX at 0, then SEARCH ELT ... WHEN IX = 2) printed AT END at --std 85; the 1985 text wants FOUND AT 2. The full witness of the note printed `5 AT END` where the draft expects `5 SECOND WHEN`.

Root cause: `ControlFlowEmitter.EmitSerialScan` emitted the §2023:14.9.37.4 GR4 start test (index < 1 or index > bound) at every edition. §1985:VI.6.22.4 GR2 ends the search at once only when the index is greater than the highest permissible occurrence number, and §1985:VI.6.23.4 GR2 lets a PERFORM leave an index outside its table. The 2002 transcription already carries the GR4 wording, so the edge is 2002.

Fix shape: a new `DialectBehavior.SearchIndexBelowTableUnsuccessful` (2002, VCR row 7.55, both editions quoted), read once by `SearchBinder.BindSearch` through `DialectBehaviors.IsActive` into `BoundSearch.IndexBelowTableIsUnsuccessful`, which the emitter turns into the start test. No inline year comparison. The drift registers (`compiler-facts.json`, `PublicAPI.Unshipped.txt`, VCR marker) are updated in the same change.

Sibling sweep: SEARCH ALL ignores the initial index (GR9), so it has no start test; the advance test in `EmitSerialScan` (`index > bound`) already agrees at both editions.

Goldens: `14.9.37-SEARCH/pb5121_search_index_below_table_85` (85, FOUND AT 2) and `pb5121_search_index_below_table` (2002 2014 2023, AT END). No refusal exists, the change gates behaviour and refuses nothing. Rows closed: none (no 1985 inventory rows yet, PB5094).
