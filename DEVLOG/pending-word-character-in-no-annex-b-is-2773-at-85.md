## A character in no edition's Annex B is COBOL2773 at COBOL-85, not the extended-letter gate (PB4130)

A word holding U+00A0 or the euro sign drew COBOL0900 "an extended letter in a COBOL word requires COBOL-2002" under `--std 85`, although neither character is an extended letter of any edition: no Annex B (2002/2014 or 2023) holds it. The refusal was right; the message blamed the target for what is the character.

**Re-probe.** On the worktree build `PREIS<euro>` and `PREIS<U+00A0>` gave COBOL0900 at 85 and COBOL2773 at 2002/2014/2023.

**Fix.** `CobolCharacterRepertoire.ViolationAtEveryEdition` finds the first character that is NotPermitted in both the 2023 table and the 2002/2014 table, and words it by what it is ("in no edition's Annex B"). `CobolWordRule.CharacterViolation` asks it below 2002, where there is no edition Annex B to judge by (from 2002 it is still `Violation` over the edition's own table). `VersionConformancePass.VisitCobolWord` keeps the `user-word-extended-letter-2002` gate for a word with a real extended letter and skips it for a word the rule refuses, so COBOL2773 is the one diagnostic. `Violation` and the new method share one `NextCodePoint` reader. The COBOL2773 catalog text and `DESIGN-frontend-grammar.md` carry the new case.

**Goldens.** New `pb4130-no-break-space-in-a-word` (refused at 85, 2002, 2014 and 2023 with COBOL2773); `pb1402-not-an-annex-b-character` (euro) is now refused at 85 too; `pb1402-extended-letter-85` (CAFE-acute) still draws COBOL0900. Unit test `WordCharacterEveryEditionTests`.

**Rows.** None claimed or closed (`closes_rows: []`, a wording fix). PB3845 (the U+00A0-only line breaking the parse) is a different mechanism and stays open.
