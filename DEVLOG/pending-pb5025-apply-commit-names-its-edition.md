## PB5025: APPLY COMMIT below COBOL-2023 is refused by its edition, and the last construct gate is gone

kb/Work PB5025, wave 20261946 group C. Below COBOL-2023 the I-O-CONTROL `APPLY COMMIT` clause drew COBOL1970, the
closed-format refusal ("'APPLY' is not a clause of the I-O-CONTROL paragraph"), because `ioControlClause` predicted
`applyCommitClause` only under `{is2023()}?`. D7 (DESIGN.md section 1.1) puts that judgement in the conformance pass,
which names the edition. It was the last of the three construct gates PB2291 recorded; PB5007 removed the other two.

**Re-probe.** On c865ee6dc3 the `apply-commit-clause-2023` matrix program drew COBOL1970 at `--std 85`, `2002` and
`2014` and compiled at 2023.

**Fix.** The predicate is deleted (the union grammar). `VersionConformancePass.ParseArm.VisitApplyCommitClause` checks
`apply-commit-clause-2023` on recognition, and the row's code is COBOL0900, so the same program now draws
`COBOL0900: APPLY COMMIT clause (commit and rollback) requires COBOL-2023`. The binder's section 12.4.6.3.3 syntax rules
are unchanged. No other I-O-CONTROL clause begins with a user word, and the token-level reservation gate retypes a
declared APPLY or COMMIT, so a 2002 or 2014 program keeps those words as names. `EditionReadDriftTests` drops the
recorded site, and `ConstructGates_AreOnlyTheOnesPb2291Found` becomes `ConstructGates_AreNone`. The verdict stays,
holding no sites, so a new parse-time construct gate fails the test.

**Goldens.** The new `cases/12.4.6.4-SAME/pb5025_files_named_apply_commit` (accept 2002, 2014) declares files named
APPLY and COMMIT and writes `SAME RECORD AREA FOR APPLY COMMIT.`, the clause's own spelling. Its output comes from
section 12.4.6.4.4 GR2: both records hold PB5025, and the READ returns it. The refusals `pb3338_apply_commit_below_2023`
and `pb3540_commit_under_method_below_2023` now expect COBOL0900.

**Register and docs.** FMT-12.4.6.3.2 keeps CONFORMS: a record_verdicts batch corrected its note and added the gate's
site, and the GAP stays 0. The VCR row 39 text and the generated index, DESIGN.md D7 and DESIGN-architecture-review
section 8.4 are updated. The sibling sweep found two `{isXXXX()}?` predicates left in the grammar, the bare LOCATION
in PERFORM and OPEN's RETRY. Both are word status.

**Gate.** `=== BUILD-LOCAL GATE: GREEN — Conformance 22,757/22,757 · Unit 40,617/40,617 · Characterization 37/37 cases ran (skipped 0)` (run 000001-20261109T113529Z, at b1a0bbcb5d). Self-review (architecture, code, performance, duplication): one edition check per clause, made at the one recognition site, as the sibling I-O-CONTROL arms (MULTIPLE FILE, RERUN) make theirs; no new allocation and no second statement of the rule. No new defect was found.
