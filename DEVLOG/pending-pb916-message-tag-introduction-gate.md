## PB916 — USAGE MESSAGE-TAG answers to one registry row per edition: the 2023 decline no longer fires below 2023, and an inherited message-tag usage is no longer reported as floating-point

kb/Work PB916 filed three false statements about the MESSAGE-TAG surface. Re-probed on a fresh Debug build of main
669f45311 (0 warnings, 0 errors), all three reproduced as filed: `01 M USAGE MESSAGE-TAG.` drew the byte-identical COBOL1943
at `--std 85`, 2002, 2014 and 2023; `01 G USAGE MESSAGE-TAG. 05 M PIC X.` drew COBOL1521 naming COMP-1/COMP-2/FLOAT-*; and
the `CobolClass.Index` remark still said message-tag had no `Usage` member.

**Arm 1, the root.** MESSAGE-TAG exists from 2023 only (§8.9 reserves it then; Annex E.2 item 25) but had no construct row, so
nothing in the tree could say that below 2023 the introduction gate is the whole answer, and the Annex A.3 decline fired
unconditionally in `PictureAnalyzer.ParseUsage`. COBOL1943's own text promised the gate it pre-empted. The fix is a row,
`usage-message-tag-2023` (`pending`, as `initialize-category-2023` is: no program is accepted at 2023, the facility being
declined), gated from the bound arm in `VersionConformancePass.UsageConstructId` (the `usage-function-pointer-2014` shape,
so the written, inherited and TYPE-cloned spellings all reach it), and the decline asks the SAME row
(`ConstructRegistry.IsAvailable`, the shape `StatementBinder.BindDeclinedMcs` set for RECEIVE/SEND). Below 2023 the entry now
draws COBOL0900 "USAGE MESSAGE-TAG requires COBOL-2023 (targeting COBOL-85)" and nothing else about the facility; at 2023
COBOL1943 stands. The syntax rules over the usage (SR4 COBOL1726, SR14 COBOL1724, SR21 COBOL2158) are reported beside the
gate at every edition, as COBOL1958 is beside FUNCTION-POINTER's, which is what lets PB544's negative golden take all four
editions (it had been pinned to 2023 to keep from freezing this defect).

**Siblings of the same mechanism, fixed the same way.** SET Format 17 (`SetBinder.BindMessageTagSet`, COBOL2205) named the
statement half of the facility at 2002 and 2014 too; below 2023 it now binds a no-op and the operands' COBOL0900 fails the
compile. The OPTIONS FLOAT-DECIMAL decline (`OptionsBinder.DeclineFloatDecimal`, the COBOL2424 warning) fired at 2002 beside
the COBOL0900 that says the clause needs 2014; it now asks `options-float-decimal-2014`. Both found by sweeping every
`Declined(` site (`grep -rn "\.Declined(" src`) and probing the ones whose facility is introduced after COBOL-85. Left
as leads in the report (not this slice): the SCREEN SECTION decline (COBOL1560) names no introduction gate at 85, and the
FLOAT-BINARY-128 / FLOAT-DECIMAL-16/-34 usages keep COBOL1564 at every edition by a recorded choice in `UsageConstructId`.

**Arm 2.** `DataBinder.Sr8PicturelessVerdict` ended in `_ => ("COBOL1521", "... a floating-point usage ...")`, so every
picture-less usage without an arm of its own was reported as floating-point; §13.16.3 SR8 names message-tag in the same
sentence as the float usages, which is why the text looked plausible. MessageTag now has its arm (code COBOL0881, text
naming USAGE MESSAGE-TAG), the float family is a named arm, and the fall-through throws, so the next picture-less usage fails
the drift test that enumerates `Usage` instead of mislabelling its refusal.

**Arm 3.** The remark now says what is true: `Usage.MessageTag` exists since PB487, and the absence of a `CobolClass` member
is DELIBERATE (a declined facility's recovery profile has no analysed category, so every class screen fails open and the
entry's own refusal is the diagnostic). The decision lives in `IntrinsicArgumentClassDriftTests`, whose comment said "the
storage category answers" (false for a recovery profile) and is corrected.

**Witnesses.** `wiseowl/cases/13.18.60-USAGE/pb916-message-tag-introduction` (refused at 85, 2002, 2014 with COBOL0900),
`13.16-Data-description/pb916-message-tag-inherited-picture` (all four editions, text naming MESSAGE-TAG), `pb544-message-tag-usage-exclusive` swept
over all four; `MessageTagIntroductionGateTests` asserts the ABSENCE of COBOL1943 and COBOL2205 below 2023 (a refusal file can
only assert that a code is reported); `DocumentedNonSupportWitnessTests` gains the FLOAT-DECIMAL 2002 case. Rows: none
claimed or closed (`closes_rows: []`; the fix changes which edition answers, not a verdict). Docs updated in the same change:
`docs/CONFORMANCE.md` §4 item 1, `docs/DATA_MODEL_DESIGN.md`, the comments of the PB453 golden and the SR21 screen.

**Self-review.** Architecture: one row, three readers (the pass, the usage decline, the SET decline), no edition number in the
binder. Full code: the row's introduction edition and citations were `--check`ed (§13.18.60.4 GR9, §8.9, §13.16.3 SR8). Sibling
arms: written / bare / group-inherited spellings are all in the new theory. Performance: one dictionary lookup per MESSAGE-TAG
entry, none on any other path. Duplication: the two `IsAvailable` asks name one row; no second copy of the edition fact.
