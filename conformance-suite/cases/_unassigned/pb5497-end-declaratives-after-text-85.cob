      *>--- conformance-case
      *> id: pb5497-end-declaratives-after-text-85
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5497 - §1985:IV.7.5: DECLARATIVES and END
      *> DECLARATIVES "must each appear on a line by itself". END below
      *> starts in area A but follows the paragraph X on its line, so at
      *> --std 85 it is COBOL3734 (a header after text is legal only for
      *> a paragraph-name, §1985:IV.7.3.3).
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
