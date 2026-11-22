      *>--- conformance-case
      *> id: 1985-GR-III_2-p244
      *> rules: [1985-GR-III.2-p244]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p244 ((section preamble)). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-99.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * Probe for 1985-GR-III.2-p244 (record number = ordinal number
      * of a record in a sequential file) as READ GR4 uses it:
      * records come back in ascending record number (write order).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RECNUM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "RECNUM.DAT"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  F-REC PIC X(1).
       WORKING-STORAGE SECTION.
       01  GOT PIC X(3) VALUE SPACES.
       01  N   PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F.
           MOVE "C" TO F-REC. WRITE F-REC.
           MOVE "A" TO F-REC. WRITE F-REC.
           MOVE "B" TO F-REC. WRITE F-REC.
           CLOSE F.
           OPEN INPUT F.
           PERFORM 3 TIMES
               READ F AT END MOVE "?" TO F-REC END-READ
               ADD 1 TO N
               MOVE F-REC TO GOT (N:1)
           END-PERFORM.
           CLOSE F.
           IF GOT = "CAB"
               DISPLAY "PASS RECNUM " GOT
           ELSE
               DISPLAY "FAIL RECNUM " GOT
           END-IF.
           STOP RUN.
