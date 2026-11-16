      *>--- conformance-case
      *> id: pb5647-picture-long-s-no-currency
      *> rules: [SR-13.18.40.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5647 (review R1-3) - a PICTURE that writes LATIN SMALL LETTER LONG S (U+017F) with no currency clause.
      *> Below 2014 the case equivalence is between COBOL basic letters only (§2002:8.1.2.1 GR3), so U+017F is no lowercase S
      *> and no picture symbol: refused. From 2014 the invariant upper case S applies (S9 binds), so the program is
      *> refused at 85 and 2002 only.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5647N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N1 PIC ſ9 VALUE -5.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY N1
           STOP RUN.
