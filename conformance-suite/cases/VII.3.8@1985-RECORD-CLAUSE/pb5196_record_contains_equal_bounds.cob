      *>--- conformance-case
      *> id: pb5196_record_contains_equal_bounds
      *> rules: [1985-FMT-VII.3.8.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5196 - at COBOL-85 Format 3 of the RECORD clause may state one
      *> value twice. §1985:VII.3.8.4 GR14 (cite.py --check OK): "When format 3 of
      *> the RECORD clause is used, integer-4 and integer-5 refer to the minimum
      *> number of characters in the smallest size data record and the maximum
      *> number of characters in the largest size data record, respectively."
      *> §1985:VII.3.8.3 states syntax rules for formats 1 and 2 only (SR3,
      *> "Integer-3 must be greater than integer-2", is format 2's), so nothing
      *> orders integer-4 and integer-5; a file whose one record is five
      *> characters has five for both. The ordering rule is §2002:13.16.41.2 SR8
      *> ("Integer-5 shall be greater than integer-4"), refused from 2002 by
      *> pb5196-record-contains-equal-bounds-2002.
      *> DERIVATION: F1 holds one record of 5 characters, so WRITE stores HELLO
      *> and the read back returns it with status 00; the next READ is at end
      *> (status 10).
      *> Fails if: the clause is refused (COBOL2009), or the read back differs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5196EQ85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb5196eq85.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST.
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           RECORD CONTAINS 5 TO 5 CHARACTERS.
       01  R1 PIC X(5).
       WORKING-STORAGE SECTION.
       01  ST PIC XX.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F1.
           MOVE "HELLO" TO R1.
           WRITE R1.
           DISPLAY "W " ST.
           CLOSE F1.
           OPEN INPUT F1.
           MOVE SPACES TO R1.
           READ F1 AT END DISPLAY "EOF EARLY" END-READ.
           DISPLAY "R " ST " [" R1 "]".
           READ F1 AT END DISPLAY "AT END " ST END-READ.
           CLOSE F1.
           STOP RUN.
