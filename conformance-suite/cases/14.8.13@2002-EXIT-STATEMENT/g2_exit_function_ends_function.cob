      *>--- conformance-case
      *> id: g2_exit_function_ends_function
      *> rules: [2002-SR-14.8.13.2-9]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.13.2 SR9 - "An EXIT FUNCTION statement may be
      *>   specified only in a function procedure division." The granted
      *>   form: EXIT FUNCTION in the procedure division of user
      *>   function G2EXT04F, accepted and run. The refusal is
      *>   g2-exit-function-in-program (COBOL0827).
      *> §2002:14.8.13.3 GR8 - "The execution of an EXIT FUNCTION
      *>   statement causes the executing function to terminate, and
      *>   control to return to the activating statement. The value in
      *>   the data item referenced by the RETURNING phrase in the
      *>   containing function definition becomes the result of the
      *>   function activation."
      *> EXPECTED (derived): G2EXT04F moves GOOD to LR and exits before
      *>   the MOVE "BAD "; the activation's result is GOOD (R=GOOD) and
      *>   the activating DISPLAY completes, then AFTER-FUNCTION.
      *> FAILS IF: EXIT FUNCTION is refused in a function, or the
      *>   function does not end there (R=BAD ).
       IDENTIFICATION DIVISION.
       FUNCTION-ID. G2EXT04F.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LR PIC X(4).
       PROCEDURE DIVISION RETURNING LR.
           MOVE "GOOD" TO LR.
           EXIT FUNCTION.
           MOVE "BAD " TO LR.
       END FUNCTION G2EXT04F.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2EXT04.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION G2EXT04F.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "R=" FUNCTION G2EXT04F.
           DISPLAY "AFTER-FUNCTION".
           STOP RUN.
       END PROGRAM G2EXT04.
