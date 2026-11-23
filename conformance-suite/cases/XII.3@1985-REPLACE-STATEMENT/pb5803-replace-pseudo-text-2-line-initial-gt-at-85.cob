      *>--- conformance-case
      *> id: pb5803-replace-pseudo-text-2-line-initial-gt-at-85
      *> rules: [1985-SR-XII.3.3-4]
      *> accept: [85]
      *> source-format: fixed
      *>---
      *> kb/Work PB5803. §1985:XII.3.3 SR4: "Pseudo-text-2 may contain zero,
      *> one, or more text words" (cite.py --check: OK). COBOL-85 has no
      *> compiler directive lines, so the line ">>XYZ" that pseudo-text-2
      *> holds below is one text word (§1985:III.2 GRL4.3), and the REPLACE
      *> statement is legal. Its pseudo-text-1 matches nothing, so the
      *> program DISPLAYs "[OK  ]". The leg can fail: the edition-blind
      *> directive recognizer refused the statement (COBOL2697, COBOL2572)
      *> and nothing was run.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5803B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(4) VALUE "OK".
       PROCEDURE DIVISION.
           REPLACE ==NEVERMATCHED== BY ==
           >>XYZ
           ==.
       P1.
           DISPLAY "[" W "]".
           STOP RUN.
