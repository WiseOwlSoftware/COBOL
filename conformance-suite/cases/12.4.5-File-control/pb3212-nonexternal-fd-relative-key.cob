      *>--- conformance-case
      *> id: pb3212-nonexternal-fd-relative-key
      *> rules: [GR-12.4.5.3-1]
      *> source-format: fixed
      *>---
      *> kb/Work PB3212 review R1-1 (a). ISO 2023 14.8.4.2: "For each
      *> external file connector, the file status, linage and relative
      *> key data items shall be external data items" (12.4.5.3 GR1 h).
      *> KEYS-FILE has no EXTERNAL clause, so 13.18.22.4 GR4 b) does not
      *> make its record's RK external, and RK cannot be the RELATIVE KEY
      *> of the EXTERNAL file REL-FILE.  The legal side, the same program
      *> with FD KEYS-FILE IS EXTERNAL, is
      *> conformance:pb3212-external-fd-item-relative-key.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3212N.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT KEYS-FILE ASSIGN TO "pb3212nk.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT REL-FILE ASSIGN TO "pb3212nr.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK.
       DATA DIVISION.
       FILE SECTION.
       FD KEYS-FILE.
       01 KEYS-REC.
          05 RK PIC 9(4).
          05 FILLER PIC X(6).
       FD REL-FILE IS EXTERNAL.
       01 REL-REC PIC X(10).
       PROCEDURE DIVISION.
       MAIN-P.
           STOP RUN.
