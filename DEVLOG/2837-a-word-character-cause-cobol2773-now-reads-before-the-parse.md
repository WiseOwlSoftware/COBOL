## Entry 2837 — 2026-10-30 21:45 PDT — A word-character cause (COBOL2773) now reads before the parse error (COBOL0001) it explains

PB4131 (group C of wave 20261783). Re-probe on a fresh Debug build: golden `pb3845-nbsp-word-unparseable` printed
`(17,12) COBOL0001 cannot parse construct near 'STOP'` and then `(16,8) COBOL2773 ... U+00A0`, the effect before its cause
(reproduced; the cause sits on an earlier line than the effect, so the order was not even by position).

Root cause: PB3845's frontend screen (`WordCharacterScreen.RunOnTokens`) needs the failed parse's partial tree, to know which
tokens are a comment-entry's text, so `Frontend.LexAndParseOnce` runs it after `ParseToFixpoint` and the report order put its
diagnostic last. Diagnostics print in report order and nothing else orders them (a global sort by position would merge two
files' positions and reorder every multi-error golden of the SR7 stage, which report ahead of the parse by design).

Fix shape (one place, never per screen): `LexAndParseOnce` screens into a bag of its own and calls the new
`DiagnosticBag.AddBeforeEffects(causes, from)`, which places each report before the first parse report of the same file at or
after its position, a tie going to the cause; with none, it is appended. A parse error BEFORE the word stays before it, one
after stays after; reports before the parse's own start (`parseStart`) are never passed. The sibling arms were swept: the
screen is the only late-reporting stage with a cause (the §8.7.1 operator-context reports after the parse describe the
tree's operators, not why a parse failed; the SR7, literal, PICTURE and separator reports already precede the parse); the
binder-side screen (`RunOnTree`) is on a parse that succeeded, so it has no effect to precede.

Tests: `WordCharacterScreenTests.TheWordCharacterCause_ReadsBeforeTheParseErrorItExplains` (four editions; a parse error
before, at and after the word keeps position order) and `..._OnItsOwnLine_ReadsBeforeTheParseErrorOfTheNextLine` (the
golden's shape). The refusal goldens compare a code, not an order, so the unit tests are the pin. Design doc
`DESIGN-frontend-grammar.md` (where the legality is asked) records the order.

Rows re-verdicted: none (`closes_rows: []`; a diagnostic-quality fix).

