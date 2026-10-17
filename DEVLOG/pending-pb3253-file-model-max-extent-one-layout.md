## PB3253: a variable-length record's GR8 b) maximum is the laid-out record plus its dynamic-length leaves

`FileModel.MaxDynamicExtent` summed each non-redefining child's own `ByteWidth` for a record with a dynamic-length member,
so two same-level `PIC 1(3) USAGE BIT` items each counted a byte where the one 8.5.1.6.3 walk shares one. The record
`01 R. 05 B1 PIC 1(3) USAGE BIT. 05 B2 PIC 1(3) USAGE BIT. 05 D PIC X DYNAMIC LENGTH LIMIT 10.` measured `1 to 12 bytes`
(its minimum already shared the byte) and `RECORD CONTAINS 11` was refused COBOLNET2010 (ISO 13.18.43.3 SR3), while the same
record with `D PIC X(10)` compiled. The same wrong maximum fed `VaryMax`, `RecordMax`, SR4's upper arm and the fixed-form
REWRITE length.

Fix shape (PB1277's, applied to the maximum): the maximum is `ByteWidth` of the item, which already lays bits out and counts a
dynamic-length leaf as zero bytes, plus `DynamicLeafSum` over the dynamic-length leaves it holds, each leaf at
`DynamicLeafMaximum` (its `DynMaxSize` in bytes plus its dynamic length structure) times the occurrence counts between it and
the item. One walk now serves both ends: `MinimumStructureOverhead` is the same `DynamicLeafSum` at `MinimumOccurrences`.

Siblings found in the two members and fixed, not filed. The minimum twin, `StructureOverheadBeneath`, did not multiply a
dynamic-length item's structure overhead by the occurrence count of a table around it (three DELIMITED items in a three-element
table had minimum 1, not 3), and `MinExtent`'s bit-bearing occurs-depending arm left the overhead out altogether (a bit item, a
DELIMITED item and a `1 TO 4` bit table measured 2, not 3). `StructureOverheadBeneath` is gone.

Goldens: `pb3253_record_contains_bits_beside_dynamic` (accept 2014; writes two records under `RECORD CONTAINS 11` and reads
them back, `101 011 D=[HELLO]` and `000 110 D=[]`) and `pb3253-record-contains-bits-beside-dynamic-one-over` (`RECORD CONTAINS 10`,
refused COBOLNET2010 at 2014 and 2023). `RecordSizeModelTests` gains four range cases and three structure cases, with values
derived from 13.18.43.4 GR4/GR8 and 12.3.7.4 GR18/GR19. No inventory row was claimed or closed (`closes_rows: []`).
