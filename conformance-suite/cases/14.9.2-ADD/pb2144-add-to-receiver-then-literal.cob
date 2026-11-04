      *>--- conformance-case
      *> id: pb2144-add-to-receiver-then-literal
      *> rules: [FMT-14.9.2.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2144 - the TO / FROM / BY / INTO operand of ADD, SUBTRACT, MULTIPLY and DIVIDE is
      *> one grammar rule (arithmeticPhraseOperand), so every shape a format does not print reaches
      *> the binder and draws the one named refusal, COBOL1689, at every verb.
      *> ADD Format 1 prints `TO {identifier-2 [rounded-phrase]} ...`: receivers only, so the literal 3
      *> is refused by name. Before the fix it was a parse error (COBOL2269); MULTIPLY's twin was named.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2144C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9(4) VALUE 5.
       01 B PIC 9(4) VALUE 6.
       01 C PIC 9(4).
       01 D PIC 9(4).
       01 E PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           ADD A TO B 3
           STOP RUN.
