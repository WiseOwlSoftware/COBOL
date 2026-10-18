*>--- conformance-case
*> id: pb1563-line-sequential-dynamic-members-below-2023
*> rules: [GR-12.4.5.10.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1563 - a LINE SEQUENTIAL record holding dynamic-length members is read back by the take
      *> step (determination D-FRA (x), docs/CONFORMANCE.md section 3), but the LINE SEQUENTIAL phrase is a
      *> COBOL-2023 introduction (12.4.5.10.3 GR2; the same refusal as pb688-line-sequential-below-2023)
      *> and the DYNAMIC LENGTH clause a COBOL-2014 one (8.5.1.10 / 13.18.19), so below 2023 the program
      *> is refused by the edition gate.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1563NEG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb1563neg.dat"
               ORGANIZATION IS LINE SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R.
          05 A PIC X DYNAMIC LENGTH LIMIT 5.
          05 K PIC X(3).
          05 C PIC X DYNAMIC LENGTH LIMIT 5.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F
           MOVE "AA" TO A MOVE "KEY" TO K MOVE "CCCC" TO C
           WRITE R
           CLOSE F
           STOP RUN.
