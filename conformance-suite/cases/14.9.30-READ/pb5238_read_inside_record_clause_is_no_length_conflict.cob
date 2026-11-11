      *>--- conformance-case
      *> id: pb5238_read_inside_record_clause_is_no_length_conflict
      *> rules: [GR-14.9.30.4-14]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5238 - a READ of a record that lies inside the file's minimum and maximum record size is a plain successful
      *> READ, 00, whatever the sizes of the record DESCRIPTIONS the reading program has: the record length conflict, I-O status
      *> 04, is a violation of the FIXED FILE ATTRIBUTES.
      *>
      *> §2023:9.1.13.2 item 3 (checked with scripts/spec/cite.py --check): "I-O status = 04. A READ statement is successfully
      *> executed but the physical record from the file is shorter than or longer than the minimum or maximum length of records
      *> allowed for the fixed file attributes for that file."  §2023:9.1.6 names "the minimum and maximum logical record size"
      *> among those attributes, and §2023:14.9.30.4 NOTE 2 (under GR14) says the condition "will occur only when the operating
      *> environment does not check either the minimum or maximum record length as a fixed file attribute during OPEN
      *> processing or when a specific physical record within a physical file violates the fixed file attributes for that physical file".  GR14's "minimum size specified
      *> by the record description entries" is read as that same quantity: the file's minimum and maximum record size, which
      *> §2023:13.18.43.4 GR9 and GR10 take from the descriptions only when the RECORD clause omits integer-2 or integer-3.
      *> Measured against the descriptions instead, a 7-byte record under a 10-byte description would answer 04, and so would
      *> every short record of the idiom RECORD VARYING 1 TO 80 over one PIC X(80).  The 1985 text defines 04 the same way
      *> (§1985:VIII.1.3.4 item 1 b: "does not conform to the fixed file attributes for that file").
      *>
      *> The reading FDs have descriptions of 10 and 15 bytes under a clause of 6 to 30; the relative and indexed records are
      *> 7, 12 and 25 bytes, all inside the clause, written through an FD of 30 bytes.  Every READ is therefore 00, on both
      *> organizations, by key and in sequence, at every edition.  Why it can fail: an implementation that measures the READ
      *> against the descriptions answers 04 for the 7-byte and the 25-byte record.
      *>
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5238N.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RW ASSIGN TO "pb5238n-r.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS WK FILE STATUS IS FSW.
           SELECT RR ASSIGN TO "pb5238n-r.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK FILE STATUS IS FSR.
           SELECT IW ASSIGN TO "pb5238n-i.dat"
               ORGANIZATION IS INDEXED ACCESS MODE IS DYNAMIC
               RECORD KEY IS W-KEY FILE STATUS IS FSW.
           SELECT IR ASSIGN TO "pb5238n-i.dat"
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
