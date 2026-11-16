      *>--- conformance-case
      *> id: g85_f15_04
      *> rules: [1985-GR-IX.4.7.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.7.4 GR7 - transfer of control after START by the
      *> presence or absence of INVALID KEY and NOT INVALID KEY, per
      *> §1985:IX.1.3.5 GR2, GR3 and GRL2.2 (the invalid key
      *> condition); every invalid key status begins with 2
      *> (§1985:IX.1.3.4 GRL2.3), so CLASS shows 2.
      *> L1: both phrases, a record matches: NOT INVALID KEY runs.
      *> L2: both phrases, no record matches: INVALID KEY runs, NOT
      *> INVALID KEY is ignored and the USE procedure is not executed.
      *> L3: NOT INVALID KEY only, no match: the USE procedure runs,
      *> NOT INVALID KEY is ignored, control returns to the next
      *> statement (§1985:IX.4.8.4 GR6, status 2x is not critical).
      *> L4: INVALID KEY only, a match: INVALID KEY is ignored.
      *> L5: neither phrase, a match: control goes to the end of the
      *> statement and no USE procedure runs.
      *> L6: neither phrase, no match: the USE procedure runs.
      *> A leg fails if the wrong imperative runs, both run, the USE
      *> procedure runs when INVALID KEY is given or when the START
      *> succeeds, or control does not reach the next statement.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F1504.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX-FILE ASSIGN TO G85F15X
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-KEY
               FILE STATUS IS WS-STAT.
       DATA DIVISION.
       FILE SECTION.
       FD  IX-FILE
           LABEL RECORDS ARE STANDARD.
       01  IX-REC.
           05 IX-KEY  PIC X(4).
           05 IX-DATA PIC X(6).
       WORKING-STORAGE SECTION.
       01  WS-STAT.
           05 WS-S1 PIC X.
           05 WS-S2 PIC X.
       01  WS-USES PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       IX-ERR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON IX-FILE.
       IX-ERR-PARA.
           ADD 1 TO WS-USES.
           DISPLAY "USE " WS-USES " CLASS " WS-S1.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-PARA.
           OPEN OUTPUT IX-FILE.
           MOVE "A001" TO IX-KEY.
           MOVE "ONE" TO IX-DATA.
           WRITE IX-REC.
           MOVE "A003" TO IX-KEY.
           MOVE "THREE" TO IX-DATA.
           WRITE IX-REC.
           MOVE "A005" TO IX-KEY.
           MOVE "FIVE" TO IX-DATA.
           WRITE IX-REC.
           CLOSE IX-FILE.
           OPEN INPUT IX-FILE.
           DISPLAY "OPEN " WS-STAT " USES " WS-USES.
      * L1
           MOVE "A003" TO IX-KEY.
           START IX-FILE KEY IS EQUAL TO IX-KEY
               INVALID KEY DISPLAY "L1 INVALID"
               NOT INVALID KEY DISPLAY "L1 NOT INVALID " WS-STAT
           END-START.
           DISPLAY "L1 END".
      * L2
           MOVE "A004" TO IX-KEY.
           START IX-FILE KEY IS EQUAL TO IX-KEY
               INVALID KEY DISPLAY "L2 INVALID " WS-S1
               NOT INVALID KEY DISPLAY "L2 NOT INVALID"
           END-START.
           DISPLAY "L2 END USES " WS-USES.
      * L3
           MOVE "A009" TO IX-KEY.
           START IX-FILE KEY IS GREATER THAN IX-KEY
               NOT INVALID KEY DISPLAY "L3 NOT INVALID"
           END-START.
           DISPLAY "L3 END USES " WS-USES.
      * L4
           MOVE "A001" TO IX-KEY.
           START IX-FILE KEY IS GREATER THAN IX-KEY
               INVALID KEY DISPLAY "L4 INVALID"
           END-START.
           DISPLAY "L4 END " WS-STAT " USES " WS-USES.
      * L5
           MOVE "A005" TO IX-KEY.
           START IX-FILE KEY IS NOT LESS THAN IX-KEY.
           DISPLAY "L5 END " WS-STAT " USES " WS-USES.
      * L6
           MOVE "A006" TO IX-KEY.
           START IX-FILE KEY IS NOT LESS THAN IX-KEY.
           DISPLAY "L6 END USES " WS-USES.
           CLOSE IX-FILE.
           STOP RUN.
