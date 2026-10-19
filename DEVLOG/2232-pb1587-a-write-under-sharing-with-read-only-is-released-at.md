## Entry 2232 — 2026-10-18 17:58 PDT — PB1587: a WRITE under SHARING WITH READ ONLY is released at the statement, in every organization

kb/Work PB1587 (wave 1126, group C of the PB3353 campaign) asked what §9.1.15 requires for the visibility of a record
written through a connector opened `SHARING WITH READ ONLY` to a second connector's READ before the writer's CLOSE, and
offered two exits: a documented determination or a defect. The answer is a defect, and the doc-rows-2 / PB643 determination
it grew from is retired.

**What the re-probe showed.** On the wave's build, one program per organization (a READ ONLY writer, OUTPUT or EXTEND, one
WRITE, then an `OPEN INPUT` ALL OTHER reader's READ): SEQUENTIAL and LINE SEQUENTIAL answered WRITE `00` and READ `10`;
RELATIVE answered `00`; INDEXED answered `00` for a reader in the same run unit (one in-memory store) and `10` for a reader
in another (the whole store was persisted at CLOSE). Three organizations, three answers to one program. GnuCOBOL 3.2 under
WSL: SEQUENTIAL and RELATIVE `00`, LINE SEQUENTIAL and INDEXED `10` - its own stdio and Berkeley DB buffers, which is no
authority where ISO controls.

**The rule.** §14.9.51.4 GR12, *"The successful execution of a WRITE statement releases a logical record to the operating
environment"*, puts the record in the physical file at the statement; §9.1.15 2), *"The sharing with read only mode restricts
concurrent access to a physical file through file connectors other than this one, to input mode"*, admits the INPUT connector
beside the writer; §14.9.30.4 GR21 makes its READ select from the physical file. The standard prints no buffering rule that
could keep a record in a run-unit area. PB643 had read the 4,096-byte input-output area as the place a READ ONLY writer's
records wait and had made RESERVE's count observable that way (64, 64 and 0 of 100 records); that is retired.

**The fix.** One predicate, in both arms. `SequentialConnector.ReleaseRecord` flushes and announces the release to sibling
readers whenever `FileLockPosture.AdmitsAnotherRunUnit(HostShare)` - every sharing mode but NO OTHER - where it asked
`AdmitsAnotherWriter`; the ordinal count off the medium and the repositioning append stream stay on `AdmitsAnotherWriter`,
because they are about a second WRITER. `IndexedConnector.CoherentUnder` (the whole-store engine, which PB3334 and PB3335
retire) asks the same predicate, so a READ ONLY writer's statements persist under the store mutex at the statement. A
`SHARING WITH NO OTHER` writer keeps its buffered load: nothing can read the file. The cost is one flush per WRITE for a READ
ONLY writer. DESIGN-runtime-library.md had argued the opposite in so many words ("one flush per record for nothing") without
asking who the reader of a READ ONLY writer is; it and DOC-A.1-164 are rewritten, and PB3334 carries the cross-run-unit
INDEXED test the engine must keep green.

**Goldens and tests.** `pb1587_write_released_to_input_reader` (2002, record sequential: the empty file's AT END, the record
released, the READ after AT END as `46` caught by a USE procedure, the read-ahead reader, EXTEND, and the `61` refusals of a
READ ONLY reader and of any reader beside NO OTHER), `pb1587_write_released_line_sequential` (2023) and
`pb1587_write_released_keyed_organizations` (relative and indexed; the INVALID KEY `21` with and without the phrase releases
nothing). `pb643_reserve_areas_visibility` is re-derived and now reads 100 in every phase. `ReadOnlyWriterReleaseDriftTests`
(Unit) covers every framing x OUTPUT/EXTEND x READ ONLY/ALL OTHER, the read-ahead reader, the empty file, `61`, the two keyed
organizations across two run units (`22` releases nothing) and, on Linux, `/dev/full` refusing the flush at the FIRST write
(`34`). With the fix reverted the READ ONLY arms of the sequential tests are red (16 of 29), and the INDEXED cross-run-unit
test was red before its own fix.

**Register.** Rows DOC-A.1-164, GR-12.4.5.14.3-1, GR-14.9.51.4-12 and GR-9.1.15-2 were CONFORMS and stay CONFORMS (batch
`w1126c-PB1587` restated their notes and added the witnesses; `closes_rows: []` with its reason). PB1587 is landed.

**Self-review.** Architecture: one predicate per engine question, none duplicated; the RELATIVE engine and the two streaming
arms now agree. Code: the flush is inside `Write`'s `IOException` catch, so a refused medium is `34`/`30`, never an escaping
exception; REWRITE already flushed unconditionally. Performance: a syscall per WRITE for READ ONLY writers only. Comments:
the ReleaseRecord, OpenWriter, OpenConnectorWriteStream, KeyedConnector and WholeStore remarks that said "another writer"
now say who is meant.

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (run `20261018T220148Z-07045e`, after the last test-only commit; the first, `20261018T213620Z-551610`, was GREEN too). The Linux unit leg also ran GREEN (34,368 unit tests, `/dev/full` included).

