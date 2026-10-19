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
