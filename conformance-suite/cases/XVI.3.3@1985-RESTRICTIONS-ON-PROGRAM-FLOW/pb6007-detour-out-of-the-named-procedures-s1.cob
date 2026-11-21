      *>--- conformance-case
      *> id: pb6007-detour-out-of-the-named-procedures-s1
      *> rules: [1985-SR-XVI.3.3.2-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6007 - the accepting control of
      *> pb6007-perform-reach-out-and-back-85. PERFORM A-SEC THRU C-SEC
      *> names A-SEC and C-SEC (independent segment 60) over B-SEC
      *> (segment 10). A-1 goes to D-1, outside the named procedures
      *> and in segment 60 too, which goes to C-1: the walk follows the
      *> path out and back, and no path reaches B-SEC (A-1 and D-1 each
      *> end in a GO TO), so the executed range lies wholly in segment
      *> 60 (§1985:XVI.3.3.2 SR1). Output: A, D, C, DONE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6007S1.
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       M-1.
           PERFORM A-SEC THRU C-SEC.
           DISPLAY "DONE".
           STOP RUN.
       A-SEC SECTION 60.
       A-1.
           DISPLAY "A".
           GO TO D-1.
       B-SEC SECTION 10.
       B-1.
           DISPLAY "B".
       C-SEC SECTION 60.
       C-1.
           DISPLAY "C".
       D-SEC SECTION 60.
       D-1.
           DISPLAY "D".
           GO TO C-1.
