      *>--- conformance-case
      *> id: g2con01_every_2002_position
      *> rules: [2002-SR-14.8.8.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.8.2 SR1 -- "The CONTINUE statement may be used
      *> anywhere a conditional statement or an imperative-statement may
      *> be used", with §2002:14.8.8.3 GR1: it "has no effect on the
      *> execution of the runtime element". Written in the 2002-only
      *> positions as well as the common ones:
      *> - a sentence of its own;
      *> - CALL format 1's ON OVERFLOW imperative-statement-1 (archaic,
      *>   removed later). The program does not exist, so
      *>   EC-PROGRAM-NOT-FOUND transfers control to imperative-
      *>   statement-1 and then to the end of the CALL
      *>   (§2002:14.8.4.3 GR3): L1 prints; the same CALL with a DISPLAY
      *>   there prints L2, showing the overflow path is the one taken;
      *> - CALL ON EXCEPTION imperative-statement-1: L3 prints and NOT
      *>   ON EXCEPTION is ignored (no NOEX);
      *> - IF statement-1 and ELSE statement-2, EVALUATE WHEN;
      *> - ON SIZE ERROR: 1 + 9 does not fit PIC 9, so N1 keeps 1
      *>   (§2002:14.6.4: "the values of all of the resultant data
      *>   items remain unchanged") and NOT ON SIZE ERROR is skipped:
      *>   L4 N1=1; NOT ON SIZE ERROR: 1 + 1 = 2, L5 N1=2;
      *> - the communication RECEIVE's NO DATA and WITH DATA
      *>   imperative-statements (§2002:A.2.5.1; obsolete at 2002,
      *>   COBOL0903 warning). MSG is spaces, so the RECEIVE is not
      *>   executed: no message control system is needed, the leg is
      *>   that the form is accepted.
      *> Fails if any position refuses CONTINUE, or a CONTINUE changes
      *> control flow (a BAD- line, or a missing L line).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CON1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N1 PIC 9 VALUE 1.
       01 MSG PIC X(20) VALUE SPACES.
       COMMUNICATION SECTION.
       CD CIN FOR INPUT
           SYMBOLIC QUEUE IS QN
           MESSAGE COUNT IS MC.
       PROCEDURE DIVISION.
       MAIN.
           CONTINUE.
           CALL "G2CNOSUCH" ON OVERFLOW CONTINUE END-CALL
           DISPLAY "L1 AFTER-OVERFLOW-CONTINUE"
           CALL "G2CNOSUCH" ON OVERFLOW DISPLAY "L2 OVERFLOW-RAISED"
           END-CALL
           CALL "G2CNOSUCH" ON EXCEPTION CONTINUE
               NOT ON EXCEPTION DISPLAY "NOEX"
           END-CALL
           DISPLAY "L3 AFTER-EXCEPTION-CONTINUE"
           IF N1 = 1 CONTINUE ELSE DISPLAY "BAD-IF1" END-IF
           IF N1 = 2 DISPLAY "BAD-IF2" ELSE CONTINUE END-IF
           ADD 9 TO N1 ON SIZE ERROR CONTINUE
               NOT ON SIZE ERROR DISPLAY "BAD-NOSE"
           END-ADD
           DISPLAY "L4 N1=" N1
           ADD 1 TO N1 ON SIZE ERROR DISPLAY "BAD-SE"
               NOT ON SIZE ERROR CONTINUE
           END-ADD
           DISPLAY "L5 N1=" N1
           EVALUATE N1
               WHEN 2 CONTINUE
               WHEN OTHER DISPLAY "BAD-EVAL"
           END-EVALUATE
           IF MSG = "NEVER"
               RECEIVE CIN MESSAGE INTO MSG
                   NO DATA CONTINUE
                   WITH DATA CONTINUE
               END-RECEIVE
           END-IF
           DISPLAY "L6 END"
           STOP RUN.
