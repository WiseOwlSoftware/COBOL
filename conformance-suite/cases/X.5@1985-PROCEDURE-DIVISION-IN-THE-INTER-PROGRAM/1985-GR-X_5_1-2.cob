      *>--- conformance-case
      *> id: 1985-GR-X_5_1-2
      *> rules: [1985-GR-X.5.1-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.1 GR2 (PROCEDURE DIVISION IN THE INTER-PROGRAM
      *> COMMUNICATION MODULE). An output test: the program must
      *> translate and run at 85 and DISPLAY exactly the lines its .out
      *> holds, the outcome the rule requires. Placed from written and
      *> run by the witness-drafting pass (a rule the audit never
      *> drafted) by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R85PD2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-G.
           05  WS-G-A PIC XX VALUE "12".
           05  WS-G-N PIC 9(4) VALUE 3456.
       01  WS-NUM PIC 9(6) VALUE 654321.
       01  WS-X PIC X(6) IS GLOBAL VALUE "ABCDEF".
       77  WS-77 PIC X(3) VALUE "777".
       PROCEDURE DIVISION.
       MAIN-1.
           CALL "R85PD2S" USING WS-G WS-NUM
               BY REFERENCE WS-X WS-77.
           DISPLAY "M OUT " WS-G-A " " WS-G-N " " WS-NUM " " WS-X
               " " WS-77.
           CALL "R85PD2S" USING WS-G WS-NUM
               BY REFERENCE WS-X WS-77.
           DISPLAY "M OUT " WS-G-A " " WS-G-N " " WS-NUM " " WS-X
               " " WS-77.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R85PD2S.
       DATA DIVISION.
       LINKAGE SECTION.
       01  LK-N PIC 9(6).
       01  LK-G.
           05  LK-G1 PIC X(3).
           05  LK-G2 PIC 9(3).
       01  LK-P.
           05  LK-P1 PIC XX.
           05  LK-P2 PIC XX.
           05  LK-P3 PIC XX.
       77  LK-77 PIC X(3).
       PROCEDURE DIVISION USING LK-N LK-G LK-P LK-77.
       SUB-1.
           DISPLAY "S IN  " LK-N " " LK-G1 " " LK-G2 " " LK-P1 "/"
               LK-P2 "/" LK-P3 " " LK-77.
           ADD 1 TO LK-N.
           MOVE "987" TO LK-G1.
           ADD 1 TO LK-G2.
           MOVE LK-P1 TO LK-P2.
           DISPLAY "S GLOBAL " WS-X.
           MOVE "SUB" TO LK-77.
       SUB-EXIT.
           EXIT PROGRAM.
       END PROGRAM R85PD2S.
       END PROGRAM R85PD2.
