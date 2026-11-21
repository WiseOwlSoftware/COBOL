# PB6011, 2026-11-21: the missing-period half lands; the parser knows each token's reference-format area (wave 20262131 group n)

**Landed.** `01  A PIC X VALUE "A"` then `01  B PIC X.` (`E:/COBOL-coord/scratch/w20262131c/p/noperiod.cob`) drew at --std 85
COBOL3735 "'01' begins in area A", COBOL1639 twice, COBOL1585 and COBOL2423 (base 26e6da8f73, re-probed through run_probe.py). It now
draws one diagnostic, `noperiod.cob(6,8): error COBOL0307: cannot parse construct near '01'. A period may be missing at the end of
the previous sentence.` The same for a condition-name's `VALUE 1 THRU 5` and a 77 item's `VALUE 3` before an area-A level-number
(one COBOL0307 at each), and for any level-number written in area A (§1985:IV.7.4 "Each new data description entry may begin any
number of positions to the right of margin A").

**The mechanism.** The redesign the computer-name addendum named: the token's area reaches the parse. `FixedFormAreas.LineOpeningsInAreaA`
(sharing `LastLineOf`, the one reading of "begins its line", with `AreaPlacementPass`) lists the tokens that begin their fixed-form
line in area A, only at an edition without the `fixed-form-text-in-area-a-2002` row; `Frontend.NewParser` hands it to
`CobolParserCoreBase.AreaAOpenings`. The grammar asks `{!nextEntryOpensHere()}?` at the left edge of every INTEGERLIT-led operand
alternative (`integerLiteral`, `signedIntegerLiteral`, `numericLiteralCore`, `residueInteger`; `AreaAOpeningDriftTests` holds the
inventory), so the entry ends at the level-number (§1985:IV.7.4 "The last clause is always terminated by a separator period";
§1985:IV.7.3.3 "Successive sentences or entries either begin in area B"). The recovery reads the same fact:
`OffendingPosition.OpensAreaA` / `PeriodIsMissingBeforeAnAreaAOpening` feed hint 14 (whose expected-set reading cannot see the
period, the predicate-blind set holding the refused integer) and `ClosedFormatResidue`'s "begins what follows" guess.

**At 2002, 2014 and 2023** there are no areas (§2023:6.3.1, cite.py OK) and free form never had them: the set is empty and the parse,
its diagnostics and the cascades are unchanged (probed at --std 2023). No layout fact settles the reading there.

**Tests.** Conformance `pb6011-missing-period-before-level-01-85`, `pb6011-missing-period-condition-range-85` (refused at 85, RED on
the base), `pb6011-value-list-continued-in-area-b-85` (accepted control: a VALUE list continued in area B is one entry);
`AreaPlacementPassTests.AMissingPeriodBeforeAnAreaALevelNumber_IsReportedOnce_WhereItIsMissing` (four shapes, RED on the base),
`ALiteralListContinuedInAreaB_IsOneEntry_At85`; `AreaAOpeningDriftTests` (RED on the base).

**Not in this change: the procedure division's twin**, filed as PB6018.
