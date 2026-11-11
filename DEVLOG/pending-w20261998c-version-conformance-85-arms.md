## The 1985 arms of nine version-conformance mechanisms: INSPECT BEFORE/AFTER, the class condition, PERFORM BY, and six 2002 forms gated below 2002

Wave 20261998 group C (kb/Work PB5120, PB5122, PB5177, PB5194, PB5179, PB5172, PB5159, PB5116, PB5170). Each note was
re-probed on the branch's build before its fix, and each reproduced as filed.

**PB5120, INSPECT (wrong answer).** At `--std 85` an operand with both BEFORE and AFTER looked for the BEFORE delimiter
only from the AFTER boundary, so AYAAXAA with AFTER "X" BEFORE "Y" tallied 2. §1985:VI.6.18.4 GR8 b, the 2002 text and
the 2014 FCD measure the BEFORE portion from the leftmost position, and the operand participates in the intersection
with the AFTER portion (0 here); §2023:14.9.22.4 GR9 b reworded it with BACKWARD. A behaviour row,
`DialectBehavior.InspectBeforeFromFirstEligiblePosition` (2023, VCR row 7.56), sets `BoundInspect.BeforeFromLeftmost`
below 2023 and `CobolInspect.Region` searches from position 0; TALLYING, REPLACING and CONVERTING share `Region`, so all
three changed. Witness `14.9.22-INSPECT/pb5120_before_after_intersection` (`.out` and `.2023.out`).

**PB5122, the class condition (rejects legal source and under-rejects).** The operand screen applied the 2023 rule at
85: a numeric edited item was refused for ALPHABETIC and class-name tests, and the 1985 NUMERIC bars were not asked.
Each alternative of `ClassConditionModel` now carries its §1985:VI.6.3.1.2 rule set, chosen by
`ClassAlternative.RulesAt`, with three codes: COBOL6916 (ALPHABETIC, class-name over a numeric item), COBOL6917 (NUMERIC
over an alphabetic item or a group holding a signed item) and COBOL6918 (usage DISPLAY, or an alphanumeric function).

**PB5177, PERFORM VARYING BY index-name.** §2023:14.9.28.2 and §1985:VI.6.21.2 both print BY { identifier | literal },
so an index-name in the BY slot is now COBOL2119 at every edition, through one slot-by-kind table (`Admits`).

**Six forms gated below 2002**, each a construct row and one visit in `VersionConformancePass`: a reference-modified
function-identifier as the UNSTRING sender (PB5194, the existing construct now asked of every sender alternative), a
partial expression as an EVALUATE object (PB5179, 2002 F.2 item 48), the CLASS clause's IN phrase (PB5172), REWRITE's
word RECORD (PB5159), the TYPE clause's omitted control operand, ON/FOR word and OR PAGE (PB5116), and a configuration
paragraph's lone second period (PB5170, read through `ParagraphPeriods.LonePeriod`). The migrated 85 case
`a413_declined_phrase_positive_control` wrote `REWRITE F-REC RECORD`, which the 1985 format does not print; it now
writes the 1985 spelling.

Every fix has a positive at its introducing edition and a refusal below it. PB5171 (the SPECIAL-NAMES clause sequence and
once-only clauses at 85) and PB5198 (the RERUN facility) are left for the next link of the chain: the first needs the
clause-list table to rank repeatable clauses, the second a design decision.
