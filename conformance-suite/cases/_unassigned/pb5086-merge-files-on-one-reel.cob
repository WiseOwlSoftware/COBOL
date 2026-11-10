      *>--- conformance-case
      *> id: pb5086-merge-files-on-one-reel
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5086 - §1985:XI.4.1.3 SR6: "No two files specified in
      *> any one MERGE statement may reside on the same multiple file
      *> reel." The MULTIPLE FILE TAPE clause states that IN1 and IN2
      *> share a reel (§1985:VII.2.11.3 GR1), and both are USING files
      *> of the MERGE. The clause was parsed and dropped, so this
      *> compiled clean at --std 85. (SORT has the opposite rule,
      *> §1985:XI.4.4.3 SR7: its USING files may share a reel.)
      *> Expected: COBOL6815 at --std 85 (from 2002 the clause is also
      *> COBOL0902, removed).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5086M1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IN1 ASSIGN TO "pb5086m1a.dat".
           SELECT IN2 ASSIGN TO "pb5086m1b.dat".
           SELECT OUT1 ASSIGN TO "pb5086m1c.dat".
           SELECT SD1 ASSIGN TO "pb5086m1d.dat".
       I-O-CONTROL.
           MULTIPLE FILE TAPE CONTAINS IN1 IN2.
       DATA DIVISION.
       FILE SECTION.
       FD  IN1.
       01  R1 PIC X(10).
       FD  IN2.
       01  R2 PIC X(10).
       FD  OUT1.
       01  R3 PIC X(10).
       SD  SD1.
       01  SR PIC X(10).
       PROCEDURE DIVISION.
       P1.
           MERGE SD1 ON ASCENDING KEY SR USING IN1 IN2 GIVING OUT1.
           STOP RUN.
