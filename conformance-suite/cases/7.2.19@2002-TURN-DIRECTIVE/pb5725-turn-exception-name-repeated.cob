      *>--- conformance-case
      *> id: pb5725-turn-exception-name-repeated
      *> rules: [2002-SR-7.2.19.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.19.2 SR3 - "No exception-name shall be specified
      *>   more than once in one TURN directive."
      *>   cite.py: OK  §2002:7.2.19.2 SR3
      *> §2014:7.3.20.2 SR3 says the same (cite.py: OK).
      *> NEGATIVE: the directive names EC-I-O twice, each time with a
      *> different file-name. The prohibition grants no form; the
      *> compiler shall refuse the directive line at 2002 and 2014.
      *> (2023 SR3 keys on the name and file combination and accepts it:
      *> 7.3.25-TURN/pb5725_turn_name_with_two_files.)
       >>TURN EC-I-O F1 EC-I-O F2 CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5725TN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb5725n1.dat" ORGANIZATION SEQUENTIAL.
           SELECT F2 ASSIGN TO "pb5725n2.dat" ORGANIZATION SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(4).
       FD F2.
       01 R2 PIC X(4).
       PROCEDURE DIVISION.
           DISPLAY "NOT ACCEPTED".
           STOP RUN.
