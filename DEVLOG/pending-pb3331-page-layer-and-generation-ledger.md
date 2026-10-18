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
