## Entry 2845 — 2026-10-31 00:30 PDT — COBOL5506 names the keywords the compile edition has, and the §8.10 half stays open (PB4174)

**PB4174** (wave 20261783, group F): COBOL5506's list of what is expected was edition-blind. The parse is the
superset of the four editions, and `AdmittedKinds.Name` listed whatever the parse took.

**Re-probe.** `COMPUTE W-N = (W-N + 1.` at each `--std` (conformance `l1c17-paren-unclosed`) named
`B-AND, B-OR, B-SHIFT-L, B-SHIFT-LC, B-SHIFT-R, B-SHIFT-RC, B-XOR` at 85, 2002, 2014 and 2023 alike. §8.9 reserves the
boolean operators from 2002 and the shift operators from 2023 (Annex E.2 3), and §8.3.2.1 1) makes a word not yet
reserved a user-defined one. `PROGRAM-ID. X IS GLOBAL.` named PROTOTYPE at 85 and `USE PROCEDURE` named EC and EO at 85
(2002 words). `ADD 1 TO W ROUNDED MODE IS FOO.` at 85 named the eight ROUNDED MODE words, a 2014 phrase.

**Fix shape.** `ReservedWords.AdmitsAsKeyword(word, edition)` is false for one shape the §8.9 table proves: not
reserved at the edition, not reserved at any older edition, reserved at a later one. A word an older edition reserved
and this one dropped (WORDS at 2023, `COBOL` and `PHYSICAL`, which 2023 formats underline although its §8.9 omits them),
a row below high confidence and a word with no row are all kept: wrongly naming a word costs a sentence, wrongly
omitting one costs the user the right keyword. `CobolParserCoreBase.PrintsKeyword` adds the two kinds of unreserved
keyword, the §8.10 context-sensitive words and the `keywordWhereFree` words, the latter through a new generated
`CobolLexer.IsKeywordWhereFree` (`gen-cobol-words.ps1`, from the rows that already carry the determination, with a drift
test both ways). `AdmittedKinds.Refusal` takes the test and applies it to the predicate-tested words and the token
spellings. The edition read is a recorded `WordStatus` site in `EditionReadDriftTests`.

**What stays open.** The ROUNDED MODE names are §8.10 context-sensitive words, which carry no per-edition flag, and the
repository holds no 1985/2002/2014 text. The GnuCOBOL per-standard lists that fed §8.9 are no safe source for them
(measured: the 2014 list omits `NEAREST-EVEN`, the 2002 and 2014 lists omit the `LC_` words), so those words are still
named at every edition. PB4174 is re-scoped to that residue and stays open; CONFORMANCE.md D-EXPECT and
DESIGN-frontend-grammar record both halves. Incidentally, the COBOL5506 descriptor comment now says "the parse expects",
the correction PB4175 asked for.

**Goldens.** `8.3.2-COBOL-words/pb4174-expected-words-are-the-editions-85|2002|2023` (refused at 85 / 2002 and 2014 /
2023, each naming exactly the edition's boolean operators), `WrittenKindRefusalTests` (the same list at four
editions, PROGRAM-ID and USE at 85 against 2002, and the `AdmitsAsKeyword` table), `CobolWordsDriftTests`
(`IsKeywordWhereFree` against the rows). Three existing refusals (`l1c17-paren-unclosed`,
`pb1507-program-id-global-attribute`, `pb332-use-format1-needs-exception-or-error`) assert the sentence up to the first
edition-dependent word, because one `expect` substring serves all four editions.

**Self-review.** Architecture: the edition rule lives with the §8.9 table (`ReservedWords`), the parser base asks it
once through the word funnel, the strategy passes a delegate; no table of statements. Full code: the sentence cites
§8.3.2.1 and §8.9, which `cite.py --check` confirmed; the sibling enumerations (figurative-family hint, closed-format
residue) name no superset expected set. Performance: error path only, one dictionary lookup per named word.
Duplication: `keywordWhereFree` is read from the same rows as the keyword rules, not restated.

