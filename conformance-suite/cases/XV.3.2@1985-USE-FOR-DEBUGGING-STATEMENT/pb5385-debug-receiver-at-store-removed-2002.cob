      *>--- conformance-case
      *> id: pb5385-debug-receiver-at-store-removed-2002
      *> rules: [1985-GR-XV.3.2.4-14]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5385, the refusal beside
      *> pb5385-debug-receiver-identified-at-store. The debugging
      *> section whose DEBUG-SUB-1 the receiver's store identifies
      *> (§1985:XV.3.2.4 GR14, §1985:VI.6.19.4 GR2) is a 1985 rule: the
      *> USE FOR DEBUGGING declarative is not in the 2002 standard, so
      *> from 2002 on the same program is refused with COBOL0902.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5385R.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. ANY-COMPUTER WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T.
           05 NT PIC 9 OCCURS 5 TIMES.
       01  SUB PIC 9.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE FOR DEBUGGING ON NT.
       D1P.
           DISPLAY "D1 [" DEBUG-SUB-1 "]".
       END DECLARATIVES.
       M SECTION.
       M1.
           MOVE 1 TO SUB.
           MOVE 3 TO SUB NT (SUB).
           STOP RUN.
