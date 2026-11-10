      *>--- conformance-case
      *> id: pb5069-external-relative-key-not-external
      *> rules: [GR-12.4.5.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5069 - 1985:VIII.2.4.4 GR8: "data-name-1 must reference an external data item" when the file connector
      *> is external (2002:12.3.4.3 GR1 i), 2023:12.4.5.3 GR1 h)). REL-KEY belongs to KEY-AREA, a record with no EXTERNAL
      *> clause. conformance:pb5069_external_entries_agree is the legal side.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5069E.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EXT-FILE ASSIGN TO "PB5069X.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS REL-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  EXT-FILE IS EXTERNAL.
       01  EXT-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  KEY-AREA.
           05  REL-KEY PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
