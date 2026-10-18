## PB3335: the whole-store model deleted, and coherence across run units completed at page granularity

kb/Work PB3335 is group 5 of the keyed store engine (PB3330), implemented from docs/rearchitecture/DESIGN-runtime-library.md
§2.2.1.7, 2.2.1.8, 2.2.1.11 (d) and 2.2.1.13. It follows PB3334 (same files). After it there is ONE statement path for
every keyed connector and every sharing mode, and no whole-file read or write exists under `Runtime/IO`.

**Re-probe.** By construction, on the merged predecessor's build. Both organizations already drove the engine, and what
the group owned was still there: `RecordFraming`'s store-level shape and its last callers, `KeyedStoreTable`, the
pass-through `KeyedConnector.CompleteStatement`, and two coherence defects in the engine's entry that PB3334 had reported
as a lead. Each defect now has a running case, and each case went red (6 reds) when its fix was taken out.

**Mechanism 1: coherence completed in the connectors.**
- *A reader's entry wrote under a SHARED mutex.* An `OPEN INPUT` connector beside a writable sibling in its own run unit
  holds the store mutex shared. Its `PagedFile.Enter` still ran as the writable store's entry, so a PROMOTING flag left on
  page 2 by another run unit's refused CLOSE was resolved with page writes under the shared hold. `PagedFile.Enter(asReader)`
  makes a statement through a read-only handle adopt page 2 and leave the mints and the flag to the next writable entry,
  which holds the mutex exclusively (2.2.1.7: "an INPUT connector ignores the mint and the flag, which concern writers
  only"). A torn page 2 leaves such a reader on the tree it holds.
- *The reader's re-adoption ran outside the mutex.* When the run unit's last writable connector closes and a reader stays,
  `KeyedStore.DetachHandle` reopens the store as the reader's: R6 in memory, reading pages 0 to 2. That ran after the
  CLOSE had given the mutex back. `KeyedConnector.Detach` now holds the mutex for it. `DetachHandle` returns its status,
  and a refused read leaves the store on the meta it holds.
- `StoreStatement.Complete` and `KeyedConnector.CompleteStatement` are deleted. They had been a pass-through since PB3333,
  because every verb writes its pages and page 2 before it answers. The 11 `FileRegistry` callers return the verb's status
  directly.
- `KeyedStoreTable` → `StoreTable` (`IO/StoreTable.cs`). The engine handle and its two views move to `IO/KeyedStore.cs`.
  `KeyedStore` keeps its name: it already plays the `PagedStore` role 2.2.1.7 describes. Every caller is moved, and
  `NoConnectorClearsASharedStore` is re-derived for the engine: no connector clears anything, and `PagedFile.Create` is
  reached only through `KeyedStore.Create`.

**Mechanism 2: the whole-store model's remains deleted.** `RecordFraming` is now the frame codec alone: the engine's leaf
value (`EncodeFrame`/`DecodeFrame`) and the stream shape. Deleted with their callers: `ComposeStore`, `DecodeStore`,
`ReadHeader`, `DecodeHeader`/`WriteHeader`, the magic, version and fixed-header constants, `MaxStoreBytes`, `GapBytes`,
`HeaderBytes`, `OrdinalSlots`, `FrameBytes`, `GapTag`/`EncodeGap` with `FrameKind` (`ReadFrame` answers bool), and the
generation probe. `ImageMedium` is now `ValueMedium`, the leaf value's medium. The key descriptor codec and `StoreFormat`
move to `Store/MetaPage.cs`, the one format that still records a key, and `SequentialConnector.IsKeyedStore` asks only
`MetaPage.Describes`. `PagedFile.MaxPages` and `MaxFileBytes` (2^32 pages, 2^44 bytes) name the format's boundary, and
`HostRegionLocks`' Windows note and FileStatus '24' cite them. The `OutOfMemoryException` mappings of the CLOSE and of the
coherent entry are removed; they existed only for the whole-store image's single allocation.

**Cases.** `StoreCoherenceTests` (13, both organizations, `[Trait("Host", "ReFS")]`):
- 2.2.1.17's coherence row, formerly `PendingStoreCases`' PB3335 row. A sibling's change is read at one page. Another run
  unit's change is adopted with the cache dropped: an INPUT-only run unit then reads exactly the cold-read count (1 + d),
  and a writable store adds the ledger's path (≤ 2). That measurement corrected 2.2.1.11's coherent-entry row.
- The two witnesses above, the re-adoption on both region hosts.
- 2.2.1.12's last R6 row: a crashed file (page 2 torn) read through INPUT ALL OTHER stays on the committed tree, '23' for
  the crashed statement's record, until another run unit's writable OPEN fences the file; from then on the reader sees
  page 2's tree.
- PB3330's quadratic case: two run units interleave a 2 000-record load through ALL OTHER at ≤ d + 6 page writes per
  statement, with no growth.
- The predicate per engine: the keyed store asks `AdmitsAnotherRunUnit`, while `AdmitsAnotherWriter`'s callers are kept
  and a READ ONLY sequential writer still buffers.

`WholeStoreBanDriftTests` (witness (d); it reads `Runtime/IO`, so `drift_rules.py` files it as a specific rule of those files, not a tree-wide sweep, and it carries no `Sweep` trait) checks three things: positional host I/O only in
`IO/Store/` and one page per call, no pooled buffer sized by a length, and no survivor of 2.2.1.13's deletion list. It was
shown red on a planted violation. `FrameDecoderDriftTests` has a new case for the leaf value being exactly one frame;
`RecordExtentsTests` goes through `EncodeFrame`/`DecodeFrame`; `FileBoundaryTests.MaxStoreBytes_*` is deleted; and the
engines' format-4/5 fixtures are written by `Store/WholeStoreFile`. No golden: no COBOL-observable status changes. The
statuses and their branches are the engine's, already witnessed by the PB3333/PB3334 cases these build on.

**Docs.** DESIGN-runtime-library §2.2 now describes the engine: the "ACROSS RUN UNITS" paragraph and its bullets,
"Emptying a keyed store", the 2.2.1 STATUS line, 2.2.1.7, 2.2.1.8, 2.2.1.11 (the coherent-entry row with the ledger;
witness (d) named), 2.2.1.13 (`StoreStatement` without `Complete`; the codec's move) and 2.2.1.14. Also updated:
COBOLNET_FILES_DESIGN (the format, the boundary, the per-file store, the medium sites) and docs/CONFORMANCE.md DOC-A.1-75
(a shared hold writes nothing), DOC-A.1-129 and DOC-A.1-151 (the frame codec).

**Register.** PB3335 is landed, with `closes_rows: []` and the reason. PB3336 is unblocked. Batch
`pb3335-whole-store-remains-witnesses` re-points 20 rows from the deleted members: GAP 66 → 66, `audit_witness_loss
--check` GREEN.

**Self-review.**
- Architecture: one entry rule (the mutex mode decides a reader's entry), one table, one codec per format.
- Full code: every citation was checked.
- Performance: a reader's statement now does strictly less work, and the CLOSE takes one more lock round trip only when a
  reader stays.
- Duplication: the stream helper `FillExactly` has a one-line twin in `MetaPage`. It was left rather than coupling the page
  format to the frame codec.

**Gate.** The first run, `TestResults/build-local/20261018T180903Z-d823b9`, was RED on one case,
`HostWitnessTraitDriftTests`. The ban test carried a `Sweep` trait, but `drift_rules.py` classes it by the depth of the
paths it reads, and `Runtime/IO` is specific, so the trait went and the docs now say so. The second run,
`TestResults/build-local/20261018T182734Z-f4edbc` (gated at `7c4351bca`), printed:
`=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN — leg 2 left to CI's whole-population run: Conformance
6,438 · Unit 358 · Characterization 31 — Conformance 11,241/17,679 · Unit 33,930/34,288 · Characterization 5/36 cases ran
(skipped 0) in 1 of 2 leg(s) ===`. Every `StoreCoherenceTests` and `WholeStoreBanDriftTests` case ran in leg 1 and passed.
LINUX GATE: the lander's (batched gating). Semgrep verify: PASS (one rule went 244 → 243, not this branch's).
