      *>--- conformance-case
      *> id: phrase_order_arithmetic
      *> rules: [GR-14.9.2.4-6, GR-14.9.8.4-2, GR-14.9.26.4-4,
      *>    FMT-14.9.2.2, FMT-14.9.8.2, FMT-14.9.12.2, FMT-14.9.26.2,
      *>    FMT-14.9.44.2]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 5.2.6.4 — the ON SIZE ERROR / NOT ON SIZE ERROR pair is enclosed in CHOICE INDICATORS in every
      *> arithmetic statement's printed general format, so BOTH phrases may be written, each at most once,
      *> IN ANY ORDER. This golden pins the REVERSED order (NOT-then-ON) for all five arithmetic statements
      *> and proves each phrase still binds to its own role: the non-overflowing case must take the NOT
      *> branch, the overflowing case must take the ON branch.
      *> Accepted from 2002: COBOL-85 writes the phrases "in the sequence given in the general formats"
      *> (§1985:IV.2.1), ON before NOT, and ISO/IEC 1989:2002 Annex F.2 item 33 frees the order (kb/Work PB5158).
      *> Regression guard for the defect fixed 2026-07-19 (DEVLOG 927): the grammar admitted only ON-then-NOT,
      *> so every line below was rejected with COBOL0001 "no viable alternative at input 'ON'".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PHORDARITH.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SMALL   PIC 9(3) VALUE 1.
       01 BIG     PIC 9(3) VALUE 999.
       01 RES     PIC 9(3).
       PROCEDURE DIVISION.
       MAIN.
           ADD 1 TO SMALL
               NOT ON SIZE ERROR DISPLAY "ADD-NOT"
               ON SIZE ERROR DISPLAY "ADD-ON"
           END-ADD.
           ADD 500 TO BIG
               NOT ON SIZE ERROR DISPLAY "ADD2-NOT"
               ON SIZE ERROR DISPLAY "ADD2-ON"
           END-ADD.

           MOVE 5 TO SMALL.
           SUBTRACT 1 FROM SMALL
               NOT ON SIZE ERROR DISPLAY "SUB-NOT"
               ON SIZE ERROR DISPLAY "SUB-ON"
           END-SUBTRACT.

           MOVE 2 TO SMALL.
           MULTIPLY 3 BY SMALL
               NOT ON SIZE ERROR DISPLAY "MUL-NOT"
               ON SIZE ERROR DISPLAY "MUL-ON"
           END-MULTIPLY.

           MOVE 900 TO BIG.
           MULTIPLY 5 BY BIG
               NOT ON SIZE ERROR DISPLAY "MUL2-NOT"
               ON SIZE ERROR DISPLAY "MUL2-ON"
           END-MULTIPLY.

           MOVE 10 TO SMALL.
           DIVIDE 2 INTO SMALL
               NOT ON SIZE ERROR DISPLAY "DIV-NOT"
               ON SIZE ERROR DISPLAY "DIV-ON"
           END-DIVIDE.

           COMPUTE RES = 1 + 1
               NOT ON SIZE ERROR DISPLAY "CMP-NOT"
               ON SIZE ERROR DISPLAY "CMP-ON"
           END-COMPUTE.

           COMPUTE RES = 999 * 99
               NOT ON SIZE ERROR DISPLAY "CMP2-NOT"
               ON SIZE ERROR DISPLAY "CMP2-ON"
           END-COMPUTE.

           STOP RUN.
