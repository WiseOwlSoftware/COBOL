      *>--- conformance-case
      *> id: pb2144-multiply-by-function-rounded
      *> rules: [FMT-14.9.26.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2144 - the TO / FROM / BY / INTO operand of ADD, SUBTRACT, MULTIPLY and DIVIDE is
      *> one grammar rule (arithmeticPhraseOperand), so every shape a format does not print reaches
      *> the binder and draws the one named refusal, COBOL1689, at every verb.
      *> MULTIPLY Format 2's BY operand is a sending operand and takes no rounded-phrase. Before the fix
      *> ROUNDED after a literal was named (COBOL1689) and after a function-identifier a parse error.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2144K.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9(4) VALUE 5.
       01 B PIC 9(4) VALUE 6.
       01 C PIC 9(4).
       01 D PIC 9(4).
       01 E PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           MULTIPLY A BY FUNCTION ABS(B) ROUNDED GIVING C
           STOP RUN.
