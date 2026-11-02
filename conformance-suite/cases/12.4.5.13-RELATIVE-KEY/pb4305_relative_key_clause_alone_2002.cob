      *>--- conformance-case
      *> id: pb4305_relative_key_clause_alone_2002
      *> rules: [FMT-12.4.5.13.2]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4305 - 12.4.5.13.2 prints the RELATIVE KEY clause as
      *>   a clause of its own, and no rule ties it to the ACCESS MODE
      *>   clause (2002 Annex F.2 item 112: "A RELATIVE KEY clause may
      *>   be specified by itself in a file control entry"). The entry
      *>   writes it with no ACCESS MODE clause. Expected: "OK" (the
      *>   program translates and runs). It fails if the clause is
      *>   refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4305RK.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "PB4305RK"
               ORGANIZATION IS RELATIVE
               RELATIVE KEY IS RK.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R PIC X(10).
       WORKING-STORAGE SECTION.
       01 RK PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "OK"
           STOP RUN.
