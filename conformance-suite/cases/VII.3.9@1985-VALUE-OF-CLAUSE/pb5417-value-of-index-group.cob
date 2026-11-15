      *>--- conformance-case
      *> id: pb5417-value-of-index-group
      *> rules: [1985-SR-VII.3.9.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5417. §1985:VII.3.9.3 SR1: "nor can data-name-1 be an item
      *> described with the USAGE IS INDEX clause". The clause may be written at
      *> a group level, where "it applies to each elementary item in the group"
      *> (§1985:VI.5.14.4 GR1), and the group's own entry is then described with
      *> it: G-IX (level 01) and H-IX (level 03) each write USAGE INDEX, so each
      *> is refused as data-name-1 (COBOL6849), as their elementary subordinates
      *> are (conformance:pb5070-value-of-index-item).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5417VG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "PB5417VG.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           VALUE OF ID IS G-IX RETENTION IS H-IX.
       01  R1 PIC X(10).
       WORKING-STORAGE SECTION.
       01  G-IX USAGE INDEX.
           05  A-IX.
           05  B-IX.
       01  G.
           03  H-IX USAGE INDEX.
               05  C-IX.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "OK".
           STOP RUN.
