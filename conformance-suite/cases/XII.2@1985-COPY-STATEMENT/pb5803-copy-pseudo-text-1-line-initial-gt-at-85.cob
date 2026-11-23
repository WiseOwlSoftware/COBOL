      *>--- conformance-case
      *> id: pb5803-copy-pseudo-text-1-line-initial-gt-at-85
      *> rules: [1985-SR-XII.2.3-3]
      *> accept: [85]
      *> files: [PB5803L.cpy]
      *> source-format: fixed
      *>---
      *> kb/Work PB5803. §1985:XII.2.3 SR3: "Pseudo-text-1 must contain one or
      *> more text words" (cite.py --check: OK). COBOL-85 has no compiler
      *> directive lines, so ">>XYZ" at the head of a line is one text word
      *> (§1985:III.2 GRL4.3). Pseudo-text-1 below is that word, and the
      *> library text PB5803L is that word alone, so COPY ... REPLACING
      *> turns it into the statement the BY operand holds and the program
      *> DISPLAYs "[LIB ]". Each leg can fail: the edition-blind directive
      *> recognizer refused the COPY statement (COBOL2697, COBOL2449) and
      *> nothing was run; unreplaced, the word is a syntax error.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5803C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(4) VALUE "OK".
       PROCEDURE DIVISION.
       P1.
           COPY PB5803L REPLACING ==
           >>XYZ
           == BY ==MOVE "LIB" TO W==.
           DISPLAY "[" W "]".
           STOP RUN.
