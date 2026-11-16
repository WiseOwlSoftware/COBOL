      *>--- conformance-case
      *> id: g85_f24_03
      *> rules: [1985-GR-VIII.4.6.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.6.4 GR2 - in sequential access, a REWRITE that
      *> follows a successful READ replaces the record that READ
      *> accessed. Record 2 is rewritten after the second READ and
      *> record 3 after the third; rereading the file shows records
      *> 2 and 3 replaced and record 1 untouched. Replacing the wrong
      *> record (the first, or the next) changes the reread lines.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2403.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REL-FILE ASSIGN TO G85F2403
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  REL-FILE
           LABEL RECORDS ARE STANDARD.
       01  REL-REC PIC X(8).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT REL-FILE.
           MOVE "REC1-OLD" TO REL-REC.
           WRITE REL-REC INVALID KEY DISPLAY "WRITE FAIL".
           MOVE "REC2-OLD" TO REL-REC.
           WRITE REL-REC INVALID KEY DISPLAY "WRITE FAIL".
           MOVE "REC3-OLD" TO REL-REC.
           WRITE REL-REC INVALID KEY DISPLAY "WRITE FAIL".
           CLOSE REL-FILE.
           OPEN I-O REL-FILE.
           READ REL-FILE AT END DISPLAY "EOF1".
           READ REL-FILE AT END DISPLAY "EOF2".
           MOVE "REC2-NEW" TO REL-REC.
           REWRITE REL-REC.
           READ REL-FILE AT END DISPLAY "EOF3".
           MOVE "REC3-NEW" TO REL-REC.
           REWRITE REL-REC.
           CLOSE REL-FILE.
           OPEN INPUT REL-FILE.
           READ REL-FILE AT END DISPLAY "EOF4".
           DISPLAY REL-REC.
           READ REL-FILE AT END DISPLAY "EOF5".
           DISPLAY REL-REC.
           READ REL-FILE AT END DISPLAY "EOF6".
           DISPLAY REL-REC.
           READ REL-FILE AT END DISPLAY "EOF7".
           CLOSE REL-FILE.
           STOP RUN.
