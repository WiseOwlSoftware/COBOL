      *>--- conformance-case
      *> id: pb4305_exit_program_not_last_2002
      *> rules: [FMT-14.9.14.2]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4305 - EXIT PROGRAM may stand before other
      *>   statements of its sequence (2002 Annex F.2 item 50);
      *>   executed in a called program it returns control to the
      *>   caller, so the statement after it does not run. Expected:
      *>   "IN" then "BACK". It fails if the program is refused or the
      *>   statement after EXIT PROGRAM runs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4305EP.
       PROCEDURE DIVISION.
       MAIN-P.
           CALL "PB4305ES"
           DISPLAY "BACK"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4305ES.
       PROCEDURE DIVISION.
       MAIN-S.
           DISPLAY "IN"
           EXIT PROGRAM
           DISPLAY "NOT REACHED".
       END PROGRAM PB4305ES.
       END PROGRAM PB4305EP.
