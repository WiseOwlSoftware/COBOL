## PB5702: COBOL 2002 folds a user-defined word by its own Annex D, not the 2014 table

**Re-probe.** `01 X<U+2160>Y` and `01 X<U+2170>Y` (ROMAN NUMERAL ONE and its small form) at `--std 2002` drew COBOL1639, one name
declared twice. §2002:8.1.2.1 GR4c folds an uppercase extended letter by the mapping of Annex D, and 2002's Annex D (D.2.1,
D.2.2, 600 pairs) lists no mapping for U+2160, so 2002 has two names. 130 pairs of 2023's table touch a character that
2002's Annex C permits and are absent from 2002's Annex D (the Roman numerals, U+2126, U+212B, Georgian U+10A0..).

**Root cause.** `CobolNames` is one comparer holding 2023's Annex C; a word lexed under an earlier edition is rewritten to that
edition's key (`EditionSpelling`), which can say "this letter folds to that one" and cannot say "this letter is not folded
here" for a letter 2023 folds.

**Fix.** `scripts/spec/gen_annex_bc.py` transcribes 2002 Annex D into `CobolNames.D2002Mappings` (598 pairs applied, the two that
cross between a basic and an extended letter stay in `NotApplied`). `Fold(cp, level)` reads an edition's own list through
`Tables.Own`, an edition-keyed register (no inline edition year). `EditionSpelling` writes each character whose edition key
differs from 2023's as that key, or, when 2023 would still move the key, as one private-use stand-in per key (`Reserved`: U+E000 plus
the key's rank among the BMP characters 2023 folds, so the character keeps its UTF-16 width), which no Annex B or C permits in a
word. Because the lexer's text then holds a stand-in, `CobolWordRule.WrittenText` reads the user's characters back from the input
for the two readers that judge characters (`WordCharacterScreen`, `CobolErrorStrategy.APeriodLetsTheSentenceFollow`), so COBOL2773 still
names the character written. Annex D prints (0256,0189) beside (0189,0256); transcribed as printed, U+0189 and U+0256 are two letters at 2002
(2014 Annex E.2 item 14 lists the pair as deleted).

**Goldens.** `pb5702-annex-d-roman-numeral-is-not-folded-2002` (accepted at 2002, output `UP LO`) and
`pb5702-annex-d-roman-numeral-is-folded-2014` (refused at 2014 and 2023, COBOL1639). Unit: `D2002Mappings_EqualAnnexD_LessTheCrossingPairs`,
`EditionSpelling_GivesTheEditionsOwnNames`, `ReservedSpelling_IsInjective_UnfoldedAndOutsideEveryRepertoire`; the width test also holds the new list.

**Register.** `2002-GR-8.1.2.1-4` PARTIAL to CONFORMS (GAP 1133 to 1132). PB5702 closes.

**Left open (COBOL 2014 waits).** 2014 is still 2023's table with Annex E reversed, and its repertoire the same derivation (`Xµ` at `--std 2014`).

**Review round 1.** R1-1 (blocking): the ASSIGN operand's word read `GetText()`, so a 2002 stand-in (U+E000..) reached the emitted C# and the host file name. `CobolWordRule.WrittenText(ParserRuleContext)` reads the written text back from the input and `AssignTargetRule` uses it for both word arms; `AssignNameWrittenTextTests` (2002, 2014, 2023) holds the emitted C# carries the written word and no stand-in. R1-2 (lead): the design text now says which readers see the written text; diagnostics that print a spelling and the listing cross-reference still show the stand-in (report lead).
