      *>--- conformance-case
      *> id: pb2555-statement-cut-at-margin-r
      *> rules: [DOC-A.1-158]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO/IEC 1989:2023 6.3.1 (Annex A.1 item 158): margin R is position 72 here, and text past it is not
      *> program text. The CALL below is complete only with BY CONTENT WS-REPLY, which stands from position
      *> 73 and is therefore ignored: the statement ends after BY, and the parse fails at STOP (kb/Work PB2555
      *> adds the note COBOL5788 beside the error, pinned by unit:MarginRCutNoteTests).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2555N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-MSG PIC X(8) VALUE "MSG".
       01 WS-REPLY PIC X(8).
       PROCEDURE DIVISION.
           CALL "SUBPROG" USING WS-MSG                               BY CONTENT WS-REPLY
           STOP RUN.
