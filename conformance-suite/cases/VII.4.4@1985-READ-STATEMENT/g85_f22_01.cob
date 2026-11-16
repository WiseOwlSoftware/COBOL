      *>--- conformance-case
      *> id: g85_f22_01
      *> rules: [1985-GR-VII.4.4.4-5, 1985-GR-VII.4.4.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.4.4 GR5 - the record is available before
      *> imperative-statement-2 (NOT AT END) and, with no
      *> imperative-statement-2, before the next statement.
      *> §1985:VII.4.4.4 GR6 - two record descriptions of one FD
      *> share one record area (implicit redefinition): a record
      *> written through RA is seen through the fields of RB after
      *> READ. Both records are as long as the area, so no field
      *> lies beyond the current record (the undefined part is not
      *> observed). A leg fails if NOT AT END sees the old area,
      *> or RB does not alias RA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2201.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "G85F2201.DAT"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F
           LABEL RECORDS ARE STANDARD.
       01  RA PIC X(6).
       01  RB.
           05  RB-1 PIC X(2).
           05  RB-2 PIC 9(4).
       WORKING-STORAGE SECTION.
       01  W PIC 9(4).
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F.
           MOVE "AB1234" TO RA.
           WRITE RA.
           MOVE "CD0042" TO RA.
           WRITE RA.
           CLOSE F.
           OPEN INPUT F.
           READ F AT END DISPLAY "EOF-1"
               NOT AT END DISPLAY "IN-NAE " RB-1 " " RB-2
           END-READ.
           READ F AT END DISPLAY "EOF-2".
           DISPLAY "AFTER " RB-1 " " RB-2.
           ADD 1 RB-2 GIVING W.
           DISPLAY "SUM " W.
           DISPLAY "RA " RA.
           MOVE "EF" TO RB-1.
           DISPLAY "RA2 " RA.
           READ F AT END DISPLAY "EOF-3".
           CLOSE F.
           STOP RUN.
