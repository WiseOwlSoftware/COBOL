      *>--- conformance-case
      *> id: g2rsm01_resume_in_declarative_2002
      *> rules: [2002-SR-14.8.31.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.31.2 SR1 - "The RESUME statement may be specified
      *>   only in a declarative." The ACCEPTED leg: RESUME written in
      *>   a declarative compiles and runs (its refusal leg, RESUME in
      *>   the nondeclarative portion, is
      *>   g2rsm02-resume-outside-declarative-2002).
      *> DERIVATION (2002 text only). >>TURN enables checking for
      *>   EC-USER-G2R (§2002:14.5.12.1: "Checking is not enabled
      *>   unless the TURN directive is used to enable checking for
      *>   the exception"). RAISE EXCEPTION EC-USER-G2R: "the
      *>   associated exception condition is raised"
      *>   (§2002:14.8.28.3 GR1), so the USE AFTER EXCEPTION
      *>   declarative runs: DECL. RESUME AT NEXT STATEMENT:
      *>   "control is transferred to an implicit CONTINUE statement
      *>   that immediately follows the end of the statement that was
      *>   executing" (§2002:14.8.31.3 GR2), the RAISE (GR2a "the
      *>   applicable statement is the one in which the exception
      *>   condition was raised"): AFTER. A compiler that refused
      *>   RESUME in a declarative, or resumed elsewhere, fails.
       >>TURN EC-USER-G2R CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RSM01.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION. USE AFTER EXCEPTION CONDITION EC-USER-G2R.
       D1-P.
           DISPLAY "DECL".
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       M SECTION.
       MAIN-P.
           RAISE EXCEPTION EC-USER-G2R.
           DISPLAY "AFTER".
           STOP RUN.
