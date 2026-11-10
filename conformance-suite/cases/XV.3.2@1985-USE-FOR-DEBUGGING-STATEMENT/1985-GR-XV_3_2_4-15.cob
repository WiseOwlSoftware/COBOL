      *>--- conformance-case
      *> id: 1985-GR-XV_3_2_4-15
      *> rules: [1985-GR-XV.3.2.4-15]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XV.3.2.4 GR15 (THE USE FOR DEBUGGING STATEMENT). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the confirmation pass's redraft (WEAK
      *> original) by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DBGCONT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. ANY-COMPUTER WITH DEBUGGING MODE.
       OBJECT-COMPUTER. ANY-COMPUTER.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT WIDE-FILE ASSIGN TO "WIDE".
       DATA DIVISION.
       FILE SECTION.
       FD  WIDE-FILE.
       01  WIDE-REC.
           02  FILLER    PIC X(149).
           02  WIDE-LAST PIC X.
       WORKING-STORAGE SECTION.
       01  SRC.
           02  FILLER PIC X(199) VALUE ALL "A".
           02  FILLER PIC X VALUE "Z".
       01  BIG PIC X(200) VALUE SPACES.
       01  SMALL PIC X VALUE SPACE.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE FOR DEBUGGING ON BIG SMALL WIDE-FILE.
       D1-A.
           IF DEBUG-NAME = "BIG"
               IF DEBUG-CONTENTS (1:200) = SRC
                   DISPLAY "BIG   200 [" DEBUG-CONTENTS (198:3) "]"
               ELSE
                   DISPLAY "BIG   NOT ITS CONTENT".
           IF DEBUG-NAME = "SMALL"
               DISPLAY "SMALL   1 [" DEBUG-CONTENTS (1:3) "]".
           IF DEBUG-NAME = "WIDE-FILE"
               DISPLAY "RECORD 150 [" DEBUG-CONTENTS (148:3) "]".
       END DECLARATIVES.
       M SECTION.
       M1.
           MOVE SRC TO BIG.
           MOVE "S" TO SMALL.
           OPEN OUTPUT WIDE-FILE.
           MOVE ALL "R" TO WIDE-REC.
           MOVE "W" TO WIDE-LAST.
           WRITE WIDE-REC.
           CLOSE WIDE-FILE.
           OPEN INPUT WIDE-FILE.
           READ WIDE-FILE AT END DISPLAY "AT END".
           CLOSE WIDE-FILE.
           STOP RUN.
