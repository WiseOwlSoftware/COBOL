## PB4507: a function-identifier is a data item to the intrinsic argument screens

**The defect.** `FUNCTION INTEGER-OF-FORMATTED-DATE("YYYY-MM-DD", FUNCTION UPPER-CASE(WS-D))` was refused with COBOL1627, "argument-2
shall be a DATA ITEM". The fuzz smoke batch of 2026-11-03 found it (kb/Work PB4506, adjudicator slice C1). Re-probed on this wave's
build: refused before, `+000145792` after, which is the integer date of 2000-03-01 (checked with Python's `datetime`).

**The rule.** ISO 15.48.3 3) asks argument-2 to be "a data item of the same type as argument-1", and 8.4.3.2.4 1) says "A
function-identifier references a temporary data item whose value is determined when the function is referenced at runtime". The
alphanumeric result of UPPER-CASE is therefore a data item of the right type. The rule excludes a literal, a figurative constant and
an expression. (All five citations were run through `scripts/spec/cite.py --check`.)

**The cause and the fix.** The data-item half of 15.48.3 r3 was one hand-written arm in `IntrinsicBinder.BindIntrinsicCore`,
`args[1] is not BoundFieldOperand`, which admitted a field and nothing else. It is now the `DataItemOnly` argument predicate on the
`INTEGER-OF-FORMATTED-DATE` row of `IntrinsicArgumentRules.Verified`, and `IntrinsicArgumentRules.IsDataItem` is the one place that
answers "is this operand a data item": a field operand (reference modification and a user function's result item included), a counter
register, an address-identifier and a nested or folded function-identifier are; a literal, a figurative constant, an ALL literal, a
boolean expression and an arithmetic expression are not. A violation stays a hard error under `--permissive`, as the arm's was.

**The sibling the sweep found.** The `DataItemOrLiteralOnly` predicate (FIND-STRING argument-3, 15.37.3 r3; BASECONVERT argument-2 and
argument-3, 15.12.3 r1) barred every nested function, including an INTEGER function whose temporary is an integer data item. It had
two negative goldens (`pb58-findstr-func`, `pb2079-baseconvert-function-base`) that pinned the over-rejection. It now bars the
arithmetic expression only; a nested numeric function that is not an integer function is still refused by the 15.3 type 6 integer
test (8.4.3.2.3 SR11). `FIND-STRING(X "ABC" FUNCTION INTEGER(1))` returns 4 and `BASECONVERT("1A" FUNCTION INTEGER(16) 10)` returns 26.
The other `is not BoundFieldOperand` sites in the compiler (`StatementValidation`, `MoveOverlap`, `ConditionRenderer`,
`InspectEmitter`, `EvaluateBinder`) ask identity or storage questions, not "is this a data item", and are unchanged.

**Goldens.** Accepted: `pb4507_iofd_function_identifier_argument_2` (2014), `pb4507_find_string_function_argument_3` and
`pb4507_baseconvert_function_bases` (2023), their values derived from 15.48.4, 15.37.4 r2 and 15.12.4. Refused:
`pb4507-iofd-figurative-argument-2`, `pb4507-findstr-numeric-function`, `pb2079-baseconvert-expression-base`. The two negatives above
were deleted. Rows AR-15.48.3-3, AR-15.37.3-3 and AR-15.12.3-1 gained witnesses and corrected prose (verdicts unchanged, GAP 1 to 1).
`docs/INTRINSICS_DESIGN.md` describes the two predicate kinds and the shared question.
