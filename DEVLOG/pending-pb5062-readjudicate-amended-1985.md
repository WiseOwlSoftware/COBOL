## PB5062, PB5092, PB5051: the 1985 change records withdrawn for amended text are re-decided, and their inconsistent siblings take one ruling each

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

PB5092 is then done on the same branch. Mythos 5.2 adjudicators gave ONE ruling to each family of records the re-decision left
inconsistent, and a second round took the further siblings the first named. The 11 rules that require AT END or INVALID KEY
when no USE procedure applies are all CHANGED (2023 lifts the refusal and states the run-time effect; six were REMOVED);
eight READ general rules on the I-O status and the file position indicator are CHANGED (2023 leaves the indicator unchanged
after a record operation conflict, reachable with no post-1985 syntax); five INSPECT general rules are CHANGED (BACKWARD is
a case 2023 adds wherever its rule follows the scan direction). 24 records recorded (batch `pb5092-sibling-rulings-1985`);
the GAP rises from 1,044 to 1,053, because nine rows had been closed on a KEPT record their own text does not support and now
need their own 1985 verdict. PB5086's contested question is decided: an accepted-inert MULTIPLE FILE TAPE clause leaves the
OPEN EXTEND and MERGE syntax rules in force, so both are compiler defects (the hardware exemption covers the clause's
function only).

Two small items ride along. PB5051: Errata C32 (SET general rule 2, `index-name-1` corrected to `index-name-2`) had
justified the correction by saying the printed word would make general rule 3a's conversion unreachable, which is unsound,
because the printed sentence constrains only the value of index-name-2, not its table. The Errata entry and its flag now give
the sound grounds (each sentence of the rule pairs an index with its own table; an occurrence number means something only in
its index's table; 3a's conversion reads index-name-2's value in its own table; 2023 names index-name-2's table), and the
correction stands. MANDATORY-PRACTICES P1 no longer says Mythos's only planned use is the architecture review's R1: it takes
the work the owner approves for Fable (review lead R1-1 of PR #1435).
