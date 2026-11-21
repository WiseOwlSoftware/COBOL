      *>--- conformance-case
      *> id: pb5803-replace-pseudo-text-1-line-initial-gt-at-85
      *> rules: [1985-SR-XII.3.3-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5803. §1985:XII.3.3 SR3: "Pseudo-text-1 must contain one or
      *> more text words" (cite.py --check: OK). COBOL-85 has no compiler
      *> directive lines, so a line that begins with two greater-than signs is
      *> text: ">>XYZ" is a sequence of contiguous COBOL characters bounded by
      *> separators, a text word (§1985:III.2 GRL4.3, cite.py --check: OK).
      *> Pseudo-text-1 below is that one text word, so the REPLACE statement
      *> is legal and replaces the ">>XYZ" word of the program text
      *> (§1985:XII.3.4 GR1: "Each matched occurrence of pseudo-text-1 in
      *> the source program is replaced by the corresponding pseudo-text-2",
      *> cite.py --check: OK): the program DISPLAYs "[HIT ]". Each leg
      *> can fail: the edition-blind directive recognizer refused the
      *> REPLACE statement (COBOL2572, COBOL2697) and nothing was run; a
      *> recognizer that left the word unreplaced would display "[OK  ]".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5803A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(4) VALUE "OK".
       PROCEDURE DIVISION.
           REPLACE ==
           >>XYZ
           == BY ==MOVE "HIT" TO W==.
       P1.
           >>XYZ
           DISPLAY "[" W "]".
           STOP RUN.
