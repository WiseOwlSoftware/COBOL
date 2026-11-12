      *>--- conformance-case
      *> id: 1985-SR-VIII_3_2_3-3
      *> rules: [1985-SR-VIII.3.2.3-3]
      *> source-format: fixed
      *>---
      *> §1985:VIII.3.2.3 SR3 (THE FILE DESCRIPTION ENTRY, relative
      *> I-O): "One or more record description entries must follow
      *> the file description entry." The relative file's FD below
      *> has a RECORD clause and no record description entry, which
      *> the rule forbids at 85 (the 2002 and later editions permit
      *> it). A refusal: a compiler that accepts the FD fails
      *> (kb/Work PB5071).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N5071RL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REL-FILE ASSIGN TO "N5071RL"
               ORGANIZATION IS RELATIVE.
       DATA DIVISION.
       FILE SECTION.
       FD  REL-FILE RECORD CONTAINS 10 CHARACTERS.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "DONE".
           STOP RUN.
