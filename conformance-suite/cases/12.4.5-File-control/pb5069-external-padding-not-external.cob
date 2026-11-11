      *>--- conformance-case
      *> id: pb5069-external-padding-not-external
      *> rules: [GR-12.4.5.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5069 - 1985:VII.2.7.4 GR6: "If data-name-1 is specified, it must reference an external data item."
      *> (2002:12.3.4.3 GR1 h)). WS-PAD has no EXTERNAL clause. conformance:pb5069_external_padding_agrees is the legal side.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5069J.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EXT-FILE ASSIGN TO "PB5069X.DAT"
               PADDING CHARACTER IS WS-PAD.
       DATA DIVISION.
       FILE SECTION.
       FD  EXT-FILE IS EXTERNAL.
       01  EXT-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  WS-PAD PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
