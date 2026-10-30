## PB3845: the word-character rules of ISO §8.3.2.1 are asked of the lexer's words, so a parse failure no longer hides their cause

The U+00A0-only line of golden `pb1660-nbsp-only-line-is-not-blank` drew a bare `COBOL0001 cannot parse construct near
'STOP'` at every edition and never COBOL2773 naming U+00A0. Re-probed on the branch build before any edit: reproduced.

**Mechanism.** The character half of §8.3.2.1 (the underscore gate, the extended-letter gate, Annex B.3 / COBOL2773, the
2023 removal of Annex E.2 item 4) sat in the parse-tree funnel `VersionConformancePass.VisitCobolWord`. A funnel that walks
the tree never runs on a source that does not parse, yet the lexer reads such a word on purpose (`NAME_BODY` admits every
non-basic character so the diagnostic can name it). The U+00A0 word is read, no grammar rule can place it, the parse fails
at the next token, and the cause is lost.

**Fix.** `WordCharacterScreen` (Frontend/Parsing) asks the rule of every `IDENTIFIER` token, the only token built from
`NAME_BODY`, from `Frontend.LexAndParseOnce` after the parse, whether or not it succeeded; the funnel's copy is deleted.
All four checks moved together (one mechanism): the underscore gate, the extended-letter gate, COBOL2773 and the removed
construct (a warning under `--permissive`). The pb1660 line now draws COBOL2773 naming U+00A0 from 2002, and the
extended-letter gate (COBOL0900) at 85, next to the parse error. One report per distinct word under the Annex C fold.

**Structure.** The first draft of the screen flagged `AUTHOR. JOSÉ`, a legal COBOL-85 comment: the tokens of a comment-entry
are not words. The six identification comment paragraphs spelled the same `~DOT+` body six times (`authorContent`,
`installationContent`, `dateWrittenContent`, `dateCompiledContent`, `securityContent`, `remarksContent`); they are now ONE
rule, `commentEntry`, and the screen finds the non-word tokens by that one context, so a seventh paragraph needs no arm.

**Goldens.** `pb3845-nbsp-word-unparseable` (2002-2023, COBOL2773), `pb3845-extended-letter-unparseable-85` and
`pb3845-underscore-unparseable-85` (COBOL0900), `pb3845-removed-character-unparseable-2023` (COBOL0902), and
`WordCharacterScreenTests` for what a golden cannot hold (a comment-entry is not words at all four editions; a distinct
word is reported once; `--permissive` keeps the warning next to the parse error; a phrase-word position is screened).

**Behaviour change.** The reports are frontend errors, so a source with an offending word now stops before the binder, as
every parse-stage diagnostic does; the binder's undefined-name errors no longer accompany them (the EditionGateArm
"gate survives the bind" fact is unaffected: the gate is earlier, not later).

**Rows.** None closed: the rule's inventory rows were closed by PB1402; this names the cause when the parse fails.
