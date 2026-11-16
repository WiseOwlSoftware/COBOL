      *>--- conformance-case
      *> id: g85_f21_01
      *> rules: [1985-GR-VII.4.4.4-2, 1985-GR-VII.4.4.4-14]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.4.4 GR2 - "The NEXT phrase is optional and has no
      *> effect on the execution of the READ statement": the reads
      *> R1 (NEXT), R2 (no NEXT) and R3 (NEXT) walk the three records
      *> in order with status 00, exactly as three plain READs would.
      *> A NEXT that skipped, repeated or failed changes R1..R3.
      *> §1985:VII.4.4.4 GR14 - "The END-READ phrase delimits the scope
      *> of the READ statement": in each IF the DISPLAY after END-READ
      *> is outside the READ. Leg 1 takes the at-end path; without the
      *> delimiting it would belong to NOT AT END and not run. Leg 2
      *> succeeds; without the delimiting it would belong to AT END and
      *> not run.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2101.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "G85F2101.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS WS-STAT.
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           LABEL RECORDS ARE STANDARD.
       01  F1-REC PIC X(3).
       WORKING-STORAGE SECTION.
       01  WS-STAT PIC XX.
       01  WS-FLAG PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT F1.
           MOVE "AAA" TO F1-REC.
           WRITE F1-REC.
           MOVE "BBB" TO F1-REC.
           WRITE F1-REC.
           MOVE "CCC" TO F1-REC.
           WRITE F1-REC.
           CLOSE F1.
           OPEN INPUT F1.
           READ F1 NEXT RECORD AT END DISPLAY "EOF-1".
           DISPLAY "R1 " F1-REC " " WS-STAT.
           READ F1 RECORD AT END DISPLAY "EOF-2".
           DISPLAY "R2 " F1-REC " " WS-STAT.
           READ F1 NEXT AT END DISPLAY "EOF-3".
           DISPLAY "R3 " F1-REC " " WS-STAT.
           IF WS-FLAG = 1
               READ F1 NEXT RECORD
                   AT END DISPLAY "AT END " WS-STAT
                   NOT AT END DISPLAY "NOT AT END"
               END-READ
               DISPLAY "AFTER END-READ 1"
           END-IF.
           CLOSE F1.
           OPEN INPUT F1.
           IF WS-FLAG = 1
               READ F1
                   AT END DISPLAY "UNEXPECTED AT END"
               END-READ
               DISPLAY "AFTER END-READ 2 " F1-REC
           END-IF.
           CLOSE F1.
           STOP RUN.
