      *>--- conformance-case
      *> id: pb5497-end-declaratives-after-text-85
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5497 - §1985:IV.7.5: DECLARATIVES and END
      *> DECLARATIVES "must each appear on a line by itself". END below
      *> starts in area A but follows the paragraph X on its line, so at
      *> --std 85 it is COBOL3734. Starting in area A is not enough for
      *> these two alone: a section header, a division header or an end
      *> marker that starts in area A after other text is accepted.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB5497ED.
000300 PROCEDURE DIVISION.
000400 DECLARATIVES.
000500 D1 SECTION.
000600     USE AFTER STANDARD ERROR PROCEDURE ON INPUT.
000700 X. END DECLARATIVES.
000800 M SECTION.
000900 P0.
001000     DISPLAY "OK".
001100     STOP RUN.
