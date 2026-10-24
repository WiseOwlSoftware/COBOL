## PB1910: an overlapping MOVE converts in place the way libcob does (runtime OverlappingMove), and an ALL-literal VALUE under a REDEFINES base seeds its value

PB1910 asked whether the corners PB1907's D-OVL2 left out should be adopted: libcob also prepares PACKED-DECIMAL
receivers before it reads the sender, and re-punches a signed sender's sign. The adjudication's recipe was one
classifier arm: clear the packed receiver, then convert. The re-probe showed the recipe is wrong. The note's own
measurement (`AD 9(6)` = 123456, `AP 9(5) PACKED-DECIMAL` over its tail) is 23300 in GnuCOBOL 3.2.0; "clear, then
convert" gives 23000. `cob_move_display_to_packed` does clear the receiver, but it then writes it one digit PAIR at a
time while reading the sender from live storage, so the second pair is formed from the byte the first pair has just
written. The same is true of the numeric DISPLAY receiver D-OVL2(b) already adopted: "zero the receiver, then convert"
holds only where the receiver lies at or left of the sender's digits. A `PIC 9(4)` over bytes 4 to 7 of `123456` is
3333 in libcob (each digit written is the next read) and 0000 here.

What the re-probe was: a generator (scratch `w20261027h/gen.py`) of 9 senders x 20 receivers x 7 byte offsets, each case
a REDEFINES overlay dumped byte by byte, run through cobc 3.2.0 (ASCII and `-fsign=EBCDIC`) and through this compiler,
with a comparator that discards pairs whose non-overlapping control already differs in representation. Of 1,260
cases, the families that differ from the snapshot are the four in-place conversions (DISPLAY to DISPLAY, alphanumeric
to DISPLAY, DISPLAY to PACKED-DECIMAL, PACKED-DECIMAL to DISPLAY, whose digits are a snapshot but whose sign is read from
the sender's last byte after the store), numeric-edited receivers, PACKED-DECIMAL into PACKED-DECIMAL, and, under the
IBM convention only, a signed DISPLAY sender into a BINARY receiver over its sign byte.

The fix is the algorithm, not a corner. `OverlappingMove` (runtime) has one method per adopted family, each the
library's own order of reads and writes on both operands' storage images laid over each other, including its sign
handling (the sender's sign read first and written back last, so a receiver over the sign byte ends with the sign
punched onto whatever that byte holds) in both sign conventions. `MoveOverlap.Classify` chooses the family at bind time
(the type is now `OverlapStore`, renamed from `OverlapPrefill`; the `ZeroDigits` prefill is gone because the in-place
algorithm subsumes it), `MoveEmitter.EmitInPlaceStore` reads the two images, calls it and writes the receiver back, and
the JUSTIFIED RIGHT space prefill stays. After the change the matrix agrees with libcob for every numeric DISPLAY and
PACKED-DECIMAL receiver from a DISPLAY, alphanumeric or (DISPLAY receiver) PACKED-DECIMAL sender in both conventions,
and BINARY receivers in the ASCII one.

The note's item 2 (the re-punched sign makes a negative zero) is not an overlap effect: libcob keeps the sender's sign
on a zero result in every MOVE. D-OVL2 now says so; the in-place conversions reproduce the negative zero only because
they reproduce the sign handling.

Not adopted, each with its reason in D-OVL2 and the repro in the note: numeric-edited receivers (the editing routine
scans the picture while it reads and back-patches earlier positions), PACKED-DECIMAL into PACKED-DECIMAL (`memcpy` over
overlapping storage, which the C library decides), and the IBM-convention BINARY re-punch.

Found in passing and fixed (I9): `01 A. 05 F PIC X(6) VALUE ALL ".".` with a REDEFINES of A seeded the parse text
`ALL"."` instead of six periods. `GroupImageCodec`'s character-image seed was a second copy of `ValueInitializer`'s
recipe without its Format-6 arm; both now ask `ValueInitializer.AllLiteralInitializer`.

Goldens: `14.6.10-Overlapping-operands/pb1910_overlap_in_place_move` (identical on cobc 3.2.0 and here) and
`8.3.3.6-Figurative-constant-values/pb1910_value_all_literal_redefined_record`; `OverlappingMoveTests` (nine cases,
every expected image measured), `MoveOverlapDriftTests` extended. Row GR-14.6.10-1 restated (verdict batch
`w20261027h-pb1910`; no row closed). Self-review: the emitter's native-receiver arm (an item moved into a reference
modification of itself) takes its carrier from the stored image through `NumericRenderer.CarrierOfImage`, the one
crossing decode, whose parse is the tolerant one, since a smear can copy a non-digit.

**Review round 1 (P-scaled operands).** The reviewer found the in-place DISPLAY-to-PACKED and PACKED-to-DISPLAY ports counted a P position as a digit (`digits += scale` for a trailing P): `PIC 9(6)` into `9(3)PP PACKED-DECIMAL` over its tail gave `0x00 0x6F` where libcob gives `0x23 0x3F`, and `PP9(3) PACKED-DECIMAL` crashed with an IndexOutOfRangeException in a program that ran on main. The library's offsets count the STORED digits (`Digits`), the P positions live in the scale alone, so the adjustment is deleted. libcob itself leaves its operands when the sender's and receiver's digit windows share no decimal position (it reads the bytes past the sender or writes past the receiver); `MoveOverlap.SharesADigitPosition` keeps the snapshot there. A 320-case P-scaled matrix (8 senders x 8 receivers x 5 offsets, measured with cobc 3.2.0) now differs from the library only in the documented packed-into-packed family and the disjoint case. Goldens `PP-TAIL`, `PP-SEND`, `PP-PACK`, `PP-OUT`, `PP-LOW` in `pb1910_overlap_in_place_move`; three `OverlappingMoveTests` cases. The lead (comments said three in-place kinds, `MoveStore.Overlap` described only the prefill) is fixed in the files changed anyway.

**Finisher (the landing's Linux unit leg).** The landing stopped on `SortNumericKeyLaneTests.OnlyTheNamedSites_DecodeANumericImage`: the in-place store's native-receiver arm called `NumParseImage` itself, a decode lane outside the named CodeGen sites (kb/Work PB186). It now asks `NumericRenderer.CarrierOfImage`, the one decode of a native carrier from its item's storage image (the same tolerant `ParseImage` under the receiver's carrier type, so the program's behavior does not change). The Windows gate never ran the test: a source-scanning rule kept in a class not named `*DriftTests` is in no drift-rule index and no leg-1 tier, and kb/Work PB2099 met the same test late. The fact moved to `NumericImageDecodeSiteDriftTests`, which the index now lists for every CodeGen file (tier 0d).
