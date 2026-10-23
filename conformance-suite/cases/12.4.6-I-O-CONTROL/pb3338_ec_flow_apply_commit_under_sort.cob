*>--- conformance-case
*> id: pb3338_ec_flow_apply_commit_under_sort
*> rules: [GR-12.4.6.3.4-4, GR-12.4.6.3.4-5]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3338, review finding w1154a R1-2 - the COMPILED path of ISO 12.4.6.3.4 GR4: "If a runtime element that
*> contains an APPLY COMMIT clause is attempted to be invoked under the control of a file sort or a merge, then the
*> EC-FLOW-APPLY exception condition is set to exist." Table 13 (14.6.13.1.6) names the condition EC-FLOW-APPLY-COMMIT:
*> "Invocation of a runtime element with an APPLY COMMIT clause in effect by a runtime element for which commit and
*> rollback is not permitted", Fatal. It is the ACTIVATING element's condition, so the CALL statement's checking decides
*> it (docs/CONFORMANCE.md 5, A.4.3 determination (d)); the run unit latches it at the CALL, before the activated
*> element's own checking scope opens. PB3338SA's SORT input procedure CALLs PB3338SB, whose clause names W:
*>   B W=Z                         the first CALL, checking off at that CALL: no condition, and the clause stays
*>                                 inactive (determination (d)), so PB3338SB's ROLLBACK has no active clause and is a
*>                                 CONTINUE (14.9.36.4 GR1): W keeps the Z PB3338SB moved into it.
*>   B W=Z                         the second, from PB3338SC (activated by a CALL with checking on), whose own CALL
*>                                 statement has checking off: the latch is the activating CALL's, not PB3338SA's, so
*>                                 again no condition and an inactive clause.
*>   HANDLED=EC-FLOW-APPLY-COMMIT  the third CALL, checking on: the condition exists and the declarative runs; the
*>                                 activation was only attempted, so PB3338SB does not run. The declarative ends in
*>                                 RESUME, so it does not complete normally (14.6.13.1.2), and 14.9.40.4 GR17 "If a USE
*>                                 procedure invoked while a format 1 SORT statement is active does not complete
*>                                 normally, the SORT statement is terminated": control leaves the SORT.
*>   AFTER SORT
*>   B W=I                         the fourth CALL, after the SORT: the clause is activated (GR5), saving W = I, so the
*>                                 ROLLBACK restores it (14.9.36.4 GR3): the positive control.
      >>TURN EC-FLOW-APPLY-COMMIT CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3338SA.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "PB3338SW".
       DATA DIVISION.
       FILE SECTION.
       SD SF.
       01 SR PIC X(4).
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
           SORT SF ON ASCENDING KEY SR
               INPUT PROCEDURE IP
               OUTPUT PROCEDURE OP
           DISPLAY "AFTER SORT"
           CALL "PB3338SB"
           STOP RUN.
       IP SECTION.
       IP-1.
      >>TURN EC-FLOW-APPLY-COMMIT CHECKING OFF
           CALL "PB3338SB"
      >>TURN EC-FLOW-APPLY-COMMIT CHECKING ON
           CALL "PB3338SC"
           CALL "PB3338SB"
           MOVE "AAAA" TO SR
           RELEASE SR.
       OP SECTION.
       OP-1.
           RETURN SF AT END CONTINUE END-RETURN.
       END PROGRAM PB3338SA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3338SB.
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
       END PROGRAM PB3338SB.
      >>TURN EC-FLOW-APPLY-COMMIT CHECKING OFF
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3338SC.
       PROCEDURE DIVISION.
           CALL "PB3338SB"
           GOBACK.
       END PROGRAM PB3338SC.
