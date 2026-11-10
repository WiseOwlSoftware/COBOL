      *>--- conformance-case
      *> id: 1985-GR-IX_4_4_4-12
      *> rules: [1985-GR-IX.4.4.4-12]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.4.4 GR12 (THE OPEN STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. EXTIDX.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IFILE ASSIGN TO IDXFILE
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IKEY.
       DATA DIVISION.
       FILE SECTION.
       FD  IFILE
           LABEL RECORDS ARE STANDARD.
       01  IREC.
           05  IKEY  PIC X(3).
           05  IDATA PIC X(5).
       WORKING-STORAGE SECTION.
       01  EOF-FLAG PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IFILE.
           MOVE "001" TO IKEY.
           MOVE "FIRST" TO IDATA.
           WRITE IREC INVALID KEY DISPLAY "BAD WRITE 1".
           CLOSE IFILE.
           OPEN EXTEND IFILE.
           MOVE "002" TO IKEY.
           MOVE "SECND" TO IDATA.
           WRITE IREC INVALID KEY DISPLAY "BAD WRITE 2".
           CLOSE IFILE.
           OPEN INPUT IFILE.
           PERFORM READ-PARA UNTIL EOF-FLAG = "Y".
           CLOSE IFILE.
           STOP RUN.
       READ-PARA.
           READ IFILE AT END MOVE "Y" TO EOF-FLAG.
           IF EOF-FLAG = "N" DISPLAY IREC.
