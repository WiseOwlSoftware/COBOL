## PB5217: a record area whose widest record holds an OCCURS DEPENDING table is read whole, so a shorter record and a key past the table keep their characters

**The defect.** In a file whose record descriptions include one with an OCCURS DEPENDING ON table, `WRITE R3` of a
shorter `PIC X(7)` record with the DEPENDING item at 1 wrote `ABCDE` and spaces (re-probed at the base 725550baca at
`--std 85` and `--std 2023`, sequential and indexed). The record area is the widest record's place, and for an ODO record
that place's sending image is cut to the DEPENDING item's current extent, which describes that record alone
(§2023:13.18.43.4 GR13c), not the area every level-1 entry redefines (§2023:13.18.33.4 GR3).

**The siblings, same root.** The sweep of every reader of `PlaceTable.RecordArea` found three more: the ODO record
itself written at one element in a fixed-length file sent 5 characters and spaces where D-WRT1 and DOC-A.1-147 send the
10-byte record with the area's tail; the keyed READ, DELETE and START read their key out of the cut image, so a RECORD
KEY of a shorter record lying past the extent was lost (a distinct second key answered `22`, an existing record `23`);
and 1985 USE FOR DEBUGGING rule 23 c ("For READ, DEBUG-CONTENTS contains the entire record read", §1985:XV.3.2.4 GR23c)
gave the cut record.

**The fix.** `OperandText.RecordSendImage` takes the `AreaUse` vocabulary `RecordAreaImage` already had;
`SequentialIoEmitter.RecordAreaSendImage` is the one whole-area image, used by `SentRecordImage` (WRITE, REWRITE and
RELEASE in every organization) and by the keyed READ, DELETE and START; the debug contents read the area whole.
`FileModel.TransfersPastRecord` also holds for an OCCURS DEPENDING record-name-1, whose current size can be below the
area's. The whole reading never unwraps an out-of-line variable-length group (its image is its current composition; the first gate's red, pb2497_vlg_record_odo_element_tables at 2014, threw there). docs/CONFORMANCE.md D-WRT1 states the whole-area reading; StartTemporaryKeyAreaDriftTests names the one channel.

**Goldens.** `14.9.51-WRITE/pb5217_write_beside_odo_record` (85 on: sequential, relative, indexed and RECORD VARYING
DEPENDING files, with the indexed abnormal paths: a duplicate key answers 22 and leaves the stored record, an absent key
answers 23 on START, DELETE and REWRITE), `14.9.32-RELEASE/pb5217_release_beside_odo_record` (the RELEASE arm: a SORT on a
key past the ODO record's extent returns KEY0 to KEY3 in order; the base cut the keys) and
`XV.3.2@1985-USE-FOR-DEBUGGING-STATEMENT/pb5217_debug_contents_odo_area`; expected values written from the rules before
the run. The RELEASE golden and the abnormal paths answer the branch review's R1-1 and R1-2. There is no edition below 1985 to refuse. No inventory row changes verdict.

**Self-review.** Looked for a second copy of the area image (none: one helper), sibling arms that still cut (the APPLY
COMMIT save renders an item, not an area, and its ODO object must be committed with it, SR8), and LINE SEQUENTIAL (unchanged:
`TransfersPastRecord` is false there).

**Gate.** `build-local` GREEN at 0f9280226a (run 000003-20261114T021230Z; Conformance and Characterization from run 000002). Run 000001 was red on the two reds above; run 000002 on an unrelated GrammarDiagramGeneratorDriftTests watcher buffer overflow.
