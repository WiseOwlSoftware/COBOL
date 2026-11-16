      *>--- conformance-case
      *> id: g2-pd-raising-ec-user
      *> rules: [2002-SR-14.1.2-7]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.1.2 SR7 - "Exception-name-1 shall be a level-3
      *>   exception-name for EC-USER." The ACCEPTED arm: a procedure
      *>   division header RAISING EC-USER-BAD.
      *> §2002:14.5.12.1 - "The level-3 exceptions-names for
      *>   user-defined exceptions shall start with the characters"
      *>   'EC-USER-' and end with a suffix of basic letters, digits,
      *>   hyphen and underscore: EC-USER-BAD is one. "All user-defined
      *>   exception conditions shall be non-fatal"; "Checking is not
      *>   enabled unless the TURN directive is used to enable checking
      *>   for the exception" (the >>TURN below enables it).
      *> The header is load-bearing: §2002:14.8.13.2 SR3 requires an
      *>   EC-USER name in a RAISING phrase of EXIT (and so of GOBACK)
      *>   to be "specified in the RAISING phrase of the procedure
      *>   division header of the source element in which this EXIT
      *>   statement is contained".
      *> DERIVATION.
      *>   IN SUB - G2PDR1S runs (CALL).
      *>   CAUGHT [EC-USER-BAD     ] - §2002:14.8.17.3 GR1: a GOBACK in
      *>     a called program "operates as if executing an EXIT PROGRAM
      *>     statement with the RAISING phrase"; §2002:14.8.13.3 GR3/GR3a:
      *>     "an exception condition is raised in the activating runtime
      *>     element if checking for that exception condition is enabled
      *>     in the activating runtime element", and it is
      *>     exception-name-1. Non-fatal with an applicable USE
      *>     (§2002:14.5.12.1.3 item 2): "the associated declarative is
      *>     executed". EXCEPTION-STATUS (§2002:15.28.2 RV1) is the
      *>     31-character left-justified name, moved to X(16).
      *>   AFTER - §2002:14.5.12.1.3: "If execution of the declarative
      *>     completes normally, execution continues as specified in the
      *>     statement for normal execution": after the CALL.
      *> Fails if: the header is refused (SR7 over-applied to a level-3
      *>   EC-USER name), the header is ignored so the raise is lost
      *>   (no CAUGHT line), or the raise is not EC-USER-BAD.
       >>TURN EC-USER-BAD CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PDR1M.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-EC PIC X(16).
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER EXCEPTION CONDITION EC-USER-BAD.
       D1-P.
           MOVE FUNCTION EXCEPTION-STATUS TO WS-EC
           DISPLAY "CAUGHT [" WS-EC "]".
       END DECLARATIVES.
       M SECTION.
       M-P.
           CALL "G2PDR1S"
           DISPLAY "AFTER"
           STOP RUN.
       END PROGRAM G2PDR1M.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PDR1S.
       PROCEDURE DIVISION RAISING EC-USER-BAD.
       S-P.
           DISPLAY "IN SUB".
           GOBACK RAISING EXCEPTION EC-USER-BAD.
       END PROGRAM G2PDR1S.
