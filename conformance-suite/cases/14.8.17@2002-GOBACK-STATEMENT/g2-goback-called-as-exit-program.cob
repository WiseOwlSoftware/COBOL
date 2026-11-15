      *>--- conformance-case
      *> id: g2-goback-called-as-exit-program
      *> rules: [2002-GR-14.8.17.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.17.3 GR1 - a GOBACK executed in a program under the
      *>   control of a calling runtime element: "the program operates as
      *>   if executing an EXIT PROGRAM statement with the RAISING phrase,
      *>   if any, that is specified in the GOBACK statement".
      *> §2002:14.8.13.3 GR6 - "The execution of an EXIT PROGRAM
      *>   statement causes the executing program to terminate, and
      *>   control to return to the calling statement"; shared data "may
      *>   have been changed"; with a RETURNING phrase in the header "the
      *>   value in the data item referenced by that RETURNING phrase
      *>   becomes the result of the program activation".
      *> §2002:14.8.13.3 GR3 - with RAISING "an exception condition is
      *>   raised in the activating runtime element if checking for that
      *>   exception condition is enabled in the activating runtime
      *>   element"; GR3a: exception-name-1 is that condition.
      *> DERIVATION.
      *>  S1: MOVE 7 to the BY REFERENCE item, GOBACK: control returns to
      *>   the CALL, NEVER-1 is not shown: "AFTER-1 7".
      *>  S2: header RETURNING L-R = 42, GOBACK: the CALL's RETURNING item
      *>   receives the result: "AFTER-2 42".
      *>  S3: checking for EC-USER-G2ON is ON in the caller and OFF in S3
      *>   (>>TURN; user conditions are checked only when TURN enables
      *>   them, §2002:14.5.12.1). GOBACK RAISING EXCEPTION EC-USER-G2ON
      *>   raises it in the CALLER: its declarative shows
      *>   CAUGHT-IN-CALLER and RESUME AT NEXT STATEMENT continues after
      *>   the CALL (§2002:14.8.31.3 GR2): "IN-SUB3", "CAUGHT-IN-CALLER",
      *>   "AFTER-3".
      *> CAN FAIL: a GOBACK that ran on (NEVER-n), a lost BY REFERENCE
      *>   change (AFTER-1 1), a lost result (AFTER-2 00), or a RAISING
      *>   judged by the callee's checking (no CAUGHT-IN-CALLER).
      *>   conformance:cases/14.9.14-EXIT/pb408_raising_enablement_locus
      *>   (accept [2002]) adds the disabled-activator and LAST legs.
       >>TURN EC-USER-G2ON CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2GOBK01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-A  PIC 9  VALUE 1.
       01  WS-R  PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H-ON SECTION.
           USE AFTER EXCEPTION CONDITION EC-USER-G2ON.
       H-ON-P.
           DISPLAY "CAUGHT-IN-CALLER".
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           CALL "G2GOBKS1" USING WS-A.
           DISPLAY "AFTER-1 " WS-A.
           CALL "G2GOBKS2" RETURNING WS-R.
           DISPLAY "AFTER-2 " WS-R.
           CALL "G2GOBKS3".
           DISPLAY "AFTER-3".
           STOP RUN.
       END PROGRAM G2GOBK01.
       >>TURN EC-USER-G2ON CHECKING OFF
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2GOBKS1.
       DATA DIVISION.
       LINKAGE SECTION.
       01  L-A   PIC 9.
       PROCEDURE DIVISION USING L-A.
       S1-P.
           MOVE 7 TO L-A.
           GOBACK.
           DISPLAY "NEVER-1".
       END PROGRAM G2GOBKS1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2GOBKS2.
       DATA DIVISION.
       LINKAGE SECTION.
       01  L-R   PIC 99.
       PROCEDURE DIVISION RETURNING L-R.
       S2-P.
           MOVE 42 TO L-R.
           GOBACK.
           DISPLAY "NEVER-2".
       END PROGRAM G2GOBKS2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2GOBKS3.
       PROCEDURE DIVISION RAISING EC-USER-G2ON.
       S3-P.
           DISPLAY "IN-SUB3".
           GOBACK RAISING EXCEPTION EC-USER-G2ON.
           DISPLAY "NEVER-3".
       END PROGRAM G2GOBKS3.
