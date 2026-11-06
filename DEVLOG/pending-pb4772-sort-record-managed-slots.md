## A SORT over an SD record holding a message tag, object reference or pointer runs, and the slots follow their records

**What was wrong (kb/Work PB4772).** A file SORT over an SD record described as a strongly-typed group with a USAGE
MESSAGE-TAG leaf (the only place a tag may sit inside a record, ISO 13.18.60.3 SR14), keyed on another item, compiled with
COBOL1756 and aborted when the statement ran. The re-probe on the build showed the reviewer's lead was narrower than the
defect: a POINTER leaf did exactly the same, because `SortBinder.SortRecordOf` admitted only records with a character image
and a slot-carried leaf has none that reads back.

**Fix shape.** A slot has no byte form, so the store carries such a record in two halves. The characters are the one-way
transfer image the keys already read (`OperandText.AsStorageImage(..., transfer: true)`); the slots are the record's leaf
vector (`PlaceRenderer.GroupLeaves`, the carrier an activation boundary already uses). `CobolSort.ReleaseStatement` takes the
vector (`slots`); `SortWork` keeps it resident, one small array per record (a managed reference cannot be written to a run
file), and the frame, which may spill, carries only an integer ticket (`StoredFrame.Carried`), which `SortRunFile` writes
behind the sign bit of the character count so a record with no slot costs nothing. RETURN stores the vector back with
`WithLeaves` (`CobolSort.LastReturnedSlots`), RETURN INTO and RELEASE FROM go through the existing leaf-vector MOVE. The
emitter decides by `SortEmitter.HoldsSlots` (a slot leaf), never by the image capability, so DESIGN-message-control S4's
later two-way tag image cannot turn this path into a silent loss. Admitted: one record description, no SAME RECORD AREA
peer, fixed size. Not admitted (COBOL1756, now with its own reason): a USING or GIVING file and every MERGE over such a
record, since a file record has no managed-slot half; that is the file record codec's gap, registered as a lead.

**Citations.** The comments I touched cited `ISO §14.9.3.4 GR9` for "pointer slots ride the managed slot"; `cite.py` shows
14.9.3.4 GR9 is ALLOCATE's null-seeding, so the three sort sites that carried it now cite D-SLOT / DOC-A.1-210. (Six other
files cite it for the null-seeding sentence, which it does say.)

**Tests.** Golden `14.9.40-SORT/sort-sd-record-with-managed-slots` (pointer and tag in one SD record; each returned record
keeps the pointer set when it was released, in the record area and in the INTO receiver; the tag stays NULL) with the
refusal `sort-sd-record-with-managed-slots-refused-2014` (COBOL0900, the tag's introduction gate). Unit
`SortManagedSlotTests`: 20,000 records through disk runs with every fourth slot-less, an extent table and a ticket in one
head, a RELEASE refused by EC-FLOW-RELEASE and by EC-SORT-MERGE-RELEASE keeping nothing, the next statement on the file
inheriting nothing, a failing cascade merge closing every run. docs/CONFORMANCE.md D-SWK (f) records the resident slots and
the USING/GIVING deferral.

**Gate.** (verdict line filled in at the gate)
