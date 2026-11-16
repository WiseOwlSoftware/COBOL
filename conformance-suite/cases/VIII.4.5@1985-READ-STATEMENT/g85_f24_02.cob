      *>--- conformance-case
      *> id: g85_f24_02
      *> rules: [1985-GR-VIII.4.5.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.5.4 GR6 - the file's two record descriptions
      *> share one record area, an implicit redefinition: after each
      *> READ the record is visible through BOTH record-names. The
      *> two descriptions are the same size, so no item lies beyond
      *> the current record (the rule's undefined leg is not used).
      *> A separate area per description prints spaces or stale data.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2402.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REL-FILE ASSIGN TO G85F2402
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  REL-FILE
           LABEL RECORDS ARE STANDARD.
       01  REC-A PIC X(8).
       01  REC-B.
           05  REC-B-1 PIC X(3).
           05  REC-B-2 PIC 9(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT REL-FILE.
           MOVE "XYZ12345" TO REC-A.
           WRITE REC-A INVALID KEY DISPLAY "WRITE FAIL".
           MOVE "PQR" TO REC-B-1.
           MOVE 678 TO REC-B-2.
           WRITE REC-B INVALID KEY DISPLAY "WRITE FAIL".
           CLOSE REL-FILE.
           OPEN INPUT REL-FILE.
           READ REL-FILE AT END DISPLAY "EOF1".
           DISPLAY "B1 " REC-B-1.
           DISPLAY "B2 " REC-B-2.
           READ REL-FILE AT END DISPLAY "EOF2".
           DISPLAY "A  " REC-A.
           CLOSE REL-FILE.
           STOP RUN.
