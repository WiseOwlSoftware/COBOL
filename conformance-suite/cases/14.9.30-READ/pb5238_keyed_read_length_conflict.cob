      *>--- conformance-case
      *> id: pb5238_keyed_read_length_conflict
      *> rules: [GR-14.9.30.4-14]
      *> accept: [85, 2002, 2014]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5238 - a RELATIVE or INDEXED READ of a record shorter than the shortest or longer than the longest
      *> RECORD DESCRIPTION of the file is successful with the record length conflict status, 04.
      *>
      *> §1985:VIII.4.5.4 GR16 (relative) and §1985:IX.4.5.4 GR18 (indexed), checked with scripts/spec/cite.py --check:
      *> "In either of these cases, the READ statement is successful and an I-O status is set indicating a record
      *> length conflict has occurred."  The 2002 text (§2002:14.8.29.3 GR13) and the 2014 text (§2014:14.9.29.3 GR14)
      *> say it for every organization; §2023:14.9.30.4 GR14 limits it to a record sequential file (see
      *> wiseowl/cases pb5238_keyed_read_length_conflict_2023).  The I-O status for that condition is 04
      *> (§1985:VIII.1.3.4 item 1 b)).
      *>
      *> The record description sizes are not the RECORD clause's.  §1985:VII.3.8.4 GR10 refuses a WRITE only below
      *> integer-2 or above integer-3, so the writing FD (X(30), clause 6 to 30) stores records of 7, 12 and 25
      *> bytes, and the reading FD (clause 6 to 30, descriptions of 10 and 15 bytes) has a smallest description of 10
      *> and a largest of 15.  Length 7 is below 10: 04.  Length 12 lies between: 00.  Length 25 is above 15: 04.
      *> Both organizations are read at random and then with READ NEXT.  Each READ carries its INVALID KEY or AT END
      *> phrase, which the 1985 text requires when no USE procedure exists, and none runs: the record exists.
      *>
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5238K.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RW ASSIGN TO "pb5238k-r.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS WK FILE STATUS IS FSW.
           SELECT RR ASSIGN TO "pb5238k-r.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK FILE STATUS IS FSR.
           SELECT IW ASSIGN TO "pb5238k-i.dat"
               ORGANIZATION IS INDEXED ACCESS MODE IS DYNAMIC
               RECORD KEY IS W-KEY FILE STATUS IS FSW.
           SELECT IR ASSIGN TO "pb5238k-i.dat"
               ORGANIZATION IS INDEXED ACCESS MODE IS DYNAMIC
               RECORD KEY IS R-KEY FILE STATUS IS FSR.
       DATA DIVISION.
       FILE SECTION.
       FD RW RECORD IS VARYING IN SIZE FROM 6 TO 30 CHARACTERS
               DEPENDING ON LW.
       01 WA PIC X(30).
       FD RR RECORD IS VARYING IN SIZE FROM 6 TO 30 CHARACTERS
               DEPENDING ON LR.
       01 RA10 PIC X(10).
       01 RA15 PIC X(15).
       FD IW RECORD IS VARYING IN SIZE FROM 6 TO 30 CHARACTERS
               DEPENDING ON LW.
       01 IA.
          05 W-KEY PIC X(6).
          05 W-REST PIC X(24).
       FD IR RECORD IS VARYING IN SIZE FROM 6 TO 30 CHARACTERS
               DEPENDING ON LR.
       01 IA10.
          05 R-KEY PIC X(6).
          05 FILLER PIC X(4).
       01 IA15 PIC X(15).
       WORKING-STORAGE SECTION.
       01 WK PIC 9.
       01 RK PIC 9.
       01 LW PIC 99.
       01 LR PIC 99.
       01 FSW PIC XX.
       01 FSR PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RW.
           MOVE ALL "A" TO WA.
           MOVE 7 TO LW. MOVE 1 TO WK.
           WRITE WA INVALID KEY DISPLAY "W1-INV".
           MOVE 12 TO LW. MOVE 2 TO WK.
           WRITE WA INVALID KEY DISPLAY "W2-INV".
           MOVE 25 TO LW. MOVE 3 TO WK.
           WRITE WA INVALID KEY DISPLAY "W3-INV".
           CLOSE RW.
           OPEN OUTPUT IW.
           MOVE ALL "B" TO IA.
           MOVE "K00001" TO W-KEY. MOVE 7 TO LW.
           WRITE IA INVALID KEY DISPLAY "X1-INV".
           MOVE "K00002" TO W-KEY. MOVE 12 TO LW.
           WRITE IA INVALID KEY DISPLAY "X2-INV".
           MOVE "K00003" TO W-KEY. MOVE 25 TO LW.
           WRITE IA INVALID KEY DISPLAY "X3-INV".
           CLOSE IW.
           OPEN INPUT RR.
           MOVE 1 TO RK.
           READ RR INVALID KEY DISPLAY "R1-INV".
           DISPLAY "REL RANDOM 7:  " FSR.
           MOVE 2 TO RK.
           READ RR INVALID KEY DISPLAY "R2-INV".
           DISPLAY "REL RANDOM 12: " FSR.
           MOVE 3 TO RK.
           READ RR INVALID KEY DISPLAY "R3-INV".
           DISPLAY "REL RANDOM 25: " FSR.
           CLOSE RR.
           OPEN INPUT RR.
           READ RR NEXT AT END DISPLAY "N1-END".
           DISPLAY "REL NEXT 7:    " FSR.
           READ RR NEXT AT END DISPLAY "N2-END".
           DISPLAY "REL NEXT 12:   " FSR.
           READ RR NEXT AT END DISPLAY "N3-END".
           DISPLAY "REL NEXT 25:   " FSR.
           CLOSE RR.
           OPEN INPUT IR.
           MOVE "K00001" TO R-KEY.
           READ IR INVALID KEY DISPLAY "I1-INV".
           DISPLAY "IDX RANDOM 7:  " FSR.
           MOVE "K00002" TO R-KEY.
           READ IR INVALID KEY DISPLAY "I2-INV".
           DISPLAY "IDX RANDOM 12: " FSR.
           MOVE "K00003" TO R-KEY.
           READ IR INVALID KEY DISPLAY "I3-INV".
           DISPLAY "IDX RANDOM 25: " FSR.
           CLOSE IR.
           OPEN INPUT IR.
           READ IR NEXT AT END DISPLAY "M1-END".
           DISPLAY "IDX NEXT 7:    " FSR.
           READ IR NEXT AT END DISPLAY "M2-END".
           DISPLAY "IDX NEXT 12:   " FSR.
           READ IR NEXT AT END DISPLAY "M3-END".
           DISPLAY "IDX NEXT 25:   " FSR.
           CLOSE IR.
           STOP RUN.
