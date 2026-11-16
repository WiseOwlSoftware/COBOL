      *>--- conformance-case
      *> id: g85_bf21_02
      *> rules: [1985-GR-VII.4.7.4-12, 1985-GR-VII.4.7.4-13]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.7.4 GR12 - the successor relationship is set by
      *> the order of the WRITEs that create the file (DELTA, ALPHA,
      *> CHARLIE: not collating order), a REWRITE in place does not
      *> change it, and only records added at the end extend it: the
      *> "A" lines. A file that reorders on WRITE or moves a rewritten
      *> record fails.
      *> §1985:VII.4.7.4 GR13 - in extend mode WRITE adds records at the
      *> end as though in output mode, the first one (BRAVO) being the
      *> successor of the last record (CHARLIE): "A 4"; on an existing
      *> file with no records the extend WRITE gives the file its first
      *> record: the "B" lines. Overwriting from the start, or losing
      *> the old records, changes the "A" lines.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2102.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SEQ-A ASSIGN TO "BF2102A.DAT"
               ORGANIZATION IS SEQUENTIAL.
           SELECT SEQ-B ASSIGN TO "BF2102B.DAT"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  SEQ-A.
       01  A-REC                PIC X(8).
       FD  SEQ-B.
       01  B-REC                PIC X(8).
       WORKING-STORAGE SECTION.
       01  WS-CNT               PIC 9 VALUE 0.
       01  WS-EOF               PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT SEQ-A.
           MOVE "DELTA" TO A-REC.
           WRITE A-REC.
           MOVE "ALPHA" TO A-REC.
           WRITE A-REC.
           MOVE "CHARLIE" TO A-REC.
           WRITE A-REC.
           CLOSE SEQ-A.
           OPEN I-O SEQ-A.
           READ SEQ-A AT END DISPLAY "UNEXPECTED END 1".
           READ SEQ-A AT END DISPLAY "UNEXPECTED END 2".
           MOVE "ALPHA-RW" TO A-REC.
           REWRITE A-REC.
           CLOSE SEQ-A.
           OPEN EXTEND SEQ-A.
           MOVE "BRAVO" TO A-REC.
           WRITE A-REC.
           MOVE "ECHO" TO A-REC.
           WRITE A-REC.
           CLOSE SEQ-A.
           OPEN INPUT SEQ-A.
           MOVE 0 TO WS-CNT.
           MOVE "N" TO WS-EOF.
           PERFORM READ-A UNTIL WS-EOF = "Y".
           CLOSE SEQ-A.
           OPEN OUTPUT SEQ-B.
           CLOSE SEQ-B.
           OPEN EXTEND SEQ-B.
           MOVE "ONLY" TO B-REC.
           WRITE B-REC.
           CLOSE SEQ-B.
           OPEN INPUT SEQ-B.
           MOVE 0 TO WS-CNT.
           MOVE "N" TO WS-EOF.
           PERFORM READ-B UNTIL WS-EOF = "Y".
           CLOSE SEQ-B.
           STOP RUN.
       READ-A.
           READ SEQ-A
               AT END
                   MOVE "Y" TO WS-EOF
                   DISPLAY "A END AFTER " WS-CNT
               NOT AT END
                   ADD 1 TO WS-CNT
                   DISPLAY "A " WS-CNT " [" A-REC "]"
           END-READ.
       READ-B.
           READ SEQ-B
               AT END
                   MOVE "Y" TO WS-EOF
                   DISPLAY "B END AFTER " WS-CNT
               NOT AT END
                   ADD 1 TO WS-CNT
                   DISPLAY "B " WS-CNT " [" B-REC "]"
           END-READ.
