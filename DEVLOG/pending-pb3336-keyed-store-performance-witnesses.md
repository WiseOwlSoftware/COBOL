## PB3336: the keyed store engine's performance witnesses, counted in pages, and the bounds the counts corrected

kb/Work PB3336 is group 6 of the keyed store engine (PB3330): the witnesses (a) to (c) of
docs/rearchitecture/DESIGN-runtime-library.md 2.2.1.11, on the engine PB3331-PB3335 built. It merged group A5's branch
(PB3335) first; PB3331-PB3335's notes land through this branch.

**Re-probe.** On the merged build the witnesses did not exist, and neither did the seam they need: `PagedFile` counts its
page I/O (`Counters`) and drops its cache (`DropCache`), but no test could reach a connector's store. The seam is
`KeyedConnector.PagedStore` and `FileRegistry.PagedStoreOf` (internal, read-only).

**The cases.** `tests/Cobol.Net.Tests.Unit/Store/KeyedStorePerformanceWitnessTests.cs` is one abstract class with one derived
class per file shape (RELATIVE; INDEXED with no alternate key; INDEXED with one alternate key WITH DUPLICATES and one unique),
17 cases each, cells ascending and random keys × NO OTHER and ALL OTHER. They drive the real connectors through a
`FileRegistry`, with the pages in `PageArrayMedium` (new: an in-memory page array per physical file that keeps the latest
version of each page, because `MemoryPageMedium` keeps every version, which the crash cases need and a 20 000-record load
cannot afford). The host file still carries the handles and the locks. The 51 cases take 1.6 minutes in parallel.
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
  - a WRITE the medium refuses, '30' (§9.1.13.6 1)), then the permanent error's replay (§9.1.13.1). The replay reads only
    page 2, and after the CLOSE the next run unit's WRITE is within its row and the refused record is absent;
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
- A new row covers the unsuccessful statements.
- The stale `PagedStore` names in 2.2.1.11, 2.2.1.12 and the coherence tables now say `PagedFile` or `KeyedStore`.

**Self-review.**
- Architecture: one seam from a connector to its paged store, and no second fault seam (`MediumObserver` hands
  `PageArrayMedium` in).
- Full code: every citation passed `cite.py --check`, and each expected value comes from a 2.2.1.11 row or a general rule.
- Performance: the seam is a property read.
- Duplication: the B+ tree page walk is the test's own, sharing `PageHeader`'s and `PagePointer`'s codecs.

Lead: the ledger could be organized so that a statement's entries land on one leaf. It is in the wave-1118 A6 report.

**Gate:** see the report (`w1118a6-PB3336-report.md`).
