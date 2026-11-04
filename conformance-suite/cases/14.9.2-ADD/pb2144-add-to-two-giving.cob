      *>--- conformance-case
      *> id: pb2144-add-to-two-giving
      *> rules: [FMT-14.9.2.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2144 - the TO / FROM / BY / INTO operand of ADD, SUBTRACT, MULTIPLY and DIVIDE is
      *> one grammar rule (arithmeticPhraseOperand), so every shape a format does not print reaches
      *> the binder and draws the one named refusal, COBOL1689, at every verb.
      *> ADD Format 2 prints ONE `TO {identifier-2 | literal-2}`; two TO operands are a format violation.
      *> Before the fix this was a parse error (COBOL2269) where MULTIPLY's twin drew COBOL1689.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2144A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9(4) VALUE 5.
       01 B PIC 9(4) VALUE 6.
       01 C PIC 9(4).
       01 D PIC 9(4).
       01 E PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           ADD A TO 2 3 GIVING C
           STOP RUN.
