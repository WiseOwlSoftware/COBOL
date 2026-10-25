## Entry 2508 — 2026-10-24 19:48 PDT — A numeric function value sent to a group stores its digits, in a MOVE, a CALL and an INVOKE (PB3613); A numeric, integer, index or boolean function is no STRING or INSPECT character operand, and a folded FUNCTION LENGTH is a function, not a literal (PB3665)

### A numeric function value sent to a group stores its digits, in a MOVE, a CALL and an INVOKE (PB3613)

PB3570 decided that a numeric literal sent to a group stores its digits only, and left the numeric FUNCTION value as a lead:
`MOVE FUNCTION MIN(N 3) TO G` (N = -15) stored `-15` and `FUNCTION MIN(M 3)` (M = -1.5) stored `-1.5`.

**What the re-probe showed.** The note reproduced, and the same value was wrong on every channel that sends into a plain group. A
function the compiler folds to a literal (`FUNCTION MIN(-15 3)`) kept `-15`, because PB3570 exempted it on purpose. `CALL ... AS NESTED
USING BY CONTENT FUNCTION MIN(N 3)` into a plain-group formal was refused (COBOLNET1688, "an arithmetic expression has no character
image"): `ActivationDescriptions.OfValue` gave a numeric function the arithmetic expression's shape, though a function-identifier is a MOVE
sender (14.9.25.4 GR1) and 14.8.2.2 rule 2 makes a BY CONTENT group argument conform "the same as for a MOVE statement". A Format-1 CALL
crossed the value cell's 38-digit zoned image (`000000` in a six-position formal), and an INVOKE crossed the sending-value temporary's own
signed text (`-15`, `-1.500`).

**The determination.** A numeric or integer function has "an operational sign" and no usage (15.2 4)-5), and 15.4.1 leaves the returned
value's "characteristics and representation" to the implementor under native arithmetic, so GR4's "no conversion of data from one form of
internal representation to another" has no representation of it to preserve, exactly as for a literal. PB3570's owner determination (the
digits of the value, no sign, no decimal point) therefore covers the value too. GnuCOBOL sends the function's own internal field instead
(`1u` for MIN over an S99, a 4-byte binary for INTEGER, `15` for ABS, measured with cobc 3.2.0), which is no rule for a value the standard
gives no representation, so the value follows the literal. D-B4 in `docs/CONFORMANCE.md` now says so.

**The fix, one digits rule.** `CobolNum.GroupDigits` (runtime) is the one reader of "digits only, significand digits for a float": the
compiler calls it on a literal's canonical text, the emitted code on a function value's run-time text, so a folded call and the same call
evaluated at run time send the same characters, and `OperandText.LiteralDigits` is gone. `OperandText.NonElementaryMoveSender` takes
every numeric literal (the `FunctionValue: false` exemption is deleted) and a numeric computed operand. The CALL lane describes a numeric
function as a MOVE sender (`ActivationShape.Literal`, the arithmetic expression keeps `Expression` and stays refused into a group), sends
it through the one sender into a known plain-group formal, and `CobolArgAdapt.WithValueSending` states the digits as the argument's
`StorageImage` for a formal the activating element cannot see. The INVOKE binder keeps a numeric function on the expression arm for a
plain-group formal and the group arm of `OoEmitter` sends it.

**Goldens.** `pb3613_numeric_function_group_move` and `..._comma` (MOVE; folded and evaluated calls, truncation, leading zeros,
DECIMAL-POINT IS COMMA), `pb3613_numeric_function_group_formal` (CALL AS NESTED, Format-1 CALL, INVOKE) and its negative
`..._below_2002`. `GroupMoveSenderChannelDriftTests` pins both arms and the absence of a second digit filter.

**Rows.** No inventory row claimed or closed: the landing adds witnesses to rules already CONFORMS (`closes_rows: []` with its reason in
`kb/Work/PB3613.md`).

**Branch review, round 1.** One blocking finding, a sibling the sweep missed: FUNCTION LENGTH of a variable-length group is lowered to a
`BoundBinary` sum, indistinguishable from an arithmetic expression, so `CALL ... AS NESTED USING BY CONTENT FUNCTION LENGTH(VG)` into a plain-group
formal was still refused (COBOLNET1688) while MOVE and a Format-1 CALL stored `5`. The binder now says which it is once
(`BoundComputedOperand.FunctionValue`, set by `IntrinsicBinder.OperandOf(expr, functionValue)`, whose parameter is required so a new position
must decide); `IsNumericFunctionValue` is the one reader for the activation description, the CALL arm, the INVOKE guard and emitter
(`BoundInvokeArg.ContentFunctionValue`) and the sending codec, and `DataItem.IsPlainGroup` is the one "plain group" test (the INVOKE binder guard
wrote the predicate inline beside `CallEmitter.IsPlainGroupFormal`; that method is deleted and five other inline copies now read the property).
`pb3613_numeric_function_group_formal` gained the LENGTH(VG) case on the three lanes; `pb3613_arithmetic_expression_group_formal_refused`
pins that a real arithmetic expression stays refused. `GroupMoveSenderChannelDriftTests` pins both single sources. The summary of
`NonElementaryMoveSender` now says a numeric literal or function value sends digits only.

**Self-review.** The sibling sweep found the arithmetic-expression/function ambiguity above. Left open, as a lead: a STANDARD-DECIMAL function
(COMP-2 argument) is cut to the receiver-less 6 fraction digits in a Format-1 CALL, so its digits there differ from AS NESTED and MOVE.

**Gate.** The first gate of the review fix read RED on one drift test whose guard string named the deleted `CallEmitter.IsPlainGroupFormal`
(`Gr4GroupDecisionDriftTests`, now guarding `a.Formal.IsPlainGroup`); the re-gate, run `000002-20261025T013211Z`:
`=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN — leg 2 left to CI's whole-population run — Conformance 8,367/18,407 · Unit 36,832/38,153 · Characterization 36/36 cases ran (skipped 0) in 1 of 2 leg(s) · Linux half (WSL clone of HEAD) GREEN ===`;
`scripts/semgrep/verify.py` PASS (no rule up).

### A numeric, integer, index or boolean function is no STRING or INSPECT character operand, and a folded FUNCTION LENGTH is a function, not a literal (PB3665)

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

**For the next implementer.** `IntrinsicSig.ImplicitUsageOf` (IntrinsicCatalog.cs) is the one 15.2 usage-by-type table: ask it, never respell it.
`IntrinsicArgumentRules.FunctionIsNotDisplayOrNational` / `FunctionIdentifierCategory` classify a function-identifier in its three shapes
(`BoundComputedOperand` over a `BoundIntrinsicCall`, `BoundComputedOperand.FunctionValue`, `BoundNumericLiteral.FunctionValue`): a rule
that reads `BoundNumericLiteral` first sees the folded LENGTH as a literal. `StringUnstringBinder.Sr1Offence` and
`StatementValidation.CheckInspectFunctionUsage` are the two callers. The trap: `ConditionBinder` and `StaticUsageOf` each had their own
copy of the table. A conformance program that sends a numeric function to STRING or INSPECT is illegal source (sweep: compile every
`.cob` mentioning FUNCTION and a verb, with the header's `source-format` and `accept`).

**Self-review.** Architecture: one table, one reader, no C# text in a bound node, the catalog descriptor registered. Full code: the rule is
15.2 by type, three shapes, both verbs; a user FUNCTION-ID result is a data item and keeps its own usage. Performance: nothing on a hot
path (bind time, one switch). Duplication: two private copies of the usage table folded into the one (ConditionBinder, StaticUsageOf).
Found and fixed on the way: the UNSTRING delimiter wording, two goldens that sent a numeric function to STRING, a raw `COBOLNET0847`
literal at three INSPECT sites.

**Review round 1.** The reviewer found the sibling arm of the rule left open: INSPECT identifier-1 as a function-identifier (Format 1, TALLYING
only) went straight to `BindPhrases` under a comment that called 14.9.22.3 SR1 "satisfied by construction", so `INSPECT FUNCTION INTEGER(N)
TALLYING T FOR ALL "7"` compiled and tallied the value's digits while the same branch refused the operand form. `InspectBinder.BindFunctionTarget`
now asks `StatementValidation.CheckInspectIdentifier1FunctionUsage`, the SR1 wording of the same `FunctionIsNotDisplayOrNational` reader
(COBOLNET1626, the code of INSPECT identifier-1's other usage refusal), and the comment is corrected. The reviewer's lead (R1-2) sat in the same
file and is fixed too: an identifier-3 .. identifier-n written as a keyword-omitted intrinsic (`REPOSITORY. FUNCTION ALL INTRINSIC.`) never
asked `KeywordOmittedFunction`, so `INSPECT S TALLYING T FOR ALL UPPER-CASE("b")` was refused as an undefined data name (COBOLNET1639); the
operand arm now asks it first (it yields to a declared data item) and reaches the SR2 screen. Goldens: `pb3665-inspect-identifier1-{integer,length}-function`
(refused at every edition, COBOLNET1626), `pb3665-inspect-omitted-keyword-integer-function-operand` (refused 2002 and later, COBOLNET0847; below 2002
the REPOSITORY FUNCTION entry is itself refused, COBOLNET0900), and the control `pb3665_inspect_omitted_keyword_function_controls` (M1 = 3, M2 = 1,
M3 = abxcb, computed from 15.2 1)); `DisplayOrNationalUsagePredicateDriftTests` pins the two new calls. SR-14.9.22.3-1 gained its witnesses.

**Gate.** After review round 1, run `TestResults/build-local/000001-20261025T023757Z` on head 5c3427ed1: `=== BUILD-LOCAL GATE: LEG 1 ONLY
(batched gating, PB2515): GREEN — ... Conformance 8,342/18,504 · Unit 36,970/38,194 · Characterization 36/36 cases ran (skipped 0) in 1 of 2
leg(s) · Linux half (WSL clone of HEAD) GREEN`. All 58 `pb3665` Conformance results passed (leg-1-Conformance.trx) and the five
`DisplayOrNationalUsagePredicateDriftTests`. The first implementer gate (run 000001-20261025T021404Z) was GREEN on 44 `pb3665` results.

