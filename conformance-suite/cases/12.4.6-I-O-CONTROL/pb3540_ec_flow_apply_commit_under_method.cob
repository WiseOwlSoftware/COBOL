*>--- conformance-case
*> id: pb3540_ec_flow_apply_commit_under_method
*> rules: [GR-12.4.6.3.4-3, GR-9.1.18.7-p1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3540 - the activation arm of 12.4.6.3.4 GR3 for a METHOD: "If a runtime element that contains an APPLY
*> COMMIT clause is attempted to be activated under the control of a recursive runtime element, then the EC-FLOW-APPLY
*> exception condition is set to exist." 9.1.18.7 words it as "an APPLY COMMIT clause is attempted to be activated in a
*> runtime element that is invoked by a recursive runtime element", and 8.6.6 makes a method one: "Functions and
*> methods are always recursive." Table 13 (14.6.13.1.6) names the condition EC-FLOW-APPLY-COMMIT, Fatal; it is the
*> ACTIVATING element's condition, decided by the CALL statement's checking (docs/CONFORMANCE.md 5, A.4.3
*> determination (d)). The method DOWORK of PB3540A is running throughout and CALLs PB3540B, whose clause names W,
*> once with checking on at the CALL and once with checking off. Expected:
*>   HANDLED=EC-FLOW-APPLY-COMMIT  the first CALL, checking on: the condition exists, the declarative runs, and
*>                                 RESUME AT NEXT STATEMENT continues after the CALL; the activation was only
*>                                 attempted, so PB3540B does not run.
*>   B W=Z                         the second CALL, checking off at that CALL: no condition, and the clause stays
*>                                 inactive (determination (d)), so PB3540B's ROLLBACK has no active clause and is a
*>                                 CONTINUE (14.9.36.4 GR1).
*>   END
      >>TURN EC-FLOW-APPLY-COMMIT CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3540M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB3540A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB3540A.
       PROCEDURE DIVISION.
       MAIN-P.
           INVOKE PB3540A "NEW" RETURNING O
           INVOKE O "DOWORK"
           DISPLAY "END"
           STOP RUN.
       END PROGRAM PB3540M.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB3540A INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. DOWORK.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-SEC SECTION.
           USE AFTER EXCEPTION CONDITION EC-FLOW-APPLY-COMMIT.
       D-PARA.
           DISPLAY "HANDLED=" FUNCTION EXCEPTION-STATUS
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MMAIN SECTION.
       MMAIN-P.
           CALL "PB3540B"
      >>TURN EC-FLOW-APPLY-COMMIT CHECKING OFF
           CALL "PB3540B".
       END METHOD DOWORK.
       END OBJECT.
       END CLASS PB3540A.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3540B.
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
       END PROGRAM PB3540B.
