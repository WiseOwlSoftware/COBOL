*>--- conformance-case
*> id: pb374_ec_mcs_names_raise
*> rules: [GR-14.9.29.4-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB374, PB4744 - the asynchronous messaging facility's
      *> exception-names are ordinary 2023 level-3 names (Table 13,
      *> ISO 14.6.13.1.6: EC-MCS-INVALID-TAG, EC-MCS-MESSAGE-LENGTH, all
      *> nonfatal). A RAISE raises one like any level-3 name: 14.9.29.4
      *> GR1 "the associated exception condition is raised", with
      *> checking enabled by the TURN directive for the declarative and
      *> by the WHEN phrase for the PERFORM. Both are NONFATAL, so
      *> control continues after the RAISE. (SEND and RECEIVE raise the
      *> same names; their cases are under 14.9.31 and 14.9.38.)
       >>TURN EC-MCS-INVALID-TAG CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB374EC.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER EXCEPTION CONDITION EC-MCS-INVALID-TAG.
       D1-P.
           DISPLAY "DECLARATIVE "
               FUNCTION TRIM (FUNCTION EXCEPTION-STATUS).
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           RAISE EXCEPTION EC-MCS-INVALID-TAG.
           DISPLAY "AFTER RAISE".
           PERFORM
               RAISE EXCEPTION EC-MCS-MESSAGE-LENGTH
           WHEN EC-MCS-MESSAGE-LENGTH
               DISPLAY "WHEN "
                   FUNCTION TRIM (FUNCTION EXCEPTION-STATUS)
           END-PERFORM.
           DISPLAY "DONE".
           STOP RUN.
