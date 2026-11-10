      *>--- conformance-case
      *> id: 1985-SR-VI_6_27_3-3
      *> rules: [1985-SR-VI.6.27.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.27.3 SR3 (THE UNSTRING STATEMENT). A refusal: the
      *> rule forbids line 10 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL1626 there; a compiler that accepts it
      *> fails. Placed from written and run by the triage of the drafts
      *> that did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. UNSTR4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  SRC PIC X(9) VALUE "1234,ABCD".
       01  OUT-1 PIC 9(4) USAGE IS COMPUTATIONAL.
       01  OUT-2 PIC X(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           UNSTRING SRC DELIMITED BY "," INTO OUT-1 OUT-2.
           DISPLAY OUT-2.
           STOP RUN.
