      *>--- conformance-case
      *> id: 1985-GR-XV_3_2_4-23
      *> rules: [1985-GR-XV.3.2.4-23]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XV.3.2.4 GR23 (THE USE FOR DEBUGGING STATEMENT). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the confirmation pass's redraft (WEAK
      *> original) by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DBGGR23.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. ANY-COMPUTER WITH DEBUGGING MODE.
       OBJECT-COMPUTER. ANY-COMPUTER.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT MCS-DEFS ASSIGN TO "mcs.json".
           SELECT DFILE1 ASSIGN TO "DFILE1".
       DATA DIVISION.
       FILE SECTION.
       FD  MCS-DEFS.
       01  DEFS-REC PIC X(60).
       FD  DFILE1.
       01  DREC PIC X(5).
       WORKING-STORAGE SECTION.
       01  BUF PIC X(10).
       01  LINE-1 PIC X(6) VALUE SPACES.
       01  LINE-2 PIC X(6) VALUE SPACES.
       01  LINE-3 PIC X(6) VALUE SPACES.
       01  CNT    PIC 9 VALUE 0.
       COMMUNICATION SECTION.
       CD  CIN FOR INPUT.
       01  CIN-REC.
           02  IN-Q    PIC X(12).
           02  IN-SUBS PIC X(36).
           02  IN-DATE PIC 9(6).
           02  IN-TIME PIC 9(8).
           02  IN-SRC  PIC X(12).
           02  IN-LEN  PIC 9(4).
           02  IN-END  PIC X.
           02  IN-STAT PIC XX.
           02  IN-CNT  PIC 9(6).
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG-SECT SECTION.
           USE FOR DEBUGGING ON DFILE1 CIN.
       DBG-PARA.
           IF DEBUG-NAME = "CIN"
               ADD 1 TO CNT
               DISPLAY "[" DEBUG-NAME (1:10) "]"
               DISPLAY "[" DEBUG-CONTENTS (1:87) "]"
           ELSE
               DISPLAY "[" DEBUG-NAME (1:10) "] ["
                   DEBUG-CONTENTS (1:5) "]".
           IF DEBUG-NAME = "CIN" AND CNT = 1
               MOVE DEBUG-LINE TO LINE-1.
           IF DEBUG-NAME = "CIN" AND CNT = 2
               MOVE DEBUG-LINE TO LINE-2.
           IF DEBUG-NAME = "CIN" AND CNT = 3
               MOVE DEBUG-LINE TO LINE-3.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       SEED-MCS.
           OPEN OUTPUT MCS-DEFS.
           MOVE "{""queues"":[{""name"":""Q1"",""messages"":[{"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """segments"":[""HELLO""],""source"":""T1"","
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """date"":""851231"",""time"":""23595999""},{"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """segments"":[""BYE""],""source"":""T1"","
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """date"":""860101"",""time"":""00000100""}]}],"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """sources"":[{""name"":""T1""}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           OPEN OUTPUT DFILE1.
           MOVE "ABCDE" TO DREC.
           WRITE DREC.
           CLOSE DFILE1.
           OPEN INPUT DFILE1.
           READ DFILE1 AT END DISPLAY "AT END".
           CLOSE DFILE1.
           MOVE SPACES TO CIN-REC.
           MOVE "Q1" TO IN-Q.
           MOVE ZERO TO IN-CNT.
           PERFORM RCV-PARA 2 TIMES.
           ACCEPT CIN MESSAGE COUNT.
           IF LINE-1 = SPACES
               DISPLAY "DEBUG-LINE IS SPACES".
           IF LINE-1 = LINE-2
               DISPLAY "SAME STATEMENT, SAME DEBUG-LINE"
           ELSE
               DISPLAY "SAME STATEMENT, ANOTHER DEBUG-LINE".
           IF LINE-1 = LINE-3
               DISPLAY "ANOTHER STATEMENT, SAME DEBUG-LINE"
           ELSE
               DISPLAY "ANOTHER STATEMENT, ANOTHER DEBUG-LINE".
           STOP RUN.
       RCV-PARA.
           RECEIVE CIN MESSAGE INTO BUF
               NO DATA DISPLAY "NO DATA".
