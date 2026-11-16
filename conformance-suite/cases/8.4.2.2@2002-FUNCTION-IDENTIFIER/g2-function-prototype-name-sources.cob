      *>--- conformance-case
      *> id: g2-function-prototype-name-sources
      *> rules: [2002-SR-8.4.2.2.2-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.4.2.2.2 SR3 - "Function-prototype-name-1 shall be the
      *> user-function-name of the containing function definition or a
      *> function prototype specified in the REPOSITORY paragraph."
      *> Both branches are written:
      *> - G2FACT references itself as FUNCTION G2FACT with no
      *>   REPOSITORY paragraph of its own: the user-function-name of
      *>   the containing function definition. A user-defined function
      *>   "always possesses the recursive attribute and may call
      *>   itself" (§2002:9.4), so G2FACT (5) = 5*4*3*2*1 = 120;
      *> - G2MAIN names G2DBL and G2FACT in its REPOSITORY paragraph:
      *>   FUNCTION G2DBL (A) = 21 * 2 = 0042.
      *> Each leg can fail: a refused name fails the compile, a wrong
      *> activation prints another value. The refused half is
      *> g2-function-prototype-not-in-repository beside it.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. G2DBL.
       DATA DIVISION.
       LINKAGE SECTION.
       01  X   PIC 9(4).
       01  R   PIC 9(4).
       PROCEDURE DIVISION USING X RETURNING R.
           COMPUTE R = X * 2.
           GOBACK.
       END FUNCTION G2DBL.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. G2FACT.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01  M   PIC 9(2).
       LINKAGE SECTION.
       01  N   PIC 9(2).
       01  F   PIC 9(6).
       PROCEDURE DIVISION USING N RETURNING F.
           IF N NOT > 1
               MOVE 1 TO F
           ELSE
               COMPUTE M = N - 1
               COMPUTE F = N * FUNCTION G2FACT (M)
           END-IF.
           GOBACK.
       END FUNCTION G2FACT.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2MAIN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION G2DBL
           FUNCTION G2FACT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A   PIC 9(4) VALUE 21.
       01  A5  PIC 9(2) VALUE 5.
       01  W   PIC 9(4).
       01  W6  PIC 9(6).
       PROCEDURE DIVISION.
           COMPUTE W = FUNCTION G2DBL (A).
           DISPLAY "P1 " W.
           COMPUTE W6 = FUNCTION G2FACT (A5).
           DISPLAY "P2 " W6.
           STOP RUN.
       END PROGRAM G2MAIN.
