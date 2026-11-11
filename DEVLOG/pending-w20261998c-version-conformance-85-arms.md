## The 1985 arms of nine version-conformance mechanisms: INSPECT BEFORE/AFTER, the class condition, PERFORM BY, and six 2002 forms gated below 2002

Wave 20261998 group C (kb/Work PB5120, PB5122, PB5177, PB5194, PB5179, PB5172, PB5159, PB5116, PB5170). Each note was
re-probed on the branch's build before its fix, and each reproduced as filed.

**PB5120, INSPECT (wrong answer).** At `--std 85` an operand with both BEFORE and AFTER looked for the BEFORE delimiter
only from the AFTER boundary, so AYAAXAA with AFTER "X" BEFORE "Y" tallied 2. §1985:VI.6.18.4 GR8 b, the 2002 text and
the 2014 FCD measure the BEFORE portion from the leftmost position, and the operand participates in the intersection
with the AFTER portion (0 here); §2023:14.9.22.4 GR9 b reworded it with BACKWARD. A behaviour row,
`DialectBehavior.InspectBeforeFromFirstEligiblePosition` (2023, VCR row 7.58), sets `BoundInspect.BeforeFromLeftmost`
below 2023 and `CobolInspect.Region` searches from position 0; TALLYING, REPLACING and CONVERTING share `Region`, so all
three changed. Witness `14.9.22-INSPECT/pb5120_before_after_intersection` (`.out` and `.2023.out`).

**PB5122, the class condition (rejects legal source and under-rejects).** The operand screen applied the 2023 rule at
85: a numeric edited item was refused for ALPHABETIC and class-name tests, and the 1985 NUMERIC bars were not asked.
Each alternative of `ClassConditionModel` now carries its §1985:VI.6.3.1.2 rule set, chosen by
`ClassAlternative.RulesAt`, with three codes: COBOL6916 (ALPHABETIC, class-name over a numeric item), COBOL6917 (NUMERIC
over an alphabetic item or a group holding a signed item) and COBOL6918 (usage DISPLAY, or an alphanumeric function).

**PB5177, PERFORM VARYING BY index-name.** §2023:14.9.28.2 and §1985:VI.6.21.2 both print BY { identifier | literal },
so an index-name in the BY slot is now COBOL2119 at every edition, through one slot-by-kind table (`Admits`).

**Six forms gated below 2002**, each a construct row and one visit in `VersionConformancePass`: a reference-modified
function-identifier as the UNSTRING sender (PB5194, the existing construct now asked of every sender alternative), a
partial expression as an EVALUATE object (PB5179, 2002 F.2 item 48), the CLASS clause's IN phrase (PB5172), REWRITE's
word RECORD (PB5159), the TYPE clause's omitted control operand, ON/FOR word and OR PAGE (PB5116), and a configuration
paragraph's lone second period (PB5170, read through `ParagraphPeriods.LonePeriod`). The migrated 85 case
`a413_declined_phrase_positive_control` wrote `REWRITE F-REC RECORD`, which the 1985 format does not print; it now
writes the 1985 spelling.

Every fix has a positive at its introducing edition and a refusal below it. PB5171 (the SPECIAL-NAMES clause sequence and
once-only clauses at 85) and PB5198 (the RERUN facility) are left for the next link of the chain: the first needs the
clause-list table to rank repeatable clauses, the second a design decision.

### Gate 000003 (RED) and its roots

The first full gate of the branch ran 40,748 Unit and 24,049 Conformance cases and found eight reds, all mechanical consequences of the fixes. The runtime ABI pin moved because the INSPECT entry points gained the `beforeFromLeftmost` parameter (a changed shipped signature): `abi_pin.py bump --runtime-major` took the runtime to major 26 and measured its hashes. The documented-facts file gained COBOL6916 to COBOL6918. `ConditionBinder` now asks `Place.DenotedItem` instead of a hand-written reference-modification test (PlaceDenotedItemDriftTests). Three older goldens (pb1090, pb1401_computed_numeric_numeric, pb230_class_numeric_image) claimed `accept: [85]` for constructs the 1985 standard refuses (a second period after SPECIAL-NAMES; §1985:VI.6.3.1.2 bars NUMERIC over a non-DISPLAY item and over a numeric function), so they now start at 2002, the edition that introduced them.

### Review R1-1: a level-66 alias escaped the 1985 NUMERIC bar

The branch review found that `IF GS-ALIAS IS NUMERIC`, where `66 GS-ALIAS RENAMES GS` and GS holds `PIC S99`, compiled at `--std 85` while `IF GS IS NUMERIC` drew COBOL6917. The bar read the operand's description from `Place.DenotedItem`, the identity half, and the alias's own entry has no subordinates. A description is an attributes question: §1985:VI.5.11.4 GR2 gives a no-THROUGH alias "all of the data attributes of data-name-2", and GR1 makes a THROUGH alias "a group item which includes all elementary items starting with data-name-2", which failed the same way. The bar now reads `MoveTable16.OperandItem` (the attributes half, null for a reference-modified slice), asks `ItemCategory.IsGroupItem`, and walks the new `DataItem.SubordinatesOf`. That one reader answers the members of a group item for both alias forms (data-name-2's descendants, or the THROUGH alias's `IncludedElementaryItems`). The sweep moved four more binder screens off the identity half or off `IsGroup` over an alias onto the same readers: the INVOKE ACTIVE-CLASS subordinate screen (OoBinder), the SEND/RECEIVE message-area walk (MessagingBinder), `RelationPair.GroupItemOf`, and the relation's variable-length pair check. StatementValidation's private copy of the operand-item reader is deleted. §2023:13.18.45.3 SR8 keeps object, pointer, strongly-typed and variable-length items out of a RENAMES range, so those four could not misfire on a conforming program today; they now ask the question the rule asks. Witnesses: `pb5122-numeric-signed-group-alias-at-85`, `pb5122-numeric-signed-group-thru-alias-at-85` (COBOL6917 at 85) and `pb5122_numeric_alias_operands_2002` (both aliases tested as content from 2002).
