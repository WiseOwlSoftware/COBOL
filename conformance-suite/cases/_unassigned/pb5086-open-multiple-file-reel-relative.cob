      *>--- conformance-case
      *> id: pb5086-open-multiple-file-reel-relative
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5086 - §1985:VII.4.3.4 GR20: "Whenever a set of
      *> files resides on a multiple file reel, and one of this set of
      *> files is referenced in an OPEN statement, the following rules
      *> apply: ... d. Each of the files must be a sequential file."
      *> The MULTIPLE FILE TAPE clause puts F1 and F2 on one reel
      *> (§1985:VII.2.11.3 GR1) and F2 is RELATIVE, so opening F1 (a
      *> sequential file) breaks the rule. This compiled clean at
      *> --std 85.
      *> Expected: COBOL6816 at --std 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5086R1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb5086r1a.dat".
           SELECT F2 ASSIGN TO "pb5086r1b.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS SEQUENTIAL.
       I-O-CONTROL.
           MULTIPLE FILE TAPE CONTAINS F1 F2.
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1 PIC X(4).
       FD  F2.
       01  R2 PIC X(4).
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F1.
           CLOSE F1.
           STOP RUN.
