      *>--- conformance-case
      *> id: pb3212-external-fd-key-positions-differ
      *> rules: [GR-12.4.5.3-1]
      *> source-format: fixed
      *>---
      *> kb/Work PB3212 review R1-1 (b). ISO 2023 14.8.4.2: the relative
      *> key data items of an external file connector "shall refer to
      *> the same corresponding storage in each runtime element for
      *> which the file connector is used" (12.4.5.3 GR1 h: "The same
      *> specification of the RELATIVE KEY clause").  Both programs name
      *> RK of the EXTERNAL file KEYS-FILE's record, but PB3212P puts it
      *> at character positions 1-4 and PB3212Q at 7-10 of the one shared
      *> record area, so they are not the same corresponding item.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3212P.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT KEYS-FILE ASSIGN TO "pb3212pk.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT REL-FILE ASSIGN TO "pb3212pr.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK.
       DATA DIVISION.
       FILE SECTION.
       FD KEYS-FILE IS EXTERNAL.
       01 KEYS-REC.
          05 RK PIC 9(4).
          05 FILLER PIC X(6).
       FD REL-FILE IS EXTERNAL.
       01 REL-REC PIC X(10).
       PROCEDURE DIVISION.
       MAIN-P.
           CALL "PB3212Q".
           STOP RUN.
       END PROGRAM PB3212P.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3212Q.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT KEYS-FILE ASSIGN TO "pb3212pk.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT REL-FILE ASSIGN TO "pb3212pr.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK.
       DATA DIVISION.
       FILE SECTION.
       FD KEYS-FILE IS EXTERNAL.
       01 KEYS-REC.
          05 FILLER PIC X(6).
          05 RK PIC 9(4).
       FD REL-FILE IS EXTERNAL.
       01 REL-REC PIC X(10).
       PROCEDURE DIVISION.
       MAIN-P.
           EXIT PROGRAM.
       END PROGRAM PB3212Q.
