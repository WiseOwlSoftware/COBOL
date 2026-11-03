      *>--- conformance-case
      *> id: pb4307-multiple-file-tape-names-no-file
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4307 - the X3.23-1985 MULTIPLE FILE clause states
      *>   which files share one reel; its format is MULTIPLE FILE
      *>   [TAPE] [CONTAINS] {file-name [POSITION integer]} ..., the
      *>   file list in braces with an ellipsis, so it is required
      *>   (DERIVED, not quoted: no 1985 text is held; 2002 Annex F.1
      *>   removes the clause and 2023 prints no format for it; GnuCOBOL
      *>   -std=cobol85, ibm and mf refuse it; VCR Table 7 row 7.6).
      *>   The clause below names no file. Expected: COBOL5944 at every
      *>   edition (from 2002 the clause is COBOL0902 as well). It fails
      *>   if the empty clause is accepted, as it was at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4307MFT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "PB4307A".
       I-O-CONTROL.
           MULTIPLE FILE TAPE.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(10).
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "OK"
           STOP RUN.
