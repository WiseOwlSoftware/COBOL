      *>--- conformance-case
      *> id: g85_f23_02
      *> rules: [1985-GR-VIII.4.3.4-2, 1985-GR-VIII.4.3.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.3.4 GR2 - sequential access: a successful READ
      *> (BBB) precedes DELETE, and DELETE removes the record that READ
      *> accessed; the re-read shows AAA, CCC, then at end (10). Fails if
      *> DELETE removes another record (AAA or CCC) or none.
      *> §1985:VIII.4.3.4 GR7 - DELETE updates the I-O status: FS is
      *> set to 99 first and must read 00 (§1985:VIII.1.3.4) after it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2302.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REL-FILE ASSIGN TO F2302REL
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  REL-FILE
           LABEL RECORDS ARE STANDARD.
       01  REL-REC PIC X(3).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT REL-FILE.
           MOVE "AAA" TO REL-REC.
           WRITE REL-REC INVALID KEY DISPLAY "W1 INVALID".
           MOVE "BBB" TO REL-REC.
           WRITE REL-REC INVALID KEY DISPLAY "W2 INVALID".
           MOVE "CCC" TO REL-REC.
           WRITE REL-REC INVALID KEY DISPLAY "W3 INVALID".
           CLOSE REL-FILE.
           OPEN I-O REL-FILE.
           READ REL-FILE AT END DISPLAY "R1 AT END".
           DISPLAY "R1 " FS " " REL-REC.
           READ REL-FILE AT END DISPLAY "R2 AT END".
           DISPLAY "R2 " FS " " REL-REC.
           MOVE "99" TO FS.
           DELETE REL-FILE RECORD.
           DISPLAY "DEL " FS.
           CLOSE REL-FILE.
           OPEN INPUT REL-FILE.
           READ REL-FILE AT END DISPLAY "S1 AT END".
           DISPLAY "S1 " FS " " REL-REC.
           READ REL-FILE AT END DISPLAY "S2 AT END".
           DISPLAY "S2 " FS " " REL-REC.
           READ REL-FILE AT END DISPLAY "S3 AT END".
           DISPLAY "S3 " FS.
           CLOSE REL-FILE.
           STOP RUN.
