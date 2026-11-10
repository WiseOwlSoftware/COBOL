      *>--- conformance-case
      *> id: pb5069-external-padding-differs
      *> rules: [GR-12.4.5.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5069 - 1985:VII.2.7.4 GR6: "all PADDING CHARACTER clauses in the run unit which are associated with that
      *> file connector must have the same specifications" (2002:12.3.4.3 GR1 h)). PB5069H pads with "*", PB5069I with
      *> "#". conformance:pb5069_external_padding_agrees is the legal side.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5069H.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EXT-FILE ASSIGN TO "PB5069X.DAT"
               PADDING CHARACTER IS "*".
       DATA DIVISION.
       FILE SECTION.
       FD  EXT-FILE IS EXTERNAL.
       01  EXT-REC PIC X(10).
       WORKING-STORAGE SECTION.
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "PB5069I".
           STOP RUN.
       END PROGRAM PB5069H.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5069I.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EXT-FILE ASSIGN TO "PB5069X.DAT"
               PADDING CHARACTER IS "#".
       DATA DIVISION.
       FILE SECTION.
       FD  EXT-FILE IS EXTERNAL.
       01  EXT-REC PIC X(10).
       WORKING-STORAGE SECTION.
       PROCEDURE DIVISION.
       MAIN-PARA.
           EXIT PROGRAM.
       END PROGRAM PB5069I.
