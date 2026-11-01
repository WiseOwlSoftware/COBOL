*>--- conformance-case
*> id: pb3953_function_argument_below_2002
*> rules: [RV-15.4.1-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3953, PB4296 - the negative of pb3953_function_argument_int128_window. FUNCTION ABS is an ISO/IEC 1989:2002
      *> introduction (Annex F.2 item 78 of that edition lists it among the new intrinsic functions; tests/version-matrix/
      *> intrinsic-introductions.json), so the program is refused by name below 2002 - COBOL1502, the D8 catalog window - at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3953FB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BIG PIC 9(18) VALUE 999999999999999999.
       01 K2  PIC 99    VALUE 99.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY FUNCTION ABS (BIG * BIG * K2)
           STOP RUN.
