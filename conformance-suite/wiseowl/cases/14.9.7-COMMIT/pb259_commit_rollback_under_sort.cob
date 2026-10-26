*>--- conformance-case
*> id: pb259_commit_rollback_under_sort
*> rules: [GR-14.9.7.4-1, GR-14.9.36.4-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB259, PB3338 - COMMIT and ROLLBACK under an active SORT with NO active APPLY COMMIT clause.
*> ISO 14.9.7.4 GR1 and 14.9.36.4 GR1: "If this statement is executed when there is no active APPLY COMMIT clause,
*> then it has the same effect as a CONTINUE statement with no additional phrases."
*> GR1 is asked before GR2 (EC-FLOW-COMMIT / EC-FLOW-ROLLBACK under a SORT or MERGE): with no clause there is no
*> transaction for the statement to act on, so it continues. HELPER lies outside both procedure ranges (the
*> static reach, a COMMIT written inside the procedures, is 14.9.7.3 SR2's refusal, COBOL1690) and is
*> PERFORMed from them while the SORT is active. Under EC-ALL checking no condition is raised, so
*> EXCEPTION-STATUS stays blank and the SORT completes in key order. The same statements under an ACTIVE clause
*> raise the conditions: unit:TransactionControlTests.Commit_and_rollback_under_an_active_sort_raise_ec_flow_or_change_nothing.
       >>TURN EC-ALL CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB259SRT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO 'PB259SWK'.
       DATA DIVISION.
       FILE SECTION.
       SD SF.
       01 SR PIC X(4).
       WORKING-STORAGE SECTION.
       01 WS-ST PIC X(31).
       01 WS-EOF PIC X VALUE 'N'.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SORT SF ON ASCENDING KEY SR
               INPUT PROCEDURE IS FEED
               OUTPUT PROCEDURE IS DRAIN.
           DISPLAY 'SORTED'.
           STOP RUN.
       FEED.
           MOVE 'BBBB' TO SR.
           RELEASE SR.
           PERFORM HELPER.
           MOVE 'AAAA' TO SR.
           RELEASE SR.
       DRAIN.
           PERFORM HELPER.
           PERFORM UNTIL WS-EOF = 'Y'
               RETURN SF
                   AT END MOVE 'Y' TO WS-EOF
                   NOT AT END DISPLAY 'REC ' SR
               END-RETURN
           END-PERFORM.
       HELPER.
           COMMIT.
           MOVE FUNCTION EXCEPTION-STATUS TO WS-ST.
           DISPLAY 'COMMIT ST=[' WS-ST ']'.
           ROLLBACK.
           MOVE FUNCTION EXCEPTION-STATUS TO WS-ST.
           DISPLAY 'ROLLBACK ST=[' WS-ST ']'.
