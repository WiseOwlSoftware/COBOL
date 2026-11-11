## PB5130, PB5161, PB5162, PB5208: the 1989 intrinsic argument rules get their edition arms, and the type-6 screen a power arm

Four intrinsic-argument defects found by the 1985 witness-drafting, code-read and refuter passes, fixed as one group (wave
20262001, group A). Each was re-probed on e3c9e8551a first, and each reproduced.

**PB5130 (wrong answer, silent).** At `--std 85` FUNCTION INTEGER-OF-DATE (100000101) returned 0. The 1989 module bounds the
year below only (§1989:A.2.16.3 AR1 a. "It must be an integer greater than 1600"; the same for INTEGER-OF-DAY, DATE-OF-INTEGER
and DAY-OF-INTEGER), while the 2002 text adds the 9999 ceiling (§2023:15.5.2, §2023:15.46.3 AR1 a). It is a behaviour change,
so it is `DialectBehavior.IntegerDateFormBounded` (2002, VCR row 7.58), read by the binder into
`BoundIntrinsicCall.IntegerDateFormUnbounded` and rendered as `unbounded: true`. The four `CobolDate` bodies now count days by
the calendar's own arithmetic (400-year cycles of 146,097 days from 1601-01-01), because System.DateTime ends at 9999. A
sibling on the way: DISPLAY of DATE-OF-INTEGER's value used the fixed eight-digit temporary of DOC-A.1-92, which would have
dropped the leading digit of 100000101, so `FunctionValueField.OfDigits` keeps the value's digits when it has more. At 85 the
repro prints 00000001, 03067671, 03067672, 03068037.

**PB5161 (rejects legal source).** At 85 NUMVAL-C ("Fr.12.50" "Fr.") was refused (COBOL1627) and a data item "Fr." returned 0.
The content bans on argument-2 are §2023:15.68.3 AR2's, present in the 2002 text; the 1989 module asks only the class
(§1989:A.2.30.3 AR4). The binder and the runtime each held a copy of the rule. There is one now,
`CobolIntrinsics.CurrencyStringViolatesContentRules`, asked where `DialectBehavior.NumvalCCurrencyContentRules` (2002, VCR row
7.59) is active; below it the binder sets `BoundIntrinsicCall.CurrencyContentUnrestricted` (`contentUnrestricted: true`).

**PB5162 (rejects legal source).** At 85 ORD of a one-character PIC Z item was refused by the category predicate of
§2023:15.70.3 AR1; §1989:A.2.31.3 AR1 says class alphanumeric, which holds the edited categories (§1985:IV.4.3.3). An
`ArgPredicate` now carries `Since`, the first edition whose rule states it, and ORD's category predicate starts at 2002. A new
drift test holds every 1985 function's category row to its 1989 sentence.

**PB5208 (under-rejects).** FUNCTION MOD (N ** -1, 7), N ** 0.5 and N ** M with M signed passed the §2023:15.3 type 6 screen
and ran on a truncated argument, while N / 2 is refused. `NotAlwaysIntegral` gained a root power arm, `PowerWitness`, sound by
a witness valuation (the base holding 3 × 10^(−s)); a literal base and an expression base still fail open, as documented.

**Goldens.** `pb5130_integer_date_year_10000_85` and `pb5130_integer_date_form_bounded`; `pb5161_numval_c_currency_content_85`
and the refusal `pb5161-numval-c-currency-content`; `pb5162_ord_numeric_edited_85` (the refusal
`pb658-ord-numeric-edited-argument` now refuses from 2002); refusals `pb5208-power-negative-exponent`,
`-fractional-exponent`, `-signed-exponent` and the complement `pb5208_power_integral_exponents`. The first full gate found
two older goldens that wrote `FUNCTION MOD (A ** X, B)` over a SIGNED exponent item X (`pb65_power_receiver`,
`pb69_native_power_past_the_carrier`): by §2023:15.3 type 6 that argument is not always an integer, so the new arm refuses
it, and the goldens now declare X (and Y) unsigned, which keeps the data-item-exponent leg they exist for. The 1985
inventory store re-verdicts eight rows to CONFORMS (1989-AR-A.2.12.3-1, A.2.13.3-1, A.2.16.3-1, A.2.17.3-1, A.2.30.3-1,
A.2.30.3-4, A.2.31.3-1, A.2.2-4; its GAP 478 → 470), and AR-15.68.3-2's code-location moves from the deleted
`InvalidCurrency` to `CurrencyStringViolatesContentRules` (the 2023 rows stay CONFORMS). No diagnostic code was needed;
COBOL6970-COBOL6972 are returned. Design: `docs/INTRINSICS_DESIGN.md` gains the section on edition arms of argument rules
and the power arm of the type-6 screen. Left for adjudication: whether the 1989 module trims NUMVAL-C argument-2's edge
spaces (the 2002 text says they are ignored; the scan still trims at 85).

**Gate.** `=== BUILD-LOCAL GATE: GREEN — Conformance 24,025/24,025 ran · Unit 40,757/40,757 ran · Characterization up to
date ... (run 000003-20261111T220507Z) ===` at fdce0fbfb2, after two reds (an inert-lane finding: the first drift test read
the 1985 Markdown, which no compiler test may, PB4453; then the two signed-exponent goldens and AR-15.68.3-2's stale site).
