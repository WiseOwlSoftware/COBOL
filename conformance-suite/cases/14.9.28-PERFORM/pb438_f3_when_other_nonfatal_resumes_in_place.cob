*>--- conformance-case
*> id: pb438_f3_when_other_nonfatal_resumes_in_place
*> rules: [GR-14.9.28.4-18, GR-14.9.28.4-19, GR-14.9.28.4-20]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB438 - where control goes after WHEN OTHER's imperative-statement-3 (docs/CONFORMANCE.md §3,
      *> "where control goes after WHEN OTHER's imperative-statement-3").
      *>
      *> THE RULES, and every expected value below derived from them (no observation):
      *>  14.9.28.4 GR18 - "If during the execution of imperative-statement-1 any exception condition that is not
      *>   associated with any exception condition specified in a WHEN phrase is raised and there is a WHEN OTHER
      *>   phrase, imperative-statement-3 is executed." It then prints TWO consecutive sentences naming different
      *>   destinations: "control is passed to the end of the PERFORM statement or, if WHEN COMMON is specified, to
      *>   imperative-statement-4", and "control is passed as indicated in General rule 14.9.29 or, if WHEN COMMON
      *>   is specified, to imperative-statement-4" (a clause number where a general-rule number belongs).
      *>  14.9.28.4 GR20 - "If an exception-condition was raised during the processing of imperative-statement-1 that
      *>   caused a transfer to the imperative-statement in a WHEN phrase, and the processing in General rules 17, 18,
      *>   and 19 is completed, return depends on whether the exception condition was fatal or nonfatal. If the
      *>   exception condition was nonfatal, execution continues with an implicit CONTINUE statement immediately
      *>   following the statement in imperative-statement-1 in which the exception condition occurred."
      *>  GR20 names General rule 18, so the destination after imperative-statement-3 is GR20's: the second sentence,
      *>  with its misprinted reference read as General rule 20, exactly as GR17 and GR19 word it. A nonfatal
      *>  condition therefore resumes IN PLACE and the rest of imperative-statement-1 still runs - with or without a
      *>  WHEN COMMON (GR19: imperative-statement-4 completes "as indicated in General rule 20").
      *>
      *> THE PROBE has NO RESUME in either WHEN OTHER: a RESUME NEXT STATEMENT there would print the same lines under
      *> both sentences and so tell them apart not at all. EC-USER-DEMO is a user-defined (nonfatal) exception-name
      *> that no WHEN names, EC-ALL checking is on (GR14: WHEN OTHER processes only conditions enabled where they are
      *> detected), and each RAISE is followed by a DISPLAY that only the resume-in-place reading reaches.
      *>
      *> EXPECTED (GR18 + GR20, then GR18 + GR19 + GR20):
      *>   OTHER-1 AFTER-1 DONE-1       (imp-3, then the statement after the RAISE, then the end of the PERFORM)
      *>   OTHER-2 COMMON-2 AFTER-2     (imp-3, imp-4 per GR18/GR19, then GR20's resume)
      *>   DONE-2
      *> The exception-checking PERFORM is new in COBOL-2023 (Annex E.3.3 item 36), so 2023 is the whole edition window.
       >>TURN EC-ALL CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB438OTH.
       PROCEDURE DIVISION.
       MAIN-P.
           PERFORM
               RAISE EXCEPTION EC-USER-DEMO
               DISPLAY "AFTER-1"
           WHEN EC-BOUND-SUBSCRIPT
               DISPLAY "SPECIFIC-1"
           WHEN OTHER
               DISPLAY "OTHER-1"
           END-PERFORM.
           DISPLAY "DONE-1".
           PERFORM
               RAISE EXCEPTION EC-USER-DEMO
               DISPLAY "AFTER-2"
           WHEN EC-BOUND-SUBSCRIPT
               DISPLAY "SPECIFIC-2"
           WHEN OTHER
               DISPLAY "OTHER-2"
           WHEN COMMON
               DISPLAY "COMMON-2"
           END-PERFORM.
           DISPLAY "DONE-2".
           STOP RUN.
