## The VALUE literal screens: zero on a numeric-edited item and a plus sign on an unsigned one (PB3566, PB3567, PB3555)

Wave 1157 group D, three notes in `DataBinder`, two of them from the third fuzz night (`fuzz-20261022-163846`).

**PB3566, the literal zero as the VALUE of a numeric-edited item.** `PIC ZZ9.99 VALUE 0` compiled at `--std 2002` and
`2014` while `VALUE 10` drew COBOLNET0900. One predicate, `IsNonZeroNumericLiteral`, answered two different questions:
the SR6 edition screen ("is this a numeric literal, which is a COBOL-2023 permission", Annex E.3.3 item 43) and the
float-edited form arm ("is this a nonzero fixed-point literal", the real exemption in SR6's last sentence). Asked the
first question, "nonzero" read "numeric", so zero escaped. It is now `MagnitudeOfNumericLiteral` (NotNumeric, Zero,
NonZero) and each site asks its own: the edition screen `is not NotNumeric`, the form arm `is NonZero`. The old name is
gone. This overturns kb/Work PB921's "Complement, measured", which had read "the integer and decimal forms of the literal
zero may also be specified for either format" as an all-edition exemption; the sentence separates the fixed-point from
the floating-point formats inside the 2023 permission, and the only zero a numeric-edited item had before 2023 was the
figurative ZERO (Annex E.2 item 28), which stays accepted at every edition. A BLANK WHEN ZERO item is numeric-edited
(§13.18.8.4 GR2), so `PIC 9(3) BLANK WHEN ZERO VALUE 0` is gated too, and that is what the population scan found: the
`pb566_blank_when_zero_stored_value` golden (accept 85) lost its VALUE 0 leg (the 2023 witness pins it) and the 2002 and
2014 copies of `pb646_national_form_numeric` store their numeric-edited national item with a MOVE instead of a VALUE.
Every conformance case accepted below 2023 (1759 programs) and the NIST corpus (459 rows, 18 pending ones fail as before)
were compiled to find them; none else.

**PB3567, a plus sign on an unsigned item.** `PIC 9(4) VALUE +5` compiled at every edition. `ValidateNumericValue` tested
`neg && !signBearing`; the CA34 fix had scoped SR3 to negative literals as "the harmless idiom". §13.18.63.3 3) speaks of
a signed literal and §8.3.3.3.2 2) makes a literal that uses a sign a signed one, so the test is now
`signedLiteral = t[0] is '-' or '+'`. One funnel (`ScreenValueLiteral`) asks the rule for the item, table, level-88 and
report VALUE, so there is no second arm. INITIALIZE REPLACING and MOVE convert and are not this rule.

**PB3555** was fixed on main by the PB3338 branch (d79d98771: SR9 walks `CommitControlledFiles`). Re-probed on this
build: under `--permissive` a declined SEQUENTIAL operand leaves the status item at 10 after ROLLBACK. Closed with that
test as its witness; no code change.

Goldens: `pb3566-numeric-edited-value-zero-below-2023` (refused at 85/2002/2014), `pb3566_numeric_edited_value_zero_2023`,
`pb3566_numeric_edited_value_zero_figurative` (every edition), `pb3567-value-plus-literal-unsigned-subject` (refused at
all four), `pb3567_value_signed_literal_signed_subject` (every edition), `pb3567_value_signed_literal_numeric_edited_2023`;
`NumericEditedValueEditionGateDriftTests` gates the zero spellings in every format. SR-13.18.63.3-3 and -6 are
re-verdicted CONFORMS with restated statements; no row closed (GAP 74). No diagnostic code was used.

Self-review (test analyzer, silent-failure hunter, comment analyzer): the new expected values come from §13.18.40.5 rule 7
and Table 8 and §13.18.63.3 SR8 NOTE 2, the signed-subject golden reads values back through comparisons so it does not
pin an implementor-defined sign image, and two stale comments ("negative literal" in `ValidateNumericValue`'s remarks) were
corrected. Gate: GATE-VERDICT-PENDING.
