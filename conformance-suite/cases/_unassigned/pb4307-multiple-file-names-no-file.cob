      *>--- conformance-case
      *> id: pb4307-multiple-file-names-no-file
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4307 - the twin of pb4307-multiple-file-tape-names-
      *>   no-file without the optional TAPE: MULTIPLE FILE [TAPE]
      *>   [CONTAINS] {file-name [POSITION integer]} ... requires a
      *>   file-name whichever optional words are written (DERIVED, not
      *>   quoted; VCR Table 7 row 7.6). Expected: COBOL5944 at every
      *>   edition. It fails if the clause is accepted with no file.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4307MF.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "PB4307B".
       I-O-CONTROL.
           MULTIPLE FILE.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(10).
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "OK"
           STOP RUN.
