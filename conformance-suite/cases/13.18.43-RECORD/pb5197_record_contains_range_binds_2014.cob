      *>--- conformance-case
      *> id: pb5197_record_contains_range_binds_2014
      *> rules: [GR-13.18.43.4-19]
      *> accept: [2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5197 - a WRITE outside RECORD CONTAINS integer-4 TO integer-5 is unsuccessful, from COBOL-2014.
      *>
      *> §2023:13.18.43.4 GR19 (cite.py --check OK): "If the number of bytes in the logical record to be written is
      *> less than integer-4 or greater than integer-5, the following occurs: a) If a REWRITE or WRITE statement is
      *> being executed, the EC-I-O-LOGIC-ERROR exception condition is set to exist and the execution of the REWRITE
      *> or WRITE statement is unsuccessful."  The I-O status of a boundary violation is '44' (§2023:9.1.13.7 item 4).
      *> The 2014 FCD carries the same rule; the 2002 text and §1985:VII.1.3.5 do not (see the _2002 and COBOL-85 twins).
      *>
      *> FD FX is 3 TO 5: RX2 (2 bytes) 44, RX4 (4 bytes) 00, RX6 (6 bytes) 44.  Only the 4-byte record reaches the
      *> file, so the read-back through the Format 2 file GX (2 TO 6) finds exactly one record of size 4.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5197B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FX ASSIGN TO "PB5197B.DAT" FILE STATUS IS ST.
           SELECT GX ASSIGN TO "PB5197B.DAT" FILE STATUS IS SG.
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
           DISPLAY "G " SG.
           CLOSE GX.
           STOP RUN.
