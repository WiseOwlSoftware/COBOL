      *>--- conformance-case
      *> id: pb5238_sequential_read_length_conflict
      *> rules: [GR-14.9.30.4-14]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5238 - a record sequential READ of a record shorter than the shortest or longer than the longest
      *> RECORD DESCRIPTION is successful with the record length conflict status, 04, even when the RECORD clause's
      *> integer-2 and integer-3 admit the record.
      *>
      *> §2023:14.9.30.4 GR14 (checked with scripts/spec/cite.py --check): "if the number of bytes in the record that
      *> is read is less than the minimum size specified by the record description entries for file-name-1 ... or
      *> greater than the maximum size specified by the record description entries ... the READ statement is
      *> successful, and the I-O status value for file-name-1 is set to '04'".  §1985:VII.4.4.4 GR13, the 2002 text
      *> (§2002:14.8.29.3 GR13) and the 2014 text (§2014:14.9.29.3 GR14) say it too.
      *>
      *> The writing FD (clause 6 to 20, one description of 20) stores records of 20, 7 and 12 bytes, all admitted by
      *> §1985:VII.3.8.4 GR10.  The reading FD has the same clause and descriptions of 10 and 15 bytes: its smallest
      *> description is 10 and its largest 15.  20 is above 15: 04.  7 is below 10: 04.  12 lies between: 00.
      *>
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5238S.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FW ASSIGN TO "pb5238s.dat" FILE STATUS IS FSW.
           SELECT FR ASSIGN TO "pb5238s.dat" FILE STATUS IS FSR.
       DATA DIVISION.
       FILE SECTION.
       FD FW RECORD IS VARYING IN SIZE FROM 6 TO 20 CHARACTERS
               DEPENDING ON LW.
       01 W20 PIC X(20).
       FD FR RECORD IS VARYING IN SIZE FROM 6 TO 20 CHARACTERS
               DEPENDING ON LR.
       01 R10 PIC X(10).
       01 R15 PIC X(15).
       WORKING-STORAGE SECTION.
       01 LW PIC 99.
       01 LR PIC 99.
       01 FSW PIC XX.
       01 FSR PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT FW.
           MOVE ALL "A" TO W20.
           MOVE 20 TO LW. WRITE W20.
           MOVE 7 TO LW. WRITE W20.
           MOVE 12 TO LW. WRITE W20.
           CLOSE FW.
           OPEN INPUT FR.
           READ FR AT END DISPLAY "R1-END".
           DISPLAY "SEQ READ 20: " FSR.
           READ FR AT END DISPLAY "R2-END".
           DISPLAY "SEQ READ 7:  " FSR.
           READ FR AT END DISPLAY "R3-END".
           DISPLAY "SEQ READ 12: " FSR.
           CLOSE FR.
           STOP RUN.
