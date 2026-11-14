- **D-INS2 — a FORMAT 3 INSPECT whose counter is in the storage of identifier-1 or of a REPLACING operand (§2023:14.9.22.4
  GR13 with GR19).** `cite.py --check 14.9.22.4 "A format 3 INSPECT statement is interpreted and executed as though two
  successive INSPECT statements specifying the same identifier-1 had been written"` → OK 19). **Determination:** the
  statement runs as GR19 words it: item identification once, then the whole tallying statement (counters stored), then
  the whole replacing statement over identifier-1 and operand values as they stand when it starts. A counter inside
  identifier-1 therefore keeps its tallied value (`INSPECT G5 TALLYING G5T FOR ALL "A" REPLACING ALL "A" BY "B"` over
  `0AAA` gives `3BBB`; before this entry the replacing half wrote back the pre-tally image and the counter was lost,
  `0BBB`), and a replacing operand that is the counter reads the counter's new value. The re-read is the same
  `StorageOverlap` test as D-INS1, applied between the two halves. GnuCOBOL 3.2 emits the two statements separately
  and gives the same lines. Pinned by `conformance:cases/14.9.22-INSPECT/pb1907_inspect_format3_counter_overlap`.
  **The length of an occurs-depending identifier-1 (kb/Work PB5119, VCR row 7.57)** is part of that identification
  (§2023:14.6.4 step 6; the 2002 text words it for a variable-length data item), so from 2002 a TALLYING counter that is
  the DEPENDING ON item does not change the length the REPLACING half works over (`ZZZBA`, pinned by
  `conformance:cases/14.9.22-INSPECT/pb5119_format3_odo_length`). At `--std 85` GR17 fixes only the subscripting, so the
  second of the two successive statements takes the length at its own start (`ZZZZZ`, `pb5119_format3_odo_length_1985`).
  The same holds for EVERY form of identifier-1 whose identification reads the object: a reference-modified group with
  or without a length (an omitted length runs to the end of the group as identified, §2023:14.6.4 step 6 before step 7),
  the record above the group, a group of the table, and an element whose reference tests the §2023:13.18.38.4 GR7 bound
  (identified once, so a counter that takes the object past integer-2 raises no EC-BOUND-ODO at the write-back);
  pinned by `pb5119_format3_odo_forms` and `pb5119_format3_odo_forms_1985`. STRING, UNSTRING, MOVE and SEARCH store into
  an operand's object correctly at every edition (`14.6.4-Item-identification/pb5119_odo_object_changed_by_statement`).
  A function-identifier operand of any phrase is identified once, before the first counter is stored (§2023:14.9.22.4 GR6,
  kb/Work PB5184, `pb5184_replacing_function_once`).
