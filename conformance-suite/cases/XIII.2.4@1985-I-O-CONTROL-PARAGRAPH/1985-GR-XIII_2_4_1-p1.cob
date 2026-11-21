      *>--- conformance-case
      *> id: 1985-GR-XIII_2_4_1-p1
      *> rules: [1985-GR-XIII.2.4.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.2.4.1 p1 (THE I-O-CONTROL PARAGRAPH). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-17.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * XIII.2.4.1-p1: the I-O-CONTROL paragraph of the Report Writer
      * module: a report file in a SAME AREA clause (shared memory
      * area) and in a MULTIPLE FILE TAPE clause (location on a
      * multiple file reel). PRT and SEQ are never open together.
      * Expected: REC=HELLO / RPT OK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWIOC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "rwioc1.txt".
           SELECT SEQ ASSIGN TO "rwioc2.txt".
       I-O-CONTROL.
           SAME AREA FOR PRT SEQ
           MULTIPLE FILE TAPE CONTAINS PRT POSITION 1 SEQ POSITION 2.
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-1.
       FD  SEQ.
       01  SEQ-REC PIC X(5).
       REPORT SECTION.
       RD  R-1.
       01  DE-1 TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC X(5) VALUE "LINE1".
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT SEQ.
           MOVE "HELLO" TO SEQ-REC.
           WRITE SEQ-REC.
           CLOSE SEQ.
           OPEN INPUT SEQ.
           READ SEQ AT END DISPLAY "EMPTY".
           DISPLAY "REC=" SEQ-REC.
           CLOSE SEQ.
           OPEN OUTPUT PRT.
           INITIATE R-1.
           GENERATE DE-1.
           TERMINATE R-1.
           CLOSE PRT.
           DISPLAY "RPT OK".
           STOP RUN.
