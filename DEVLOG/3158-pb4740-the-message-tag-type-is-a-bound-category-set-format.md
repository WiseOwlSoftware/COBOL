## Entry 3158 — 2026-11-05 13:09 PST — PB4740: the message-tag type is a bound category, SET Format 17 and the message-tag relation run, COBOL1943 and COBOL2205 retire

Slice S1 of the asynchronous messaging design (`docs/rearchitecture/DESIGN-message-control.md`, approved 2026-11-04). The re-probe on a
fresh build before the change: `01 T USAGE MESSAGE-TAG` was refused (COBOL1943), `SET T TO NULL` was refused (COBOL2205) and
`IF T = NULL` drew COBOL0869. After it, the usage, NULL, SET Format 17, the relation, INITIALIZE, a strongly-typed group's tag and a tag passed
BY REFERENCE or BY CONTENT all bind and run.

**The type.** A message tag is a native field, as the design says. `PicCategory.MessageTag` and `PicInfo.MessageTagItem` (PICTURE-less per
13.16.3 SR8), `StorageForm.MessageTagRef` (image width 0), `CobolClass.MessageTag` (so every screen over the Table 2 lattice decides a tag instead
of failing open), the slot rule (`SlotWindow.CarriedBySlot`: 8 positions, `FUNCTION BYTE-LENGTH` 8, DOC-A.1-212), the runtime `MessageTag`
readonly struct (`MessageTag.Null`, `SameParty`, `MessageParty`; a struct and not a record, so C# has no `==` an emitter could mistake for 8.8.4.2.14's
equality), `RepositoryCategory.MessageTag = 10` and `HostTypeKind.MessageTag` (a tag crosses a call and a host entry like any slot). Deleted:
`PicInfo.MessageTagRecovery`, the written-usage arm of `ItemCategory.IsMessageTag`, COBOL1943 and COBOL2205.

**The statements.** SET Format 17 (`BoundSetMessageTag`, `SetBinder.BindMessageTagSet`, `SetEmitter.EmitSetMessageTag`) with data-name-4 the receiver
in both general rules (the printed GR40 and GR41 disagree with the format; D-SET17, owner-approved as Q1). The relation renders through
`MessageTag.SameParty`, equality only, NULL admitted; a strongly-typed group's tag leaf reaches it through `StrongGroupComparison` unchanged.
INITIALIZE binds each form through its existing arms once `InitializeItemCategory` classifies the item.

**What the re-probe of the class rules found.** The design expected each rule that names message-tag to "fire as soon as the usage binds". Seven did
not: DISPLAY of a tag threw inside the emitter, ACCEPT into one and `MOVE T TO N` reached Roslyn as a CS error, `MOVE N TO T` compiled and stored
nothing, the MAX and MIN family let one through, the EVALUATE range and the class-condition list omitted the class, and REDEFINES named only
"object/pointer". Each is fixed at its class list (`DisplayExcludedClasses`, the ACCEPT receiver screen, `MoveTable16.Sr1ClassRefusal`,
`PolymorphicExcluded`, `EvaluateBinder.ScreenRangeOperands`, `ClassConditionModel.Sr1Refusal`, `ClassifyRedefinesClassesPass`), with a refusal
golden each. BY VALUE: the header formal (14.2.2 SR2) and INVOKE (14.9.23.3 SR15) admit the class; CALL's SR22 does not and refuses it.

**Goldens.** Positive: `message-tag-usage`, `set-message-tag`, `message-tag-relation`, `initialize-message-tag`, `message-tag-call-by-reference`.
Refusals: twenty-one `message-tag-refused-*`, one per class rule. `pb453-set-message-tag-format17` and `pb487-message-tag-usage`, the refusals of
the retired codes, are deleted; the pb544, pb819 and pb916 goldens still hold and their comments no longer name COBOL1943. A runtime unit test
(`MessageTagTests`) pins the non-null half of 8.8.4.2.14, which no golden can build.

**Registry.** Eight rows are CONFORMS (GR-13.18.60.4-9, SR-13.18.60.3-21, SR-14.9.39.3-35, GR-14.9.39.4-40 and -41, GR-8.4.3.10.4-4, GR-8.8.4.2.1-9,
DOC-A.1-212) and 24 more gained a witness. `derived-verdicts.mcs-only` stops selecting the message-tag class (S4 deletes it); `usage-message-tag-2023`
is active. CONFORMANCE.md: §4 item 1, A.3 row 4, DOC-A.1-212 (new), DOC-A.1-125, DOC-A.1-46, D-SET17 and D-MCS-ARG (new). The user guide
(`communication.md`) gains the tag section.

**Review round 1.** Six blocking findings, all fixed with goldens. (R1-1) A program or function RETURNING a tag drew Roslyn CS0315: `CobolArgAdapt.StoreReturn`
had an overload for each pointer struct and none for `MessageTag`; added (golden `message-tag-call-returning`). (R1-2) `01 T USAGE MESSAGE-TAG EXTERNAL` was refused
COBOL1796 because the EXTERNAL screen asked the slot predicate, which the tag joined; §13.18.22.3 SR4 lists object and pointer only, so the screen now excludes the
message-tag category (golden `message-tag-external`, two programs sharing the item). (R1-3) The sort and merge key screen had no tag arm: a tag key in a file SORT or
MERGE compiled and aborted, in a table SORT it reached Roslyn. `SortKeyAdmission.Violation` now refuses it (COBOL1757, three refusal goldens); the table form is
refused on §8.8.4.2.14's equality-only comparison, since SR14 c) does not name the class (determination D-MCS-SORTKEY). (R1-4) `IF T = SPACE` was accepted and rendered the
SPACE as NULL, because `CheckFormat3Relation` abstained for a figurative operand; a figurative is decided, not undecidable, so it now draws COBOL0869, and the same site fixes
the pointer and object bands (goldens `message-tag-refused-relation-figurative`, `pointer-refused-relation-figurative`, `message-tag-refused-evaluate-figurative`). (R1-5)
Witnesses added for the class rules the first landing left unwitnessed: occurs key, renames, move corresponding, accept temporal, evaluate, convert, ord-max, ord-min,
invoke by value, property, and both D-MCS-ARG mismatch arms. Writing the CONVERT golden found a real hole, the §15.19.3 rule 7 screen lacked the message-tag usage
(compiled and aborted at run time); fixed in the same change. (R1-6) The stale COBOL1943 and COBOL2205 present-tense claims in seven test comments, the README and 40
inventory notes are rewritten (record_verdicts batches 4 to 6). The two leads (a second class diagnostic below 2023; SORT over an SD record with a tag leaf) stay for the clerk.

**Not done here, on purpose.** `DataItem.ElementImageCapable` is unchanged, so a group holding a tag has the one-way transfer image an object
reference's group has; the two-way image SEND and RECEIVE need is S4's, with its emitter (design §4.2 now says so). The unequal-tags case of 8.8.4.2.14
needs a live party. The 8.8.4.2.14 prose gap note is a registrar lead in the implementer's report.

