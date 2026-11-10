## Entry 3414 — 2026-11-09 23:53 PST — PB5062, PB5092, PB5051: the 1985 change records withdrawn for amended text are re-decided, and their inconsistent siblings take one ruling each; PB5086, PB5075: the COBOL-85 multiple file reel rules and the 18-digit composite of operands are enforced

### PB5062, PB5092, PB5051: the 1985 change records withdrawn for amended text are re-decided, and their inconsistent siblings take one ruling each

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

### PB5086, PB5075: the COBOL-85 multiple file reel rules and the 18-digit composite of operands are enforced

Two under-rejections the 1985 `--std 85` draft triage found, finished on the branch that carried the PB5086 adjudication.
The finish-first cherry-pick took that branch's three commits (PB5062's re-decision, PB5051, PB5092's sibling rulings) onto
main without conflict.

PB5086. The MULTIPLE FILE TAPE clause was resolved and dropped, so OPEN EXTEND of a file in it and a MERGE naming two files
of one clause compiled clean at 85. The adjudication decided DEFECT for both: the hardware-dependent exemption
(§1985:I.1.5.2.5.1) excuses the clause's function, not the syntax rules of the OPEN and MERGE statements. The clause now
records its membership on each file (`FileModel.MultipleFileReels`), and three rules read it: COBOL6814 (OPEN EXTEND,
§1985:VII.4.3.3 SR2), COBOL6815 (MERGE, §1985:XI.4.1.3 SR6; SORT is exempt, §1985:XI.4.4.3 SR7) and COBOL6816, the
adjudicator's named sibling (an OPEN of a file on a reel that holds a non-sequential file, §1985:VII.4.3.4 GR20 d). A private
`IsSequentiallyOrganized` that duplicated `FileModel.IsSequential` is gone. `docs/CONFORMANCE.md` section 3.4 is the
determination the clause lacked.

PB5075. `CheckComposite` capped the composite at 31 digits at every edition and counted every operand. At 85 the cap is 18
(§1985:VI.6.4.4 item 2) and MULTIPLY and DIVIDE superimpose the receiving items only (§1985:VI.6.20.3 SR3,
§1985:VI.6.11.3 SR3). The cap is now `EditionContext.MaxDigits` and the six MULTIPLY and DIVIDE call sites pass
`CompositeMembers.ReceivingItems85`, so `ADD A10 TO F9` (9(10), V9(9)) is refused and `MULTIPLY F18 BY R18` accepted. The
old justification, CCVS-85 NC101A, applied the 2023 composite to a 1985 statement: its 1985 composite is one 9(18) receiver.
`docs/NUMERIC_DESIGN.md` and the SR-14.7.7-2 row's notes (a `record_verdicts` batch) say so.

Goldens: `cases/_unassigned/pb5086_multiple_file_reel_legal_85` and `pb5075_composite_receivers_only_85` (accept 85), and
four refusals at 85 (`pb5086-open-extend-multiple-file-reel`, `pb5086-merge-files-on-one-reel`,
`pb5086-open-multiple-file-reel-relative`, `pb5075-composite-over-18-digits-85`). They are unassigned with `rules: []`
because the inventory has no 1985 verdict rows yet. Neither note closes a row.

