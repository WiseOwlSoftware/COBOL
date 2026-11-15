      *>--- conformance-case
      *> id: g2_use_file_phrase_ec_i_o
      *> rules: [2002-SR-14.8.45.2-13]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.45.2 SR13 - "Exception-name-2 shall be an
      *>   exception-name beginning with 'EC-I-O'." The legal form: the
      *>   FILE phrase follows EC-I-O (level 2) and EC-I-O-PERMANENT-ERROR
      *>   (level 3), each beginning with 'EC-I-O'; the program compiles
      *>   and runs. Its refused twin g2_use_file_phrase_not_ec_i_o writes
      *>   FILE after EC-SIZE.
      *> Each leg can fail: a FILE phrase refused after an EC-I-O name
      *>   fails the compile.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2USE03.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "g2use03.dat" FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(4).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER EC EC-I-O FILE F1.
       D1-P.
           DISPLAY "D1".
       D2 SECTION.
           USE AFTER EXCEPTION CONDITION EC-I-O-PERMANENT-ERROR
               FILE F1.
       D2-P.
           DISPLAY "D2".
       END DECLARATIVES.
       M SECTION.
       M1.
           DISPLAY "OK"
           STOP RUN.
