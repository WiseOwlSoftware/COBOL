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
theirs; that changes the shipped surface, so the runtime major went 7 to 8 and the ABI pin was re-pinned. GR6's MERGE
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
