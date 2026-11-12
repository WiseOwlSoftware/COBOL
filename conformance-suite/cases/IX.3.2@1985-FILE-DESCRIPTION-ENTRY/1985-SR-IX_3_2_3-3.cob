      *>--- conformance-case
      *> id: 1985-SR-IX_3_2_3-3
      *> rules: [1985-SR-IX.3.2.3-3]
      *> source-format: fixed
      *>---
      *> §1985:IX.3.2.3 SR3 (THE FILE DESCRIPTION ENTRY, indexed
      *> I-O): "One or more record description entries must follow
      *> the file description entry." The indexed file's FD below has
      *> a RECORD clause and no record description entry, which the
      *> rule forbids. A refusal at 85: a compiler that accepts the
      *> FD fails (kb/Work PB5071).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N5071IX.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IDX-FILE ASSIGN TO "N5071IX"
               ORGANIZATION IS INDEXED
               RECORD KEY IS IDX-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  IDX-FILE RECORD CONTAINS 10 CHARACTERS.
       WORKING-STORAGE SECTION.
       01  IDX-KEY PIC X(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "DONE".
           STOP RUN.
