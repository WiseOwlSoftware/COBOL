## PB3332: the keyed store's B+ tree over the page layer, with the LEDGER and the FREE set on the same engine

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
