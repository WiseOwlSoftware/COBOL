*>--- conformance-case
*> id: pb3338_ec_flow_apply_commit_under_recursive_element
*> rules: [GR-12.4.6.3.4-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3338, review finding w1154a R1-2 - the COMPILED path of ISO 12.4.6.3.4 GR3: "If a runtime element that
*> contains an APPLY COMMIT clause is attempted to be activated under the control of a recursive runtime element, then
*> the EC-FLOW-APPLY exception condition is set to exist." Table 13 (14.6.13.1.6) names the condition
*> EC-FLOW-APPLY-COMMIT, Fatal; it is the ACTIVATING element's condition, decided by the CALL statement's checking
*> (docs/CONFORMANCE.md 5, A.4.3 determination (d)). The RECURSIVE PB3338RA is active throughout and CALLs PB3338RB,
*> whose clause names W:
*>   HANDLED=EC-FLOW-APPLY-COMMIT  the first CALL, checking on: the condition exists, the declarative runs, and RESUME
*>                                 AT NEXT STATEMENT continues after the CALL; the activation was only attempted, so
*>                                 PB3338RB does not run.
*>   B W=Z                         the second CALL, checking off at that CALL: no condition, and the clause stays
*>                                 inactive (determination (d)), so PB3338RB's ROLLBACK has no active clause and is a
*>                                 CONTINUE (14.9.36.4 GR1).
*>   END
      >>TURN EC-FLOW-APPLY-COMMIT CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3338RA RECURSIVE.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-SEC SECTION.
           USE AFTER EXCEPTION CONDITION EC-FLOW-APPLY-COMMIT.
       D-PARA.
           DISPLAY "HANDLED=" FUNCTION EXCEPTION-STATUS
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       M-PARA.
           CALL "PB3338RB"
      >>TURN EC-FLOW-APPLY-COMMIT CHECKING OFF
           CALL "PB3338RB"
           DISPLAY "END"
           STOP RUN.
       END PROGRAM PB3338RA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3338RB.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       I-O-CONTROL.
           APPLY COMMIT ON W.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X VALUE "I".
       PROCEDURE DIVISION.
           MOVE "Z" TO W
           ROLLBACK
           DISPLAY "B W=" W
           MOVE "I" TO W
           GOBACK.
       END PROGRAM PB3338RB.
