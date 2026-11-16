      *>--- conformance-case
      *> id: g85_bf31_03
      *> rules: [1985-GR-VIII.4.7.4-10]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.7.4 GR10 - "The comparison described in general
      *> rule 4 uses the data item referenced by the RELATIVE KEY
      *> phrase of the ACCESS MODE clause associated with file-name-1."
      *> Records 1,2,4,6. S1: START with no KEY phrase (GR2: IS EQUAL
      *> TO implied) and RKEY=4 -> the comparison takes its value from
      *> RKEY, so record 4 qualifies: NOT INVALID KEY and READ NEXT
      *> gives REC04 (GR4a). S2: KEY NOT LESS THAN with RKEY=3 -> first
      *> record >= 3 is 4. S3: KEY GREATER THAN with RKEY=4 -> 6. S4:
      *> EQUAL with RKEY=5 -> no record (GR4b) -> INVALID KEY. S5:
      *> GREATER THAN with RKEY=6 -> none -> INVALID KEY. A START that
      *> compared against anything but the current RKEY value (the
      *> record area, a stale key, the file position) gives a different
      *> line on at least one leg.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3103.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RFILE ASSIGN TO "g85bf3103.dat"
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
           MOVE 4 TO RKEY.
           MOVE "REC04" TO RREC.
           WRITE RREC INVALID KEY DISPLAY "WRITE4 FAIL".
           MOVE 6 TO RKEY.
           MOVE "REC06" TO RREC.
           WRITE RREC INVALID KEY DISPLAY "WRITE6 FAIL".
           CLOSE RFILE.
           OPEN INPUT RFILE.
           MOVE 4 TO RKEY.
           START RFILE
               INVALID KEY DISPLAY "S1 INVALID"
               NOT INVALID KEY DISPLAY "S1 OK"
           END-START.
           READ RFILE NEXT RECORD
               AT END DISPLAY "S1 READ END"
               NOT AT END DISPLAY "S1 READ " RREC
           END-READ.
           MOVE 3 TO RKEY.
           START RFILE KEY IS NOT LESS THAN RKEY
               INVALID KEY DISPLAY "S2 INVALID"
               NOT INVALID KEY DISPLAY "S2 OK"
           END-START.
           READ RFILE NEXT RECORD
               AT END DISPLAY "S2 READ END"
               NOT AT END DISPLAY "S2 READ " RREC
           END-READ.
           MOVE 4 TO RKEY.
           START RFILE KEY IS GREATER THAN RKEY
               INVALID KEY DISPLAY "S3 INVALID"
               NOT INVALID KEY DISPLAY "S3 OK"
           END-START.
           READ RFILE NEXT RECORD
               AT END DISPLAY "S3 READ END"
               NOT AT END DISPLAY "S3 READ " RREC
           END-READ.
           MOVE 5 TO RKEY.
           START RFILE KEY IS EQUAL TO RKEY
               INVALID KEY DISPLAY "S4 INVALID"
               NOT INVALID KEY DISPLAY "S4 OK"
           END-START.
           MOVE 6 TO RKEY.
           START RFILE KEY IS GREATER THAN RKEY
               INVALID KEY DISPLAY "S5 INVALID"
               NOT INVALID KEY DISPLAY "S5 OK"
           END-START.
           CLOSE RFILE.
           STOP RUN.
