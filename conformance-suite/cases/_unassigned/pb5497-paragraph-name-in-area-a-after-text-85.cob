      *>--- conformance-case
      *> id: pb5497-paragraph-name-in-area-a-after-text-85
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5497 - §1985:IV.7.3.3: "The paragraph header or
      *> paragraph-name starts in area A of any line following the first
      *> line of a division or a section." Area A is positions 8-11, so Y
      *> below, which starts in position 11 after the paragraph X, starts
      *> in area A and the program is conforming at --std 85. Expected:
      *> P0 performs Y, which displays IN Y; X is an empty paragraph.
      *> A refusal of Y as "following other text of its line" fails the
      *> compile (the position-12 negative is
      *> pb5497-paragraph-name-in-area-b-after-text-85).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB5497AP.
000300 PROCEDURE DIVISION.
000400 P0.
000500     PERFORM Y.
000600     STOP RUN.
000700 X. Y.
000800     DISPLAY "IN Y".
