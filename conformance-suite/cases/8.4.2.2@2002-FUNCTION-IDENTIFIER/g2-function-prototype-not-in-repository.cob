      *>--- conformance-case
      *> id: g2-function-prototype-not-in-repository
      *> rules: [2002-SR-8.4.2.2.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.4.2.2.2 SR3 - "Function-prototype-name-1 shall be the
      *> user-function-name of the containing function definition or a
      *> function prototype specified in the REPOSITORY paragraph."
      *> G2NRM is a program, not the function G2NDBL, and has no
      *> REPOSITORY paragraph, so FUNCTION G2NDBL (A) names neither and
      *> is refused. The accepted half is
      *> g2-function-prototype-name-sources beside it.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. G2NDBL.
       DATA DIVISION.
       LINKAGE SECTION.
       01  X   PIC 9(4).
       01  R   PIC 9(4).
       PROCEDURE DIVISION USING X RETURNING R.
           COMPUTE R = X * 2.
           GOBACK.
       END FUNCTION G2NDBL.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2NRM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A   PIC 9(4) VALUE 21.
       01  W   PIC 9(4).
       PROCEDURE DIVISION.
           COMPUTE W = FUNCTION G2NDBL (A).
           DISPLAY W.
           STOP RUN.
       END PROGRAM G2NRM.
