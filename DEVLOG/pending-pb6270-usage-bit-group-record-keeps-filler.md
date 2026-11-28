## A published record of a USAGE BIT group keeps its FILLER bits (PB6270)

**Defect.** The host entry's published record struct (`PublishedRecordEmitter`) keeps the positions no member covers
(FILLER, a REDEFINES view's tail) in a private `__image`, so `FromImage` to `AsImage` and a call's write-back round-trip
(DESIGN-external-repository section 4.3). The emitted `__TryGet` skipped the assignment for every USAGE BIT group
(`!BitLayout.IsBitItem(group)`), the published root and a bit-group element of a dynamic-capacity table included, whose
`__Land` reads the image. `__image` stayed null, and `AsImage` landed the members into `BlankImage(GroupUsage.Bit)`, all zero bits.

**Re-probe (own build, before the fix).** `01 LK-G GROUP-USAGE BIT. 05 LK-A PIC 1(3). 05 FILLER PIC 1(5).` with a host
`FromImage("\u001f")`: `AsImage()` was `00` (expected `1F`), and after the activated program stored `B"101"` into `LK-A`
the record read `A0` (expected `BF`). A DISPLAY or NATIONAL group of the same shape kept its FILLER.

**Fix.** The image is kept by every record whose `__Land` reads it: the published root and an element of a dynamic-capacity
table (a scope of its own), each at `__at = 0`, byte-aligned, whatever the group's usage. The guard is now "the record is
the root or a dynamic-table element" in place of "not a bit group". A nested group's `__Land` is never called (the root's
`__Put` lands it into the root's buffer), and a nested bit group may start mid-byte where a byte-granular `Substring` has
no meaning, so the nested groups of any usage no longer copy an image nothing reads.

**Test.** `HostEntryTests.AHost_PassesAUsageBitGroup_KeepingItsFillerBitsAcrossFromImageTheCallAndAsImage` (ISO
section 2023:14.2.3 GR8, `cite.py` OK): the root round trip and a repeated call, and a dynamic-capacity table of bit-group
elements whose FILLER bits the first call set and the second kept. The behaviour is observable only through the .NET host
entry, so it is a host test beside `HostEntryShapeDriftTests`, not a COBOL-only golden; the COBOL-level GROUP-USAGE BIT
goldens (`pb79_group_usage_bit` and the 2002 `g2-gu-*` cases) already cover the language.

**Register.** `kb/Work/PB6270.md` flipped to landed; it closes no inventory row (GR-14.2.3-8 and GR-14.2.3-13 stay CONFORMS).
