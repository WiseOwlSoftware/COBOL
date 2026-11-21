- **D-85-ADVANCING-ZERO — the figurative constant ZERO as the integer of an ADVANCING phrase at COBOL-85 (kb/Work PB5902).**
  This entry speaks for COBOL-85. `WRITE ... BEFORE/AFTER ADVANCING ZERO LINES` (and `SEND ... ADVANCING ZERO`) is accepted
  and advances zero lines. The text admits two readings. A figurative constant "may be used whenever 'literal' appears in a
  format" (`§1985:IV.4.2.2.2.3`), and the ADVANCING format writes `integer`, which "must be a numeric literal which is an
  integer" (`§1985:III.2`), of which "Integer-1 may be positive or zero, but must not be negative" (`§1985:VII.4.7.3 SR5`): read
  narrowly, ZERO may stand only where the word `literal` is printed; read as the definition gives it, `integer` is a numeric
  literal and ZERO stands for the value zero SR5 permits. The text does not settle which, so the precedence of CLAUDE.md rule 1
  decides: GnuCOBOL 3.2 accepts it (`-std=cobol85`). The COBOL-85 validation suite reads it the same way: CCVS-85 program
  SQ101M (`tests/nist/programs/SQ101M.cob`, test WRT-TEST-GF-25) writes `BEFORE ADVANCING ZERO` to overprint a marker line as
  a feature under test; it is evidence of how the text was read when it was in force, not the authority. Decided by the Mythos
  session on 2026-11-20, reversing an adjudication and its refuter that read the rule narrowly without that evidence; the
  earlier acceptance of `SEND ... ADVANCING ALL ZERO` (kb/Work PB5289) stands. Source: `kb/Work/PB5902.md` and its addenda.
