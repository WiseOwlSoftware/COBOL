## COPY processor: the edition's text-word limit, character repertoire and identifier subscripts; a comment-entry a REPLACING phrase produces

Three notes of one root, `CopyProcessor.cs`, fixed in harm order (wave 20262057, group D).

**PB5732 (1985).** The note blamed the position of the nested-COPY gate (library text tested before its REPLACING is
applied). Re-probe at `--std 85` (`AUTHZ.cpy` = `PLACEHOLDER. COPY ZZZ.`, `COPY AUTHZ REPLACING ==PLACEHOLDER== BY ==AUTHOR==`)
gave COBOL0900, CBL3620 and COBOL0001, as filed. Moving the test would change nothing: a REPLACING phrase can neither write
nor remove the word COPY of library text (`ForbiddenIn` refuses the one, COPY is no text-word to match for the other), so the
library text and the produced text differ only in what a comment-entry is. The converter read the entries of the text as
written (§2023:6.5: conversion before replacing) and `PLACEHOLDER.` heads none. Fix: `NormalizeCopybookMapped` is handed the
spelling the REPLACING phrase gives a word (`ParagraphWordAsReplaced`, the one replacing action run on the word alone) and the
two converters ask `IsCommentEntryParagraph` of THAT spelling (§1985:XII.2.3 SR10). The library text then holds no COPY, GR12
is met, and nothing is looked up. Witnesses: `pb5732-copy-replacing-produces-comment-entry` (accepted at 85, prints `X=3`;
`PB5732Z` is a second DATA DIVISION header that would break the program if copied) and its twin
`pb5732-copy-replacing-leaves-nested-copy-refused` (REPLACING matches nothing, so COBOL0900). The repro as filed still
draws COBOL3734 at 85: pseudo-text-2 is written in area B on the COPY line, which the area-A rule of the placed text reads
(a different rule, not touched); written with `==AUTHOR==` on a line of its own it runs. Sibling NOT fixed: a `REPLACE`
statement of the source text that writes the header word (`REPLACE ==PLACEHOLDER== BY ==AUTHOR==. PLACEHOLDER. COPY ZZZ.`)
still expands the COPY, because the converter reads a source line before any REPLACE statement is found; reported as a lead.
`docs/conformance/3/D-CE2.md` carries the determination.

**PB5700 (2002).** One constant, `MaxTextWordLength = 65,535`, served every edition. It is now `EditionInfo.MaxTextWordLength`
(322 at 2002, §2002:7.1.2.2 SR11 and §2002:7.1.3.2 SR9; 65,535 from 2014; 1985 only "must allow" 322 and keeps 65,535, the
latitude the two 1985 rows already recorded), asked by `OperandScreen.TextWordTooLong` for pseudo-text, library text and REPLACE
alike. `OperandScreen` now holds the edition (its optional edition was null only for the library-text length check). A COPY
REPLACING identifier operand keeps §2002:7.1.2.2 SR9 / §2014:7.2.2.2 SR9: `ReplacingIdentifierSubscripts` finds the first
subscript group holding an arithmetic expression (`T(I * 2)`, `T(I + J)`); a literal, an identifier with its qualifiers, an
identifier plus or minus an integer, ALL, reference modifiers and function argument lists pass. The screen applies at 2002,
2014 and 2023 under `--permissive` (the removed form still accepted), as the word operand's SR10 does.

**PB5608 (2002).** Below 2014 a text-word is "a sequence of contiguous COBOL characters" (§1985:III.2, §2002:7.1.1.4), from
2014 one from the compile-time coded character set. `OperandScreen.TextWordCharacters` refuses (COBOL2572) a character-string
text-word of an operand holding a character outside the edition's repertoire (`CobolCharacterRepertoire.IsOutsideRepertoire`:
beyond ASCII, and at 2002 not an Annex B extended letter; every one at 85). `REPLACE LEADING ==XX== BY ==Y¤Z==` is now
COBOL2572 at 2002 and 85 and still accepted at 2014 and 2023; `==Yé==` stays accepted at 2002.

Goldens (all in `conformance-suite/cases/`): `pb5700-copy-pseudo-text-word-322-accepted` / `-323-refused`,
`pb5700-copy-library-text-word-323-refused`, `pb5700-replace-pseudo-text-word-322-accepted` / `-323-refused`,
`pb5700-copy-replacing-subscript-offset-accepted` / `-arithmetic-refused` (2002 and 2014),
`pb5608-replace-partial-word-2-one-text-word-accepted` / `-non-cobol-character-refused`, and the two PB5732 cases above, each
refusal with its `wiseowl/diagnostics/<id>.json`. Unit tests: `TextManipulationStatementTests` (length by edition and place,
subscript shapes, repertoire by edition). Rows: 2002-SR-7.1.2.2-11, 2002-SR-7.1.2.2-9, 2002-SR-7.1.3.2-9, 2002-SR-7.1.3.2-6 and
1985-GR-XII.2.4-12 recorded CONFORMS (2002 GAP 1805 to 1801, 1985 GAP 780 to 779); 1985-SR-XII.2.3-8 and 1985-SR-XII.3.3-6
re-sited to `EditionInfo.cs#MaxTextWordLength`. No diagnostic code was needed (COBOL2572, COBOL0900 reused).

Self-review found nothing further: the 85 text-word character rule was added for COBOL-85 too (same definition) and is covered
by the unit test, not a golden.
