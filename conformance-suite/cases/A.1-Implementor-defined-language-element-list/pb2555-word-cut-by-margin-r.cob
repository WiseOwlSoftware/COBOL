      *>--- conformance-case
      *> id: pb2555-word-cut-by-margin-r
      *> rules: [DOC-A.1-158]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO/IEC 1989:2023 6.3.1 (Annex A.1 item 158): margin R is position 72 here. END-SEND begins at
      *> position 66, so the program-text area holds END-SEN and the D. stands past margin R, ignored: the
      *> statement is followed by the word END-SEN (kb/Work PB2555 adds the note COBOL5788 beside the error).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2555N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-MSG PIC X(8) VALUE "MSG".
       01 WS-REPLY PIC X(8).
       PROCEDURE DIVISION.
           SEND TO LINAGE-COUNTER FROM WS-MSG RETURNING WS-REPLY END-SEND.
           STOP RUN.
