      *>--- conformance-case
      *> id: g2rw03_from_sender_validity
      *> rules: [2002-SR-14.8.33.2-6]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.33.2 SR6 - "If record-name-1 is specified,
      *>   identifier-1 or literal-1 shall be valid as a sending
      *>   operand in a MOVE statement specifying record-name-1 as the
      *>   receiving operand." (cite.py: OK). The 2002 MOVE rules
      *>   decide validity: §2002:14.8.24.2 SR5 forbids only SPACE to
      *>   a numeric item, and SR8 sends every other case to table 17
      *>   (cite.py: OK each), where an alphanumeric sender to a
      *>   numeric receiver is "Yes". Table 18 (§2002:14.8.24.3 GR5,
      *>   cite.py: OK) makes an ALL alphanumeric literal, and
      *>   HIGH-VALUE, LOW-VALUE and QUOTE moved to a numeric receiver,
      *>   alphanumeric. So every REWRITE FROM below is valid at 2002
      *>   and the program shall be accepted.
      *> The record G2RW03-N is PIC 9(5), one record 11111 written
      *> first. Each leg rewrites it and reads it back:
      *>   L1 FROM ALL "7": an alphanumeric figurative sender to a
      *>      numeric record; GR4 b) 3. b. gives it the receiver's 5
      *>      digits, replicated: 77777.
      *>   L2 FROM CN, a numeric item of the record of an output CD
      *>      (identifier-1 may be any data item; the CD record is
      *>      one, §2002:13.16.31.3 GR3). CN = 456, an integer moved to
      *>      9(5) aligns by the decimal point with zero fill: 00456.
      *>   L3 FROM W-X, an alphanumeric item "123": GR4 b) 3. a.
      *>      treats it as a 3-digit unsigned integer: 00123.
      *>   L4 FROM HIGH-VALUE, LOW-VALUE and QUOTE: valid senders
      *>      (table 18, table 17), compiled but on a path never
      *>      executed, because their content fails the numeric class
      *>      test and GR4 b) leaves such a MOVE undefined. The
      *>      program's acceptance is the leg; N stays 00123.
      *> Each value is displayed by READ-BACK after its READ and
      *> before its CLOSE: §2002:14.8.33.3 GR5 releases the record
      *> from the area at REWRITE and §2002:14.8.6.3 GR7 makes the
      *> area unavailable after CLOSE (F is in no SAME RECORD AREA),
      *> so only a just-READ record is defined content.
      *> §2002:14.8.33.3 GR16 ('44' when the size differs from the
      *> record being replaced) does not fire for the 3-byte senders
      *> CN and W-X: GR6 b) makes the statement executed the REWRITE
      *> without FROM, whose record is record-name-1 (5 bytes).
      *> Fails if any of these forms is refused, or the implicit MOVE
      *> does not follow the MOVE rules. The facility of the CD draws
      *> the obsolete warning COBOL0903 (§2002:G.2).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RW03.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "g2rw03.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  G2RW03-N PIC 9(5).
       WORKING-STORAGE SECTION.
       01  W-X     PIC X(3) VALUE "123".
       01  W-NEVER PIC X VALUE "N".
       01  W-TAG   PIC X(2).
       COMMUNICATION SECTION.
       CD  COUT FOR OUTPUT.
       01  CREC.
           05 CA PIC A(3).
           05 CN PIC 9(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT F.
           MOVE 11111 TO G2RW03-N.
           WRITE G2RW03-N.
           CLOSE F.
           OPEN I-O F.
           READ F.
           REWRITE G2RW03-N FROM ALL "7".
           CLOSE F.
           MOVE "L1" TO W-TAG.
           PERFORM READ-BACK.
           MOVE 456 TO CN.
           OPEN I-O F.
           READ F.
           REWRITE G2RW03-N FROM CN.
           CLOSE F.
           MOVE "L2" TO W-TAG.
           PERFORM READ-BACK.
           OPEN I-O F.
           READ F.
           REWRITE G2RW03-N FROM W-X.
           CLOSE F.
           MOVE "L3" TO W-TAG.
           PERFORM READ-BACK.
           IF W-NEVER = "Y"
               OPEN I-O F
               READ F
               REWRITE G2RW03-N FROM HIGH-VALUE
               READ F
               REWRITE G2RW03-N FROM LOW-VALUE
               READ F
               REWRITE G2RW03-N FROM QUOTE
               CLOSE F
           END-IF.
           MOVE "L4" TO W-TAG.
           PERFORM READ-BACK.
           STOP RUN.
       READ-BACK.
           OPEN INPUT F.
           READ F.
           DISPLAY W-TAG " N=[" G2RW03-N "]".
           CLOSE F.
