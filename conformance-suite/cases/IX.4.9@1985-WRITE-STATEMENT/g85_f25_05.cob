      *>--- conformance-case
      *> id: g85_f25_05
      *> rules: [1985-GR-IX.4.9.4-14]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.9.4 GR14 - in sequential access mode records are
      *> released in ascending prime key order: in OUTPUT, K001 and
      *> K003 succeed, K002 (below K003) is refused with the invalid
      *> key condition (GR17a) and status 21 (§1985:IX.1.3.4, the
      *> ascending sequence requirement violated), and K004 then
      *> succeeds. In EXTEND (GR14's level-2 sentence) the first record
      *> must exceed the highest key in the file (K004): K002 is
      *> refused with 21, K005 and K006 succeed. The final listing
      *> shows only the released records. Fails if an out-of-order
      *> record is accepted or an in-order one refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2505.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX-FILE ASSIGN TO F2505IX
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IX-KEY
               FILE STATUS IS WS-STAT.
       DATA DIVISION.
       FILE SECTION.
       FD  IX-FILE
           LABEL RECORDS ARE STANDARD.
       01  IX-REC.
           05 IX-KEY   PIC X(4).
           05 IX-DATA  PIC X(6).
       WORKING-STORAGE SECTION.
       01  WS-STAT PIC XX.
       01  WS-EOF  PIC X VALUE "N".
       01  WS-KEY  PIC X(4).
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT IX-FILE.
           MOVE "K001" TO IX-KEY WS-KEY.
           MOVE "VAL001" TO IX-DATA.
           WRITE IX-REC
               INVALID KEY DISPLAY "W " WS-KEY " IK " WS-STAT
               NOT INVALID KEY DISPLAY "W " WS-KEY " OK " WS-STAT
           END-WRITE.
           MOVE "K003" TO IX-KEY WS-KEY.
           MOVE "VAL003" TO IX-DATA.
           WRITE IX-REC
               INVALID KEY DISPLAY "W " WS-KEY " IK " WS-STAT
               NOT INVALID KEY DISPLAY "W " WS-KEY " OK " WS-STAT
           END-WRITE.
           MOVE "K002" TO IX-KEY WS-KEY.
           MOVE "VAL002" TO IX-DATA.
           WRITE IX-REC
               INVALID KEY DISPLAY "W " WS-KEY " IK " WS-STAT
               NOT INVALID KEY DISPLAY "W " WS-KEY " OK " WS-STAT
           END-WRITE.
           MOVE "K004" TO IX-KEY WS-KEY.
           MOVE "VAL004" TO IX-DATA.
           WRITE IX-REC
               INVALID KEY DISPLAY "W " WS-KEY " IK " WS-STAT
               NOT INVALID KEY DISPLAY "W " WS-KEY " OK " WS-STAT
           END-WRITE.
           CLOSE IX-FILE.
           OPEN EXTEND IX-FILE.
           MOVE "K002" TO IX-KEY WS-KEY.
           MOVE "VAL002" TO IX-DATA.
           WRITE IX-REC
               INVALID KEY DISPLAY "E " WS-KEY " IK " WS-STAT
               NOT INVALID KEY DISPLAY "E " WS-KEY " OK " WS-STAT
           END-WRITE.
           MOVE "K005" TO IX-KEY WS-KEY.
           MOVE "VAL005" TO IX-DATA.
           WRITE IX-REC
               INVALID KEY DISPLAY "E " WS-KEY " IK " WS-STAT
               NOT INVALID KEY DISPLAY "E " WS-KEY " OK " WS-STAT
           END-WRITE.
           MOVE "K006" TO IX-KEY WS-KEY.
           MOVE "VAL006" TO IX-DATA.
           WRITE IX-REC
               INVALID KEY DISPLAY "E " WS-KEY " IK " WS-STAT
               NOT INVALID KEY DISPLAY "E " WS-KEY " OK " WS-STAT
           END-WRITE.
           CLOSE IX-FILE.
           OPEN INPUT IX-FILE.
           MOVE "N" TO WS-EOF.
           PERFORM UNTIL WS-EOF = "Y"
               READ IX-FILE
                   AT END DISPLAY "EOF"
                          MOVE "Y" TO WS-EOF
                   NOT AT END DISPLAY "REC " IX-REC
               END-READ
           END-PERFORM.
           CLOSE IX-FILE.
           STOP RUN.
