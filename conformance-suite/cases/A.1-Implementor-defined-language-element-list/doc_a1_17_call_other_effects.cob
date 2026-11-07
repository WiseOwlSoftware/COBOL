*>--- conformance-case
*> id: doc_a1_17_call_other_effects
*> rules: [DOC-A.1-17]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §2023:14.9.4.4 GR3 h) 2. (Annex A.1 item 17) - the other effects
      *> of a CALL that was not successful, whose condition is handled by
      *> exception processing (checking enabled, no ON EXCEPTION phrase).
      *>   cite.py --check 14.9.4.4 "any applicable exception processing
      *>     statements are executed. If control is returned from these
      *>     statements, control is then transferred to the end of the
      *>     CALL statement." -> OK 3) h) 2.
      *>   cite.py --check 14.9.4.4 "All other effects of the CALL
      *>     statement are defined by the implementor." -> OK 3) h) 2.
      *> THE DETERMINATION PINNED (docs/CONFORMANCE.md DOC-A.1-17): none
      *> beyond the transfer of control. No program is activated, every
      *> USING argument and the RETURNING item keep the values they held
      *> before the CALL, NOT ON EXCEPTION is not executed, and the last
      *> exception status names the condition. This case drives every
      *> condition GR3 h) 2. reaches for a CALL: the five EC-PROGRAM
      *> conditions and EC-EXTERNAL's CALL-time one. Its 2002/2014 twin,
      *> without EC-EXTERNAL (a 2023 name), is
      *> doc_a1_17_call_other_effects_2002.
      *> Each declarative ends with RESUME AT NEXT STATEMENT (§2023:14.9.33.4
      *> GR2), so control returns from it. Without RESUME a fatal
      *> condition's declarative has no next executable statement and
      *> the run unit ends abnormally (cite.py --check 14.6.3 "In the
      *> declarative section for a fatal exception condition, there is
      *> no next executable statement" -> OK 3)).
      *> DERIVATION of every .out line:
      *>   A117OK-RAN / NOT-ON-EXCEPTION / CONTROL X=ZZZZ R=0009
      *>       control: a conforming CALL activates A117OK, which stores
      *>       into both, and NOT ON EXCEPTION runs (GR3 i)). So the
      *>       "X=ABCD R=1234" lines below are not a CALL that does nothing.
      *>   DECL EC-PROGRAM-NOT-FOUND / NOT-FOUND X=ABCD R=1234
      *>       no program A117NOPE (GR3 b)); the declarative runs, then
      *>       the end of the CALL: X and R untouched, no NOT ON EXCEPTION.
      *>       EXCEPTION-STATUS is 31 characters, left-justified (15.33.3;
      *>       the runner trims the trailing spaces; cite.py --check
      *>       14.6.13.1.1 "the last exception status is set to indicate
      *>       that exception condition" -> OK).
      *>   DECL EC-PROGRAM-PTR-NULL / PTR-NULL X=ABCD R=1234
      *>       cite.py --check 14.9.4.4 "the EC-PROGRAM-PTR-NULL exception
      *>       condition is set to exist" -> OK 3).
      *>   DECL EC-PROGRAM-ARG-MISMATCH / ARG-MISMATCH X=ABCD R=1234
      *>       one argument against A117AM's two required formals;
      *>       cite.py --check 14.9.4.4 "the EC-PROGRAM-ARG-MISMATCH
      *>       exception condition is set to exist if checking for it is
      *>       enabled in both the activated program and activating runtime
      *>       element" -> OK 3) d). A117AM-RAN is not displayed.
      *>   DECL EC-EXTERNAL-FORMAT-CONFLICT / FORMAT-CONFLICT X=ABCD R=1234
      *>       XE is PIC 9(4) here and PIC X(6) in A117FC (GR3 e), row
      *>       "Correspondence of external data item formats" -> OK 3));
      *>       checking is enabled in both before the Environment division
      *>       (cite.py --check 14.8.4.1 "the EC-EXTERNAL exception
      *>       conditions to be checked shall be enabled in both the
      *>       activating and activated runtime elements" -> OK).
      *>       A117FC-RAN is not displayed.
      *>   DECL EC-PROGRAM-RESOURCES / RESOURCES X=ABCD
      *>       NOT-ON-EXCEPTION=NO  ACTIVATED=NO
      *>       the recursive A117DP calls itself until an activation's
      *>       stack reserve is not left (GR3 c); the one resource checked,
      *>       docs/CONFORMANCE.md DOC-A.1-14). At the failing level its
      *>       own declarative runs once; afterwards the argument LX holds
      *>       ABCD, NOE was not set and no entry was counted. (NOE is
      *>       PIC X(3), hence the two spaces.) Each level passes its own
      *>       LOCAL-STORAGE item, not its formal: a formal passed on by
      *>       reference through every level is kb/Work PB4768.
      *>   RESOURCES-RETURNED X=ABCD
      *>       every level above the failing one returned normally.
      *>   DECL EC-PROGRAM-RECURSIVE-CALL / RECURSIVE-CALL X=ABCD R=1234
      *>       A117RB calls the active, non-recursive A117M (cite.py
      *>       --check 14.9.4.4 "If the program being called is in the
      *>       active state and that program does not have the recursive
      *>       attribute, the EC-PROGRAM-RECURSIVE-CALL exception condition
      *>       is set to exist" -> OK 3) f)); A117RB's own declarative runs
      *>       and A117M is not re-entered.
      *>   END EC-PROGRAM-RECURSIVE-CALL
      *>       back in A117M the last exception status, which is the run
      *>       unit's (cite.py --check 14.6.13.1.1 "a last exception status
      *>       exists for the entire run unit" -> OK), still names the last
      *>       condition: A117RB's successful return does not reset it.
       >>TURN EC-PROGRAM CHECKING ON
       >>TURN EC-EXTERNAL CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. A117M.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(4).
       01 R PIC 9(4).
       01 XE PIC 9(4) IS EXTERNAL.
       01 PP USAGE PROGRAM-POINTER.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H-PROG SECTION.
           USE AFTER EXCEPTION CONDITION EC-PROGRAM EC-EXTERNAL.
       H-PROG-P.
           DISPLAY "DECL " FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       M.
           MOVE "ABCD" TO X MOVE 1234 TO R
           CALL "A117OK" USING X RETURNING R
               NOT ON EXCEPTION DISPLAY "NOT-ON-EXCEPTION"
           END-CALL
           DISPLAY "CONTROL X=" X " R=" R
           MOVE "ABCD" TO X MOVE 1234 TO R
           CALL "A117NOPE" USING X RETURNING R
               NOT ON EXCEPTION DISPLAY "NOT-ON-EXCEPTION"
           END-CALL
           DISPLAY "NOT-FOUND X=" X " R=" R
           MOVE "ABCD" TO X MOVE 1234 TO R
           SET PP TO NULL
           CALL PP USING X RETURNING R
               NOT ON EXCEPTION DISPLAY "NOT-ON-EXCEPTION"
           END-CALL
           DISPLAY "PTR-NULL X=" X " R=" R
           MOVE "ABCD" TO X MOVE 1234 TO R
           CALL "A117AM" USING X RETURNING R
               NOT ON EXCEPTION DISPLAY "NOT-ON-EXCEPTION"
           END-CALL
           DISPLAY "ARG-MISMATCH X=" X " R=" R
           MOVE "ABCD" TO X MOVE 1234 TO R
           CALL "A117FC" USING X RETURNING R
               NOT ON EXCEPTION DISPLAY "NOT-ON-EXCEPTION"
           END-CALL
           DISPLAY "FORMAT-CONFLICT X=" X " R=" R
           MOVE "ABCD" TO X
           CALL "A117DP" USING X
           DISPLAY "RESOURCES-RETURNED X=" X
           CALL "A117RB"
           DISPLAY "END " FUNCTION EXCEPTION-STATUS
           STOP RUN.
       END PROGRAM A117M.

       >>TURN EC-PROGRAM CHECKING ON
       >>TURN EC-EXTERNAL CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. A117OK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 XE PIC 9(4) IS EXTERNAL.
       LINKAGE SECTION.
       01 L-A PIC X(4).
       01 L-R PIC 9(4).
       PROCEDURE DIVISION USING L-A RETURNING L-R.
       P.
           DISPLAY "A117OK-RAN"
           MOVE "ZZZZ" TO L-A MOVE 9 TO L-R
           GOBACK.
       END PROGRAM A117OK.

       >>TURN EC-PROGRAM CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. A117AM.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-A PIC X(4).
       01 L-B PIC X(4).
       01 L-R PIC 9(4).
       PROCEDURE DIVISION USING L-A L-B RETURNING L-R.
       P.
           DISPLAY "A117AM-RAN"
           MOVE "ZZZZ" TO L-A MOVE 9 TO L-R
           GOBACK.
       END PROGRAM A117AM.

       >>TURN EC-EXTERNAL CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. A117FC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 XE PIC X(6) IS EXTERNAL.
       LINKAGE SECTION.
       01 L-A PIC X(4).
       01 L-R PIC 9(4).
       PROCEDURE DIVISION USING L-A RETURNING L-R.
       P.
           DISPLAY "A117FC-RAN"
           MOVE "ZZZZ" TO L-A MOVE 9 TO L-R
           GOBACK.
       END PROGRAM A117FC.

       >>TURN EC-PROGRAM CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. A117RB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(4) VALUE "ABCD".
       01 R PIC 9(4) VALUE 1234.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H-RB SECTION.
           USE AFTER EXCEPTION CONDITION EC-PROGRAM.
       H-RB-P.
           DISPLAY "DECL " FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       P.
           CALL "A117M" USING X RETURNING R
               NOT ON EXCEPTION DISPLAY "NOT-ON-EXCEPTION"
           END-CALL
           DISPLAY "RECURSIVE-CALL X=" X " R=" R
           GOBACK.
       END PROGRAM A117RB.

       >>TURN EC-PROGRAM CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. A117DP RECURSIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ENTRIES    PIC 9(9) VALUE 0.
       01 FAIL-DEPTH PIC 9(9) VALUE 0.
       01 DEPTH      PIC 9(9) VALUE 0.
       LOCAL-STORAGE SECTION.
       01 MY-DEPTH   PIC 9(9).
       01 PRE-ENTRIES PIC 9(9).
       01 NOE        PIC X(3) VALUE "NO".
       01 LX         PIC X(4) VALUE "ABCD".
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
           ADD 1 TO ENTRIES ADD 1 TO DEPTH
           MOVE DEPTH TO MY-DEPTH
           MOVE ENTRIES TO PRE-ENTRIES
           CALL "A117DP" USING LX
               NOT ON EXCEPTION MOVE "YES" TO NOE
           END-CALL
           IF MY-DEPTH = FAIL-DEPTH
               IF ENTRIES = PRE-ENTRIES
                   DISPLAY "RESOURCES X=" LX " NOT-ON-EXCEPTION=" NOE
                       " ACTIVATED=NO"
               ELSE
                   DISPLAY "RESOURCES ACTIVATED=YES"
               END-IF
           END-IF
           SUBTRACT 1 FROM DEPTH
           GOBACK.
       END PROGRAM A117DP.
