*>--- conformance-case
*> id: pb1136_invoke_null_receiver
*> rules: [SR-14.9.23.3-1, SR-8.4.3.7.3-2, GR-14.9.23.4-5]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 §14.9.23 — the predefined NULL as INVOKE's identifier-1 (kb/Work PB1136).
      *>   SR1: "Identifier-1 shall be an object reference."
      *>   §8.4.3.7.3 SR2: "NULL is implicitly described as class object and category object reference, and is not a
      *>   universal object reference" — so every INVOKE below is legal source: SR4's a)-i) name descriptions NULL
      *>   does not have, SR6 (no BY CONTENT / BY VALUE) is the universal receiver's, and no formal parameter exists
      *>   for SR5 or SR18 to compare an argument with. It used to be a compile-time warning COBOL1756 and a
      *>   run-time NotImplemented abort.
      *>   §14.9.23.4 GR5: "If identifier-1 is null, the EC-OO-NULL exception condition is set to exist and execution
      *>   of the INVOKE statement is terminated." EC-OO-NULL is fatal (Table 13) and its checking is enabled by the
      *>   >>TURN directive (§7.3.25), so the USE declarative runs and RESUME AT NEXT STATEMENT (§14.9.33) continues
      *>   after each terminated INVOKE.
      *> Every expected line is DERIVED from the standard, not captured:
      *>   1 HANDLED=EC-OO-NULL  INVOKE NULL "PING": PING is never invoked (no PINGED), EC-OO-NULL is raised; the
      *>                         EXCEPTION-STATUS value is the exception-name, left-justified in its 31 positions.
      *>   2 AFTER1              RESUME AT NEXT STATEMENT.
      *>   3 HANDLED=EC-OO-NULL  INVOKE NULL with BY REFERENCE, keyword-less, BY CONTENT, BY VALUE and OMITTED
      *>                         arguments and a RETURNING item — the same EC-OO-NULL.
      *>   4 W=KEEP X=ABC        the statement was terminated, so GR8 never placed a result in W and no argument was
      *>                         passed: both keep their values.
      *>   5 EVALUATED           GR7 a): "Arithmetic-expression-1, boolean-expression-1, identifier-1, identifier-2,
      *>                         identifier-3, and identifier-5 are evaluated ... at the beginning of the execution
      *>                         of the INVOKE statement" - the inline invocation O :: "GETV" (identifier-3) runs
      *>                         before the null receiver's exception terminates the statement.
      *>   6 HANDLED=EC-OO-NULL  then GR5 raises EC-OO-NULL and no method is invoked.
      *>   7 AFTER2              RESUME AT NEXT STATEMENT.
      >>TURN EC-OO-NULL CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1136NR.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1136NC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(3) VALUE "ABC".
       01 N PIC 9(3) VALUE 5.
       01 W PIC X(4) VALUE "KEEP".
       01 O USAGE OBJECT REFERENCE PB1136NC.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-OO-NULL.
       H-P.
           DISPLAY "HANDLED=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           INVOKE NULL "PING".
           DISPLAY "AFTER1".
           INVOKE NULL "GETN" USING BY REFERENCE X X BY CONTENT N + 1 "LIT"
               BY VALUE N 7 BY REFERENCE OMITTED RETURNING W.
           DISPLAY "W=" W " X=" X.
           INVOKE PB1136NC "NEW" RETURNING O.
           INVOKE NULL "PING" USING BY CONTENT O :: "GETV".
           DISPLAY "AFTER2".
           STOP RUN.
       END PROGRAM PB1136NR.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1136NC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "PINGED".
       END METHOD PING.
       METHOD-ID. GETV.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R PIC 9(3).
       PROCEDURE DIVISION RETURNING R.
       MAIN-P.
           DISPLAY "EVALUATED".
           MOVE 7 TO R.
       END METHOD GETV.
       END OBJECT.
       END CLASS PB1136NC.
