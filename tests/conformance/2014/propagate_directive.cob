      *> >>PROPAGATE directive (ISO 7.3.21, COBOL-2002+): recognized and
      *> edition-gated (COBOLNET0883 below 2002). It controls automatic
      *> exception-condition propagation to the activating runtime element
      *> (GR1/GR2, default OFF per GR4); the RUNTIME propagation semantics are
      *> the deferred PHASE-13 EC work, so here the directive is
      *> recognized-and-consumed and the program compiles and runs.
      >>PROPAGATE ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PROPAGATE-DIRECTIVE.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "PROPAGATE OK".
           STOP RUN.
