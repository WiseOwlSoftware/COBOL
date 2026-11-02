*>--- conformance-case
*> id: pb4107-invoke-byref-datapointer-two-types
*> rules: [GR-14.8.2.3.2-4]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4107 - the data-pointer twin: PA is of type TA and the method's formal LP of type TB, both USAGE
      *> POINTER TO G. 9.3.6 3 e) matches (one USAGE clause) and binds M; its 14.8.2.3.2 conformance, which a typed
      *> INVOKE asks at compile time (14.9.23.3 SR5 c)), refuses the pair (COBOL0828).
      *> 14.8.2.3.2: "If either is a restricted pointer, both shall be restricted and of the same type."
      *>   cite.py: OK  14.8.2.3.2 4)
      *> 8.5.3.1: "Two typed items are of the same type when:" "The items are described with TYPE clauses that reference
      *>   equivalent type declarations"   cite.py: OK  8.5.3.1
      *> 14.9.23.3 SR5 c): "The rules for conformance as specified in 14.8.2, Parameters and 14.8.3, Returning items
      *>   apply."   cite.py: OK  14.9.23.3 5) c)
      *> A SET PB TO PA is legal: 14.9.39.3 SR19 asks only "a data-pointer restricted to the same type" (G), which a BY
      *> CONTENT pass asks too (14.8.2.3.3).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. T4107D.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS C4107D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G TYPEDEF PIC X(4).
       01 TA TYPEDEF USAGE POINTER TO G.
       01 PA TYPE TA.
       01 OC USAGE OBJECT REFERENCE C4107D.
       PROCEDURE DIVISION.
           INVOKE C4107D "NEW" RETURNING OC
           INVOKE OC "M" USING BY REFERENCE PA
           GOBACK.
       END PROGRAM T4107D.

       IDENTIFICATION DIVISION.
       CLASS-ID. C4107D INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G TYPEDEF PIC X(4).
       01 TB TYPEDEF USAGE POINTER TO G.
       PROCEDURE DIVISION.
       METHOD-ID. M.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP TYPE TB.
       PROCEDURE DIVISION USING BY REFERENCE LP.
           DISPLAY "in M".
       END METHOD M.
       END OBJECT.
       END CLASS C4107D.
