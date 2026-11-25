*>--- conformance-case
*> id: pb6175-goback-global-declarative-cites-2014
*> rules: [SR-14.9.18.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB6175 - COBOL 2014 states the GOBACK rule as its own 14.9.17.2 SR1: "The GOBACK statement shall not be
      *> specified in a declarative procedure for which the GLOBAL phrase is specified in the associated USE statement."
      *> The program breaks only that rule (the USE statement is legal at every edition), so at --std 2014 the refusal
      *> (COBOL2102) cites §2014:14.9.17.2 SR1, the rule of the edition compiled at, not §2023:14.9.18.3 SR1.
      *> pb409-goback-in-global-declarative is the same refusal at 2002, 2014 and 2023.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6175G.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb6175g.dat" ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(10).
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-SEC SECTION.
           USE GLOBAL AFTER STANDARD ERROR PROCEDURE ON F.
       D-PARA.
           GOBACK.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-PARA.
           STOP RUN.
