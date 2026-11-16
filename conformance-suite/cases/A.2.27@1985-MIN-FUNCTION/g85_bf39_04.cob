      *>--- conformance-case
      *> id: g85_bf39_04
      *> rules: [1989-FMT-A.2.27.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.27.2 - FUNCTION MIN ( { argument-1 } ... ): one
      *> argument (N1), three integer arguments (N2), three numeric
      *> arguments (N3), and four and three alphanumeric arguments
      *> (N4, N5) are all legal forms of the repetition; a compiler
      *> that refuses one rejects legal source, and one that reads
      *> only some arguments returns a wrong least value
      *> (§1989:A.2.27.4 RV1): N2 03, N3 -1.5, N4 [W], N5 [CA].
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3904.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-I PIC 99.
       01 W-S PIC S9V9.
       01 W-N PIC -9.9.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "N1=[" FUNCTION MIN ("Q") "]".
           COMPUTE W-I = FUNCTION MIN (11 3 7).
           DISPLAY "N2=" W-I.
           COMPUTE W-S = FUNCTION MIN (2.5 3 -1.5).
           MOVE W-S TO W-N.
           DISPLAY "N3=" W-N.
           DISPLAY "N4=[" FUNCTION MIN ("Z" "Y" "X" "W") "]".
           DISPLAY "N5=[" FUNCTION MIN ("CB" "CA" "D") "]".
           STOP RUN.
