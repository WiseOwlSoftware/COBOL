      *>--- conformance-case
      *> id: g2rcr01_recursive_attribute
      *> rules: [2002-GR-11.9.3-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:11.9.3 GR4 - "The RECURSIVE clause specifies that the
      *> program and any programs contained within it are recursive.
      *> The program may be called while it is active and may call
      *> itself. If the RECURSIVE clause is not specified in a program
      *> or implied for a program, the program shall not be called
      *> while it is active."  cite.py --check -> OK
      *> §2002:8.6.5 - "The program cannot be activated while it is
      *> active unless RECURSIVE is specified."  cite.py OK
      *> §2002:14.8.4.3 GR3e - a CALL of an active program without the
      *> recursive attribute sets EC-PROGRAM-RECURSIVE-CALL and the
      *> program is not called; GR3g1 - with "an ON OVERFLOW or ON
      *> EXCEPTION phrase is specified in the CALL statement, control
      *> is transferred to imperative-statement-1" and NOT ON EXCEPTION
      *> is ignored.  cite.py OK (both)
      *> §2002:8.4.5.2 rule 1 - a contained non-common program may be
      *> referenced in its container "or, if the program possesses the
      *> recursive attribute, in the program itself".  cite.py OK
      *> §2002:8.6.3 - local-storage: "Each activated instance of the
      *> runtime element has its own copy of the item".  cite.py OK
      *> DERIVATION OF THE EXPECTED OUTPUT:
      *>   G2RCRA IS RECURSIVE PROGRAM calls itself with depth 1, 2, 3
      *>   (each activation's own LS-N), so RA ENTER 1..3; at depth 3
      *>   it calls G2RCRC.
      *>   G2RCRC has no RECURSIVE clause; contained in G2RCRA it is
      *>   recursive by GR4 ("any programs contained within it"), so
      *>   its self-call is legal: RC ENTER 1, RC ENTER 2, RC LEAVE 2,
      *>   RC LEAVE 1, then RA LEAVE 3, 2, 1 (each LK-D is still its
      *>   caller's own copy).
      *>   G2RCN1 (no RECURSIVE, not contained in a recursive program)
      *>   calls G2RCN2, which calls G2RCN1 while it is active: not
      *>   called (GR4, CALL GR3e); ON EXCEPTION runs (N2 EXCEPTION,
      *>   NOT ON EXCEPTION ignored), then the 2002 ON OVERFLOW form
      *>   takes the same route (N2 OVERFLOW). N1 ENTER appears once;
      *>   N2 LEAVE, N1 LEAVE, MAIN END.
      *> EACH LEG CAN FAIL: a refused self-call or a shared depth
      *> prints fewer or wrong RA/RC lines; a compiler that does not
      *> give the contained program the attribute refuses or reroutes
      *> G2RCRC's self-call; re-entering G2RCN1 prints a second N1
      *> ENTER or N2 NOT EXCEPTION; an ON OVERFLOW not taken loses N2
      *> OVERFLOW.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RCR01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-D PIC 9 VALUE 1.
       PROCEDURE DIVISION.
           CALL "G2RCRA" USING WS-D
           CALL "G2RCN1"
           DISPLAY "MAIN END"
           STOP RUN.
       END PROGRAM G2RCR01.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RCRA IS RECURSIVE PROGRAM.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 LS-N PIC 9.
       LINKAGE SECTION.
       01 LK-D PIC 9.
       PROCEDURE DIVISION USING LK-D.
           DISPLAY "RA ENTER " LK-D
           IF LK-D < 3
               COMPUTE LS-N = LK-D + 1
               CALL "G2RCRA" USING LS-N
           ELSE
               MOVE 1 TO LS-N
               CALL "G2RCRC" USING LS-N
           END-IF
           DISPLAY "RA LEAVE " LK-D
           GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RCRC.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 LS-M PIC 9.
       LINKAGE SECTION.
       01 LK-C PIC 9.
       PROCEDURE DIVISION USING LK-C.
           DISPLAY "RC ENTER " LK-C
           IF LK-C < 2
               COMPUTE LS-M = LK-C + 1
               CALL "G2RCRC" USING LS-M
           END-IF
           DISPLAY "RC LEAVE " LK-C
           GOBACK.
       END PROGRAM G2RCRC.
       END PROGRAM G2RCRA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RCN1.
       PROCEDURE DIVISION.
           DISPLAY "N1 ENTER"
           CALL "G2RCN2"
           DISPLAY "N1 LEAVE"
           GOBACK.
       END PROGRAM G2RCN1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RCN2.
       PROCEDURE DIVISION.
           CALL "G2RCN1"
               ON EXCEPTION DISPLAY "N2 EXCEPTION"
               NOT ON EXCEPTION DISPLAY "N2 NOT EXCEPTION"
           END-CALL
           CALL "G2RCN1"
               ON OVERFLOW DISPLAY "N2 OVERFLOW"
           END-CALL
           DISPLAY "N2 LEAVE"
           GOBACK.
       END PROGRAM G2RCN2.
