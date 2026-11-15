      *>--- conformance-case
      *> id: pb5480_rewrite_after_truncated_read_85
      *> rules: [GR-14.9.35.4-16]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5480 review R1-1. The REWRITE compares the bytes of
      *> the record being replaced (the physical record, 20 bytes),
      *> never the delivered length the DEPENDING ON item reports (10,
      *> the reader's maximum), so a 10-byte REWRITE after a READ that
      *> truncated a 20-byte record is '44' and the record is
      *> unchanged (§2023:14.9.35.4 GR16). The leg fails if the
      *> REWRITE answers '00' and half-replaces the record.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5480R.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT WF ASSIGN TO "PB5480R.SEQ" FILE STATUS IS FSW.
           SELECT RDF ASSIGN TO "PB5480R.SEQ" FILE STATUS IS FSR.
       DATA DIVISION.
       FILE SECTION.
       FD  WF RECORD VARYING FROM 1 TO 20 DEPENDING ON LW.
       01  WR PIC X(20).
       FD  RDF RECORD VARYING FROM 5 TO 10 DEPENDING ON LR.
       01  RR PIC X(10).
       WORKING-STORAGE SECTION.
       01  FSW PIC XX.
       01  FSR PIC XX.
       01  LW PIC 99.
       01  LR PIC 99.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION. USE AFTER STANDARD EXCEPTION PROCEDURE ON WF RDF.
       D1P. EXIT.
       END DECLARATIVES.
       M SECTION.
       P.
           OPEN OUTPUT WF.
           MOVE "ABCDEFGHIJKLMNOPQRST" TO WR. MOVE 20 TO LW.
           WRITE WR.
           MOVE "1234567" TO WR. MOVE 7 TO LW.
           WRITE WR.
           CLOSE WF.
           OPEN I-O RDF. DISPLAY "OPEN " FSR.
           READ RDF. DISPLAY "READ1 " FSR " LR=" LR " [" RR "]".
           MOVE "zzzzzzzzzz" TO RR. MOVE 10 TO LR.
           REWRITE RR. DISPLAY "REWRITE " FSR.
           CLOSE RDF.
           OPEN INPUT WF.
           READ WF. DISPLAY "W1 " FSW " LW=" LW " [" WR(1:LW) "]".
           READ WF. DISPLAY "W2 " FSW " LW=" LW " [" WR(1:LW) "]".
           READ WF. DISPLAY "W3 " FSW.
           CLOSE WF.
           STOP RUN.
