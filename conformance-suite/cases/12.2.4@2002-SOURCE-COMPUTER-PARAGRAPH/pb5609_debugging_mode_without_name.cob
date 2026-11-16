      *>--- conformance-case
      *> id: pb5609_debugging_mode_without_name
      *> rules: [2002-GR-12.2.4.3-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5609. §2002:12.2.4.1 prints two independent brackets,
      *>   SOURCE-COMPUTER. [ computer-name-1 ] [ WITH DEBUGGING MODE ] .
      *> so the clause stands without computer-name-1, and §2002:12.2.4.3
      *> GR3: "When the SOURCE-COMPUTER paragraph is specified, but
      *> computer-name-1 is not specified, the computer upon which the
      *> source unit is being compiled is the source computer".
      *> §2002:12.2.4.3 GR4: "If the DEBUGGING MODE clause is specified in
      *> a source unit, all debugging lines are compiled as if the
      *> debugging indicator was replaced by three spaces", and GR1 makes
      *> the clause "apply to the source unit in which they are explicitly
      *> or implicitly specified and to any source unit contained within
      *> that source unit". The clause and the debugging lines are obsolete
      *> at 2002 (COBOL0903 is a warning and does not fail the case).
      *> DERIVATION. The paragraph compiles; the outer program's debugging
      *> line is source (DEBUG-ON), and so is the contained program's
      *> (NESTED-DEBUG), in statement order:
      *>   NO-NAME, DEBUG-ON, NESTED, NESTED-DEBUG.
      *> Each leg fails if the paragraph is refused (no output), if the
      *> clause is dropped (a debugging line read as a comment loses
      *> DEBUG-ON), or if the contained program does not inherit it
      *> (NESTED-DEBUG missing). Refused at 85, where §1985:VI.4.3.2 nests
      *> the clause inside the name's bracket:
      *> pb5609-debugging-mode-without-name-at-85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5609DM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. WITH DEBUGGING MODE.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "NO-NAME".
      D    DISPLAY "DEBUG-ON".
           CALL "PB5609NS".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5609NS.
       PROCEDURE DIVISION.
       NESTED-PARA.
           DISPLAY "NESTED".
      D    DISPLAY "NESTED-DEBUG".
           EXIT PROGRAM.
       END PROGRAM PB5609NS.
       END PROGRAM PB5609DM.
