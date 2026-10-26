## PB3691 — a numeric, integer or index function-identifier is no integer operand of a relation with a character operand (D-FNUSE reaches §8.8.4.2.5)

**The question the owner closed.** PB3665 read §15.2 as giving numeric, integer and index functions no usage, so STRING SR1 and
INSPECT SR2 refuse them as "described … as usage display or national". §8.8.4.2.5 states the same condition a third time for
item 6 of §8.8.4.2.1: "The numeric integer operand shall be an integer literal or an integer numeric data item of usage display
or national." The relation checkpoint still admitted an INTEGER function there, by the A.1 item 92 reading that moves such a
function to an alphanumeric receiver in its literal text form. The owner decided on 2026-10-25, after the operator explained
that §8.8.4.2.5 needs the operand's digit count (a display item has one in its PICTURE, a function result has none the
standard states): "Refuse it per the spec".

**Re-probe (fresh Debug build, N PIC 9(4) VALUE 7).** `IF FUNCTION INTEGER(N) = "0007"` compiled and answered not equal,
where `IF N = "0007"` answers equal. The sweep found the rule wider than the note: the other operand order, `X = FUNCTION
ABS(N)` and `MAX(N 3)`, the abbreviated `X = "0007" OR FUNCTION INTEGER(N)`, EVALUATE subject and object, SEARCH WHEN, PERFORM
UNTIL and the national form all compiled. The folded `FUNCTION LENGTH(N) = "4"` compiled AND answered equal, because the fold
reaches the relation as a literal-shaped operand: one function, two answers by what the optimizer could compute. Already
refused: `FUNCTION NUMVAL(X) = X` (non-integer, COBOLNET2532) and an index function opposite a character operand
(`FUNCTION MAX(IX IX)`, COBOLNET2533, §8.8.4.2.13).

**Fix.** `RelationComparability.IsNumericIntegerOperand` asks the one reader STRING and INSPECT ask,
`IntrinsicArgumentRules.FunctionIsNotDisplayOrNational`, before any other arm. It reads a function-identifier in all three shapes
that reach a binder (call, lowered tree, folded literal), so every one of them is COBOLNET2532 in either operand order, with no
new code, and the one relation checkpoint carries it to IF, EVALUATE, SEARCH WHEN, PERFORM UNTIL and abbreviated relations. The
INTEGER-function arm is deleted. `SendingValueTemp.FunctionValuePic` was the owner's second question: it is no usage claim. Its
USAGE DISPLAY is the implementor's representation (§15.4.1) and sizes the item for `FUNCTION LENGTH` and IN-ARITHMETIC-RANGE,
the only two readers; its comment says so and a drift test pins that the relation table asks the one reader first and never
reads a returned-value description. An EVALUATE subject's Display temp cannot reopen the hole, since the checkpoint judges the
operand as written. A user-defined function is described by its RETURNING item (§8.4.3.2.4 GR1), so a `PIC 9(4)` result is
admitted and a COMP one refused, unchanged.

**Goldens.** Nine refusals `pb3691-*`, the controls `pb3691_relation_function_operands` (85 through 2023) and
`pb3691_relation_national_function_controls` (2002 on), and `RelationComparabilityTests` (24-kind pair matrix with an
alphanumeric and a national function kind; FI and FL no longer item-6 integer operands; every-surface and complement theories).
The one existing golden that asserted the retracted admission, `da2_function_as_text` (`TXT = FUNCTION MIN(3 -14 0)` answered
`CF=EQ`), lost that relation. A scan compiling every conformance, tests and samples program that names a FUNCTION (1,725) found no
other. Row `GR-8.8.4.2.1-6` is re-verdicted by batch with the admission retracted and seven witnesses added; no row closed.
D-FNUSE gained the relation-condition paragraph, D-RELCLASS retracts its INTEGER-function sentence, the conditions design doc
follows. GnuCOBOL 3.2 answers not equal for the function pair and accepts a COMP item, so it is no precedent either way.
