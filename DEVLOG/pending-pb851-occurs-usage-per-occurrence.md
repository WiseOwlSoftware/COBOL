## PB851: USAGE per occurrence of an OCCURS entry is witnessed on all three table bands

kb/Work PB851 claimed `GR-13.18.38.4-1` (every data description clause other than OCCURS applies to each occurrence)
as an open GAP with no witness. Re-probed on a fresh build, the row was no longer a GAP: the golden lane had already
closed it CONFORMS with `l1c20_occurs_clauses_per_occurrence` (fixed and ODO tables) and
`l1c20_occurs_dynamic_clauses_per_occurrence` (OCCURS DYNAMIC), which pin PICTURE editing, JUSTIFIED, BLANK WHEN ZERO
and SIGN SEPARATE on occurrences 2 and 3, and SYNCHRONIZED had `l1_sync_occurs_and_no_auto_align`. The clause the note
named that nothing pinned was USAGE, so no golden said a later occurrence has the clause's storage form.

Two spec-derived goldens close it. `l1c20_occurs_usage_per_occurrence` (formats 1 and 2, accepted at every edition from 85:
BINARY and PACKED-DECIMAL carry no later introduction edge in constructs.json or the version change reference) moves each table group into `X PIC X(9)` and reads `X(7:3)`: a three-occurrence
PACKED-DECIMAL S9(3) or BINARY S9(4) group is 6 bytes (DOC-A.1-215, DOC-A.1-205) so the tail is spaces, a DISPLAY 9(3)
control is 9 bytes and answers `003`, and an ODO group moves only its current extent (§13.18.38.4 GR8 a). Arithmetic on
occurrence 3 reading occurrence 1 shows the values (-14, 299, -24). `l1c20_occurs_dynamic_usage_per_occurrence` (format 4)
asks BYTE-LENGTH of occurrences 1-3 (2, 2, 3) and repeats the arithmetic. Both ran correctly on the first probe: no
defect, the replication holds because every table is one repeated element type. No negative was added: the gating of
OCCURS DYNAMIC below 2014 is already pinned, and the USAGE spellings are legal at every edition, so there is no new refusal.

Rows: `GR-13.18.38.4-1` stays CONFORMS; two test-refs added and its stale "PB851 owns the missing witness" sentence
restated through `record_verdicts`. PB851 flips to landed with `closes_rows: [GR-13.18.38.4-1]`. The sibling sweep of
the REDEFINES (§13.18.44) and TYPE (§13.18.57) rows found them all verdicted. Self-review: the comment blocks quote only
`cite.py --check`ed text; the §8.5.1.9.2 quote was narrowed to the sending-operand sentence it really carries.

Branch review round 1 (R1-1): the formats 1 and 2 golden first dropped `--std 85` on a false edition fact (BINARY and
PACKED-DECIMAL are not 2002 introductions here); it now lists 85, and the 85 run prints the same eight lines as the others.
