      *>--- conformance-case
      *> id: g2tn03_file_name_after_ec_i_o
      *> rules: [2002-SR-7.2.19.2-5]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.19.2 SR5 - "If file-name-1 is specified,
      *>   exception-name-1 shall begin with the COBOL characters
      *>   'EC-I-O'." Both directives below satisfy it: file-name F1
      *>   follows EC-I-O itself, and F1 F2 follow EC-I-O-AT-END, a name
      *>   beginning EC-I-O. The program shall compile and run; its
      *>   refused counterpart g2tn04_file_name_needs_ec_i_o writes F1
      *>   after EC-SIZE. Together they pin the prefix test: this case
      *>   fails if every file-name is refused, g2tn04 if none is.
      *> EXPECTED OUTPUT, DERIVED: COMPILED (the only DISPLAY; no file
      *>   is opened, so no EC-I-O condition can occur).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2TN03.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "G2TN03A.DAT".
           SELECT F2 ASSIGN TO "G2TN03B.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 F1-REC PIC X(10).
       FD F2.
       01 F2-REC PIC X(10).
       PROCEDURE DIVISION.
       >>TURN EC-I-O F1 CHECKING ON
       >>TURN EC-I-O-AT-END F1 F2 CHECKING ON
       M1.
           DISPLAY "COMPILED".
           STOP RUN.
