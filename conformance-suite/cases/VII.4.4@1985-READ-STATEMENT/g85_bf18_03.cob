      *>--- conformance-case
      *> id: g85_bf18_03
      *> rules: [1985-GR-VII.4.4.4-13]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.4.4 GR13 - a record read that is shorter than the
      *> minimum size of the record description entries leaves the
      *> record area right of its last character undefined; one longer
      *> than the maximum "is truncated on the right to the maximum
      *> size"; "In either of these cases, the READ statement is
      *> successful and an I-O status is set indicating a record length
      *> conflict has occurred", which §1985:VII.1.3.5 GRL2.1 item b
      *> names as 04. OUTF writes a 3-, an 8- and a 15-character record
      *> to a variable-length sequential file (RECORD CONTAINS 3 TO 15,
      *> §1985:VII.3.8.2). INF reads the same file through RECORD
      *> CONTAINS 5 TO 10: the 3-character record is below its minimum
      *> (04; only the 3 characters read are shown, the rest is
      *> undefined), the 8-character record is inside it (00) and the
      *> 15-character record is above it (04, truncated to its first 10
      *> characters). Because each READ is successful, its NOT AT END
      *> phrase runs and its AT END phrase does not. A sequential file's
      *> record sizes are not validated at OPEN (§1985:VII.4.3.4 GR8
      *> leaves the set to the implementor; docs/conformance
      *> D-IO-STATUS-39), so INF's OPEN is 00. Each leg fails if a
      *> length conflict reads 00, an in-range record reads 04, the long
      *> record is not truncated to 10, or a conflicting READ takes the
      *> AT END path.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1803.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OUTF ASSIGN TO "G85BF1803.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS WS.
           SELECT INF ASSIGN TO "G85BF1803.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS RS.
       DATA DIVISION.
       FILE SECTION.
       FD  OUTF
           RECORD CONTAINS 3 TO 15 CHARACTERS.
       01  W3 PIC X(3).
       01  W8 PIC X(8).
       01  W15 PIC X(15).
       FD  INF
           RECORD CONTAINS 5 TO 10 CHARACTERS.
       01  R5 PIC X(5).
       01  R10 PIC X(10).
       WORKING-STORAGE SECTION.
       01  WS PIC XX.
       01  RS PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT OUTF.
           MOVE "AAA" TO W3.
           WRITE W3.
           MOVE "BBBBBBBB" TO W8.
           WRITE W8.
           MOVE "CCCCCCCCCCDDDDD" TO W15.
           WRITE W15.
           CLOSE OUTF.
           DISPLAY "WRITTEN " WS.
           OPEN INPUT INF.
           DISPLAY "OPEN " RS.
           READ INF
               AT END DISPLAY "READ-1 AT END"
               NOT AT END DISPLAY "READ-1 NOT AT END"
           END-READ.
           DISPLAY "READ-1 " RS " [" R10(1:3) "]".
           READ INF
               AT END DISPLAY "READ-2 AT END"
               NOT AT END DISPLAY "READ-2 NOT AT END"
           END-READ.
           DISPLAY "READ-2 " RS " [" R10(1:8) "]".
           READ INF
               AT END DISPLAY "READ-3 AT END"
               NOT AT END DISPLAY "READ-3 NOT AT END"
           END-READ.
           DISPLAY "READ-3 " RS " [" R10 "]".
           READ INF
               AT END DISPLAY "READ-4 AT END"
               NOT AT END DISPLAY "READ-4 NOT AT END"
           END-READ.
           DISPLAY "READ-4 " RS.
           CLOSE INF.
           STOP RUN.
