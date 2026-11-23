      *>--- conformance-case
      *> id: pb5803-replace-pseudo-text-1-line-initial-gt-source-at-85
      *> rules: [1985-SR-XII.3.3-3]
      *> accept: [85]
      *> source-format: fixed
      *>---
      *> kb/Work PB5803. §1985:XII.3.3 SR3: "Pseudo-text-1 must contain one or
      *> more text words" (cite.py --check: OK). COBOL-85 has no compiler
      *> directive lines, so the line ">>SOURCE X" that pseudo-text-1 holds
      *> below is the text words ">>SOURCE" and "X" (§1985:III.2 GRL4.3,
      *> cite.py --check: OK), and the REPLACE statement is legal. It
      *> matches nothing, so the program DISPLAYs "[OK  ]". The leg can
      *> fail: the edition-blind logical conversion discarded the >>SOURCE
      *> line as a SOURCE FORMAT directive and the statement was refused
      *> (COBOL0900, COBOL2697, COBOL2572) and nothing was run.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5803D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(4) VALUE "OK".
       PROCEDURE DIVISION.
           REPLACE ==NEVERMATCHED
           >>SOURCE X
           == BY ==MOVE "HIT" TO W==.
       P1.
           DISPLAY "[" W "]".
           STOP RUN.
