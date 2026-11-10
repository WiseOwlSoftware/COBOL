      *>--- conformance-case
      *> id: 1985-GR-VII_1_3_5-5
      *> rules: [1985-GR-VII.1.3.5-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.1.3.5 GR5 (LANGUAGE CONCEPTS). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. IOSTAT9.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SFILE ASSIGN TO SEQFILE
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS WS-STAT.
       DATA DIVISION.
       FILE SECTION.
       FD  SFILE
           LABEL RECORDS ARE STANDARD.
       01  SREC PIC X(10).
       WORKING-STORAGE SECTION.
       01  WS-STAT PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT SFILE.
           DISPLAY WS-STAT.
           MOVE "ABCDEFGHIJ" TO SREC.
           WRITE SREC.
           DISPLAY WS-STAT.
           CLOSE SFILE.
           DISPLAY WS-STAT.
           OPEN INPUT SFILE.
           DISPLAY WS-STAT.
           READ SFILE AT END CONTINUE END-READ.
           DISPLAY WS-STAT.
           READ SFILE AT END CONTINUE END-READ.
           DISPLAY WS-STAT.
           CLOSE SFILE.
           DISPLAY WS-STAT.
           STOP RUN.
