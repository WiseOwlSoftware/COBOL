      *>--- conformance-case
      *> id: pb721_zero_length_record_2002
      *> rules: [SR-13.18.43.3-7]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> The COBOL-2002 half of the Annex A.3 item 43 claim (the 2023 witness is
      *> conformance-suite/cases/8.5.4-Zero-length-items/pb721_zero_length_record.cob, which carries the full
      *> derivation). §2002:13.16.41.2 SR6 (cite.py --check OK): "Integer-2 shall be greater than or equal
      *> to zero." is the first text to permit a zero minimum, so RECORD IS VARYING IN SIZE FROM 0 is legal
      *> from 2002 and a zero-length record round-trips on a sequential and a relative file. COBOL-85 has no
      *> such rule (§1985:VII.3.8.3) and §1985:III.2 bars a zero integer, so --std 85 refuses it
      *> (kb/Work PB5196; pb5196-record-varying-zero-minimum-1985). Until PB5196 this program was the
      *> accepted-at-85 twin, on the reading that no edition gate existed; the 1985 text, now in the
      *> repository, says otherwise.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB721-ZERO-LEN-85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQ ASSIGN TO "pb721z85-sq.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS WS-ST.
           SELECT RL ASSIGN TO "pb721z85-rl.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS WS-RK
               FILE STATUS IS WS-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  SQ RECORD IS VARYING IN SIZE FROM 0 TO 20 CHARACTERS
               DEPENDING ON WS-LEN.
       01  SQ-REC PIC X(20).
       FD  RL RECORD IS VARYING IN SIZE FROM 0 TO 20 CHARACTERS
               DEPENDING ON WS-LEN.
       01  RL-REC PIC X(20).
       WORKING-STORAGE SECTION.
       01  WS-ST  PIC XX.
       01  WS-RK  PIC 9(4) VALUE 0.
       01  WS-LEN PIC 9(4) VALUE 0.
       01  WS-Z   PIC X(10) VALUE ALL "-".
       PROCEDURE DIVISION.
       MAIN.
           MOVE ALL "-" TO WS-Z
           MOVE 0 TO WS-LEN
           MOVE SPACES TO SQ-REC
           OPEN OUTPUT SQ
           WRITE SQ-REC
           DISPLAY "SQ W=" WS-ST
           CLOSE SQ
           MOVE 99 TO WS-LEN
           OPEN INPUT SQ
           READ SQ INTO WS-Z
               AT END DISPLAY "SQ ATEND"
           END-READ
           DISPLAY "SQ R=" WS-ST " LEN=" WS-LEN
           DISPLAY "SQ Z=[" WS-Z "]"
           CLOSE SQ

           MOVE ALL "-" TO WS-Z
           MOVE 0 TO WS-LEN
           MOVE SPACES TO RL-REC
           OPEN OUTPUT RL
           WRITE RL-REC INVALID KEY CONTINUE END-WRITE
           DISPLAY "RL W=" WS-ST
           CLOSE RL
           MOVE 99 TO WS-LEN
           OPEN INPUT RL
           READ RL INTO WS-Z
               AT END DISPLAY "RL ATEND"
           END-READ
           DISPLAY "RL R=" WS-ST " LEN=" WS-LEN
           DISPLAY "RL Z=[" WS-Z "]"
           CLOSE RL
           STOP RUN.
