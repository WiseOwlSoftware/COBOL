      *>--- conformance-case
      *> id: g85_f24_01
      *> rules: [1985-GR-VIII.4.5.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.5.4 GR5 - a record is available to the object
      *> program before imperative-statement-2 (NOT AT END), before
      *> imperative-statement-4 (NOT INVALID KEY), and before the next
      *> statement when neither phrase is written. Each leg reads a
      *> different record, so a record area not yet filled (still
      *> holding the previous record) prints the wrong value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2401.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REL-FILE ASSIGN TO G85F2401
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS REL-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  REL-FILE
           LABEL RECORDS ARE STANDARD.
       01  REL-REC PIC X(5).
       WORKING-STORAGE SECTION.
       77  REL-KEY PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT REL-FILE.
           MOVE 1 TO REL-KEY.
           MOVE "AAAAA" TO REL-REC.
           WRITE REL-REC INVALID KEY DISPLAY "WRITE FAIL".
           MOVE 2 TO REL-KEY.
           MOVE "BBBBB" TO REL-REC.
           WRITE REL-REC INVALID KEY DISPLAY "WRITE FAIL".
           MOVE 3 TO REL-KEY.
           MOVE "CCCCC" TO REL-REC.
           WRITE REL-REC INVALID KEY DISPLAY "WRITE FAIL".
           CLOSE REL-FILE.
           OPEN INPUT REL-FILE.
      * Format 1, imperative-statement-2 present.
           READ REL-FILE NEXT RECORD
               AT END DISPLAY "EOF1"
               NOT AT END DISPLAY "NAE " REL-REC
           END-READ.
      * Format 1, no imperative-statement-2: the next statement.
           READ REL-FILE NEXT RECORD
               AT END DISPLAY "EOF2"
           END-READ.
           DISPLAY "NXT " REL-REC.
      * Format 2, imperative-statement-4 present.
           MOVE 3 TO REL-KEY.
           READ REL-FILE RECORD
               INVALID KEY DISPLAY "IK3"
               NOT INVALID KEY DISPLAY "NIK " REL-REC
           END-READ.
      * Format 2, no imperative-statement-4: the next statement.
           MOVE 1 TO REL-KEY.
           READ REL-FILE RECORD
               INVALID KEY DISPLAY "IK1"
           END-READ.
           DISPLAY "FOL " REL-REC.
           CLOSE REL-FILE.
           STOP RUN.
