      *>--- conformance-case
      *> id: pb3517-no-sign-phrase-on-binary-usage
      *> rules: [FMT-13.18.60.2, GR-13.18.60.4-11]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 13.18.60.2 general format (the printed page, rendered):
      *> WITH NO SIGN is printed only on the PACKED-DECIMAL line,
      *> "PACKED-DECIMAL [ WITH NO SIGN ]", and 13.18.60.4 GR11
      *> gives it its meaning there alone ("If the WITH NO SIGN
      *> phrase is specified the representation of the data item
      *> ... reserves no storage for representing any sign value").
      *> BINARY prints no such phrase. The refusal had no witness
      *> before kb/Work PB3517 swept the clause's phrases.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3517N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-B PIC 9(4) USAGE BINARY WITH NO SIGN VALUE 12.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY WS-B
           STOP RUN.
