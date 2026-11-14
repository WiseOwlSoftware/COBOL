## PB5302: at --std 85 a format 1 SEARCH judges its incremented index against both bounds

kb/Work PB5302 asked whether COBOL-85's serial SEARCH ends when the incremented index is still below the table. PB5121 had made
an index below 1 start the scan at 85, which §1985:VI.6.22.4 GR2a requires, since it tests only "greater than". The advance test
after each increment still looked only at the upper bound. So an index left at -1 by `PERFORM ... VARYING IX FROM 3 BY -2` went
on to -1, 0, 1, 2, and the search found occurrence 2.

**Adjudication: the text wins.** GR2b repeats the search "unless the new value of the index-name settings for identifier-1
corresponds to a table element outside the permissible range of occurrence values". That wording names no bound, so both apply.
Figure 1 loops back to the "greater than" test only, but GR10 says the figure "is not intended to dictate implementation".
§2023:14.9.37.4 GR4 words the advance the same way.

**Re-probe** (the reviewer's `m1.cob`, this tree before the fix): `--std 85` printed `FOUND AT 2` and `--std 2023` printed `AT END`.
After the fix both print `AT END`.

**Fix.** `ControlFlowEmitter.EmitSerialScan` forms the in-range test once, as an upper-bound test and an outside-the-range
test. The start test uses outside-the-range from 2002 and the upper bound at 85, unchanged from PB5121. The advance test now
uses the other one: the upper bound from 2002, where the start test leaves no index below 1 to increment, and outside-the-range
at 85. Code emitted from 2002 on is byte-identical. The 85 characterization snapshot `char_occurs.85.g.cs.txt` gains the `< 1`
half. Doc comments were updated in `DialectBehavior.SearchIndexBelowTableUnsuccessful`, `BoundSearch` and `EmitSerialScan`, and
VCR row 7.56 was corrected.

**Golden.** `14.9.37-SEARCH/pb5302_search_advance_below_table_85` (accept 85) starts at IX = -1 and prints `AT END`. The PB5121
witness, which starts at 0, still prints `FOUND AT 2`. No row was re-verdicted, because the inventory carries no 1985 rows yet
(PB5094).
