      *>--- conformance-case
      *> id: pb5412-write-from-renames-whole-record
      *> rules: [GR-14.9.51.4-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5412 - 1985:VII.4.7.3 SR1: "Record-name-1 and identifier-1
      *>   must not reference the same storage area." R1-ALL renames R1-A
      *>   THRU R1-B, the whole of R1 (2023:13.18.45.4 GR2); refused
      *>   COBOL6827 at 85 and 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5412N4.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "PB5412N4.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1.
           05 R1-A PIC X(4).
           05 R1-B PIC X(6).
       66  R1-ALL RENAMES R1-A THRU R1-B.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F1.
           WRITE R1 FROM R1-ALL.
           CLOSE F1.
           STOP RUN.
