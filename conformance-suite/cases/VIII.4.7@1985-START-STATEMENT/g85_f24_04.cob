      *>--- conformance-case
      *> id: g85_f24_04
      *> rules: [1985-GR-VIII.4.7.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.7.4 GR2 - START with no KEY phrase implies IS
      *> EQUAL TO, compared against the RELATIVE KEY item (GR10).
      *> Records 1, 2 and 5 exist. Key 2 positions on record 2
      *> (GREATER would give 5); key 3 is invalid (NOT LESS or
      *> GREATER would find 5); key 5 positions on record 5
      *> (GREATER would be invalid).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2404.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REL-FILE ASSIGN TO G85F2404
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
           MOVE "REC01" TO REL-REC.
           WRITE REL-REC INVALID KEY DISPLAY "WRITE FAIL".
           MOVE 2 TO REL-KEY.
           MOVE "REC02" TO REL-REC.
           WRITE REL-REC INVALID KEY DISPLAY "WRITE FAIL".
           MOVE 5 TO REL-KEY.
           MOVE "REC05" TO REL-REC.
           WRITE REL-REC INVALID KEY DISPLAY "WRITE FAIL".
           CLOSE REL-FILE.
           OPEN INPUT REL-FILE.
           MOVE 2 TO REL-KEY.
           START REL-FILE
               INVALID KEY DISPLAY "START2 INVALID"
               NOT INVALID KEY DISPLAY "START2 OK"
           END-START.
           READ REL-FILE NEXT RECORD AT END DISPLAY "EOF2".
           DISPLAY REL-REC.
           MOVE 3 TO REL-KEY.
           START REL-FILE
               INVALID KEY DISPLAY "START3 INVALID"
               NOT INVALID KEY DISPLAY "START3 OK"
           END-START.
           MOVE 5 TO REL-KEY.
           START REL-FILE
               INVALID KEY DISPLAY "START5 INVALID"
               NOT INVALID KEY DISPLAY "START5 OK"
           END-START.
           READ REL-FILE NEXT RECORD AT END DISPLAY "EOF5".
           DISPLAY REL-REC.
           CLOSE REL-FILE.
           STOP RUN.
