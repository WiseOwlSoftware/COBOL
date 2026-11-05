*>--- conformance-case
*> id: call-by-reference-formal-passed-on-at-85
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4768 - a formal passed on BY REFERENCE through a deep recursion is the SAME storage at every level.
      *> Derived from ISO/IEC 1989:2023:
      *>   cite.py --check 14.2.3 "the activated runtime element operates as if the formal parameter occupies the
      *>     same storage area as the argument" -> OK 8): at every level L-A is the driver's X, however many levels
      *>     passed it on, so a store through it at the deepest level reaches X.
      *>   cite.py --check 14.9.4.4 "the EC-PROGRAM-RESOURCES exception condition is set to exist, the program
      *>     call is not successful, and execution continues" -> OK 3) c): the CALL at the deepest level fails, its
      *>     declarative runs, RESUME AT NEXT STATEMENT returns control, and the run CONTINUES - it neither ends
      *>     nor crashes.
      *> Each chain recurses (through the formal, never a copy of it) until a level's CALL cannot activate (the one
      *> resource checked is the stack an activation needs, docs/CONFORMANCE.md DOC-A.1-14), so the depth is the
      *> implementor's and no line below prints it. DERIVATION of every .out line, once per chain (DPX: PIC X(4),
      *> DPN: PIC 9(4), DPC: PIC 9(4) COMP):
      *>   DECL EC-PROGRAM-RESOURCES   the failing level's declarative (EXCEPTION-STATUS names the condition).
      *>   AT-FAIL L-A=<v>             control returned to the failing level after its CALL: L-A still holds the
      *>                               driver's value (ABCD / 1234), read through every level that passed it on.
      *>   BACK X=<w>                  the failing level stored the new value into L-A; every level returned, and the
      *>                               driver's own item holds it (GR8: the same storage area).
       >>TURN EC-PROGRAM CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CBRN-DRV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 XA PIC X(4) VALUE "ABCD".
       01 XN PIC 9(4) VALUE 1234.
       01 XC PIC 9(4) COMP VALUE 1234.
       PROCEDURE DIVISION.
       P.
           CALL "CBRN-DPX" USING XA
           DISPLAY "BACK X=" XA
           CALL "CBRN-DPN" USING XN
           DISPLAY "BACK X=" XN
           CALL "CBRN-DPC" USING XC
           DISPLAY "BACK X=" XC
           STOP RUN.
       END PROGRAM CBRN-DRV.

       >>TURN EC-PROGRAM CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CBRN-DPX RECURSIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 DEPTH PIC 9(9) VALUE 0.
       01 FAIL-DEPTH PIC 9(9) VALUE 0.
       LOCAL-STORAGE SECTION.
       01 MY-DEPTH PIC 9(9).
       LINKAGE SECTION.
       01 L-A PIC X(4).
       PROCEDURE DIVISION USING L-A.
       DECLARATIVES.
       H-DP SECTION.
           USE AFTER EXCEPTION CONDITION EC-PROGRAM-RESOURCES.
       H-DP-P.
           DISPLAY "DECL " FUNCTION EXCEPTION-STATUS
           MOVE DEPTH TO FAIL-DEPTH
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       P.
           ADD 1 TO DEPTH
           MOVE DEPTH TO MY-DEPTH
           CALL "CBRN-DPX" USING L-A
           IF MY-DEPTH = FAIL-DEPTH
               DISPLAY "AT-FAIL L-A=" L-A
               MOVE "WXYZ" TO L-A
           END-IF
           SUBTRACT 1 FROM DEPTH
           GOBACK.
       END PROGRAM CBRN-DPX.

       >>TURN EC-PROGRAM CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CBRN-DPN RECURSIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 DEPTH PIC 9(9) VALUE 0.
       01 FAIL-DEPTH PIC 9(9) VALUE 0.
       LOCAL-STORAGE SECTION.
       01 MY-DEPTH PIC 9(9).
       LINKAGE SECTION.
       01 L-A PIC 9(4).
       PROCEDURE DIVISION USING L-A.
       DECLARATIVES.
       H-DP SECTION.
           USE AFTER EXCEPTION CONDITION EC-PROGRAM-RESOURCES.
       H-DP-P.
           DISPLAY "DECL " FUNCTION EXCEPTION-STATUS
           MOVE DEPTH TO FAIL-DEPTH
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       P.
           ADD 1 TO DEPTH
           MOVE DEPTH TO MY-DEPTH
           CALL "CBRN-DPN" USING L-A
           IF MY-DEPTH = FAIL-DEPTH
               DISPLAY "AT-FAIL L-A=" L-A
               MOVE 4321 TO L-A
           END-IF
           SUBTRACT 1 FROM DEPTH
           GOBACK.
       END PROGRAM CBRN-DPN.

       >>TURN EC-PROGRAM CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CBRN-DPC RECURSIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 DEPTH PIC 9(9) VALUE 0.
       01 FAIL-DEPTH PIC 9(9) VALUE 0.
       LOCAL-STORAGE SECTION.
       01 MY-DEPTH PIC 9(9).
       LINKAGE SECTION.
       01 L-A PIC 9(4) COMP.
       PROCEDURE DIVISION USING L-A.
       DECLARATIVES.
       H-DP SECTION.
           USE AFTER EXCEPTION CONDITION EC-PROGRAM-RESOURCES.
       H-DP-P.
           DISPLAY "DECL " FUNCTION EXCEPTION-STATUS
           MOVE DEPTH TO FAIL-DEPTH
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       P.
           ADD 1 TO DEPTH
           MOVE DEPTH TO MY-DEPTH
           CALL "CBRN-DPC" USING L-A
           IF MY-DEPTH = FAIL-DEPTH
               DISPLAY "AT-FAIL L-A=" L-A
               MOVE 4321 TO L-A
           END-IF
           SUBTRACT 1 FROM DEPTH
           GOBACK.
       END PROGRAM CBRN-DPC.
