      *>--- conformance-case
      *> id: g2_propagate_handled_not_propagated
      *> rules: [2002-GR-7.2.17.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.17.3 GR2 - under PROPAGATE ON, "any exception
      *>   condition raised and not handled by either an exception
      *>   phrase or a declarative in that runtime element shall be
      *>   propagated as though a GOBACK RAISING LAST statement were
      *>   executed in a declarative for that exception condition".
      *>   All three arms in elements under PROPAGATE ON: handled by
      *>   the element's own declarative (B), handled by an exception
      *>   phrase (C), and not handled (D).
      *> RULES USED (each through scripts/spec/cite.py --check, OK):
      *>   §2002:7.2.19.3 GR4 - the TURN before G2PRP1A enables
      *>     EC-SIZE-ZERO-DIVIDE checking for every procedure division
      *>     statement that follows in the compilation group.
      *>   §2002:7.2.17.3 GR1 - PROPAGATE ON enables propagation for
      *>     the programs that follow it (B, C, D, not A).
      *>   §2002:14.6.4 - DIVIDE by zero with no SIZE ERROR phrase sets
      *>     EC-SIZE-ZERO-DIVIDE (Fatal, table 14); with the phrase
      *>     "no EC-SIZE exception declaratives are performed".
      *>   §2002:14.5.12.1.2 - fatal: item 1 conditional phrase, item 2
      *>     "the associated declarative is executed", item 3 PROPAGATE
      *>     ON "propagated as if a GOBACK statement with the RAISING
      *>     LAST EXCEPTION phrase were executed".
      *>   §2002:14.8.13.3 GR3 - the condition "is raised in the
      *>     activating runtime element if checking for that exception
      *>     condition is enabled in the activating runtime element".
      *>   §2002:14.8.31.3 GR2 - RESUME AT NEXT STATEMENT after a
      *>     propagated condition: "the applicable statement is the CALL
      *>     or INVOKE statement that activated the entity".
      *> EXPECTED OUTPUT, DERIVED (why each leg can fail):
      *>   B OWN DECLARATIVE / B AFTER DIVIDE / A AFTER B - B's own
      *>     USE handles the condition and RESUMEs, so it is not
      *>     propagated: a "A CAUGHT" line before A AFTER B would mean
      *>     a handled condition was propagated.
      *>   C PHRASE / C AFTER DIVIDE / A AFTER C - the SIZE ERROR
      *>     phrase handles it: again no "A CAUGHT".
      *>   A CAUGHT EC-SIZE-ZERO-DIVIDE / A AFTER D - D's condition is
      *>     unhandled, so it is propagated to A, raised at the CALL,
      *>     A's declarative runs and resumes after the CALL. "D AFTER
      *>     DIVIDE" never prints; without this arm the absence of
      *>     "A CAUGHT" in the B and C arms would prove nothing.
       >>TURN EC-SIZE-ZERO-DIVIDE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PRP1A.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DA SECTION.
           USE AFTER EXCEPTION CONDITION EC-SIZE.
           DISPLAY "A CAUGHT " FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN-A SECTION.
           CALL "G2PRP1B".
           DISPLAY "A AFTER B".
           CALL "G2PRP1C".
           DISPLAY "A AFTER C".
           CALL "G2PRP1D".
           DISPLAY "A AFTER D".
           STOP RUN.
       END PROGRAM G2PRP1A.
       >>PROPAGATE ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PRP1B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Z PIC 9 VALUE 0.
       01 Q PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DB SECTION.
           USE AFTER EXCEPTION CONDITION EC-SIZE.
           DISPLAY "B OWN DECLARATIVE".
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN-B SECTION.
           DIVIDE Z INTO 5 GIVING Q.
           DISPLAY "B AFTER DIVIDE".
           GOBACK.
       END PROGRAM G2PRP1B.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PRP1C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Z PIC 9 VALUE 0.
       01 Q PIC 9 VALUE 0.
       PROCEDURE DIVISION.
           DIVIDE Z INTO 5 GIVING Q
               ON SIZE ERROR
                 DISPLAY "C PHRASE"
           END-DIVIDE.
           DISPLAY "C AFTER DIVIDE".
           GOBACK.
       END PROGRAM G2PRP1C.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PRP1D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Z PIC 9 VALUE 0.
       01 Q PIC 9 VALUE 0.
       PROCEDURE DIVISION.
           DIVIDE Z INTO 5 GIVING Q.
           DISPLAY "D AFTER DIVIDE".
           GOBACK.
       END PROGRAM G2PRP1D.
