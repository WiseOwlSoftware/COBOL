      *>--- conformance-case
      *> id: g2_use_file_phrase_not_ec_i_o
      *> rules: [2002-SR-14.8.45.2-13]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.45.2 SR13 - "Exception-name-2 shall be an
      *>   exception-name beginning with 'EC-I-O'." The FILE phrase here
      *>   follows EC-SIZE, which does not begin with 'EC-I-O': the
      *>   source violates the rule and is refused. Expected: COBOL0715.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2USE03N.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "g2use03n.dat" FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(4).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER EC EC-SIZE FILE F1.
       D1-P.
           DISPLAY "D1".
       END DECLARATIVES.
       M SECTION.
       M1.
           DISPLAY "OK"
           STOP RUN.
