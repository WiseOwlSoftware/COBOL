      *>--- conformance-case
      *> id: g85_bf31_01
      *> rules: [1985-GR-VIII.4.3.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.3.4 GR4 - after a successful DELETE the record
      *> "has been logically removed from the file and can no longer be
      *> accessed" (relative file, dynamic access). Records 1,2,3
      *> written; DELETE with RKEY=2 (GR3: the record identified by the
      *> relative key) -> NOT INVALID KEY, status 00. Then every access
      *> path to record 2 must fail: a random READ of 2 takes INVALID
      *> KEY with status 23 (§1985:VIII.1.3.4: "An attempt is made to
      *> randomly access a record that does not exist in the file"); a
      *> second DELETE of 2 takes INVALID KEY (GR3: "If the file does
      *> not contain the record specified by the key, the invalid key
      *> condition exists"); record 3 is untouched; a sequential pass
      *> after CLOSE/OPEN INPUT returns REC01, REC03, then AT END. Each
      *> leg fails if the delete is not applied, is applied to the
      *> wrong record, or does not persist past CLOSE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3101.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RFILE ASSIGN TO "g85bf3101.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RKEY
               FILE STATUS IS RST.
       DATA DIVISION.
       FILE SECTION.
       FD  RFILE.
       01  RREC PIC X(5).
       WORKING-STORAGE SECTION.
       77  RKEY PIC 9(4).
       77  RST  PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RFILE.
           MOVE 1 TO RKEY.
           MOVE "REC01" TO RREC.
           WRITE RREC INVALID KEY DISPLAY "WRITE1 FAIL".
           MOVE 2 TO RKEY.
           MOVE "REC02" TO RREC.
           WRITE RREC INVALID KEY DISPLAY "WRITE2 FAIL".
           MOVE 3 TO RKEY.
           MOVE "REC03" TO RREC.
           WRITE RREC INVALID KEY DISPLAY "WRITE3 FAIL".
           CLOSE RFILE.
           OPEN I-O RFILE.
           MOVE 2 TO RKEY.
           DELETE RFILE RECORD
               INVALID KEY DISPLAY "DEL2 INVALID"
               NOT INVALID KEY DISPLAY "DEL2 OK"
           END-DELETE.
           DISPLAY "DEL2 ST=" RST.
           MOVE 2 TO RKEY.
           READ RFILE
               INVALID KEY DISPLAY "READ2 INVALID"
               NOT INVALID KEY DISPLAY "READ2 FOUND " RREC
           END-READ.
           DISPLAY "READ2 ST=" RST.
           MOVE 2 TO RKEY.
           DELETE RFILE RECORD
               INVALID KEY DISPLAY "DEL2 AGAIN INVALID"
               NOT INVALID KEY DISPLAY "DEL2 AGAIN OK"
           END-DELETE.
           MOVE 3 TO RKEY.
           READ RFILE
               INVALID KEY DISPLAY "READ3 INVALID"
               NOT INVALID KEY DISPLAY "READ3 FOUND " RREC
           END-READ.
           CLOSE RFILE.
           OPEN INPUT RFILE.
           PERFORM 3 TIMES
               READ RFILE NEXT RECORD
                   AT END DISPLAY "SEQ END"
                   NOT AT END DISPLAY "SEQ " RREC
               END-READ
           END-PERFORM.
           CLOSE RFILE.
           STOP RUN.
