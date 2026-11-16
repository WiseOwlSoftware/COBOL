      *>--- conformance-case
      *> id: g2-record-varying-unstated-max
      *> rules: [2002-GR-13.16.41.3-10]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.41.3 GR10 "If integer-3 is not specified, the maximum
      *> number of bytes to be contained in any record of the file is equal to
      *> the greatest number of bytes described for a record in that file".
      *> F's RECORD clause gives FROM 5 and no TO. Its two records describe
      *> R1 = 10 bytes and R2 = 5 + 5 x 4 = 25 bytes: §2002:13.16.41.3 GR8
      *> counts "all elementary data items excluding redefinitions and
      *> renamings" and, GR8b, "The maximum number of table elements
      *> described in the record" (OCCURS 1 TO 5). So the maximum is 25.
      *> The size written is the content of data-name-1 L (§2002:13.16.41.3
      *> GR13a), and a size "greater than integer-3" sets EC-I-O-LOGIC-ERROR
      *> and the WRITE is unsuccessful (§2002:13.16.41.3 GR14a), I-O status
      *> 44: "a record that is larger than the largest ... record allowed by
      *> the RECORD IS VARYING clause" (§2002:9.1.12.6).
      *>   L=10, R1's size: W10=00.  L=25, the maximum: W25=00.
      *>   L=26, one past it: W26=44.
      *> A leg fails if the maximum is taken from anything but the greatest
      *> described record: the first record (10, W25 refused), the sum of
      *> all records (35, W26 accepted), or R2 with its table at the
      *> minimum (9, so the greatest is R1's 10 and W25 is refused).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2REC10.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "g2rec10.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  F RECORD IS VARYING IN SIZE FROM 5 CHARACTERS
             DEPENDING ON L.
       01  R1 PIC X(10).
       01  R2.
           05 R2A PIC X(5).
           05 R2T PIC X(4) OCCURS 1 TO 5 DEPENDING ON N.
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       01  L  PIC 99.
       01  N  PIC 9 VALUE 5.
       PROCEDURE DIVISION.
           OPEN OUTPUT F
           MOVE ALL "A" TO R1
           MOVE 10 TO L
           WRITE R1
           DISPLAY "W10=" FS
           MOVE 5 TO N
           MOVE "BBBBB" TO R2A
           MOVE ALL "C" TO R2T(1) R2T(2) R2T(3) R2T(4) R2T(5)
           MOVE 25 TO L
           WRITE R2
           DISPLAY "W25=" FS
           MOVE 26 TO L
           WRITE R2
           DISPLAY "W26=" FS
           CLOSE F
           STOP RUN.
