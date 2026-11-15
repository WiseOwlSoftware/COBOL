      *>--- conformance-case
      *> id: pb5566-record-key-ambiguous
      *> rules: [1985-SR-IX.2.7.3-1]
      *> source-format: fixed
      *>---
      *> kb/Work PB5566. §1985:IX.2.7.3 SR1 (cite.py: OK): "Data-name-1
      *> may be qualified." §1985:IV.4.3.8.1 SR3 requires qualification
      *> for each user-defined name explicitly referenced, and
      *> §2023:8.4.2.2.3 SR1 (cite.py: OK) requires that "uniqueness
      *> shall be established through a sequence of qualifiers". K is
      *> declared in the file's record and again in working storage, so
      *> the unqualified RECORD KEY K names two items. The key's
      *> SR2 (the key lies in the file's record) is a constraint on the
      *> item, not a way to pick between two. A refusal at every
      *> edition: a compiler that quietly takes the record's K fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5566A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "PB5566A.dat" ORGANIZATION INDEXED
               RECORD KEY IS K.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  FR.
           05 K PIC XX.
       WORKING-STORAGE SECTION.
       01  K PIC XX.
       PROCEDURE DIVISION.
       P0.
           OPEN OUTPUT F.
           CLOSE F.
           DISPLAY "RK"
           STOP RUN.
