      *>--- conformance-case
      *> id: pb5609-method-debugging-mode-without-name
      *> rules: [2002-SR-12.2.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5609. §2002:12.2.2 SR2: "The configuration section shall
      *> not be specified in a method definition". The method's
      *> configuration section writes the 2002 form the 2023 text lacks,
      *> SOURCE-COMPUTER without computer-name-1 and WITH DEBUGGING MODE
      *> (§2002:12.2.4.1), so the refusal must be this rule's, COBOL2644,
      *> and not a parse error of the paragraph (it was COBOL1971).
       IDENTIFICATION DIVISION.
       CLASS-ID. PB5609MC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. M1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. WITH DEBUGGING MODE.
       PROCEDURE DIVISION.
           DISPLAY "M1".
       END METHOD M1.
       END OBJECT.
       END CLASS PB5609MC.
