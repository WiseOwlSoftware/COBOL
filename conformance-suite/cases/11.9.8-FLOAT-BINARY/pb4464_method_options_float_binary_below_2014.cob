*>--- conformance-case
*> id: pb4464_method_options_float_binary_below_2014
*> rules: [SR-11.9.8.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> PB4464 - the FLOAT-BINARY clause is a COBOL-2014 clause of the OPTIONS paragraph (ISO 11.9.8), and a METHOD's
      *>   own OPTIONS paragraph (10.6.1's method skeleton) is no way around that gate: the same program as the accepted
      *>   pb4464_method_options_float_binary leg, one method, is refused at 2002 with the clause's introduction arm.
       IDENTIFICATION DIVISION.
       CLASS-ID. NEGMF INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M1.
       OPTIONS.
           FLOAT-BINARY DEFAULT IS HIGH-ORDER-RIGHT.
       PROCEDURE DIVISION.
           DISPLAY "M1".
       END METHOD M1.
       END OBJECT.
       END CLASS NEGMF.
