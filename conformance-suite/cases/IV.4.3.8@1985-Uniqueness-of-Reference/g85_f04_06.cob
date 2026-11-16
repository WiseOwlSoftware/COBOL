      *>--- conformance-case
      *> id: g85_f04_06
      *> rules: [1985-SR-IV.4.3.8.1-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.8.1 SR2 - a user-defined name is uniquely referenced
      *> when "It is unique within the context of a REDEFINES clause."
      *> §1985:VI.5.10.3 SR7: "Data-name-2 must not be qualified even if it
      *> is not unique". F, FR, F1 and F2 are each declared in both G1 and
      *> G2, and each REDEFINES F names the unqualified, non-unique F: the
      *> placement makes it unique, so the program is legal. Every other
      *> reference is qualified. G1 keeps "1234", G2 "5678"; F1 OF G2 is
      *> "56", F2 OF FR OF G1 is "34"; the MOVE to F2 OF G2 goes through
      *> G2's redefinition, so F OF G2 becomes "56XY" and G1 is untouched.
      *> A compiler that refused the unqualified REDEFINES F, or bound it to
      *> G1's F, fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0406.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G1.
           05  F   PIC X(4) VALUE "1234".
           05  FR  REDEFINES F.
               10  F1  PIC XX.
               10  F2  PIC XX.
       01  G2.
           05  F   PIC X(4) VALUE "5678".
           05  FR  REDEFINES F.
               10  F1  PIC XX.
               10  F2  PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "A " F1 OF G2.
           DISPLAY "B " F2 OF FR OF G1.
           MOVE "XY" TO F2 OF G2.
           DISPLAY "C " F OF G2.
           DISPLAY "D " F OF G1.
           STOP RUN.
