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