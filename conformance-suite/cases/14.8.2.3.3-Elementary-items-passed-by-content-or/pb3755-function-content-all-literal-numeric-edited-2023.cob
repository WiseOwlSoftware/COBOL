*>--- conformance-case
*> id: pb3755-function-content-all-literal-numeric-edited-2023
*> rules: [GR-14.8.2.3.3-L2.2, SR-14.9.25.3-5]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3755, the user-defined-function lane of pb3755-call-content-high-value-numeric-edited-2023:
      *> 14.2.3 GR2 sends a function's formal parameters to 14.8.2, and 14.8.2.3.3 rule 2 lists "a function" among the
      *> activations whose BY CONTENT formal takes the MOVE rules, so a non-digit ALL literal into the PIC ZZ9 formal of a
      *> function conforms as MOVE ALL "X" TO <a numeric-edited item> does - valid through 2014, removed at 2023
      *> (14.9.25.3 SR5; Annex E.2 item 1 bullet 1) - and is COBOLNET0902 at 2023.
      *>   cite.py --check 14.2.3 "The conformance requirements for formal parameters and returning items are specified
      *>     in 14.8.2, Parameters and 14.8.3, Returning items." -> OK 14.2.3 2)
       IDENTIFICATION DIVISION.
       FUNCTION-ID. F3755F.
       DATA DIVISION.
       LINKAGE SECTION.
       01 E PIC ZZ9.
       01 R PIC 9.
       PROCEDURE DIVISION USING E RETURNING R.
           MOVE 1 TO R
           GOBACK.
       END FUNCTION F3755F.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3755F.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION F3755F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-R PIC 9.
       PROCEDURE DIVISION.
           COMPUTE WS-R = FUNCTION F3755F(ALL "X")
           DISPLAY WS-R
           STOP RUN.
       END PROGRAM P3755F.
