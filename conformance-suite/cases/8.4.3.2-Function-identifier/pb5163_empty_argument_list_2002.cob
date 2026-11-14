      *>--- conformance-case
      *> id: pb5163_empty_argument_list_2002
      *> rules: [FMT-8.4.3.2.2, SR-8.4.3.2.3-6]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5163: an empty argument list after the word FUNCTION
      *> and a function name is a COBOL-2002 form. ISO/IEC 1989:2002
      *> 8.4.2.2.1 brackets the argument inside the parentheses, and the
      *> NOTE of its 8.4.2.2.2 SR4 writes "FUNCTION MAX (FUNCTION RANDOM
      *> () A B)"; the 1985 format (IV.4.3.8.3.2) writes the parentheses
      *> only around one or more arguments (the refusal at 85 is
      *> cases/IV.4.3.8.3@1985-Function-Identifier/pb5163-empty-argument-
      *> list-85).
      *>   RANDOM (5) starts the seed-5 sequence and RANDOM () returns
      *>   its next number, the same as RANDOM with no list at all
      *>   (§2023:15.75.3 AR5): SAME.
      *>   CURRENT-DATE () is the 21-character date (§2023:15.21): 21.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5163A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  R1 PIC 9V9(6).
       01  R2 PIC 9V9(6).
       01  N  PIC 99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R1 = FUNCTION RANDOM (5).
           COMPUTE R1 = FUNCTION RANDOM.
           COMPUTE R2 = FUNCTION RANDOM (5).
           COMPUTE R2 = FUNCTION RANDOM ().
           IF R1 = R2 DISPLAY "SAME" ELSE DISPLAY "DIFFERENT".
           COMPUTE N = FUNCTION LENGTH (FUNCTION CURRENT-DATE ()).
           DISPLAY N.
           STOP RUN.
