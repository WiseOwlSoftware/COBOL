      *>--- conformance-case
      *> id: pb5309-read-into-odo-first-record
      *> rules: [1985-GR-VII.3.8.4-12]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5309. §1985:VII.3.8.4 GR12: with INTO, "the number of
      *> character positions in the current record that participate as
      *> the sending data items in the implicit MOVE statement is
      *> determined by the following conditions", and for RECORD IS
      *> VARYING ... DEPENDING ON it is the length the data-name holds.
      *> The current record is the record the READ made available in the
      *> record area, so the OCCURS DEPENDING ON item of a record
      *> description (§1985:VI.5.8.4 GR3: "the part of the table area
      *> used in the operation") does not shorten it. FA describes the
      *> table in its FIRST record, FB in its SECOND; the table count is
      *> 2 and the record is 12 characters, so both INTO moves send
      *> ABCDEFGHIJKL (the receiver is right-justified, so the twelve
      *> characters end at its last position).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5309A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FA ASSIGN TO "pb5309a.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT FB ASSIGN TO "pb5309b.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  FA RECORD IS VARYING IN SIZE FROM 1 TO 15
               DEPENDING ON LEN-A.
       01  RA1.
           05  EA PIC X OCCURS 1 TO 15 DEPENDING ON CNT.
       01  RA2 PIC X(15).
       FD  FB RECORD IS VARYING IN SIZE FROM 1 TO 15
               DEPENDING ON LEN-B.
       01  RB1 PIC X(15).
       01  RB2.
           05  EB PIC X OCCURS 1 TO 15 DEPENDING ON CNT.
       WORKING-STORAGE SECTION.
       01  CNT   PIC 99 VALUE 15.
       01  LEN-A PIC 99.
       01  LEN-B PIC 99.
       01  W PIC X(20) JUSTIFIED RIGHT.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT FA.
           MOVE "ABCDEFGHIJKL" TO RA2.
           MOVE 12 TO LEN-A.
           WRITE RA2.
           CLOSE FA.
           OPEN OUTPUT FB.
           MOVE "ABCDEFGHIJKL" TO RB1.
           MOVE 12 TO LEN-B.
           WRITE RB1.
           CLOSE FB.
           MOVE 2 TO CNT.
           OPEN INPUT FA.
           READ FA INTO W AT END DISPLAY "EOF A".
           DISPLAY "A [" W "] " LEN-A.
           CLOSE FA.
           OPEN INPUT FB.
           READ FB INTO W AT END DISPLAY "EOF B".
           DISPLAY "B [" W "] " LEN-B.
           CLOSE FB.
           STOP RUN.
