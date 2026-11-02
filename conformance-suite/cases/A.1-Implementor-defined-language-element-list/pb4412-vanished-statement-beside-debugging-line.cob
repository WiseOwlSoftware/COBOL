      *>--- conformance-case
      *> id: pb4412-vanished-statement-beside-debugging-line
      *> rules: [DOC-A.1-158]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO/IEC 1989:2023 6.3.1 (Annex A.1 item 158). As pb4412-vanished-statement-beside-omitted-branch,
      *> with a debugging line in place of the omitted branch: the program names no WITH DEBUGGING MODE, so
      *> the D line is a comment line and the note COBOL5788 (kb/Work PB4412) names the LIVE line only.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4412N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-MSG PIC X(8) VALUE "MSG".
       01 WS-REPLY PIC X(8).
       PROCEDURE DIVISION.
           CALL "SUBPROG" USING WS-MSG BY
      D                                                                 DISPLAY "TRACE"
                                                                        CONTENT WS-REPLY
           STOP RUN.
