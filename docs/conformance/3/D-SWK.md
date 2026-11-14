- **D-SWK — the sort-merge file's storage: a memory limit and temporary run files (§2023:13.4.6.1; §2023:14.9.40.4 GR3, GR4,
  GR9; §2023:14.9.24.4 GR4, GR6)**: the standard says what a sort-merge file DOES, never where its records are kept — the SD
  entry "furnishes information concerning the physical structure pertaining to a sort or merge file" and nothing
  more — so the storage is the implementor's, and WiseOwl COBOL follows GnuCOBOL 3.2 (CLAUDE.md rule 1: bounded sort
  memory, sorted runs on temporary files, merged) (kb/Work PB3339). **(a) The memory limit** is `WISEOWL_COBOL_SORT_MEMORY`:
  a byte count with an optional `K`, `M` or `G` suffix (powers of 1024, case-insensitive); unset or not a size, it is
  **128 MiB** (GnuCOBOL's `COB_SORT_MEMORY` default); below **64 KiB** it is 64 KiB. It is read once per run unit. A
  SORT or MERGE keeps at most that many bytes of records in memory (the estimate counts each record's UTF-16 image,
  its frame and extent table, and its decoded keys), or one record when a single record is larger; what exceeds it is
  written as sorted RUNS and merged, at most F at a time (F = the limit ÷ 256 KiB, between 2 and 16), and the return
  phase reads F runs through 64 KiB buffers. A SORT or MERGE whose records fit is done wholly in memory and writes
  nothing. **(b) The temporary files** are created in `WISEOWL_COBOL_SORT_TMPDIR`, or in the host's temporary directory
  when it is unset (`TMPDIR` on Linux and macOS, `TMP` / `TEMP` on Windows). Each is deleted when its handle closes
  (Windows) or unlinked as soon as it is created (Linux, macOS), so none survives the statement, the run unit or a
  killed process, and the statement's end closes every one, whether it ends normally or by a failure part-way
  through writing or merging a run (one owner, `SortWork._files`, holds each file from its creation to its close).
  **(c) The ordering does not depend on the limit**: a run
  is a stable sort, and every merge takes the lowest key and, on equal keys, the record of the earliest run, so
  equal keys are returned in input-file and release order (§2023:14.9.40.4 GR3 a), b) DUPLICATES IN ORDER; without the
  phrase the order GR4 leaves undefined is the same one) and a MERGE keeps §2023:14.9.24.4 GR4 a), b). With
  EC-SORT-MERGE-SEQUENCE unchecked an out-of-order MERGE input is merged in stream order when it fits in memory, and in
  groups of F streams when it does not — both within GR6's "undefined". **(d) A failure of a temporary file** — the
  directory missing or not writable, the volume full — stops the run unit with a message naming the directory and the
  host's reason (an implementor-defined fatal stop that raises no exception condition, like `CobolImplementorFatalException`'s
  other uses); no record is dropped silently. **(e) Two undefined results** this storage settles: a RETURN executed
  before the sequence phase, which only unchecked EC-FLOW-RETURN admits (§2023:14.9.34.4 GR1), finds the file at end; a
  RELEASE executed after it, which only unchecked EC-FLOW-RELEASE admits (§2023:14.9.32.4 GR1), releases to no phase and
  the record is not returned. **(f) A record holding managed slots** (kb/Work PB4772) — a strongly-typed SD record with a
  message-tag, object-reference or pointer leaf, which has no byte form (D-SLOT, A.1 item 210) — is stored as its one-way
  transfer image, the positions the keys are windows over, and the slot values themselves stay RESIDENT for the
  statement's life, one small array per such record (`SortWork._carried`), because a managed reference cannot be written
  to a run file; the record's frame, which can spill, holds only the index of its array, and RETURN gives the array back
  with the record (§2023:14.9.32.4 GR2, §2023:14.9.34.4 GR3, GR5 b)). The memory limit of (a) therefore bounds the records' characters
  and not these arrays. The INPUT/OUTPUT PROCEDURE form, a USING or GIVING file and a MERGE are all admitted (kb/Work PB4838): a file record
  carries each pointer or message-tag leaf as its 8-position token (A.1 items 212/216), so USING decodes the record into
  the SD record and releases the slot vector beside its image (`SortEmitter.EmitInputFile`, `CobolSort.Release`'s `slots`), and
  GIVING writes the stored image, which is that token. Witnesses: `conformance-test:SortWorkSpillConformanceTests`
  (a SORT and a MERGE many times the limit, run as a separate process, the work directory empty afterwards),
  `SortWorkBoundTests` (the measured bound, every key class, the extent table and the character repertoire through a run
  file) and `SortManagedSlotTests` (a slot vector follows its record through disk runs, with an extent table, past a refused
  RELEASE and a failing merge), with `conformance:cases/14.9.40-SORT/sort-sd-record-with-managed-slots`.
