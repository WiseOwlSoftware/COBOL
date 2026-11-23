## COBOL1637 at --std 85 cites the reference-modification bounds rule, not the OCCURS rule

kb/Work PB6088. Compiled at `--std 85`, an index-name in a reference-modification bound drew COBOL1637 saying
§1985:VI.5.8.3 SR13 "makes both bounds arithmetic expressions" and naming "five contexts". `cite.py --check` shows the bounds rule is
§1985:IV.4.3.8.4.3 SR2 and that SR13 (OCCURS) lists no contexts; the five-context list is §2023:13.18.38.3 SR7 (and 2002's).

Fix: `IndexNameRule` (the one place these wordings are written) gains `ReferenceModificationBounds` and `ReferenceModificationContext`;
`SubscriptBinder.IndexNameInPositionError` calls them instead of writing a citation inline. The 85 arm claims no count. The 2023
text is unchanged. Test: `ArithmeticOperandClassTests.IndexNameInARefModBound_CitesTheBoundsRuleOfItsOwnEdition` (85 and 2023);
the existing `pb170-refmod-index-name` negative already refuses at every edition. Closes no inventory row.

Sibling registered, not fixed: five other COBOL1637 messages print `Contexts()` followed by "admits an index-name only as", which at
85 attributes a closed list to §1985:VI.5.8.3 SR13 (the root is `IndexNameRule.Contexts`, noted in the PB6088 note).
