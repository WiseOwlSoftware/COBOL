      *>--- conformance-case
      *> id: pb5802-assign-to-null-2002
      *> rules: [SR-8.4.3.10.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5802, PB6074 - the 2002-and-later LEG of ASSIGN TO NULL (the 85 leg is pb5802-assign-to-null-85). From COBOL-2002
      *> NULL is the reserved predefined address of class pointer (§2002:8.4.2.10), which "may be used only as a sending operand in
      *> an INITIALIZE or a SET statement" and in the other places that rule lists (§2002:8.4.2.10.2 SR1, cite.py: OK); the
      *> ASSIGN clause is none of them, and literal-1 there shall be an alphanumeric literal (§2023:12.4.5.2 SR4). The program is
      *> refused with COBOL2576 at 2002, 2014 and 2023. The leg fails if a compiler takes NULL for the file's implementor-name,
      *> as COBOL-85 does (§1985:VII.2.3.3 SR3), once the word is reserved.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5802B.
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
