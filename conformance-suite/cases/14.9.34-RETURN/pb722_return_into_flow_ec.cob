      *>--- conformance-case
      *> id: pb722_return_into_flow_ec
      *> rules: [GR-14.9.34.4-1, GR-14.9.34.4-3, GR-14.9.34.4-5]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB722 (campaign PB3353): RETURN ... INTO's OWN exception
      *> conditions, with a USE declarative.  ISO §14.9.34.4 GR1: "A
      *> RETURN statement may be executed only when it is within the
      *> range of an output procedure being executed by a MERGE or SORT
      *> statement that references file-name-1.  If it is executed at any
      *> other time, the EC-FLOW-RETURN exception condition is set to
      *> exist."  GR3: "After the execution of imperative-statement-1 in
      *> the AT END phrase, no RETURN statement may be executed as part of
      *> the current output procedure.  If such a RETURN statement is
      *> executed, the EC-SORT-MERGE-RETURN exception condition is set to
      *> exist".  Both are Table 13 fatal; the declarative runs under
      *> CHECKING ON and RESUME AT NEXT STATEMENT continues after the
      *> offending statement (§14.6.13.1.3 5), NOTE 1) -- except that a
      *> RESUME is a not-normal completion (§14.6.13.1.2 1)), which
      *> terminates the SORT whose output procedure raised it
      *> (§14.9.40.4 GR17), so nothing after leg D's offending RETURN
      *> runs inside that procedure.  Nothing of a
      *> RETURN with an INTO phrase happens for such a RETURN: GR5 b) "The
      *> implied MOVE statement does not occur if the execution of the
      *> RETURN statement was unsuccessful", and neither AT END nor NOT AT
      *> END runs.  The receiver is X(12) JUSTIFIED RIGHT holding "KEEP",
      *> so a move of the record area (still "K01PQRST") would show.  The
      *> declarative-LESS fatal arm and the checking-OFF arm are asserted
      *> with their exit codes by ReturnIntoExceptionPathTests (a corpus
      *> golden must exit 0).
      *> LEG D - EC-SORT-MERGE-RETURN.  RETURN 1 succeeds, RETURN 2 finds
      *> the end (AT END runs), RETURN 3 is the offending one: the
      *> declarative prints HANDLED and the receiver ("KEEP": the raise
      *> precedes the move); the RESUME ends the SORT, so neither phrase
      *> and not "D3-AFTER" runs, and MAIN's "DONE" shows "KEEP" too.
      *> LEG E - EC-FLOW-RETURN.  MAIN executes the RETURN with no SORT
      *> active: the declarative runs, neither phrase runs, the receiver
      *> keeps "KEEP".
       >>TURN EC-FLOW-RETURN EC-SORT-MERGE-RETURN CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB722R2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SRTF ASSIGN TO "pb722r2s.tmp".
       DATA DIVISION.
       FILE SECTION.
       SD SRTF RECORD IS VARYING IN SIZE FROM 3 TO 20
              DEPENDING ON WS-SLEN.
       01 S-REC.
          05 S-KEY PIC X(3).
          05 S-DATA PIC X(17).
       WORKING-STORAGE SECTION.
       01 WS-SLEN PIC 9(4) VALUE 8.
       01 WS-J12 PIC X(12) JUSTIFIED RIGHT.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-FLOW-RETURN
               EC-SORT-MERGE-RETURN.
       H-P.
           DISPLAY "HANDLED=" FUNCTION EXCEPTION-STATUS
               " R=[" WS-J12 "]".
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE "KEEP" TO WS-J12.
           RETURN SRTF INTO WS-J12
               AT END DISPLAY "E-ATEND"
               NOT AT END DISPLAY "E-NOTATEND"
           END-RETURN.
           DISPLAY "E-AFTER R=[" WS-J12 "]".
           SORT SRTF ON ASCENDING KEY S-KEY
               INPUT PROCEDURE IS IN-ONE
               OUTPUT PROCEDURE IS OUT-D.
           DISPLAY "DONE R=[" WS-J12 "]".
           STOP RUN.
       IN-ONE SECTION.
       IN-ONE-P.
           MOVE 8 TO WS-SLEN.
           MOVE "K01" TO S-KEY.
           MOVE "PQRST" TO S-DATA.
           RELEASE S-REC.
       OUT-D SECTION.
       OUT-D-P.
           RETURN SRTF INTO WS-J12
               AT END DISPLAY "D1-ATEND"
               NOT AT END DISPLAY "D1-NOTATEND R=[" WS-J12 "]"
           END-RETURN.
           RETURN SRTF INTO WS-J12
               AT END DISPLAY "D2-ATEND"
           END-RETURN.
           MOVE "KEEP" TO WS-J12.
           RETURN SRTF INTO WS-J12
               AT END DISPLAY "D3-ATEND"
               NOT AT END DISPLAY "D3-NOTATEND"
           END-RETURN.
           DISPLAY "D3-AFTER R=[" WS-J12 "]".
