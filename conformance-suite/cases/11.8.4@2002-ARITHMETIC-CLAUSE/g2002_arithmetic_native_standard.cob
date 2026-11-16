      *>--- conformance-case
      *> id: g2002_arithmetic_native_standard
      *> rules: [2002-FMT-11.8.4.1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:11.8.4.1 - the ARITHMETIC clause offers exactly two
      *>   alternatives, NATIVE and STANDARD; IS is not underlined on the
      *>   printed page (PDF p206), so it is optional. Both alternatives
      *>   are written here, each in its own outermost program's OPTIONS
      *>   paragraph (§2002:11.8.1): G2ARI01 says ARITHMETIC IS NATIVE,
      *>   G2ARI01S says ARITHMETIC STANDARD with IS omitted.
      *> Each program computes an exact integer result (7 * 6 = 42 into
      *>   PIC 9(4)), one that native (§2002:11.8.4.2 GR1) and standard
      *>   (§2002:11.8.4.2 GR2) arithmetic must both deliver, so the
      *>   golden pins the accepted FORMAT and not the mode's precision.
      *> Fails if either alternative, or the IS-less spelling, is refused
      *>   at 2002, or if the STANDARD program is not reached.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ARI01.
       OPTIONS.
           ARITHMETIC IS NATIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC 9(4).
       PROCEDURE DIVISION.
           COMPUTE R = 7 * 6.
           DISPLAY "NATIVE " R.
           CALL "G2ARI01S".
           STOP RUN.
       END PROGRAM G2ARI01.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ARI01S.
       OPTIONS.
           ARITHMETIC STANDARD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC 9(4).
       PROCEDURE DIVISION.
           COMPUTE R = 7 * 6.
           DISPLAY "STANDARD " R.
           GOBACK.
       END PROGRAM G2ARI01S.
