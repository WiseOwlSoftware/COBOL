## Entry 2235 — 2026-10-18 20:08 PDT — PB3334: INDEXED on the keyed store engine, and the keyed lifecycle written once; PB3335: the whole-store model deleted, and coherence across run units completed at page granularity; PB3336: the keyed store engine's performance witnesses, counted in pages, and the bounds the counts corrected

### PB3334: INDEXED on the keyed store engine, and the keyed lifecycle written once

Wave 1118, group A4 (campaign PB3330), on top of A3's RELATIVE engine branch (PB3331-PB3333, merged).

**Re-probe.** This was construction, not a fix. On the merged predecessor the INDEXED organization still ran on the
whole-store model (`IndexedConnector.WholeStore.cs`): a record list, `FindRandom` scanning every record, `Write` scanning
every record per key, and `Ordered` sorting the file on every READ NEXT (kb/Work PB2129: IDXHOT 3.3x-3.8x per doubling).

**Mechanism.** `IndexedConnector` drives `IndexedStore` (`IO/KeyedStoreTable.cs`): the PRIME tree (the prime key's channel
bytes → the record's frame, its ordinal vector one release ordinal per key the meta page records) and one COMPOSITE tree per
alternate key ((key bytes, 8-byte big-endian ordinal) → prime key), each under the order the key's collating sequence and the
file's CODE-SET select (`IndexedKeyOrders`). WRITE is n + 1 inserts under ONE ordinal minted from the meta page
(`PagedFile.ReleaseMint`, claimed by `Begin`; §14.9.51.4 GR40); a SUPPRESS WHEN value is no entry (§12.4.5.6.4 GR6, GR41 a));
REWRITE moves only the changed keys' entries to the end of their duplicates (§14.9.35.4 GR24 a)/b) by construction); DELETE is
n + 1 deletes; each is ONE page-layer statement over every tree it changes (`KeyedStore.Change`). READ by key, READ NEXT and
PREVIOUS (a fresh seek from the key value and the prior READ's ordinal; §14.9.30.4 GR21 d)-f) and the 2023 after-OPEN rule
unchanged) and START (a prefix seek under the key's order, §14.9.41.4 GR17; FIRST/LAST the prime tree's edges) are seeks; the
'02' look-ahead is the next entry's key part (GR27 a)). Every key comparison is the key order's over channel bytes, so the
string `KeyCompare`/`KeyEq` are gone with `Ordered`, `FindRandom`, `KeyedRec`, `IsSuppressed` and `IndexedConnector.WholeStore.cs`.

**The lifecycle, once (kb/Work PB2753).** With both organizations on one engine the §14.9.27.4 arms, the attach and adoption,
the CLOSE's promotion and §9.1.15 release, `AbandonOpen`, `FpiValid`, `StartFail` and the one `InvalidateFilePosition`
override live in `KeyedConnector`; the organizations keep `ResetPosition`/`Opened` and their verbs. `KeyedStore` is the one
engine handle both stores derive from (its `MediumObserver` the one fault seam); `KeyedStoreTable.Attach<T>` replaces
`AttachRelative`/`AttachIndexed`. The whole-store hooks (`RecordedAttributes`, `CoherentUnder`, `AllocateAreas`,
`EnterStore`, `CompleteStore`) collapsed into the base: the coherence condition is `AdmitsAnotherRunUnit` for both.

**Design corrections, measured.** An alternate key's tree entry key is the key and then the 8-byte ordinal, so the format's
bound is 1 008 bytes for an alternate key (1 016 for the prime): `KeyedConnector.FormatHolds` refuses a longer key, or
descriptors past the meta page, with '39' at the OPEN in every mode before the file is touched (2.2.1.4, DOC-A.1-129). The
`COBOLNET_KEYCHECK` opt-out drives the FILE's trees by key number; an alternate entry is a record's only while the record
still carries its ordinal, so a tree a narrower description does not maintain is passed over, never misread (DOC-A.1-129).

**Cases.** `IndexedEngineTests` (30): 2.2.1.16's death after every write and refusal at every write of WRITE/REWRITE/DELETE
and the CLOSE under ALL OTHER, READ ONLY and NO OTHER, read back by BOTH keys, with a second run unit's next statement;
2.2.1.17's shared duplicate order across run units and '02'; the R2-6 READ ONLY writer beside a concurrent INPUT reader; a
duplicate walk through a tree another run unit splits; START with each operator, full and partial keys, found and not found;
GR24 a)'s byte-identical untouched trees; the opt-out's trees; format 4 and 5 files '39'; the key bounds '39'; a torn tree
page '30'; 2.2.1.11's per-statement page bound. Golden `14.9.41-START/pb3334_start_indexed_tree_operators` (2002, 2023):
every operator on a full and a partial key, on the prime key and an alternate key, with a FILE STATUS item and with a USE
declarative. `FileBoundaryTests` re-derived (the n + 1 entries read back from the file's pages; the host's '24'/'30' at an
indexed statement); `KeyedStoreReloadFailureDriftTests`' indexed rows run on the engine (the whole-store reload rows went
with the reload). The PB3334 rows of `PendingStoreCases` are running cases. The 245 indexed conformance cases produce
byte-identical stdout on this build; IDXHOT runs 0.29 / 0.75 / 1.27 / 2.21 / 3.77 s at 2 000-32 000 records (about 1.7x
per doubling).

**Docs and register.** DOC-A.1-107 (both organizations, 2.2.1.9's text), DOC-A.1-129 (the meta page, the key bounds, the
opt-out's trees) and DOC-A.1-75's statement-mutex sentence rewritten; DESIGN-runtime-library 2.2.1.4 and 2.2.1.14 and
COBOLNET_FILES_DESIGN's duplicate-order, coherent-entry and opt-out paragraphs describe the engine. Batch
`pb3334-indexed-engine-witnesses` re-pointed 28 rows' witnesses (no verdict changed; GAP 66 → 66). Landed: PB3334, PB2129,
PB2753, PB2222 (`LastReadPrime` deleted), PB2342 (re-measured: `IndexedStore` is the KeyIndex seam); PB3335 unblocked.
Runtime major stays 11 (PB3333's, unlanded), the runtime ABI hash re-pinned.

**Self-review.** Architecture: one statement path per change, one lifecycle, one engine handle; the connector sees only
`IndexedStore`. Full code: every quoted citation re-checked with `cite.py --check`; two inherited ones were wrong and are
fixed (the "is not considered to exist" text is §14.9.30.4 GR21 c), not §12.4.5.6.4 GR6; §9.1.13.5 item 4 reads "write
outside the externally-defined boundaries of a physical relative or indexed file"). Performance: every positional question
is a seek; a sequential READ re-seeks from values (warm-cache page lookups) so it survives any interleaved change. Duplication:
one key comparison mechanism (the key orders), one frame codec, one boundary mapping (`Failed`). Silent failures: every
`StoreStatus` maps to '24'/'30' or a READ/START `Unsuccessful`.

**Gate.** Run `TestResults/build-local/20261018T170109Z-82a5d9` (gated at `ac19595c5`): `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN — Conformance 11,235/17,605 · Unit 33,858/34,267 · Characterization 5/36 cases ran (skipped 0) in 1 of 2 leg(s) ===`. The first gate (`20261018T165843Z-bc88d9`) was RED on one CITATIONS phantom (a design-section reference split across a doc-comment line, `KeyedStoreTable.cs`), fixed. The golden passed in leg 1 at 2002 and 2023; the 32 `IndexedEngineTests` cases passed. Semgrep verify PASS (one count down, not this branch's). LINUX GATE: the lander's (batched gating).

**Review round 1 (wave 1118 review finisher, branch `worktree-wf_14a1e62b-326-26`).** Four findings, all fixed. R1-2 first:
the branch carried PB3331-PB3333 at their implementer heads, so it merged the reviewed PB3331+PB3332 link
`worktree-wf_a49f0036-841-7` (14398a0e2) and PB3333's reviewed head `worktree-wf_14a1e62b-326-20` (7f5d2dcf8). The engine side
came from -841-7 (`IPageMedium.Length`/`SetLength`, `StatementBound`, the key length on `WeightedKeyOrder`,
`MediumSizeLimitException` replacing PB3333's first-cut `IsFileTooLarge`; `HandlePageMedium.Refusal` became the one place a host
code becomes the contract's exception, which the fault-injecting test media raise through). PB3333's connector fixes were
re-applied to this branch's single lifecycle: `KeyedConnector.Enter` passes the hold's exclusivity to `PagedFile.Enter`, and
R1-1's split, `KeyedConnector.RecordWriteFailed` ('24' for a record WRITE or REWRITE) beside `Failed` ('30' otherwise), now
serves both organizations, so the indexed DELETE at the host's boundary is '30' with no invalid key condition. -841-7's new
page-layer tests count medium reads, so they now drop PB3333's page cache before measuring; its refused-read matrix's CLOSE path
is PB3333's sync-only CLOSE and reads nothing; PB3333's refused OPEN OUTPUT case expects GR25's undo. R1-3: the indexed CLOSE
promotion, OPEN fence and OPEN OUTPUT creation are refused at every write and sync, and each OPEN is killed after every write.
R1-4: `IndexedEnginePermanentErrorWitnessTests` runs ten compiled programs over a torn file and over a file at its boundary.
They run in every USE x FILE STATUS combination with checking off and on, and every expected line comes from §9.1.14,
§14.9.30.4 GR13/GR24 and DOC-A.1-103. The boundary file is measured, not assumed: the fewest pages below 2^32 that let the OPEN
I-O's fence complete. That measurement showed a fence reserves its bound and so fails a few pages short of the format's end,
which design 2.2.1.9 now states.

Review round 1 gate: run `TestResults/build-local/20261018T193020Z-b4351c` (gated at `57b0af458`): `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN — Conformance 11,338/17,706 · Unit 34,000/34,359 · Characterization 5/36 cases ran (skipped 0) in 1 of 2 leg(s) ===`. Every new case passed in leg 1: the ten compiled programs, the CLOSE and OPEN refusal and death theories, the R1-2 sibling and the indexed boundary case. Semgrep verify PASS. LINUX GATE: the lander's (batched gating).

**Review round 2 (wave 1118, review finisher on branch `worktree-wf_14a1e62b-326-32`).** One finding, R2-1 (critical): 67
NIST programs failed `NistProgram_MatchesGolden` on the written-file check, and the round-1 gate never ran them (all 72 IX
and RL rows sat in its leg 2). The 30 RL programs failed because the branch did not carry A3's re-captured relative
goldens; merging PB3333's landing tip `worktree-wf_14a1e62b-326-20` (2f5b26d36, at d745abaab) brought them in, with its
stamped DEVLOG 2220. The 37 IX programs failed because the branch moves INDEXED onto the paged store (meta-page format 6)
while `tests/nist/valid-files` still held the format-5 whole store. At 56c69829c their written files were captured
through `COBOLNET_UPDATE_NIST_FILES=1`, 48 files in all: 41 format-5 goldens of 30 programs re-captured, and 7 new ones,
the `tf024.txt` of IX114A-IX120A, which main never captured. The CCVS reports, which read every record back, still
match. IX114A-IX120A each chain from IX109A's `tf024.txt`, open it I-O and CLOSE it with no record changed, and the
harness grades every file whose last-write time the run changed (`NistRunner.Written`). The whole store never wrote a file
at an unchanged CLOSE, while the engine's writable OPEN writes its fence (DESIGN-runtime-library 2.2.1.6, R6 step 4: a
promotion of the adopted meta, so pages 0, 2 and 9 differ from IX109A's file), so main never captured those goldens. The
sibling in the same blind spot was `SpecTraceabilityInventoryDriftTests.EveryCodeLocation_ResolvesInTheTree`: four rows
(GR-9.1.13.5-4, GR-14.9.51.4-33, GR-14.9.51.4-42, DOC-A.1-107) named the deleted `PagedFile.IsFileTooLarge`. A
`record_verdicts` batch re-witnessed them on the engine's boundary mechanism (no verdict changed; GAP 66 → 66), and
`FileStatusCode.BoundaryViolation`'s summary and `FileConnector`'s OPEN comment no longer describe the whole store.

The round-2 gates then found two more reds, both fixed here. (1) Both hosts' unit legs failed
`PagedFileCrashMatrixTests.A_refused_read_at_every_read_of_every_path_answers_30_and_decides_nothing(path: Close)`. The
d745abaab merge took A3's `SceneFor`, whose CLOSE scene changes A so the CLOSE runs its whole promotion, but kept this
branch's arm asserting that the CLOSE reads nothing; that arm is gone, so every read of the CLOSE's promotion is now refused
one at a time. (2) CI's guard leg, run from WSL, failed IX114A-IX120A on `tf024.txt`. `guard-run-group.sh` ran a chain
component in ONE shared directory, so a consumer ran over whatever the members before it left: IX115A, which declares only
IX109A, met the file IX110A and IX114A had fenced. `NistRunner` runs each program in a clean directory after only its
declared predecessors. The two inputs agreed only while an unchanged OPEN and CLOSE left the bytes alone. The group runner now
runs every program in its own clean directory after exactly its declared predecessors (`guard_chain_preds`, the manifest's
one shell reader of the chain-preds column, with self-test cases), so both arms grade the same input. The serial
`scripts/guard.sh`, which only the battery runs, still shares one directory and is a reported lead.

### PB3335: the whole-store model deleted, and coherence across run units completed at page granularity

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
  page 2 by another run unit's refused CLOSE was resolved with page writes under the shared hold. `PagedFile.Enter(exclusive)`
  makes a shared entry adopt page 2 and read the ledger and the mints as a writer does, but leave a PROMOTING flag to the
  next exclusive entry or the CLOSE, which holds the mutex exclusively (2.2.1.7: "an INPUT connector ignores the mint and the flag, which concern writers
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
`MetaPage.Describes`. `PagedFile.FormatPages` (2^32 pages of 4 096 bytes, so 2^44 bytes) names the format's boundary, and
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

**Finisher (round 1).** The branch had merged PB3334 at its unreviewed implementer head (kb/Work PB3380), so the finisher
merged the reviewed heads, `worktree-wf_14a1e62b-326-26` at `7e585e723` and then `worktree-wf_14a1e62b-326-20` at
`46934aa87`. The merged side won where it answers a review finding. Its `PagedFile.Enter(exclusive)` fixed the same
shared-mutex defect as this branch (A3 review R1-2), so its PROMOTING rule stands. Its `FormatPages` replaces this branch's
`MaxPages` and `MaxFileBytes`. `KeyedStoreTable.cs` stays deleted, and -26's host-refusal seam in `FileBoundaryTests`
carries the PB3333 side's REWRITE leg. Four inventory rows named members that no longer exist; one `record_verdicts.py`
batch retired them (GAP 66 → 66).

The review's two leads were fixed with their cases, as the owner's standing rule on file I/O tests requires. R1-L1: a torn
page 2 under the SHARED entry of an INPUT connector beside its writable sibling keeps the tree it holds ('00'), and only the
exclusive entry answers '30'. 2.2.1.7 now says the reader is the entry, not the store. The fault-injecting medium can
refuse a read, and a refused page-2 read is '30'. R1-L2: a refused re-adoption at the last writable connector's CLOSE was
answered '00' and left a writable store with no fence behind it, so the next `OPEN I-O` skipped R6's fence. The CLOSE now
answers '30' and the store's fence is owed. 2.2.1.16 gains the row. The torn-page case and the re-adoption case were red
on the unfixed code; the refused-read case witnesses behaviour that was right but untested.

The finisher's gates. The first Windows run stopped in its audits (FLEET PRACTICES): the merge commits had been staged
with `git add -A` while the worktree's submodule was still checked out at brent-tools 1.20.0, which reverted the 1.21.0
pin both merged heads carried. The pin is restored at `a828e3ea8`. The second run, `20261018T210832Z-926085`, was RED on
`PagedFileCrashMatrixTests.A_refused_read_at_every_read_of_every_path_answers_30_and_decides_nothing(path: Close)`,
expected 0 reads but 3. The two heads had changed that case's halves independently: -26 asserted that the Close scene's
CLOSE reads nothing, and -20 made the scene change A first, so the CLOSE promotes. The stronger form wins, as main and
PB3334's round-2 finisher have it. The run at `d3d2fdfd8`, `20261018T215211Z-457feb`, printed:
`=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN — leg 2 left to CI's whole-population run: Conformance
6,902 · Unit 709 · Characterization 31 — Conformance 10,915/17,817 · Unit 33,684/34,393 · Characterization 5/36 cases ran
(skipped 0) in 1 of 2 leg(s) ===`. The six new `StoreCoherenceTests` cases ran in leg 1 and passed. CI's Linux legs under
WSL at `8ec12f7cb` printed `=== LINUX GATE: RED (failed or not run: conformance guard; HEAD 8ec12f7cb; logs in
TestResults/linux-gate/) ===`. The unit leg (34,428), the characterization leg and both self-test legs were GREEN. Every
red of the conformance and guard legs is one of the 37 INDEXED NIST programs whose report matched but whose written file
differs from its format-5 golden. That is review w1118-a4 R2-1, on -26's reviewed head `7e585e723`, and it is fixed on
PB3334's round-2 finisher branch (`worktree-wf_14a1e62b-326-32` at `d633581e9`), which lands before this branch; the
landing's merge of origin/main brings the re-captured goldens in.

**Finisher (round 2).** Two jobs: merge PB3334's reviewed head before it lands, and add the test that review lead R2-L1
asked for. The branch merged `worktree-wf_14a1e62b-326-32` at `d633581e9` (PB3334's round-2 finisher, with the written-file
goldens of the 37 INDEXED programs, 41 re-captured and 7 new, that were the reds of this branch's Linux legs), then origin/main. Eight files
conflicted, and every resolution keeps both sides. The runtime hunks were doc comments and one blank line. The
`PagedFile.Enter` rule (`_writable && exclusive`) and `_fenceOwed` are unchanged. In `FileBoundaryTests`, -32's only change
was the loss, at its own merge `d745abaab`, of PB3333's REWRITE leg of the host-refusal theory, which main carries; this
branch keeps the leg. The inventory's four boundary rows keep this branch's text and take -32's witnesses through one
`record_verdicts.py` batch (GAP 66 → 66).

R2-L1: no compiled program checked a keyed CLOSE that answers '30'. `KeyedClosePermanentErrorWitnessTests` now has 20
cases. They cover both organizations, both refusals (the CLOSE's promotion, and the INPUT sibling's re-adoption) and
every USE × FILE STATUS combination, plus a FILE STATUS clause alone with EC-I-O checking enabled. The refusal comes from
the fault-injecting medium, which is per async flow, so the program runs in the test's own process through its generated
.NET host entry, and its BY REFERENCE formal carries its trace back. The expected values come from §14.9.6.4 GR4,
§9.1.13.6 1), §9.1.13.1 with DOC-A.1-103, and GR8 with DOC-A.1-105: the USE procedure runs, the FILE
STATUS item holds '30', the run continues under either clause and ends abnormally with EC-I-O-PERMANENT-ERROR when neither
covers the CLOSE or when checking is enabled with no USE procedure, and the next OPEN is '00'. The records survive a
refused promotion; after a refused re-adoption the sibling keeps reading, and the next OPEN I-O fences. All 20 cases are
red with both arms of `KeyedConnector.CloseCore` disabled.

The round-2 gates. CI's Linux legs at `ffc17d2d0` were RED on one unit case,
`AnnexA1RegisterDriftTests.EveryDocRow_IsFiledUnderTheItemAnnexA1Names`. The inventory's DOC-A.1-103 row named the new
case and the register's Pinned by cell did not, so the cell now names it (`d2530f7d2`). The conformance and guard legs were
GREEN at that head, because PB3334's re-captured goldens came in with the merge. At `d2530f7d2` the Windows run
`20261018T232911Z-0220c3` printed `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN — leg 2 left to CI's
whole-population run: Conformance 6,574 · Unit 569 · Characterization 31 — Conformance 11,245/17,819 · Unit 33,844/34,413
· Characterization 5/36 cases ran (skipped 0) in 1 of 2 leg(s) ===`, and the 20 new cases passed in leg 1. CI's Linux
legs printed `=== LINUX GATE: GREEN (legs selftests unit characterization conformance guard selftests-built; HEAD
d2530f7d2) ===`.

**Finisher (round 3).** The branch now lands PB3334 with it: PB3334's own landing bounced on a `docs/CONFORMANCE.md`
conflict with main's PB1880 landing, and its pending entry folds into this one at the stamp. Three review leads, all fixed.
R3-L1 is the landing conflict, in two parts. First the reviewed bounded-walk branch `worktree-wf_2ae0707b-b3d-4`
(PB3331 and PB3332 round 3, `5a0f7783c`), which is about to land, was merged at `9fdd84b1d`. Its one conflict was
`IndexedConnector.cs`: this branch had deleted the string-keyed engine whose `KeyCompare` b3d-4 changed, so the merge keeps
this branch's engine plus b3d-4's `SequenceOf` and `FreezeOpenSequences`, and `ResetPosition` builds each tree's
`WeightedKeyOrder` from the sequence frozen at the OPEN (§12.4.5.7.4 GR1: "Each collating sequence is a fixed file
attribute"). The golden `pb3331_locale_key_sequence_fixed_at_open` is red with the trees on the declared sequences and green
with the frozen ones. Then origin/main (`8f7df8caf`, at `5852ba65a`; `38926bffb`, clean, at `da8cd56d8`). Of the three
conflicted Annex A.1 rows, DOC-A.1-103 and DOC-A.1-107 keep this branch's text and pins, which already include main's, and
DOC-A.1-105 takes main's row, which adds the WITH LOCK sentence and three pins to text this branch never changed. The
carried PB3334 entry and note now give the golden count the tree supports: 56c69829c captured 48 IX written files, 41
format-5 goldens of 30 programs re-captured and 7 new (IX114A-IX120A's `tf024.txt`). R3-L2: the test's comment had
§14.9.6.4 GR8 backwards and cited `FileConnector.IsOpen`. GR8 takes the connector out of its open mode after a successful
CLOSE and says nothing of an unsuccessful one. DOC-A.1-105 is where an unsuccessful CLOSE also leaves the connector closed.
R3-L3: `KeyedClosePermanentErrorWitnessTests` gains three EC-I-O-checking covers, in both organizations and at both fault
sites, 32 cases in all. They are a format 1 USE procedure with and without FILE STATUS (§14.6.13.1.3 rule 3, then rule 7)
and a declarative for EC-I-O (rule 5). Each trace ends at "USE", and the run unit then ends abnormally with
EC-I-O-PERMANENT-ERROR. All 32 cases are red with both '30' arms of `KeyedConnector.CloseCore` disabled, and the new
covers are red at the trace too.

The round-3 gates ran at `a2f97fc91`. The Windows run `20261019T003930Z-e04bf9` printed `=== BUILD-LOCAL GATE: LEG 1
ONLY (batched gating, PB2515): GREEN — leg 2 left to CI's whole-population run: Conformance 6,592 · Unit 561 ·
Characterization 31 — Conformance 11,352/17,944 · Unit 33,953/34,514 · Characterization 5/36 cases ran (skipped 0) in 1
of 2 leg(s) ===`, and all 32 witness cases passed in leg 1. CI's Linux legs printed `=== LINUX GATE: GREEN (legs
selftests unit characterization conformance guard selftests-built; HEAD a2f97fc91) ===`. Both gates were also GREEN at
`da8cd56d8`, before the last two commits (the fourth cover, the counts and this entry) and the clean merge of b3d-4's
landing.

Round 4 answered R4-L1, the conflict the landing would have met: main had landed PB1587 (PR #265) on the whole store
after the round-3 tip. `90fc2692c` merged origin/main `bb24cae07`. The deletion of `IndexedConnector.WholeStore.cs` wins,
since PB1587's change there (`CoherentUnder` asking `AdmitsAnotherRunUnit`) is already the engine's rule in
`KeyedConnector.TakeFileLock`. PB1587's READ ONLY refused-persist case now lives in the engine's own refusal theories in
`KeyedStoreReloadFailureDriftTests`. `APageWriteTheHostRefuses_Is30_AtTheStatement` and
`ACloseWhosePromotionTheHostRefuses_Is30_AndKeepsTheRecords` run under ALL OTHER, READ ONLY and NO OTHER in both
organizations, all '30' (§9.1.13.6 1)). Main's whole-store persist test went with the model it tested. Measured: the
case PB1587 left for this engine, `AKeyedReadOnlyWriter_IsReadByAnotherRunUnit_AtTheWrite`, stays green even with the
predicate weakened to `AdmitsAnotherWriter`. On the engine a WRITE's visibility to another run unit is structural, because
the statement writes its pages before it returns. The predicate only serializes that writer against the reader. The
three `pb1587_write_released_*` goldens pass on the paged store, and none was re-captured because they pin standard
output only. In the inventory this branch's side was taken and main's four PB1587 rows were re-applied by the
`record_verdicts` batch `w1118a5-r4-PB1587-merge`. GR-14.9.51.4-12's dead `WholeStore.cs#CoherentUnder` witness now
points at `KeyedConnector.cs#TakeFileLock`, and the GAP stays at 66. DESIGN-runtime-library 2.2.1.7's "why not only ALL
OTHER" paragraph and the PB3334 and PB1587 notes now describe the engine.

The first round-4 gates at `1f86951c4` were RED twice, and both reds came from the merge. On Windows the witness-loss
audit stopped the run (`20261019T015012Z-c7d1ac`) because GR-14.9.51.4-12 had dropped main's `WholeStore.cs#CoherentUnder`
with no retirement mark: taking this branch's side of the row was a loss to the audit, not a retirement. Two
`record_verdicts` batches fixed it. `-2a` puts main's witness back and `-2b` retires it with its reason, so the audit reads
0 unexcused against main. On Linux, `StoreCoherenceTests`, a test only this branch has, still pinned the rule PB1587
retired: it expected a `SHARING WITH READ ONLY` sequential writer to buffer until CLOSE and `AdmitsAnotherRunUnit` to be
asked only in `KeyedConnector.cs`. It now pins PB1587's rule. The release asks about any other connector (READ ONLY and
ALL OTHER hand the record over at the WRITE, NO OTHER keeps it until the CLOSE). The append position, the ordinal count
and the shared append stream still ask about another writer only. The test is red at `1f86951c4` and green now.

The round-4 gates ran at `b3a83d9e8`. The Windows run `20261019T020846Z-c57ec5` printed `=== BUILD-LOCAL GATE: LEG 1 ONLY
(batched gating, PB2515): GREEN — leg 2 left to CI's whole-population run: Conformance 6,563 · Unit 493 · Characterization
31 — Conformance 11,384/17,947 · Unit 34,060/34,553 · Characterization 5/36 cases ran (skipped 0) in 1 of 2 leg(s) ===`,
and the renamed `StoreCoherenceTests` case ran in its leg 1. CI's Linux legs printed `=== LINUX GATE: GREEN (legs
selftests unit characterization conformance guard selftests-built; HEAD b3a83d9e8) ===`.

### PB3336: the keyed store engine's performance witnesses, counted in pages, and the bounds the counts corrected

kb/Work PB3336 is group 6 of the keyed store engine (PB3330): the witnesses (a) to (c) of
docs/rearchitecture/DESIGN-runtime-library.md 2.2.1.11, on the engine PB3331-PB3335 built. The branch carries the
predecessors at their REVIEWED heads (origin/main for PB3331-PB3333, then PB3335's branch `worktree-wf_14a1e62b-326-19`,
which carries PB3334); only PB3336 lands here, and each predecessor's note keeps the status its own landing gives it.

**Re-probe.** On the merged build the witnesses did not exist, and neither did the seam they need: `PagedFile` counts its
page I/O (`Counters`) and drops its cache (`DropCache`), but no test could reach a connector's store. The seam is
`KeyedConnector.PagedStore` and `FileRegistry.PagedStoreOf` (internal, read-only).

**The cases.** `tests/Cobol.Net.Tests.Unit/Store/KeyedStorePerformanceWitnessTests.cs` is one abstract class with one derived
class per file shape (RELATIVE; INDEXED with no alternate key; INDEXED with one alternate key WITH DUPLICATES and one unique),
29 cases each, cells ascending and random keys × NO OTHER and ALL OTHER. They drive the real connectors through a
`FileRegistry`, with the pages in `PageArrayMedium` (new: an in-memory page array per physical file that keeps the latest
version of each page, because `MemoryPageMedium` keeps every version, which the crash cases need and a 20 000-record load
cannot afford). The host file still carries the handles and the locks. The 87 cases take about 4 minutes in parallel.
- (a) Linearity: a load of N = 5 000 and then 4N records, sizes that straddle the prime tree's root split (the note's
  N = 20 000 put both sizes at depth 3). Every WRITE stays within its row, the ledger stays within R2's checkpoint, and
  the 4N mean stays within the N mean plus the depth growth, the ledger's growth and the FREE batch.
- (b) An OPEN after a CLOSE reads 5 pages and an I-O OPEN writes 5 and syncs twice, at N and at 4N alike. After a process
  death the OPEN reads exactly 3, plus the ledger tree's pages, plus its fresh pages, and adopts the dead run unit's
  completed statements (R6).
- (c) 1 000 cold random READs by the prime key read exactly d pages, plus 1 under the mutex. By the unique alternate key
  they read its path plus the prime tree's.
- The READ NEXT walk reads each page of the tree once. Its end answers '10' (ISO §14.9.30.4 GR24 a)), and along the key
  WITH DUPLICATES '02' comes back exactly when the next record shares the value (GR27 a)).
- The exception paths, each beside its success:
  - a WRITE refused '22' for the prime key or the unique alternate key (§14.9.51.4 GR42 b), c); GR33 a));
  - a READ and a DELETE refused '23' (§9.1.13.5 3) a));
  - a WRITE the medium refuses, '30' (§9.1.13.6 1)); after the CLOSE the next run unit's WRITE is within its row and the
    refused record is absent;
  - the permanent error's replay (§9.1.13.1), one case per verb: WRITE, READ, READ NEXT, START, REWRITE and DELETE, in both
    sharing modes. Each answers '30' and reads, writes and syncs nothing, not even page 2: docs/CONFORMANCE.md DOC-A.1-105
    says such a statement "does not touch the physical file", so `KeyedConnector.BeginStatement` does not enter the store
    (no store mutex) and no lock question is asked first. The condition also outranks another run unit's lock: a READ and a
    DELETE of a locked record answer '30', not '51', with no RETRY pause;
  - a READ that meets another run unit's lock, '51' (§9.1.13.8 1)), then RETRY 3 TIMES: four bounded attempts
    (§14.7.9.3 GR1), and a retry that sees the lock released succeeds (GR4).

  None of them writes a page.

**The design corrected from the counts (2.2.1.11).**
- The ledger's share of a statement is not "≤ 2". The ledger is keyed by page number and loaded whole, and a scattered-key
  statement releases pages anywhere in the file. A random relative load writes about 5 ledger pages a statement at 20 000
  records (ascending: 1), and with two alternate keys the ledger reached 1 028 entries on 9 pages. R2's checkpoint bounds
  this share, so the load stays linear. The rows now carry ℓ.
- A split adds s pages, one per level that splits.
- An OPEN after a death reads the ledger tree too, and so does a writable store's adoption.
- A READ by an alternate key reads 1 more page when its composite's prefix seek crosses a leaf (13 to 17 READs in 1 000).
- A new row covers the unsuccessful statements; a replayed permanent error touches nothing.
- An OPEN after a death verifies up to R2's checkpoint PLUS one statement's pages: `PagedFile.Begin` promotes only a
  ledger that already lists more than 1 024, so the statement that begins there adds its own first (1 028 measured).
- The stale `PagedStore` names in 2.2.1.11, 2.2.1.12 and the coherence tables now say `PagedFile` or `KeyedStore`.

**Self-review.**
- Architecture: one seam from a connector to its paged store, and no second fault seam (`MediumObserver` hands
  `PageArrayMedium` in).
- Full code: every citation passed `cite.py --check`, and each expected value comes from a 2.2.1.11 row or a general rule.
- Performance: the seam is a property read.
- Duplication: the B+ tree page walk is the test's own, sharing `PageHeader`'s and `PagePointer`'s codecs.

Lead: the ledger could be organized so that a statement's entries land on one leaf. It is in the wave-1118 A6 report.

**Review round 1 (three findings, all fixed).**
- R1-1 (high): the branch carried PB3331-PB3335 at their implementer heads and flipped their notes. Fixed by merging the
  reviewed content in order, origin/main (cba91326e) and then `worktree-wf_14a1e62b-326-19` (f5ee2a9cd), with every file
  outside PB3336's own set taken as the reviewed side has it; the predecessors' notes are theirs again.
- R1-2 (medium): the replay witness had pinned "reads page 2", which contradicts DOC-A.1-105. The replay is now answered
  before the connector enters the store, and the per-verb cases above assert zero pages in both sharing modes.
- R1-3 (low): the OPEN-after-death row said "up to the 1 024"; the row and the ℓ sentence now give the checkpoint plus one
  statement's pages, the bound the witness asserts.
- Cleanup owed to rule 4: nothing was left. PB3335's reviewed head had already deleted `RecordFraming.ComposeStore`,
  `DecodeStore`, `MaxStoreBytes`, their test and the `OutOfMemoryException` catch of `FileConnector.MappedCloseCore`.

**Gate.** At 93ef4cbd1 (the predecessors' reviewed content merged: origin/main, then PB3335's branch at b3a83d9e8),
run `TestResults/build-local/20261019T020842Z-9a08d6`, the 36 replay cases Passed in leg-1-Unit.trx:
`=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN — leg 2 left to CI's whole-population run: Conformance
6,563 · Unit 493 · Characterization 31 … Conformance 11,384/17,947 · Unit 34,147/34,640 · Characterization 5/36 cases ran
(skipped 0) in 1 of 2 leg(s) … ===`. Linux, the same commit: `=== LINUX GATE: GREEN (legs selftests unit characterization
conformance guard selftests-built; HEAD 93ef4cbd1) ===`. The gate one merge earlier (c13feb6cf) was RED on exactly the two
reds PB3335's branch then had from its PB1587 merge (a lost witness of GR-14.9.51.4-12 and one StoreCoherenceTests case,
red on its own gates too); they were fixed there and merged here, not fixed twice.

