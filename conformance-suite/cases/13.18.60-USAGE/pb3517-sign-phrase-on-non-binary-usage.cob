      *>--- conformance-case
      *> id: pb3517-sign-phrase-on-non-binary-usage
      *> rules: [FMT-13.18.60.2, GR-13.18.60.4-12]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 13.18.60.2 general format (the printed page, rendered):
      *> the SIGNED / UNSIGNED choice is printed only after
      *> BINARY-CHAR, BINARY-SHORT, BINARY-LONG and BINARY-DOUBLE,
      *> and 13.18.60.4 GR12 tabulates "BINARY-CHAR UNSIGNED" and
      *> its siblings, no other usage. COMP, DISPLAY and
      *> PACKED-DECIMAL print no such phrase, so each of the three
      *> entries below is nonconforming; each fails alone if the
      *> phrase is accepted (and ignored) on its usage, as it was
      *> before kb/Work PB3517 (the program printed 07 AB -005).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3517N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-U PIC 9(2) USAGE COMP UNSIGNED VALUE 7.
       01 WS-X PIC X(2) USAGE DISPLAY SIGNED VALUE "AB".
       01 WS-P PIC S9(3) PACKED-DECIMAL UNSIGNED VALUE -5.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY WS-U " " WS-X " " WS-P
           STOP RUN.
