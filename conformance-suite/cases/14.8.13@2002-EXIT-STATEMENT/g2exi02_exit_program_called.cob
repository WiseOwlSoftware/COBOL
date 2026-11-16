      *>--- conformance-case
      *> id: g2exi02_exit_program_called
      *> rules: [2002-GR-14.8.13.3-6, 2002-FMT-14.8.13.1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.13.3 GR6 - "The execution of an EXIT PROGRAM statement
      *> causes the executing program to terminate, and control to return
      *> to the calling statement. If a RETURNING phrase is specified in
      *> the procedure division header of the program containing the EXIT
      *> statement, the value in the data item referenced by that
      *> RETURNING phrase becomes the result of the program activation."
      *> "The state of the calling runtime element is not altered ...
      *> except that the contents of data items ... shared between the
      *> calling runtime element and the called program may have been
      *> changed." For an initial program "an implicit CANCEL statement
      *> referencing that program is executed upon return", and "the
      *> exception condition that is specified in the RAISING phrase
      *> exists after the execution of that implicit CANCEL statement"
      *> (cite.py --check OK on each quotation, GR6).
      *> XPSUB IS INITIAL, its C starts at 0 and is incremented once per
      *> activation. Call 1 (mode 0): C=1, RES 11, plain EXIT PROGRAM
      *>  (Format 2): R=11 (the result), S=5 (the shared BY REFERENCE item
      *>  the callee changed), K=7 (not shared: unaltered); SUB-NEVER is
      *>  never shown (the program terminated).
      *> Call 2 (mode 1): C=1 again (the CANCEL leaves it in its initial
      *>  state), RES 22, EXIT PROGRAM RAISING EXCEPTION EC-USER-IX: the
      *>  condition exists in the caller, whose checking is enabled, so
      *>  DECL-IX runs (§2002:14.8.13.3 GR3); EC-USER-* is non-fatal
      *>  (§2002:14.5.12.1 "All user-defined exception conditions shall
      *>  be non-fatal") and the declarative completes normally, so
      *>  "execution continues as specified in the statement for normal
      *>  execution" (§2002:14.5.12.1.3): the CALL stores the result,
      *>  R=22, then DONE.
       >>TURN EC-USER-IX CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2EXI02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R  PIC 9(2) VALUE 0.
       01 S  PIC 9 VALUE 0.
       01 K  PIC 9 VALUE 7.
       01 M0 PIC 9 VALUE 0.
       01 M1 PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER EXCEPTION CONDITION EC-USER-IX.
       D1P.
           DISPLAY "DECL-IX".
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-PARA.
           CALL "G2EXI02S" USING BY CONTENT M0 BY REFERENCE S
               RETURNING R
           DISPLAY "R=" R " S=" S " K=" K
           CALL "G2EXI02S" USING BY CONTENT M1 BY REFERENCE S
               RETURNING R
           DISPLAY "R=" R
           DISPLAY "DONE"
           STOP RUN.
       END PROGRAM G2EXI02.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2EXI02S IS INITIAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 C PIC 9 VALUE 0.
       LINKAGE SECTION.
       01 MODE-X PIC 9.
       01 SH     PIC 9.
       01 RES    PIC 9(2).
       PROCEDURE DIVISION USING MODE-X SH RETURNING RES
           RAISING EC-USER-IX.
       P1.
           ADD 1 TO C
           DISPLAY "C=" C
           MOVE 5 TO SH
           IF MODE-X = 0
               MOVE 11 TO RES
               EXIT PROGRAM
           END-IF
           MOVE 22 TO RES
           EXIT PROGRAM RAISING EXCEPTION EC-USER-IX
           DISPLAY "SUB-NEVER".
       END PROGRAM G2EXI02S.
