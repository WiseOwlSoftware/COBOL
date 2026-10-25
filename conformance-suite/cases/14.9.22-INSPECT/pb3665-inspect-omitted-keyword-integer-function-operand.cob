*>--- conformance-case
*> id: pb3665-inspect-omitted-keyword-integer-function-operand
*> rules: [SR-14.9.22.3-2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3665 (review round 1, R1-2) - 8.4.3.2.3 SR2 makes the keyword-omitted INTEGER(N) (REPOSITORY
*> FUNCTION ALL INTRINSIC) and FUNCTION INTEGER(N) ONE function-identifier, so ISO 14.9.22.3 SR2 refuses it at
*> an identifier-3 ... identifier-n position exactly as it refuses the written form
*> (pb3665-inspect-integer-function-operand): 15.2 5) gives an integer function no usage. The operand used to
*> resolve as a data name and be refused as undefined (COBOLNET1639) whatever the function.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3665OI.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY. FUNCTION ALL INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(4) VALUE 7.
       01 S PIC X(8) VALUE "ab7cd7ef".
       01 T PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           INSPECT S TALLYING T FOR ALL INTEGER(N)
           STOP RUN.
