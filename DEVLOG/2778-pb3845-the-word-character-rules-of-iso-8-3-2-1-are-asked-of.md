## Entry 2778 — 2026-10-30 09:00 PDT — PB3845: the word-character rules of ISO §8.3.2.1 are asked of the lexer's words, so a parse failure no longer hides their cause

The U+00A0-only line of golden `pb1660-nbsp-only-line-is-not-blank` drew a bare `COBOL0001 cannot parse construct near
'STOP'` at every edition and never COBOL2773 naming U+00A0. Re-probed on the branch build before any edit: reproduced.

**Mechanism.** The character half of §8.3.2.1 (the underscore gate, the extended-letter gate, Annex B.3 / COBOL2773, the
2023 removal of Annex E.2 item 4) sat in the parse-tree funnel `VersionConformancePass.VisitCobolWord`. A funnel that walks
the tree never runs on a source that does not parse, yet the lexer reads such a word on purpose (`NAME_BODY` admits every
non-basic character so the diagnostic can name it). The U+00A0 word is read, no grammar rule can place it, the parse fails
at the next token, and the cause is lost.

**Fix.** `WordCharacterScreen` (Frontend/Parsing) asks the rule of every `IDENTIFIER` token, the only token built from
`NAME_BODY`; the funnel's copy is deleted. All four checks moved together (one mechanism): the underscore gate, the
extended-letter gate, COBOL2773 and the removed construct (a warning under `--permissive`). One body, two word sources:
a failed parse is screened by `Frontend.LexAndParseOnce` over the lexer's whole stream, a successful one by `BinderDriver`
over the tree's words. The first draft screened the stream in every case and reported through the frontend, which stopped
a parseable source before the binder: the gate went RED on `pb2983-supplementary-letters-parser-error-column`, whose
COBOL1941 (a bind-time diagnostic) no longer accompanied COBOL2773 at 2002 and 2014. The pb1660 line now draws COBOL2773
naming U+00A0 at every edition, next to the parse error (at 85 too: no edition's Annex B holds U+00A0, so it is no
extended letter and the 2002 gate does not apply; main's PB4130 made that the rule while this branch waited, and the
conflict finisher moved it into the screen's one body). One report per distinct word under the Annex C fold.

**Structure.** The first draft of the screen flagged `AUTHOR. JOSÉ`, a legal COBOL-85 comment: the tokens of a comment-entry
are not words. The six identification comment paragraphs spelled the same `~DOT+` body six times (`authorContent`,
`installationContent`, `dateWrittenContent`, `dateCompiledContent`, `securityContent`, `remarksContent`); they are now ONE
rule, `commentEntry`, and the screen finds the non-word tokens by that one context, so a seventh paragraph needs no arm.

**Goldens.** `pb3845-nbsp-word-unparseable` (85-2023, COBOL2773), `pb3845-extended-letter-unparseable-85` and
`pb3845-underscore-unparseable-85` (COBOL0900), `pb3845-removed-character-unparseable-2023` (COBOL0902), and
`WordCharacterScreenTests` for what a golden cannot hold (a comment-entry is not words at all four editions; a distinct
word is reported once; `--permissive` keeps the warning next to the parse error; a phrase-word position is screened).

**Rows.** None closed: the rule's inventory rows were closed by PB1402; this names the cause when the parse fails.

**Review round 1.** The reviewer found that the failed-parse screen read every `IDENTIFIER` of the stream, including the words
of a fixed-form debugging line that is a comment (the rewriter moves it to the `ABSENT_DEBUG_LINE` channel), so a program
with `D    DISPLAY MY_VAR.` and an unrelated parse error drew a false COBOL0900 naming comment text; `IsCandidate` now skips that
channel like the other two stream readers. The screen's gate widened from "the parser reported" to "the frontend reported any
error" (the binder is skipped on any, so a source that parses beside a COBOL2631 separator error lost the underscore gate);
it runs after the last stream reader and counts the pass's earlier preprocessor errors. `CursorBagSink` was a second copy of
`BagSink`'s severity mapping: it is deleted and `BagSink` carries the cursored form. Three new `WordCharacterScreenTests` pin
all of it (each fails with its fix removed), including the failed-parse comment-entry exclusion that nothing had tested.

