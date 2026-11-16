      *>--- conformance-case
      *> id: g2tn04_file_name_needs_ec_i_o
      *> rules: [2002-SR-7.2.19.2-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.19.2 SR5 - "If file-name-1 is specified,
      *>   exception-name-1 shall begin with the COBOL characters
      *>   'EC-I-O'." F1 is a declared file-name (SR1: a word not
      *>   beginning 'EC-' is file-name-1), written after EC-SIZE, which
      *>   does not begin EC-I-O. The program violates SR5 and nothing
      *>   else, so it shall be refused at the >>TURN line.
      *> Its accepted counterpart is g2tn03_file_name_after_ec_i_o.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2TN04.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "G2TN04.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 F1-REC PIC X(10).
       PROCEDURE DIVISION.
       >>TURN EC-SIZE F1 CHECKING ON
       M1.
           DISPLAY "A".
           STOP RUN.
