      *>--- conformance-case
      *> id: pb5609-debugging-mode-without-name-at-85
      *> rules: [2002-GR-12.2.4.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5609 - the edition half of
      *> pb5609_debugging_mode_without_name. §1985:VI.4.3.2 prints
      *>   SOURCE-COMPUTER. [ computer-name [ WITH DEBUGGING MODE ] . ]
      *> the clause inside the bracket that holds the REQUIRED name, so the
      *> name-less clause is not a 1985 form; §2002:12.2.4.1 gives the
      *> clause a bracket of its own. Expected: COBOL0900 at 85, the
      *> 2002 relaxation computer-name-optional-2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5609R8.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. WITH DEBUGGING MODE.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "NO-NAME".
           STOP RUN.
