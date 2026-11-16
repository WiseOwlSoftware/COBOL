      *>--- conformance-case
      *> id: g85_bf31_05
      *> rules: [1985-GR-X.4.5.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.4.5.4 GR1 - the external record "may be accessed and
      *> processed by any program in the run unit which describes and,
      *> optionally, redefines it". MAIN describes BF31-EXT as CODE
      *> X(2) + QTY 9(4) and stores AB/1234. R describes it as X(6) and
      *> REDEFINES it completely (GR2 permits a non-identical complete
      *> redefinition) as X(3)+X(3): R must see "AB1" and "234", and
      *> its MOVE "XY9" to the first half makes the record XY9234,
      *> which MAIN sees as "XY 9234". P describes it like MAIN, sees
      *> "XY 9234", ADDs 5 and MOVEs PQ: MAIN sees "PQ 9239". Each line
      *> fails if any program holds a private copy.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3105.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  BF31-EXT EXTERNAL.
           05  BF31-E-CODE PIC X(2).
           05  BF31-E-QTY  PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "AB" TO BF31-E-CODE.
           MOVE 1234 TO BF31-E-QTY.
           CALL "G85BF3105R".
           DISPLAY "MAIN " BF31-E-CODE " " BF31-E-QTY.
           CALL "G85BF3105P".
           DISPLAY "MAIN " BF31-E-CODE " " BF31-E-QTY.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3105R.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  BF31-EXT EXTERNAL PIC X(6).
       01  BF31-ALT REDEFINES BF31-EXT.
           05  BF31-ALT-1 PIC X(3).
           05  BF31-ALT-2 PIC X(3).
       PROCEDURE DIVISION.
       R-PARA.
           DISPLAY "R " BF31-ALT-1 " " BF31-ALT-2.
           MOVE "XY9" TO BF31-ALT-1.
           EXIT PROGRAM.
       END PROGRAM G85BF3105R.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3105P.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  BF31-EXT EXTERNAL.
           05  BF31-P-CODE PIC X(2).
           05  BF31-P-QTY  PIC 9(4).
       PROCEDURE DIVISION.
       P-PARA.
           DISPLAY "P " BF31-P-CODE " " BF31-P-QTY.
           ADD 5 TO BF31-P-QTY.
           MOVE "PQ" TO BF31-P-CODE.
           EXIT PROGRAM.
       END PROGRAM G85BF3105P.
       END PROGRAM G85BF3105.
