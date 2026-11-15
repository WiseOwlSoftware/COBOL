**2026-11-15, wave 20262045 group A: re-probed, decided, landed. A size error in a 1985 position expression ends the run unit, naming its condition.**

Re-probe at `--std 85` (`E:/COBOL-coord/scratch/w20262045a/p54/rm.cob`): `IF X (B / Z : 1) = "A"` ends the run unit with EC-SIZE-ZERO-DIVIDE, and
`IF X (Z : 1) = "A"` (Z = 0, no arithmetic) ends it with EC-BOUND-REF-MOD, "reference modification (0:1) out of range". The first half of the note
(a function argument in MOVE) was discharged in wave 20262044: it is refused COBOL0900 at 85.

The decision the earlier addendum left open (CLAUDE.md "Ownership"; recorded in `docs/conformance/7/DOC-A.1-70.md`):
- §1985:B.2.2 item 45 restates §1985:VI.6.4.2 GR3, whose subject is "the affected resultant identifiers" of an arithmetic statement; a reference
  modifier has no resultant identifier (a 1985 subscript admits no arithmetic expression, COBOL0900).
- A position whose value could not be formed (a zero divisor, §1985:VI.6.4.2 GR2, "always causes a size error condition") would have to be invented
  to continue, and §1985:IV.4.3.8.4.4 GR4a asks that the evaluation "must result in a positive nonzero integer". An invented zero ends at the
  reference modifier's own range check (EC-BOUND-REF-MOD, shown above); an invented stale value reads a character no rule names. Both are worse
  than ending at the cause.
- A condition is different (PB5501): its leaf has an outcome to choose without inventing a value (FALSE).

So the code is unchanged, `ArithmeticEmitter.EmitPositionValue` keeps the termination, and the note lands as decided. No inventory row was claimed
(`closes_rows: []`). The note's other suggestion, one place for the 1985 disposition of every expression evaluation, would be a restructuring with
no behaviour change left to make once this is decided, so it is not taken.
