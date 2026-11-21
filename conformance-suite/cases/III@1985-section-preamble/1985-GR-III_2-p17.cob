      *>--- conformance-case
      *> id: 1985-GR-III_2-p17
      *> rules: [1985-GR-III.2-p17]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p17 ((section preamble)). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-99.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. BLKFILE.
      * 1985 III.2 p17: block size has no direct relationship to the
      * size of the file. 3 records, BLOCK CONTAINS 5 RECORDS: the
      * file read back holds exactly 3 records, not a block of 5.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "BLKFILE.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  F1 BLOCK CONTAINS 5 RECORDS.
       01  F1-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01  WS-N   PIC 9 VALUE 0.
       01  WS-EOF PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT F1.
           MOVE "AAAA" TO F1-REC. WRITE F1-REC.
           MOVE "BBBB" TO F1-REC. WRITE F1-REC.
           MOVE "CCCC" TO F1-REC. WRITE F1-REC.
           CLOSE F1.
           OPEN INPUT F1.
           PERFORM UNTIL WS-EOF = "Y"
               READ F1 AT END MOVE "Y" TO WS-EOF
                   NOT AT END ADD 1 TO WS-N
               END-READ
           END-PERFORM.
           CLOSE F1.
           IF WS-N = 3
               DISPLAY "BLKFILE OK 3"
           ELSE
               DISPLAY "BLKFILE FAIL " WS-N
           END-IF.
           STOP RUN.
