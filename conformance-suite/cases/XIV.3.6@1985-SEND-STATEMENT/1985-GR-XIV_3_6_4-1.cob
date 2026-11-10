      *>--- conformance-case
      *> id: 1985-GR-XIV_3_6_4-1
      *> rules: [1985-GR-XIV.3.6.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.6.4 GR1 (THE SEND STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SENDFIX.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  MSG.
           02  MSG-A       PIC X(100) VALUE ALL "A".
           02  MSG-B       PIC X(50)  VALUE ALL "B".
       COMMUNICATION SECTION.
       CD OUT-CD FOR OUTPUT.
       01  OUT-REC.
           02  OUT-COUNT   PIC 9(4).
           02  OUT-LEN     PIC 9(4).
           02  OUT-STATUS  PIC XX.
           02  OUT-ERR     PIC X.
           02  OUT-DEST    PIC X(12).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 1 TO OUT-COUNT.
           MOVE 150 TO OUT-LEN.
           MOVE "PRINTER1" TO OUT-DEST.
           SEND OUT-CD FROM MSG WITH EMI.
           DISPLAY "SEND DONE".
           STOP RUN.
