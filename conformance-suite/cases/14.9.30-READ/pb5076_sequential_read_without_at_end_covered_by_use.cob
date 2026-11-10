      *>--- conformance-case
      *> id: pb5076_sequential_read_without_at_end_covered_by_use
      *> rules: [GR-14.9.30.4-24]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5076 - the LEGAL side of 1985:VII.4.4.3 SR2 ("The AT END phrase must be specified if no applicable USE AFTER STANDARD
      *>   EXCEPTION procedure is specified for file-name-1", cite.py: OK): a USE AFTER STANDARD EXCEPTION procedure ON SF is applicable,
      *>   so the READs name no AT END. conformance:negative/pb5076-read-sequential-without-at-end-85 is the refused side.
      *> DERIVATION. The first READ succeeds (status 00, no exception, the declarative does not run): SREC = ABCD, HITS 0.
      *>   The second READ finds no record (status 10, the at end condition); with no AT END phrase the applicable USE procedure is
      *>   executed (1985:VII.4.4.4 GR3 b: "Any applicable USE AFTER EXCEPTION procedure is executed"; 2023:14.9.30.4 GR24 d)), HITS 1,
      *>   and control returns to the DISPLAY, which shows the status 10.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5076P1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "PB5076P1.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  SF.
       01  SREC PIC X(4).
       WORKING-STORAGE SECTION.
       01  FS   PIC XX.
       01  HITS PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       SF-USE SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON SF.
       SF-USE-P.
           ADD 1 TO HITS.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN-PARA.
           OPEN OUTPUT SF.
           MOVE "ABCD" TO SREC.
           WRITE SREC.
           CLOSE SF.
           OPEN INPUT SF.
           READ SF.
           DISPLAY "FIRST " SREC " " FS " " HITS.
           READ SF.
           DISPLAY "SECOND " FS " " HITS.
           CLOSE SF.
           STOP RUN.
