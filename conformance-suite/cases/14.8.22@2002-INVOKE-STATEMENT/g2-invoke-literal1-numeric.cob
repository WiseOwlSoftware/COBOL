      *>--- conformance-case
      *> id: g2-invoke-literal1-numeric
      *> rules: [2002-SR-14.8.22.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.22.2 SR2 - "Literal-1 shall be of class alphanumeric
      *> or national." The numeric literal 123 as the method name is of
      *> class numeric, so the INVOKE at line 21 is refused at 2002
      *> (COBOL0823). The accepted alphanumeric and national forms are
      *> g2-invoke-cd-receiving-operands (A1, A5). Why it can fail: the
      *> compiler accepts the numeric literal or reports another code.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2INV02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION.
       MAIN.
           IF O NOT = NULL
               INVOKE O 123
           END-IF.
           DISPLAY "COMPILED".
           STOP RUN.
       END PROGRAM G2INV02.
