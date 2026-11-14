## Entry 3620 — 2026-11-14 13:24 PST — An ALL subscript followed by a qualifier draws COBOL2776 alone (PB5485, PB5509)

`FUNCTION MAX (TE (ALL) OF TT)` writes a qualifier after the subscripts, which §2023:8.4.2.3.2 Format 1 does not print. It was refused
COBOL2776 rightly, and also COBOL2363 ("ALL is permitted only ... an intrinsic function argument"), because `TryBindAllArgument` gave the
argument up on a qualification after the subscript group and the ordinary bind then refused the ALL. PB5485 and PB5509 filed the same
shape; one root. The suffix walk now flattens the trailing qualifier into the qualifier list, as every other reader of a reference does,
so the formation pass's COBOL2776 is the whole report (a spurious COBOL0819 from the failed bind went with it); a second subscript group
still declines. Re-probe before: three errors; after: COBOL2776 only, also with `(1:1)` between. Witness: unit test
`AllSubscriptQualifierAfterSubscriptTests` (the refusal-file format cannot assert an absent diagnostic). No inventory row closed.

