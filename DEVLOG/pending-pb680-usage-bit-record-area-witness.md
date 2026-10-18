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
