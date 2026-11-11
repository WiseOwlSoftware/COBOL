      *>--- conformance-case
      *> id: pb5197_record_contains_range_documents_2002
      *> rules: [GR-13.18.43.4-19]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5197 - at COBOL-2002 RECORD CONTAINS integer-4 TO integer-5 bounds no WRITE.
      *>
      *> §1985:VII.1.3.5 (cite.py --check OK) gives the boundary violation '44' for a record "larger than the largest
      *> or smaller than the smallest record allowed by the RECORD IS VARYING clause" only, and §1985:VII.3.8.4 GR14
      *> (OK) says of format 3: "However, in this case, the size of each data record is completely defined in the
      *> record description entry."  §2002:13.16.41.3 GR14 (OK) is stated for integer-2 and integer-3, Format 2; the
      *> rule that makes a Format 3 range bound a WRITE is §2023:13.18.43.4 GR19, from COBOL-2014 (see the _2014 twin; the COBOL-85 twin is in the VII.1.3 LANGUAGE-CONCEPTS 1985 directory).
      *>
      *> FD FX is 3 TO 5 over records of 2, 4 and 6 bytes: all three WRITEs succeed ('00'), and the Format 2 file GX
      *> (2 TO 6) reads the three records back with sizes 2, 4 and 6.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5197A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FX ASSIGN TO "PB5197A.DAT" FILE STATUS IS ST.
           SELECT GX ASSIGN TO "PB5197A.DAT" FILE STATUS IS SG.
       DATA DIVISION.
       FILE SECTION.
       FD  FX RECORD CONTAINS 3 TO 5 CHARACTERS.
       01  RX2 PIC X(2).
       01  RX4 PIC X(4).
       01  RX6 PIC X(6).
       FD  GX RECORD IS VARYING IN SIZE FROM 2 TO 6 CHARACTERS
               DEPENDING ON LG.
       01  RG PIC X(6).
       WORKING-STORAGE SECTION.
       01  ST PIC XX VALUE "XX".
       01  SG PIC XX VALUE "XX".
       01  LG PIC 99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT FX.
           MOVE "XX" TO ST. MOVE "22" TO RX2. WRITE RX2.
           DISPLAY "WRITE 2 UNDER 3 TO 5 " ST.
           MOVE "XX" TO ST. MOVE "4444" TO RX4. WRITE RX4.
           DISPLAY "WRITE 4 UNDER 3 TO 5 " ST.
           MOVE "XX" TO ST. MOVE "666666" TO RX6. WRITE RX6.
           DISPLAY "WRITE 6 UNDER 3 TO 5 " ST.
           CLOSE FX.
           OPEN INPUT GX.
           READ GX AT END CONTINUE END-READ.
           DISPLAY "G " SG " L=" LG " [" RG (1:LG) "]".
           READ GX AT END CONTINUE END-READ.
           DISPLAY "G " SG " L=" LG " [" RG (1:LG) "]".
           READ GX AT END CONTINUE END-READ.
           DISPLAY "G " SG " L=" LG " [" RG (1:LG) "]".
           READ GX AT END CONTINUE END-READ.
           DISPLAY "G " SG.
           CLOSE GX.
           STOP RUN.
