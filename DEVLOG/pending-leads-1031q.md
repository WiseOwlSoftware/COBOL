## kb/Work: file PB4229, the lead of PB4206's branch

PB4206's branch (PR #901) reported that a word reserved at every edition, written as a procedure-name (`PERFORM READ.`, `PERFORM DISPLAY.`), draws COBOL0001 `unexpected '.'` instead of COBOL0901. This was filed as PB4229 (MINOR, misleads_diagnosis), with the sites in `procedureName` and `CobolLexer.IsReservationGated`.

No open note covered the mechanism: PB4206 involves a gated token, and PB4132 is held by an unlanded branch, so neither was edited. PB4229 lists both as related.
