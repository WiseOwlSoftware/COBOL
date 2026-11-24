      *>--- conformance-case
      *> id: pb5725_turn_name_with_two_files
      *> rules: [SR-7.3.25.3-3]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5725 - what "specified more than once" means in a
      *> TURN directive is the edition's. §2023:7.3.25.3 SR3: "No
      *> exception-name-1 and file-name-1 combination shall be specified
      *> more than once in a TURN directive."
      *>   cite.py: OK  §2023:7.3.25.3 SR3
      *> The directive below names EC-I-O twice, each time with another
      *> file-name, so no COMBINATION repeats and 2023 accepts it.
      *> §2002:7.2.19.2 SR3 and §2014:7.3.20.2 SR3 key on the name alone:
      *> "No exception-name shall be specified more than once in one TURN
      *> directive" (cite.py: OK for both), so at 2002 and 2014 the same
      *> directive is refused, COBOL0718
      *> (wiseowl/diagnostics/pb5725-turn-exception-name-repeated.json).
       >>TURN EC-I-O F1 EC-I-O F2 CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5725TF.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb5725f1.dat" ORGANIZATION SEQUENTIAL.
           SELECT F2 ASSIGN TO "pb5725f2.dat" ORGANIZATION SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(4).
       FD F2.
       01 R2 PIC X(4).
       PROCEDURE DIVISION.
           DISPLAY "OK".
           STOP RUN.
