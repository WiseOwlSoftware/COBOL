## PB3212 branch review R1 findings answered (wave 20262139 group A)

**R1-1 (test coverage).** Four tests now pin the external-identity change's refusals and its exception path:
`conformance-suite/cases/12.4.5-File-control/pb3212-nonexternal-fd-relative-key` (a RELATIVE KEY in the record of a file
with no EXTERNAL clause, refused COBOL1624 at 85/2002/2014/2023), `…/pb3212-external-fd-key-positions-differ` (two programs
describe the external record's RK at positions 1-4 and 7-10, refused COBOL1575), `cases/13.18.22-EXTERNAL/pb3212-external-fd-key-invalid-key`
(READ of an unwritten relative record number through the FD-record key takes INVALID KEY with status 23, §2023:14.9.30.4
GR29, §2023:9.1.13.5 GR3; record 2 still reads), and `cases/VII.2.7@1985-PADDING-CHARACTER-CLAUSE/pb3212-external-fd-item-padding-refused`.
The last answers (d): the review supposed the identity change admits a PADDING CHARACTER item of an external FD record.
It does not: §1985:VII.2.7.3 SR3 bars data-name-1 from the File Section whatever its external attribute, and the
compiler refuses it COBOL6848 at 85 and 2002 (the clause is gone from 2014).

**R1-3 (duplication).** The five `file.Records.Contains(record)` scans and the sixth in `FileModel.TransfersPastRecord` read
`FileModel.HoldsRecord`, which asks the record's own `RecordOfFile`; the ASSIGN USING own-record walk in
`FileControlBinder.Resolve` is `RecordLayout.IsInRecordOfFile`. No reader scans a file's record list for membership.

**R1-2 (duplication).** `DataItem.IsInFileSectionRecord` is `Section is EntrySection.File`, the section fact the I-O-CONTROL
placement rules read, so a record of a repeated file description entry (bound in the file section, joined to no file)
answers the same to both.
