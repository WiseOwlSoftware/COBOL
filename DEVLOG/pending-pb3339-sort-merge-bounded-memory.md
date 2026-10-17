## PB3339: SORT and MERGE keep their records in bounded memory, with sorted runs on temporary files

The owner, on PB3330's keyed files (2026-10-17): "Reading the entire file into memory as a design requirement is a fatal
flaw, IMO". The operator swept the runtime for the same pattern. The sequential organizations stream through their
handle, and the collation loaders read locale data, not user files. The SORT/MERGE store did not stream:
`SortFileTable.Records` was a `List<StoredFrame>` of UTF-16 record images, `CobolSort` sorted it into a second full list
and never wrote to disk, so a SORT whose input exceeded the process's memory ended in OutOfMemoryException. GnuCOBOL 3.2
bounds its sort memory (`COB_SORT_MEMORY`, default 128M), writes sorted runs to temporary files and merges them, and
kb/Work PB3339 (BLOCKER) set the same bar. Wave 1110 group A took it.

**Re-probe.** At 5ee921375 the code sites were as the note measured (the store list, the index `Array.Sort` and its full
copy, MERGE's third list, no disk write). The structure is the defect, so the re-probe is the code and the new bound test.

**Design first.** COBOLNET_DESIGN §8.2 gained "The sort-merge store is an EXTERNAL MERGE SORT in BOUNDED MEMORY",
DESIGN-runtime-library §2.2 the work-store paragraph (and the HostFile roles became five), and docs/CONFORMANCE.md §3 the
determination D-SWK: the limit (`COBOLNET_SORT_MEMORY`, K/M/G suffixes, default 128 MiB, floor 64 KiB), the temporary
files (`COBOLNET_SORT_TMPDIR`, else the host temp directory; delete-on-close on Windows, unlinked at creation elsewhere),
an ordering independent of the limit, a loud stop on a work-file failure, and the two undefined results the storage
settles (a RETURN before the sequence phase is at end, a RELEASE after it is not returned). §13.4.6.1 is the licence:
the SD entry "furnishes information concerning the physical structure pertaining to a sort or merge file" and says
nothing of where records live.

**The mechanism.** `SortWork` holds one in-memory chunk, charging each record its resident estimate (image, frame,
extent table, decoded keys). A record that would pass the limit seals the chunk: a SORT sorts it stably and writes a
run, a MERGE appends each USING stream's part to that stream's run. Runs of one level merge F at a time (`FanIn`, the
limit over four 64 KiB buffers, 2 to 16), always the last F, so release order is kept. The sequence phase is in memory
when nothing was written; otherwise the return phase is a k-way merge (`SortMergeCursor`) whose tie goes to the earliest
run, the one rule that yields §14.9.40.4 GR3 a)/b) and §14.9.24.4 GR4 a)/b). `SortRunFile` stores each image's UTF-16
code units unconverted and its extent table with an interned layout index, so U+FFFF, a lone surrogate and the
`RecordExtents.Layout` object survive a run. The key comparison moved unchanged to `SortKeyColumns`, now slot-based.
`CobolSort.Init` takes the keys (the store must order runs during the release phase), so `Sort` and `Merge` dropped
theirs; that changes the shipped surface, so the runtime major went to 9 (over PB1099's 8, which main took first) and the ABI pin was re-pinned over both surfaces. GR6's MERGE
check runs as records arrive and `Merge` raises it before merging. Sequencing triggered by a RELEASE or RETURN runs
under the statement's `RaisedBySortMerge` mark (§14.6.13.1.3 2)).

**Witnesses.** `SortWorkBoundTests` measures the bound (peak resident bytes within the 64 KiB floor for an input at
least eight times it) and checks every arm against an order computed independently: stable SORT, GIVING rewind from
disk, numeric DESCENDING and national keys, a three-stream MERGE, GR6 on a stream already on disk, the repertoire and
extent table through a run file, no disk for a fit, and a loud missing directory. `SortWorkBoundDriftTests` refuses any
record field in the sort-merge types but the three bounded ones and a chunk add not behind the limit test.
`SortWorkSpillConformanceTests` runs a 30,000-record SORT GIVING two files and a MERGE of them as its own process at 64K
and at the default; both print the spec-derived lines and leave the work directory empty.

**Rows.** None claimed, none closed (`closes_rows_reason` in the note).

**Gate.** The first gate (20261017T200110Z-58386c) was RED on SpecTraceabilityInventoryDriftTests: five inventory rows (GR-14.9.24.4-3/-5, GR-14.9.40.4-2/-5/-8) named the comparison's old home in CobolSort.cs, and batch w1110a-pb3339-key-columns-moved moved them to SortKeyColumns.cs (5 retired with reasons, GAP 58 to 58). The re-gate 20261017T201827Z-e715fb: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (Conformance 3,872 · Unit 33,562 · Characterization 5 cases ran in leg 1; leg 2 left to CI).

**Branch review, round 1 (two findings, both fixed).** R1-1: a run file was owned by whichever list or cursor held
it at the moment, so a failure in the windows between those owners leaked it until a GC finalized its stream: the
cascade's output in `MergeInto` (created, not yet in the run list), a new run in `WriteSortedChunk`, and the stream
runs `SequenceMerge` had already cleared from its list before merging them. The fix gives every open run file ONE
owner, `SortWork._files`. `NewRun` registers a file before its first record is written, `MergeInto` closes its inputs
only after its output is complete, and the run lists, `SortRun` and `SortMergeCursor` only refer to files, with
neither of the last two disposable any more. `Dispose` closes the set, and `OpenRunFiles` is its count. Two new
witnesses inject a failure that only a merge of two runs can meet, through a collation that throws when it compares
two sentinel keys each released into a different chunk: a SORT failing in a cascade and a MERGE failing while it
merges three spilled streams. On the reviewed code they fail with 1 and 2 files still open; with the fix every file
is closed at `Close`. The sibling: `WorkDirectoryIsEmpty` was vacuous on Linux, where a run file is unlinked when it is
created. It now counts the `/proc/self/fd` links that name the test's directory, so every "leaves no file" assertion
in `SortWorkBoundTests` has force on both hosts. R1-2: two crefs in CobolSort.cs still named the removed
`Init(string, CobolCollation?, CobolCollation?)`. Both now name `Init(string, Key[], CobolCollation?, CobolCollation?)`.
A runtime build with XML documentation on shows no unresolved cref left in the sort files. The finisher's gate
20261017T205734Z-8b53d4: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (Conformance 11,921 ·
Unit 33,575 · Characterization 5 cases ran in leg 1, the two new witnesses and the drift test among them).

**Branch review, round 3 (one finding, fixed).** R3-1: round 1's R1-2 sweep grepped for the cref spelling of the
removed `Init(string, CobolCollation?, CobolCollation?)` and missed the prose spelling. So DESIGN-locale-facility.md,
the deep-dive rule 2 says to implement from, still told its reader in two places (the SORT/MERGE locale snapshot
paragraph and the T1 landing row) that both sequences are snapshotted by `CobolSort.Init(name, collation, national)`.
A reader following the doc would write a call that does not compile, and would never learn that Init now takes the
statement's key descriptors. Both places now name `CobolSort.Init(name, keys, collation, national)`, and the snapshot
paragraph says why the keys moved there: the bounded store orders a run whenever the release phase fills its memory
limit, and `SortWork` holds the keys and both snapshots for the whole statement. The witness is
`SortRuntimeDocShapeDriftTests.EveryDocCallOfTheSortRuntime_NamesAShippedShape`, a tree-wide sweep with its own class. It reads every backquoted `CobolSort.<Member>(args)`
call in `docs/` and requires a public CobolSort member that takes it. That means the right arity, and an argument
spelled as one of the member's parameter names must stand at that parameter's position. Position matters because the
removed three-argument spelling has an arity the new Init still accepts. On the reviewed doc text the test fails and
names both stale calls; on the fixed text it passes. The same kind of check over every runtime type's prose calls in
`docs/` is wider than this branch. A rough survey finds about 14 suspect calls in other subsystems' docs, and they are
reported as a lead rather than fixed here.
The first round-3 gate (20261017T220947Z-2fa2b0) was RED on HostWitnessTraitDriftTests: reading `docs/` made the
class holding the witness a tree-wide sweep, and that class lacked the `Sweep` trait. The witness moved to its own
class, so SortWorkBoundDriftTests keeps only the store's specific rules. The re-gate 20261017T221618Z-42bd2c:
`=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (Conformance 11,579 · Unit 33,577 ·
Characterization 5 cases ran in leg 1, the new witness among them).

**Branch review, round 4 (one finding, fixed).** R1-1 (review w1111c): the round-1 sibling sweep re-sited only the
five inventory rows whose `#Member` anchors broke. The rows whose anchors still resolved kept evidence about code
that no longer exists, because the mechanism moved into SortWork.cs behind them. GR-14.9.24.4-1 said `CobolSort.Merge`
"k-way merges all streams into f.Records", GR-14.9.24.4-4 said "Merge picks the head with STRICT less-than",
GR-14.9.24.4-6 said "CobolSort.Merge scans each USING stream", and GR-14.9.40.4-3 / -4 said `CobolSort.Sort` "ties on
the buffer index". Both members are now thin calls into `SortWork.Sequence`, and no row named SortWork.cs.
Ten rows are restated through `record_verdicts.py` (batch `w1111c-pb3339-sortwork-trace`: +17 code-location and +9
test-ref witnesses, none retired, GAP 58 -> 58): GR-14.9.24.4-1, -4, -5, -6, -12, -14 and GR-14.9.40.4-3, -4, -9, -15.
Each now names the SortWork member that carries the rule (`Sequence`, `SequenceMerge`, `WriteStreamChunks`,
`NextStream`, `CheckOrder`, `OrderViolations`, `SortedOrder`, `Cascade`, `Rewind`, `SortMergeCursor`) and the spill
witnesses (SortWorkBoundTests, SortWorkSpillConformanceTests), and its notes describe the bounded store.
GR-14.9.24.4-4 also drops a stale "test-needed": the row's own golden and the spill tests cover it. Every quoted rule
text was re-checked with `cite.py --check` (14.9.24.4 1), 4) a), 6), 12); 14.9.40.4 3) b), 9), 15): all OK). The
regression witness is `SortWorkTraceabilityDriftTests.EveryStatementRuleSortWorkCites_IsARowThatNamesSortWork`: every
`§14.9.24.4 GRn` / `§14.9.40.4 GRn` that SortWork.cs cites must be a row whose code-location names
`SortWork.cs#<member>`, so a rule the file takes on later is held without editing the test. On the reviewed inventory
it fails and names the six untraced rules; on the fix it passes. The same stale claim, "the in-memory sort store", was
swept out of four comments (CobolSort.AssociationMade, FileConnector.AssociationFailure, SequentialIoEmitter,
OoEmitter).
