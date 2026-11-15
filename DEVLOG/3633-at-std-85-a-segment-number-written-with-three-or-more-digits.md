## Entry 3633 — 2026-11-14 18:10 PST — At --std 85 a segment-number written with three or more digits is refused (PB5379)

PB5379: `SEGMENT-LIMIT IS 010`, `IS 001` and a section header `SECTION 020`, `050` or `0050` compiled at --std 85, though
§1985:III.2 says "A segment-number may be expressed either as a one or two digit number" and the level-number of the same
definition list already drew COBOL1746. Re-probe on this build before the fix: each compiled and ran; after it, each is
refused COBOL6819 and `SEGMENT-LIMIT IS 09` / `SECTION 07` still run.

Root: `VersionConformancePass.ParseArm.ScreenSegmentNumber` read the integer through `Segmentation.ValueOf`, which strips leading
zeros, so only the value was tested. The fix asks the written form (more than two characters) in that one method, which serves
the SEGMENT-LIMIT clause, the section header and the declarative section header; the code is COBOL6819 (its catalog text now
carries the form rule), so no new code was spent (COBOL7270-COBOL7272 returned).

Goldens: `pb5379-segment-number-two-digit-form` (output, accepted at 85), `pb5379-section-segment-number-three-digits` and
`pb5379-segment-limit-three-digits` (refused at 85, COBOL6819). Rows re-verdicted PARTIAL to CONFORMS through
`record_verdicts.py --edition 1985`: 1985-FMT-XVI.2.3.2 and 1985-FMT-XVI.3.2.2 (GAP 169 to 167).

