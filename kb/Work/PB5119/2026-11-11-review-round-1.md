## Review round 1 (wave 20261998, group B; reviewer w20261998b-reviewer at 392ad26b27)

**R1-1 (high, a wrong answer): a reference-modified identifier-1 kept the defect.** The landing pinned the DEPENDING ON
object only when identifier-1 was a plain `OdoGroupPlace`. The reviewer's program (record R = `H PIC X` then group G
with `E PIC X OCCURS 1 TO 6 DEPENDING ON N`, R "HABABAB", N 3): `INSPECT G (2:) TALLYING N FOR ALL "A" REPLACING
CHARACTERS BY "Z"` printed `HAZZ AB` at 85, 2002 and 2023. Re-probed on this build with every form (probe programs in
the wave scratch, `w20261998b/probes/pi.cob`): `G (2:)` `HAZZ AB` and `R (2:)` `HZZZ  B` at every edition;
`G`, `G (2:2)`, `R`, `EG (1)` and `EG (1) (1:)` right.

Rule (citations checked with `scripts/spec/cite.py --check`, and each ordinal read in `specs/`, because the checker
does not validate the rule number, kb/Work PB5317): §2023:14.6.4 makes step 6, "length evaluation for an
occurs-depending group item", part of item identification before step 7, reference modification; §2023:14.9.22.4 GR19
identifies the format 2 statement's identifiers once; §2023:13.18.38.4 GR8 a takes data-name-1 "at the start of the
operation" and GR7 (its second paragraph) tests the bounds "At the time the subject of entry is referenced". The 2002
text is the same (§2002:14.8.21.3 GR18, §2002:14.5.4 step 6). At 85, §1985:VI.6.18.4 GR17 fixes only subscripting and
§1985:VI.5.8.4 GR3 a reads the object at the start of each of the two successive statements. So from 2002 `G (2:)`
gives `HAZZBAB` and `R (2:)` `HZZZBAB`; at 85 `HAZZZAB` and `HZZZZZB`.

Fix: the pin is a property of place identification, not of one place kind. `Binding/Model/OdoDependence` is the one
walk (`Rebind`, and `ReadsDependingObject` as the same walk) over every `Place` kind, driven by a `Roles` roster that
classifies each kind (group extent, decoration whose inner is walked, access-path leaf whose table steps carry the GR7
bound test, leaf with no object); `OdoDependenceRosterDriftTests` asserts the roster complete against the assembly and
mutation-checked red when a row is dropped, so a new place kind cannot be added without stating its ODO dependence.
`StatementBinder.FreezeOdoExtentBeforeRefMod` now pins every object the walk finds (a DEPENDING ON place met twice is copied once) and
serves both the PB1123 resolver hook (`ReferenceResolver.FreezeOdoExtent`, now over any inner place) and
`FreezeOdoExtentForStatement`; `InspectBinder.Bind` asks `OdoDependence.ReadsDependingObject(target)` for the 2002 pin
and the 85 `ReplacingTakesLengthAnew`.

After (85 / 2002 / 2023): `G (2:)` HAZZZAB / HAZZBAB / HAZZBAB; `R (2:)` HZZZZZB / HZZZBAB / HZZZBAB; `T2 (1:)` HZZZZAB /
HZZABAB / HZZABAB; `G (2:2)`, `EG (1)` unchanged and right; `EG (1)` under `>>TURN EC-BOUND-ODO CHECKING ON` with the
counter taking N past integer-2 raises nothing at the write-back (`HZZAAAA`), where the bound test re-read N before.
Goldens: `14.9.22-INSPECT/pb5119_format3_odo_forms` (2002 2014 2023) and `pb5119_format3_odo_forms_1985` (85).

Sibling sweep (statements that store into an operand's own DEPENDING ON object), probed at 85, 2002 and 2023, all
already right, pinned by `14.6.4-Item-identification/pb5119_odo_object_changed_by_statement` (85 2002 2014 2023):
STRING INTO G WITH POINTER N (§2023:14.9.43.4 GR8: identified at length 1, overflow, N 2); UNSTRING G INTO N W
(§2023:14.9.48.4 GR11: W XYZW); MOVE G TO N W (§2023:14.9.25.4 GR1, §1985:VI.6.19.4 GR2: the sender's length once); MOVE
S TO N G (the same rules: each receiver identified immediately before its move, G at N 3); SEARCH TE VARYING N
(§2023:14.9.37.4 GR3 b 2 and GR4, §1985:VI.6.22.4 GR2 b: each new index is tested against the current permissible range;
TE is a table element, not an occurs-depending group, so step 6 does not fix it). A reference-modified STRING receiver is
refused (§2023:14.9.43.3 SR4) and a reference-modified UNSTRING sender at 85 (COBOL0900), so those forms do not arise.

**R1-2 (medium): the DialectBehavior value and VCR row collided with PB5121.** origin/main merged; this branch's member
is now `InspectFormat3LengthIdentifiedOnce = 7` and its VCR row 7.57, in every file that named them.

**Sites:** `src/WiseOwl.Cobol.Compiler/Binding/Model/OdoDependence.cs` (`OdoDependence.Roles`, `Rebind`,
`ReadsDependingObject`), `src/WiseOwl.Cobol.Compiler/Binding/Bound/StatementBinder.SubscriptSegments.cs`
(`FreezeOdoExtentBeforeRefMod`, `FreezeOdoExtentForStatement`), `src/WiseOwl.Cobol.Compiler/Binding/ReferenceResolver.cs`
(`FreezeOdoExtent`, `ResolveImplCore`'s reference-modification tail), `src/WiseOwl.Cobol.Compiler/Binding/Procedure/Verbs/InspectBinder.cs`
(`Bind`), `tests/WiseOwl.Cobol.Tests.Unit/OdoDependenceRosterDriftTests.cs`.
