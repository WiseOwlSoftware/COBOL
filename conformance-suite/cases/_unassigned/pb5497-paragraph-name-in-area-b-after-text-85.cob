      *>--- conformance-case
      *> id: pb5497-paragraph-name-in-area-b-after-text-85
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5497 - §1985:IV.7.3.3: a paragraph-name "starts in
      *> area A". Y below follows the paragraph X and starts in position
      *> 12, area B, so at --std 85 it is COBOL3734 (the position-11
      *> positive is pb5497-paragraph-name-in-area-a-after-text-85).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB5497BP.
000300 PROCEDURE DIVISION.
000400 P0.
000500     PERFORM Y.
000600     STOP RUN.
000700 X.  Y.
000800     DISPLAY "IN Y".
