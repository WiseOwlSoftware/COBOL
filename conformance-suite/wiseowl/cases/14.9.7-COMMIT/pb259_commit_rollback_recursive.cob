*>--- conformance-case
*> id: pb259_commit_rollback_recursive
*> rules: [GR-14.9.7.4-1, GR-14.9.36.4-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB259, PB3338 - COMMIT and ROLLBACK under an active RECURSIVE runtime element with NO active APPLY
*> COMMIT clause.
*> ISO 14.9.7.4 GR1 and 14.9.36.4 GR1: "If this statement is executed when there is no active APPLY COMMIT clause,
*> then it has the same effect as a CONTINUE statement with no additional phrases."
*> GR1 is asked before GR2 (EC-FLOW-COMMIT / EC-FLOW-ROLLBACK under a recursive runtime element): with no clause
*> there is no transaction for the statement to act on, so it continues. PB259RSB is a non-recursive program called
*> while the RECURSIVE PB259REC is active (a COMMIT written IN a recursive source element is 14.9.7.3 SR1's
*> refusal, COBOL1690); under EC-ALL checking no condition is raised, so both EXCEPTION-STATUS reads are blank
*> and control returns normally. The same statements under an ACTIVE clause raise the conditions:
*> cases/14.9.7-COMMIT/pb3338_commit_rollback_under_recursive_element.
       >>TURN EC-ALL CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB259REC RECURSIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-ST PIC X(31).
       PROCEDURE DIVISION.
           CALL 'PB259RSB'.
           MOVE FUNCTION EXCEPTION-STATUS TO WS-ST.
           DISPLAY 'BACK ST=[' WS-ST ']'.
           STOP RUN.
       END PROGRAM PB259REC.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB259RSB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-ST2 PIC X(31).
       PROCEDURE DIVISION.
           COMMIT.
           MOVE FUNCTION EXCEPTION-STATUS TO WS-ST2.
           DISPLAY 'COMMIT ST=[' WS-ST2 ']'.
           ROLLBACK.
           MOVE FUNCTION EXCEPTION-STATUS TO WS-ST2.
           DISPLAY 'ROLLBACK ST=[' WS-ST2 ']'.
           GOBACK.
       END PROGRAM PB259RSB.
