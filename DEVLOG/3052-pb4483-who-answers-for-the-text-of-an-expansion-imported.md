## Entry 3052 — 2026-11-03 14:35 PST — PB4483: who answers for the text of an expansion (imported, placement (b), two places of one text)

PB4469 made a placement-(a) expansion report its text's warnings once, from its own compilation. Three siblings of the same root stayed
open, and the re-probe on `11c6abfb1` reproduced all three with the CLI. (1) An IMPORTED expansion: CLIENT1 wrote `HOLD-BOX.dll`, and a
second client of the same triple, which imports it and compiles nothing of it, printed COBOL3146 at its own EXPANDS entry. (2) Placement
(b) of the same repository skeleton printed COBOL3146 but not the COBOL0903 for NEXT SENTENCE that placement (a)'s own compilation prints:
the edition pass's parse-tree arm walked only the written tree, which does not hold an expansion lexed from the recorded text. (3) A
skeleton with two `IF 1 = 1` relations printed COBOL3146 once in placement (b) and twice in (a): PB4469 had moved every token of a
repository text to the entry's line AND column, so `EditionContext.AddOnce` merged two places of the text into one.

The fix is one rule, and it is a property of the TEXT, carried by the `DiagnosticRegion` every expansion token already names. A skeleton
written in the group is the user's own text: the group reports it at its own positions, once for every expansion, wherever the expansion
lives; the own-assembly compilation's warnings of it are the same facts under the same state and are dropped (they used to replace the
group's report with one at the entry), and an import leaves the group's report standing. A skeleton read from the repository gets a
region that carries the EXPANDS entry (`ReportedAt`): `EditionContext.Render` prints every diagnostic of it there, followed by
`[in the expansion 'NAME']` (`DiagnosticRegion.InTheExpansion`, the one spelling, which the sub-compilation's relabel uses too);
`EditionContext.KeyOf` keys the once-rule by the place in the text (the token keeps its own column in the recorded line, its line is still
the entry's for the directive folds); and only the compilation that compiles the text answers for its warnings: the group for placement
(b), the sub-compilation for (a), and, for an import, the compilation that made it, so `ExpansionPlacementPass` withdraws the group's
report as it places the import (ISO §9.3.12: an expansion "is treated in all respects the same as if it were a class that is not a
parameterized class"; nothing is reported of an imported class). Withdrawal is now only ever of a repository region, whose lines are its
own, so the owner bookkeeping became a per-region list. The parse-tree screens (the edition pass's parse arm, expression formation,
declined facility, closed format) walk `GroupBindContext.Texts`: the written tree, then each repository expansion whose recorded state
binds as the group's (`CompileState.BindsAs`, the one same-state rule, moved off `ExpansionPlacementPass`). `GroupTextScreenDriftTests`
holds every other walk to it; the `>>FLAG` walk and the pre-bind error-only screens are named with their reasons.

A self-found defect fixed on the way: three expansion tests asserted `HOLD-LBOX.dll` was not written, a file no placement could write
(the expansion is `HOLD-BOX` there), so their placement-(b) witness was vacuous; they now name `HOLD-BOX.dll`. The FragmentParse.ParseTokens
comment (kb/Work PB4484 item 1) now says what the positions are.

Witnesses: `ExternalRepositoryExpansionTests` gains the import Theory over both client orders, the placement-(b) edition warning, the two
places of one text in placement (a), (b) and a check-only compile, and a skeleton written in the group under placement (a) and an import;
`DiagnosticRegionTests` is rewritten for the rule. DESIGN-external-repository §10.5 "Who answers for the text of an expansion" replaces the
PB4469 paragraph. No inventory row is owned (closes_rows: []). The frontend's two token screens still walk the written tree alone (the
§10.5 text says so); a word-character removal warning under `--permissive` is the one report placement (b) does not repeat.

Self-review (architecture, full code, performance, duplication): the rule lives on the region and in `EditionContext.Render` / `KeyOf`,
the suffix has one spelling, the same-state test has one home (`CompileState.BindsAs`), and the once-key allocates only for a repository
text; two comments that described the old shape were corrected in a trailing commit. Gate at `86200486c`:
`=== BUILD-LOCAL GATE: GREEN — Conformance 21,333/21,333 · Unit 39,633/39,633 · Characterization 37/37 cases ran (skipped 0) ===`
(run `000001-20261103T215106Z`); the head's own gate re-ran the whole population after the comment commit.

The branch review found one more case of the same rule (R1-1). The parse-tree arm walked every member of `Texts` with ONE walker, whose
once-per-word set for COBOL0901 (ISO §8.9) and COBOL1567 was shared across texts. A reserved word flagged first in a repository text that
the group later withdraws (placement (a), or an import) was never flagged in a placement-(b) text that declares it too, and a word the
written tree declared hid it in every repository text. The reviewer's probe (skeletons HOLD and KEEP declaring `01 EDITING PIC X.`, HOLD
expanded over another assembly's BOX and KEEP over the client's own LBOX, `--std 2023 --permissive`) printed only the HOLD-BOX report; it
now prints `CLIENT.cob(28,5): warning COBOL0901 ... [in the expansion 'KEEP-LBOX']` too, and that one alone when HOLD-BOX is imported.
`VersionConformancePass.Run` walks each text with a walker of its own, so every per-walk state is per text by construction; the other
three screens over `Texts` keep no state across texts. Witness: `AReservedWordOfAWithdrawnExpansionText_DoesNotHideTheSameWordInAnotherText`,
with and without the word in the written tree, red on the shared walker and green on the fix.

