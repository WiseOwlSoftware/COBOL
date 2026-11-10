      *>--- conformance-case
      *> id: 1985-GR-XIV_2_2_4-21
      *> rules: [1985-GR-XIV.2.2.4-21]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.2.2.4 GR21 (THE COMMUNICATION DESCRIPTION ENTRY). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GR21TST.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  MSG-AREA                PIC X(5) VALUE "HELLO".
       COMMUNICATION SECTION.
       CD  OUT-CD FOR OUTPUT.
       01  OUT-REC.
           02  OUT-COUNT           PIC 9(4).
           02  OUT-LEN             PIC 9(4).
           02  OUT-STATUS          PIC XX.
           02  OUT-ERR             PIC X.
           02  OUT-DEST            PIC X(12).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 0 TO OUT-COUNT.
           MOVE 5 TO OUT-LEN.
           MOVE "TERM1" TO OUT-DEST.
           SEND OUT-CD FROM MSG-AREA WITH EMI.
           DISPLAY OUT-STATUS.
           STOP RUN.
