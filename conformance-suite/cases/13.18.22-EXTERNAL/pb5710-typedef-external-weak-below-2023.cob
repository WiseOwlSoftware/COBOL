*>--- conformance-case
*> id: pb5710-typedef-external-weak-below-2023
*> rules: [SR-13.18.22.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5710 - a WEAK EXTERNAL type declaration (TYPEDEF without STRONG) is a COBOL-2023 construct: §2002:13.14.2 SR5 and
      *> §2014:13.16.2 SR5 forbid the EXTERNAL clause in the same data description entry as the TYPEDEF clause, and 2023's
      *> §2023:13.18.22.3 SR1 admits "level 1 type declarations", so below 2023 the edition gate refuses it (COBOL0900).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5710W.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T IS TYPEDEF IS EXTERNAL PIC X(4).
       01 V TYPE T.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "ABCD" TO V
           DISPLAY V
           STOP RUN.
