## Entry 2580 — 2026-10-25 15:54 PDT — PB508 + PB498: BLANK WHEN ZERO's usage pair is one predicate, and three GR6 citations name the rule they quote

**PB508 (re-probe).** The note's finding (the GR2 promotion hard-codes usage display) had already been fixed by PB646, which
made national-form numerics real: `PIC 9(5) USAGE NATIONAL BLANK WHEN ZERO` blanks on a stored zero like its display twin,
shows 00012 after `MOVE 12`, de-edits into a numeric receiver, and `ADD 1 TO` it is COBOLNET1675 (14.9.2.3 SR2: the resultant
is numeric-edited, which is only true if 13.18.8.4 GR2 defined it so). No golden pinned any of it, and the usage pair was
still spelled inline at the promotion, at SR2's own screen (`DataBinder.BlankWhenZeroViolation`) and in a private copy in
`ClassConditionModel`, which is how the promotion and the screen had once named different usages.

**PB508 (fix).** `UsageFamilies.IsDisplayOrNational(Usage)` (PicInfo.cs, beside the float and binary families) is the one
spelling of "usage display or usage national" (13.18.8.3 SR2). PictureAnalyzer's GR2 promotion and its SR28 currency-symbol
screen, the SR2 screen, `PicInfo.IsCharacterFormNumeric` and `SignKindFor` ask it; `ClassConditionModel`'s private
`DisplayOrNational` is deleted. A stale `PicInfo.Usage.National` summary that still called the national-form numeric legs
"recognized-but-staged (0899)" and PictureAnalyzer's comment saying SR2's screen was missing are corrected. About fifteen other
inline `Usage.Display or Usage.National` spellings (ControlFlowBinder, IntrinsicBinder, MoveBinder, StringUnstringBinder,
ReferenceResolver, RelationComparability, DataBinder.SignClause/Reports) each state their own rule and are correct; moving
them onto the predicate is a mechanical cleanup in other groups' files and is reported as a lead rather than done here.

**PB508 (pins).** `BlankWhenZeroUsagePairDriftTests` measures promotion against the predicate over every `Usage` member
(legal combinations only: a usage the picture cannot carry is corrected to display after its own diagnostic) and checks that
the promotion line and the screen method ask the predicate and do not spell `Usage.Display`. Goldens:
`13.18.8-BLANK-WHEN-ZERO/pb508_blank_when_zero_national` (accepted 2002 to 2023, one `.out`) and
`pb508_blank_when_zero_national_resultant` (COBOLNET1675 at 2002, 2014, 2023). Both were mutation-checked on this build: with the
promotion reverted to `usage is Usage.Display` the first prints `N=[00000]` and the second compiles. Below 2002 the national
item is itself refused by `pb646-national-form-numeric-at-85`. Row GR-13.18.8.4-2 PARTIAL to CONFORMS.

**PB498 (re-probe).** Three of the four findings are gone: COBOLNET1658 refuses only the FOR form and cites SR12's FOR-phrase
rules (PB866), `PIC 9(32)` is COBOLNET0801 citing 13.18.40.3 SR14 and naming 8.3.3.3.2 for the literal leg (PB529), and the
COBOLNET0899 national-through-range staging was deleted (PB761). The survivor is a citation defect of the inherited kind:
`RuntimeApi.EditCompose`, the numeric-edited VALUE arm of `GroupImageCodec` and
`DecimalPointDifferentialTests.NumericValue_OnEditedItem_ConvertsPerMoveRules_SpecPinned` cite "13.18.63 GR6" for the
conversion of a numeric VALUE literal on a numeric-edited item, which is 13.18.63.3 SR6; 13.18.63.4 GR6 is the OCCURS
DEPENDING initialization rule. All three now cite 13.18.63.3 SR6 (`cite.py --check` OK for both clauses), and the two real GR6
readers (`OdoModel`, `OdoDifferentialTests`) now say 13.18.63.4 so a grep tells the rules apart. The sweep over
`13\.18\.63 (SR|GR)` found no other mismatch. SR-13.18.40.3-12 PARTIAL to CONFORMS (its float-edited leg was PB866's; the
other three claimed rows were already CONFORMS).

**Self-review.** Architecture: one predicate for one sentence, beside its siblings. Silent failure: the unit test fails
loudly if the measurement reaches no usage in the pair. Comment accuracy: the two stale comments above fixed. Test coverage:
positive, refusal and the structural pin each fail under the reverted predicate or a re-spelled one.

