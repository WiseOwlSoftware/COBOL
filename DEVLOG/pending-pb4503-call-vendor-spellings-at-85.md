## PB4503: at COBOL-85 each Format 1 vendor CALL spelling is reported once, as a 2002 introduction; --permissive accepts it

At `--std 85`, OMITTED and BY VALUE in a CALL that names no prototype each drew two errors, COBOL0899 from
`CallBinder.Format1Narrowing` and COBOL0900 from the edition-conformance pass. Under `--permissive` the COBOL0899
warning said "accepted" while the pass's COBOL0900 still rejected the program. Every COBOL0899 at 85 also told the user
to write the program-prototype CALL with an AS phrase, which COBOL-85 does not have. The four literal and expression
spellings drew COBOL0899 alone, with the same wrong advice.

The owner decided on 2026-11-03 that `--permissive` accepts the vendor spellings at 85 too, after the PB4499 research
showed IBM listing OMITTED and BY VALUE in CALL among its extensions over its ISO 1989:1985 baseline and HP documenting
both as HP extensions. `CallBinder.Format1Narrowing` is now the one place a CALL argument's spelling is judged at every
edition. Each spelling names the construct of the edition that introduced it: `call-by-value-2002`,
`omitted-arguments-2002`, and a new row, `call-literal-expression-argument-2002`, quoted from ISO/IEC 1989:2002 Annex
F.2 item 15 ("CALL statement. Arithmetic expressions and literals may be used as arguments of the CALL statement.").
Below 2002, strict reports that row's COBOL0900 once; `--permissive` warns with text that names COBOL-85 and binds the
argument as 2002 does. From 2002 on nothing changes. The pass's three CALL-argument gates are gone; it keeps
OPTIONAL, the omitted-argument condition and INVOKE's OMITTED.

VCR row 7.47 gains item 15 (GnuCOBOL `-std=cobol85` accepts a literal argument and refuses an expression), and
docs/CONFORMANCE.md's CALL paragraph states the 85 rule and the decision. Witnesses: three new
`FormatOneVendorCallPermissiveTests` cases (six COBOL0900 and no COBOL0899 at 85; six warnings and no error under
`--permissive`; a literal argument run at 85).
