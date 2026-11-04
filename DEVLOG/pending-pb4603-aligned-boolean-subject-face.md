## PB4603: the ALIGNED refusal no longer tells a USAGE NATIONAL boolean item that its usage is DISPLAY

`CheckAlignedClausesPass.DescribeAlignedSubject` wrote "an elementary boolean item whose usage is DISPLAY" for every
boolean-category subject of COBOL1942 (ISO §13.18.1.3 SR1). A boolean item may also be described with USAGE NATIONAL
(§13.18.60.3 SR12), so `01 B PIC 1(4) USAGE NATIONAL ALIGNED.` was told something untrue. PB1654 had fixed the same
untrue face in the group arms and left this one.

**Re-probe (Debug build, before the fix).** `PIC 1(4) ALIGNED`, `PIC 1(4) USAGE NATIONAL ALIGNED` and
`PIC 1(4) USAGE DISPLAY ALIGNED` all printed "whose usage is DISPLAY".

**Fix.** The boolean arm now reads `ItemCategory.Face(item)` ("a category-boolean item"), the one reading of an
elementary item's Table 2 category the other arms already use, then the BIT hint (§13.18.60.4 GR5). It states no usage,
so no arm of the pass keeps a classification of its own. Sweep: `grep -rnE "whose usage is|usage is DISPLAY|elementary
boolean item" src` found no other diagnostic that guesses a usage.

**Golden.** `conformance-suite/cases/13.18.1-ALIGNED/pb4603-aligned-national-boolean.cob` with
`wiseowl/diagnostics/pb4603-aligned-national-boolean.json`, refused at 2002, 2014 and 2023 (ALIGNED is reserved from
2002), pinning the new words. No inventory row was claimed or closed.

**Self-review.** Architecture: no new classification, the pass defers to `ItemCategory.Face`. Full code: the message
cites SR1 and GR5, both run through `cite.py --check`. Performance: the message is built only on a refusal.
Duplication: one fewer hand-written face.
