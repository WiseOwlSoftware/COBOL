      *>--- conformance-case
      *> id: pb5683_assign_symbolic_character_word_85
      *> rules: [1985-SR-VII.2.3.3-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5683, the rulings of read pass 17, mechanism 5
      *> (kb/Work/PB5683/2026-11-16-rulings-code-reads-batch-17.md).
      *> The 1985 ASSIGN clause offers implementor-name-1 and literal-1
      *> at one place and leaves their allowable content to the
      *> implementor (§1985:VII.2.3.3 SR3); it does not say whether a
      *> word the program also declares as a symbolic-character is the
      *> implementor-name or the figurative constant. The register
      *> sentence of docs/conformance/7/DOC-A.1-71.md decides it: in the
      *> ASSIGN clause such a word is the implementor-name, the file's
      *> external name, as GnuCOBOL 3.2 reads it. So ASSIGN TO SYMA
      *> compiles, the file is opened, written and closed under that
      *> name, and the program prints RAN; a compiler that took SYMA as
      *> the figurative constant would refuse the clause (a figurative
      *> constant is no literal-1 there) and print nothing.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5683SYM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYMA IS 66.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQF ASSIGN TO SYMA
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD SQF.
       01 SQ-REC PIC X(5).
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT SQF.
           MOVE "HELLO" TO SQ-REC.
           WRITE SQ-REC.
           CLOSE SQF.
           DISPLAY "RAN".
           STOP RUN.
