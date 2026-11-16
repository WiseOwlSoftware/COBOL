**2026-11-16, ruled a defect: this note becomes a defect, harm under_rejects (the Mythos lane's rulings on the ten open readings of the 1985 code reads; row `1985-SR-VII.4.7.3-5`, ruling CONTRADICTED; tracked copy `kb/Work/PB5683/2026-11-16-rulings-open-readings.md`, section 6).** The section follows unchanged.

## 6. A written sign on the integer of WRITE ADVANCING must be refused at 1985 (PB5805): a DEFECT

**Ruling: CONTRADICTED. Harm: under_rejects. PB5805 becomes the defect.**

`§1985:III.2` sets the default for every "integer" of a general format: "it must be neither signed nor zero unless
explicitly allowed by the rules for that format". `§1985:VII.4.7.3 SR5`, "Integer-1 may be positive or zero, but
must not be negative", allows zero in so many words. It does not allow a sign in so many words, and where the
standard does allow one it says so: `§1985:IV.4.3.8.2.3 SR6`, "Integer-1 may be signed and, if signed, it must be
positive"; `§1985:VI.6.23.3 SR4`, "Integer-1 and integer-2 may be signed. Integer-1 must be positive". "Must not
be negative" forbids; it is not an explicit allowance. So integer-1 of ADVANCING is unsigned at 1985.

GnuCOBOL reads it the same way: `cobc/parser.y` line 17834 takes the count as `num_id_or_lit`, whose literal is
the `integer` of line 20033, and that refuses every literal with a sign, plus or minus ("unsigned integer value
expected", lines 20036 to 20040); `ZERO` is admitted beside it (line 19675).

What the compiler does (`contradictions-b4/src/adv_*.cob`, `--std 85`): `+3` and `+0` compile (WRONG: each must be
refused); `-0` and `-1` are refused (right); `0`, `3` and `ZERO` compile (right). Site:
`src/WiseOwl.Cobol.Compiler/Binding/Procedure/Verbs/SequentialIoBinder.cs#ScreenAdvancingCount`, which admits any
integer literal that does not start with `-`. The fix refuses a signed literal there at `--std 85`. Each other
edition's arm is decided from that edition's own text by whoever fixes it (2023's row is `SR-14.9.51.3-15`); 2002
is frozen, so the fix adds no 2002 case and no 2002 verdict.
