      *>--- conformance-case
      *> id: pb5924-broken-word-literal-picture-85
      *> rules: [1985-SR-VI.1.3.7-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:VI.1.3.7 (cite.py: OK):
      *> "In level 2 a word, numeric literal, or PICTURE character-string can be broken"
      *> A PICTURE string 9(6), the word WS-LONG and the numeric literal 1234
      *> each broken over a continuation line (level 2).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. BRK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-LONG                                                 PIC 9(
      -    6) VALUE ZERO.
       PROCEDURE DIVISION.
       P1.
                                                     MOVE 123450 TO WS-L
      -    ONG.
           DISPLAY WS-LONG.
                                                                  ADD 12
      -    34 TO WS-LONG.
           DISPLAY WS-LONG.
           STOP RUN.
