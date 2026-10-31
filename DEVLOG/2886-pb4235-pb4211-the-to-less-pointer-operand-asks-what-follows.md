## Entry 2886 — 2026-10-31 15:31 PDT — PB4235, PB4211: the TO-less pointer operand asks what follows in THIS entry, so a screen word names a type in a data entry

**The defect (kb/Work PB4235, the Mythos front-end review's N6).** §13.18.60.2 prints `POINTER [ TO type-name-1 ]` and
`PROGRAM-POINTER [ TO program-prototype-name-1 ]` with TO not underlined, so by §5.2.3 the operand may follow the usage
keyword directly (PB848). The grammar guards that TO-less `cobolWord` with `pointerOperandHere()` so it cannot swallow
the next clause, and the guard refused every token in the context-free FOLLOW(`usageKeyword`). The usage clause is a
clause of the data description entry, the report group description entry AND the screen description entry, so that
FOLLOW was the union of all three entries' clause words. §8.10 makes AUTO, BACKGROUND-COLOR, BELL, BLINK, ERASE,
FOREGROUND-COLOR, FULL, HIGHLIGHT, LOWLIGHT, REQUIRED, REVERSE-VIDEO, SECURE and UNDERLINE context-sensitive words of the
"screen description entry", and §8.3.2.1 3) lets such a word be a user-defined word "in contexts other than the
language construct in which they are defined". So `01 PT TYPEDEF USAGE POINTER AUTO.` in WORKING-STORAGE is the same
entry as `... POINTER TO AUTO.`, and the compiler refused it.

**Re-probe** on this branch's build of 11e8949be: all 26 spellings (the 13 words after POINTER and after PROGRAM-POINTER,
each word declared as a TYPEDEF) drew `COBOL1941: ... 'AUTO' is not a clause of the data description entry for 'PT'`,
a false and misleading diagnostic. The `TO AUTO` control compiled clean.

**The fix.** `pointerOperandHere` now asks the question `keywordContinuesHere` already asks at the end of an operand
list: does the lookahead word take its keyword reading here (`IsKeywordReadingHere`), and can it be matched as a keyword
of what follows, walked off the generated ATN through the parser's LIVE rule-invocation stack (`KeywordFollows`)? That
question is now one method, `CobolParserCoreBase.KeywordContinuesFrom(exit)`, and both predicates call it. The pointer
guard starts the walk at the pointer rule's stop state, since the optional operand is each pointer rule's last element.
That gives the same answer in prediction (State is the block decision) and in the inline predicate check (State is the
predicate's own state). A data entry now excludes only the data entry's clause words and the USAGE tail phrases. A screen
entry still reads AUTO as its AUTO clause. One more consequence: a word that §8.9 leaves free at the edition and that the
program declared is now the operand, as in every operand list (the PB805 determination). The context-free forwarder
`UsageFollowSet` is deleted, which closes kb/Work PB4211. `CobolScreen.g4` needed no change. Its `screenClause` was
correct, and the leak came from the guard's set.

**Tests.** `PointerUsageOperandDriftTests` is rewritten per entry. It derives each entry's own follow of a usage keyword
from the ATN along that entry's rule chain, independently of the parser's walk. For every word `cobolWord` admits, it
then parses `USAGE {POINTER|PROGRAM-POINTER|FUNCTION-POINTER} W` in a data entry (at 85, 2002, 2014 and 2023), a report
group entry and a screen entry, and requires the operand exactly when W does not follow. The non-vacuity fact requires
HIGH-ORDER-RIGHT and SIGNED in the data follow, and all 13 screen words in the screen follow and none in the data follow.
A shape fact holds the last-element assumption. `EditionReadDriftTests` records `KeywordContinuesFrom` and
`pointerOperandHere` as word-status reads, plus the three pointer-rule sites. Goldens:
`conformance-suite/cases/13.18.60-USAGE/pb4235_pointer_operand_screen_word` is accepted from 2002. In it POINTER AUTO,
POINTER SECURE and PROGRAM-POINTER BELL each equal their TO-full twins, which §14.9.39.3 SR19/SR22 admit only when both
name the same type or prototype, and a CALL goes through the program-pointer. `pb4235-pointer-operand-screen-word-below-2002`
is refused at 85 by the POINTER introduction gate. FMT-13.18.60.2 is re-verdicted CONFORMS with the new witnesses
(batch `w20261788c-PB4235`), and the retired theory is recorded on the row. `docs/rearchitecture/DESIGN-frontend-grammar.md`
and the CobolData.g4 design comment now describe the live-context follow.

**Self-review.** Architecture: one follow question for both predicates, and no second copy. Performance: the walk runs
only at a pointer operand decision, as `keywordContinuesHere` runs at a list decision. Duplication: the FOLLOW cache
keeps its one caller (`ReadingParses`). Comments: each predicate cites §8.3.2.1 3) and §8.10, checked with `cite.py`.

**Gate.** Run 000001-20261031T213728Z on 5e969b810: `LEG 1 ONLY (batched gating, PB2515): GREEN`, with the Linux
half (WSL clone of HEAD) GREEN. Both goldens and all 15 PointerUsageOperandDriftTests cases passed in leg 1.

