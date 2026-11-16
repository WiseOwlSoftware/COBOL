      *>--- conformance-case
      *> id: pb5627-ambiguous-record-key-85
      *> rules: [1985-SR-IV.4.3.8.1-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5627. §1985:IV.4.3.8.1 SR3 (cite.py: OK): "uniqueness
      *> must then be established through qualification for each
      *> user-defined name explicitly referenced". RECORD KEY IS K names a
      *> K in the file's record and a K in Working-Storage, and nothing
      *> qualifies it. The refusal (COBOL1639) names that rule at
      *> COBOL-85, not §2023:8.4.2.2.3 SR1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5627E.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb5627e.txt"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS K.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  F-REC.
           05  K PIC X(4).
           05  F-DATA PIC X(4).
       WORKING-STORAGE SECTION.
       01  W-G.
           05  K PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
