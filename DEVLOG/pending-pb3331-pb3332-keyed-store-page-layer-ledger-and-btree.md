## PB3331 and PB3332: the keyed store engine's page layer, generation ledger and B+ tree, simulator first

One branch lands groups 1 and 2 of PB3330's engine: PB3331 (the page layer and the generation ledger) merged into PB3332 (the B+ tree over it), with every branch-review round of both answered.

### PB3331 — the keyed store engine's page layer and generation ledger, simulator first

kb/Work PB3331 is group 1 of PB3330's engine (DESIGN-runtime-library.md 2.2.1). PB3330's Route of 2026-10-18 put the
deterministic fault-injecting page medium of 2.2.1.12 FIRST, with the rules R1-R6, the crash points of 2.2.1.16 and the
interleavings of 2.2.1.17 as executable cases written from the design text before any engine logic, and the design
corrected from what they found. That is the order of this branch: the first commit holds the medium, the cases and the
`PagedFile` API with no logic; the second the engine; the third the design corrections.

**Re-probe.** A construction note: on a fresh build of main (0 warnings, 0 errors) `src/Cobol.Net.Runtime/IO/Store/`
did not exist and no connector reached a paged store. Nothing to discharge.

**The simulator** (`tests/Cobol.Net.Tests.Unit/Store/MemoryPageMedium.cs`). An in-memory page array that keeps every
version of every page, logs every write (page, generation, kind, run unit) and every sync, and can lose every unsynced
write or any chosen subset of them (a power loss), tear a page, or serve an older version of it (a lost write over a
reused page). Each run unit reaches it through its own handle, whose fault plan kills it before or after, or refuses, its
n-th write, sync or operation. `StoreKit` adds a path-copying two-level test tree over `PagedFile` (kb/Work PB3332's B+
tree does not exist yet) and an auditor that rebuilds both slots' trees from the raw medium as they stood at each write.

**The cases.** R1 at every write; R2 after every statement; R3's no (page, generation) pair twice, no mint decreasing,
and the rerun after an abandoned history stamping above it; R4 after a CLOSE (and the next OPEN's fence) or a checkpoint;
R5's sync order, slot alternation, both-slot read, retirement order and restart rule (landed, unlanded, an unlanded
COMMIT's claim unwound). Every operation of every page-layer path — a coherent statement, a NO OTHER statement, growth,
CLOSE, the checkpoint, the fence, creation — is killed before it, killed after it and refused at it, and each run is
continued by the next writer, a fresh OPEN and a power loss; the expected state is the last completed statement (or, after
a power loss, the last durable one), never a torn one, with a live INPUT reader checked at every point. The R6 matrix's
no-transaction column and the page-layer rows of 2.2.1.17 are named cases. The layout goldens read every offset off the
PAGE and META tables. The transaction slice's rows (PB3337) and the rows the later core groups' paths own (PB3333-PB3335)
are `PendingStoreCases`: data with their expected results, not run.

**The engine** (`src/Cobol.Net.Runtime/IO/Store/`). `PageHeader` (32 bytes, CRC-32C), `PagePointer` (12 bytes,
stamped), `MetaPage` (paged format 6; `Describes` for §14.9.27.4 GR10: Empty, Described, Foreign, and the new
`StoreFormat.Damaged`, R6's '30'), `PageChain` (overflow chains, the INTERIM serialization of the ledger and the FREE set
until PB3332's trees replace both), `Ledger`, `IPageMedium` with `HandlePageMedium` (`RandomAccess` through the
connector's own handle), and `PagedFile`: the claim, the reservation, page 2 last, promotion and retirement, the restart
rule, the adoption rule and its fence, creation, CLOSE and the I/O counters. RecordFraming's key descriptor became one
codec, `WriteKeyDescriptor`/`ReadKeyDescriptor`, shared by the whole-store header and the meta page. Layout constants:
page 4 096 bytes, header 32, body 4 064, chain payload 4 056, pointer 12, meta keys from offset 169,
`GenerationCeiling` 4 096, `LedgerCheckpoint` 1 024, format version 6. No connector calls any of it yet (PB3333 is the
first caller), and no inventory row changed (`closes_rows: []` with its reason).

**What the cases found in the engine.** On the first run, a `released` page reused for the ledger's own chain kept its
`released` entry, so the ledger listed its own page as reusable and a later statement overwrote a page page 2 still
referenced (R1 red, R2 red). The chain's pages now leave the list.

**What the cases found in the design** (2.2.1.4, 2.2.1.6, 2.2.1.7, 2.2.1.12, 2.2.1.14, 2.2.1.16 corrected). The
checksum covered bytes 8..4095 and left the kind, flags and entry count unprotected. It now covers every byte but its
own four. "Format version 5" collided with the whole-store header's version 5 (kb/Work PB1099), so the paged format is
version 6, with its magic at offset 32. A per-key root after the prime key's descriptor duplicated root[0]; only an
alternate key's descriptor carries one now. Creation set the length to three pages, which left the old store's meta
adoptable after a death mid-creation; it now empties the medium first. A promotion promoted page 2 at its own generation
g, so refused claims since the base could push the retirement's claim past g + 4 096, inside the window the next
fence reissues. A promotion now promotes at a fresh claim P. A restarted retirement writes its claim first. A rejected
history's grown pages are reused by growth and never enter the FREE set. A torn page 2 at a live entry answers '30' to a
writer and leaves a reader on its tree. The fence's end state is the committed meta and page 2, not "both committed
slots".

**Mutations** (each turned cases red, then reverted). With the fence at G + 1, the first R2-2 case stayed green, because
the rejected slot's readable mint protected it. A second case loses page 2's newer versions while their pages survive,
and asserts that the rerun stamps above the abandoned history; that case caught the mutation. With no claim on page 2,
9 cases went red. With the promoted ledger's own pages freed into FREE (finding R3-2's bug), 31 went red.

**Self-review.** Architecture: one engine type per job, the page layer's rules in `PagedFile` alone, no caller yet.
Code: every refusal (an `IOException` or `UnauthorizedAccessException`) becomes '30' and nothing else is caught. A torn
read is a typed `PageCheck`, never an exception. Performance: the interim chains rewrite the whole ledger and FREE set
per statement, bounded by the 1 024-page checkpoint, and PB3332 replaces them. Duplication: one key codec, one checksum,
one chain codec. The design's references in code drop the § sign (`2.2.1.x`), so the citation audit reads only ISO
clauses as clauses.

**Gate.** The first gate (run 20261018T125745Z-135db2) was RED on `FrameDecoderDriftTests`. Its selector treats every
32-bit little-endian decode under `IO/` as a second frame decoder, and it read the page layout's own fields that way.
The test now lists those decoders by file and member as `PageLayoutHomes` and requires each one to still exist. The
second gate (run 20261018T131036Z-c945b0): `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN — leg 2
left to CI's whole-population run: Conformance 6,392 · Unit 341 · Characterization 31 … Conformance 11,156/17,548 ·
Unit 33,824/34,165 · Characterization 5/36 cases ran (skipped 0) in 1 of 2 leg(s) ===`. All 73 Store cases ran in its
leg 1. The Linux gate runs at the landing (batched gating).

**Branch review, round 1: five findings, four mechanisms.** The reviewer read the branch and found the abnormal paths
thinner than the successful ones. Each finding was re-probed on the finisher's build and fixed at its root.

*The boundary (R1-1).* 2.2.1.9 says a WRITE or REWRITE that would take the store past 2^32 pages, or past the host's
maximum file size, answers '24' (§9.1.13.5 item 4) with the file unchanged, while a full medium answers '30'. The page
layer had no way to say it. `Reserve` threw an `IOException` out of `Begin`, `Open` and `Close` at the format's end, and
the host's `EFBIG` was just another '30'. Now `StoreStatus.Boundary` exists. `Begin` takes its reservation before its
claim, so a statement past the format's end claims nothing and writes nothing. `HandlePageMedium` reports the host's
"the file cannot grow further" as `MediumSizeLimitException`, classified by `HostFile.IsFileSizeLimit`, the narrower half
that `IsMediumBoundary` now calls, so the host's codes are still read in one place. A promotion, CLOSE and OPEN have no
'24' and answer '30'.

*Refused reads (R1-2).* The fault-injecting medium could not refuse a read, so no refused-read arm of the engine ran in
any case. With `FaultAtRead` added, the cases found a silent defect. At OPEN, a refused read of a DESCENDANT's ledger or
fresh page was taken as "the page does not verify": the work slot was rejected and its statements were dropped, with
'00'. `TryLoad` and `FreshPagesVerify` now tell a refused read from damage, and a refused read is '30' that decides
nothing. Every read of every path is now refused in turn, along with a reader's entry and tree reads, an INPUT OPEN and
the restart rule's slot reads.

*Creation (R1-3, R1-4).* The truncation was not an operation of the simulator, and the creation case accepted either
answer at every point. The design's two creation rows also contradicted each other. Pinning every cell against the spec
showed that the zero-first creation broke §14.9.27.4 GR25 (*"If the execution of the OPEN statement is unsuccessful, the
file is not affected"*). Every refusal after its truncation destroyed the previous store. It also restarted stamps at 1
over pages the previous store had stamped. `Create` is now a promotion of the empty store with one durable write. The
three meta pages are read and kept. The empty meta goes into the slot that does not hold the committed meta, at a
generation above every stamp the previous store can have issued (the fence's bound), and is synced. Then come page 2,
the other slot, the one truncation to three pages, and a sync. A refusal before the last sync restores the previous
bytes and length. The cases pin every operation over a previous store, an empty file and a foreign file, each continued
by an OPEN, a power loss and a re-creation, with no stamp reused. The 2.2.1.4 paragraph and the 2.2.1.16 rows are
rewritten from what the cases showed, and rows are added for a refused read and for the boundary.

*Text (R1-5).* `PageCheck.ChecksumFails` and the note's Mechanism still named the first draft's layout: a checksum over
bytes 8..4095, and format 5. The sibling in PB3330's design summary said the same. All three are corrected.

Each mechanism's mutation turned its cases red and was reverted: a refused read served as zeros, a creation with no
undo, the claim written before the boundary check, the size-limit refusal answered '30', and the created store at
generation 0.

**Branch review round 2 (the second review finisher).** Two findings, both fixed.

*The previous file's length (R2-1).* The undo of a refused creation kept the previous file's length as the medium's
page count, which is the length in whole pages, rounded down. So a refused OPEN OUTPUT over a 5 000-byte file (a
format-5 whole store, or any foreign file) cut it to 4 096 bytes and never restored a page the file held only part of,
against GR25. The test medium kept its length in pages too, so no case could express the file. The medium contract is
now a length in BYTES (`IPageMedium.Length` and `SetLength`, replacing `PageCount` and `SetPageCount` in both media and
every caller). The undo restores every meta page that held any previous byte and then cuts the file back to its exact
length. `MemoryPageMedium` keeps bytes, zeroes a truncated page's tail and builds a previous file of any length
(`Holding`). The creation case now runs every operation over files of 0, 5 000, 8 000, 12 000, 16 384 and 20 000 bytes
and asserts the file byte for byte after each refusal; the previous-store case asserts the same. A new case drives the
production medium over a real 5 000-byte file. The mutation that put back the whole-page length turned the 5 000-,
8 000- and 12 000-byte cells red.

*The DELETE at the boundary (R2-2).* The design gave the boundary an I-O status only at a WRITE or REWRITE ('24'), but
a DELETE goes through the same `Begin` and `Complete` and can meet it too, since its tree and ledger rewrite can need a
growth page. The spec leaves a DELETE no '24': §9.1.13.5 item 4 is an attempt to write outside the boundaries, and
§14.9.10.4 GR3 and GR4 give a DELETE the invalid key condition only when the file does not contain the record. So a
DELETE at the boundary answers §9.1.13.6 item 1's '30'. The page layer does not know which statement it serves, so the
connector answers by the statement. Decision 6, 2.2.1.9, the 2.2.1.16 boundary row, the `StoreStatus.Boundary` doc and
PB3331's landing line now say so, and `PendingStoreCases` carries the relative and indexed connector cells for PB3333
and PB3334.

### PB3332: the keyed store's B+ tree over the page layer, with the LEDGER and the FREE set on the same engine

Group 2 of PB3330's keyed store engine (wave 1118, group A2), built on PB3331's page layer, which this branch merged
and lands with it. Implemented from docs/rearchitecture/DESIGN-runtime-library.md 2.2.1.4, 2.2.1.6 and 2.2.1.12.

**The re-probe.** Construction, not a fix: the merged base had the page layer and no tree, and no connector reaches the
store, so there was nothing to reproduce and nothing to discharge.

**The tree.** `IO/Store/BTree.cs` and `BTreeCursor.cs` (one partial class): variable-length keys (at most 1 016 bytes)
and values, ordered by one `IKeyOrder` (`KeyOrders.cs`: ordinal bytes, which is also the numeric order of a relative
record number or a page number stored big-endian; weighted, which decodes the channel bytes and compares them under the
key's collating sequence exactly as `IndexedConnector.KeyCompare` does; composite, the key part then the release
ordinal). Every change is made in memory — the path copied, nodes split, merged or borrowed — and flushed once per
statement: each changed node is written exactly once, children before parents, from the statement's reservation, and
every page the tree stopped referencing goes to the ledger. So R1 and R3's no-pair-twice corollary hold by
construction, and a statement knows its page count before `PagedFile.Begin` reserves it (`Begin(..., entered: true)`).
Values past the inline rule go to an overflow chain (`OverflowChain.cs`, the successor of PB3331's `PageChain`, which
is deleted with the interim payloads). Every read verifies its page against its stamp; a torn or stale page answers
'30' and discards the pending edit. Seeks cover =, >, >=, <, <= and a START prefix (§14.9.41.4 GR17 b) to e)).

**The LEDGER and the FREE set on the engine.** The ledger is a resident tree (page → generation, state). Because it
never lists its own pages, its write settles before it writes anything: superseded ledger pages become `released`, a
reserved page it takes leaves the list, an untaken reserved FREE page becomes `released`, repeated until nothing moves
(with rebalancing off, so its page count only grows and the loop ends). The FREE set is the FREE tree: a reservation
deletes its batch of lowest pages in memory and the completion flushes it; a retirement inserts the moved pages.

**What the cases found in the design** (2.2.1.4 corrected in the same branch): a leaf cannot carry a sibling pointer
under path copying, so `next` is 0 and a cursor crosses leaves through its path; the inline rule needs an entry bound
(half a body) besides the value's 1 016 bytes, or two maximal entries do not share a page and a split cannot always
make two pages; a midpoint split leaves a sequential load half full, so an append splits off only what it added and a
node is rebalanced only when a change shrank it; the fan-out figures hold for 8-byte frames and are now given with
their formula; the START prefix is GR17 b) to e). A cursor whose tree another run unit changed must re-seek:
`BTree.Adopt` re-roots the tree after a coherent entry.

**The cases.** `BTreeModelTests`: a `SortedDictionary` model under the ordinal, numeric, weighted (alphabet "ZYX…A")
and composite orders, with values inline, at 1 016 and 1 017 bytes, at 4 065 and of 1 MiB, grown to three levels and
shrunk to one and to nothing; after every statement the tree is re-read from the medium in both directions, with every
seek relation and a START prefix, and its shape audited by a raw decoder. `BTreeShapeTests`: the fan-out (184 leaf
entries of 22 bytes, 184 internal keys) and the depth at its boundaries (184, 185, 34 040, 34 041); a cold READ reads
the depth and a scan reads each page once; a one-record statement writes the depth, the ledger's path (at most 2 over
3 000 statements) and page 2, and the file grows only once the released pages are taken (R2); stale and torn pages,
internal and overflow, refused (R3); the FREE tree gives its lowest pages first and the file grows only past it; a
cursor re-seeks after `Adopt`; the key orders' rules. `BTreeCrashTests`: a statement that splits, merges and swaps
inline and overflow values, killed before, killed just after and refused at each of its writes, coherent and NO OTHER,
continued by another run unit's statement, the refused run unit's own statement and CLOSE, an OPEN, and a power loss:
the file is always the last completed statement's, R1, R2, R3 and the mints audited. PB3331's 73 cases run over the
real tree now (its `StoreKit` caller drives it; its auditor decodes the 2.2.1.4 layouts raw). Five `PendingStoreCases`
rows hand the tree's COBOL surface — START relations with INVALID KEY taken and not taken, AT END, '22', '23', and '30'
with and without a USE declarative and a FILE STATUS item — to PB3333 and PB3334.

**Mutations** (each turned cases red, then reverted): a borrow's separator not updated; a superseded page not
released; the ledger listing the pages it takes; the internal append split dropped — which the depth boundaries alone
missed, so the second level's fan-out check was added.

**Rows.** None: no connector calls the tree yet (PB3333 is the first), so `closes_rows: []` with its reason; PB3333 is
unblocked.

**Self-review.** Architecture: the tree knows pages only through `PagedFile.Read` and an `IPageSink`; the reservation
and every R-rule stay in `PagedFile`, the ledger's settling in `Ledger`. Full code: every cited rule re-checked with
`cite.py --check` (the START prefix is GR17 b) to e), not the b) the note carried; the alphabet rule is §12.3.7.4 GR7
k) 3.; the native key order §12.4.5.7.4 GR4). Performance: one write per changed node per statement, reads only on the
walked path, the ledger resident. Duplication: one node codec, one overflow-chain codec, one statement path; the test
auditor's raw decoder is the format's independent second reading by design.

**Gate.** Run `20261018T142837Z-cf7664`, all 93 Store cases in leg 1 and passed: `=== BUILD-LOCAL GATE: LEG 1 ONLY
(batched gating, PB2515): GREEN — leg 2 left to CI's whole-population run: Conformance 6,273 · Unit 312 ·
Characterization 31 … — Conformance 11,275/17,548 · Unit 33,873/34,185 · Characterization 5/36 cases ran (skipped 0) in
1 of 2 leg(s) …`. The Linux gate is the landing's (batched gating).

**Branch review, round 1 (three findings, the review finisher in worktree-wf_14a1e62b-326-16).**
- R1-1, a wrong answer for a legal record: the reservation sized the LEDGER from the pages a statement RESERVES, but a
  statement that drops a committed overflow chain adds one `freed` entry per page of it during the flush. A DELETE of
  a 4 MB record (1 000 overflow pages) threw `InvalidOperationException` out of `PageStatement.Complete`, and every
  retry met the same chain. `Begin` now takes a `StatementBound` (pages written, pages released), read off the pending
  edits by `StatementBound.Of(trees)` so no caller counts by hand; `Reserve` adds the FREE tree's own superseded pages,
  and `Ledger.WriteBound` counts one change per release with a level-by-level bound for the entries a write adds.
  Cases: 1 000- and 2 069-page records deleted and shortened, committed and not, then CLOSEd into the FREE tree; the
  1 000-page DELETE killed, killed just after and refused at each of its writes.
- R1-2, a cursor stepping through a node changed in place: a cursor detected a change by comparing the root, which a
  second edit of a node the pending edit already copied leaves equal, so it returned an entry twice or skipped one. The
  tree now carries a change counter (edit, flush, discard, adoption); `ReadValue` after a change looks the value up by
  key and refuses a deleted current entry loudly. Cases: the review's two probes, Replace/Delete/Discard under a
  cursor, and a seeded model walk of more than 1 000 steps forward and backward with edits and flushes between steps.
- R1-3, PB3331's review fixes missing from the branch that lands it: worktree-wf_14a1e62b-326-10 merged and re-derived
  on the tree (11 conflict hunks in `PagedFile.cs`): the boundary answered by the tree's `Reserve` before the claim; a
  refused read told from damage at the resident ledger's load (`BTree.LoadResident`/`Ledger.Load` return the
  `PageCheck`), so R6 step 3 never rejects a descendant for a refusal; refused-read cases at every tree read point (a
  statement's entry and ledger adoption, path fetches, the overflow supersede, the FREE batch; a scan; an OPEN of a
  descendant). PB3331's round-3 finding R3-1 is answered here too: the creation case runs a sub-page previous file
  (1 000 bytes) and its length classes are described by the page each ends in.
- Mutations, each red and then reverted: the root comparison (3 cursor cases red), the releases left out of the bound
  (5), a refused read at OPEN taken as damage (3, PB3331's fence case among them). A dangling `PageChain` cref in
  `PageHeader.cs` was fixed on the way.

**The landing gate (wave 1122, group A, the review finisher resumed).** The branch was resumed at 2d38a9173, merged with origin/main 6fb6a20f8 (clean), and its two pending entries folded into this one. Every round's findings were re-verified against the code (review-w1118-a2.jsonl R1-1..R1-3, review-w1118-a.jsonl R1-1..R1-5, R2-1, R2-2, R3-1, and the loop's review of worktree-wf_14a1e62b-326-3, whose R1-1..R1-7 restate them). Run `20261018T165332Z-bc0116`, 131 Store cases in leg 1 and passed: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN — leg 2 left to CI's whole-population run: Conformance 6,294 · Unit 336 · Characterization 31 … — Conformance 11,383/17,677 · Unit 33,902/34,238 · Characterization 5/36 cases ran (skipped 0) in 1 of 2 leg(s) …`. The Linux gate is the landing's (batched gating).

**Branch review, wave 1122 A (review-w1122-a.jsonl R1-1).** The reviewer found that a START's partial key was narrowed by binary search under every key order, and that this is valid only when the order is PREFIX-CONSISTENT (a key below another never has a greater prefix). A LOCALE-BASED sequence is not: §8.8.4.2.11 says the comparison *"is not necessarily performed character-by-character"*, and under the root locale "áa" < "ab" (primary a,a < a,b) while their one-character prefixes order "á" > "a" (secondary). Over the keys áa, ab, ác, ad, NOT LESS THAN "á" answered ác where §14.9.41.4 GR17 c)'s sequential search takes áa, and LESS THAN "á" answered ab where d)'s reverse search takes ad. The fix states the property where it lives: `CobolCollation.UsesStandardComparison` (internal abstract, so every collation arm answers it: the literal-phrase arms true, §8.8.4.2.7 1)'s position-by-position comparison; the locale arm false), `IKeyOrder.PrefixConsistent` derived from it (ordinal and the composite's key order alike), and `BTree.Seek` under a prefix and an inconsistent order walks the tree entry by entry in GR17's direction, which is how `IndexedConnector.Start` searches every key today. Cases: `BTreeShapeTests.A_START_prefix_under_a_locale_sequence_is_the_sequential_search_of_GR17` (the reviewer's scenario and the reverse arms) and a locale model run, `BTreeModelTests.The_tree_agrees_with_a_sorted_model_under_a_locale_order_whose_prefixes_are_not_consistent`; both were red with the flag forced true. The design's 2.2.1.4 key paragraph now states the rule.

The review finisher's gate: run `20261018T173458Z-985656` was RED on the citations audit before any leg ran (two new `GR17` citations read against the preceding clause, §14.9.30.4 and §8.8.4.2.7; both now name §14.9.41.4, 5d2767aab). The re-gate, run `20261018T173724Z-302b9e`, 133 Store cases in leg 1 and passed, the two new cases among them: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN — leg 2 left to CI's whole-population run: Conformance 6,437 · Unit 433 · Characterization 31 … — Conformance 11,240/17,677 · Unit 33,807/34,240 · Characterization 5/36 cases ran (skipped 0) in 1 of 2 leg(s) …`. The Linux gate is the landing's (batched gating).

**Branch review, wave 1122 A, round 2 (review-w1122-a.jsonl R2-1).** The reviewer found that the R1-1 fix sent EVERY START prefix under a locale order to the walk, a START key as long as the record key too. §14.9.41.4 GR17 b) sets the first temporary area's length and c) truncates the second *"to the same length as the first"*, so at the key's whole length nothing is truncated, the prefix comparison IS the key order and the binary search is exact; the walk instead read every leaf before the answer (the reviewer's scenario: a full-key START GREATER THAN above the last of 1 000 000 records reads the whole prime tree to answer '23'), against PB3330's bar that no statement reads the whole file. `IKeyOrder.PrefixConsistent` became `PrefixNarrows(prefixLength)`, the one question the cursor asks: the ordinal, the positional sequences and a composite over them narrow at every length, a locale sequence at the key's length only, which `WeightedKeyOrder` now takes (it refuses a longer key or prefix in the one place every key is decoded, so a whole-length prefix can never truncate after all). Case `BTreeShapeTests.A_whole_length_START_key_under_a_locale_sequence_reads_the_depth_and_not_the_file`: 3 000 six-character keys under the root locale (depth 2, 15 leaves), every relation at present, absent, below-first and above-last probes against the sorted list, each seek's pages counted: depth to depth + 2 (a LESS relation settles forward, then back across one boundary), and the reviewer's GREATER THAN above the last reads exactly the depth. With the cursor forced back to the walk it was red (`Range: (2 - 4) Actual: 15`). The locale model run now draws a whole-length prefix one time in four; the key-order case asserts both arms of `PrefixNarrows` and the length refusals. A SHORTER partial key under a locale sequence still walks, as GR17 states; that reads the file, so the design's 2.2.1.4 now records it as an open defect of the engine, not an accepted cost, and it goes to the register as this round's lead (a narrowing sound under the locale engine's contractions and ignorables is a design beyond this slice).

**Branch review, wave 1122 A, round 3 (review-w1122-a.jsonl R3-1), finished in wave 1123 B.** The Mythos lane stopped the round-3 review before it ran: a design text that records an open defect is not acceptable (owner, 2026-10-18 ~11:10 PDT: *"We should implement the best, most performant, long-term supportable and maintable design."*), so this link implements the Mythos design decision of 2026-10-18 (rules 3 and 4): a SHORTER partial START key under a locale-based sequence is GR17's sequential search started at a PRIMARY-LEVEL BOUND and ended at the end of the run that ties it. *The marks.* Each tree keeps two high-water marks on its ROOT page's header (bytes 24..27, formerly reserved): the PREFIX DEFICIT g (the largest cut minus the level-1 weights before it, over every key) and the CONTRACTION OVERHANG c − 1 (the longest run between two cuts, less one). An INSERT raises them (`IKeyOrder.MarksOf`, once per inserted key), a DELETE never lowers them, an emptied tree drops them, a root split moves them to the new root and zeros the old one, and a collapse onto an untouched child copies it to carry them. The root page is the place because path copying rewrites the root at every change: the marks are crash-consistent with the root the meta names, travel with another run unit's root through `BTree.Adopt`, and need no meta field. *The shape.* `Collator.PrefixShapeOf` walks a text once through the element iterator's new mapping API (`TryNextMapping`, `TryNextOfMapping`, `MappingStart`/`MappingEnd`, the contraction walker's `MatchEnd` for a discontiguous match's furthest mark; `TryNext` is now built on them) and, for a text walked in NFD, through the normalizer's ORIGINS (`Normalizer.ToNfd(text, table, origin)`), so it counts positions of the text itself; a CUT is a mapping start that no earlier mapping reached past and that is a starter. `LocaleCollation` takes it over the operand §8.8.4.2.11 compares (*"trailing spaces are truncated from the operands except that an operand consisting of all spaces is truncated to a single space"*), so padding counts for nothing. *The bound.* m = k − g − (c − 1); B = the probe's first min(m, |Pp|) level-1 weights. Implementing it found a hole in the decision as written: the trimming makes a truncation that ends in a stored key's INTERIOR spaces a proper prefix of a probe that has a TAB there (a weight below a space's), so the truncation is below the probe while the stored key's own space weights place it above a bound that kept the TAB, and the LESS relations skipped it. The bound is therefore cut before the first weight below a space's (and a tailoring with a contraction that holds a space takes the empty bound); 2.2.1.4 states the cut and why. *The walk.* `BTree.Cursor.Search` seats at the first entry at or above B (forward) or the last at or below it (reverse) by the tree's binary descent — `Bound` is now one generic binary search over a probe (`KeyProbe`, `BoundProbe`), shared with `PathTo` and the whole-key `Position` — and walks GR17's comparison only through the run: the entry past it answers GREATER and NOT LESS (and ends EQUAL with none), the entry before it LESS and NOT GREATER. The empty bound is GR17 from the first or last entry as written; ordinary text never takes it. *Found on the way and fixed (I9):* the parallel unit run turned a Danish model case red with `ArgumentException: collation keys built by different collators are not comparable` — `WeightedKeyOrder.CompareNative` (and its sibling `IndexedConnector.KeyCompare`) built the two keys of one comparison by two resolutions of the locale sequence, and a test that clears the process-wide locale cache (`LocaleManager.ClearCache`, the "process-globals" collection) between them handed it keys of two collators. `CobolCollation.CompareByKeys` builds both under one resolution; both sites take it. *Cases* (`LocalePartialStartTests`, 30): the prefix shapes (controls, a soft hyphen, expansions, Czech "ch", Danish "aa", trailing pad; identical strength and reordered marks in NFD); the order's bound (margin, space cut, monotone compare); the GR17 model against the engine for every relation and every k over root, Czech and Danish key sets of variants, controls, spaces and tabs and contractions cut by the truncation, Hungarian doubled digraphs (three characters, two primaries: the overhang's case), interior spaces under a TAB probe (the cut's case) and the composite order; the pages (ordinary text reads at most 2d + 2 + the leaves its run crosses, a probe above every key exactly d, a run of variants its variants ahead); the marks on the root and zeros elsewhere, across CLOSE and OPEN, a DELETE and the emptying; a statement that raises them killed before, after or refused at each of its writes, reopened (the previous tree with its marks or the new one with the raised marks, and GR17's answers over it); a refused read at every read of a search ('30', then GR17's answer); another run unit's statement adopted between two of this one's. `BTreeModelTests.AuditShape` now checks the mark invariant in every model run. Seven mutations of the mechanism (no deficit, no overhang, no space cut, the reverse seat, the EQUAL exit, no stamp on the root, no demotion) each turned a case red. DESIGN-runtime-library.md 2.2.1.4 states the design (the PAGE layout's bytes 24..27, the marks, the bound, the walk, the cases) with no open defect; kb/Work PB3332 records the round and says that `prefix` means shorter than the key part.

**Branch review, wave 1123 B, round 1 (review-w1123-b.jsonl R1-1).** The reviewer found that the four defects the round-3 report listed as note-ready were neither fixed nor filed. Each was re-probed. *(1) and the live sibling it led to: one collator per statement and per open file.* `SortKeyColumns.Set` builds a run head's key when the head advances, through `_seq[k].KeyOf`, and the statement's snapshot (§14.6.6 r5) re-resolved the locale at every call, so a `LocaleManager.ClearCache` between two RETURNs of a merge handed the heads keys of two collators and the merge threw `ArgumentException: collation keys built by different collators are not comparable` (the new case `LocaleStateTests.ASortMergingRuns_KeepsOneCollator_AcrossACacheClearBetweenReturns`, red with the pin removed). `LocaleCollation.Snapshot` now returns a sequence that PINS its collator at its first use (so EC-LOCALE-MISSING still fires at the point of use) and answers it from then on, for the named form too (`Snapshot_PinsOneCollator_SoKeysBuiltAcrossACacheClearCompare`). The sweep of every holder of a sequence across uses found the live defect behind (3): the whole-store `IndexedConnector` compared under the DECLARED carrier, and `ALPHABET … IS LOCALE` without a locale-name is `LocaleCollation.Current`, resolved at each use, so a `SET LOCALE LC_COLLATE` while an indexed file was open reordered it (probe: seven keys written under the root walked na, ña, nb, ñb, nz, oa, ob before the switch and na, nb, nz, ña, ñb, oa, ob after it), against §12.4.5.7.4 GR1, *"Each collating sequence is a fixed file attribute"*, and against DESIGN-locale-facility L8's own text. `FileConnector.Open` now calls `FreezeOpenSequences` before §14.9.27.4 GR10's comparison, and the indexed connector freezes each key's snapshot there, so the comparison, the attributes a creating OPEN records and every key comparison of the open file use one sequence. Golden `cases/12.4.5.7-COLLATING-SEQUENCE/pb3331_locale_key_sequence_fixed_at_open`: after the switch every present key reads '00' (NOT INVALID KEY taken), an absent one '23' (INVALID KEY taken), the walk is the root's and its end '10' (AT END taken); a re-OPEN while es-ES is current is '39' and execution continues (FILE STATUS, no declarative); under the user default again the OPEN is '00' and the walk the root's. Red before the freeze (the N line walked the Spanish order). The table SORT and the relation conditions compare once per use, as §12.3.7.4 GR7 e) says, and hold no keys. *(3) for the engine:* kb/Work PB3334 now requires that each tree's `WeightedKeyOrder` take the frozen sequence. *(4):* kb/Work PB3333 now requires `StatementBound.Of(trees)` for a changing statement's reservation (the held attempt on worktree-wf_14a1e62b-326-13 summed `PagesToWrite`). *(2), measured, no exposure:* `CobolCollationTests`, `CldrLocaleLoaderTests`, `CldrIcuCrossCheckTests` and `GraphemeBreakerTests` build every key they compare from one collator instance, and the members of "process-globals" that change the environment install site files only for private-use tags (qaa–qad, zz-QQ, xx-SV) and probe private variable names, so a parallel clear changes only which instance a later resolution returns; the one consumer that held keys across resolutions was `SortKeyColumns`, now pinned. Docs: CONFORMANCE.md A.3 items 41–42 and DESIGN-locale-facility L8 state the frozen order and the '39'.

The branch's gate (wave 1123 B, after review round 1; it replaces round 3's run `20261018T201145Z-34449e`, which was itself a re-gate of `20261018T194711Z-749d26`, RED on `RunUnitStateDriftTests.NoStaticMutableCollection_OutsideTheDocumentedProcessStores` until the per-collator memo `LocaleCollation.s_pads` joined the register of process-lifetime stores, 6cac35968): run `20261018T211009Z-7804e1` ran the golden `pb3331_locale_key_sequence_fixed_at_open` (2002) and the two new `LocaleStateTests` cases in leg 1, all passed: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN — leg 2 left to CI's whole-population run: Conformance 6,485 · Unit 438 · Characterization 31 … — Conformance 11,328/17,813 · Unit 33,847/34,285 · Characterization 5/36 cases ran (skipped 0) in 1 of 2 leg(s) …`. The Linux gate is the landing's (batched gating).
