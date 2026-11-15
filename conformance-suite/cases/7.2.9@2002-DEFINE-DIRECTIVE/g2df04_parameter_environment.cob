      *>--- conformance-case
      *> id: g2df04_parameter_environment
      *> rules: [2002-GR-7.2.9.3-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.9.3 GR4 - "If the PARAMETER phrase is specified, the
      *>   value referenced by compilation-variable-name-1 is obtained
      *>   from the operating environment by an implementor-defined
      *>   method when the DEFINE directive is processed. If no value is
      *>   made available from the operating environment,
      *>   compilation-variable-name-1 is not defined."
      *> The implementor-defined method (docs/conformance/7/DOC-A.1-49.md,
      *>   at every edition): the environment variable of the compiler
      *>   process named by the compilation-variable-name in upper case.
      *> EXPECTED OUTPUT, DERIVED:
      *>   PATH-DEFINED            PATH is set in every compiler process
      *>                           (the program search path of the host),
      *>                           so a value is made available and PATH
      *>                           is defined (first sentence;
      *>                           §2002:7.2.7.3.3 GR1).
      *>   UNSET-NOT-DEFINED       no variable G2DF04-NO-SUCH-VAR exists,
      *>                           so the name "is not defined" (second
      *>                           sentence; §2002:7.2.7.3.3 GR2).
      *>   OVERRIDE-NOW-UNDEFINED  G2DF04-WAS-SET AS 1, then AS PARAMETER
      *>                           OVERRIDE with no variable: the value is
      *>                           obtained "when the DEFINE directive is
      *>                           processed", none is available, so the
      *>                           name is not defined from then on.
      *> How each leg can fail: an environment not read prints
      *> PATH-BAD-UNDEFINED; a value invented for an absent variable
      *> prints UNSET-BAD-DEFINED; keeping the earlier definition prints
      *> OVERRIDE-BAD-STILL-DEFINED. Measured: with the variable
      *> G2DF04-NO-SUCH-VAR=3 set, the second line is UNSET-BAD-DEFINED.
      *> The case assumes PATH is in the environment of the compiler and
      *> G2DF04-NO-SUCH-VAR and G2DF04-WAS-SET are not.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DF04.
       PROCEDURE DIVISION.
       MAIN-PARA.
       >>DEFINE PATH AS PARAMETER
       >>IF PATH IS DEFINED
           DISPLAY "PATH-DEFINED"
       >>ELSE
           DISPLAY "PATH-BAD-UNDEFINED"
       >>END-IF
       >>DEFINE G2DF04-NO-SUCH-VAR AS PARAMETER
       >>IF G2DF04-NO-SUCH-VAR IS NOT DEFINED
           DISPLAY "UNSET-NOT-DEFINED"
       >>ELSE
           DISPLAY "UNSET-BAD-DEFINED"
       >>END-IF
       >>DEFINE G2DF04-WAS-SET AS 1
       >>DEFINE G2DF04-WAS-SET AS PARAMETER OVERRIDE
       >>IF G2DF04-WAS-SET IS NOT DEFINED
           DISPLAY "OVERRIDE-NOW-UNDEFINED"
       >>ELSE
           DISPLAY "OVERRIDE-BAD-STILL-DEFINED"
       >>END-IF
           STOP RUN.
