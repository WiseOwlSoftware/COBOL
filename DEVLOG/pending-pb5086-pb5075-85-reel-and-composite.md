## PB5086, PB5075: the COBOL-85 multiple file reel rules and the 18-digit composite of operands are enforced

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
