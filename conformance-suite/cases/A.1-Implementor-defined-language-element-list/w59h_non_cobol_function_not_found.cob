*>--- conformance-case
*> id: w59h_non_cobol_function_not_found
*> rules: [DOC-A.1-88]
*> accept: [2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1536 Q1 / R43 item 3, kb/Work PB5703 - Annex A.1 item 88 is
      *> CONDITIONALLY REQUIRED on support for non-COBOL functions, and
      *> WiseOwl COBOL provides none: docs/CONFORMANCE.md section 7 records it
      *> as "Condition absent.". THIS PROGRAM IS THE WITNESS: a function
      *> prototype whose externalized name spells a real NON-COBOL module
      *> deployed beside every WiseOwl COBOL program - the runtime assembly
      *> WiseOwl.Cobol.Runtime.dll - is never located.
      *>   cite.py --check 8.4.3.2.4 "If the function is not found, the
      *>     EC-FUNCTION-NOT-FOUND exception condition is set to exist"
      *>     -> OK 8.4.3.2.4 6)
      *> DERIVATION of every .out line:
      *>   FN-NOT-FOUND    the function is not found, EC-FUNCTION-NOT-FOUND
      *>                   (enabled by the TURN) runs its declarative, which
      *>                   resumes at the statement after the MOVE.
      *>   Y=7             so the MOVE never stores: Y keeps VALUE 7.
      *> It runs from 2014: EC-FUNCTION-NOT-FOUND is on the 2014 and 2023
      *> exception-name lists and not on 2002's (§2002:8.4.2.2.3 names
      *> EC-PROGRAM-NOT-FOUND for a function that is not found).
       IDENTIFICATION DIVISION.
       FUNCTION-ID. W59HFN AS "WiseOwl.Cobol.Runtime" IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R PIC 9.
       PROCEDURE DIVISION RETURNING R.
       END FUNCTION W59HFN.
       >>TURN EC-FUNCTION-NOT-FOUND CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W59HFNF.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION W59HFN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Y  PIC 9 VALUE 7.
       PROCEDURE DIVISION.
       DECLARATIVES.
       HF SECTION.
           USE AFTER EXCEPTION CONDITION EC-FUNCTION-NOT-FOUND.
       HF-P.
           DISPLAY "FN-NOT-FOUND".
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       M-P.
           MOVE FUNCTION W59HFN TO Y
           DISPLAY "Y=" Y
           STOP RUN.
       END PROGRAM W59HFNF.
