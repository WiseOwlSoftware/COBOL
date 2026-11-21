      *>--- conformance-case
      *> id: 1985-GR-VIII_1_3_1-p3
      *> rules: [1985-GR-VIII.1.3.1-p3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.1.3.1 p3 (LANGUAGE CONCEPTS.). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-99.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * s-99 g2: VIII.1.3.1 p3 at --std 85. A relative file whose
      * records are described at 5 and 20 character positions; the
      * implementor may reserve any number of positions per record on
      * the medium, but each record read back must equal the record
      * written. Expected: R1=SHORT R2=LONGRECORD-TWENTY!! then
      * RELVAR OK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RELVAR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RFILE ASSIGN TO "G2RELVAR.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS WS-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  RFILE
           LABEL RECORDS ARE STANDARD
           RECORD CONTAINS 5 TO 20 CHARACTERS.
       01  RSHORT PIC X(5).
       01  RLONG  PIC X(20).
       WORKING-STORAGE SECTION.
       01  WS-KEY PIC 9(4).
       01  WS-R1  PIC X(5).
       01  WS-R2  PIC X(20).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RFILE.
           MOVE 1 TO WS-KEY. MOVE "SHORT" TO RSHORT.
           WRITE RSHORT INVALID KEY DISPLAY "BAD W1".
           MOVE 2 TO WS-KEY. MOVE "LONGRECORD-TWENTY!!" TO RLONG.
           WRITE RLONG INVALID KEY DISPLAY "BAD W2".
           CLOSE RFILE.
           OPEN INPUT RFILE.
           MOVE 2 TO WS-KEY.
           READ RFILE INVALID KEY DISPLAY "BAD R2".
           MOVE RLONG TO WS-R2.
           MOVE 1 TO WS-KEY.
           READ RFILE INVALID KEY DISPLAY "BAD R1".
           MOVE RSHORT TO WS-R1.
           CLOSE RFILE.
           DISPLAY "R1=" WS-R1 " R2=" WS-R2.
           IF WS-R1 = "SHORT" AND WS-R2 = "LONGRECORD-TWENTY!!"
               DISPLAY "RELVAR OK"
           ELSE
               DISPLAY "RELVAR FAIL"
           END-IF.
           STOP RUN.
