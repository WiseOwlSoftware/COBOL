      *>--- conformance-case
      *> id: pb5167-file-control-without-entry
      *> rules: [1985-FMT-VII.2.2.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      * X3.23-1985 VII 2.2.2 prints "FILE-CONTROL. {file-control-
      * entry} ...": the entry is in braces, so one is required (IV
      * 2.1.4). The paragraph below has none; it compiled and ran at
      * 85 (kb/Work PB5167). ISO 2002 brackets the entries, so the
      * refusal is the construct row file-control-entry-optional-2002
      * (COBOL0900) and the program is accepted from 2002
      * (l1c10_file_control_no_entries).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5167F.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
       DATA DIVISION.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "EMPTY FILE-CONTROL".
           STOP RUN.
