## At --std 85, a sum counter named by LINAGE, RECORD DEPENDING, RECORD KEY or ALTERNATE RECORD KEY is refused under the 1985 rule

PB5361. The refusals were right and the citations were not. Probed on the base at `--std 85` with a CF FINAL sum counter `KTOT`
named by each clause: LINAGE and RECORD DEPENDING ON (already routed to the clause's own rules by PB5310) cited
`§2023:13.18.34.3 SR2` and `§2023:13.18.43.3 SR6` where §1985:VII.3.7.3 SR1 and §1985:VII.3.8.3 SR4 govern, and RECORD KEY and
ALTERNATE RECORD KEY still drew COBOL5092 citing the 2023 report-item section rule §2023:13.8.6.2.3, which COBOL-85 does not have.

The fix is one structure, not four edits. `Rule1985` is the one place a clause-rule citation chooses its edition. `RecordClauseRule`
rows carry `Of1985` and their message is the body alone, `Render` appending the edition's own citation (SR6's section list is
edition-worded too); the LINAGE operand rules use the same type; the file-control key table's `Rules1985` gained the SR2 category and
location rows of both key clauses, each `Supersedes` the 2023 row it replaces. `ReportSectionBinder.ReportItemOperandRefused` is the
one ask of a clause operand no data item answers: at 85 the sum counter is the clause's data-name (§1985:XIII.3.9.3 SR10d), so
`ResolveClauseOperand`, the key arms, ASSIGN USING and APPLY COMMIT reach the clause's own rules instead of the 2023 section rule.

Goldens: four refusals at 85 whose `expect` is the 1985 citation, one per clause. Drift tests: every 1985 designation is re-derived
from `specs/ANSI_COBOL_1985.md` and a row in force at 85 with none is refused (`RecordClauseRuleDriftTests`); the callers of the raw
refusal are pinned (`ReportItemOperandDriftTests`). No inventory row moves: the 1985 rules are KEPT bridges to 2023 rows that stay
CONFORMS. Registered, not fixed: the other 85 refusals of these tables (RELATIVE KEY, FILE STATUS) still cite 2023 text.
