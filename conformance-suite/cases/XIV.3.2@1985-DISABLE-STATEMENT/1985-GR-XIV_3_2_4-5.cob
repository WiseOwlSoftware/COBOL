      *>--- conformance-case
      *> id: 1985-GR-XIV_3_2_4-5
      *> rules: [1985-GR-XIV.3.2.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.2.4 GR5 (THE DISABLE STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DISIOT.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  IOCD FOR I-O
           MESSAGE DATE IS IO-DATE
           MESSAGE TIME IS IO-TIME
           SYMBOLIC TERMINAL IS IO-TERM
           TEXT LENGTH IS IO-LEN
           END KEY IS IO-END
           STATUS KEY IS IO-STAT.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "TERM01" TO IO-TERM.
           DISABLE I-O TERMINAL IOCD.
           DISPLAY "DISABLED".
           STOP RUN.
