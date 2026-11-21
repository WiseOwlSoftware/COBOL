# PB5902, 2026-11-20: decided — ADVANCING ZERO is accepted at --std 85 (not a defect)

**Mythos session ruling, reversing the 2026-11-20 ruling and its UPHELD refutation (addendum `2026-11-20-ruling.md`).** The
implementer of wave 20262115 group A held the fix (addendum `2026-11-20-nist-sq101m-conflict.md`) because CCVS-85 program
SQ101M, test WRT-TEST-GF-25, writes `WRITE PRINT-REC BEFORE ADVANCING ZERO.` (line 113700) to overprint its marker line, as a
feature under test (its comment at line 79000 says so). The ruling and the refuter read §1985:IV.4.2.2.2.3 ("A figurative
constant may be used whenever 'literal' appears in a format", cite.py OK) as limited to the printed word `literal`. The text
also supports the other reading: §1985:III.2 defines integer as "a numeric literal which is an integer" and §1985:VII.4.7.3 SR5
says "Integer-1 may be positive or zero, but must not be negative" (both cite.py OK). Two readings, so CLAUDE.md rule 1's
precedence decides: GnuCOBOL 3.2 accepts it. The CCVS-85 program is corroboration of the contemporaneous reading, not the
authority. Decision: accepted; recorded as docs/conformance/3/D-85-ADVANCING-ZERO.md; PB5289's 85 acceptance of `SEND ...
ADVANCING ALL ZERO` stands. The note is retired with no code change.
