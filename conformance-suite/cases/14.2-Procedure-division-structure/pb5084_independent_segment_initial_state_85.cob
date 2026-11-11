      *>--- conformance-case
      *> id: pb5084_independent_segment_initial_state_85
      *> rules: [FMT-14.2.1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5084 - 1985 XVI.1.4.3: an independent segment (50
      *>   through 99, XVI.3.2.4 GR3) "is also in its initial state"
      *>   when control reaches it from another segment by an explicit
      *>   transfer (PERFORM IND-A twice; GO TO C-SW twice), by a SORT
      *>   entering its input procedure (IND-S) and by falling through
      *>   (M-FALL into IND-F), and "in its last-used state" when
      *>   control returns to it implicitly (PERFORM HELPER from C-SW's
      *>   segment). VI.6.7.4 GR1: the initial state returns an
      *>   ALTERed GO TO to its written target. The fixed segment 40
      *>   (FIX-A) keeps its ALTER. Fails if a re-entered independent
      *>   segment prints ALTERED, or C-SW forgets its ALTER across the
      *>   PERFORM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5084IS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SORT-FILE ASSIGN TO "PB5084WK".
       DATA DIVISION.
       FILE SECTION.
       SD  SORT-FILE.
       01  SORT-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01  CNT PIC 9 VALUE 0.
       01  FCNT PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       M-1.
           PERFORM IND-A.
           PERFORM IND-A.
           PERFORM FIX-A.
           PERFORM FIX-A.
           SORT SORT-FILE ON ASCENDING KEY SORT-REC
               INPUT PROCEDURE IS IND-S
               OUTPUT PROCEDURE IS OUT-S.
           SORT SORT-FILE ON ASCENDING KEY SORT-REC
               INPUT PROCEDURE IS IND-S
               OUTPUT PROCEDURE IS OUT-S.
           GO TO C-SW.
       M-BACK.
           ADD 1 TO CNT.
           IF CNT < 2 GO TO C-SW.
       M-FALL.
           EXIT.
       IND-F SECTION 80.
       F-SW.
           GO TO F-INIT.
       F-INIT.
           DISPLAY "F INITIAL".
           ALTER F-SW TO PROCEED TO F-ALT.
           GO TO F-NEXT.
       F-ALT.
           DISPLAY "F ALTERED".
       F-NEXT.
           ADD 1 TO FCNT.
           IF FCNT < 2 GO TO M-FALL.
           STOP RUN.
       IND-C SECTION 70.
       C-SW.
           GO TO C-INIT.
       C-INIT.
           DISPLAY "C INITIAL".
           ALTER C-SW TO PROCEED TO C-ALT.
           PERFORM HELPER.
           GO TO C-SW.
       C-ALT.
           DISPLAY "C ALTERED".
           GO TO M-BACK.
       IND-A SECTION 60.
       A-SW.
           GO TO A-INIT.
       A-INIT.
           DISPLAY "A INITIAL".
           ALTER A-SW TO PROCEED TO A-ALT.
           GO TO A-END.
       A-ALT.
           DISPLAY "A ALTERED".
       A-END.
           EXIT.
       FIX-A SECTION 40.
       X-SW.
           GO TO X-INIT.
       X-INIT.
           DISPLAY "X INITIAL".
           ALTER X-SW TO PROCEED TO X-ALT.
           GO TO X-END.
       X-ALT.
           DISPLAY "X ALTERED".
       X-END.
           EXIT.
       IND-S SECTION 90.
       S-SW.
           GO TO S-INIT.
       S-INIT.
           DISPLAY "S INITIAL".
           MOVE "AAAAA" TO SORT-REC.
           RELEASE SORT-REC.
           ALTER S-SW TO PROCEED TO S-ALT.
           GO TO S-END.
       S-ALT.
           DISPLAY "S ALTERED".
       S-END.
           EXIT.
       OUT-S SECTION.
       O-1.
           RETURN SORT-FILE AT END GO TO O-END.
           DISPLAY "RETURNED " SORT-REC.
           GO TO O-1.
       O-END.
           EXIT.
       HELP-SEC SECTION.
       HELPER.
           DISPLAY "HELPER".
