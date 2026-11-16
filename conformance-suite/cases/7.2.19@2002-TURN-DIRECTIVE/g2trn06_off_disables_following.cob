      *>--- conformance-case
      *> id: g2trn06_off_disables_following
      *> rules: [2002-GR-7.2.19.3-6]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.19.3 GR6 - TURN ... CHECKING OFF: checking "is
      *>   disabled for all procedure division statements and procedure
      *>   division headers that follow in the compilation group and
      *>   remains disabled until another TURN directive for
      *>   exception-name-1 with the ON phrase is encountered; if
      *>   file-name-1 is specified, checking is disabled only for
      *>   exception conditions associated with that file-name. If
      *>   specified within a statement, checking for that statement is
      *>   not turned off."
      *>   cite.py: OK  §2002:7.2.19.3 GR6
      *> Supporting: §2002:7.2.19.3 GR4 (ON enables the statements that
      *>   follow); §2002:9.1.12 (I-O status 1x -> EC-I-O-AT-END set to
      *>   exist; with an AT END phrase no USE procedure is executed);
      *>   §2002:14.8.29.3 GR21 (I-O status '10', control to the AT END
      *>   imperative); §2002:14.5.12.1 raised only when enabled, and
      *>   then "the last exception status is set to indicate that
      *>   exception condition"; §2002:14.8.35.3 GR25 SET LAST EXCEPTION
      *>   TO OFF; §2002:15.28.2 RV1 spaces for no exception. Every one
      *>   cite.py --check OK.
      *> Not built on SIZE ERROR: with the phrase §2002:14.6.4 never sets
      *>   EC-SIZE-TRUNCATION to exist (§2002:E.15), so such legs cannot
      *>   tell OFF from ON.
      *> DERIVATION. F1 and F2 are empty: every READ is at end; each
      *> READ is preceded by SET LAST EXCEPTION TO OFF and its AT END
      *> phrase shows the last exception status.
      *>   1 checking is ON from the first line (GR4); the OFF TURN sits
      *>     inside this READ, so checking for that statement is not
      *>     turned off (GR6) -> EC-I-O-AT-END.
      *>   2 the next READ follows the OFF -> spaces.
      *>   3 a later READ, no ON between: "remains disabled" -> spaces.
      *>   4 after a new >>TURN EC-I-O-AT-END CHECKING ON -> the name.
      *>   5 EC-I-O-AT-END ON for every file, then EC-I-O-AT-END F2
      *>     OFF: disabled only for F2 -> F1 [EC-I-O-AT-END], F2 spaces.
      *> An implementation that turned the enclosing statement off
      *> shows spaces on 1; one whose OFF did not reach the following
      *> statements shows the name on 2 or 3; one that let the OFF
      *> outlive the ON shows spaces on 4; one that ignored file-name-1
      *> in an OFF shows spaces on 5 F1 or the name on 5 F2.
      *> The nested-statement question (a statement inside a phrase of
      *> the statement holding the TURN) is deliberately not exercised.
       >>TURN EC-I-O-AT-END CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2TRN06.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "G2TRN061.DAT".
           SELECT F2 ASSIGN TO "G2TRN062.DAT".
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
           READ F1
       >>TURN EC-I-O-AT-END CHECKING OFF
             AT END
               DISPLAY "1 [" FUNCTION EXCEPTION-STATUS "]"
           END-READ.
           CLOSE F1.
           OPEN INPUT F1.
           SET LAST EXCEPTION TO OFF.
           READ F1 AT END
               DISPLAY "2 [" FUNCTION EXCEPTION-STATUS "]"
           END-READ.
           CLOSE F1.
           OPEN INPUT F1.
           SET LAST EXCEPTION TO OFF.
           READ F1 AT END
               DISPLAY "3 [" FUNCTION EXCEPTION-STATUS "]"
           END-READ.
           CLOSE F1.
       >>TURN EC-I-O-AT-END CHECKING ON
           OPEN INPUT F1.
           SET LAST EXCEPTION TO OFF.
           READ F1 AT END
               DISPLAY "4 [" FUNCTION EXCEPTION-STATUS "]"
           END-READ.
           CLOSE F1.
       >>TURN EC-I-O-AT-END CHECKING ON
       >>TURN EC-I-O-AT-END F2 CHECKING OFF
           OPEN INPUT F1 F2.
           SET LAST EXCEPTION TO OFF.
           READ F1 AT END
               DISPLAY "5 F1 [" FUNCTION EXCEPTION-STATUS "]"
           END-READ.
           SET LAST EXCEPTION TO OFF.
           READ F2 AT END
               DISPLAY "5 F2 [" FUNCTION EXCEPTION-STATUS "]"
           END-READ.
           CLOSE F1 F2.
           STOP RUN.
