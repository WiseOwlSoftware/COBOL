## FUNCTION EXCEPTION-STATEMENT is 31 characters at 2002 and 2014, 63 from 2023

PB5698 and PB5717 (one mechanism). `EcFunctions.Statement` padded to 63 at every edition, so at `--std 2002` and
`--std 2014` `FUNCTION LENGTH (FUNCTION EXCEPTION-STATEMENT)` was 63. §2002:15.27.2 RV1/RV2 and §2014:15.29.2 RV1/RV2
say 31 ("the returned value is 31 spaces"; "a 31-character alphanumeric character-string"); §2023:15.32.3 RV1/RV2 say 63.

Re-probe on this build before the fix: 63 at 2002 and 2014; after: 31, 31, 63 at 2002, 2014, 2023, with the statement
name `STRING` space-filled to column 31 after a STRING overflow under `>>TURN ... WITH LOCATION`. EXCEPTION-STATUS (31 at
every edition) was already right. EXCEPTION-LOCATION's width is implementor-defined at every edition and has no
edition split, so no sibling arm needed a change; no other 63 literal exists in `src`.

Fix shape: the edition fact is a `DialectBehavior` member, `ExceptionStatementIs63Characters` (changed in 2023, VCR row
7.117, `compiler-facts.json`), asked once by `EcFunctions.StatementLength(editionYear)`; the renderer passes that width to
`EcFunctions.Statement(int length)`, which replaces the parameterless method (the shipped signature is removed in
`PublicAPI.Unshipped.txt`).

Golden: `conformance-suite/cases/15.27@2002-EXCEPTION-STATEMENT-FUNCTION/g2-exception-statement-31` (accept 2002; the
width is shown through FUNCTION LENGTH and a closing bracket, because the harness trims trailing spaces, PB475) and its
refusal at 85 (`wiseowl/diagnostics/g2-exception-statement-31.json`, COBOL0900 for `>>TURN`). Rows `2002-RV-15.27.2-1`
and `-2` close (GAP 1173 to 1171).

The gate found one more case that pinned the 63-wide value at 2002 (`_unassigned/pb2745_statement_context_scope`); its expected output now carries 31 spaces.
