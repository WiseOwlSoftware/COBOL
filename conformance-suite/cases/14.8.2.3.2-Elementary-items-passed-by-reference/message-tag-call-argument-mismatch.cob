*>--- conformance-case
*> id: message-tag-call-argument-mismatch
*> rules: [GR-14.8.2.3.2-4]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4740 review R1-5 - D-MCS-ARG (docs/CONFORMANCE.md): a message-tag argument conforms to a message-tag
      *> formal and to no other class. 14.8.2.3.2 rule 4 sends the pair that names a message tag to the rules the standard
      *> leaves empty (cite.py --check 14.8.2.3.2 OK, rule 4), so the implementation applies the pointer rule; 14.9.4.4 GR3
      *> d) makes the violation EC-PROGRAM-ARG-MISMATCH: "If a violation of these rules is detected, the
      *> EC-PROGRAM-ARG-MISMATCH exception condition is set to exist if checking for it is enabled in both the activated
      *> program and activating runtime element, the program call is not successful" (cite.py --check 14.9.4.4 OK, GR 3 d)).
      *> The >>TURN below covers both units. The first CALL passes a tag to the tag formal and succeeds; the second passes a
      *> numeric item to it, so the ON EXCEPTION phrase runs (GR3 h) 1) and the callee's statements never do.
      *> Derived: NOT-EXC, then MISMATCH-CAUGHT.
      >>TURN EC-PROGRAM-ARG-MISMATCH CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MTARGMAIN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-T USAGE MESSAGE-TAG.
       01 W-N PIC 9(8).
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "MTARGSUB" USING W-T
               ON EXCEPTION DISPLAY "MISMATCH-CAUGHT"
               NOT ON EXCEPTION DISPLAY "NOT-EXC"
           END-CALL
           CALL "MTARGSUB" USING W-N
               ON EXCEPTION DISPLAY "MISMATCH-CAUGHT"
               NOT ON EXCEPTION DISPLAY "NOT-EXC"
           END-CALL
           STOP RUN.
       END PROGRAM MTARGMAIN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. MTARGSUB.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-T USAGE MESSAGE-TAG.
       PROCEDURE DIVISION USING L-T.
       MAIN-PARA.
           GOBACK.
       END PROGRAM MTARGSUB.
