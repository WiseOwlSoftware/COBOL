      *>--- conformance-case
      *> id: pb4491-function-prototype-pointer-formal-two-names
      *> rules: [SR-10.6.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4491 - the function twin of pb4491-prototype-
      *> pointer-formal-two-names: a function prototype whose formal
      *> is a FUNCTION-POINTER TO F1 and its definition whose formal is
      *> one TO F2 do not have the same signature, though F1 and F2 do
      *> (USING 9(4) RETURNING 9(6) each). 8.13 (cite.py --check 8.13
      *> -> OK): the signature is "the description of the parameters";
      *> a pointer formal's description is its USAGE clause, the TO
      *> phrase compared by the prototype it names (docs/CONFORMANCE.md
      *> D-RP1). So 10.6.2 SR3 (OK 10.6.2 3)) "the signatures of these
      *> two compilation units shall be the same" refuses the pair
      *> (COBOL1513).
       IDENTIFICATION DIVISION.
       FUNCTION-ID. F1 IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       01 L-R PIC 9(6).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       END FUNCTION F1.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. F2 IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-Y PIC 9(4).
       01 L-S PIC 9(6).
       PROCEDURE DIVISION USING L-Y RETURNING L-S.
       END FUNCTION F2.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. FX IS PROTOTYPE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION F1.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-P USAGE FUNCTION-POINTER TO F1.
       01 L-R PIC 9.
       PROCEDURE DIVISION USING L-P RETURNING L-R.
       END FUNCTION FX.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. FX.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION F2.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-P USAGE FUNCTION-POINTER TO F2.
       01 L-R PIC 9.
       PROCEDURE DIVISION USING L-P RETURNING L-R.
       FX-P.
           MOVE 1 TO L-R
           GOBACK.
       END FUNCTION FX.
