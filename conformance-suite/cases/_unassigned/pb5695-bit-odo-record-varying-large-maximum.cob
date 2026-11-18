      *>--- conformance-case
      *> id: pb5695-bit-odo-record-varying-large-maximum
      *> rules: []
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5695 review R2-2. A record of a USAGE BIT item and an
      *> OCCURS DEPENDING table, in a file whose RECORD VARYING maximum is
      *> 500000000 characters. The 2023 READ asks whether the DEPENDING ON
      *> item makes the record longer than that maximum (9.1.13.6 item 4 b),
      *> and the compiler searched for the largest count that fits by sizing
      *> the table at counts past its maximum of 10; the size limit's one
      *> narrowing refused those hypothetical sizes and the compiler crashed.
      *> The program is legal and small (1 + 10 x 100 characters at most).
      *> Derived: N = 2 writes the bit byte and two occurrences, 201
      *> characters, within every maximum; the READ is successful, status
      *> "00", and lands the two occurrences written:
      *> "AB" and "CD". So the program displays READ 00 ABCD.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5695OB.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb5695ob.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD F RECORD VARYING IN SIZE FROM 1 TO 500000000 CHARACTERS.
       01 R.
          05 B PIC 1 USAGE BIT.
          05 T OCCURS 1 TO 10 DEPENDING ON N.
             10 E PIC X(100).
       WORKING-STORAGE SECTION.
       01 N PIC 9(4) BINARY VALUE 2.
       01 FS PIC XX.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE B"1" TO B.
           MOVE "AB" TO E (1).
           MOVE "CD" TO E (2).
           OPEN OUTPUT F.
           WRITE R.
           CLOSE F.
           MOVE SPACES TO E (1) E (2).
           OPEN INPUT F.
           READ F
               AT END DISPLAY "EOF"
               NOT AT END
                   DISPLAY "READ " FS " " E (1) (1:2) E (2) (1:2)
           END-READ.
           CLOSE F.
           STOP RUN.
