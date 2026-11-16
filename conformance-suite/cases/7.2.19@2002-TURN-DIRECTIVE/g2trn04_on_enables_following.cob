      *>--- conformance-case
      *> id: g2trn04_on_enables_following
      *> rules: [2002-GR-7.2.19.3-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.19.3 GR4 - TURN ... CHECKING ON: enables checking for
      *>   the statements that follow; "if file-name-1 is specified,
      *>   checking is enabled only for exception conditions associated
      *>   with that file-name. If specified within a statement, the
      *>   TURN directive does not apply to that statement. Checking
      *>   remains enabled for every qualifying procedure division
      *>   statement and procedure division header that follows in the
      *>   compilation group until it is disabled by a TURN directive
      *>   with the OFF phrase."
      *>   cite.py: OK  §2002:7.2.19.3 GR4
      *> Supporting: §2002:7.2.19.3 GR1 default '>>TURN EC-ALL CHECKING
      *>   OFF'; §2002:9.1.12 an I-O status '1x' means "the associated
      *>   exception condition is set to exist" (EC-I-O-AT-END), and
      *>   with an AT END phrase "no USE procedure shall be executed";
      *>   §2002:14.8.29.3 GR21 (I-O status '10', control to the AT END
      *>   imperative); §2002:14.5.12.1 "the associated exception
      *>   conditions are raised based on the resulting I-O status
      *>   value" when checking is enabled, then "the last exception
      *>   status is set to indicate that exception condition";
      *>   otherwise nothing is raised; §2002:14.8.35.3 GR25 SET LAST
      *>   EXCEPTION TO OFF: "the last exception status is set to
      *>   indicate no exception"; §2002:15.28.2 RV1 "If the last
      *>   exception status indicates no exception, alphanumeric spaces
      *>   are returned". Every one cite.py --check OK.
      *> Not built on SIZE ERROR: with the phrase §2002:14.6.4 never sets
      *>   EC-SIZE-TRUNCATION to exist (§2002:E.15), so every such leg
      *>   prints spaces whatever the TURN does.
      *> DERIVATION. F1 and F2 are empty: every READ is at end; each
      *> READ is preceded by SET LAST EXCEPTION TO OFF and its AT END
      *> phrase shows the last exception status.
      *>   1 no TURN yet (GR1): unchecked -> spaces.
      *>   2 the ON TURN sits inside this READ, so it does not apply to
      *>     that statement (GR4) -> spaces.
      *>   3 the next READ follows the directive -> EC-I-O-AT-END.
      *>   4 SET LAST EXCEPTION TO OFF -> spaces.
      *>   5 a later READ, no OFF between: "remains enabled"
      *>     -> EC-I-O-AT-END.
      *>   6 after '>>TURN EC-I-O-AT-END CHECKING OFF' -> spaces.
      *>   7 'EC-I-O-AT-END F1 CHECKING ON': READ of the empty F1 is
      *>     raised -> F1 [EC-I-O-AT-END]; F2 is not named -> spaces.
      *> An implementation that applied the TURN to its enclosing
      *> statement shows the name on 2; one that did not enable the
      *> following statements shows spaces on 3 or 5; one that ignored
      *> file-name-1 shows the name on 7 F2.
      *> The nested-statement question (a statement inside a phrase of
      *> the statement holding the TURN) is deliberately not exercised.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2TRN04.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "G2TRN041.DAT".
           SELECT F2 ASSIGN TO "G2TRN042.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(4).
       FD F2.
       01 R2 PIC X(4).
       PROCEDURE DIVISION.
       M1.
           OPEN OUTPUT F1 F2.
           CLOSE F1 F2.
           OPEN INPUT F1.
           SET LAST EXCEPTION TO OFF.
           READ F1 AT END
               DISPLAY "1 [" FUNCTION EXCEPTION-STATUS "]"
           END-READ.
           CLOSE F1.
           OPEN INPUT F1.
           SET LAST EXCEPTION TO OFF.
           READ F1
       >>TURN EC-I-O-AT-END CHECKING ON
             AT END
               DISPLAY "2 [" FUNCTION EXCEPTION-STATUS "]"
           END-READ.
           CLOSE F1.
           OPEN INPUT F1.
           SET LAST EXCEPTION TO OFF.
           READ F1 AT END
               DISPLAY "3 [" FUNCTION EXCEPTION-STATUS "]"
           END-READ.
           CLOSE F1.
           SET LAST EXCEPTION TO OFF.
           DISPLAY "4 [" FUNCTION EXCEPTION-STATUS "]".
           OPEN INPUT F1.
           SET LAST EXCEPTION TO OFF.
           READ F1 AT END
               DISPLAY "5 [" FUNCTION EXCEPTION-STATUS "]"
           END-READ.
           CLOSE F1.
       >>TURN EC-I-O-AT-END CHECKING OFF
           OPEN INPUT F1.
           SET LAST EXCEPTION TO OFF.
           READ F1 AT END
               DISPLAY "6 [" FUNCTION EXCEPTION-STATUS "]"
           END-READ.
           CLOSE F1.
       >>TURN EC-I-O-AT-END F1 CHECKING ON
           OPEN INPUT F1 F2.
           SET LAST EXCEPTION TO OFF.
           READ F1 AT END
               DISPLAY "7 F1 [" FUNCTION EXCEPTION-STATUS "]"
           END-READ.
           SET LAST EXCEPTION TO OFF.
           READ F2 AT END
               DISPLAY "7 F2 [" FUNCTION EXCEPTION-STATUS "]"
           END-READ.
           CLOSE F1 F2.
           STOP RUN.
