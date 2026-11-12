      *>--- conformance-case
      *> id: pb5211-read-into-edited-refmod
      *> rules: [SR-14.9.30.3-1]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5211, the admitted twin of pb5211-read-into-alphabetic-refmod. The FD has two record
      *> descriptions, both alphanumeric groups, so READ ... INTO is admitted only when identifier-1 is a group item or
      *> an elementary item of category alphanumeric (§1985:IX.4.5.4 GR7 b; ISO 1989:2023 14.9.30.3 SR1 b).
      *> WS-EDIT is numeric edited, which no arm admits, but WS-EDIT (2:4) is a reference-modified item, and the
      *> categories numeric, numeric edited and alphanumeric edited become alphanumeric under reference modification
      *> (§1985:IV.4.3.8.4.4 GR5; ISO 1989:2023 8.4.3.3.4 GR6 c): the statement is legal. The record is moved to the slice
      *> as a group move, so the slice holds WXYZ: S=[WXYZ]. A compiler that reads the base item's category refuses it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5211P.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQF ASSIGN TO "pb5211p.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD SQF.
       01 REC-A.
          05 A-1 PIC X(4).
       01 REC-B.
          05 B-1 PIC X(4).
       WORKING-STORAGE SECTION.
       01 WS-EDIT PIC ZZZ9.99.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT SQF
           MOVE "WXYZ" TO A-1
           WRITE REC-A
           CLOSE SQF
           OPEN INPUT SQF
           READ SQF INTO WS-EDIT (2:4)
               AT END CONTINUE
           END-READ
           DISPLAY "S=[" WS-EDIT (2:4) "]"
           CLOSE SQF
           STOP RUN.
