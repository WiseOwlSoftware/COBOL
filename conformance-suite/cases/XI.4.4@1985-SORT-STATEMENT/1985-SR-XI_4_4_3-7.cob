      *>--- conformance-case
      *> id: 1985-SR-XI_4_4_3-7
      *> rules: [1985-SR-XI.4.4.3-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.4.4.3 SR7 (THE SORT STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SRTMFR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IN-FILE ASSIGN TO TAPE1.
           SELECT OUT-FILE ASSIGN TO TAPE1.
           SELECT SORT-FILE ASSIGN TO SORTWK.
       I-O-CONTROL.
           MULTIPLE FILE TAPE CONTAINS IN-FILE OUT-FILE.
       DATA DIVISION.
       FILE SECTION.
       FD  IN-FILE LABEL RECORDS ARE STANDARD.
       01  IN-REC PIC X(5).
       FD  OUT-FILE LABEL RECORDS ARE STANDARD.
       01  OUT-REC PIC X(5).
       SD  SORT-FILE.
       01  SORT-REC.
           05  SORT-KEY PIC X(5).
       WORKING-STORAGE SECTION.
       01  EOF-FLAG PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IN-FILE.
           MOVE "CHARL" TO IN-REC.
           WRITE IN-REC.
           MOVE "ALPHA" TO IN-REC.
           WRITE IN-REC.
           MOVE "BRAVO" TO IN-REC.
           WRITE IN-REC.
           CLOSE IN-FILE.
           SORT SORT-FILE ON ASCENDING KEY SORT-KEY
               USING IN-FILE GIVING OUT-FILE.
           OPEN INPUT OUT-FILE.
       READ-PARA.
           READ OUT-FILE AT END MOVE "Y" TO EOF-FLAG.
           IF EOF-FLAG = "N"
               DISPLAY OUT-REC
               GO TO READ-PARA.
           CLOSE OUT-FILE.
           STOP RUN.
