## Entry 2179 — 2026-10-17 23:37 PDT — PB674: the floating-point numeric literal is refused below COBOL-2002; the START half was already gated by PB370

PB674 held the 85-versus-2002 edge of three constructs the project could not date from the standard text it holds. Two had
landed or were stale; one was a real under-reject.

**ASSIGN ... USING** landed as PB746 (2026-10-05). **START ... KEY IS LESS THAN / `<=`**: re-probed on this build at
`--std 85`, both spellings draw COBOLNET0900; they landed as PB370 (`start-reverse-operators-2002`,
`VersionConformancePass.VisitStartKeyPhrase`), after the registrar's 2026-09-23 probe. Discharged, no change.

**The floating-point numeric literal** (ISO §8.3.3.3.3 1) compiled and ran at `--std 85`: `COMPUTE R = 1.5E2` printed
`0015000` for `PIC 9(5)V99`. The R61 method (GnuCOBOL's per-standard dialect files) cannot date it: `cobc -std=cobol85`,
`cobol2002` and `cobol2014` all accept it and no `config/*.conf` key mentions it. The date rests on other evidence, recorded
as a derived edge in VCR row 7.37 and the construct's `description`: IBM's Enterprise COBOL Language Reference lists
"Floating-point numeric literals" among its IBM extensions beyond the standards it conforms to (the 85 Standard first) and
has no row for them in its 2002/2014 feature list; none of the 459 NIST CCVS-85 programs writes one; every conformance case
that writes one was already filed at 2002 or later; and the traceability inventory's `editions` column for SR-8.3.3.3.3-1..4
and GR-8.3.3.3.3-5 already read `2002,2014,2023`. The 2002-versus-2014 split is not derivable in-repo, so the row takes the
provisional 2002 edge, the one that never refuses a legal 2002 program.

**Fix shape.** One construct, `floating-point-literal-2002`, gated on the TOKEN in `LiteralScreen.Run`
(`GateFloatingPoint`), over the new `LiteralTokens.FloatingPointTypes` (`FLOATLIT`, `COMMA_FLOATLIT`; the sign-adjacent
`FN_SIGNED_*` lexer twins are re-typed to these two). It is the walk that already gates the hexadecimal and zero-length
literals, so no consumer carries its own gate: probed with a COMPUTE operand, MOVE source, VALUE, level-88 VALUE, relation
operand, PERFORM VARYING value and FUNCTION argument, six positions gave six COBOLNET0900. A parse-arm override on
`numericLiteralCore` was rejected: a visitor that declines to descend hides a literal, which is the reason `LiteralScreen`
is a plain walk. `LiteralScreenDriftTests.FloatingPointTokenSet_IsEveryTokenDefinedOverAFloatBody` pins the token set to the
lexer's `FLOAT*_BODY` fragments and the construct to one reporting site, so a new float spelling joins the gate or fails there.

**Goldens.** `8.3.3.3.3-Floating-point-numeric-literals/pb674_floating_point_literal_2002` (accepted 2002, 2014, 2023; the
literal in VALUE, MOVE, COMPUTE, ADD, a relation and a FUNCTION argument, values computed from rule 5) and
`pb674-floating-point-literal-85` (refused at 85, COBOLNET0900). One existing case moved:
`wiseowl/cases/_unassigned/pb248_integer_arg_float_admitted` was `accept: [85]` and writes `6.5E1`; it accepts from 2002.
Sibling sweep: fixed-point literals over 18 digits were already gated at 85 (COBOLNET0802).

**Rows.** None open; batch `w1115f-pb674` added the gate's witnesses to SR-8.3.3.3.3-1 (+3 test-ref, +2 code-location, GAP 56
to 56). PB674 is `landed` with `closes_rows: []` and its reason. File I/O and SORT: this change touches neither a statement
nor an I-O status, so no exception-path test is added; the START gate's own exception paths (INVALID KEY on forward and
reverse operators, relative and indexed) are PB370's goldens.

**Self-review.** pr-test-analyzer lens: the positive covers six positions and three editions, the negative two; the drift test
fails on a new float token. silent-failure-hunter lens: the gate is skipped when `gateFormats` is false (a directive in an
omitted branch), the same posture as the hexadecimal and zero-length gates; `gateFormats` true everywhere a unit is compiled.
comment-analyzer lens: the construct description states the evidence and what would overturn it.

