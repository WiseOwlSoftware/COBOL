      *>--- conformance-case
      *> id: g2_exit_program_main_is_continue
      *> rules: [2002-GR-14.8.13.3-5]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.13.3 GR5 - "If the EXIT PROGRAM statement is
      *>   executed in a program that is not under the control of a
      *>   calling runtime element, the EXIT PROGRAM statement is
      *>   treated as if it were a CONTINUE statement. No exception
      *>   condition is raised even if the RAISING phrase is specified."
      *> G2EXT06 is the run unit's first program, so nothing calls it.
      *> EXPECTED (derived): the plain EXIT PROGRAM is a CONTINUE, so A
      *>   is followed by B; the EXIT PROGRAM RAISING EXCEPTION
      *>   EC-SIZE-OVERFLOW is also a CONTINUE, so C follows; and no
      *>   exception condition is raised, so the declarative D1, which
      *>   checking (TURN ... CHECKING ON) would run for a raised
      *>   EC-SIZE-OVERFLOW, never prints DECL-RAN.
      *> FAILS IF: either EXIT PROGRAM ends the program (B or C lost),
      *>   or the RAISING phrase raises EC-SIZE-OVERFLOW (DECL-RAN
      *>   appears or the run ends abnormally).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2EXT06.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER EXCEPTION CONDITION EC-SIZE-OVERFLOW.
       D1-P.
           DISPLAY "DECL-RAN".
       END DECLARATIVES.
       M SECTION.
       M1.
       >>TURN EC-SIZE-OVERFLOW CHECKING ON
           DISPLAY "A".
           EXIT PROGRAM.
           DISPLAY "B".
           EXIT PROGRAM RAISING EXCEPTION EC-SIZE-OVERFLOW.
           DISPLAY "C".
           STOP RUN.
       END PROGRAM G2EXT06.
