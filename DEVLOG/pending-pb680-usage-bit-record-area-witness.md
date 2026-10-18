## PB680: a USAGE BIT file record area gets its witness (the fix had landed with PB327, unpinned)

**Re-probe.** PB680 recorded that an elementary `01 R PIC 1(8) USAGE BIT.` under an FD had written its `'0'/'1'`
value carrier (eight characters, truncated to the one-character record) until PB327 routed every record-area send
through `OperandText.RecordAreaImage`, and that no golden pinned the repair. On a fresh build the defect does not
reproduce: `B"01000001"` written through the bit connector reads back through a second connector on the same
physical file (`PIC X(1)`) as ORD 66, and the file is one byte long. The sibling arms were swept by probe and all
agree, because there is one send channel (`RecordAreaImage`) and one receive channel
(`SequentialIoEmitter.EmitRecordAreaStore`): 5-bit and 12-bit records (ceil(bits/8) bytes, trailing filler bits
zero), a bit leaf beside an alphanumeric one (also ending in a partial byte), LINE SEQUENTIAL, RELATIVE and INDEXED,
WRITE FROM / READ INTO, REWRITE, SORT and MERGE (USING/GIVING and RELEASE/RETURN). A boolean SORT key is refused
by COBOLNET1757 (14.9.40.3 SR6 c), so the sort legs key on an alphanumeric leaf beside the bit leaf.

**What landed.** Witnesses only, no compiler change. `13.18.60-USAGE/pb680_bit_record_area_file_io` writes each
shape through a bit-record connector and reads it back through a `PIC X` connector, so the expected numbers are the
ORDs of bytes computed by hand from the bits (high-order bit first, 8.5.1.6.3), and through the bit connector, so
the values round-trip; RELATIVE and INDEXED, whose physical layout is the implementor's, are observed by value.
LINE SEQUENTIAL is its own golden (`pb680_bit_record_area_line_sequential`, accepted at 2023 only): the first gate
caught the organization's edition gate (COBOLNET0900 below 2023) when it sat in the 2002-2023 program.
`pb680_bit_record_area_file_exceptions` is the abnormal half (the owner's 2026-10-17 requirement that file I/O tests
cover exception paths as fully as success): a 16-bit record read from a one-byte file is I-O status 04
(9.1.13.2 item 3), then 10, 47, 48, 42, 41, 46 and 35 each from its 9.1.13.x item, and a USE declarative runs for an
at-end condition without an AT END phrase but not with one (9.1.12, 9.1.13.1). The negative
`pb680-bit-record-area-below-2002` is refused at 85 with COBOLNET0900. `docs/CONFORMANCE.md`'s USAGE BIT
determination now states the file-record image.

**Register.** PB680 flips to `landed` with `closes_rows: []` and a reason: no inventory row was claimed, and the
rule row the goldens touch (GR-13.18.60.4-5) belongs to PB573 and stays PARTIAL for its other claims.

**Measured on the way, not changed.** The byte 0xFF in a `PIC X` item orders as 65536 whether it came from a file,
from `X"FF"` or from `HIGH-VALUE` (the runtime's HIGH-VALUES character is U+FFFF); the goldens use no byte above
0xF0, so none of their numbers depends on that convention.

**Branch review round 1 (three findings, all test-side).** The exceptions golden now also drives the invalid key
half: a RELATIVE file and an INDEXED file whose record area is a USAGE BIT item answer 22 (duplicate WRITE, 9.1.13.5
item 2), 23 (random READ, REWRITE and START of an absent key, 9.1.13.5 item 3), each with and without the INVALID
KEY phrase, so a USE declarative counter shows the phrase displacing the declarative (9.1.14 items 2 and 3); after an
unsuccessful WRITE or REWRITE the bit record area still shows the value moved to it (14.9.51.4 GR15, 14.9.35.4
GR14), the file keeps its original records, and READ NEXT past the last record is AT END, 10. The same compiler
answered every leg on the first run, so no source changed. The io golden no longer claims GR-13.18.33.4-3 (every FD
there has one level-1 record, so it never witnessed implicit redefinition), and the exceptions header now labels the
at-end READ X10, the READ after it X10B and the declarative count X13.

Round 2 of the review found that the INDEXED leg REWROTE and STARTed on key IK right after two unsuccessful random READs, when the record area (so IK) is undefined (14.9.30.4 GR18). The program now moves ZZ to IK again before the Z5 REWRITE, so Z5 and Z6 address a key the program set; expected output unchanged.
