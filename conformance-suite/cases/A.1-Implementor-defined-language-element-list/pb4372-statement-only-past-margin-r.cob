      *>--- conformance-case
      *> id: pb4372-statement-only-past-margin-r
      *> rules: [DOC-A.1-158]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO/IEC 1989:2023 6.3.1 (Annex A.1 item 158): margin R is position 72 here, and text past it is not
      *> program text. The line after BY holds CONTENT WS-REPLY ONLY from position 73, so the reference format
      *> reads it as blank, the CALL ends after BY, and the parse fails at STOP (kb/Work PB4372 names the
      *> blank line in the note COBOL5788 beside the error, pinned by unit:MarginRCutNoteTests).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4372N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-MSG PIC X(8) VALUE "MSG".
       01 WS-REPLY PIC X(8).
       PROCEDURE DIVISION.
           CALL "SUBPROG" USING WS-MSG BY
                                                                        CONTENT WS-REPLY
           STOP RUN.
