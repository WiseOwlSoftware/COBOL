## The 2002 read of the first 974 word-identical rules files 18 defect notes

**What was read.** The first 974 2002 rules that are word-identical to the 2023 text were read against the pinned compiler at `--std 2002` (readers p-01..p-19, refuters q-01..q-20; the files are under `E:/COBOL-coord/scratch/op-20261114/read-2002/out`). The extraction found 141 defect cases. They fall into 19 new groups, 2 extensions of open notes, 4 groups on facilities the owner decides, and 2 groups dropped. This change touches only `kb/Work/` and this entry.

**New notes (18 filed; every citation re-checked with `scripts/spec/cite.py --check`).**

- PB5598: the 2014 ACTIVE-CLASS bans (property methods, PROPERTY clause, inline invocation) applied at 2002
- PB5599: the directive word table is the 2023 one (FLAG-85, FLAG-NATIVE-ARITHMETIC, PUSH, EQUATE)
- PB5600: FLAG-85 and FLAG-NATIVE-ARITHMETIC gated as words only, trailing text accepted, no flagging
- PB5601: a DEFINE with a boolean-expression operand accepted
- PB5602: a subscripted data-name refused in RECORD KEY, RELATIVE KEY, key COLLATING SEQUENCE and CONTROL
- PB5603: communication SEND refuses an inline invocation and a function-identifier
- PB5604: USAGE POINTER record under an output CD refused as not implemented
- PB5605: an ODO table in an implicitly redefining record never checked (EC-BOUND-ODO), wrong answer
- PB5606: ADDRESS OF and SAME AS accept a communication-section item
- PB5607: STRING WITH POINTER accepts a pointer item too small
- PB5608: a REPLACE partial-word with a character outside the repertoire accepted
- PB5609: SOURCE-COMPUTER WITH DEBUGGING MODE without a computer-name is a parse error
- PB5610: a bare factory working-storage INVOKE argument fails the backend compile
- PB5611: ANY LENGTH accepted as CALL AS NESTED argument and RETURNING item
- PB5612: a fatal EC-SORT-MERGE-FILE-OPEN on MERGE whose declarative completes normally continues the run unit, wrong answer
- PB5613: floating-point literals with a 32-36 digit significand or four-digit exponent accepted
- PB5614: the default `$` currency symbol refuses a national numeric-edited picture
- PB5615: SET ... TO ADDRESS OF PROGRAM refuses an alphabetic identifier

**Held, not filed.** USE AFTER EXCEPTION OBJECT naming a parameterized interface from the REPOSITORY paragraph: it depends on a reading of the text that is not settled.

**Extended (rows added to `inventory_rows`, reason in a dated file beside each).** PB5575 (34-digit compile-time arithmetic at 2002: six rows) and PB5565 (PADDING CHARACTER on an indexed or sort-merge entry at 2002: one row).

**Owner-decision notes extended (status and kind unchanged).** PB5577 (VALIDATE: six rows; a bare VALIDATE is accepted with warning COBOL1580 and validates nothing, a wrong answer), PB5578 (FORMAT and SELECT WHEN: four rows; the refused SELECT WHEN also draws a misleading cascading COBOL1672 with CODE-SET), PB5579 (WRITE FILE and REWRITE FILE: two rows), PB5580 (multiple class inheritance: three rows).

**Dropped.** Fixed on main already: the ODO object of a RECORD KEY (PB5445; rows also in PB5582). Not a defect: RESUME AT NEXT STATEMENT after EC-SORT-MERGE-RELEASE ending the SORT, because §2002:14.8.36.3 GR16 terminates the SORT when a USE procedure does not complete normally. Two further groups needed no new note: the CURSOR/CRT STATUS and SCREEN SECTION refusals are already in PB5581, and the cases the refuters upheld with the compiler outcome right.
