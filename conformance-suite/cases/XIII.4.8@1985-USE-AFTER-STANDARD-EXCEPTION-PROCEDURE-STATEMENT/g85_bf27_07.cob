      *>--- conformance-case
      *> id: g85_bf27_07
      *> rules: [1985-SR-XIII.4.8.3-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.4.8.3 SR4 - "The words ERROR and EXCEPTION are
      *> synonymous and may be used interchangeably." The Report Writer
      *> module's USE AFTER STANDARD EXCEPTION PROCEDURE statement
      *> (XIII.4.8.1), once spelled EXCEPTION (on a sequential file)
      *> and once spelled ERROR (on the report file of RD RPT-A).
      *> DERIVATION.
      *>  EXCEPTION leg: SEQ-IN holds one record; the second READ has no
      *>  AT END phrase, so VII.4.4.4 GR10 a/c set I-O status 10 and run
      *>  the file's USE procedure (XIII.4.8.4 GR5a):
      *>  HANDLER-VIA-EXCEPTION=10. 10 is not critical, so XIII.4.8.4
      *>  GR6 returns to the statement after the READ: AFTER-SEQ=10.
      *>  ERROR leg: RPT-FILE is opened OUTPUT (00), the report is
      *>  initiated, a detail generated and the report terminated; a
      *>  second OPEN OUTPUT of the still-open file is "I-O Status =
      *>  41" (VII.1.3.5), so the USE procedure spelled ERROR runs
      *>  (XIII.4.8.4 GR5a): HANDLER-VIA-ERROR=41. 41 IS critical, so
      *>  what follows a return is the implementor's (XIII.4.8.4 GR6);
      *>  the procedure therefore ends the run itself with STOP RUN and
      *>  the NOT-REACHED line must not appear.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2707.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SEQ-IN ASSIGN TO "g85bf2707s.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST-SEQ.
           SELECT RPT-FILE ASSIGN TO "g85bf2707r.dat"
               FILE STATUS IS ST-RPT.
       DATA DIVISION.
       FILE SECTION.
       FD  SEQ-IN.
       01  SEQ-REC PIC X(4).
       FD  RPT-FILE
           REPORT IS RPT-A.
       WORKING-STORAGE SECTION.
       01  ST-SEQ PIC XX VALUE "??".
       01  ST-RPT PIC XX VALUE "??".
       REPORT SECTION.
       RD  RPT-A.
       01  LINE-A TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       DECLARATIVES.
       SPELLED-EXCEPTION-SECT SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON SEQ-IN.
       SPELLED-EXCEPTION-PARA.
           DISPLAY "HANDLER-VIA-EXCEPTION=" ST-SEQ.
       SPELLED-ERROR-SECT SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON RPT-FILE.
       SPELLED-ERROR-PARA.
           DISPLAY "HANDLER-VIA-ERROR=" ST-RPT.
           STOP RUN.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       P-SEQ.
           OPEN OUTPUT SEQ-IN.
           MOVE "AAAA" TO SEQ-REC.
           WRITE SEQ-REC.
           CLOSE SEQ-IN.
           OPEN INPUT SEQ-IN.
           READ SEQ-IN.
           DISPLAY "SEQ-REC=" SEQ-REC.
           READ SEQ-IN.
           DISPLAY "AFTER-SEQ=" ST-SEQ.
           CLOSE SEQ-IN.
       P-RPT.
           OPEN OUTPUT RPT-FILE.
           DISPLAY "RPT-OPEN=" ST-RPT.
           INITIATE RPT-A.
           GENERATE LINE-A.
           TERMINATE RPT-A.
           OPEN OUTPUT RPT-FILE.
           DISPLAY "NOT-REACHED".
           STOP RUN.
