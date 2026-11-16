**2026-11-16, the re-read of row `1985-SR-VI.6.13.3-4` found this note's fix sound and one reading unstated (the Mythos lane's ruling on read pass 16, slice 1985-236; tracked copy `kb/Work/PB5683/2026-11-16-rulings-code-reads-batch-16.md`, mechanism 4).** The row was this note's. The refuter doubted the verdict; the Mythos lane REJECTED the doubt: the code conforms, a recorded determination, and the reading the fix left unstated is stated below. The section is copied unchanged, heading included.

## Mechanism 4, rejected: SPACE in a THROUGH range of EVALUATE is of class alphanumeric; a recorded determination

`§1985:VI.6.13.3 SR4`: "Two operands connected by a THROUGH phrase must be of the same class". The compiler takes
the figurative constant SPACE as class alphanumeric there: it accepts `SPACE THRU "Z"` and refuses SPACE THRU an
alphabetic item. The refuter doubts it because the only 1985 sentence that gives SPACE a category is in the MOVE
statement: "The figurative constant SPACE (SPACES) belongs to the category alphabetic" (`§1985:VI.6.19.4 GR3`).
Under that sentence taken generally the two results would be the other way round.

The text does not decide. That sentence stands in the rule that defines an elementary MOVE, beside a sentence
about ZERO that is expressly about a move; no rule gives a figurative constant a class anywhere else. What the
text says in general is that "A figurative constant may be used whenever 'literal' appears in a format"
(`§1985:IV.4.2.2.2.3`), and where it says what a figurative constant then is, it says a nonnumeric literal: "When
a figurative constant is used as the delimiter, it stands for a single character nonnumeric literal"
(`§1985:VI.6.27.4 GR8`). Both readings are honest. The precedence: GnuCOBOL has no rule here (`cobc` makes no
class test of the operands of an EVALUATE THROUGH: `typeck.c` and `parser.y` hold none), so the vendor manuals
decide, and IBM's Enterprise COBOL Language Reference (6.4, the figurative constants) treats SPACE as an
alphanumeric literal where an alphanumeric character is required. That is the compiler's reading.

REJECTED: the read's CONFORMS stands. **Register sentence owed (1985, `docs/conformance/`, key
`D-85-SPACE-CLASS`):** "In a context that requires a class and is not a MOVE, the figurative constant SPACE is of
class alphanumeric, as a nonnumeric literal is: `SPACE THRU "Z"` is one range of an EVALUATE and SPACE THRU an
alphabetic item is refused (`§1985:VI.6.13.3 SR4`). The 1985 text gives SPACE the category alphabetic in the MOVE
statement only (`§1985:VI.6.19.4 GR3`); GnuCOBOL makes no class test there; IBM Enterprise COBOL treats SPACE as
an alphanumeric literal." The witness is `pb5796`'s 1985 case (the reader names it).
