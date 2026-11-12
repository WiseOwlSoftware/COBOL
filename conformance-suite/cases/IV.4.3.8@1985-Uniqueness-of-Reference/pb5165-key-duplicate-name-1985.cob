      *>--- conformance-case
      *> id: pb5165-key-duplicate-name-1985
      *> rules: [1985-SR-IV.4.3.8.1-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5165 - §1985:IV.4.3.8.1 SR3 (cite.py --check OK): "Identical
      *> user-defined names may appear in a source program; however, uniqueness
      *> must then be established through qualification for each user-defined
      *> name explicitly referenced, except in the case of redefinition." K, the key of the table, is declared in R1 and in R2 and written with no qualifier.
      *> Written with the qualifier the clause admits it is legal (§1985:VI.5.8.3
      *> SR2 and §1985:VI.5.11.3 SR5: "may be qualified"). From 2002 the groups
      *> above the clause's subject are implicit qualifiers (§2002:F.2, item 67;
      *> pb5165_implicit_qualifier_entry_clauses_2002).
      *> Fails if: the program compiles at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5165KY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  R1.
           05  TK OCCURS 3 TIMES ASCENDING KEY IS K.
               10  K PIC 9.
       01  R2.
           05  K PIC 9.
       PROCEDURE DIVISION.
       P1.
           STOP RUN.
