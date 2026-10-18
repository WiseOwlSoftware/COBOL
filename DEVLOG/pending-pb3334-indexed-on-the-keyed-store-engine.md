## PB3334: INDEXED on the keyed store engine, and the keyed lifecycle written once

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
