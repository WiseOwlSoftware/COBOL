## PB3331 — the keyed store engine's page layer and generation ledger, simulator first

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
