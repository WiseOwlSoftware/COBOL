      *>--- conformance-case
      *> id: pb5069_external_padding_agrees
      *> rules: [GR-12.4.5.3-1]
      *> accept: [85, 2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5069 - 1985:VII.2.7.4 GR6: "all PADDING CHARACTER clauses in the run unit which are associated with that
      *> file connector must have the same specifications. If data-name-1 is specified, it must reference an external data
      *> item." (2002:12.3.4.3 GR1 h)). Both entries name EXT-PAD, an EXTERNAL one-character item. The clause is deleted
      *> from 2014. A, then B.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5069F.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EXT-FILE ASSIGN TO "PB5069X.DAT"
               PADDING CHARACTER IS EXT-PAD.
       DATA DIVISION.
       FILE SECTION.
       FD  EXT-FILE IS EXTERNAL.
       01  EXT-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  EXT-PAD IS EXTERNAL PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "A".
           CALL "PB5069G".
           STOP RUN.
       END PROGRAM PB5069F.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5069G.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EXT-FILE ASSIGN TO "PB5069X.DAT"
               PADDING CHARACTER IS EXT-PAD.
       DATA DIVISION.
       FILE SECTION.
       FD  EXT-FILE IS EXTERNAL.
       01  EXT-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  EXT-PAD IS EXTERNAL PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "B".
           EXIT PROGRAM.
       END PROGRAM PB5069G.
