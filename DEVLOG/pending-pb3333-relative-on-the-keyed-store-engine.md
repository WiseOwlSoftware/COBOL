## PB3333: RELATIVE on the keyed store engine

kb/Work PB3333 (wave 1118, group A3, the PB3330 cluster's third core group), built on PB3332's B+ tree and PB3331's page
layer, merged from `worktree-wf_14a1e62b-326-3`. It was a construction, not a fix: the predecessor's base held the tree and
the pages but no connector used them, so there was nothing to re-probe.

**The mechanism.** `RelativeConnector` keeps its §14.9.30.4 GR21 selection rules, its statuses and their DOC-A.1-104 order
and changes only its medium. Its store, `RelativeStore` (in `KeyedStoreTable.cs`, so the PB143 attach contract and the PB754
refusal are unchanged), is one `PagedFile` plus one B+ tree keyed by the 8-byte big-endian relative record number, whose value
is the record's frame through a new `RecordFraming.EncodeFrame`/`DecodeFrame` pair (the one head encoder and the one decoder,
`ReadFrame`; `ComposeStore` now composes from `EncodeFrame`). READ NEXT/PREVIOUS and START are seeks of the tree; WRITE,
REWRITE and DELETE are one page-layer statement each (`RelativeStore.Change`: edit in memory, `Begin` sized to the edit's
pages, flush, page 2 last). PB2755's quadratic walks went with the map. The page I/O goes through the run unit's connectors'
own handles (`Store/RunUnitPageMedium`), so no second handle is taken (PB771); a store opened through no writable handle is
opened again, and fenced, when a writable sibling attaches.

**Coherence.** The engine writes at every statement, so a relative connector is coherent under every sharing mode but NO
OTHER: `KeyedConnector.CoherentUnder` follows the engine, relative asks the new `FileLockPosture.AdmitsAnotherRunUnit` and
indexed keeps `AdmitsAnotherWriter` (finding R2-7's split). The statement's entry is `EnterStore` (relative: `PagedFile.Enter`,
page 2 adopted and the cache dropped; indexed: the whole-store reload), its completion `CompleteStore`, and OPEN and CLOSE run
under the mutex through `InStoreMutex`. The whole-store machinery left `KeyedConnector` for `IndexedConnector.WholeStore.cs`,
the one file PB3335 deletes; two of its protected members left the public surface, so the runtime major went 10 → 11 and
`RuntimeAbiPinDriftTests` was re-pinned.

**The boundary.** `StoreStatus.Boundary` is the format's 2^32 pages or the host's file-too-large refusal
(`ERROR_FILE_TOO_LARGE`, `EFBIG`), answered '24' at the WRITE or REWRITE; a full medium is '30' at that statement.
`StoreHolds` and `FramedBytesAfterPut` are gone for RELATIVE. `pb1192_relative_write_boundary_85` was re-derived from the new
DOC-A.1-107 text: record number 600 000 000 is one leaf entry, so it answers '00' with NOT INVALID KEY and reads back after
record 5. `FileBoundaryTests` gained the host-boundary case and the '24'/'30' refusal case; `RelativeStore_PredictedSize` was
deleted with the arithmetic.

**Corrections the build found.** (1) The note put a statement's writes at `StoreStatement.Complete`. The verb already knows
its pages and its '24'/'30', so the verb writes them inside the registry's statement, which is still inside the mutex
(2.2.1.7 corrected). (2) A CLOSE that promoted unconditionally failed with '30' over a page 2 that another run unit's death
had torn, even when the closing connector had changed nothing. `PagedFile.Close` now promotes only after a statement completed
since the last promotion and otherwise only syncs. (3) The page cache of 2.2.1.13 was not built, so RESERVE meant nothing to
the engine. `Store/PageCache` is LRU, its floor is the connectors' areas with a default of 256 pages, it is dropped on
adoption, and a hit is checked against the pointer's stamp. The page layer's stale-page and cold-read cases now drop it
explicitly, because a live store's cache holds the page as that store wrote it.

**Cases.** All 188 relative suite subjects (383 cases) and NIST RL1-RL4 plus IX106A pass over the engine. The new
`RelativeEngineTests` drive 2.2.1.16's crash rows and refused-statement rows through a real connector and real file
(`RelativeStore.MediumObserver` with `DyingPageMedium` or `RefusingPageMedium`). They cover a death after every write of
WRITE, REWRITE, DELETE and the CLOSE under ALL OTHER, READ ONLY and NO OTHER, where the next OPEN reads the last completed
statement's file, and a refusal at every write, where the statement answers '30', the error is replayed, the CLOSE ends it
and another run unit's next statement succeeds. They also cover 2.2.1.17's pair rows; the R2-6 regression (a READ ONLY writer
and an INPUT reader on two threads, where READ NEXT never answers '30'); a reader through a tree another run unit splits; a
round trip of gaps, varying frames, extent tables and a CODE-SET; a format-5 file answering '39'; and the tree's '22'/'23'
and torn-page '30'. The PB3333 rows of `PendingStoreCases` are now these cases.

**Register and docs.** Batch `pb3333-relative-engine-witnesses` re-pointed nine inventory rows' witnesses (GAP 66 → 66, none
re-verdicted). DOC-A.1-107 and the relative half of DOC-A.1-75 were rewritten. PB3333 and PB2755 are landed and PB3334 is
unblocked.
