*>--- conformance-case
*> id: pb1160-resume-in-global-declarative
*> rules: [SR-14.9.33.3-2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB1160 - §2023:14.9.33.3 SR2 (§2002:14.8.31.2 SR2, the same words): "The RESUME statement shall not be
*> specified in a declarative procedure for which the GLOBAL phrase is specified in the associated USE statement."
*>   OK  §2023:14.9.33.3 2)  (Syntax rules)    OK  §2002:14.8.31.2 2)  (Syntax rules)
*> The direct form: a RESUME written in the declarative procedure G whose USE statement carries GLOBAL is refused
*> with COBOL0713. Every statement here exists in 2002, so the refusal witnesses the rule at 2002 as at 2023; the
*> WHEN-phrase form (pb1160-resume-in-when-in-global-declarative) needs the 2023 exception-checking PERFORM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1160DIR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "no-such-file-pb1160d.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 FREC PIC X.
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       G SECTION.
           USE GLOBAL AFTER STANDARD ERROR PROCEDURE ON F.
       G1.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           OPEN INPUT F
           STOP RUN.
