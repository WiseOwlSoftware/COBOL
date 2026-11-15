**2026-11-15, implementer wave 20262043 (group A): the design the note lacked, and the split.**

The note had no design doc to implement from (CLAUDE.md rule 2). `docs/rearchitecture/DESIGN-validate-facility.md` is it:
the facility is implemented at every edition that has it (this ends the A.4.14 decline), expanded at bind time into a
per-element plan as INITIALIZE is, with five build stages S1 to S5 (statement and format stage; DESTINATION; CLASS and 88
VALID/INVALID; INVALID WHEN, PRESENT WHEN, OCCURS, REDEFINES; VALIDATE-STATUS and the deletion of COBOL1708/COBOL1710).
Re-probe on this tree: the code sites are as the earlier addenda say (`DeclinedFacilityPass`, `CobolDeclined.g4`,
`DeclinedFacilityBinder`, `StatementBinder.cs:758`); nothing is built yet, so no row closes. The next implementer starts at S1.
