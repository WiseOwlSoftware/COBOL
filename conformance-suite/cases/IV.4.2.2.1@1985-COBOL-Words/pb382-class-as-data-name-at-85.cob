      *>--- conformance-case
      *> id: pb382-class-as-data-name-at-85
      *> rules: [1985-SR-IV.4.2.2.1-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB382. §1985:IV.4.2.2.1.3: a reserved word "must not
      *> appear in the programs as user-defined words", and
      *> §1985:IV.4.2.2.1 SR1: "Reserved words form disjoint sets with
      *> user-defined words". CLASS is reserved at 85, and 1985 has no
      *> CLASS clause in a data description entry, so the entry writes
      *> CLASS as its data-name. The refusal names the word (COBOL0901),
      *> never a parse error at PIC.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB382C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OK1 PIC X.
       01 CLASS PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
