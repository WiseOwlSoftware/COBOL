      *>--- conformance-case
      *> id: pb4412-vanished-statement-beside-omitted-branch
      *> rules: [DOC-A.1-158]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO/IEC 1989:2023 6.3.1 (Annex A.1 item 158) and 7.3.16.4 GR 3. The CALL ends after BY because the
      *> line that holds CONTENT WS-REPLY ONLY past margin R reads as blank; the false >>IF branch above it
      *> is omitted, its own past-margin-R line with it. The parse fails at STOP, and the note COBOL5788
      *> (kb/Work PB4412) names the LIVE line only, pinned by unit:MarginRCutNoteTests.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4412N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-MSG PIC X(8) VALUE "MSG".
       01 WS-REPLY PIC X(8).
       PROCEDURE DIVISION.
           CALL "SUBPROG" USING WS-MSG BY
       >>IF TRACING IS DEFINED
                                                                        DISPLAY "TRACE"
       >>END-IF
                                                                        CONTENT WS-REPLY
           STOP RUN.
