*>--- conformance-case
*> id: pb5640-function-arg-bit-refmod-data-length
*> rules: [2002-SR-8.4.2.2.2-12]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5640 review R1-2. §2002:8.4.2.2.2 SR12 (§2014:8.4.2.2.2 SR14): a bit argument of a function whose formal is BY REFERENCE
      *> has reference modification of fixed-point literals only, the length included, so B8(1:L) with the data-name L is refused at 2002 and 2014
      *> (COBOL2471). §2023:8.4.3.2.3 SR14 narrows it to the leftmost position.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. N5640FF.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 1(8) USAGE BIT.
       01 L-R PIC X(8).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
           MOVE L-X TO L-R
           GOBACK.
       END FUNCTION N5640FF.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. N5640FM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION N5640FF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 L PIC 9(2) VALUE 8.
       01 BG.
          05 B8 PIC 1(16) USAGE BIT VALUE B"1111111100000000".
       01 WR PIC X(8).
       PROCEDURE DIVISION.
           MOVE FUNCTION N5640FF(B8(1:L)) TO WR
           DISPLAY "R=" WR
           STOP RUN.
       END PROGRAM N5640FM.
