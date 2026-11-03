      *>--- conformance-case
      *> id: pb4491-function-prototype-pointer-formal-typed-untyped
      *> rules: [SR-10.6.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4491 (branch review R1-1) - the function twin of
      *> pb4491-prototype-pointer-formal-two-type-names, on the arm
      *> where one side is untyped: a function prototype whose formal
      *> is an untyped FUNCTION-POINTER TO F1 and its definition whose
      *> formal is TO F1 through type TF do not have the same
      *> signature. 8.13 (cite.py --check 8.13 -> OK) puts the "type
      *> declarations required for the description of parameters and
      *> returning items" in the signature, and only one side has one.
      *> Were they admitted, a caller checked against the untyped
      *> prototype formal could pass a pointer of any type-name, which
      *> the definition's typed formal refuses BY REFERENCE
      *> (14.8.2.3.2, OK 14.8.2.3.2 4): "both shall be restricted and
      *> of the same type"). So 10.6.2 SR3 (OK 10.6.2 3)) "the
      *> signatures of these two compilation units shall be the same"
      *> refuses the pair (COBOL1513).
       IDENTIFICATION DIVISION.
       FUNCTION-ID. F1 IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       01 L-R PIC 9(6).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       END FUNCTION F1.
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
           FUNCTION F1.
       DATA DIVISION.
       LINKAGE SECTION.
       01 TF IS TYPEDEF USAGE FUNCTION-POINTER TO F1.
       01 L-P TYPE TF.
       01 L-R PIC 9.
       PROCEDURE DIVISION USING L-P RETURNING L-R.
       FX-P.
           MOVE 1 TO L-R
           GOBACK.
       END FUNCTION FX.
