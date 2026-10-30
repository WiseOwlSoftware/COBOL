*>--- conformance-case
*> id: pb4124_held_function_argument_below_2002
*> rules: [GR-8.4.3.2.4-2, RV-15.4.1-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4124 - the negative of pb4124_held_function_argument_keeps_description. The user-defined function whose
      *> activation holds the argument to its left is a COBOL-2002 introduction (the FUNCTION-ID paragraph, ISO 11.5), so
      *> the program is refused by name at 85 - COBOL0900, the introduction gate.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB4124N.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(8).
       01 L-R PIC 9(8).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       P.
           MOVE L-X TO L-R.
           GOBACK.
       END FUNCTION PB4124N.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4124B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC 9(4) VALUE 7.
       01 WS-B PIC 9(4) VALUE 3.
       01 WS-LO PIC 9(8) VALUE 0.
       01 WS-N PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION LENGTH (FUNCTION MAX (WS-A * WS-B, FUNCTION PB4124N (WS-LO))) TO WS-N
           STOP RUN.
       END PROGRAM PB4124B.
