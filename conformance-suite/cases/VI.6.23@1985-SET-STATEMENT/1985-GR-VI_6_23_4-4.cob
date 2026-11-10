      *>--- conformance-case
      *> id: 1985-GR-VI_6_23_4-4
      *> rules: [1985-GR-VI.6.23.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.23.4 GR4 (THE SET STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SETUPBY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  STEP-VALUES   PIC X(5) VALUE "21111".
       01  STEP-TABLE REDEFINES STEP-VALUES.
           05  ST        PIC 9 OCCURS 5 TIMES INDEXED BY IX1 IX2.
       01  OUT-LINE.
           05  N1        PIC 9.
           05  FILLER    PIC X VALUE SPACE.
           05  N2        PIC 9.
       PROCEDURE DIVISION.
       P1.
           SET IX1 TO 1.
           SET IX2 TO 1.
           SET IX1 IX2 UP BY ST (IX1).
           SET N1 TO IX1.
           SET N2 TO IX2.
           DISPLAY OUT-LINE.
           STOP RUN.
