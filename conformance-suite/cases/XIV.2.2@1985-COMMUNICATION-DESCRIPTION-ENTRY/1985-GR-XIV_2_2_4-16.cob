      *>--- conformance-case
      *> id: 1985-GR-XIV_2_2_4-16
      *> rules: [1985-GR-XIV.2.2.4-16]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.2.2.4 GR16 (THE COMMUNICATION DESCRIPTION ENTRY). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the confirmation pass's redraft (WEAK
      *> original) by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CDTEST.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  OUTCD FOR OUTPUT
           DESTINATION COUNT IS OUT-COUNT
           TEXT LENGTH IS OUT-LEN
           STATUS KEY IS OUT-STAT
           ERROR KEY IS OUT-ERR
           SYMBOLIC DESTINATION IS OUT-DEST.
       01  OUT-REC.
           02 REC-COUNT PIC X(4).
           02 REC-LEN   PIC X(4).
           02 REC-STAT  PIC XX.
           02 REC-ERR   PIC X.
           02 REC-DEST  PIC X(12).
       CD  TABCD FOR OUTPUT
           DESTINATION COUNT IS TAB-COUNT
           TEXT LENGTH IS TAB-LEN
           STATUS KEY IS TAB-STAT
           DESTINATION TABLE OCCURS 3 TIMES
           ERROR KEY IS TAB-ERR
           SYMBOLIC DESTINATION IS TAB-DEST.
       01  TAB-REC PIC X(49).
       PROCEDURE DIVISION.
       P1.
           MOVE 1 TO OUT-COUNT.
           MOVE 80 TO OUT-LEN.
           MOVE "AB" TO OUT-STAT.
           MOVE "E" TO OUT-ERR.
           MOVE "TERMINAL0001" TO OUT-DEST.
           DISPLAY OUT-REC.
           DISPLAY REC-COUNT.
           DISPLAY REC-LEN.
           DISPLAY REC-STAT.
           DISPLAY REC-ERR.
           DISPLAY REC-DEST.
           MOVE 3 TO TAB-COUNT.
           MOVE 7 TO TAB-LEN.
           MOVE "CD" TO TAB-STAT.
           MOVE "F" TO TAB-ERR (1).
           MOVE "FIRSTDEST001" TO TAB-DEST (1).
           MOVE "G" TO TAB-ERR (2).
           MOVE "SECONDDEST02" TO TAB-DEST (2).
           MOVE "H" TO TAB-ERR (3).
           MOVE "THIRDDEST003" TO TAB-DEST (3).
           DISPLAY TAB-REC.
           STOP RUN.
