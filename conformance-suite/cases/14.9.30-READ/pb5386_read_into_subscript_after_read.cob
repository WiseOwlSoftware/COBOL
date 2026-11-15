      *>--- conformance-case
      *> id: pb5386_read_into_subscript_after_read
      *> rules: [GR-14.9.30.4-4]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *>---
      *> kb/Work PB5386. READ ... INTO identifier-1 identifies
      *> identifier-1 after the record is read: "Item identification
      *> of the data item referenced by identifier-1 is done after the
      *> record has been read and immediately before it is moved to
      *> the data item" (ISO 14.9.30.4 GR4 b); in 1985 "Any
      *> subscripting associated with identifier-1 is evaluated after
      *> the record has been read" (VII.4.4.4 GR8b). The subscript SEQ
      *> is in the record read, so A3 goes to WE (3), B1 to WE (1) and
      *> C2 to WE (2). The FD's two record descriptions make SEQ a
      *> window of the shared record area, the subscript form that was
      *> evaluated before the READ. The fourth READ is at end: the
      *> implied MOVE does not occur and WN keeps its value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5386RD.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb5386rd.dat"
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  FR.
           05 K1 PIC X.
           05 SEQ PIC 9.
       01  FR-ALT PIC X(2).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       01  WT.
           05 WE PIC X(2) OCCURS 3 VALUE "..".
       01  WN PIC X(2) VALUE "IN".
       PROCEDURE DIVISION.
       M1.
           OPEN OUTPUT F.
           MOVE "A3" TO FR. WRITE FR.
           MOVE "B1" TO FR. WRITE FR.
           MOVE "C2" TO FR-ALT. WRITE FR-ALT.
           CLOSE F.
           OPEN INPUT F.
           MOVE 1 TO SEQ.
           PERFORM 3 TIMES
             READ F INTO WE (SEQ) AT END DISPLAY "EARLY END"
             END-READ
           END-PERFORM.
           READ F INTO WN AT END DISPLAY "AT END " FS
           END-READ.
           CLOSE F.
           DISPLAY "WT[" WT "] WN[" WN "]".
           STOP RUN.
