      *>--- conformance-case
      *> id: g85_f16_05
      *> rules: [1985-GR-IX.2.7.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.2.7.4 GR1 - the RECORD KEY clause names the prime
      *> record key, and its values must be unique among the records
      *> of the file. Records are written in random order through
      *> dynamic access; a second record with the prime key A001 must
      *> be refused (invalid key 22, item b of §1985:IX.1.3.4 GRL2.3)
      *> and must
      *> not replace or join the first. Sequential reading then
      *> returns the three records once each in prime key order
      *> (A001 ONE, B002 TWO, C003 THREE, then at end 10), and a
      *> random READ by the prime key finds B002. A leg fails if the
      *> duplicate is accepted (a fourth record or DUP in place of
      *> ONE), or the prime key does not select the record.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F1605.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "g85f1605.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-KEY
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  IXF
           LABEL RECORDS ARE STANDARD.
       01  IXREC.
           05 IX-KEY           PIC X(4).
           05 IX-DATA          PIC X(6).
       WORKING-STORAGE SECTION.
       01  FS                  PIC XX.
       01  BR                  PIC X(3).
       01  NREC                PIC 9 VALUE 0.
       01  WKEY                PIC X(4).
       PROCEDURE DIVISION.
       MAIN-LOGIC SECTION.
       M-WRITE.
           OPEN OUTPUT IXF.
           MOVE "C003THREE" TO IXREC.
           PERFORM W-ONE.
           MOVE "A001ONE" TO IXREC.
           PERFORM W-ONE.
           MOVE "B002TWO" TO IXREC.
           PERFORM W-ONE.
           MOVE "A001DUP" TO IXREC.
           PERFORM W-ONE.
           CLOSE IXF.
       M-READ.
           OPEN INPUT IXF.
           PERFORM 4 TIMES
               READ IXF NEXT RECORD
                   AT END DISPLAY "AT END " FS
                   NOT AT END ADD 1 TO NREC
                       DISPLAY "SEQ " FS " " IXREC
               END-READ
           END-PERFORM.
           DISPLAY "RECORDS " NREC.
           MOVE "B002" TO IX-KEY.
           MOVE SPACES TO IX-DATA.
           READ IXF INVALID KEY DISPLAY "RAND IK " FS
               NOT INVALID KEY DISPLAY "RAND " FS " " IXREC
           END-READ.
           CLOSE IXF.
           STOP RUN.
       W-ONE.
      * The record area is not available after a successful WRITE
      * (§1985:IX.4.9.4 GR2), so the key is shown from a copy.
           MOVE IX-KEY TO WKEY.
           MOVE "NO" TO BR.
           WRITE IXREC INVALID KEY MOVE "IK" TO BR
               NOT INVALID KEY MOVE "NIK" TO BR
           END-WRITE.
           DISPLAY "W " WKEY " " FS " " BR.
