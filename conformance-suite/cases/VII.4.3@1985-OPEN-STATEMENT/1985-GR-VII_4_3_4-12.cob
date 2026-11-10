      *>--- conformance-case
      *> id: 1985-GR-VII_4_3_4-12
      *> rules: [1985-GR-VII.4.3.4-12]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.3.4 GR12 (THE OPEN STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. REVTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SEQ-FILE ASSIGN TO "REVFILE"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  SEQ-FILE
           LABEL RECORDS ARE STANDARD.
       01  SEQ-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01  EOF-FLAG PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT SEQ-FILE.
           MOVE "REC01" TO SEQ-REC.
           WRITE SEQ-REC.
           MOVE "REC02" TO SEQ-REC.
           WRITE SEQ-REC.
           MOVE "REC03" TO SEQ-REC.
           WRITE SEQ-REC.
           CLOSE SEQ-FILE.
           OPEN INPUT SEQ-FILE REVERSED.
           PERFORM READ-PARA UNTIL EOF-FLAG = "Y".
           CLOSE SEQ-FILE.
           STOP RUN.
       READ-PARA.
           READ SEQ-FILE AT END MOVE "Y" TO EOF-FLAG.
           IF EOF-FLAG = "N" DISPLAY SEQ-REC.
