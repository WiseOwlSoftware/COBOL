## The 1985 operator spacing rules are enforced for unary, relational and logical operators (PB5989)

The 1985 prose-requirement campaign (kb/Work PB5924) found three spacing sentences the compiler did not enforce at `--std 85`:
§1985:VI.6.2.2 (every arithmetic operator "must be preceded by a space and followed by a space", with no exception for a unary
operator beside a parenthesis), §1985:VI.6.3.1.1 ("A space must precede and follow each reserved word comprising the relational
operator") and §1985:VI.6.3.2 ("The logical operators must be preceded by a space and followed by a space"). Re-probed on this
tree before the fix, each compiled and ran at exit 0: `COMPUTE N = -(M) + 9`, `IF N=M`, `IF N GREATER(M)` and
`IF (A = 1)AND(B = 2)` (which printed BOTH).

The mechanism is one: COBOL2993's spacing verdict was asked of the arithmetic arm only, and its unary arm admitted a left
parenthesis at every edition. The fix stays where COBOL2993 is decided, `SeparatorRule`. The unary exception is now keyed by
`EditionInfo.UnaryOperatorMayTouchParenthesis` (2002 and later, §2002:8.7.1). `SeparatorRule.ConditionOperatorViolations` walks the
parse tree at the editions where `EditionInfo.ConditionOperatorsNeedSeparatorSpaces` holds (COBOL-85) and judges every token of a
`comparisonOperator` and every AND, OR and NOT that is a direct child of a condition tier through the same `OperatorSpacingMessage`
verdict, with no parenthesis standing in for the space. The tree is needed because the OR of `GREATER THAN OR EQUAL TO` is a word of
the operator while the OR of `A = 1 OR B = 2` is a connective. The new code is COBOL7672 (`separator-condition-operator-spacing`).
No later edition states the relational or logical sentence (grepped §2002, §2014, §2023), so they accept the same text; a golden
pins that.

Goldens: 12 programs in `conformance-suite/cases/_unassigned/` (`rules: []`, because the catalog has no rows for unnumbered prose,
PB5924), 8 refusals at 85 with their `wiseowl/diagnostics` files (unary after and before a parenthesis, relation character,
relational word before a parenthesis, last word of a multi-word operator, AND, OR, NOT) and 3 spaced programs accepted at every edition, plus one
program accepted at 2002 and later in the shapes the 85 refusals pin. Unit tests in `SeparatorRuleTests` cover the unary arm at all four editions.
No inventory row was open or closed. The design doc DESIGN-frontend-grammar.md carries the 1985 paragraph.
