      *>--- conformance-case
      *> id: pb6007-perform-reach-out-and-back-85
      *> rules: [1985-SR-XVI.3.3.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6007 - PERFORM O30 THRU O60 from segment 10 names
      *> O30 (segment 30) and O60 (independent segment 60). O30-1 goes
      *> to O20-1, outside the named procedures, which goes back to
      *> O30-X, and O30-X falls into O60: the range "includes all
      *> statements that are executed as the result of a transfer of
      *> control by CALL, EXIT, GO TO, and PERFORM statements in the
      *> range of the PERFORM statement" (§1985:VI.6.21.4 GR11), so it
      *> holds O30 and O60, neither wholly non-independent nor wholly
      *> one independent segment (§1985:XVI.3.3.2). Refused COBOL6877;
      *> it was accepted while the walk dropped the path that leaves
      *> the named procedures and comes back.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6007PR.
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       M-1.
           GO TO MRG-1.
       MRG-SEC SECTION 10.
       MRG-1.
           PERFORM O30 THRU O60.
           DISPLAY "DONE".
           STOP RUN.
       O20 SECTION 20.
       O20-1.
           DISPLAY "O20".
           GO TO O30-X.
       O30 SECTION 30.
       O30-1.
           GO TO O20-1.
       O30-X.
           DISPLAY "O30-X".
       O60 SECTION 60.
       O60-1.
           DISPLAY "O60".
