*>--- conformance-case
*> id: pb2296_method_options_arithmetic_reaches_body
*> rules: [GR-11.9.4-1, GR-11.9.5.2-3, RV-15.84.4-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2296 - a METHOD's own OPTIONS paragraph governs the arithmetic of its whole body, not only its
      *> LOCAL-STORAGE initial state (ISO 11.9.4 GR1: the clauses of an OPTIONS paragraph "apply to the source element in
      *> which they are specified"; a method definition is a source element of its own, 3.164). The method M1 alone writes
      *> ARITHMETIC IS STANDARD-DECIMAL, and the class and the program do not. Under it ISO 15.84.4 2) fixes
      *> FUNCTION SQRT(2) exactly: "the exact square root of argument-1 rounded to 34 digits according to the rules for
      *> standard-decimal arithmetic" = 1.414213562373095048801688724209698; the store into the scale-30 receiver
      *> truncates (no ROUNDED is written) -> 1.414213562373095048801688724209. The method's emitted arithmetic used the
      *> unit's mode (the binary64 root, 1.414213562373095145474621858738), because the emitter's scoped OPTIONS override
      *> covered the LOCAL-STORAGE initializers only.
       IDENTIFICATION DIVISION.
       CLASS-ID. CPB2296 INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS CPB2296.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. M1.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 R PIC 9V9(30).
       PROCEDURE DIVISION.
       P.
           COMPUTE R = FUNCTION SQRT(2)
           IF R = 1.414213562373095048801688724209
               DISPLAY "M1 SQRT2 OK"
           ELSE
               DISPLAY "M1 SQRT2 BAD " R
           END-IF
           GOBACK.
       END METHOD M1.
       END OBJECT.
       END CLASS CPB2296.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2296M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CPB2296.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O OBJECT REFERENCE CPB2296.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE CPB2296 "NEW" RETURNING O
           INVOKE O "M1"
           STOP RUN.
       END PROGRAM PB2296M.
