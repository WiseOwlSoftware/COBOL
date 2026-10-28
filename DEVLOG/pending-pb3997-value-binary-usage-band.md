## PB3997: a VALUE literal past a fixed-width binary usage's band is refused (COBOL1625)

**Defect.** `BINARY-CHAR VALUE 255`, `BINARY-CHAR UNSIGNED VALUE 256`, `BINARY-CHAR VALUE -129`, `BINARY-SHORT VALUE
65535` and `BINARY-LONG VALUE 4294967295` compiled with no diagnostic, and the stored value wrapped (`BINARY-CHAR VALUE
255` displayed `+255`, then `-001` after `ADD 0`). ISO §13.18.63.3 SR2 requires "permissible values within the range
indicated by the PICTURE clause or the USAGE clause"; `DataBinder.ValidateNumericValue` tested only the picture-digit
span, and a `BinaryCapacity` item's Digits is 3/5/10/19, so 255 "fit" BINARY-CHAR. Owner, 2026-10-28: letting illegal
source through is a fatal defect.

**Re-probe.** The note's six-entry program reproduced on this build (`+255 +200 +65535 +4294967295 256 -129`, then
`-001`). After the fix each of the six entries is COBOL1625 ("... not a permissible value in the range the USAGE clause
indicates -- -128 through 127 (ISO §13.18.63.3 SR2)").

**Fix shape.** The band is written once, in `AlgebraicRanges.Of` (the byte width in `PicInfo.StorageWidth` under the
`BinaryCapacity` discipline), and was already read by the runtime store bound and the HIGHEST/LOWEST-ALGEBRAIC fold.
The new `AlgebraicRanges.Admits(range, magnitude, negative)` compares a literal's magnitude to that band through
`CompareMagnitude` (exact, BigInteger-scaled), and `ValidateNumericValue` asks it after the picture-digit span for a
`BinaryCapacity` subject. `ValidateNumericValue` is the single funnel of `ScreenValueLiteral`, so the item VALUE, the
level-88 value, both ends of a THRU range, the WHEN SET TO FALSE value, the format-2 table occurrences and the report
entry all gain the check by construction; probes of each refused, and the band edges (-128, 127, 255, BINARY-DOUBLE's
64-bit edges) compile. Float usages already refused out-of-range VALUEs (COBOL1661).

**Goldens.** `pb3997_value_binary_usage_band_edges_2002` (accepted 2002 and later; every band edge),
`pb3997-value-binary-usage-above-band` and `pb3997-value-binary-usage-condition-above-band` (refused at 2002, 2014,
2023, `diagnostics.json`). The usages are 2002 constructs, so there is no lower-edition refusal for this rule.
`docs/CONFORMANCE.md` DOC-A.1-206 now states the VALUE refusal. No inventory row was claimed or closed.

**Left open (lead).** `PIC 9(4) COMP-5 VALUE 65535` is still refused by the picture-digit span although the item
owns its 16-bit container (DOC-A.1-208); whether SR2's "PICTURE clause or the USAGE clause" admits it is a separate
adjudication.
