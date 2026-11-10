## PB5062: the 95 1985 change records withdrawn for amended text are re-decided against the amended 1985 (GAP 1,081 to 1,044)

When PB5045 applied both amendments to the 1985 transcription in place, 95 change records that had been decided against the
unamended text were withdrawn. Twelve Mythos 5.2 cobol-adjudicators re-decided all of them against the amended text, the
design's change kinds quoted verbatim (DESIGN-spec-conformance-review.md section 10.2), every citation run through
`cite.py --check`, no case left undecided. The result is 45 KEPT, 47 CHANGED and 3 REMOVED. Thirteen kinds moved from the
withdrawn record, most of them because an amendment brought the 1985 rule into line with 2023 (MAX, MIN, ORD-MAX and
ORD-MIN AR1; REWRITE and WRITE GR9 now pointers; subscript and class-condition texts word for word 2023's), others because
a lifted refusal is CHANGED, not REMOVED (PB5021). Many `to` lists and reasons were corrected even where the kind held:
several withdrawn reasons called a difference a 2023 addition that the amended 1985 itself now carries. Recorded through
`record_verdicts.py --edition 1985` (batch `pb5062-readjudicate-amended-1985`); the 1985 GAP falls from 1,081 to 1,044.
Leads filed: PB5092 (sibling records the re-decision did not reach now disagree with it), PB5093 (the catalog's
`amended_by` misses or misplaces Amendment 2 on three rows) and PB5094 (`work.py check` cannot take a 1985 rule id in
`inventory_rows`, so the triage's defect notes list their rows in prose).
