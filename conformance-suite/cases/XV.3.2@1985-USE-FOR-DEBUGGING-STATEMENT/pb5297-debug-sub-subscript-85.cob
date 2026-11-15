      *>--- conformance-case
      *> id: pb5297-debug-sub-subscript-85
      *> rules: [1985-SR-XV.3.2.3-11]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5297. §1985:XV.3.2.3 SR11: "References to the
      *> special register DEBUG-ITEM are restricted to references from
      *> within a debugging section". DEBUG-SUB-1 is a member of
      *> DEBUG-ITEM, and here it is written as a subscript
      *> in paragraph P, outside the debugging section: refused at 85
      *> with COBOL6442, as MOVE DEBUG-SUB-1 TO W there is.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5297S.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMPUTER-1 WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TB VALUE "ABCDEFGHI".
           05 TE PIC X OCCURS 9.
       01  W PIC X(9) VALUE "ABCDEFGHI".
       01  V PIC X.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG SECTION.
           USE FOR DEBUGGING ON P.
       DBG-1.
           DISPLAY "D".
       END DECLARATIVES.
       MAIN SECTION.
       P.
           MOVE "A" TO TE (DEBUG-SUB-1).
           DISPLAY "END".
           STOP RUN.
