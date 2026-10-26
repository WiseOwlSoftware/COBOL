## Entry 2595 — 2026-10-25 18:44 PDT — PB3682: a TYPE declaration takes the usage and SIGN walks, and a description copied before them still has its subordinates

PB3682 filed one silent screen: a TYPEDEF `GROUP-USAGE NATIONAL` over a `GROUP-USAGE BIT` subgroup compiled while its storage twin drew
COBOLNET1653. Re-probed on the branch build, the root is the walk, not the block: `UsageInheritancePass` walked `Roots` only, so none of
the §13.18.60.4 GR1 screens reached a TYPE declaration (a TYPEDEF group `USAGE BINARY` over a `USAGE PACKED-DECIMAL` member, over a `PIC X`
member, `USAGE PACKED-DECIMAL WITH NO SIGN` over `PIC S9`, a VALUE under `USAGE INDEX` were all silent unreferenced). Beside it ran the opposite
hole: a valid `01 TG TYPEDEF USAGE BINARY-LONG. 05 A. 05 B.` was refused (COBOLNET2191 and COBOLNET0881), and probing that shape found three more
faults with one cause.

Fixes. (1) `UsageInheritancePass` and `InheritSignClauses` walk `AllTypeDecls()` after the roots, so a declaration answers for itself and its
members take their group's usage and SIGN as a TYPE subject's do. That makes a template and its subject one description to the §8.5.3.1
comparison: `SET P TO ADDRESS OF W` with P restricted to a STRONG type whose group wrote USAGE or SIGN was refused COBOLNET0869 and is accepted.
A copy takes the walk's application but not its screens where the fact is its declaration's (`AnsweredByTypeDeclaration`, through the new
`EditionContext.Muted`), so a TYPEDEF referenced twice draws one diagnostic naming the declaration, not one per reference naming the subject;
a clause from outside the copy and §13.18.29.3 SR1's "not strongly typed" stay the copy's. A copy of a template the container's bind already
walked (a GLOBAL declaration in a contained program) arrives with its leaves' profiles, and the elementary arm leaves them alone. (2) The
TYPE and SAME AS copies run before the walk and asked `IsGroup` (`Pic is null`), false of `01 G USAGE BINARY-LONG. 05 A.` until the walk
sheds the header's profile: `01 V SAME AS X` over such a group was ONE elementary item a quarter of X's size (BYTE-LENGTH 8 against 4, a silent
wrong answer), a TYPE subject got no members (`A OF W` undefined), `TYPEDEF STRONG` of the shape drew COBOLNET1529, and `SAME AS H` of a level-5
group with a usage passed SR7. They ask `DataItem.HasSubordinates` now, pinned by a drift test. (3) `CompleteDescriptionAhead` recorded the
profile as written for a later copy but not its provenance or the deferred mark, so a SAME AS after `CONSTANT AS LENGTH OF X` read the header's
profile as a written PICTURE on a group; `PictureAsWritten` keeps all three.

Goldens: nine refused programs, one per screen on a TYPEDEF tree, plus the SAME AS level-1 refusal; five accepted programs (the template, the
GLOBAL template in a contained program, SAME AS, SAME AS after a length constant, the restricted pointer). `UsageInheritanceDriftTests` gains the
third spelling (`01 TG TYPEDEF USAGE u. 05 A pic.`, typed and unreferenced) over its whole usage x phrase x picture matrix, which fails 49 of 77
cells with the template walk removed, plus tests for once-per-declaration and for the copies' subordinates. The inventory rows were already
CONFORMS for their storage arm: witnesses added to 15 rows (`closes_rows: []` with its reason). Self-review found the walked-template copy
hazard (a contained program copying a completed template) and the muted-diagnostic dedupe before the gate; both have witnesses above.

Branch review round 1. One blocking finding, the sibling arm of part 2 that the first pass listed as a lead: `01 G USAGE BINARY-LONG. 05 E.
01 S SAME AS E.` is legal (§13.18.49.3 SR7: E is elementary) and was refused as "not elementary", legal source rejected on the base build too.
Two reads of the completed form remained in the SAME AS expansion: SR7 asked `IsElementary` (a `Pic` test, null on E until the walk) and the
§13.18.49.4 GR3 ancestor transfer asked `GroupKindsOf` (`IsGroup`) of G, whose header still held the profile entry bind synthesized for its
usage. SR7 now asks `!HasSubordinates`; `GroupKindsOf` and the two helpers it composes (`IsStrongGroup`, `IsVariableLength`) ask a new
`DataItem.IsGroupEntry` (subordinates and no PICTURE the program wrote, equal to `IsGroup` once the walk ran), pinned by
`TheGroupKindClassifier_AsksTheEntryForm_NotTheCompletedForm`; and the transfer hands the subject the group's synthesized profile and
deferred mark, as the walk hands E them, through `PictureAsWritten.GiveTo`, which `CopyEntryDescription` now calls too (the first spelling
of the hand-over was a hand copy and `NoCodeOutsideTheOneCopy_HandCopiesTwoDescriptionClauses` caught it). The pin on the three copies now
refuses any `IsElementary` that is not the STRONG screen, so the half-fixed `IsElementary && !HasSubordinates` form is red. Accepted golden
`pb3682_same_as_leaf_under_group_usage` (P3682O). The lead (R1-2) sat in a file this change already edits, so it is fixed: the SR2/SR3 subgroup
screen muted on `ClonedFrom` alone, which also dropped the SR3 fact of a TYPE or SAME AS subject whose GROUP-USAGE an enclosing group outside
the copy implied; `GroupUsagePairIsTheDeclarations` mutes only a pair the declaration holds. Refused goldens
`pb3682-type-subject-group-usage-from-enclosing-group` (P3682P) and `pb3682-same-as-subject-group-usage-from-enclosing-group` (P3682Q),
both COBOLNET1653 at 2002, 2014 and 2023.

