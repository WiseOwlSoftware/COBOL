## PB1925: every bound-arm conformance diagnostic names its statement, not only the SORT and MERGE ones

kb/Work PB1925 said the placement diagnostics COBOLNET2700 and COBOLNET1572 carry no source position because the
conformance pass builds them from bound nodes, which have no span. Re-probed on a fresh build, those two print
`file(line,col)`: kb/Work PB812 had given `BoundSort`, `BoundMerge` and `BoundCommitRollback` each an `At` cursor.
The note's stated cause was the real one, though, and PB812 had fixed three nodes rather than the cause. A sweep of
every `diagnostics.json` negative at its first refused edition, keeping the error and warning lines with no
`(line,col)`, and direct probes showed the rest of the pass still printing bare: `SET p UP BY` and `SET fp TO fp`
(COBOLNET0900), `MOVE SPACE TO numeric` (COBOLNET0902), INITIALIZE REPLACING, the USAGE and PICTURE data gates, and the
`>>COBOL-WORDS` SR3 and SR4 checks (COBOLNET1623).

The fix is the shape that makes the next case automatic. `BoundStatement.At` is now a property of the base node,
stamped once by `StatementBinder.BindStatement`, the funnel every statement passes through, which already positions the
diagnostic cursor. The three per-node `At` properties and their three construction-site assignments are deleted.
`VersionConformancePass.WalkStatement` and the SORT/MERGE placement walk position the sink from the node;
`GateDataItem` positions it from `DataItem.DeclaredAt`; `ValidateCobolWords` from `CobolWordsOp.At`, which the
directive stage fills with the directive's resultant line and indicator column. A new bound statement, or a new gate
over one, inherits the position. `docs/COBOLNET_VALIDATION_DESIGN.md` (Positions) names the mechanism.

Witnesses: `BoundArmDiagnosticPositionTests` (31 cases). The placement rules are tested on their refusal paths and
their legal twins: SORT in a SORT input procedure, a SORT output procedure and a MERGE output procedure at every
edition (ISO 14.9.40.3 SR3); MERGE in each of the three at 2023 as an error, as a warning under `--permissive`, and
accepted at 2002 and 2014 (14.9.24.3 SR1, Annex E.2 item 20); COMMIT and ROLLBACK in each (14.9.7.3 SR2, 14.9.36.3
SR2); a statement nested under an IF; the same statements outside a procedure draw nothing. The SET, MOVE, USAGE and
COBOL-WORDS arms are pinned at their own line and column. This group touches only compile-time diagnostics, so the
runtime exception paths (I-O statuses, EC-I-O, AT END, INVALID KEY, lock contention, work-file failures) are not
changed by it.

The sweep's other 23 position-less negatives belong to other passes (report-section RD rules, external-file
consistency, OO method-header rules, OptionsBinder's COBOLNET0806) and are reported as leads.

Branch review round 1 (two findings, both fixed). The SUM-counter PICTURE gate of the report-Sums loop still ran with no
cursor; it now positions its sink at the counter register, whose DeclaredAt is the SUM entry. The COBOL-WORDS operation
carried its position twice (a required 0-based Line that nothing read, and the new optional At); Line is deleted and At
is the one required positional member, filled by the directive stage, so no producer can leave it unset by omission.
Two new cases (printable and non-printable SUM entry) pin the SUM arm.

Branch review rounds 2 and 3. Round 2 corrected the register (the landing map and kb/Work PB2034 no longer name the deleted CobolWordsOp.Line). Round 3 found that PB2034 still claimed the reserved-word, word-length and word-character diagnostics print bare: they sit inside ParseArm, whose sink cursor follows the walk, so they already print file(line,col) (probed: a 41-character data name under --std 85 and COMMIT as a data name under --std 2023). PB2034 now names only what still prints bare, each re-probed with --source-format free: the report-section RD rules, external-file consistency 1573/1575/1624, the OO header rules 2648/1542/1643 and OptionsBinder COBOLNET0806, so the next implementer starts from real work.
