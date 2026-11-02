## The 2002 and 2014 reserved-word flags follow the ISO texts

### PB4297: the 2002 and 2014 flags follow the ISO texts, not GnuCOBOL's implementor words

The compiler refused 19 words as data-names at `--std 2002` and 12 at `--std 2014` with COBOL0901 citing §8.9, though
the printed 2002 §8.9 list and the 2009 draft's §8.9 hold none of them. Among them were POSITION, PROCEED, WORDS,
COBOL, the DEBUG-n registers, SEGMENT-LIMIT, COMMUNICATION at 2014 and the draft's context-sensitive
NEGATIVE-INFINITY and POSITIVE-INFINITY. The re-probe of `G5/rw-position.cob` reproduced it at 2002 and 2014. The
cause was in the generator: GnuCOBOL's per-standard word lists decided both flags, and those lists also carry
implementor words. PB656 had made that choice while the repository held no 2002 text.

Two transcribed word lists now enter the repository (words only, sources in each file's `_comment`):
`tests/version-matrix/reserved-words-2002.json`, the 2002 standard's §8.9, and `words-2009-cd.json`, the 2009
draft's §8.9 and §8.10. `scripts/gen-reserved-words.ps1` takes the 2002 flag from the 2002 list. It adds ALIGNED,
which the same text's Annex F.1 item 6 adds and its §8.9 omits. The 2014 flag stays GnuCOBOL's, less every word the
draft knows that neither its §8.9 nor 2023's reserves. A 2014-final word the draft cannot speak to (PHYSICAL) keeps
its flag. The generator asserts both lists' shape, the 2002 list against GnuCOBOL's, and the premise of its older
85/2023 interpolation. The regenerated table changed exactly the note's 19 and 12 words.

Goldens: `pb4297_1985_words_user_defined_from_2002`, `pb4297_communication_user_defined_from_2014`,
`pb4297_infinity_words_user_defined`, and the negatives `pb4297-position-reserved-at-85` and
`pb4297-communication-reserved-85-2002`. The negative `pb656-cobol-reserved-through-2014` is deleted and its witness
retired from SR-8.3.2.1-2 (batch `w20261825a-pb4297`; GAP 19 → 19). D-RW3, VCR row 7.8, two construct rows and two
design docs describe the new sources.

### PB4307: a MULTIPLE FILE clause that names no file is refused

At `--std 85`, `MULTIPLE FILE TAPE.` and `MULTIPLE FILE.` compiled clean (re-probed), while `TAPE CONTAINS.` drew
COBOL0001. GnuCOBOL refuses both under its cobol85, ibm and mf dialects. `DataBinder.BindIoControl` now asks
`ScreenMultipleFileClause` beside SAME's syntax rules, and an empty file list is the new COBOL5944 at every edition.
The grammar keeps the list optional, because a required list would fall to the I-O-CONTROL error production's false
COBOL1970. The descriptor states that the 85 format is derived, not quoted. Negatives:
`_unassigned/pb4307-multiple-file-tape-names-no-file` and `pb4307-multiple-file-names-no-file`.

### PB4279: COPY's text-name and library-name obey the COBOL-word rule

`COPY PB4279U_.` and `COPY PB4279H-.` were accepted silently, with their members present (re-probed). A text-name is a
system-name (§8.3.2.3.1) and so a COBOL word (§8.3.2.1), but the lexer never sees the COPY statement.
`WordCharacterScreen.Check`'s per-word body became `WordCharacterScreen.CheckWord`, one rule for both readers.
`CopyProcessor.ScreenCopyWord` asks it, and the length rule, of text-name-1 and library-name-1 when they are written as
words. Negatives: `8.1.3-COBOL-character-repertoire/pb4279-copy-text-name-edge-underscore` and `…-edge-hyphen` give
COBOL2773 at every edition.

The first gate (`000001-20261102T152138Z`) was RED on four `Ansi85AcceptanceTests.Word_FreesAsUserWord_AtItsTableEdition`
rows. They pinned EVERY, CLOCK-UNITS, REFERENCES and PROCEDURES as reserved through 2014, which was PB4297's old
GnuCOBOL reading, so the rows now free the words at 2002.

### PB4305: fourteen 2002 Annex F.2 relaxations are refused at COBOL-85

All fourteen compiled at `--std 85` with no edition diagnostic (re-probed). Following the owner's decision of
2026-10-31, the 2002 Annex F.2 "formerly" statements decide the 85 edge, as D-SIGN2 and PB3683 already did. Each item
is now a `constructs.json` row (introducedIn 2002, COBOL0900), and VCR row 7.47 and determination D-F2-85 record them.
Each row is gated in one place:
- `VersionConformancePass`'s parse arm handles the eleven syntactic items: a signed DISPLAY literal, EXIT PROGRAM not
  last, GLOBAL in linkage, CONVERTING TO ALL, sentences before the first paragraph, an inline PERFORM AFTER, a RELATIVE
  KEY outside ACCESS MODE, an expression subscript, a reference-modified UNSTRING sender, USE without PROCEDURE, and
  VALUE in linkage, file or EXTERNAL.
- Its bound arm handles CALL USING of a group item at levels 02-49, adjudicated in `EditionGateArmDriftTests`.
- The binder handles the alphabet-name class condition and the qualified index-name, at their resolutions.

One correction to the evidence: probe p03 passes an elementary level-05 item, which item 10 allows at 85, so the
golden passes a level-05 group. Each item has a positive at 2002 and a negative at 85, and the negative expects its
own row's text.

Gate `000002-20261102T160241Z` was RED on 166 cases. Each is a program that the repository's own tests accept at 85
and that writes one of the relaxations; CCVS-85 stayed green. In 103 suite tests the program begins its procedure
division or a section with a sentence. Each of those now gets `PB4305-LEAD.` where the compiler names the sentence, so
it keeps its 85 coverage. The two pb3505 tests drop 85, because the signed DISPLAY literal is their subject. Twelve
85-only suite tests and about 55 C# cases remain for this branch's next link (kb/Work PB4305, "Gate fallout").
