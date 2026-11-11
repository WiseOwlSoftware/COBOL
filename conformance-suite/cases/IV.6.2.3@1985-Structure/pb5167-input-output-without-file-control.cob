      *>--- conformance-case
      *> id: pb5167-input-output-without-file-control
      *> rules: [1985-FMT-IV.6.2.3.1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      * X3.23-1985 IV 6.2.3.1 prints the INPUT-OUTPUT SECTION as
      * "INPUT-OUTPUT SECTION. FILE-CONTROL. {file-control-entry} ...
      * [ I-O-CONTROL. ... ]": FILE-CONTROL is not bracketed. The
      * section below writes I-O-CONTROL alone; it compiled and ran at
      * 85 (kb/Work PB5167). ISO 2002 brackets the paragraph, so the
      * refusal is the construct row file-control-entry-optional-2002
      * (COBOL0900) and the program is accepted from 2002
      * (l1c16_io_section_paragraphs).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5167S.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       I-O-CONTROL.
       DATA DIVISION.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "NO FILE-CONTROL".
           STOP RUN.
