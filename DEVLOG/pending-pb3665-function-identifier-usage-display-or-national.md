## A numeric, integer, index or boolean function is no STRING or INSPECT character operand, and a folded FUNCTION LENGTH is a function, not a literal (PB3665)

The fourth fuzz night found `STRING FUNCTION INTEGER(N) DELIMITED BY SIZE INTO R` compiling and storing 7, while `STRING FUNCTION
LENGTH(G) ...` was refused as "a numeric literal". The owner decided (2026-10-24, after the operator laid out the ISO text and GnuCOBOL's
behavior) to refuse both. 14.9.43.3 SR1 requires every identifier but the POINTER to be "described implicitly or explicitly as usage
display or national"; 8.4.3.2.1 makes a function-identifier the data item the function results in, and 15.2 describes that item by type:
alphanumeric functions have an implicit usage display (1), boolean functions usage bit (2), national functions usage national (3), and
the numeric, integer and index functions (4-6) are given no usage. GnuCOBOL 3.2 checks a usage only for a data item and stores the raw
four bytes of `cob_intr_integer`'s binary field, so it is no precedent; docs/CONFORMANCE.md D-FNUSE records the choice.

**What the re-probe showed (fresh build).** Both notes reproduced. The sweep found the rule wider: every numeric function (`ORD`, `NUMVAL`,
`PI`) and the boolean `BOOLEAN-OF-INTEGER` (usage bit; it sent "0111") compiled as STRING senders and delimiters, and INSPECT SR2 (the
same sentence for identifier-3 .. identifier-n) admitted them at TALLYING FOR, REPLACING BY, CONVERTING and BEFORE/AFTER. UNSTRING already
refused the numeric and boolean functions through SR2's category; only its folded-LENGTH delimiter was worded as a literal.

**Fix shape.** One table and one reader. `IntrinsicSig.ImplicitUsageOf(category)` states 15.2 1)-3) once; the `StaticUsageOf` computed arm
and `ConditionBinder`'s class-condition usage, which each carried a private copy of the table, now read it.
`IntrinsicArgumentRules.FunctionIsNotDisplayOrNational` classifies a function-identifier in the three shapes it reaches a verb binder (the
runtime call, the lowered tree of a variable-length group's LENGTH, the literal a fixed item's LENGTH folds to). STRING's `Sr1Offence` asks
it before its numeric-literal arm (a real numeric literal keeps the literal wording) and `StatementValidation.CheckInspectFunctionUsage`
asks it for INSPECT. No new diagnostic code: COBOLNET1626 and COBOLNET0847; the latter, which INSPECT's three rules reported as a raw literal,
gets its catalog descriptor `InspectOperandRule` (semgrep `raw-diagnostic-code-literal` 227 -> 225, baseline not re-locked). The UNSTRING delimiter now names the folded function as
category numeric under SR2. `DisplayOrNationalUsagePredicateDriftTests` pins the shared reader, the arm order and the single table.

**Goldens.** Nine refusals `pb3665-*` (STRING integer, folded LENGTH, numeric and boolean senders, integer and LENGTH delimiters; INSPECT
integer and LENGTH operands; UNSTRING LENGTH delimiter, pinned by its message) and two controls (an alphanumeric function as STRING sender
and delimiter, and at INSPECT's three identifier positions). Two existing positives sent a numeric function to STRING and are corrected:
`da4_function_sending_operand` (ORD -> LOWER-CASE) and `pb26_ec_argument_function_every_statement` (LOG10 -> CHAR(0), which raises the same
EC-ARGUMENT-FUNCTION). A sweep compiling the 270 conformance, test and sample programs that mention both FUNCTION and one of the three verbs
found exactly those two.

**Rows.** SR-14.9.43.3-1, SR-14.9.22.3-2 and SR-14.9.48.3-2 gained witnesses (batch w20261102a-pb3665); no row opened or closed
(`closes_rows: []` with a reason). **Not changed:** the 8.8.4.2.5 relation (an INTEGER function against an alphanumeric operand is admitted
as the 15.4 temporary, DOC-A.1-92) is another rule's decision; it is a lead in the report.
