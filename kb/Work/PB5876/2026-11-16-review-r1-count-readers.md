# Review round 1, 2026-11-16: the readers of a table's CURRENT count ask the same screen

**Finding R1-1 (wave 20262092 group A).** The Format-2 table SORT reads its OCCURS DEPENDING ON object through
`PlaceBuilder.CurrentOccurrenceCount` (`§2023:14.9.40.4 GR20`), outside the operand's place, so the screen of the landing
did not reach it: `SORT L-E ASCENDING` over `L-E OCCURS 1 TO 5 DEPENDING ON L-N`, `L-N` in a LINKAGE record that is no
USING operand, compiled at `--std 2023` (exit 0, `W-1=EDCBA`, nothing sorted).

**Sibling found by the sweep** (`grep -rn "CurrentOccurrenceCount(" src/WiseOwl.Cobol.Compiler`): a table(ALL) intrinsic
argument (`§2023:15.3`) ranges its levels by the same count. `DISPLAY FUNCTION MAX (L-E (ALL))` in the same program
compiled and ended the run unit with EC-ARGUMENT-FUNCTION. The other readers are not statement operands: SEARCH
(screened by the landing), `PlaceBuilder.WholeTable` (the SEARCH/SORT debugging place), `SendingValueTemp` (a compiler
temporary), the SORT slot-window inner levels (an addressability test) and `SortEmitter` (code generation).

**Fix.** `NameResolution.ScreenImplicitLinkageCounts` screens the data-name-1 of each `AllCount.Odo` count through
`ScreenImplicitLinkageObjects`; `SortBinder.SortBindTable` and `IntrinsicBinder`'s table(ALL) argument ask it. Both now
give COBOL2746 at 2002, 2014 and 2023.

**Goldens** (`conformance-suite/cases/13.7-Linkage-section/`): `pb5876-linkage-odo-object-table-sort` and
`pb5876-linkage-odo-object-table-all-argument` (refused at 2002, 2014, 2023), with controls `-using-operand`
(`W-1=CDEBA`, `MAX=C`: the first 3 occurrences only).

**Sites:**
- `src/WiseOwl.Cobol.Compiler/Binding/NameResolution.cs#ScreenImplicitLinkageCounts`
- `src/WiseOwl.Cobol.Compiler/Binding/Procedure/Verbs/SortBinder.cs#SortBindTable`
- `src/WiseOwl.Cobol.Compiler/Binding/Procedure/Verbs/IntrinsicBinder.cs#TryBindAllArgument`
