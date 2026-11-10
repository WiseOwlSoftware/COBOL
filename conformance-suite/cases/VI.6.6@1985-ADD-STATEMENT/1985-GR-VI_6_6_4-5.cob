      *>--- conformance-case
      *> id: 1985-GR-VI_6_6_4-5
      *> rules: [1985-GR-VI.6.6.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.6.4 GR5 (THE ADD STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ADDGR5.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  A PIC 9V99 VALUE 1.25.
       01  B-GROUP.
           05  B PIC 99V9 VALUE 10.0.
       01  C-GROUP.
           05  C PIC 9 VALUE 9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           ADD A TO B ROUNDED.
           DISPLAY B-GROUP.
           ADD 1 TO C ON SIZE ERROR DISPLAY "SIZE ERROR".
           DISPLAY C-GROUP.
           STOP RUN.
