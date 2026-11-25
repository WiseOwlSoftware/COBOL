      *>--- conformance-case
      *> id: pb6074_assign_to_null_first_of_two_operands
      *> rules: [SR-12.4.5.2-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6074 item 10 (PB5802): the two-operand ASSIGN TO message names what the first operand IS. NULL is
      *>   parsed as the predefined NULL at every edition; below COBOL-2002 it is a free word, implementor-name-1
      *>   (§1985:VII.2.3.3 SR3, cite.py: OK; kb/Work PB5802), and from 2002 the reserved predefined address, never a
      *>   literal. The list "NULL "p6074.dat"" is refused at every edition because the implementor allows only a
      *>   device class before the file's name (§2023:12.4.5.2 SR5, which leaves device-name-1 and literal-1 to the
      *>   implementor; docs/conformance/7/DOC-A.1-71.md), and the refusal says "here 'NULL' is not a device class",
      *>   which is what the diagnostics file expects. The text used to call the first operand "a literal".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6074N.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO NULL "p6074.dat".
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(10).
       PROCEDURE DIVISION.
       P1.
           STOP RUN.
