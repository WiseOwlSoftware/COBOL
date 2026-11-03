      *>--- conformance-case
      *> id: pb4305-exit-program-not-last-at-85
      *> rules: [FMT-14.9.14.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4305 - the edition half of
      *>   pb4305_exit_program_not_last_2002. COBOL-85 required EXIT
      *>   PROGRAM to be the last statement of its sequence of
      *>   imperative statements (2002 Annex F.2 item 50). Expected:
      *>   COBOL0900 at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4305NEP.
       PROCEDURE DIVISION.
       MAIN-P.
           EXIT PROGRAM
           DISPLAY "AFTER".
