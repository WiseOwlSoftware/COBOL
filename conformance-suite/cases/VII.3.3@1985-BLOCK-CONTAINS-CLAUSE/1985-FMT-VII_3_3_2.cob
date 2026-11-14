      *>--- conformance-case
      *> id: 1985-FMT-VII_3_3_2
      *> rules: [1985-FMT-VII.3.3.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5115 - §1985:VII.3.3.2 General Format: BLOCK CONTAINS
      *> [integer-1 TO] integer-2 {RECORDS | CHARACTERS}; the clause has no
      *> syntax rules, and §1985:VII.3.3.3 GR4 says only that integer-1 and
      *> integer-2 "refer to the minimum and maximum size of the physical
      *> record, respectively" (cite.py OK). So equal bounds (2 TO 2) translate
      *> at 85 and the file works as any other. The 2002 text first requires
      *> integer-2 to be greater than integer-1 (§2002:13.16.10.2 SR1): the
      *> twin pb1217-block-contains-equal-bounds is refused from 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. BLKEQUAL85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FILE-D ASSIGN TO "BLKEQUAL85.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  FILE-D
           BLOCK CONTAINS 2 TO 2 RECORDS.
       01  REC-D               PIC X(4).
       WORKING-STORAGE SECTION.
       01  WS-EOF              PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT FILE-D.
           MOVE "D001" TO REC-D. WRITE REC-D.
           MOVE "D002" TO REC-D. WRITE REC-D.
           MOVE "D003" TO REC-D. WRITE REC-D.
           CLOSE FILE-D.
           OPEN INPUT FILE-D.
           PERFORM READ-D UNTIL WS-EOF = "Y".
           CLOSE FILE-D.
           STOP RUN.
       READ-D.
           READ FILE-D
               AT END MOVE "Y" TO WS-EOF
                      DISPLAY "D END".
           IF WS-EOF = "N"
               DISPLAY REC-D.
