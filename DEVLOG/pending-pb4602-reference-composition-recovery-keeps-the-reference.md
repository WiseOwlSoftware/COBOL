## PB4602: the SAME AS / TYPE composition recovery drops the refused co-clause and keeps the reference

**Found.** Wave 20261863 C reported that `01 X TYPE T ALIGNED.` drew COBOL2150 (ISO 13.16.3 SR14, the one real refusal) plus a false
COBOL1942 ("not an elementary or group data item") and a COBOL0881.

**Re-probe.** The note undercounted the mechanism. The SAME AS twin (SR12) did the same: `01 X SAME AS Z ALIGNED.` drew COBOL1555, COBOL1942 and
COBOL0881, and `TYPE TP USAGE COMP`, `BLANK WHEN ZERO` and `JUSTIFIED` drew COBOL0881 (and COBOL2405) beside COBOL2150. Both recoveries in
`DataBinder.BindEntry` cleared the REFERENCE (`typeRefName = null`, `sameAsName = null`), so the entry reached the later screens as a PICTURE-less
leaf still carrying the refused co-clause. The wrong clause was being thrown away: the entry's description is the referenced one (13.18.57.4 GR1,
13.18.49.4 GR1), and it is the co-clause that is illegal.

**Fix.** The recovery inverts. `ScreenReferenceComposition` (DataBinder.ClausePlacement.cs) runs both screens over the written clause set before the
decode loop and returns the refused clauses. The loop skips each one (the `placementRefused` discipline already used for EXTERNAL, GLOBAL, PROPERTY and
BASED) and the TYPE / SAME AS name stays, so the entry expands from its type like a legal one. The receiver-wins description copy (PB513) stays safe in
the recovery path too: a refused PICTURE no longer discards the type's description. An entry that writes both TYPE and SAME AS draws both refusals and
keeps SAME AS. The two blocks in `BindEntry` (and the "placed after CONSTANT RECORD" ordering constraint they carried) are gone.

**Tests.** `ReferenceCompositionRecoveryTests` pins the COUNT of diagnostics (one per co-clause in both arms, two for the mutual case, none for the
permitted co-clauses), which the substring-matched refusal goldens cannot. The golden refusal
`cases/13.16-Data-description/pb4602-type-aligned-one-refusal` carries the witness. The pb487 golden's comment no longer promises a second diagnostic
for `SAME AS Z ALIGNED`.

**Self-review.** Architecture: one screen per rule pair, in the file that already owns the clause-placement discipline. Full code: the refused mask is
the set difference the cited SRs state; the mutual case has a defined survivor. Performance: one flag test per entry, a per-clause lookup only for an
entry that was refused. Duplication: none added; two inline blocks removed.

**Rows.** None closed (SR-13.16.3-12 and -14 were already CONFORMS); `closes_rows_reason` records it.

**Review round 1.** The reviewer found that the first recovery dropped EVERY refused co-clause, including TYPEDEF and BASED, which SR12 refuses
beside SAME AS but which are not part of the description the copy supplies. `01 X TYPEDEF SAME AS Z1. 01 Y TYPE X.` then drew COBOL1555 plus one
false COBOL5999 per `TYPE X`, and `01 X BASED SAME AS Z1.` with `ALLOCATE X.` / `SET ADDRESS OF X` drew COBOL1555 plus a false COBOL0869 per
reference: the cascade moved from the entry to every reference. The recovery now drops the DESCRIPTION clauses only. `DataClauseKinds.EntryRoleClauses`
(TYPEDEF, BASED, PROPERTY: the clauses a reference to the entry depends on) are reported in the one COBOL1555 and kept. Two tests in
`ReferenceCompositionRecoveryTests` carry a referencing program for each and fail without the mask. The stale comments the move left
(`PlacementAnsweredElsewhere` rows for TYPE and SAME AS, two "once entryWhere exists" remarks in `BindEntry`) are corrected.
