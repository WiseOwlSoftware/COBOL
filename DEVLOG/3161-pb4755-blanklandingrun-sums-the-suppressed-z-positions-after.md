## Entry 3161 — 2026-11-05 14:36 PST — PB4755: BlankLandingRun sums the suppressed Z positions after a floating string

Re-probe on this build: `PIC LLLZZ9 EDITING L FOR NEGATIVE IS "(("` (positive text two spaces, SR12c) holding 5 is
`      5`, and the de-editing MOVE gave +00000. `CobolEdit.BlankLandingRun` reads a BLANK landing wider than one
position as the whole run of suppression spaces and walks the string's positions until their widths sum to the run
less the landing's extra width. The walk stopped at the string's end, but a Z beside a FOR character-1 (§13.18.40.3
SR12 b) is suppressed too and its spaces belong to the same run, so the sum never matched, the method returned null
and the per-position reads dropped the digit.

Fix: when the run outlasts the string, every position of the string is suppressed and the landing is its last
position (§13.18.40.5 rule 6 a, the rightmost suppressed position), so a second walk continues over the positions to
the right of the string (one space each; a simple insertion symbol among them, its own width) until the widths
account for the run exactly. Otherwise the method still returns null.

Tests: unit `DeEdit_FloatingExtendedSignBlankWideLandingBeforeSuppressedZ_RoundTrips` (round trip through Format and
DeEdit), golden `pb4755_deedit_for_floating_blank_landing_before_z` (accept 2023; values derived from rule 6 a and
the GR14 'es' width) and its refusal below 2023 (COBOL0900). closes_rows: none (no inventory row claimed).

