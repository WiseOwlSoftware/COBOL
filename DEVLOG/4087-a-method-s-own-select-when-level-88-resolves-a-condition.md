## Entry 4087 — 2026-11-27 21:21 PST — A method's own SELECT WHEN level-88 resolves, a condition-name's ODO object is screened, and a note's premise fails

Three binder sites answered from too narrow a view (wave 20262177, group J: kb/Work PB6276, PB6341, PB6303).

**PB6276, legal source refused.** `SymbolTable.SelectWhenConditionsOf` read the element's condition-name table only. A SELECT WHEN
clause on a METHOD's LOCAL-STORAGE or LINKAGE record that names the method's own level-88 (§2023:13.18.51.3 SR1 admits the clause
there, GR2 gives it no effect) drew COBOL7343 "is not the name of a level-88 entry". Re-probe on the unchanged tree: a class whose
method declares `01 R SELECT WHEN K. 05 X PIC X. 88 K VALUE "A".` and no object K was refused at `--std 2002`. The sibling arm
reproduced as the note said: with an object-level 88 K present the clause was accepted, and a method data item K that hides the
object's level-88 (§2023:11.7.4 GR5) did not refuse it. The fix resolves the clause through `TryResolveCondition`, the one condition
lookup with its method tier, in `DataBinder.ScopeOf(record.Root)`. Goldens: `13.18.51-SELECT-WHEN-clause/pb6276-method-select-when-own-condition-name`
(accept, 2002 on, prints METHOD-K) and `11.7-METHOD-ID/pb6276-method-select-when-hidden-condition-name` (refused COBOL7343 at 2002, 2014, 2023).

**PB6303, under-rejection.** `IF W-BLANK` and `SET W-BLANK TO TRUE`, with `W-BLANK` a level-88 of a group whose OCCURS DEPENDING ON
object is in a LINKAGE record the procedure division may not reference, compiled clean (the probe printed BLANK); `MOVE W-REC TO X`
was refused COBOL2746. `ResolveForItem` (the condition-name's conditional variable) built its place by hand and never ran
`ScreenImplicitLinkageReferences` (§2023:8.4.5, §2023:13.7.3 SR4; PB5876 closed it for every other entry). Both `ResolveImplCore` and
`ResolveForItem` now build through one private `ReferenceResolver.WrittenPlace` (link to the occurrence, then the screen when the
resolver commits), and `ResolveForItem` positions its diagnostics at the reference as `ResolveHere` does (PB4383). After the fix the
probe draws COBOL2746 at both statements. Golden `13.7-Linkage-section/pb6303-linkage-odo-object-condition-name` (refused at 2002,
2014, 2023). Drift: `ReferenceIdentificationDriftTests.EveryCommittedPlaceEntry_EndsInTheOneImplicitLinkageScreen`. The sibling sweep
of `Linked(Places.PlaceForItem(` found `ResolveByName` (its one caller, the table(ALL) argument, already screens its counts, PB5876) and
`ResolveForAddressOf` (an address, no extent read); neither needs the screen.

**PB6341, retired.** The note's scenario cannot be written: its entry `PIC 9,9` is numeric-edited and is no SUM addend (COBOL2730,
§2023:13.18.54.3 SR4). With `PIC 9V9 VALUE 1,5` and `PIC 9V99 VALUE 2,25` under DECIMAL-POINT IS COMMA, `SUM X Y` already printed
`0375` (3.75) on the unchanged tree. The note's "the raw text is stored unnormalized" is wrong: `DataBinder.RawValueOperandText`
returns `SpecialNames.NormalizeIfNumericLiteral(op.GetText())` (the one normalizer, `NumericLiteral.Normalize`, since 7e0598be1b), so
`ReportWriterBinder.RawNumber` filters an already canonical literal, and a floating-point VALUE is not admitted on a report entry
(COBOL2198). No code changed. Golden `13.18.54-SUM/pb6341_report_rolled_value_decimal_point_comma` pins the behaviour.

Inventory rows re-verdicted: none (the three notes carry none). Registry: PB6276 and PB6303 landed, PB6341 retired.

