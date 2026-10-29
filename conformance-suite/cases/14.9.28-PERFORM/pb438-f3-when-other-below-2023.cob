*>--- conformance-case
*> id: pb438-f3-when-other-below-2023
*> rules: [GR-14.9.28.4-18]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB438 - the negative half of
      *> conformance:cases/14.9.28-PERFORM/pb438_f3_when_other_nonfatal_resumes_in_place. WHEN OTHER belongs to the
      *> exception-checking (Format 3) PERFORM, a COBOL-2023 introduction (Annex E.3.3 item 36), so below 2023 the
      *> statement is refused (COBOL0900) and nothing is dropped silently.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB438OTHN.
       PROCEDURE DIVISION.
       MAIN-P.
           PERFORM
               CONTINUE
           WHEN EC-USER-DEMO
               CONTINUE
           WHEN OTHER
               CONTINUE
           END-PERFORM.
           STOP RUN.
