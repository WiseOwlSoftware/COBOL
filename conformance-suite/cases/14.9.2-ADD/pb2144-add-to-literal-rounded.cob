      *>--- conformance-case
      *> id: pb2144-add-to-literal-rounded
      *> rules: [FMT-14.9.2.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2144 - the TO / FROM / BY / INTO operand of ADD, SUBTRACT, MULTIPLY and DIVIDE is
      *> one grammar rule (arithmeticPhraseOperand), so every shape a format does not print reaches
      *> the binder and draws the one named refusal, COBOL1689, at every verb.
      *> ADD Format 2's TO operand is a sending operand and takes no rounded-phrase; only the GIVING
      *> receivers do. Before the fix ROUNDED after a literal was a parse error (COBOL2269).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2144B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9(4) VALUE 5.
       01 B PIC 9(4) VALUE 6.
       01 C PIC 9(4).
       01 D PIC 9(4).
       01 E PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           ADD A TO 2 ROUNDED GIVING C
           STOP RUN.
