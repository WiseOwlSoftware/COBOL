## PB2656: one numeric image writer, and a zoned decode that reads the image in place

**Mechanism.** The R2 performance review (kb/Work PB2656, cluster PB1754) found MOVEHOT's hot loop paying for the
record-image codec it reaches three times a pass: `FormatImageSendersSign` (into `FormatImage`) for RA-AMT, `FormatImage`
for RA-CODE and `ParseImageSending` for RB-CODE. The re-probe on this branch's build showed it unchanged (MOVEHOT prints
`MOVEHOT 000000009999000000 [OVER   2,000,0]`, its computed witness; the emitted C# makes exactly those three calls). Under
them, the zoned writer formatted the magnitude with `ToString`, padded it with `PadLeft`, and then rebuilt the string with
two substrings and a concatenation to place an over-punch. Under the default `SignEncoding.Ibm` that is every signed
image, positive or not, because the IBM positive table is not the plain digits. The zoned decoder `ParseDisplayMagnitude`
copied the image with `ToCharArray` so it could blank the sign carrier, although `DigitMagnitude(ReadOnlySpan<char>)`
already scans a span.

**Fix.** One zoned writer, `FormatZoned` over `WriteDigits`, writes every DISPLAY image (over-punched, separate,
binary-minus), the STANDARD DATA FORMAT image SEND counts (`FormatStandardData`) and both unsigned formatters
(`FormatUnsignedDisplay`, `FormatUnsignedDisplayU`) into one `string.Create`. The over-punch is written in place
through the same `ZonedSign` tables (§2023:13.18.52.4 GR4 and GR5b, kb/Work PB803). `Overpunch` and `DecodeOverpunch` are
deleted. `ParseDisplayMagnitude` reads the image in place: a separate sign or binary minus is not a digit, so
`DigitMagnitude`'s scan passes over it. A leading punched digit seeds the scan (a private `DigitMagnitude(leading,
span)`), and a trailing one is the last digit of the scan over the rest. The sibling sweep covered the other arms of
`FormatImage`: `BinaryBytes` built a `char[]` and `FormatPackedImage` a `byte[]` and a `char[]` before their string; both
now write straight into it. Per call, a signed zoned image goes from up to five allocations to one, a zoned decode from
one to none, a packed image from three to one, and a binary image from two to one. Emitted C# is unchanged.

**Neutrality.** A differential of the old codec, transcribed from the base, against the new one ran once and is not
committed, because it is a second copy of the rule. It covered every digit count 0 to 38, every `NumericSign`, both
`SignEncoding`s, signed and unsigned: 156,000 values for format, `FormatStandardData` and the decode, plus random junk
images for `ParseDisplay` and `ImageIsNegative`. It also covered 136,800 packed and binary images, signed and unsigned
lanes. Every case agreed. The §4 oracle result is in the report.

**Guard.** `NumericImageAllocationTests` pins one string per image and zero bytes per decode, counted on the thread. It
is a structural guard, not a wall-clock limit. Its image vectors are computed from the pinned sign tables.

**Also fixed.** Three code citations of `§2023:14.9.25.4 GR6b` for the unsigned receiver's absolute value now cite
`§2023:14.9.25.4 GR6d.2.b`, the rule `cite.py --check` resolves the text to.

No inventory row (behavior-neutral restructuring). No goldens: no COBOL-visible behavior changed.
