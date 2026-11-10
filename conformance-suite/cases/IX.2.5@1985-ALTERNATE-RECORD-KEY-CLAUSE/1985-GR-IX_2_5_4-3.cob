      *>--- conformance-case
      *> id: 1985-GR-IX_2_5_4-3
      *> rules: [1985-GR-IX.2.5.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.2.5.4 GR3 (THE ALTERNATE RECORD KEY CLAUSE). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the confirmation pass's redraft (WEAK
      *> original) by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ALTDUP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IFILE ASSIGN TO IDXFILE
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS PKEY
               ALTERNATE RECORD KEY IS AKEY WITH DUPLICATES
               ALTERNATE RECORD KEY IS UKEY.
       DATA DIVISION.
       FILE SECTION.
       FD  IFILE.
       01  IREC.
           05 PKEY PIC X(4).
           05 AKEY PIC X(4).
           05 UKEY PIC X(4).
       WORKING-STORAGE SECTION.
       01  CNT PIC 9 VALUE 0.
       01  EOF-FLAG PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IFILE.
           MOVE "0001" TO PKEY.
           MOVE "AAAA" TO AKEY.
           MOVE "U001" TO UKEY.
           WRITE IREC
               INVALID KEY DISPLAY "WRITE 1 INVALID"
               NOT INVALID KEY DISPLAY "WRITE 1 OK"
           END-WRITE.
           MOVE "0002" TO PKEY.
           MOVE "AAAA" TO AKEY.
           MOVE "U002" TO UKEY.
           WRITE IREC
               INVALID KEY DISPLAY "WRITE 2 INVALID"
               NOT INVALID KEY DISPLAY "WRITE 2 OK"
           END-WRITE.
           MOVE "0003" TO PKEY.
           MOVE "BBBB" TO AKEY.
           MOVE "U001" TO UKEY.
           WRITE IREC
               INVALID KEY DISPLAY "WRITE 3 INVALID"
               NOT INVALID KEY DISPLAY "WRITE 3 OK"
           END-WRITE.
           CLOSE IFILE.
           OPEN INPUT IFILE.
           MOVE "AAAA" TO AKEY.
           START IFILE KEY IS EQUAL TO AKEY
               INVALID KEY MOVE "Y" TO EOF-FLAG
           END-START.
           PERFORM UNTIL EOF-FLAG = "Y"
               READ IFILE NEXT RECORD
                   AT END MOVE "Y" TO EOF-FLAG
                   NOT AT END
                       IF AKEY = "AAAA"
                           ADD 1 TO CNT
                       ELSE
                           MOVE "Y" TO EOF-FLAG
                       END-IF
               END-READ
           END-PERFORM.
           DISPLAY "COUNT " CNT.
           CLOSE IFILE.
           STOP RUN.
