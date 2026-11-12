      *>--- conformance-case
      *> id: 1985-SR-VII_3_2_3-3
      *> rules: [1985-SR-VII.3.2.3-3]
      *> source-format: fixed
      *>---
      *> §1985:VII.3.2.3 SR3 (THE FILE DESCRIPTION ENTRY): "One or
      *> more record description entries must follow the file
      *> description entry." The rule has no exception, so the RECORD
      *> clause of the FD below does not stand in for the record
      *> description entry it lacks (the 2002 and later editions
      *> permit that form; 1985 does not). A refusal at 85: a
      *> compiler that accepts the FD fails (kb/Work PB5071).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N5071SQ.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OUT-FILE ASSIGN TO "N5071SQ".
       DATA DIVISION.
       FILE SECTION.
       FD  OUT-FILE RECORD CONTAINS 10 CHARACTERS.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "DONE".
           STOP RUN.
