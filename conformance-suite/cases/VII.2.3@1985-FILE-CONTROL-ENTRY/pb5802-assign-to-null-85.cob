      *>--- conformance-case
      *> id: pb5802-assign-to-null-85
      *> rules: [1985-SR-VII.2.3.3-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5802 - §1985:VII.2.3.3 SR3: "The meaning and rules for
      *> the allowable content of implementor-name-1 and the value of
      *> literal-1 are defined by the implementor". NULL is no 1985
      *> reserved word (§1985:IV.4.2.2.1: reserved words and system-names
      *> form disjoint sets), so in ASSIGN TO it is implementor-name-1,
      *> as NULLS is; the program compiles and runs. At 2002 and later
      *> NULL is the reserved predefined address and ASSIGN TO NULL is
      *> refused (COBOL2576, §2023:8.4.3.10.3 SR1).
      *> The leg fails if ASSIGN TO NULL is refused at 85 (COBOL1639).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5802A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO NULL.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(10).
       PROCEDURE DIVISION.
       P1.
           DISPLAY "DONE".
           STOP RUN.
