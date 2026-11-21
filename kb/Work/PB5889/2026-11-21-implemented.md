**Implemented (wave 20262131 group l, branch train85b-intarg), the ruling of 2026-11-21.** The integer-argument screen has three
outcomes (`IntrinsicArgumentRules.ClassifyTypeSix`; docs/INTRINSICS_DESIGN.md, the type-6 intake; register entry
`docs/conformance/3/D-INTEGER-ARGUMENT-SCREEN.md`):
- PROVEN: the polynomial proof as before (PB5886, PB5891); accepted.
- REFUTED: `Refutation` evaluates the exact value at points of the operands (fixed-point items, floating-point items in steps of 0.5;
  integer terms kept as polynomial atoms, refuted only when the value is a non-integer constant plus an integer part), with storage
  independence required of two or more operands; COBOL1627 names the value (`at N = 1 it is 1/3`). A value of a function is never a
  counterexample.
- UNDECIDED: accepted; `IntrinsicRenderer.UndecidedIntegerSite` renders `CobolIntrinsics.CheckedIntegerArgument*` (one per carrier)
  before the integer intake, naming the function, the argument and the value: EC-ARGUMENT-FUNCTION from 2002 (§2023:15.3 AR14), run-unit
  termination at 85 (§1985:p.B-46).

The probes of the adjudication (`E:/COBOL-coord/scratch/w20262131/pb5889/src/`): IDMAX prints 6 and IDMEAN 120 at 85 and 2023 (base:
COBOL1627 at both); MODTHIRD stays refused, "at N = 1 it is 1/3"; SQRTARG stays refused by the type bar. COBOL1627's citations are now
`§2023:15.3 AR6` and every schema row's `AR` designator (93 single and 22 composite clause strings, each component through
`cite.resolve`).

**Goldens.** `pb5889-identity-difference` was already retired by PB5891 (its program is admitted and carried by
`pb5891_polynomial_integer_arguments`); the residue is `pb5889_function_meaning_identities` (accepted at 85, 2014, 2023). Of the
refusals the screen no longer makes, four conforming forms are accepted with their values (`pb5886-numeric-function-terms`,
`-item-quotients`, `-power-shapes`, `pb5891-degree-over-cap`) and 23 are run-time-check cases from 2002; the new run-time cases are
`pb5889_run_time_check_85`, `pb5889_run_time_check` and `pb5889_run_time_check_unchecked`. Twelve refusals with an operand
counterexample stay refused.

**Leads.** kb/Work PB6019 (at 85 only the integrality check terminates; every other argument violation returns 0). The four PB5886
refusals whose old message valued a FUNCTION (`at FUNCTION ORD (A) = 1 it is 0.5`) are now checked at run time, because such a value is
not a value of the operands (`FUNCTION FACTORIAL (N + 2) / 2` would have been "refuted" at FACTORIAL = 1, yet is never odd); modelling a
function's range (ORD: 1 to the alphabet's size; MOD by a literal) would refuse them soundly and is left to a ruling.
