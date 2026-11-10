      *>--- conformance-case
      *> id: pb5086-open-extend-multiple-file-reel
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5086 - §1985:VII.4.3.3 SR2: "The EXTEND phrase must
      *> not be specified for a multiple file reel." The MULTIPLE FILE
      *> TAPE clause states that F1 and F2 share a reel
      *> (§1985:VII.2.11.3 GR1), so OPEN EXTEND F1 violates the rule.
      *> The clause was parsed and dropped, so this compiled clean at
      *> --std 85. The OPEN of F3 (named in no clause) is legal.
      *> Expected: COBOL6814 at --std 85 (from 2002 the clause is also
      *> COBOL0902, removed).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5086E1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb5086e1a.dat".
           SELECT F2 ASSIGN TO "pb5086e1b.dat".
           SELECT F3 ASSIGN TO "pb5086e1c.dat".
       I-O-CONTROL.
           MULTIPLE FILE TAPE CONTAINS F1 F2.
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1 PIC X(4).
       FD  F2.
       01  R2 PIC X(4).
       FD  F3.
       01  R3 PIC X(4).
       PROCEDURE DIVISION.
       P1.
           OPEN EXTEND F3.
           CLOSE F3.
           OPEN INPUT F2 EXTEND F1.
           CLOSE F1 F2.
           STOP RUN.
