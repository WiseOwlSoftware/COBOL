      *>--- conformance-case
      *> id: pb5069_external_entries_agree
      *> rules: [GR-12.4.5.3-1]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5069 - every edition requires the file control entries of the run unit that describe one external
      *> file connector to agree, and the RELATIVE KEY item to be the same external data item (1985:VIII.2.3.4 GR1,
      *> 1985:VIII.2.4.4 GR8; 2002:12.3.4.3 GR1; 2023:12.4.5.3 GR1). Both programs here write identical entries,
      *> whose RELATIVE KEY is an item of one EXTERNAL record, so the group compiles and runs: A, then B.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5069A.
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
       01  EXT-KEYS IS EXTERNAL.
           05  REL-KEY PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "A".
           CALL "PB5069B".
           STOP RUN.
       END PROGRAM PB5069A.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5069B.
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
       01  EXT-KEYS IS EXTERNAL.
           05  REL-KEY PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "B".
           EXIT PROGRAM.
       END PROGRAM PB5069B.
