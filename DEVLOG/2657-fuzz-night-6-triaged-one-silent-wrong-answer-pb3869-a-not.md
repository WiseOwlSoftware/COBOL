## Entry 2657 — 2026-10-27 10:30 PDT — Fuzz night 6 triaged: one silent wrong answer (PB3869, a NOT-only SIZE ERROR phrase), no regression of the thirteen pre-run fixes

The sixth overnight differential fuzz run (fuzz-20261026-225310, 6,646 programs, judge main deae52c90) was triaged by
twelve read-only Opus adjudicator slices over 260 signatures (1,773 programs): 123 WiseOwl correct, 59 GnuCOBOL's side,
42 latitude, 17 not comparable, 11 uninitialized, 7 model errors and one WiseOwl defect. No program of a landed note
regressed, the thirteen notes that gated the run and the fifth night's PB3829 to PB3832 included.

The defect, filed for the owner (standing instruction 2026-10-24) and approved for fixing on 2026-10-27: an arithmetic statement
written with only `NOT ON SIZE ERROR` takes the SIZE ERROR phrase's disposition with checking off. `DIVIDE 1 BY 0 GIVING Q
NOT ON SIZE ERROR ...` continues with Q unchanged, and `ADD 1 TO X` (PIC 9 VALUE 9) keeps 9 while its phraseless twin stores
0; §14.7.5 says the NOT phrase is then "ignored" and the no-phrase rules apply (kb/Work PB3869). One side observation became
a witness on open note PB3843 (a false COBOL0901 after a COBOL2269). Record:
`docs/rearchitecture/evidence/fleet-optimization/2026-10-27-differential-fuzz-sixth-night.md`.

