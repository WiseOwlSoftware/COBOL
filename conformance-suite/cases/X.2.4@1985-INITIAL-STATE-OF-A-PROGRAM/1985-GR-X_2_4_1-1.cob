      *>--- conformance-case
      *> id: 1985-GR-X_2_4_1-1
      *> rules: [1985-GR-X.2.4.1-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.2.4.1 GR1 (INITIAL STATE OF A PROGRAM). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the witness-drafting pass (a rule the
      *> audit never drafted) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R85IS1.
       PROCEDURE DIVISION.
       MAIN-1.
           DISPLAY "CALL 1".
           CALL "R85IS1S".
           DISPLAY "CALL 2".
           CALL "R85IS1S".
           CANCEL "R85IS1S".
           DISPLAY "CALL 3 AFTER CANCEL".
           CALL "R85IS1S".
           DISPLAY "INITIAL 1".
           CALL "R85IS1I".
           DISPLAY "INITIAL 2".
           CALL "R85IS1I".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R85IS1S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  WS-A PIC X(4) VALUE "INIT".
       01  WS-N PIC 9(3) VALUE 123.
       01  WS-G.
           05  WS-G1 PIC XX VALUE "G1".
           05  WS-G2 PIC 99 VALUE 45.
       COMMUNICATION SECTION.
       CD  CD-OUT FOR OUTPUT.
       01  CD-REC.
           05  CD-COUNT PIC 9(4) VALUE 1.
           05  CD-LEN PIC 9(4) VALUE 12.
           05  CD-STAT PIC XX VALUE "ZZ".
           05  CD-ERR PIC X VALUE "E".
           05  CD-DEST PIC X(12) VALUE "DEST-INIT".
       PROCEDURE DIVISION.
       SUB-1.
           DISPLAY "S WS " WS-A " " WS-N " " WS-G1 " " WS-G2.
           DISPLAY "S CD " CD-COUNT " " CD-LEN " " CD-STAT " " CD-ERR
               " " CD-DEST ".".
           MOVE "USED" TO WS-A.
           MOVE 999 TO WS-N.
           MOVE "XX" TO WS-G1.
           MOVE 0 TO WS-G2.
           MOVE 7 TO CD-COUNT.
           MOVE 80 TO CD-LEN.
           MOVE "QQ" TO CD-STAT.
           MOVE "U" TO CD-ERR.
           MOVE "DEST-USED" TO CD-DEST.
       SUB-EXIT.
           EXIT PROGRAM.
       END PROGRAM R85IS1S.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R85IS1I IS INITIAL PROGRAM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WI-A PIC X(4) VALUE "INIT".
       01  WI-N PIC 9(3) VALUE 123.
       COMMUNICATION SECTION.
       CD  CD-OUT FOR OUTPUT.
       01  CD-REC.
           05  CD-COUNT PIC 9(4) VALUE 1.
           05  CD-LEN PIC 9(4) VALUE 12.
           05  CD-STAT PIC XX VALUE "ZZ".
           05  CD-ERR PIC X VALUE "E".
           05  CD-DEST PIC X(12) VALUE "DEST-INIT".
       PROCEDURE DIVISION.
       INI-1.
           DISPLAY "I WS " WI-A " " WI-N.
           DISPLAY "I CD " CD-COUNT " " CD-LEN " " CD-STAT " " CD-ERR
               " " CD-DEST ".".
           MOVE "USED" TO WI-A.
           MOVE 999 TO WI-N.
           MOVE 7 TO CD-COUNT.
           MOVE 80 TO CD-LEN.
           MOVE "QQ" TO CD-STAT.
           MOVE "U" TO CD-ERR.
           MOVE "DEST-USED" TO CD-DEST.
       INI-EXIT.
           EXIT PROGRAM.
       END PROGRAM R85IS1I.
       END PROGRAM R85IS1.
